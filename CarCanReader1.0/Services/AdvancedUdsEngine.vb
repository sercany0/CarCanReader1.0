' AdvancedUdsEngine.vb
' Gelişmiş UDS (Unified Diagnostic Services) motoru
' ISO-TP segmentasyon, güvenlik erişimi ve tam UDS protokol desteği
'
' Desteklenen UDS Servisleri:
'   0x10 - DiagnosticSessionControl
'   0x11 - ECUReset
'   0x22 - ReadDataByIdentifier
'   0x2E - WriteDataByIdentifier
'   0x27 - SecurityAccess (Seed/Key)
'   0x2F - InputOutputControlByIdentifier
'   0x31 - RoutineControl
'   0x34 - RequestDownload
'   0x35 - RequestUpload
'   0x36 - TransferData
'   0x37 - RequestTransferExit
'   0x3E - TesterPresent
'   0x14 - ClearDiagnosticInformation
'   0x19 - ReadDTCInformation
'
' Kullanım:
'   Dim uds As New AdvancedUdsEngine()
'   uds.SetSender(canSender)
'   Dim response = uds.ReadDataByIdentifier(&H7E0, &HF190)

Namespace Services

    ''' <summary>
    ''' UDS yanıt durumu
    ''' </summary>
    Public Enum UdsResponseStatus
        Success = 0
        Pending = 1
        NegativeResponse = 2
        Timeout = 3
        InvalidFormat = 4
        SecurityDenied = 5
        ServiceNotSupported = 6
        SubFunctionNotSupported = 7
        IncorrectMessageLength = 8
        BusyRepeatRequest = 9
        ConditionsNotCorrect = 10
        RequestSequenceError = 11
        Unknown = 99
    End Enum

    ''' <summary>
    ''' UDS oturum tipleri
    ''' </summary>
    Public Enum UdsSessionType As Byte
        DefaultSession = &H1
        ProgrammingSession = &H2
        ExtendedDiagnosticSession = &H3
        SafetySystemSession = &H4
    End Enum

    ''' <summary>
    ''' ECU Reset tipleri
    ''' </summary>
    Public Enum EcuResetType As Byte
        HardReset = &H1
        KeyOffOnReset = &H2
        SoftReset = &H3
        EnableRapidPowerShutDown = &H4
        DisableRapidPowerShutDown = &H5
    End Enum

    ''' <summary>
    ''' Routine Control tipleri
    ''' </summary>
    Public Enum RoutineControlType As Byte
        StartRoutine = &H1
        StopRoutine = &H2
        RequestRoutineResults = &H3
    End Enum

    ''' <summary>
    ''' IO Control tipleri
    ''' </summary>
    Public Enum IoControlType As Byte
        ReturnControlToEcu = &H0
        ResetToDefault = &H1
        FreezeCurrentState = &H2
        ShortTermAdjustment = &H3
    End Enum

    ''' <summary>
    ''' UDS yanıt yapısı
    ''' </summary>
    Public Class UdsResponse
        Public Property Status As UdsResponseStatus = UdsResponseStatus.Unknown
        Public Property ServiceId As Byte = 0
        Public Property Data As Byte() = New Byte() {}
        Public Property RawFrames As New List(Of Byte())()
        Public Property NegativeResponseCode As Byte = 0
        Public Property ErrorMessage As String = ""
        Public Property ResponseTime As TimeSpan = TimeSpan.Zero

        ''' <summary>
        ''' Yanıt başarılı mı
        ''' </summary>
        Public ReadOnly Property IsSuccess As Boolean
            Get
                Return Status = UdsResponseStatus.Success
            End Get
        End Property

        ''' <summary>
        ''' NRC (Negative Response Code) açıklaması
        ''' </summary>
        Public Function GetNrcDescription() As String
            Select Case NegativeResponseCode
                Case &H10 : Return "General Reject"
                Case &H11 : Return "Service Not Supported"
                Case &H12 : Return "Sub-Function Not Supported"
                Case &H13 : Return "Incorrect Message Length Or Invalid Format"
                Case &H14 : Return "Response Too Long"
                Case &H21 : Return "Busy - Repeat Request"
                Case &H22 : Return "Conditions Not Correct"
                Case &H24 : Return "Request Sequence Error"
                Case &H25 : Return "No Response From Subnet Component"
                Case &H26 : Return "Failure Prevents Execution"
                Case &H31 : Return "Request Out Of Range"
                Case &H33 : Return "Security Access Denied"
                Case &H35 : Return "Invalid Key"
                Case &H36 : Return "Exceeded Number Of Attempts"
                Case &H37 : Return "Required Time Delay Not Expired"
                Case &H70 : Return "Upload Download Not Accepted"
                Case &H71 : Return "Transfer Data Suspended"
                Case &H72 : Return "General Programming Failure"
                Case &H73 : Return "Wrong Block Sequence Counter"
                Case &H78 : Return "Request Correctly Received - Response Pending"
                Case &H7E : Return "Sub-Function Not Supported In Active Session"
                Case &H7F : Return "Service Not Supported In Active Session"
                Case Else : Return $"Unknown NRC (0x{NegativeResponseCode:X2})"
            End Select
        End Function

        Public Overrides Function ToString() As String
            If IsSuccess Then
                Return $"[OK] Service 0x{ServiceId:X2}, {Data.Length} bytes"
            Else
                Return $"[{Status}] {ErrorMessage} - {GetNrcDescription()}"
            End If
        End Function
    End Class

    ''' <summary>
    ''' ISO-TP frame tipleri
    ''' </summary>
    Public Enum IsoTpFrameType As Byte
        SingleFrame = 0
        FirstFrame = 1
        ConsecutiveFrame = 2
        FlowControl = 3
    End Enum

    ''' <summary>
    ''' Gelişmiş UDS motor sınıfı
    ''' </summary>
    Public Class AdvancedUdsEngine

#Region "Sabitler"

        ' UDS Service ID'leri
        Private Const SID_DIAGNOSTIC_SESSION_CONTROL As Byte = &H10
        Private Const SID_ECU_RESET As Byte = &H11
        Private Const SID_CLEAR_DTC As Byte = &H14
        Private Const SID_READ_DTC As Byte = &H19
        Private Const SID_READ_DATA_BY_ID As Byte = &H22
        Private Const SID_SECURITY_ACCESS As Byte = &H27
        Private Const SID_WRITE_DATA_BY_ID As Byte = &H2E
        Private Const SID_IO_CONTROL As Byte = &H2F
        Private Const SID_ROUTINE_CONTROL As Byte = &H31
        Private Const SID_REQUEST_DOWNLOAD As Byte = &H34
        Private Const SID_REQUEST_UPLOAD As Byte = &H35
        Private Const SID_TRANSFER_DATA As Byte = &H36
        Private Const SID_REQUEST_TRANSFER_EXIT As Byte = &H37
        Private Const SID_TESTER_PRESENT As Byte = &H3E

        ' Negatif yanıt
        Private Const NEGATIVE_RESPONSE As Byte = &H7F

        ' Pozitif yanıt offset
        Private Const POSITIVE_RESPONSE_OFFSET As Byte = &H40

        ' ISO-TP sabitleri
        Private Const ISOTP_SINGLE_FRAME_MAX As Integer = 7
        Private Const ISOTP_FIRST_FRAME_DATA As Integer = 6
        Private Const ISOTP_CONSECUTIVE_FRAME_DATA As Integer = 7

        ' Timeout (ms) - ConfigManager'dan alınır, varsayılan değerler backup olarak
        Private Const DEFAULT_TIMEOUT As Integer = 5000
        Private Const SECURITY_TIMEOUT As Integer = 10000
        
        ''' <summary>
        ''' ConfigManager'dan OBD timeout değerini alır
        ''' </summary>
        Private ReadOnly Property RequestTimeout As Integer
            Get
                Try
                    Return ConfigManager.Instance.OBD.RequestTimeout
                Catch
                    Return DEFAULT_TIMEOUT
                End Try
            End Get
        End Property

#End Region

#Region "Özel Alanlar"

        Private _canSender As CANSender
        Private _isoTpHandler As IsoTpHandler
        Private _responseQueue As New Queue(Of Byte())()
        Private _waitingForResponse As Boolean = False
        Private _expectedResponseId As Integer = 0
        Private _currentSession As UdsSessionType = UdsSessionType.DefaultSession
        Private _securityLevel As Byte = 0

        ' ISO-TP yanıt bekleme
        Private _isoTpResponseReceived As Boolean = False
        Private _isoTpResponseData As Byte() = Nothing
        Private ReadOnly _isoTpResponseLock As New Object()
        Private ReadOnly _isoTpResponseEvent As New Threading.ManualResetEventSlim(False)

#End Region

#Region "Olaylar"

        ''' <summary>
        ''' UDS yanıtı alındığında
        ''' </summary>
        Public Event OnResponse(response As UdsResponse)

        ''' <summary>
        ''' Hata oluştuğunda
        ''' </summary>
        Public Event OnError(message As String)

        ''' <summary>
        ''' Oturum değiştiğinde
        ''' </summary>
        Public Event OnSessionChanged(newSession As UdsSessionType)

#End Region

#Region "Yapılandırma"

        ''' <summary>
        ''' CAN Sender'ı ayarla
        ''' </summary>
        Public Sub SetSender(sender As CANSender)
            _canSender = sender
            InitializeIsoTpHandler()
        End Sub

        ''' <summary>
        ''' IsoTpHandler'ı initialize eder
        ''' </summary>
        Private Sub InitializeIsoTpHandler()
            If _canSender IsNot Nothing AndAlso _isoTpHandler Is Nothing Then
                _isoTpHandler = New IsoTpHandler(_canSender)
                _isoTpHandler.Timeout = RequestTimeout
                AddHandler _isoTpHandler.OnMessageReceived, AddressOf HandleIsoTpMessageReceived
                AddHandler _isoTpHandler.OnError, AddressOf HandleIsoTpError
            End If
        End Sub

        ''' <summary>
        ''' ISO-TP mesaj alındığında çağrılır
        ''' </summary>
        Private Sub HandleIsoTpMessageReceived(sourceId As Integer, data As Byte())
            SyncLock _isoTpResponseLock
                _isoTpResponseData = data
                _isoTpResponseReceived = True
                _isoTpResponseEvent.Set()
            End SyncLock
        End Sub

        ''' <summary>
        ''' ISO-TP hata oluştuğunda çağrılır
        ''' </summary>
        Private Sub HandleIsoTpError(message As String)
            RaiseEvent OnError($"ISO-TP: {message}")
            SyncLock _isoTpResponseLock
                _isoTpResponseReceived = False
                _isoTpResponseData = Nothing
                _isoTpResponseEvent.Set()
            End SyncLock
        End Sub

        ''' <summary>
        ''' Mevcut oturumu döndür
        ''' </summary>
        Public ReadOnly Property CurrentSession As UdsSessionType
            Get
                Return _currentSession
            End Get
        End Property

        ''' <summary>
        ''' Güvenlik seviyesini döndür
        ''' </summary>
        Public ReadOnly Property SecurityLevel As Byte
            Get
                Return _securityLevel
            End Get
        End Property

#End Region

#Region "ISO-TP Frame Oluşturma"

        ''' <summary>
        ''' UDS mesajını ISO-TP frame'lerine böler
        ''' </summary>
        Public Function BuildIsoTpFrames(data As Byte()) As List(Of Byte())
            Dim frames As New List(Of Byte())()

            Try
                If data Is Nothing OrElse data.Length = 0 Then
                    Return frames
                End If

                ' Single Frame (7 byte veya daha az)
                If data.Length <= ISOTP_SINGLE_FRAME_MAX Then
                    Dim sf(7) As Byte
                    sf(0) = CByte(data.Length) ' PCI: 0x0N (N = data length)
                    Array.Copy(data, 0, sf, 1, data.Length)
                    frames.Add(sf)
                    Return frames
                End If

                ' Multi Frame
                Dim totalLength As Integer = data.Length
                Dim offset As Integer = 0

                ' First Frame
                Dim ff(7) As Byte
                ff(0) = CByte(&H10 Or ((totalLength >> 8) And &HF)) ' PCI high nibble + length high
                ff(1) = CByte(totalLength And &HFF) ' Length low byte
                Dim ffDataLen As Integer = Math.Min(ISOTP_FIRST_FRAME_DATA, totalLength)
                Array.Copy(data, 0, ff, 2, ffDataLen)
                frames.Add(ff)
                offset = ffDataLen

                ' Consecutive Frames
                Dim seqNum As Byte = 1
                While offset < totalLength
                    Dim cf(7) As Byte
                    cf(0) = CByte(&H20 Or (seqNum And &HF)) ' PCI: 0x2N
                    Dim cfDataLen As Integer = Math.Min(ISOTP_CONSECUTIVE_FRAME_DATA, totalLength - offset)
                    Array.Copy(data, offset, cf, 1, cfDataLen)
                    frames.Add(cf)
                    offset += cfDataLen
                    seqNum = CByte((seqNum + 1) And &HF)
                End While

            Catch ex As Exception
                RaiseEvent OnError("ISO-TP frame oluşturma hatası: " & ex.Message)
            End Try

            Return frames
        End Function

        ''' <summary>
        ''' ISO-TP frame'lerini tek mesaja birleştirir
        ''' </summary>
        Public Function ParseIsoTpFrames(frames As List(Of Byte())) As Byte()
            Try
                If frames Is Nothing OrElse frames.Count = 0 Then
                    Return New Byte() {}
                End If

                Dim firstFrame = frames(0)
                Dim frameType = CType((firstFrame(0) >> 4) And &HF, IsoTpFrameType)

                ' Single Frame
                If frameType = IsoTpFrameType.SingleFrame Then
                    Dim length As Integer = firstFrame(0) And &HF
                    Dim data(length - 1) As Byte
                    Array.Copy(firstFrame, 1, data, 0, length)
                    Return data
                End If

                ' First Frame + Consecutive Frames
                If frameType = IsoTpFrameType.FirstFrame Then
                    Dim totalLength As Integer = ((firstFrame(0) And &HF) << 8) Or firstFrame(1)
                    Dim result(totalLength - 1) As Byte
                    Dim offset As Integer = 0

                    ' First frame data
                    Dim ffLen As Integer = Math.Min(ISOTP_FIRST_FRAME_DATA, totalLength)
                    Array.Copy(firstFrame, 2, result, 0, ffLen)
                    offset = ffLen

                    ' Consecutive frames
                    For i As Integer = 1 To frames.Count - 1
                        Dim cf = frames(i)
                        Dim cfType = CType((cf(0) >> 4) And &HF, IsoTpFrameType)
                        If cfType = IsoTpFrameType.ConsecutiveFrame Then
                            Dim cfLen As Integer = Math.Min(ISOTP_CONSECUTIVE_FRAME_DATA, totalLength - offset)
                            Array.Copy(cf, 1, result, offset, cfLen)
                            offset += cfLen
                        End If
                    Next

                    Return result
                End If

            Catch ex As Exception
                RaiseEvent OnError("ISO-TP parse hatası: " & ex.Message)
            End Try

            Return New Byte() {}
        End Function

        ''' <summary>
        ''' Flow Control frame oluştur
        ''' </summary>
        Public Function BuildFlowControlFrame(Optional blockSize As Byte = 0, Optional stMin As Byte = 10) As Byte()
            Dim fc(7) As Byte
            fc(0) = &H30 ' Flow Control, ContinueToSend
            fc(1) = blockSize ' Block Size (0 = no limit)
            fc(2) = stMin ' Separation Time minimum (ms)
            Return fc
        End Function

#End Region

#Region "Temel UDS İşlemleri"

        ''' <summary>
        ''' UDS isteği gönder ve yanıt bekle (ISO-TP kullanarak)
        ''' </summary>
        Private Function SendRequest(ecuId As Integer, requestData As Byte()) As UdsResponse
            Dim response As New UdsResponse()
            response.ServiceId = If(requestData.Length > 0, requestData(0), CByte(0))
            Dim startTime As DateTime = DateTime.Now

            Try
                If _canSender Is Nothing Then
                    response.Status = UdsResponseStatus.Unknown
                    response.ErrorMessage = "CAN Sender ayarlanmamış"
                    Return response
                End If

                InitializeIsoTpHandler()
                If _isoTpHandler Is Nothing Then
                    response.Status = UdsResponseStatus.Unknown
                    response.ErrorMessage = "ISO-TP Handler initialize edilemedi"
                    Return response
                End If

                ' ISO-TP ile gönder (retry mekanizması ile)
                Dim retryCount As Integer = 0
                Dim maxRetries As Integer = 3
                Dim success As Boolean = False

                While retryCount < maxRetries AndAlso Not success
                    SyncLock _isoTpResponseLock
                        _isoTpResponseReceived = False
                        _isoTpResponseData = Nothing
                        _isoTpResponseEvent.Reset()
                    End SyncLock

                    ' ISO-TP ile gönder
                    Dim sendTask = _isoTpHandler.SendMessage(ecuId, requestData)
                    success = sendTask.Result

                    If success Then
                        ' Yanıt bekle (timeout: RequestTimeout)
                        Dim received As Boolean = _isoTpResponseEvent.Wait(RequestTimeout)

                        If received AndAlso _isoTpResponseReceived AndAlso _isoTpResponseData IsNot Nothing Then
                            ' UDS yanıtını parse et
                            response = ParseUdsResponse(_isoTpResponseData)
                            response.ResponseTime = DateTime.Now - startTime
                            Return response
                        ElseIf Not received Then
                            ' Timeout
                            response.Status = UdsResponseStatus.Timeout
                            response.ErrorMessage = $"Yanıt timeout ({RequestTimeout}ms)"
                        End If
                    End If

                    retryCount += 1
                    If retryCount < maxRetries Then
                        Threading.Thread.Sleep(100)
                    End If
                End While

                If Not success Then
                    response.Status = UdsResponseStatus.Timeout
                    response.ErrorMessage = $"İstek başarısız ({maxRetries} deneme)"
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "AdvancedUdsEngine.SendRequest")
                response.Status = UdsResponseStatus.Unknown
                response.ErrorMessage = ex.Message
            End Try

            response.ResponseTime = DateTime.Now - startTime
            Return response
        End Function

        ''' <summary>
        ''' UDS yanıtını parse eder
        ''' </summary>
        Private Function ParseUdsResponse(data As Byte()) As UdsResponse
            Dim response As New UdsResponse()

            Try
                If data Is Nothing OrElse data.Length = 0 Then
                    response.Status = UdsResponseStatus.InvalidFormat
                    response.ErrorMessage = "Yanıt boş"
                    Return response
                End If

                Dim serviceId As Byte = data(0)

                ' Negatif yanıt kontrolü (0x7F)
                If serviceId = NEGATIVE_RESPONSE Then
                    response.Status = UdsResponseStatus.NegativeResponse
                    response.ServiceId = If(data.Length > 1, data(1), CByte(0))
                    response.NegativeResponseCode = If(data.Length > 2, data(2), CByte(0))
                    response.ErrorMessage = $"Negatif yanıt: NRC 0x{response.NegativeResponseCode:X2}"
                    Return response
                End If

                ' Pozitif yanıt (Service ID + 0x40)
                If (serviceId And POSITIVE_RESPONSE_OFFSET) = POSITIVE_RESPONSE_OFFSET Then
                    response.Status = UdsResponseStatus.Success
                    response.ServiceId = CByte(serviceId - POSITIVE_RESPONSE_OFFSET)

                    ' Data'yı çıkar (ilk byte hariç)
                    If data.Length > 1 Then
                        Dim responseData(data.Length - 2) As Byte
                        Array.Copy(data, 1, responseData, 0, data.Length - 1)
                        response.Data = responseData
                    Else
                        response.Data = New Byte() {}
                    End If
                Else
                    response.Status = UdsResponseStatus.InvalidFormat
                    response.ErrorMessage = $"Geçersiz yanıt formatı: 0x{serviceId:X2}"
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "AdvancedUdsEngine.ParseUdsResponse")
                response.Status = UdsResponseStatus.InvalidFormat
                response.ErrorMessage = $"Parse hatası: {ex.Message}"
            End Try

            Return response
        End Function

#End Region

#Region "Diagnostic Session Control (0x10)"

        ''' <summary>
        ''' Diagnostik oturum başlat
        ''' </summary>
        Public Function StartDiagnosticSession(ecuId As Integer, sessionType As UdsSessionType) As UdsResponse
            Dim request As Byte() = {SID_DIAGNOSTIC_SESSION_CONTROL, CByte(sessionType)}
            Dim response = SendRequest(ecuId, request)

            If response.IsSuccess Then
                _currentSession = sessionType
                RaiseEvent OnSessionChanged(sessionType)
            End If

            Return response
        End Function

        ''' <summary>
        ''' Varsayılan oturuma dön
        ''' </summary>
        Public Function ReturnToDefaultSession(ecuId As Integer) As UdsResponse
            Return StartDiagnosticSession(ecuId, UdsSessionType.DefaultSession)
        End Function

        ''' <summary>
        ''' Genişletilmiş oturum başlat
        ''' </summary>
        Public Function StartExtendedSession(ecuId As Integer) As UdsResponse
            Return StartDiagnosticSession(ecuId, UdsSessionType.ExtendedDiagnosticSession)
        End Function

        ''' <summary>
        ''' Programlama oturumu başlat
        ''' </summary>
        Public Function StartProgrammingSession(ecuId As Integer) As UdsResponse
            Return StartDiagnosticSession(ecuId, UdsSessionType.ProgrammingSession)
        End Function

#End Region

#Region "ECU Reset (0x11)"

        ''' <summary>
        ''' ECU'yu resetle
        ''' </summary>
        Public Function ResetEcu(ecuId As Integer, resetType As EcuResetType) As UdsResponse
            Dim request As Byte() = {SID_ECU_RESET, CByte(resetType)}
            Return SendRequest(ecuId, request)
        End Function

        ''' <summary>
        ''' Hard reset
        ''' </summary>
        Public Function HardResetEcu(ecuId As Integer) As UdsResponse
            Return ResetEcu(ecuId, EcuResetType.HardReset)
        End Function

        ''' <summary>
        ''' Soft reset
        ''' </summary>
        Public Function SoftResetEcu(ecuId As Integer) As UdsResponse
            Return ResetEcu(ecuId, EcuResetType.SoftReset)
        End Function

#End Region

#Region "Read Data By Identifier (0x22)"

        ''' <summary>
        ''' DID oku
        ''' </summary>
        Public Function ReadDataByIdentifier(ecuId As Integer, did As UShort) As UdsResponse
            Dim request As Byte() = {
                SID_READ_DATA_BY_ID,
                CByte((did >> 8) And &HFF),
                CByte(did And &HFF)
            }
            Return SendRequest(ecuId, request)
        End Function

        ''' <summary>
        ''' Birden fazla DID oku
        ''' </summary>
        Public Function ReadMultipleDataByIdentifier(ecuId As Integer, dids As UShort()) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_READ_DATA_BY_ID)

            For Each did In dids
                request.Add(CByte((did >> 8) And &HFF))
                request.Add(CByte(did And &HFF))
            Next

            Return SendRequest(ecuId, request.ToArray())
        End Function

        ''' <summary>
        ''' VIN oku (DID F190)
        ''' </summary>
        Public Function ReadVIN(ecuId As Integer) As UdsResponse
            Return ReadDataByIdentifier(ecuId, &HF190)
        End Function

        ''' <summary>
        ''' ECU seri numarası oku (DID F18C)
        ''' </summary>
        Public Function ReadEcuSerialNumber(ecuId As Integer) As UdsResponse
            Return ReadDataByIdentifier(ecuId, &HF18C)
        End Function

        ''' <summary>
        ''' Yazılım versiyonu oku (DID F195)
        ''' </summary>
        Public Function ReadSoftwareVersion(ecuId As Integer) As UdsResponse
            Return ReadDataByIdentifier(ecuId, &HF195)
        End Function

#End Region

#Region "Write Data By Identifier (0x2E)"

        ''' <summary>
        ''' DID'e veri yaz
        ''' </summary>
        Public Function WriteDataByIdentifier(ecuId As Integer, did As UShort, data As Byte()) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_WRITE_DATA_BY_ID)
            request.Add(CByte((did >> 8) And &HFF))
            request.Add(CByte(did And &HFF))
            request.AddRange(data)

            Return SendRequest(ecuId, request.ToArray())
        End Function

#End Region

#Region "Security Access (0x27)"

        ''' <summary>
        ''' Güvenlik erişimi - Seed iste
        ''' </summary>
        Public Function RequestSecuritySeed(ecuId As Integer, securityLevel As Byte) As UdsResponse
            ' Security level tek sayı olmalı (request seed)
            Dim level As Byte = If(securityLevel Mod 2 = 0, CByte(securityLevel - 1), securityLevel)
            Dim request As Byte() = {SID_SECURITY_ACCESS, level}
            Return SendRequest(ecuId, request)
        End Function

        ''' <summary>
        ''' Güvenlik erişimi - Key gönder
        ''' </summary>
        Public Function SendSecurityKey(ecuId As Integer, securityLevel As Byte, key As Byte()) As UdsResponse
            ' Security level çift sayı olmalı (send key)
            Dim level As Byte = If(securityLevel Mod 2 = 0, securityLevel, CByte(securityLevel + 1))

            Dim request As New List(Of Byte)()
            request.Add(SID_SECURITY_ACCESS)
            request.Add(level)
            request.AddRange(key)

            Dim response = SendRequest(ecuId, request.ToArray())

            If response.IsSuccess Then
                _securityLevel = securityLevel
            End If

            Return response
        End Function

        ''' <summary>
        ''' Varsayılan key algoritması (placeholder)
        ''' Gerçek implementasyonda araç/ECU'ya özgü algoritma kullanılmalı
        ''' </summary>
        Public Function CalculateSecurityKey(seed As Byte(), Optional algorithm As Integer = 0) As Byte()
            ' Bu basit bir XOR algoritması - gerçek implementasyonda değiştirilmeli
            Dim key(seed.Length - 1) As Byte

            Select Case algorithm
                Case 0 ' Basit XOR
                    For i As Integer = 0 To seed.Length - 1
                        key(i) = seed(i) Xor &HAA
                    Next

                Case 1 ' Bit rotation
                    For i As Integer = 0 To seed.Length - 1
                        key(i) = CByte(((seed(i) << 1) Or (seed(i) >> 7)) And &HFF)
                    Next

                Case Else
                    ' Bilinmeyen algoritma, seed'i döndür
                    Array.Copy(seed, key, seed.Length)
            End Select

            Return key
        End Function

#End Region

#Region "Routine Control (0x31)"

        ''' <summary>
        ''' Routine başlat
        ''' </summary>
        Public Function StartRoutine(ecuId As Integer, routineId As UShort, Optional options As Byte() = Nothing) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_ROUTINE_CONTROL)
            request.Add(CByte(RoutineControlType.StartRoutine))
            request.Add(CByte((routineId >> 8) And &HFF))
            request.Add(CByte(routineId And &HFF))

            If options IsNot Nothing Then
                request.AddRange(options)
            End If

            Return SendRequest(ecuId, request.ToArray())
        End Function

        ''' <summary>
        ''' Routine durdur
        ''' </summary>
        Public Function StopRoutine(ecuId As Integer, routineId As UShort) As UdsResponse
            Dim request As Byte() = {
                SID_ROUTINE_CONTROL,
                CByte(RoutineControlType.StopRoutine),
                CByte((routineId >> 8) And &HFF),
                CByte(routineId And &HFF)
            }
            Return SendRequest(ecuId, request)
        End Function

        ''' <summary>
        ''' Routine sonuçlarını iste
        ''' </summary>
        Public Function RequestRoutineResults(ecuId As Integer, routineId As UShort) As UdsResponse
            Dim request As Byte() = {
                SID_ROUTINE_CONTROL,
                CByte(RoutineControlType.RequestRoutineResults),
                CByte((routineId >> 8) And &HFF),
                CByte(routineId And &HFF)
            }
            Return SendRequest(ecuId, request)
        End Function

#End Region

#Region "IO Control By Identifier (0x2F)"

        ''' <summary>
        ''' IO kontrol
        ''' </summary>
        Public Function IoControl(ecuId As Integer, ioId As UShort, controlType As IoControlType, Optional controlState As Byte() = Nothing) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_IO_CONTROL)
            request.Add(CByte((ioId >> 8) And &HFF))
            request.Add(CByte(ioId And &HFF))
            request.Add(CByte(controlType))

            If controlState IsNot Nothing Then
                request.AddRange(controlState)
            End If

            Return SendRequest(ecuId, request.ToArray())
        End Function

        ''' <summary>
        ''' IO kontrolünü ECU'ya bırak
        ''' </summary>
        Public Function ReturnIoControlToEcu(ecuId As Integer, ioId As UShort) As UdsResponse
            Return IoControl(ecuId, ioId, IoControlType.ReturnControlToEcu)
        End Function

        ''' <summary>
        ''' Kısa süreli ayar
        ''' </summary>
        Public Function ShortTermIoAdjustment(ecuId As Integer, ioId As UShort, value As Byte()) As UdsResponse
            Return IoControl(ecuId, ioId, IoControlType.ShortTermAdjustment, value)
        End Function

#End Region

#Region "Request Download/Upload (0x34, 0x35, 0x36, 0x37)"

        ''' <summary>
        ''' Download isteği (ECU'dan veri indir)
        ''' </summary>
        Public Function RequestDownload(ecuId As Integer, memoryAddress As UInteger, memorySize As UInteger,
                                         Optional dataFormat As Byte = &H0) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_REQUEST_DOWNLOAD)
            request.Add(dataFormat) ' dataFormatIdentifier
            request.Add(&H44) ' addressAndLengthFormatIdentifier (4 bytes each)

            ' Memory address (4 bytes, big endian)
            request.Add(CByte((memoryAddress >> 24) And &HFF))
            request.Add(CByte((memoryAddress >> 16) And &HFF))
            request.Add(CByte((memoryAddress >> 8) And &HFF))
            request.Add(CByte(memoryAddress And &HFF))

            ' Memory size (4 bytes, big endian)
            request.Add(CByte((memorySize >> 24) And &HFF))
            request.Add(CByte((memorySize >> 16) And &HFF))
            request.Add(CByte((memorySize >> 8) And &HFF))
            request.Add(CByte(memorySize And &HFF))

            Return SendRequest(ecuId, request.ToArray())
        End Function

        ''' <summary>
        ''' Upload isteği (ECU'ya veri yükle)
        ''' </summary>
        Public Function RequestUpload(ecuId As Integer, memoryAddress As UInteger, memorySize As UInteger,
                                       Optional dataFormat As Byte = &H0) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_REQUEST_UPLOAD)
            request.Add(dataFormat)
            request.Add(&H44)

            request.Add(CByte((memoryAddress >> 24) And &HFF))
            request.Add(CByte((memoryAddress >> 16) And &HFF))
            request.Add(CByte((memoryAddress >> 8) And &HFF))
            request.Add(CByte(memoryAddress And &HFF))

            request.Add(CByte((memorySize >> 24) And &HFF))
            request.Add(CByte((memorySize >> 16) And &HFF))
            request.Add(CByte((memorySize >> 8) And &HFF))
            request.Add(CByte(memorySize And &HFF))

            Return SendRequest(ecuId, request.ToArray())
        End Function

        ''' <summary>
        ''' Veri transferi
        ''' </summary>
        Public Function TransferData(ecuId As Integer, blockSequenceCounter As Byte, data As Byte()) As UdsResponse
            Dim request As New List(Of Byte)()
            request.Add(SID_TRANSFER_DATA)
            request.Add(blockSequenceCounter)
            request.AddRange(data)

            Return SendRequest(ecuId, request.ToArray())
        End Function

        ''' <summary>
        ''' Transfer çıkışı
        ''' </summary>
        Public Function RequestTransferExit(ecuId As Integer) As UdsResponse
            Dim request As Byte() = {SID_REQUEST_TRANSFER_EXIT}
            Return SendRequest(ecuId, request)
        End Function

#End Region

#Region "DTC İşlemleri (0x14, 0x19)"

        ''' <summary>
        ''' DTC temizle
        ''' </summary>
        Public Function ClearDtc(ecuId As Integer, Optional groupOfDtc As UInteger = &HFFFFFF) As UdsResponse
            Dim request As Byte() = {
                SID_CLEAR_DTC,
                CByte((groupOfDtc >> 16) And &HFF),
                CByte((groupOfDtc >> 8) And &HFF),
                CByte(groupOfDtc And &HFF)
            }
            Return SendRequest(ecuId, request)
        End Function

        ''' <summary>
        ''' DTC'leri oku
        ''' </summary>
        Public Function ReadDtc(ecuId As Integer, Optional subFunction As Byte = &H1) As UdsResponse
            Dim request As Byte() = {SID_READ_DTC, subFunction}
            Return SendRequest(ecuId, request)
        End Function

        ''' <summary>
        ''' Tüm DTC'leri oku
        ''' </summary>
        Public Function ReadAllDtcs(ecuId As Integer) As UdsResponse
            Return ReadDtc(ecuId, &H2) ' reportDTCByStatusMask
        End Function

#End Region

#Region "Tester Present (0x3E)"

        ''' <summary>
        ''' Tester Present gönder (oturumu canlı tut)
        ''' </summary>
        Public Function SendTesterPresent(ecuId As Integer, Optional suppressPositiveResponse As Boolean = True) As UdsResponse
            Dim subFunction As Byte = If(suppressPositiveResponse, &H80, &H0)
            Dim request As Byte() = {SID_TESTER_PRESENT, subFunction}
            Return SendRequest(ecuId, request)
        End Function

#End Region

#Region "Yardımcı Metodlar"

        ''' <summary>
        ''' Yanıtı parse et
        ''' </summary>
        Public Function ParseResponse(responseData As Byte()) As UdsResponse
            Dim response As New UdsResponse()

            Try
                If responseData Is Nothing OrElse responseData.Length = 0 Then
                    response.Status = UdsResponseStatus.InvalidFormat
                    response.ErrorMessage = "Boş yanıt"
                    Return response
                End If

                ' ISO-TP unwrap
                Dim frameType = CType((responseData(0) >> 4) And &HF, IsoTpFrameType)
                Dim udsData As Byte()

                If frameType = IsoTpFrameType.SingleFrame Then
                    Dim length = responseData(0) And &HF
                    udsData = New Byte(length - 1) {}
                    Array.Copy(responseData, 1, udsData, 0, length)
                Else
                    udsData = responseData
                End If

                ' Negatif yanıt kontrolü
                If udsData(0) = NEGATIVE_RESPONSE Then
                    response.Status = UdsResponseStatus.NegativeResponse
                    response.ServiceId = If(udsData.Length > 1, udsData(1), CByte(0))
                    response.NegativeResponseCode = If(udsData.Length > 2, udsData(2), CByte(0))
                    response.ErrorMessage = response.GetNrcDescription()
                    Return response
                End If

                ' Pozitif yanıt
                response.Status = UdsResponseStatus.Success
                response.ServiceId = CByte(udsData(0) - POSITIVE_RESPONSE_OFFSET)
                response.Data = New Byte(udsData.Length - 2) {}
                Array.Copy(udsData, 1, response.Data, 0, udsData.Length - 1)

            Catch ex As Exception
                response.Status = UdsResponseStatus.InvalidFormat
                response.ErrorMessage = ex.Message
            End Try

            Return response
        End Function

        ''' <summary>
        ''' DID değerini string olarak döndür
        ''' </summary>
        Public Function DidToString(did As UShort) As String
            Select Case did
                Case &HF186 : Return "Active Diagnostic Session"
                Case &HF187 : Return "Vehicle Manufacturer Spare Part Number"
                Case &HF188 : Return "Vehicle Manufacturer ECU Software Number"
                Case &HF189 : Return "Vehicle Manufacturer ECU Software Version"
                Case &HF18A : Return "System Supplier Identifier"
                Case &HF18B : Return "ECU Manufacturing Date"
                Case &HF18C : Return "ECU Serial Number"
                Case &HF190 : Return "VIN"
                Case &HF191 : Return "Vehicle Manufacturer ECU Hardware Number"
                Case &HF192 : Return "System Supplier ECU Hardware Number"
                Case &HF193 : Return "System Supplier ECU Hardware Version"
                Case &HF194 : Return "System Supplier ECU Software Number"
                Case &HF195 : Return "System Supplier ECU Software Version"
                Case Else : Return $"DID 0x{did:X4}"
            End Select
        End Function

#End Region

    End Class

End Namespace

