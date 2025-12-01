' IsoTpHandler.vb
' ISO 15765-2 (ISO-TP) protokol handler'ı
' Multi-frame CAN mesajlarını işler
'
' Frame Tipleri:
'   Single Frame (SF):     0X = X byte veri (X: 1-7)
'   First Frame (FF):      1X XX = Toplam uzunluk, ilk 6 byte veri
'   Consecutive Frame (CF): 2X = Sequence number (0-F), 7 byte veri
'   Flow Control (FC):     3X YY ZZ = Flag, Block Size, STmin

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Linq

Namespace Services

    ''' <summary>
    ''' ISO-TP protokol handler'ı
    ''' Multi-frame CAN mesajlarını işler
    ''' </summary>
    Public Class IsoTpHandler

#Region "Frame Types"

        ''' <summary>
        ''' ISO-TP frame tipleri
        ''' </summary>
        Public Enum FrameType
            SingleFrame = 0
            FirstFrame = 1
            ConsecutiveFrame = 2
            FlowControl = 3
        End Enum

#End Region

#Region "State Machine"

        ''' <summary>
        ''' ISO-TP durum makinesi
        ''' </summary>
        Private Enum State
            Idle
            ReceivingMultiFrame
            SendingMultiFrame
            WaitingFlowControl
        End Enum

#End Region

#Region "Events"

        ''' <summary>
        ''' Mesaj alındığında tetiklenir
        ''' </summary>
        Public Event OnMessageReceived(sourceId As Integer, data As Byte())

        ''' <summary>
        ''' Mesaj gönderildiğinde tetiklenir
        ''' </summary>
        Public Event OnMessageSent(targetId As Integer, success As Boolean)

        ''' <summary>
        ''' Hata oluştuğunda tetiklenir
        ''' </summary>
        Public Event OnError(message As String)

        ''' <summary>
        ''' İlerleme durumu değiştiğinde tetiklenir
        ''' </summary>
        Public Event OnProgress(current As Integer, total As Integer)

#End Region

#Region "Configuration Properties"

        ''' <summary>
        ''' Flow Control Block Size (0 = sınırsız)
        ''' </summary>
        Public Property BlockSize As Byte = 0

        ''' <summary>
        ''' Flow Control STmin (frame arası bekleme, ms)
        ''' </summary>
        Public Property STmin As Byte = 10

        ''' <summary>
        ''' Genel timeout (ms)
        ''' </summary>
        Public Property Timeout As Integer = 1000

#End Region

#Region "Private Fields"

        ' CANSender referansı
        Private _canSender As CANSender

        ' Source ID (ECU yanıt adresi)
        Private _sourceId As Integer = &H7E8

        ' Target ID (OBD broadcast)
        Private _targetId As Integer = &H7DF

        ''' <summary>
        ''' Source ID (ECU yanıt adresi)
        ''' </summary>
        Public Property SourceId As Integer
            Get
                Return _sourceId
            End Get
            Set(value As Integer)
                _sourceId = value
            End Set
        End Property

        ''' <summary>
        ''' Target ID (OBD broadcast)
        ''' </summary>
        Public Property TargetId As Integer
            Get
                Return _targetId
            End Get
            Set(value As Integer)
                _targetId = value
            End Set
        End Property

        ' Alım (RX) buffer'ı
        Private _rxBuffer As New List(Of Byte)
        Private _rxExpectedLength As Integer = 0
        Private _rxSequence As Byte = 0
        Private _rxFrameId As Integer = 0

        ' Gönderim (TX) buffer'ı
        Private _txBuffer As New List(Of Byte)
        Private _txSequence As Byte = 0
        Private _txTotalLength As Integer = 0
        Private _txFrameId As Integer = 0

        ' Durum makinesi
        Private _currentState As State = State.Idle

        ' Thread safety
        Private ReadOnly _lockObject As New Object()

        ' Timeout timer
        Private _timeoutTimer As Timer
        Private _timeoutStartTime As DateTime

        ' Flow Control bekleme
        Private _flowControlReceived As Boolean = False
        Private ReadOnly _flowControlEvent As New Threading.ManualResetEventSlim(False)

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Yeni IsoTpHandler örneği oluşturur
        ''' </summary>
        ''' <param name="canSender">CANSender referansı</param>
        Public Sub New(canSender As CANSender)
            If canSender Is Nothing Then
                Throw New ArgumentNullException("canSender")
            End If

            _canSender = canSender
            _currentState = State.Idle
            _timeoutTimer = New Timer(AddressOf OnTimeout, Nothing, -1, -1)
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Gelen CAN frame'ini işler
        ''' </summary>
        ''' <param name="frameId">CAN frame ID</param>
        ''' <param name="data">Frame data (8 byte)</param>
        Public Sub ProcessFrame(frameId As Integer, data As Byte())
            Try
                SyncLock _lockObject
                    If data Is Nothing OrElse data.Length = 0 Then
                        Return
                    End If

                    ' İlk byte'ın ilk nibble'ına göre frame tipini belirle
                    Dim firstByte As Byte = data(0)
                    Dim frameTypeValue As Integer = (firstByte And &HF0) >> 4

                    Select Case frameTypeValue
                        Case 0 To 7
                            ' Single Frame (0X = X byte veri)
                            HandleSingleFrame(frameId, data)
                        Case 1
                            ' First Frame (1X XX = Toplam uzunluk)
                            HandleFirstFrame(frameId, data)
                        Case 2
                            ' Consecutive Frame (2X = Sequence)
                            HandleConsecutiveFrame(frameId, data)
                        Case 3
                            ' Flow Control (3X YY ZZ)
                            HandleFlowControl(frameId, data)
                        Case Else
                            RaiseEvent OnError($"Geçersiz frame tipi: {frameTypeValue}")
                    End Select
                End SyncLock
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.ProcessFrame")
                RaiseEvent OnError($"Frame işleme hatası: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Mesaj gönderir (multi-frame destekli)
        ''' </summary>
        ''' <param name="targetId">Hedef CAN ID</param>
        ''' <param name="data">Gönderilecek data</param>
        ''' <returns>Başarılı ise True</returns>
        Public Async Function SendMessage(targetId As Integer, data As Byte()) As Task(Of Boolean)
            Try
                If data Is Nothing OrElse data.Length = 0 Then
                    RaiseEvent OnError("Gönderilecek data boş")
                    Return False
                End If

                Dim canSend As Boolean = False
                SyncLock _lockObject
                    ' Eğer zaten gönderim yapılıyorsa bekle
                    If _currentState = State.SendingMultiFrame OrElse _currentState = State.WaitingFlowControl Then
                        RaiseEvent OnError("Zaten bir gönderim devam ediyor")
                        Return False
                    End If

                    _txBuffer.Clear()
                    _txBuffer.AddRange(data)
                    _txTotalLength = data.Length
                    _txSequence = 0
                    _txFrameId = targetId

                    ' 8 byte veya daha kısa ise Single Frame gönder
                    If data.Length <= 7 Then
                        Dim sfData As Byte() = BuildSingleFrame(data)
                        _canSender.SendBytes(targetId.ToString("X3"), sfData)
                        RaiseEvent OnMessageSent(targetId, True)
                        Return True
                    Else
                        ' Multi-frame gönder
                        _currentState = State.SendingMultiFrame
                        Dim ffData As Byte() = BuildFirstFrame(data.Length, data)
                        _canSender.SendBytes(targetId.ToString("X3"), ffData)

                        ' Flow Control bekle
                        _currentState = State.WaitingFlowControl
                        StartTimeout()
                        canSend = True
                    End If
                End SyncLock

                If canSend Then
                    ' Flow Control gelene kadar bekle (async)
                    Dim fcReceived As Boolean = Await WaitForFlowControlAsync()

                    If Not fcReceived Then
                        SyncLock _lockObject
                            _currentState = State.Idle
                        End SyncLock
                        RaiseEvent OnError("Flow Control timeout")
                        Return False
                    End If

                    ' Consecutive Frame'leri gönder
                    Await SendConsecutiveFramesAsync(targetId, data)

                    SyncLock _lockObject
                        _currentState = State.Idle
                    End SyncLock
                    RaiseEvent OnMessageSent(targetId, True)
                    Return True
                End If

                Return False
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.SendMessage")
                RaiseEvent OnError($"Gönderme hatası: {ex.Message}")
                SyncLock _lockObject
                    _currentState = State.Idle
                End SyncLock
                Return False
            End Try
        End Function

#End Region

#Region "Private Methods - Frame Handling"

        ''' <summary>
        ''' Single Frame işler
        ''' </summary>
        Private Sub HandleSingleFrame(frameId As Integer, data As Byte())
            Try
                Dim firstByte As Byte = data(0)
                Dim dataLength As Integer = firstByte And &H0F

                If dataLength = 0 OrElse dataLength > 7 Then
                    RaiseEvent OnError($"Geçersiz Single Frame uzunluğu: {dataLength}")
                    Return
                End If

                ' Data'yı çıkar (ilk byte hariç)
                Dim messageData As New List(Of Byte)
                For i As Integer = 1 To Math.Min(dataLength, data.Length - 1)
                    messageData.Add(data(i))
                Next

                ' Mesaj tamamlandı
                _currentState = State.Idle
                StopTimeout()

                RaiseEvent OnMessageReceived(frameId, messageData.ToArray())
                RaiseEvent OnProgress(messageData.Count, messageData.Count)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.HandleSingleFrame")
                RaiseEvent OnError($"Single Frame işleme hatası: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' First Frame işler
        ''' </summary>
        Private Sub HandleFirstFrame(frameId As Integer, data As Byte())
            Try
                If data.Length < 2 Then
                    RaiseEvent OnError("First Frame çok kısa")
                    Return
                End If

                ' Toplam uzunluğu al (byte 0-1)
                Dim lengthHigh As Integer = data(0) And &H0F
                Dim lengthLow As Integer = data(1)
                _rxExpectedLength = (lengthHigh << 8) Or lengthLow

                If _rxExpectedLength = 0 OrElse _rxExpectedLength > 4095 Then
                    RaiseEvent OnError($"Geçersiz First Frame uzunluğu: {_rxExpectedLength}")
                    Return
                End If

                ' Buffer'ı temizle ve başlat
                _rxBuffer.Clear()
                _rxSequence = 0
                _rxFrameId = frameId

                ' İlk 6 byte veriyi ekle
                For i As Integer = 2 To Math.Min(7, data.Length - 1)
                    _rxBuffer.Add(data(i))
                Next

                _currentState = State.ReceivingMultiFrame
                StartTimeout()

                ' Flow Control gönder
                SendFlowControl(frameId)

                RaiseEvent OnProgress(_rxBuffer.Count, _rxExpectedLength)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.HandleFirstFrame")
                RaiseEvent OnError($"First Frame işleme hatası: {ex.Message}")
                _currentState = State.Idle
            End Try
        End Sub

        ''' <summary>
        ''' Consecutive Frame işler
        ''' </summary>
        Private Sub HandleConsecutiveFrame(frameId As Integer, data As Byte())
            Try
                If _currentState <> State.ReceivingMultiFrame Then
                    RaiseEvent OnError("Consecutive Frame beklenmiyor")
                    Return
                End If

                If frameId <> _rxFrameId Then
                    RaiseEvent OnError($"Frame ID uyuşmazlığı: beklenen {_rxFrameId:X}, gelen {frameId:X}")
                    Return
                End If

                Dim firstByte As Byte = data(0)
                Dim sequence As Byte = firstByte And &H0F

                ' Sequence kontrolü
                Dim expectedSequence As Byte = (_rxSequence + 1) And &H0F
                If sequence <> expectedSequence Then
                    RaiseEvent OnError($"Sequence hatası: beklenen {expectedSequence}, gelen {sequence}")
                    _currentState = State.Idle
                    StopTimeout()
                    Return
                End If

                _rxSequence = sequence

                ' 7 byte veriyi ekle
                Dim bytesToAdd As Integer = Math.Min(7, _rxExpectedLength - _rxBuffer.Count)
                For i As Integer = 1 To Math.Min(bytesToAdd, data.Length - 1)
                    _rxBuffer.Add(data(i))
                Next

                RaiseEvent OnProgress(_rxBuffer.Count, _rxExpectedLength)

                ' Mesaj tamamlandı mı?
                If _rxBuffer.Count >= _rxExpectedLength Then
                    ' Mesaj tamamlandı
                    Dim messageData As Byte() = _rxBuffer.Take(_rxExpectedLength).ToArray()
                    _currentState = State.Idle
                    StopTimeout()

                    RaiseEvent OnMessageReceived(frameId, messageData)
                    RaiseEvent OnProgress(_rxExpectedLength, _rxExpectedLength)
                End If
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.HandleConsecutiveFrame")
                RaiseEvent OnError($"Consecutive Frame işleme hatası: {ex.Message}")
                _currentState = State.Idle
                StopTimeout()
            End Try
        End Sub

        ''' <summary>
        ''' Flow Control işler
        ''' </summary>
        Private Sub HandleFlowControl(frameId As Integer, data As Byte())
            Try
                If _currentState <> State.WaitingFlowControl Then
                    ' Flow Control beklenmiyor, görmezden gel
                    Return
                End If

                If data.Length < 3 Then
                    RaiseEvent OnError("Flow Control çok kısa")
                    Return
                End If

                Dim flag As Byte = data(0) And &H0F
                Dim blockSize As Byte = data(1)
                Dim stmin As Byte = data(2)

                ' Flag kontrolü
                Select Case flag
                    Case 0
                        ' CTS (Clear To Send) - devam et
                        BlockSize = blockSize
                        STmin = stmin
                        _currentState = State.SendingMultiFrame
                        _flowControlReceived = True
                        _flowControlEvent.Set()
                        StopTimeout()
                    Case 1
                        ' Wait - bekle
                        RaiseEvent OnError("Flow Control: Wait")
                        _currentState = State.Idle
                        _flowControlReceived = False
                        _flowControlEvent.Set()
                        StopTimeout()
                    Case 2
                        ' Overflow/Abort - iptal et
                        RaiseEvent OnError("Flow Control: Overflow/Abort")
                        _currentState = State.Idle
                        _flowControlReceived = False
                        _flowControlEvent.Set()
                        StopTimeout()
                    Case Else
                        RaiseEvent OnError($"Geçersiz Flow Control flag: {flag}")
                        _currentState = State.Idle
                        StopTimeout()
                End Select
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.HandleFlowControl")
                RaiseEvent OnError($"Flow Control işleme hatası: {ex.Message}")
                _currentState = State.Idle
                StopTimeout()
            End Try
        End Sub

#End Region

#Region "Private Methods - Frame Building"

        ''' <summary>
        ''' Single Frame oluşturur
        ''' </summary>
        Private Function BuildSingleFrame(data As Byte()) As Byte()
            Dim length As Integer = Math.Min(data.Length, 7)
            Dim frame(7) As Byte
            frame(0) = CByte(length)
            For i As Integer = 0 To length - 1
                frame(i + 1) = data(i)
            Next
            Return frame
        End Function

        ''' <summary>
        ''' First Frame oluşturur
        ''' </summary>
        Private Function BuildFirstFrame(totalLength As Integer, data As Byte()) As Byte()
            Dim frame(7) As Byte
            frame(0) = CByte(&H10 Or ((totalLength >> 8) And &H0F))
            frame(1) = CByte(totalLength And &HFF)
            Dim bytesToCopy As Integer = Math.Min(6, data.Length)
            For i As Integer = 0 To bytesToCopy - 1
                frame(i + 2) = data(i)
            Next
            Return frame
        End Function

        ''' <summary>
        ''' Consecutive Frame oluşturur
        ''' </summary>
        Private Function BuildConsecutiveFrame(data As Byte(), startIndex As Integer, sequence As Byte) As Byte()
            Dim frame(7) As Byte
            frame(0) = CByte(&H20 Or (sequence And &H0F))
            Dim bytesToCopy As Integer = Math.Min(7, data.Length - startIndex)
            For i As Integer = 0 To bytesToCopy - 1
                frame(i + 1) = data(startIndex + i)
            Next
            Return frame
        End Function

#End Region

#Region "Private Methods - Flow Control"

        ''' <summary>
        ''' Flow Control gönderir
        ''' </summary>
        Private Sub SendFlowControl(sourceId As Integer)
            Try
                Dim fcFrame(7) As Byte
                fcFrame(0) = &H30 ' CTS (Clear To Send)
                fcFrame(1) = BlockSize
                fcFrame(2) = STmin
                ' Geri kalan byte'lar 0

                ' Source ID'den 8 byte önceki adrese gönder (ISO-TP standardı)
                Dim targetId As Integer = sourceId - 8
                _canSender.SendBytes(targetId.ToString("X3"), fcFrame)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.SendFlowControl")
                RaiseEvent OnError($"Flow Control gönderme hatası: {ex.Message}")
            End Try
        End Sub

#End Region

#Region "Private Methods - Consecutive Frame Sending"

        ''' <summary>
        ''' Consecutive Frame'leri gönderir
        ''' </summary>
        Private Async Function SendConsecutiveFramesAsync(targetId As Integer, data As Byte()) As Task
            Try
                Dim startIndex As Integer = 6 ' First Frame'de 6 byte gönderildi
                _txSequence = 0

                While startIndex < data.Length
                    _txSequence = CByte((_txSequence + 1) And &H0F)
                    Dim cfData As Byte() = BuildConsecutiveFrame(data, startIndex, _txSequence)
                    _canSender.SendBytes(targetId.ToString("X3"), cfData)

                    startIndex += 7
                    RaiseEvent OnProgress(startIndex, data.Length)

                    ' STmin kadar bekle
                    If STmin > 0 Then
                        Await Task.Delay(STmin)
                    End If
                End While
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.SendConsecutiveFramesAsync")
                RaiseEvent OnError($"Consecutive Frame gönderme hatası: {ex.Message}")
            End Try
        End Function

#End Region

#Region "Private Methods - Flow Control Waiting"

        ''' <summary>
        ''' Flow Control gelene kadar bekler
        ''' </summary>
        Private Async Function WaitForFlowControlAsync() As Task(Of Boolean)
            Try
                _flowControlReceived = False
                _flowControlEvent.Reset()

                ' Timeout kadar bekle
                Dim received As Boolean = Await Task.Run(Function()
                    Return _flowControlEvent.Wait(Timeout)
                End Function)

                Return received AndAlso _flowControlReceived
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.WaitForFlowControlAsync")
                Return False
            End Try
        End Function

#End Region

#Region "Private Methods - Timeout Management"

        ''' <summary>
        ''' Timeout timer'ı başlatır
        ''' </summary>
        Private Sub StartTimeout()
            _timeoutStartTime = DateTime.Now
            _timeoutTimer.Change(Timeout, -1)
        End Sub

        ''' <summary>
        ''' Timeout timer'ı durdurur
        ''' </summary>
        Private Sub StopTimeout()
            _timeoutTimer.Change(-1, -1)
        End Sub

        ''' <summary>
        ''' Timeout callback
        ''' </summary>
        Private Sub OnTimeout(state As Object)
            Try
                SyncLock _lockObject
                    If (DateTime.Now - _timeoutStartTime).TotalMilliseconds >= Timeout Then
                        RaiseEvent OnError("ISO-TP timeout")
                        _currentState = State.Idle
                        _rxBuffer.Clear()
                        _txBuffer.Clear()
                    End If
                End SyncLock
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.OnTimeout")
            End Try
        End Sub

#End Region

#Region "IDisposable"

        ''' <summary>
        ''' Kaynakları temizler
        ''' </summary>
        Public Sub Dispose()
            Try
                If _timeoutTimer IsNot Nothing Then
                    _timeoutTimer.Dispose()
                    _timeoutTimer = Nothing
                End If
                If _flowControlEvent IsNot Nothing Then
                    _flowControlEvent.Dispose()
                End If
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "IsoTpHandler.Dispose")
            End Try
        End Sub

#End Region

    End Class

End Namespace

