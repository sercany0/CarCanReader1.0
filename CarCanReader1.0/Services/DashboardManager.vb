' DashboardManager.vb
' Dashboard veri işleme servisi
' MainForm'dan taşınan dashboard mantığını içerir
'
' Desteklenen CAN ID'ler (project-spec.md Section 6):
'   0x123 → RPM (byte0*256 + byte1)
'   0x201 → Speed (byte0)
'   0x300 → Coolant Temperature (byte0)
'   0x450 → Door Status (0 = kapalı, 1 = açık)
'   0x321 → Lights Status (0 = kapalı, 1 = açık)
'
' Kullanım:
'   Dim dashboard As New DashboardManager()
'   AddHandler dashboard.OnRPMChanged, Sub(v) lblRPM.Text = v.ToString()

Imports CarCanReader1._0.Models
'   
'   ' Her frame geldiğinde:
'   dashboard.ProcessFrame(frameId, frameData)

Namespace Services

Public Class DashboardManager

#Region "Sabitler - CAN ID Tanımları"

    ' RPM verisi için CAN ID
    Public Const CAN_ID_RPM As Integer = &H123

    ' Hız verisi için CAN ID
    Public Const CAN_ID_SPEED As Integer = &H201

    ' Motor sıcaklığı için CAN ID
    Public Const CAN_ID_TEMPERATURE As Integer = &H300

    ' Kapı durumu için CAN ID
    Public Const CAN_ID_DOOR As Integer = &H450

    ' Far durumu için CAN ID
    Public Const CAN_ID_LIGHTS As Integer = &H321

#End Region

#Region "Sabitler - Değer Sınırları"

    ' RPM sınırları
    Private Const RPM_MIN As Integer = 0
    Private Const RPM_MAX As Integer = 10000

    ' Hız sınırları (km/h)
    Private Const SPEED_MIN As Integer = 0
    Private Const SPEED_MAX As Integer = 300

    ' Sıcaklık sınırları (°C)
    Private Const TEMP_MIN As Integer = -40
    Private Const TEMP_MAX As Integer = 150

    ' Geçmiş kayıt limiti
    Private Const HISTORY_MAX_SIZE As Integer = 1000

    ' Throttling - minimum ms between UI updates (prevents UI freeze)
    Private Const MIN_UPDATE_INTERVAL_MS As Integer = 50

    ' Smoothing factor for gauges (0-1, higher = more responsive)
    Private Const SMOOTHING_FACTOR As Double = 0.3

#End Region

#Region "Olaylar (Events)"

    ''' <summary>
    ''' RPM değeri değiştiğinde tetiklenir
    ''' </summary>
    Public Event OnRPMChanged(value As Integer)

    ''' <summary>
    ''' Hız değeri değiştiğinde tetiklenir
    ''' </summary>
    Public Event OnSpeedChanged(value As Integer)

    ''' <summary>
    ''' Sıcaklık değeri değiştiğinde tetiklenir
    ''' </summary>
    Public Event OnTemperatureChanged(value As Integer)

    ''' <summary>
    ''' Kapı durumu değiştiğinde tetiklenir
    ''' </summary>
    Public Event OnDoorStateChanged(isOpen As Boolean)

    ''' <summary>
    ''' Far durumu değiştiğinde tetiklenir
    ''' </summary>
    Public Event OnLightStateChanged(isOn As Boolean)

    ''' <summary>
    ''' Herhangi bir dashboard değeri güncellendiğinde tetiklenir
    ''' </summary>
    Public Event OnDashboardUpdated()

    ' ========================================
    ' YENİ OBD-II OLAYLARI
    ' ========================================

    Public Event OnEngineLoadChanged(value As Double)
    Public Event OnThrottleChanged(value As Double)
    Public Event OnIntakeTempChanged(value As Integer)
    Public Event OnAmbientTempChanged(value As Integer)
    Public Event OnFuelLevelChanged(value As Double)
    Public Event OnMAPChanged(value As Integer)
    Public Event OnMAFChanged(value As Double)
    Public Event OnShortFuelTrimChanged(value As Double)
    Public Event OnLongFuelTrimChanged(value As Double)
    Public Event OnBarometricChanged(value As Integer)
    Public Event OnModuleVoltageChanged(value As Double)
    Public Event OnFuelRateChanged(value As Double)
    Public Event OnO2SensorChanged(sensorIndex As Integer, voltage As Double)

    ' ========================================
    ' OBD-II ADVANCED OLAYLARI (Mode 02-09)
    ' ========================================
    Public Event OnPendingDTCUpdated(dtcs As List(Of DTCInfo))
    Public Event OnStoredDTCUpdated(dtcs As List(Of DTCInfo))
    Public Event OnFreezeFrameUpdated(data As Dictionary(Of Byte, Double))
    Public Event OnMode06Updated(tests As List(Of Mode06TestResult))
    Public Event OnMode08Updated(actions As Dictionary(Of Byte, String))
    Public Event OnVehicleInfoUpdated(info As VehicleInfoModel)
    Public Event OnSupportedPIDsUpdated(pids As Boolean())

#End Region

#Region "Özel Alanlar - Güncel Değerler"

    ' Güncel değerler
    Private _rpm As Integer = 0
    Private _speed As Integer = 0
    Private _temperature As Integer = 0
    Private _doorOpen As Boolean = False
    Private _lightsOn As Boolean = False

    ' Smoothed values for gauges
    Private _smoothedRpm As Double = 0
    Private _smoothedSpeed As Double = 0
    Private _smoothedTemp As Double = 0

    ' Son güncelleme zamanları
    Private _lastRpmUpdate As DateTime = DateTime.MinValue
    Private _lastSpeedUpdate As DateTime = DateTime.MinValue
    Private _lastTempUpdate As DateTime = DateTime.MinValue
    Private _lastDoorUpdate As DateTime = DateTime.MinValue
    Private _lastLightsUpdate As DateTime = DateTime.MinValue

    ' Throttling - last event fire time
    Private _lastRpmEventTime As DateTime = DateTime.MinValue
    Private _lastSpeedEventTime As DateTime = DateTime.MinValue
    Private _lastTempEventTime As DateTime = DateTime.MinValue

    ' Thread safety lock
    Private ReadOnly _lock As New Object()

    ' ========================================
    ' YENİ OBD-II ALANLARI
    ' ========================================
    Private _engineLoad As Double = 0
    Private _throttle As Double = 0
    Private _intakeTemp As Integer = 0
    Private _ambientTemp As Integer = 0
    Private _fuelLevel As Double = 0
    Private _mapValue As Integer = 0
    Private _mafValue As Double = 0
    Private _shortFuelTrim As Double = 0
    Private _longFuelTrim As Double = 0
    Private _barometric As Integer = 0
    Private _moduleVoltage As Double = 0
    Private _fuelRate As Double = 0
    Private _o2Sensors(7) As Double

    ' ========================================
    ' OBD-II ADVANCED ALANLARI (Mode 02-09)
    ' ========================================
    Private _pendingDTCs As New List(Of DTCInfo)
    Private _storedDTCs As New List(Of DTCInfo)
    Private _freezeFrameData As New Dictionary(Of Byte, Double)
    Private _mode06Tests As New List(Of Mode06TestResult)
    Private _mode08Actions As New Dictionary(Of Byte, String)
    Private _vehicleInfo As New VehicleInfoModel()
    Private _supportedPIDs(255) As Boolean

#End Region

#Region "Özel Alanlar - Geçmiş Kayıtları"

    ' Geçmiş değer listeleri
    Private _rpmHistory As New List(Of Integer)
    Private _speedHistory As New List(Of Integer)
    Private _tempHistory As New List(Of Integer)

    ' Zaman damgalı geçmiş (CSV export için)
    Private _rpmHistoryWithTime As New List(Of Tuple(Of DateTime, Integer))
    Private _speedHistoryWithTime As New List(Of Tuple(Of DateTime, Integer))
    Private _tempHistoryWithTime As New List(Of Tuple(Of DateTime, Integer))

#End Region

#Region "Özellikler (Properties) - Güncel Değerler"

    ''' <summary>
    ''' Güncel RPM değeri
    ''' </summary>
    Public ReadOnly Property RPM As Integer
        Get
            Return _rpm
        End Get
    End Property

    ''' <summary>
    ''' Güncel hız değeri (km/h)
    ''' </summary>
    Public ReadOnly Property Speed As Integer
        Get
            Return _speed
        End Get
    End Property

    ''' <summary>
    ''' Güncel motor sıcaklığı (°C)
    ''' </summary>
    Public ReadOnly Property Temperature As Integer
        Get
            Return _temperature
        End Get
    End Property

    ''' <summary>
    ''' Kapı durumu (True = açık, False = kapalı)
    ''' </summary>
    Public ReadOnly Property DoorOpen As Boolean
        Get
            Return _doorOpen
        End Get
    End Property

    ''' <summary>
    ''' Far durumu (True = açık, False = kapalı)
    ''' </summary>
    Public ReadOnly Property LightsOn As Boolean
        Get
            Return _lightsOn
        End Get
    End Property

    ' ========================================
    ' YENİ OBD-II ÖZELLİKLERİ
    ' ========================================

    Public ReadOnly Property EngineLoad As Double
        Get
            Return _engineLoad
        End Get
    End Property

    Public ReadOnly Property Throttle As Double
        Get
            Return _throttle
        End Get
    End Property

    Public ReadOnly Property IntakeTemp As Integer
        Get
            Return _intakeTemp
        End Get
    End Property

    Public ReadOnly Property AmbientTemp As Integer
        Get
            Return _ambientTemp
        End Get
    End Property

    Public ReadOnly Property FuelLevel As Double
        Get
            Return _fuelLevel
        End Get
    End Property

    Public ReadOnly Property MAPValue As Integer
        Get
            Return _mapValue
        End Get
    End Property

    Public ReadOnly Property MAFValue As Double
        Get
            Return _mafValue
        End Get
    End Property

    Public ReadOnly Property ShortFuelTrim As Double
        Get
            Return _shortFuelTrim
        End Get
    End Property

    Public ReadOnly Property LongFuelTrim As Double
        Get
            Return _longFuelTrim
        End Get
    End Property

    Public ReadOnly Property Barometric As Integer
        Get
            Return _barometric
        End Get
    End Property

    Public ReadOnly Property ModuleVoltage As Double
        Get
            Return _moduleVoltage
        End Get
    End Property

    Public ReadOnly Property FuelRate As Double
        Get
            Return _fuelRate
        End Get
    End Property

    Public ReadOnly Property O2Sensors As Double()
        Get
            Return _o2Sensors
        End Get
    End Property

#End Region

#Region "Özellikler (Properties) - Geçmiş Kayıtları"

    ''' <summary>
    ''' RPM geçmiş değerleri listesi
    ''' </summary>
    Public ReadOnly Property RPMHistory As List(Of Integer)
        Get
            Return _rpmHistory
        End Get
    End Property

    ''' <summary>
    ''' Hız geçmiş değerleri listesi
    ''' </summary>
    Public ReadOnly Property SpeedHistory As List(Of Integer)
        Get
            Return _speedHistory
        End Get
    End Property

    ''' <summary>
    ''' Sıcaklık geçmiş değerleri listesi
    ''' </summary>
    Public ReadOnly Property TempHistory As List(Of Integer)
        Get
            Return _tempHistory
        End Get
    End Property

#End Region

#Region "Özellikler (Properties) - Son Güncelleme Zamanları"

    ''' <summary>
    ''' RPM son güncelleme zamanı
    ''' </summary>
    Public ReadOnly Property LastRPMUpdate As DateTime
        Get
            Return _lastRpmUpdate
        End Get
    End Property

    ''' <summary>
    ''' Hız son güncelleme zamanı
    ''' </summary>
    Public ReadOnly Property LastSpeedUpdate As DateTime
        Get
            Return _lastSpeedUpdate
        End Get
    End Property

    ''' <summary>
    ''' Sıcaklık son güncelleme zamanı
    ''' </summary>
    Public ReadOnly Property LastTemperatureUpdate As DateTime
        Get
            Return _lastTempUpdate
        End Get
    End Property

#End Region

#Region "Ana İşleme Metodu"

    ''' <summary>
    ''' Gelen CAN frame'i işler ve ilgili dashboard değerini günceller
    ''' </summary>
    ''' <param name="id">CAN ID</param>
    ''' <param name="data">Frame data baytları</param>
    Public Sub ProcessFrame(id As Integer, data() As Byte)
        ' Null veya boş data kontrolü
        If data Is Nothing Then Return

        ' ID'ye göre ilgili işleyiciyi çağır
        Select Case id

            Case CAN_ID_RPM
                ProcessRPM(data)

            Case CAN_ID_SPEED
                ProcessSpeed(data)

            Case CAN_ID_TEMPERATURE
                ProcessTemperature(data)

            Case CAN_ID_DOOR
                ProcessDoorState(data)

            Case CAN_ID_LIGHTS
                ProcessLightState(data)

        End Select
    End Sub

#End Region

#Region "Özel İşleme Metodları"

    ''' <summary>
    ''' RPM verisini işler (ID 0x123)
    ''' Format: byte0 * 256 + byte1
    ''' Smoothing ve throttling uygulanır
    ''' </summary>
    Private Sub ProcessRPM(data() As Byte)
        ' En az 2 byte gerekli
        If data.Length < 2 Then Return

        SyncLock _lock
            ' RPM hesapla: byte0 * 256 + byte1
            Dim rawRpm As Integer = data(0) * 256 + data(1)

            ' Değeri sınırla ve doğrula
            Dim validRpm As Integer = SanitizeValue(rawRpm, RPM_MIN, RPM_MAX)

            ' Smoothing uygula (ani değişimleri yumuşat)
            _smoothedRpm = _smoothedRpm * (1 - SMOOTHING_FACTOR) + validRpm * SMOOTHING_FACTOR
            Dim smoothedValue As Integer = CInt(Math.Round(_smoothedRpm))

            ' Değişiklik kontrolü
            If smoothedValue <> _rpm Then
                _rpm = smoothedValue
                _lastRpmUpdate = DateTime.Now

                ' Geçmişe ekle (throttle olmadan)
                AddToHistory(_rpmHistory, _rpm)
                _rpmHistoryWithTime.Add(Tuple.Create(DateTime.Now, _rpm))

                ' Throttle: minimum interval between UI events
                Dim now = DateTime.Now
                If (now - _lastRpmEventTime).TotalMilliseconds >= MIN_UPDATE_INTERVAL_MS Then
                    _lastRpmEventTime = now
                    RaiseEvent OnRPMChanged(_rpm)
                    RaiseEvent OnDashboardUpdated()
                End If
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Hız verisini işler (ID 0x201)
    ''' Format: byte0 = speed in km/h
    ''' Smoothing ve throttling uygulanır
    ''' </summary>
    Private Sub ProcessSpeed(data() As Byte)
        ' En az 1 byte gerekli
        If data.Length < 1 Then Return

        SyncLock _lock
            ' Hız hesapla
            Dim rawSpeed As Integer = data(0)

            ' Değeri sınırla
            Dim validSpeed As Integer = SanitizeValue(rawSpeed, SPEED_MIN, SPEED_MAX)

            ' Smoothing uygula
            _smoothedSpeed = _smoothedSpeed * (1 - SMOOTHING_FACTOR) + validSpeed * SMOOTHING_FACTOR
            Dim smoothedValue As Integer = CInt(Math.Round(_smoothedSpeed))

            ' Değişiklik kontrolü
            If smoothedValue <> _speed Then
                _speed = smoothedValue
                _lastSpeedUpdate = DateTime.Now

                ' Geçmişe ekle
                AddToHistory(_speedHistory, _speed)
                _speedHistoryWithTime.Add(Tuple.Create(DateTime.Now, _speed))

                ' Throttle
                Dim now = DateTime.Now
                If (now - _lastSpeedEventTime).TotalMilliseconds >= MIN_UPDATE_INTERVAL_MS Then
                    _lastSpeedEventTime = now
                    RaiseEvent OnSpeedChanged(_speed)
                    RaiseEvent OnDashboardUpdated()
                End If
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Sıcaklık verisini işler (ID 0x300)
    ''' Format: byte0 = temperature in °C
    ''' Smoothing ve throttling uygulanır
    ''' </summary>
    Private Sub ProcessTemperature(data() As Byte)
        ' En az 1 byte gerekli
        If data.Length < 1 Then Return

        SyncLock _lock
            ' Sıcaklık hesapla (işaretli değer olabilir)
            Dim rawTemp As Integer = data(0)

            ' Bazı araçlar offset kullanır (örn: -40 offset)
            ' Gerekirse burada offset uygulanabilir
            ' rawTemp = rawTemp - 40

            ' Değeri sınırla
            Dim validTemp As Integer = SanitizeValue(rawTemp, TEMP_MIN, TEMP_MAX)

            ' Smoothing uygula (sıcaklık için daha az agresif)
            _smoothedTemp = _smoothedTemp * 0.8 + validTemp * 0.2
            Dim smoothedValue As Integer = CInt(Math.Round(_smoothedTemp))

            ' Değişiklik kontrolü
            If smoothedValue <> _temperature Then
                _temperature = smoothedValue
                _lastTempUpdate = DateTime.Now

                ' Geçmişe ekle
                AddToHistory(_tempHistory, _temperature)
                _tempHistoryWithTime.Add(Tuple.Create(DateTime.Now, _temperature))

                ' Throttle
                Dim now = DateTime.Now
                If (now - _lastTempEventTime).TotalMilliseconds >= MIN_UPDATE_INTERVAL_MS Then
                    _lastTempEventTime = now
                    RaiseEvent OnTemperatureChanged(_temperature)
                    RaiseEvent OnDashboardUpdated()
                End If
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Kapı durumunu işler (ID 0x450)
    ''' Format: byte0 = 0 (kapalı), 1 (açık)
    ''' </summary>
    Private Sub ProcessDoorState(data() As Byte)
        ' En az 1 byte gerekli
        If data.Length < 1 Then Return

        ' Kapı durumunu al
        Dim isOpen As Boolean = (data(0) = 1)

        ' Değişiklik kontrolü
        If isOpen <> _doorOpen Then
            _doorOpen = isOpen
            _lastDoorUpdate = DateTime.Now

            ' Olayı tetikle
            RaiseEvent OnDoorStateChanged(_doorOpen)
            RaiseEvent OnDashboardUpdated()
        End If
    End Sub

    ''' <summary>
    ''' Far durumunu işler (ID 0x321)
    ''' Format: byte0 = 0 (kapalı), 1 (açık)
    ''' </summary>
    Private Sub ProcessLightState(data() As Byte)
        ' En az 1 byte gerekli
        If data.Length < 1 Then Return

        ' Far durumunu al
        Dim isOn As Boolean = (data(0) = 1)

        ' Değişiklik kontrolü
        If isOn <> _lightsOn Then
            _lightsOn = isOn
            _lastLightsUpdate = DateTime.Now

            ' Olayı tetikle
            RaiseEvent OnLightStateChanged(_lightsOn)
            RaiseEvent OnDashboardUpdated()
        End If
    End Sub

#End Region

#Region "OBD-II Güncelleme Metodları"

    ''' <summary>
    ''' Motor yükünü günceller (OBD PID 0x04)
    ''' </summary>
    Public Sub UpdateEngineLoad(value As Double)
        SyncLock _lock
            If Math.Abs(value - _engineLoad) > 0.1 Then
                _engineLoad = value
                RaiseEvent OnEngineLoadChanged(_engineLoad)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Gaz kelebeği pozisyonunu günceller (OBD PID 0x11)
    ''' </summary>
    Public Sub UpdateThrottle(value As Double)
        SyncLock _lock
            If Math.Abs(value - _throttle) > 0.1 Then
                _throttle = value
                RaiseEvent OnThrottleChanged(_throttle)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Emme havası sıcaklığını günceller (OBD PID 0x0F)
    ''' </summary>
    Public Sub UpdateIntakeTemp(value As Integer)
        SyncLock _lock
            If value <> _intakeTemp Then
                _intakeTemp = value
                RaiseEvent OnIntakeTempChanged(_intakeTemp)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Ortam sıcaklığını günceller (OBD PID 0x46)
    ''' </summary>
    Public Sub UpdateAmbientTemp(value As Integer)
        SyncLock _lock
            If value <> _ambientTemp Then
                _ambientTemp = value
                RaiseEvent OnAmbientTempChanged(_ambientTemp)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Yakıt seviyesini günceller (OBD PID 0x2F)
    ''' </summary>
    Public Sub UpdateFuelLevel(value As Double)
        SyncLock _lock
            If Math.Abs(value - _fuelLevel) > 0.1 Then
                _fuelLevel = value
                RaiseEvent OnFuelLevelChanged(_fuelLevel)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' MAP değerini günceller (OBD PID 0x0B)
    ''' </summary>
    Public Sub UpdateMAP(value As Integer)
        SyncLock _lock
            If value <> _mapValue Then
                _mapValue = value
                RaiseEvent OnMAPChanged(_mapValue)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' MAF değerini günceller (OBD PID 0x10)
    ''' </summary>
    Public Sub UpdateMAF(value As Double)
        SyncLock _lock
            If Math.Abs(value - _mafValue) > 0.01 Then
                _mafValue = value
                RaiseEvent OnMAFChanged(_mafValue)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Kısa vadeli yakıt trim'i günceller (OBD PID 0x06)
    ''' </summary>
    Public Sub UpdateShortFuelTrim(value As Double)
        SyncLock _lock
            If Math.Abs(value - _shortFuelTrim) > 0.1 Then
                _shortFuelTrim = value
                RaiseEvent OnShortFuelTrimChanged(_shortFuelTrim)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Uzun vadeli yakıt trim'i günceller (OBD PID 0x07)
    ''' </summary>
    Public Sub UpdateLongFuelTrim(value As Double)
        SyncLock _lock
            If Math.Abs(value - _longFuelTrim) > 0.1 Then
                _longFuelTrim = value
                RaiseEvent OnLongFuelTrimChanged(_longFuelTrim)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Atmosfer basıncını günceller (OBD PID 0x33)
    ''' </summary>
    Public Sub UpdateBarometric(value As Integer)
        SyncLock _lock
            If value <> _barometric Then
                _barometric = value
                RaiseEvent OnBarometricChanged(_barometric)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' ECU voltajını günceller (OBD PID 0x42)
    ''' </summary>
    Public Sub UpdateModuleVoltage(value As Double)
        SyncLock _lock
            If Math.Abs(value - _moduleVoltage) > 0.01 Then
                _moduleVoltage = value
                RaiseEvent OnModuleVoltageChanged(_moduleVoltage)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Yakıt tüketim oranını günceller (OBD PID 0x5E)
    ''' </summary>
    Public Sub UpdateFuelRate(value As Double)
        SyncLock _lock
            If Math.Abs(value - _fuelRate) > 0.01 Then
                _fuelRate = value
                RaiseEvent OnFuelRateChanged(_fuelRate)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' O2 sensör voltajını günceller (OBD PID 0x14-0x1B)
    ''' </summary>
    Public Sub UpdateO2Sensor(sensorIndex As Integer, voltage As Double)
        If sensorIndex < 0 OrElse sensorIndex > 7 Then Return

        SyncLock _lock
            If Math.Abs(voltage - _o2Sensors(sensorIndex)) > 0.001 Then
                _o2Sensors(sensorIndex) = voltage
                RaiseEvent OnO2SensorChanged(sensorIndex, voltage)
            End If
        End SyncLock
    End Sub

#End Region

#Region "OBD-II Advanced Güncelleme Metodları (Mode 02-09)"

    ''' <summary>
    ''' Bekleyen DTC'leri günceller (Mode 07)
    ''' </summary>
    Public Sub UpdatePendingDTC(dtcs As List(Of DTCInfo))
        SyncLock _lock
            _pendingDTCs = dtcs
        End SyncLock
        RaiseEvent OnPendingDTCUpdated(dtcs)
    End Sub

    ''' <summary>
    ''' Kayıtlı DTC'leri günceller (Mode 03)
    ''' </summary>
    Public Sub UpdateStoredDTC(dtcs As List(Of DTCInfo))
        SyncLock _lock
            _storedDTCs = dtcs
        End SyncLock
        RaiseEvent OnStoredDTCUpdated(dtcs)
    End Sub

    ''' <summary>
    ''' Freeze Frame verisini günceller (Mode 02)
    ''' </summary>
    Public Sub UpdateFreezeFrame(data As Dictionary(Of Byte, Double))
        SyncLock _lock
            _freezeFrameData = data
        End SyncLock
        RaiseEvent OnFreezeFrameUpdated(data)
    End Sub

    ''' <summary>
    ''' Mode 06 test sonuçlarını günceller
    ''' </summary>
    Public Sub UpdateMode06(tests As List(Of Mode06TestResult))
        SyncLock _lock
            _mode06Tests = tests
        End SyncLock
        RaiseEvent OnMode06Updated(tests)
    End Sub

    ''' <summary>
    ''' Mode 08 aksiyonlarını günceller
    ''' </summary>
    Public Sub UpdateMode08(actions As Dictionary(Of Byte, String))
        SyncLock _lock
            _mode08Actions = actions
        End SyncLock
        RaiseEvent OnMode08Updated(actions)
    End Sub

    ''' <summary>
    ''' Araç bilgisini günceller (Mode 09)
    ''' </summary>
    Public Sub UpdateVehicleInfo(info As VehicleInfoModel)
        SyncLock _lock
            _vehicleInfo = info
        End SyncLock
        RaiseEvent OnVehicleInfoUpdated(info)
    End Sub

    ''' <summary>
    ''' Desteklenen PID bayraklarını günceller
    ''' </summary>
    Public Sub UpdateSupportedPIDFlags(pids As Boolean())
        SyncLock _lock
            Array.Copy(pids, _supportedPIDs, Math.Min(pids.Length, _supportedPIDs.Length))
        End SyncLock
        RaiseEvent OnSupportedPIDsUpdated(pids)
    End Sub

    ' Properties for advanced OBD data
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

    Public ReadOnly Property Mode08Actions As Dictionary(Of Byte, String)
        Get
            Return _mode08Actions
        End Get
    End Property

    Public ReadOnly Property VehicleInfo As VehicleInfoModel
        Get
            Return _vehicleInfo
        End Get
    End Property

    Public ReadOnly Property SupportedPIDFlags As Boolean()
        Get
            Return _supportedPIDs
        End Get
    End Property

#End Region

#Region "Yardımcı Metodlar"

    ''' <summary>
    ''' Değeri belirtilen aralıkta sınırlar
    ''' </summary>
    Private Function SanitizeValue(value As Integer, minVal As Integer, maxVal As Integer) As Integer
        If value < minVal Then Return minVal
        If value > maxVal Then Return maxVal
        Return value
    End Function

    ''' <summary>
    ''' Geçmiş listesine değer ekler, maksimum boyutu aşarsa eski değerleri siler
    ''' </summary>
    Private Sub AddToHistory(history As List(Of Integer), value As Integer)
        history.Add(value)

        ' Maksimum boyutu aşarsa baştan sil
        While history.Count > HISTORY_MAX_SIZE
            history.RemoveAt(0)
        End While
    End Sub

#End Region

#Region "CSV Export"

    ''' <summary>
    ''' Tüm dashboard geçmişini CSV formatında döndürür
    ''' </summary>
    Public Function ExportToCSV() As String
        Dim sb As New System.Text.StringBuilder()

        ' Başlık satırı
        sb.AppendLine("Timestamp,Type,Value")

        ' RPM geçmişi
        For Each item In _rpmHistoryWithTime
            sb.AppendLine($"{item.Item1:yyyy-MM-dd HH:mm:ss.fff},RPM,{item.Item2}")
        Next

        ' Speed geçmişi
        For Each item In _speedHistoryWithTime
            sb.AppendLine($"{item.Item1:yyyy-MM-dd HH:mm:ss.fff},Speed,{item.Item2}")
        Next

        ' Temperature geçmişi
        For Each item In _tempHistoryWithTime
            sb.AppendLine($"{item.Item1:yyyy-MM-dd HH:mm:ss.fff},Temperature,{item.Item2}")
        Next

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Dashboard geçmişini CSV dosyasına kaydeder
    ''' </summary>
    Public Sub SaveToCSV(filePath As String)
        Dim csvContent As String = ExportToCSV()
        IO.File.WriteAllText(filePath, csvContent)
    End Sub

    ''' <summary>
    ''' Sütun bazlı CSV formatında döndürür (Time, RPM, Speed, Temp)
    ''' </summary>
    Public Function ExportToCSVColumns() As String
        Dim sb As New System.Text.StringBuilder()

        ' Başlık satırı
        sb.AppendLine("Time,RPM,Speed,Temperature")

        ' Tüm zaman noktalarını birleştir
        Dim allTimes As New SortedSet(Of DateTime)

        For Each item In _rpmHistoryWithTime
            allTimes.Add(item.Item1)
        Next
        For Each item In _speedHistoryWithTime
            allTimes.Add(item.Item1)
        Next
        For Each item In _tempHistoryWithTime
            allTimes.Add(item.Item1)
        Next

        ' Her zaman noktası için satır oluştur
        Dim rpmDict = _rpmHistoryWithTime.ToDictionary(Function(x) x.Item1, Function(x) x.Item2)
        Dim speedDict = _speedHistoryWithTime.ToDictionary(Function(x) x.Item1, Function(x) x.Item2)
        Dim tempDict = _tempHistoryWithTime.ToDictionary(Function(x) x.Item1, Function(x) x.Item2)

        Dim lastRpm As Integer = 0
        Dim lastSpeed As Integer = 0
        Dim lastTemp As Integer = 0

        For Each t In allTimes
            If rpmDict.ContainsKey(t) Then lastRpm = rpmDict(t)
            If speedDict.ContainsKey(t) Then lastSpeed = speedDict(t)
            If tempDict.ContainsKey(t) Then lastTemp = tempDict(t)

            sb.AppendLine($"{t:HH:mm:ss.fff},{lastRpm},{lastSpeed},{lastTemp}")
        Next

        Return sb.ToString()
    End Function

#End Region

#Region "İstatistikler"

    ''' <summary>
    ''' Maksimum RPM değerini döndürür
    ''' </summary>
    Public Function GetMaxRPM() As Integer
        If _rpmHistory.Count = 0 Then Return 0
        Return _rpmHistory.Max()
    End Function

    ''' <summary>
    ''' Maksimum hız değerini döndürür
    ''' </summary>
    Public Function GetMaxSpeed() As Integer
        If _speedHistory.Count = 0 Then Return 0
        Return _speedHistory.Max()
    End Function

    ''' <summary>
    ''' Maksimum sıcaklık değerini döndürür
    ''' </summary>
    Public Function GetMaxTemperature() As Integer
        If _tempHistory.Count = 0 Then Return 0
        Return _tempHistory.Max()
    End Function

    ''' <summary>
    ''' Ortalama RPM değerini döndürür
    ''' </summary>
    Public Function GetAverageRPM() As Double
        If _rpmHistory.Count = 0 Then Return 0
        Return _rpmHistory.Average()
    End Function

    ''' <summary>
    ''' Ortalama hız değerini döndürür
    ''' </summary>
    Public Function GetAverageSpeed() As Double
        If _speedHistory.Count = 0 Then Return 0
        Return _speedHistory.Average()
    End Function

#End Region

#Region "Sıfırlama"

    ''' <summary>
    ''' Tüm dashboard değerlerini ve geçmişi sıfırlar
    ''' </summary>
    Public Sub Reset()
        ' Güncel değerleri sıfırla
        _rpm = 0
        _speed = 0
        _temperature = 0
        _doorOpen = False
        _lightsOn = False

        ' Geçmiş listelerini temizle
        _rpmHistory.Clear()
        _speedHistory.Clear()
        _tempHistory.Clear()
        _rpmHistoryWithTime.Clear()
        _speedHistoryWithTime.Clear()
        _tempHistoryWithTime.Clear()

        ' Zaman damgalarını sıfırla
        _lastRpmUpdate = DateTime.MinValue
        _lastSpeedUpdate = DateTime.MinValue
        _lastTempUpdate = DateTime.MinValue
        _lastDoorUpdate = DateTime.MinValue
        _lastLightsUpdate = DateTime.MinValue
    End Sub

    ''' <summary>
    ''' Sadece geçmiş kayıtlarını temizler, güncel değerleri korur
    ''' </summary>
    Public Sub ClearHistory()
        _rpmHistory.Clear()
        _speedHistory.Clear()
        _tempHistory.Clear()
        _rpmHistoryWithTime.Clear()
        _speedHistoryWithTime.Clear()
        _tempHistoryWithTime.Clear()
    End Sub

#End Region

#Region "Durum Bilgisi"

    ''' <summary>
    ''' Dashboard durumunu okunabilir string olarak döndürür
    ''' </summary>
    Public Function GetStatusText() As String
        Dim sb As New System.Text.StringBuilder()

        sb.AppendLine($"RPM: {_rpm}")
        sb.AppendLine($"Hız: {_speed} km/h")
        sb.AppendLine($"Sıcaklık: {_temperature} °C")
        sb.AppendLine($"Kapı: {If(_doorOpen, "Açık", "Kapalı")}")
        sb.AppendLine($"Farlar: {If(_lightsOn, "Açık", "Kapalı")}")

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' ID'nin dashboard tarafından işlenip işlenmediğini kontrol eder
    ''' </summary>
    Public Function IsDashboardId(id As Integer) As Boolean
        Return id = CAN_ID_RPM OrElse
               id = CAN_ID_SPEED OrElse
               id = CAN_ID_TEMPERATURE OrElse
               id = CAN_ID_DOOR OrElse
               id = CAN_ID_LIGHTS
    End Function

#End Region

End Class

End Namespace

