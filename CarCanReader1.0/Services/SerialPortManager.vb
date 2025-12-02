' SerialPortManager.vb
' Thread-safe seri port yönetimi
'
' Özellikler:
'   - ConcurrentQueue ile buffer yönetimi
'   - Proper IDisposable pattern
'   - Automatic reconnection desteği
'   - Event-based asenkron okuma

Imports System.IO.Ports
Imports System.Collections.Concurrent
Imports System.Threading

Namespace Services

    ''' <summary>
    ''' Thread-safe seri port yönetim sınıfı
    ''' </summary>
    Public Class SerialPortManager
        Inherits DisposableBase

#Region "Events"

        ''' <summary>
        ''' Veri alındığında (işlenmiş satır)
        ''' </summary>
        Public Event OnDataReceived(data As String)

        ''' <summary>
        ''' Bağlantı durumu değiştiğinde
        ''' </summary>
        Public Event OnConnectionChanged(isConnected As Boolean)

        ''' <summary>
        ''' Hata oluştuğunda
        ''' </summary>
        Public Event OnError(message As String)

        ''' <summary>
        ''' Ham veri alındığında (debug için)
        ''' </summary>
        Public Event OnRawDataReceived(data As String)

#End Region

#Region "Fields"

        Private _serialPort As SerialPort
        Private ReadOnly _portLock As New Object()
        Private ReadOnly _receiveBuffer As New ThreadSafeBuffer(10000)
        Private ReadOnly _lineBuffer As New System.Text.StringBuilder()

        Private _processingThread As Thread
        Private _isProcessing As Boolean = False
        Private _processingCts As CancellationTokenSource = New CancellationTokenSource()
        Private ReadOnly _bufferSignal As New AutoResetEvent(False)

        ' Ayarlar - Varsayılanlar ConfigManager'dan yüklenir (Initialize'da)
        Private _portName As String = ""
        Private _baudRate As Integer = 500000
        Private _readTimeout As Integer = 1000
        Private _writeTimeout As Integer = 1000
        
        ''' <summary>
        ''' ConfigManager'dan ayarları yükler
        ''' </summary>
        Public Sub LoadConfigDefaults()
            Try
                Dim cfg = ConfigManager.Instance.Serial
                _baudRate = cfg.DefaultBaudRate
                _readTimeout = cfg.ReadTimeout
                _writeTimeout = cfg.WriteTimeout
                If Not String.IsNullOrEmpty(cfg.DefaultPort) Then
                    _portName = cfg.DefaultPort
                End If
            Catch ex As Exception
                ' ConfigManager yüklenemediyse varsayılanları kullan
                Debug.WriteLine($"SerialPortManager.LoadConfigDefaults error: {ex.Message}")
            End Try
        End Sub

        ' Durum
        Private _isConnected As Boolean = False
        Private _lastError As String = ""
        Private _totalBytesReceived As Long = 0
        Private _totalBytesSent As Long = 0

#End Region

#Region "Properties"

        ''' <summary>
        ''' Port adı
        ''' </summary>
        Public Property PortName As String
            Get
                Return _portName
            End Get
            Set(value As String)
                _portName = value
            End Set
        End Property

        ''' <summary>
        ''' Baud rate
        ''' </summary>
        Public Property BaudRate As Integer
            Get
                Return _baudRate
            End Get
            Set(value As Integer)
                _baudRate = value
            End Set
        End Property

        ''' <summary>
        ''' Bağlı mı?
        ''' </summary>
        Public ReadOnly Property IsConnected As Boolean
            Get
                SyncLock _portLock
                    Return _isConnected AndAlso _serialPort IsNot Nothing AndAlso _serialPort.IsOpen
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Son hata mesajı
        ''' </summary>
        Public ReadOnly Property LastError As String
            Get
                Return _lastError
            End Get
        End Property

        ''' <summary>
        ''' Buffer'daki bekleyen satır sayısı
        ''' </summary>
        Public ReadOnly Property PendingLines As Integer
            Get
                Return _receiveBuffer.Count
            End Get
        End Property

        ''' <summary>
        ''' Toplam alınan byte
        ''' </summary>
        Public ReadOnly Property TotalBytesReceived As Long
            Get
                Return _totalBytesReceived
            End Get
        End Property

        ''' <summary>
        ''' Toplam gönderilen byte
        ''' </summary>
        Public ReadOnly Property TotalBytesSent As Long
            Get
                Return _totalBytesSent
            End Get
        End Property

        ''' <summary>
        ''' Ham SerialPort referansı (geriye uyumluluk için)
        ''' </summary>
        Public ReadOnly Property RawPort As SerialPort
            Get
                Return _serialPort
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Varsayılan constructor
        ''' </summary>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Port adı ile constructor
        ''' </summary>
        Public Sub New(portName As String, Optional baudRate As Integer = 500000)
            _portName = portName
            _baudRate = baudRate
        End Sub

#End Region

#Region "Connection Management"

        ''' <summary>
        ''' Seri porta bağlanır
        ''' </summary>
        Public Function Connect() As Boolean
            ThrowIfDisposed()

            SyncLock _portLock
                Try
                    ' Zaten bağlıysa
                    If _serialPort IsNot Nothing AndAlso _serialPort.IsOpen Then
                        Return True
                    End If

                    ' Eski portu temizle
                    CleanupPort()

                    ' Yeni port oluştur
                    _serialPort = New SerialPort()
                    _serialPort.PortName = _portName
                    _serialPort.BaudRate = _baudRate
                    _serialPort.DataBits = 8
                    _serialPort.Parity = Parity.None
                    _serialPort.StopBits = StopBits.One
                    _serialPort.Handshake = Handshake.None
                    _serialPort.ReadTimeout = _readTimeout
                    _serialPort.WriteTimeout = _writeTimeout
                    _serialPort.ReadBufferSize = 65536
                    _serialPort.WriteBufferSize = 4096

                    ' Event handler'ları bağla
                    AddHandler _serialPort.DataReceived, AddressOf SerialPort_DataReceived
                    AddHandler _serialPort.ErrorReceived, AddressOf SerialPort_ErrorReceived

                    ' Portu aç
                    _serialPort.Open()

                    ' Buffer'ları temizle
                    _serialPort.DiscardInBuffer()
                    _serialPort.DiscardOutBuffer()
                    _receiveBuffer.Clear()
                    _lineBuffer.Clear()

                    ' İşleme thread'ini başlat
                    StartProcessingThread()

                    _isConnected = True
                    _lastError = ""

                    RaiseConnectionChanged(True)
                    Return True

                Catch ex As Exception
                    _lastError = ex.Message
                    _isConnected = False
                    RaiseError($"Bağlantı hatası: {ex.Message}")
                    Return False
                End Try
            End SyncLock
        End Function

        ''' <summary>
        ''' Bağlantıyı kapatır
        ''' </summary>
        Public Sub Disconnect()
            SyncLock _portLock
                Try
                    StopProcessingThread()
                    CleanupPort()
                    _isConnected = False
                    RaiseConnectionChanged(False)
                Catch ex As Exception
                    Debug.WriteLine($"Disconnect error: {ex.Message}")
                End Try
            End SyncLock
        End Sub

        ''' <summary>
        ''' Portu temizler
        ''' </summary>
        Private Sub CleanupPort()
            If _serialPort IsNot Nothing Then
                Try
                    RemoveHandler _serialPort.DataReceived, AddressOf SerialPort_DataReceived
                    RemoveHandler _serialPort.ErrorReceived, AddressOf SerialPort_ErrorReceived

                    If _serialPort.IsOpen Then
                        _serialPort.DiscardInBuffer()
                        _serialPort.DiscardOutBuffer()
                        _serialPort.Close()
                    End If

                    _serialPort.Dispose()
                Catch ex As Exception
                    Debug.WriteLine($"CleanupPort error: {ex.Message}")
                End Try

                _serialPort = Nothing
            End If
        End Sub

#End Region

#Region "Data Receiving"

        ''' <summary>
        ''' Serial port data received handler
        ''' </summary>
        Private Sub SerialPort_DataReceived(sender As Object, e As SerialDataReceivedEventArgs)
            If IsDisposed Then Return

            Try
                SyncLock _portLock
                    If _serialPort Is Nothing OrElse Not _serialPort.IsOpen Then Return

                    ' Tüm mevcut veriyi oku
                    Dim bytesToRead = _serialPort.BytesToRead
                    If bytesToRead <= 0 Then Return

                    Dim buffer(bytesToRead - 1) As Byte
                    Dim bytesRead = _serialPort.Read(buffer, 0, bytesToRead)

                    Interlocked.Add(_totalBytesReceived, bytesRead)

                    ' String'e çevir
                    Dim rawData = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead)

                    ' Debug event
                    RaiseEvent OnRawDataReceived(rawData)

                    ' Satırlara böl
                    For Each c As Char In rawData
                        If c = vbCr OrElse c = vbLf Then
                            If _lineBuffer.Length > 0 Then
                                Dim line = _lineBuffer.ToString()
                                _receiveBuffer.Enqueue(line)
                                _lineBuffer.Clear()
                            End If
                        Else
                            _lineBuffer.Append(c)
                        End If
                    Next
                End SyncLock

                _bufferSignal.Set()

            Catch ex As Exception
                Debug.WriteLine($"DataReceived error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Serial port error handler
        ''' </summary>
        Private Sub SerialPort_ErrorReceived(sender As Object, e As SerialErrorReceivedEventArgs)
            _lastError = $"Serial error: {e.EventType}"
            RaiseError(_lastError)
        End Sub

        ''' <summary>
        ''' İşleme thread'ini başlatır
        ''' </summary>
        Private Sub StartProcessingThread()
            If _isProcessing Then Return

            _isProcessing = True
            Try
                _processingCts?.Dispose()
            Catch
            End Try

            _processingCts = New CancellationTokenSource()

            _processingThread = New Thread(Sub() ProcessingLoop(_processingCts.Token))
            _processingThread.IsBackground = True
            _processingThread.Name = "SerialPort_Processing"
            _processingThread.Start()
        End Sub

        ''' <summary>
        ''' İşleme thread'ini durdurur
        ''' </summary>
        Private Sub StopProcessingThread()
            _isProcessing = False

            Try
                _processingCts.Cancel()
                _bufferSignal.Set()
                If _processingThread IsNot Nothing AndAlso _processingThread.IsAlive Then
                    _processingThread.Join(1000)
                End If
            Catch ex As Exception
                Debug.WriteLine($"StopProcessingThread error: {ex.Message}")
            Finally
                _processingThread = Nothing
            End Try
        End Sub

        ''' <summary>
        ''' Buffer işleme döngüsü
        ''' </summary>
        Private Sub ProcessingLoop(token As CancellationToken)
            While _isProcessing AndAlso Not token.IsCancellationRequested
                Try
                    Dim line As String = Nothing
                    Dim hasWork As Boolean = False
                    While _receiveBuffer.TryDequeue(line)
                        hasWork = True
                        If Not String.IsNullOrWhiteSpace(line) Then
                            RaiseDataReceived(line)
                        End If
                    End While
                    If Not hasWork Then
                        WaitHandle.WaitAny(New WaitHandle() {token.WaitHandle, _bufferSignal}, 25)
                    End If

                Catch ex As OperationCanceledException
                    Exit While
                Catch ex As ObjectDisposedException
                    Exit While
                Catch ex As Exception
                    Debug.WriteLine($"ProcessingLoop error: {ex.Message}")
                    Thread.Sleep(50)
                End Try
            End While
        End Sub

#End Region

#Region "Data Sending"

        ''' <summary>
        ''' Veri gönderir (satır sonuyla)
        ''' </summary>
        Public Function WriteLine(data As String) As Boolean
            ThrowIfDisposed()

            SyncLock _portLock
                Try
                    If _serialPort Is Nothing OrElse Not _serialPort.IsOpen Then
                        RaiseError("Port açık değil")
                        Return False
                    End If

                    _serialPort.WriteLine(data)
                    Interlocked.Add(_totalBytesSent, data.Length + 2)
                    Return True

                Catch ex As TimeoutException
                    RaiseError("Yazma zaman aşımı")
                    Return False
                Catch ex As Exception
                    RaiseError($"Yazma hatası: {ex.Message}")
                    Return False
                End Try
            End SyncLock
        End Function

        ''' <summary>
        ''' Ham veri gönderir
        ''' </summary>
        Public Function Write(data As String) As Boolean
            ThrowIfDisposed()

            SyncLock _portLock
                Try
                    If _serialPort Is Nothing OrElse Not _serialPort.IsOpen Then
                        Return False
                    End If

                    _serialPort.Write(data)
                    Interlocked.Add(_totalBytesSent, data.Length)
                    Return True

                Catch ex As Exception
                    RaiseError($"Yazma hatası: {ex.Message}")
                    Return False
                End Try
            End SyncLock
        End Function

        ''' <summary>
        ''' Byte dizisi gönderir
        ''' </summary>
        Public Function WriteBytes(data() As Byte) As Boolean
            ThrowIfDisposed()

            SyncLock _portLock
                Try
                    If _serialPort Is Nothing OrElse Not _serialPort.IsOpen Then
                        Return False
                    End If

                    _serialPort.Write(data, 0, data.Length)
                    Interlocked.Add(_totalBytesSent, data.Length)
                    Return True

                Catch ex As Exception
                    RaiseError($"Yazma hatası: {ex.Message}")
                    Return False
                End Try
            End SyncLock
        End Function

#End Region

#Region "Event Raising"

        Private Sub RaiseDataReceived(data As String)
            Try
                RaiseEvent OnDataReceived(data)
            Catch ex As Exception
                Debug.WriteLine($"RaiseDataReceived error: {ex.Message}")
            End Try
        End Sub

        Private Sub RaiseConnectionChanged(isConnected As Boolean)
            Try
                RaiseEvent OnConnectionChanged(isConnected)
            Catch ex As Exception
                Debug.WriteLine($"RaiseConnectionChanged error: {ex.Message}")
            End Try
        End Sub

        Private Sub RaiseError(message As String)
            Try
                RaiseEvent OnError(message)
            Catch ex As Exception
                Debug.WriteLine($"RaiseError error: {ex.Message}")
            End Try
        End Sub

#End Region

#Region "Static Helpers"

        ''' <summary>
        ''' Mevcut COM portlarını listeler
        ''' </summary>
        Public Shared Function GetAvailablePorts() As String()
            Try
                Return SerialPort.GetPortNames()
            Catch ex As Exception
                Debug.WriteLine($"GetAvailablePorts error: {ex.Message}")
                Return New String() {}
            End Try
        End Function

        ''' <summary>
        ''' Port'un kullanılabilir olup olmadığını test eder
        ''' </summary>
        Public Shared Function IsPortAvailable(portName As String) As Boolean
            Try
                Using testPort As New SerialPort(portName)
                    testPort.Open()
                    testPort.Close()
                    Return True
                End Using
            Catch
                Return False
            End Try
        End Function

#End Region

#Region "IDisposable"

        Protected Overrides Sub DisposeManagedResources()
            StopProcessingThread()
            CleanupPort()
            _receiveBuffer.Clear()

            Try
                _processingCts.Cancel()
                _processingCts.Dispose()
            Catch
            End Try

            Try
                _bufferSignal.Set()
                _bufferSignal.Dispose()
            Catch
            End Try
        End Sub

#End Region

    End Class

End Namespace

