' OBDService.vb
' OBD-II Mode 01-09 PID İstek/Cevap Servisi
'
' İstek Formatı (ID = 0x7DF):
'   02 01 <PID> 00 00 00 00 00
'
' Cevap Formatı (ID = 0x7E8-0x7EF):
'   03 41 <PID> <A> [<B> <C> <D>]

Imports CarCanReader1._0.Models

Namespace Services

    ''' <summary>
    ''' Mode 06 Test Sonucu modeli
    ''' </summary>
    Public Class Mode06TestResult
        Public Property TestId As Byte
        Public Property ComponentId As Byte
        Public Property TestValue As Double
        Public Property MinLimit As Double
        Public Property MaxLimit As Double
        Public Property Passed As Boolean
        Public Property Description As String
    End Class

    ''' <summary>
    ''' Araç Bilgisi modeli
    ''' </summary>
    Public Class VehicleInfoModel
        Public Property VIN As String = ""
        Public Property CalibrationID As String = ""
        Public Property CVN As String = ""
        Public Property ECUName As String = ""
        Public Property OBDStandard As String = ""
    End Class

    ' DTCInfo sınıfı artık Models/DTCInfo.vb'de tanımlı
    ' Models.DTCInfo kullanılıyor

    Public Class OBDService

#Region "Sabitler"

        Public Const OBD_REQUEST_ID As Integer = &H7DF
        Public Const OBD_RESPONSE_ID_MIN As Integer = &H7E8
        Public Const OBD_RESPONSE_ID_MAX As Integer = &H7EF
        
        ' Mode Response Codes
        Public Const OBD_MODE_01_RESPONSE As Byte = &H41
        Public Const OBD_MODE_02_RESPONSE As Byte = &H42
        Public Const OBD_MODE_03_RESPONSE As Byte = &H43
        Public Const OBD_MODE_04_RESPONSE As Byte = &H44
        Public Const OBD_MODE_05_RESPONSE As Byte = &H45
        Public Const OBD_MODE_06_RESPONSE As Byte = &H46
        Public Const OBD_MODE_07_RESPONSE As Byte = &H47
        Public Const OBD_MODE_08_RESPONSE As Byte = &H48
        Public Const OBD_MODE_09_RESPONSE As Byte = &H49

        ' Mode 09 PIDs
        Public Const MODE09_VIN As Byte = &H2
        Public Const MODE09_CALIBRATION_ID As Byte = &H4
        Public Const MODE09_CVN As Byte = &H6
        Public Const MODE09_ECU_NAME As Byte = &HA

        ' Supported PIDs query
        Public Const PID_SUPPORTED_01_20 As Byte = &H0
        Public Const PID_SUPPORTED_21_40 As Byte = &H20
        Public Const PID_SUPPORTED_41_60 As Byte = &H40
        Public Const PID_SUPPORTED_61_80 As Byte = &H60

        ' PID Sabitleri
        Public Const PID_ENGINE_LOAD As Byte = &H4
        Public Const PID_COOLANT_TEMP As Byte = &H5
        Public Const PID_SHORT_FUEL_TRIM As Byte = &H6
        Public Const PID_LONG_FUEL_TRIM As Byte = &H7
        Public Const PID_MAP As Byte = &HB
        Public Const PID_RPM As Byte = &HC
        Public Const PID_SPEED As Byte = &HD
        Public Const PID_INTAKE_TEMP As Byte = &HF
        Public Const PID_MAF As Byte = &H10
        Public Const PID_THROTTLE As Byte = &H11
        Public Const PID_O2_SENSOR_1 As Byte = &H14
        Public Const PID_O2_SENSOR_2 As Byte = &H15
        Public Const PID_O2_SENSOR_3 As Byte = &H16
        Public Const PID_O2_SENSOR_4 As Byte = &H17
        Public Const PID_O2_SENSOR_5 As Byte = &H18
        Public Const PID_O2_SENSOR_6 As Byte = &H19
        Public Const PID_O2_SENSOR_7 As Byte = &H1A
        Public Const PID_O2_SENSOR_8 As Byte = &H1B
        Public Const PID_FUEL_LEVEL As Byte = &H2F
        Public Const PID_BAROMETRIC As Byte = &H33
        Public Const PID_ECU_VOLTAGE As Byte = &H42
        Public Const PID_AMBIENT_TEMP As Byte = &H46
        Public Const PID_FUEL_RATE As Byte = &H5E

#End Region

#Region "Olaylar"

        ' Mode 01 Events (existing)
        Public Event OnPIDUpdated(pid As Byte, value As Double, unit As String)
        Public Event OnO2SensorUpdated(sensorIndex As Integer, voltage As Double)
        Public Event OnRequestSent(pid As Byte)
        Public Event OnError(message As String)

        ' Mode 02-09 Events (new)
        Public Event OnFreezeFrameReceived(dtc As String, data As Dictionary(Of Byte, Double))
        Public Event OnPendingDTCReceived(dtcs As List(Of DTCInfo))
        Public Event OnStoredDTCReceived(dtcs As List(Of DTCInfo))
        Public Event OnMode06Received(tests As List(Of Mode06TestResult))
        Public Event OnMode08Received(actions As Dictionary(Of Byte, String))
        Public Event OnVehicleInfoReceived(info As VehicleInfoModel)
        Public Event OnAutoScanCompleted(supportedPIDs As Boolean())
        Public Event OnDTCCleared()

#End Region

#Region "Alanlar"

        Private _canSender As CANSender
        Private _isoTpHandler As IsoTpHandler
        Private WithEvents _pollTimer As System.Timers.Timer
        Private _isPolling As Boolean = False
        Private _pollPIDList As New List(Of Byte)
        Private _currentPollIndex As Integer = 0
        Private ReadOnly _lock As New Object()
        Private _pidValues As New Dictionary(Of Byte, Double)
        Private _o2Voltages(7) As Double

        ' Mode 02-09 Alanları
        Private _pendingDTCs As New List(Of DTCInfo)
        Private _storedDTCs As New List(Of DTCInfo)
        Private _freezeFrameData As New Dictionary(Of Byte, Double)
        Private _mode06Tests As New List(Of Mode06TestResult)
        Private _mode08Actions As New Dictionary(Of Byte, String)
        Private _vehicleInfo As New VehicleInfoModel()
        Private _supportedPIDs(255) As Boolean
        Private _isScanning As Boolean = False
        Private _scanQueue As New Queue(Of Byte)
        Private _responseBuffer As New List(Of Byte)

        ' ISO-TP yanıt bekleme
        Private _isoTpResponseReceived As Boolean = False
        Private _isoTpResponseData As Byte() = Nothing
        Private ReadOnly _isoTpResponseLock As New Object()
        Private ReadOnly _isoTpResponseEvent As New Threading.ManualResetEventSlim(False)

#End Region

#Region "Özellikler"

        Public ReadOnly Property IsPolling As Boolean
            Get
                Return _isPolling
            End Get
        End Property

        Public ReadOnly Property O2Voltages As Double()
            Get
                Return _o2Voltages
            End Get
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            ' ConfigManager'dan polling interval al, yoksa varsayılan 250ms
            Dim pollingInterval = 250
            Try
                pollingInterval = ConfigManager.Instance.OBD.PollingInterval
            Catch
                ' ConfigManager henüz yüklenmemişse varsayılanı kullan
            End Try
            
            _pollTimer = New System.Timers.Timer(pollingInterval)
            _pollTimer.AutoReset = True
            SetDefaultPollList()
        End Sub

        Public Sub New(sender As CANSender)
            Me.New()
            _canSender = sender
            InitializeIsoTpHandler()
        End Sub

#End Region

#Region "Konfigürasyon"

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
                _isoTpHandler.SourceId = OBD_RESPONSE_ID_MIN
                _isoTpHandler.TargetId = OBD_REQUEST_ID
                _isoTpHandler.Timeout = ConfigManager.Instance.OBD.RequestTimeout
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
        ''' Polling aralığını değiştirir (milisaniye)
        ''' </summary>
        Public Sub SetPollingInterval(intervalMs As Integer)
            If intervalMs < 50 Then intervalMs = 50
            If intervalMs > 5000 Then intervalMs = 5000
            
            SyncLock _lock
                _pollTimer.Interval = intervalMs
            End SyncLock
        End Sub
        
        ''' <summary>
        ''' Mevcut polling aralığını döndürür
        ''' </summary>
        Public ReadOnly Property PollingInterval As Integer
            Get
                Return CInt(_pollTimer.Interval)
            End Get
        End Property

        Private Sub SetDefaultPollList()
            _pollPIDList.Clear()
            _pollPIDList.Add(PID_RPM)
            _pollPIDList.Add(PID_SPEED)
            _pollPIDList.Add(PID_ENGINE_LOAD)
            _pollPIDList.Add(PID_THROTTLE)
            _pollPIDList.Add(PID_COOLANT_TEMP)
            _pollPIDList.Add(PID_INTAKE_TEMP)
            _pollPIDList.Add(PID_MAF)
            _pollPIDList.Add(PID_MAP)
            _pollPIDList.Add(PID_FUEL_LEVEL)
            _pollPIDList.Add(PID_AMBIENT_TEMP)
            _pollPIDList.Add(PID_BAROMETRIC)
            _pollPIDList.Add(PID_ECU_VOLTAGE)
            _pollPIDList.Add(PID_SHORT_FUEL_TRIM)
            _pollPIDList.Add(PID_LONG_FUEL_TRIM)
            _pollPIDList.Add(PID_FUEL_RATE)
            _pollPIDList.Add(PID_O2_SENSOR_1)
            _pollPIDList.Add(PID_O2_SENSOR_2)
        End Sub

#End Region

#Region "Polling Kontrolü"

        Public Sub StartPolling()
            If _canSender Is Nothing Then
                RaiseEvent OnError("CANSender ayarlanmamış!")
                Return
            End If

            SyncLock _lock
                _isPolling = True
                _currentPollIndex = 0
                _pollTimer.Start()
            End SyncLock
        End Sub

        Public Sub StopPolling()
            SyncLock _lock
                _isPolling = False
                _pollTimer.Stop()
            End SyncLock
        End Sub

        Private Sub PollTimer_Elapsed(sender As Object, e As System.Timers.ElapsedEventArgs) Handles _pollTimer.Elapsed
            If Not _isPolling Then Return
            If _pollPIDList.Count = 0 Then Return

            SyncLock _lock
                Dim pid = _pollPIDList(_currentPollIndex)
                RequestPID(pid)
                _currentPollIndex = (_currentPollIndex + 1) Mod _pollPIDList.Count
            End SyncLock
        End Sub

#End Region

#Region "PID İstek"

        Public Sub RequestPID(pid As Byte)
            If _canSender Is Nothing Then Return

            Try
                ' SLCAN format: t7DF8 02 01 <PID> 00 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0201" & pid.ToString("X2") & "0000000000"
                _canSender.SendRaw(requestFrame)
                RaiseEvent OnRequestSent(pid)
            Catch ex As Exception
                RaiseEvent OnError("PID istek hatası: " & ex.Message)
            End Try
        End Sub

#End Region

#Region "Cevap İşleme"

        Public Function IsOBDResponse(frameId As Integer) As Boolean
            Return frameId >= OBD_RESPONSE_ID_MIN AndAlso frameId <= OBD_RESPONSE_ID_MAX
        End Function

        Public Sub ProcessFrame(frameId As Integer, data As Byte())
            If Not IsOBDResponse(frameId) Then Return
            If data Is Nothing OrElse data.Length < 4 Then Return

            Try
                Dim mode As Byte = data(1)
                If mode <> OBD_MODE_01_RESPONSE Then Return

                Dim pid As Byte = data(2)
                Dim A As Integer = If(data.Length > 3, data(3), 0)
                Dim B As Integer = If(data.Length > 4, data(4), 0)

                ' O2 sensör mü?
                If pid >= PID_O2_SENSOR_1 AndAlso pid <= PID_O2_SENSOR_8 Then
                    Dim sensorIndex = pid - PID_O2_SENSOR_1
                    Dim voltage = A * 0.005
                    _o2Voltages(sensorIndex) = voltage
                    RaiseEvent OnO2SensorUpdated(sensorIndex, voltage)
                Else
                    Dim value = DecodePID(pid, A, B)
                    SyncLock _lock
                        _pidValues(pid) = value
                    End SyncLock
                    RaiseEvent OnPIDUpdated(pid, value, GetPIDUnit(pid))
                End If

            Catch ex As Exception
                RaiseEvent OnError("Frame işleme hatası: " & ex.Message)
            End Try
        End Sub

        Public Function DecodePID(pid As Byte, A As Integer, B As Integer) As Double
            Select Case pid
                Case PID_ENGINE_LOAD
                    Return A * 100.0 / 255.0
                Case PID_COOLANT_TEMP
                    Return A - 40
                Case PID_SHORT_FUEL_TRIM, PID_LONG_FUEL_TRIM
                    Return (A - 128) * 100.0 / 128.0
                Case PID_MAP
                    Return A
                Case PID_RPM
                    Return (256 * A + B) / 4.0
                Case PID_SPEED
                    Return A
                Case PID_INTAKE_TEMP
                    Return A - 40
                Case PID_MAF
                    Return (256 * A + B) / 100.0
                Case PID_THROTTLE
                    Return A * 100.0 / 255.0
                Case PID_FUEL_LEVEL
                    Return A * 100.0 / 255.0
                Case PID_BAROMETRIC
                    Return A
                Case PID_ECU_VOLTAGE
                    Return (256 * A + B) / 1000.0
                Case PID_AMBIENT_TEMP
                    Return A - 40
                Case PID_FUEL_RATE
                    Return (256 * A + B) / 20.0
                Case Else
                    Return A
            End Select
        End Function

        Public Function GetPIDUnit(pid As Byte) As String
            Select Case pid
                Case PID_ENGINE_LOAD, PID_THROTTLE, PID_FUEL_LEVEL
                    Return "%"
                Case PID_COOLANT_TEMP, PID_INTAKE_TEMP, PID_AMBIENT_TEMP
                    Return "°C"
                Case PID_MAP, PID_BAROMETRIC
                    Return "kPa"
                Case PID_RPM
                    Return "RPM"
                Case PID_SPEED
                    Return "km/h"
                Case PID_MAF
                    Return "g/s"
                Case PID_ECU_VOLTAGE
                    Return "V"
                Case PID_FUEL_RATE
                    Return "L/h"
                Case PID_SHORT_FUEL_TRIM, PID_LONG_FUEL_TRIM
                    Return "%"
                Case Else
                    Return ""
            End Select
        End Function

        Public Function GetPIDName(pid As Byte) As String
            Select Case pid
                Case PID_ENGINE_LOAD : Return "Motor Yükü"
                Case PID_COOLANT_TEMP : Return "Soğutma Suyu"
                Case PID_SHORT_FUEL_TRIM : Return "Kısa Yakıt Trim"
                Case PID_LONG_FUEL_TRIM : Return "Uzun Yakıt Trim"
                Case PID_MAP : Return "MAP"
                Case PID_RPM : Return "RPM"
                Case PID_SPEED : Return "Hız"
                Case PID_INTAKE_TEMP : Return "Emme Havası"
                Case PID_MAF : Return "MAF"
                Case PID_THROTTLE : Return "Gaz Kelebeği"
                Case PID_FUEL_LEVEL : Return "Yakıt Seviyesi"
                Case PID_BAROMETRIC : Return "Atmosfer Basıncı"
                Case PID_ECU_VOLTAGE : Return "ECU Voltaj"
                Case PID_AMBIENT_TEMP : Return "Ortam Sıcaklığı"
                Case PID_FUEL_RATE : Return "Yakıt Tüketimi"
                Case Else : Return "PID 0x" & pid.ToString("X2")
            End Select
        End Function

        Public Function GetCachedValue(pid As Byte) As Double
            SyncLock _lock
                If _pidValues.ContainsKey(pid) Then
                    Return _pidValues(pid)
                End If
            End SyncLock
            Return 0
        End Function

#End Region

#Region "Mode 02 - Freeze Frame"

        ''' <summary>
        ''' Freeze Frame verisini iste (Mode 02)
        ''' </summary>
        Public Sub RequestFreezeFrame(Optional frameNumber As Byte = 0)
            If _canSender Is Nothing Then Return

            Try
                ' Format: 02 02 02 <frame#> 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0202" & frameNumber.ToString("X2") & "00000000"
                _canSender.SendRaw(requestFrame)
            Catch ex As Exception
                RaiseEvent OnError("Freeze Frame istek hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Freeze Frame verisini decode et
        ''' </summary>
        Private Sub DecodeFreezeFrame(data As Byte())
            If data Is Nothing OrElse data.Length < 5 Then Return

            Try
                Dim pid As Byte = data(2)
                Dim frameNum As Byte = data(3)
                Dim A As Integer = If(data.Length > 4, data(4), 0)
                Dim B As Integer = If(data.Length > 5, data(5), 0)

                Dim value = DecodePID(pid, A, B)
                
                SyncLock _lock
                    _freezeFrameData(pid) = value
                End SyncLock

                ' Freeze frame sonucu DTC ile birlikte gönder
                RaiseEvent OnFreezeFrameReceived("FF" & frameNum.ToString("X2"), _freezeFrameData)

            Catch ex As Exception
                RaiseEvent OnError("Freeze Frame decode hatası: " & ex.Message)
            End Try
        End Sub

#End Region

#Region "Mode 07 - Pending DTCs"

        ''' <summary>
        ''' Bekleyen DTC'leri iste (Mode 07)
        ''' </summary>
        Public Sub RequestPendingDTC()
            If _canSender Is Nothing Then Return

            Try
                ' Format: 01 07 00 00 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0107000000000000"
                _canSender.SendRaw(requestFrame)
            Catch ex As Exception
                RaiseEvent OnError("Pending DTC istek hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Pending DTC'leri decode et
        ''' </summary>
        Private Sub DecodePendingDTC(data As Byte())
            If data Is Nothing OrElse data.Length < 3 Then Return

            Try
                Dim dtcCount As Integer = data(2)
                Dim dtcs As New List(Of DTCInfo)

                ' Her DTC 2 byte
                Dim offset As Integer = 3
                For i As Integer = 0 To dtcCount - 1
                    If offset + 1 >= data.Length Then Exit For

                    Dim dtcCode = DecodeDTCBytes(data(offset), data(offset + 1))
                    If Not String.IsNullOrEmpty(dtcCode) Then
                        ' DTCDatabase'den detaylı bilgi al
                        Dim dtcInfo = GetEnhancedDTCInfo(dtcCode)
                        dtcInfo.IsPending = True
                        dtcInfo.LastDetected = DateTime.Now
                        dtcInfo.DetectionCount += 1
                        dtcs.Add(dtcInfo)
                    End If

                    offset += 2
                Next

                SyncLock _lock
                    _pendingDTCs = dtcs
                End SyncLock

                RaiseEvent OnPendingDTCReceived(dtcs)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.DecodePendingDTC")
            End Try
        End Sub

        ''' <summary>
        ''' DTC byte'larını kod string'e çevirir
        ''' </summary>
        Private Function DecodeDTCBytes(byte1 As Byte, byte2 As Byte) As String
            If byte1 = 0 AndAlso byte2 = 0 Then Return ""

            ' İlk 2 bit = DTC tipi (P, C, B, U)
            Dim typeCode = (byte1 >> 6) And &H3
            Dim prefix As Char
            Select Case typeCode
                Case 0 : prefix = "P"c
                Case 1 : prefix = "C"c
                Case 2 : prefix = "B"c
                Case 3 : prefix = "U"c
                Case Else : prefix = "P"c
            End Select

            ' İkinci hane
            Dim digit1 = (byte1 >> 4) And &H3
            ' Üçüncü hane
            Dim digit2 = byte1 And &HF
            ' Son 2 hane
            Dim digit34 = byte2

            Return prefix & digit1.ToString() & digit2.ToString("X") & digit34.ToString("X2")
        End Function

        ''' <summary>
        ''' DTC açıklamasını döndürür (DTCDatabase'den)
        ''' </summary>
        Private Function GetDTCDescription(code As String) As String
            Try
                Dim dtcInfo = DTCDatabase.Instance.GetDTC(code)
                If dtcInfo IsNot Nothing AndAlso Not String.IsNullOrEmpty(dtcInfo.DescriptionTR) Then
                    Return dtcInfo.DescriptionTR
                End If
            Catch
            End Try

            ' Fallback: Temel DTC açıklamaları
            If code.StartsWith("P0") Then Return "Powertrain - Genel Arıza"
            If code.StartsWith("P1") Then Return "Powertrain - Üretici Özel"
            If code.StartsWith("P2") Then Return "Powertrain - Genel Arıza"
            If code.StartsWith("P3") Then Return "Powertrain - Genel/Üretici"
            If code.StartsWith("C") Then Return "Şasi Sistemi"
            If code.StartsWith("B") Then Return "Gövde Sistemi"
            If code.StartsWith("U") Then Return "Ağ/İletişim"
            Return "Bilinmeyen DTC"
        End Function
        
        ''' <summary>
        ''' DTC koduna göre zenginleştirilmiş DTCInfo döndürür
        ''' </summary>
        Private Function GetEnhancedDTCInfo(code As String) As DTCInfo
            Try
                ' Önce veritabanından dene
                If DTCDatabase.Instance.HasDTC(code) Then
                    Return DTCDatabase.Instance.GetDTC(code)
                End If
            Catch
            End Try

            ' Veritabanında yoksa temel bilgilerle oluştur
            Return DTCInfo.FromCode(code)
        End Function

#End Region

#Region "Mode 03 - Stored DTCs"

        ''' <summary>
        ''' Kayıtlı DTC'leri iste (Mode 03)
        ''' </summary>
        Public Sub RequestStoredDTC()
            If _canSender Is Nothing Then Return

            Try
                ' Format: 01 03 00 00 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0103000000000000"
                _canSender.SendRaw(requestFrame)
            Catch ex As Exception
                RaiseEvent OnError("Stored DTC istek hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Stored DTC'leri decode et
        ''' </summary>
        Private Sub DecodeStoredDTC(data As Byte())
            If data Is Nothing OrElse data.Length < 3 Then Return

            Try
                Dim dtcCount As Integer = data(2)
                Dim dtcs As New List(Of DTCInfo)

                ' Her DTC 2 byte
                Dim offset As Integer = 3
                For i As Integer = 0 To dtcCount - 1
                    If offset + 1 >= data.Length Then Exit For

                    Dim dtcCode = DecodeDTCBytes(data(offset), data(offset + 1))
                    If Not String.IsNullOrEmpty(dtcCode) Then
                        ' DTCDatabase'den detaylı bilgi al
                        Dim dtcInfo = GetEnhancedDTCInfo(dtcCode)
                        dtcInfo.IsStored = True
                        dtcInfo.IsPending = False
                        dtcInfo.LastDetected = DateTime.Now
                        dtcInfo.DetectionCount += 1
                        dtcs.Add(dtcInfo)
                    End If

                    offset += 2
                Next

                SyncLock _lock
                    _storedDTCs = dtcs
                End SyncLock

                RaiseEvent OnStoredDTCReceived(dtcs)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.DecodeStoredDTC")
            End Try
        End Sub

#End Region

#Region "Mode 06 - On-Board Monitoring"

        ''' <summary>
        ''' Mode 06 test sonuçlarını iste
        ''' </summary>
        Public Sub RequestMode06(Optional testId As Byte = 0)
            If _canSender Is Nothing Then Return

            Try
                ' Format: 02 06 <TestID> 00 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0206" & testId.ToString("X2") & "0000000000"
                _canSender.SendRaw(requestFrame)
            Catch ex As Exception
                RaiseEvent OnError("Mode 06 istek hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Mode 06 sonuçlarını decode et
        ''' </summary>
        Private Sub DecodeMode06(data As Byte())
            If data Is Nothing OrElse data.Length < 6 Then Return

            Try
                Dim tests As New List(Of Mode06TestResult)

                ' Her test sonucu için parse
                Dim testResult As New Mode06TestResult() With {
                    .TestId = data(2),
                    .ComponentId = data(3),
                    .TestValue = (data(4) * 256 + data(5)) * 0.01,
                    .MinLimit = 0,
                    .MaxLimit = 0,
                    .Passed = True,
                    .Description = "Test 0x" & data(2).ToString("X2")
                }

                If data.Length >= 10 Then
                    testResult.MinLimit = (data(6) * 256 + data(7)) * 0.01
                    testResult.MaxLimit = (data(8) * 256 + data(9)) * 0.01
                    testResult.Passed = testResult.TestValue >= testResult.MinLimit AndAlso
                                       testResult.TestValue <= testResult.MaxLimit
                End If

                tests.Add(testResult)

                SyncLock _lock
                    _mode06Tests = tests
                End SyncLock

                RaiseEvent OnMode06Received(tests)

            Catch ex As Exception
                RaiseEvent OnError("Mode 06 decode hatası: " & ex.Message)
            End Try
        End Sub

#End Region

#Region "Mode 08 - Component Control"

        ''' <summary>
        ''' Mode 08 bileşen kontrolü iste
        ''' </summary>
        Public Sub RequestMode08(Optional tidId As Byte = 0)
            If _canSender Is Nothing Then Return

            Try
                ' Format: 02 08 <TID> 00 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0208" & tidId.ToString("X2") & "0000000000"
                _canSender.SendRaw(requestFrame)
            Catch ex As Exception
                RaiseEvent OnError("Mode 08 istek hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Mode 08 sonuçlarını decode et
        ''' </summary>
        Private Sub DecodeMode08(data As Byte())
            If data Is Nothing OrElse data.Length < 4 Then Return

            Try
                Dim actions As New Dictionary(Of Byte, String)
                Dim tid As Byte = data(2)
                Dim status As Byte = If(data.Length > 3, data(3), 0)

                actions(tid) = If(status = 0, "Destekleniyor", "Aktif")

                SyncLock _lock
                    _mode08Actions = actions
                End SyncLock

                RaiseEvent OnMode08Received(actions)

            Catch ex As Exception
                RaiseEvent OnError("Mode 08 decode hatası: " & ex.Message)
            End Try
        End Sub

#End Region

#Region "Mode 09 - Vehicle Information"

        ''' <summary>
        ''' Araç bilgilerini iste (VIN, Calibration ID, CVN, ECU Name)
        ''' Eski metod - geriye dönük uyumluluk için
        ''' </summary>
        Public Sub RequestVehicleInfo()
            ' Async metodunu çağır (fire and forget)
            Task.Run(Async Function() As Task
                         Await RequestVehicleInfoAsync()
                     End Function)
        End Sub

        ''' <summary>
        ''' Araç bilgilerini iste (VIN, Calibration ID, CVN, ECU Name)
        ''' ISO-TP kullanarak multi-frame desteği ile
        ''' </summary>
        Public Async Function RequestVehicleInfoAsync() As Task
            If _canSender Is Nothing Then Return
            InitializeIsoTpHandler()
            If _isoTpHandler Is Nothing Then Return

            Try
                ' VIN (InfoType 0x02) - ISO-TP ile
                Await RequestVINWithIsoTpAsync()

                Threading.Thread.Sleep(200)

                ' Calibration ID (InfoType 0x04) - ISO-TP ile
                Await RequestCalibrationIDWithIsoTpAsync()

                Threading.Thread.Sleep(200)

                ' CVN (InfoType 0x06) - normal frame (kısa)
                Dim cvnRequest As String = "t7DF8" & "0209060000000000"
                _canSender.SendRaw(cvnRequest)

                Threading.Thread.Sleep(100)

                ' ECU Name (InfoType 0x0A) - ISO-TP ile
                Await RequestECUNameWithIsoTpAsync()

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.RequestVehicleInfo")
                RaiseEvent OnError("Vehicle Info istek hatası: " & ex.Message)
            End Try
        End Function

        ''' <summary>
        ''' VIN'i ISO-TP ile ister (Mode 09 PID 02)
        ''' </summary>
        Private Async Function RequestVINWithIsoTpAsync() As Task
            Try
                ' Request: 09 02
                Dim requestData As Byte() = {&H2, &H9, &H2}
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
                    success = Await _isoTpHandler.SendMessage(OBD_REQUEST_ID, requestData)

                    If success Then
                        ' Yanıt bekle (timeout: 2 saniye)
                        Dim received As Boolean = _isoTpResponseEvent.Wait(2000)

                        If received AndAlso _isoTpResponseReceived AndAlso _isoTpResponseData IsNot Nothing Then
                            ' VIN decode et
                            DecodeVINFromIsoTp(_isoTpResponseData)
                            Return
                        End If
                    End If

                    retryCount += 1
                    If retryCount < maxRetries Then
                        Threading.Thread.Sleep(100)
                    End If
                End While

                If Not success Then
                    RaiseEvent OnError("VIN okuma başarısız (3 deneme)")
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.RequestVINWithIsoTp")
                RaiseEvent OnError($"VIN okuma hatası: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Calibration ID'yi ISO-TP ile ister (Mode 09 PID 04)
        ''' </summary>
        Private Async Function RequestCalibrationIDWithIsoTpAsync() As Task
            Try
                ' Request: 09 04
                Dim requestData As Byte() = {&H2, &H9, &H4}
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
                    success = Await _isoTpHandler.SendMessage(OBD_REQUEST_ID, requestData)

                    If success Then
                        ' Yanıt bekle
                        Dim received As Boolean = _isoTpResponseEvent.Wait(2000)

                        If received AndAlso _isoTpResponseReceived AndAlso _isoTpResponseData IsNot Nothing Then
                            ' Calibration ID decode et
                            DecodeCalibrationIDFromIsoTp(_isoTpResponseData)
                            Return
                        End If
                    End If

                    retryCount += 1
                    If retryCount < maxRetries Then
                        Threading.Thread.Sleep(100)
                    End If
                End While

                If Not success Then
                    RaiseEvent OnError("Calibration ID okuma başarısız (3 deneme)")
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.RequestCalibrationIDWithIsoTp")
                RaiseEvent OnError($"Calibration ID okuma hatası: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' ECU Name'i ISO-TP ile ister (Mode 09 PID 0A)
        ''' </summary>
        Private Async Function RequestECUNameWithIsoTpAsync() As Task
            Try
                ' Request: 09 0A
                Dim requestData As Byte() = {&H2, &H9, &HA}
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
                    success = Await _isoTpHandler.SendMessage(OBD_REQUEST_ID, requestData)

                    If success Then
                        ' Yanıt bekle
                        Dim received As Boolean = _isoTpResponseEvent.Wait(2000)

                        If received AndAlso _isoTpResponseReceived AndAlso _isoTpResponseData IsNot Nothing Then
                            ' ECU Name decode et
                            DecodeECUNameFromIsoTp(_isoTpResponseData)
                            Return
                        End If
                    End If

                    retryCount += 1
                    If retryCount < maxRetries Then
                        Threading.Thread.Sleep(100)
                    End If
                End While

                If Not success Then
                    RaiseEvent OnError("ECU Name okuma başarısız (3 deneme)")
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.RequestECUNameWithIsoTp")
                RaiseEvent OnError($"ECU Name okuma hatası: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' ISO-TP'den gelen VIN'i decode eder
        ''' </summary>
        Private Sub DecodeVINFromIsoTp(data As Byte())
            Try
                If data Is Nothing OrElse data.Length < 4 Then Return

                ' Format: 49 02 01 XX XX XX ... (17+ byte VIN)
                If data(0) = OBD_MODE_09_RESPONSE AndAlso data(1) = MODE09_VIN Then
                    Dim count As Byte = data(2)
                    Dim vinLength As Integer = Math.Min(count, data.Length - 3)
                    If vinLength >= 17 Then
                        Dim vinBytes(16) As Byte
                        Array.Copy(data, 3, vinBytes, 0, 17)
                        Dim vin As String = System.Text.Encoding.ASCII.GetString(vinBytes).TrimEnd(Chr(0))
                        
                        SyncLock _lock
                            _vehicleInfo.VIN = vin
                        End SyncLock

                        RaiseEvent OnVehicleInfoReceived(_vehicleInfo)
                    End If
                End If
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.DecodeVINFromIsoTp")
                RaiseEvent OnError($"VIN decode hatası: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' ISO-TP'den gelen Calibration ID'yi decode eder
        ''' </summary>
        Private Sub DecodeCalibrationIDFromIsoTp(data As Byte())
            Try
                If data Is Nothing OrElse data.Length < 4 Then Return

                ' Format: 49 04 XX XX XX ...
                If data(0) = OBD_MODE_09_RESPONSE AndAlso data(1) = MODE09_CALIBRATION_ID Then
                    Dim count As Byte = data(2)
                    Dim calLength As Integer = Math.Min(count, data.Length - 3)
                    If calLength > 0 Then
                        Dim calBytes(calLength - 1) As Byte
                        Array.Copy(data, 3, calBytes, 0, calLength)
                        Dim calId As String = System.Text.Encoding.ASCII.GetString(calBytes).TrimEnd(Chr(0))
                        
                        SyncLock _lock
                            _vehicleInfo.CalibrationID = calId
                        End SyncLock

                        RaiseEvent OnVehicleInfoReceived(_vehicleInfo)
                    End If
                End If
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.DecodeCalibrationIDFromIsoTp")
                RaiseEvent OnError($"Calibration ID decode hatası: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' ISO-TP'den gelen ECU Name'i decode eder
        ''' </summary>
        Private Sub DecodeECUNameFromIsoTp(data As Byte())
            Try
                If data Is Nothing OrElse data.Length < 4 Then Return

                ' Format: 49 0A XX XX XX ...
                If data(0) = OBD_MODE_09_RESPONSE AndAlso data(1) = MODE09_ECU_NAME Then
                    Dim count As Byte = data(2)
                    Dim ecuLength As Integer = Math.Min(count, data.Length - 3)
                    If ecuLength > 0 Then
                        Dim ecuBytes(ecuLength - 1) As Byte
                        Array.Copy(data, 3, ecuBytes, 0, ecuLength)
                        Dim ecuName As String = System.Text.Encoding.ASCII.GetString(ecuBytes).TrimEnd(Chr(0))
                        
                        SyncLock _lock
                            _vehicleInfo.ECUName = ecuName
                        End SyncLock

                        RaiseEvent OnVehicleInfoReceived(_vehicleInfo)
                    End If
                End If
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "OBDService.DecodeECUNameFromIsoTp")
                RaiseEvent OnError($"ECU Name decode hatası: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Mode 09 cevabını decode et
        ''' </summary>
        Private Sub DecodeVehicleInfo(data As Byte())
            If data Is Nothing OrElse data.Length < 4 Then Return

            Try
                Dim infoType As Byte = data(2)
                Dim info = _vehicleInfo

                Select Case infoType
                    Case MODE09_VIN ' VIN
                        If data.Length >= 20 Then
                            Dim vinBytes(16) As Byte
                            Array.Copy(data, 4, vinBytes, 0, Math.Min(17, data.Length - 4))
                            info.VIN = System.Text.Encoding.ASCII.GetString(vinBytes).TrimEnd(Chr(0))
                        End If

                    Case MODE09_CALIBRATION_ID ' Calibration ID
                        If data.Length >= 8 Then
                            Dim calBytes(15) As Byte
                            Array.Copy(data, 4, calBytes, 0, Math.Min(16, data.Length - 4))
                            info.CalibrationID = System.Text.Encoding.ASCII.GetString(calBytes).TrimEnd(Chr(0))
                        End If

                    Case MODE09_CVN ' CVN
                        If data.Length >= 7 Then
                            info.CVN = BitConverter.ToString(data, 4, Math.Min(4, data.Length - 4))
                        End If

                    Case MODE09_ECU_NAME ' ECU Name
                        If data.Length >= 8 Then
                            Dim ecuBytes(19) As Byte
                            Array.Copy(data, 4, ecuBytes, 0, Math.Min(20, data.Length - 4))
                            info.ECUName = System.Text.Encoding.ASCII.GetString(ecuBytes).TrimEnd(Chr(0))
                        End If
                End Select

                SyncLock _lock
                    _vehicleInfo = info
                End SyncLock

                RaiseEvent OnVehicleInfoReceived(info)

            Catch ex As Exception
                RaiseEvent OnError("Vehicle Info decode hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Kayıtlı araç bilgisini döndürür
        ''' </summary>
        Public Function GetVehicleInfo() As VehicleInfoModel
            Return _vehicleInfo
        End Function

#End Region

#Region "PID Auto-Scan"

        ''' <summary>
        ''' Desteklenen PID'leri otomatik tarar
        ''' </summary>
        Public Sub AutoScanPIDs()
            If _canSender Is Nothing Then
                RaiseEvent OnError("CANSender ayarlanmamış!")
                Return
            End If

            Try
                _isScanning = True
                Array.Clear(_supportedPIDs, 0, _supportedPIDs.Length)

                ' PID 0x00, 0x20, 0x40, 0x60 sorgula (her biri 32 PID'in desteklenip desteklenmediğini söyler)
                RequestPID(PID_SUPPORTED_01_20)
                Threading.Thread.Sleep(200)
                RequestPID(PID_SUPPORTED_21_40)
                Threading.Thread.Sleep(200)
                RequestPID(PID_SUPPORTED_41_60)
                Threading.Thread.Sleep(200)
                RequestPID(PID_SUPPORTED_61_80)

                _isScanning = False
                RaiseEvent OnAutoScanCompleted(_supportedPIDs)

            Catch ex As Exception
                _isScanning = False
                RaiseEvent OnError("PID Auto-Scan hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Desteklenen PID haritasını decode et (PID 0x00, 0x20, 0x40, 0x60 cevapları)
        ''' </summary>
        Private Sub DecodeSupportedPIDMap(basePid As Byte, data As Byte())
            If data Is Nothing OrElse data.Length < 4 Then Return

            Try
                ' 4 byte = 32 bit, her bit bir PID'in desteklenip desteklenmediğini gösterir
                Dim bitmap As UInteger = CUInt(data(0)) << 24 Or CUInt(data(1)) << 16 Or CUInt(data(2)) << 8 Or CUInt(data(3))

                For i As Integer = 0 To 31
                    Dim pidOffset = basePid + i + 1
                    If pidOffset < 256 Then
                        _supportedPIDs(pidOffset) = ((bitmap >> (31 - i)) And 1) = 1
                    End If
                Next

            Catch ex As Exception
                RaiseEvent OnError("PID Map decode hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Belirtilen PID'in desteklenip desteklenmediğini döndürür
        ''' </summary>
        Public Function IsPIDSupported(pid As Byte) As Boolean
            Return _supportedPIDs(pid)
        End Function

        ''' <summary>
        ''' Desteklenen PID listesini döndürür
        ''' </summary>
        Public Function GetSupportedPIDList() As List(Of Byte)
            Dim result As New List(Of Byte)
            For i As Integer = 1 To 255
                If _supportedPIDs(i) Then
                    result.Add(CByte(i))
                End If
            Next
            Return result
        End Function

#End Region

#Region "Gelişmiş Frame İşleme"

        ''' <summary>
        ''' Gelen frame'i tüm modlar için işler
        ''' ISO-TP multi-frame mesajlarını da işler
        ''' </summary>
        Public Sub ProcessFrameAdvanced(frameId As Integer, data As Byte())
            If Not IsOBDResponse(frameId) Then Return
            If data Is Nothing OrElse data.Length = 0 Then Return

            Try
                InitializeIsoTpHandler()

                ' ISO-TP frame kontrolü (ilk byte'ın ilk nibble'ına bak)
                If data.Length >= 1 Then
                    Dim firstByte As Byte = data(0)
                    Dim frameTypeValue As Integer = (firstByte And &HF0) >> 4

                    ' ISO-TP frame ise (Single Frame, First Frame, Consecutive Frame, Flow Control)
                    If frameTypeValue >= 0 AndAlso frameTypeValue <= 3 Then
                        ' ISO-TP handler'a yönlendir
                        If _isoTpHandler IsNot Nothing Then
                            _isoTpHandler.ProcessFrame(frameId, data)
                        End If
                        Return
                    End If
                End If

                ' Normal OBD frame işleme (tek frame)
                If data.Length < 3 Then Return

                Dim mode As Byte = data(1)

                Select Case mode
                    Case OBD_MODE_01_RESPONSE
                        ' Mode 01 - mevcut kodu kullan
                        ProcessMode01Response(data)

                    Case OBD_MODE_02_RESPONSE
                        ' Mode 02 - Freeze Frame
                        DecodeFreezeFrame(data)

                    Case OBD_MODE_03_RESPONSE
                        ' Mode 03 - Stored DTCs
                        DecodeStoredDTC(data)

                    Case OBD_MODE_07_RESPONSE
                        ' Mode 07 - Pending DTCs
                        DecodePendingDTC(data)

                    Case OBD_MODE_06_RESPONSE
                        ' Mode 06 - On-Board Monitoring
                        DecodeMode06(data)

                    Case OBD_MODE_08_RESPONSE
                        ' Mode 08 - Component Control
                        DecodeMode08(data)

                    Case OBD_MODE_09_RESPONSE
                        ' Mode 09 - Vehicle Information (tek frame ise)
                        DecodeVehicleInfo(data)

                End Select

                ' Supported PID map kontrolü
                Dim pid As Byte = data(2)
                If pid = PID_SUPPORTED_01_20 OrElse pid = PID_SUPPORTED_21_40 OrElse
                   pid = PID_SUPPORTED_41_60 OrElse pid = PID_SUPPORTED_61_80 Then
                    If data.Length >= 7 Then
                        Dim mapData(3) As Byte
                        Array.Copy(data, 3, mapData, 0, 4)
                        DecodeSupportedPIDMap(pid, mapData)
                    End If
                End If

            Catch ex As Exception
                RaiseEvent OnError("Frame işleme hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Mode 01 cevabını işler (mevcut ProcessFrame mantığı)
        ''' </summary>
        Private Sub ProcessMode01Response(data As Byte())
            If data.Length < 4 Then Return

            Dim pid As Byte = data(2)
            Dim A As Integer = If(data.Length > 3, data(3), 0)
            Dim B As Integer = If(data.Length > 4, data(4), 0)

            ' O2 sensör mü?
            If pid >= PID_O2_SENSOR_1 AndAlso pid <= PID_O2_SENSOR_8 Then
                Dim sensorIndex = pid - PID_O2_SENSOR_1
                Dim voltage = A * 0.005
                _o2Voltages(sensorIndex) = voltage
                RaiseEvent OnO2SensorUpdated(sensorIndex, voltage)
            Else
                Dim value = DecodePID(pid, A, B)
                SyncLock _lock
                    _pidValues(pid) = value
                End SyncLock
                RaiseEvent OnPIDUpdated(pid, value, GetPIDUnit(pid))
            End If
        End Sub

#End Region

#Region "DTC Temizleme (Mode 04)"

        ''' <summary>
        ''' DTC'leri temizle (Mode 04)
        ''' </summary>
        Public Sub ClearDTC()
            If _canSender Is Nothing Then Return

            Try
                ' Format: 01 04 00 00 00 00 00 00
                Dim requestFrame As String = "t7DF8" & "0104000000000000"
                _canSender.SendRaw(requestFrame)
                RaiseEvent OnDTCCleared()
            Catch ex As Exception
                RaiseEvent OnError("DTC temizleme hatası: " & ex.Message)
            End Try
        End Sub

#End Region

#Region "Özellikler - Gelişmiş"

        Public ReadOnly Property PendingDTCs As List(Of DTCInfo)
            Get
                Return _pendingDTCs
            End Get
        End Property

        Public ReadOnly Property FreezeFrameData As Dictionary(Of Byte, Double)
            Get
                Return _freezeFrameData
            End Get
        End Property

        Public ReadOnly Property Mode06Tests As List(Of Mode06TestResult)
            Get
                Return _mode06Tests
            End Get
        End Property

        Public ReadOnly Property SupportedPIDs As Boolean()
            Get
                Return _supportedPIDs
            End Get
        End Property

        Public ReadOnly Property IsScanning As Boolean
            Get
                Return _isScanning
            End Get
        End Property

#End Region

    End Class

End Namespace

