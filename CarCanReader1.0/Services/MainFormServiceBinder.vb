Imports System.IO.Ports
Imports System.Windows.Forms

' Composes the domain services used by MainForm and wires their events to UI-safe callbacks.
' This keeps lifecycle and subscription logic outside the form while preserving existing behavior.
Public Class ServiceBindingContext
    Public Property InitializeRecordingButtons As Action
    Public Property SerialPort As SerialPort
    Public Property CanSender As CANSender
    Public Property DashboardManager As DashboardManager
    Public Property LearningEngine As LearningEngine
    Public Property FilterManager As FilterManager
    Public Property CommandRepository As CommandRepository
    Public Property AdvancedUdsEngine As AdvancedUdsEngine
    Public Property ObdService As OBDService
    Public Property VehicleProfileManager As VehicleProfileManager
    Public Property SafeAddLog As Action(Of String)
    Public Property SafeUpdateLabel As Action(Of Label, String)
    Public Property SafeUpdateProgressBar As Action(Of ProgressBar, Integer)
    Public Property SafeUpdatePanel As Action(Of Panel, Color)
    Public Property RpmLabel As Label
    Public Property SpeedLabel As Label
    Public Property TempLabel As Label
    Public Property DoorLabel As Label
    Public Property LightLabel As Label
    Public Property EngineLoadLabel As Label
    Public Property ThrottleLabel As Label
    Public Property IntakeTempLabel As Label
    Public Property AmbientTempLabel As Label
    Public Property FuelLevelLabel As Label
    Public Property MAPLabel As Label
    Public Property MAFLabel As Label
    Public Property ShortTrimLabel As Label
    Public Property LongTrimLabel As Label
    Public Property BarometricLabel As Label
    Public Property VoltageLabel As Label
    Public Property FuelRateLabel As Label
    Public Property RpmProgress As ProgressBar
    Public Property SpeedProgress As ProgressBar
    Public Property TempProgress As ProgressBar
    Public Property DoorIndicator As Panel
    Public Property LightIndicator As Panel
    Public Property AddRpmDataPoint As Action(Of Integer)
    Public Property AddSpeedDataPoint As Action(Of Integer)
    Public Property AddTempDataPoint As Action(Of Integer)
    Public Property OnLearningByteChange As Action(Of Integer, Integer, Byte, Byte)
    Public Property OnFilterChanged As Action(Of Boolean, String)
    Public Property OnFilterError As Action(Of String)
    Public Property OnPidUpdated As Action(Of Integer, Double)
    Public Property OnO2SensorUpdated As Action(Of Integer, Double)
    Public Property OnPendingDTC As Action(Of List(Of DTCInfo))
    Public Property OnStoredDTC As Action(Of List(Of DTCInfo))
    Public Property OnFreezeFrame As Action(Of String, Dictionary(Of Byte, Double))
    Public Property OnMode06 As Action(Of List(Of Mode06TestResult))
    Public Property OnVehicleInfoReceived As Action(Of VehicleInfo)
    Public Property OnAutoScanComplete As Action(Of AutoScanReport)
    Public Property OnDtcCleared As Action
    Public Property OnSessionChanged As Action
    Public Property OnCommandPredicted As Action(Of PredictedCommand)
    Public Property OnSignalClassified As Action(Of ClassifiedSignal)
    Public Property OnPatternDetected As Action(Of LearningPattern)
    Public Property AssignOnlineCarDbScraper As Action(Of OnlineCarDbScraper)
End Class

Public Class MainFormServiceBinder
    Private ReadOnly _context As ServiceBindingContext

    Public Sub New(context As ServiceBindingContext)
        _context = context
    End Sub

    Public Sub Initialize()
        Try
            _context.InitializeRecordingButtons?.Invoke()

            _context.CanSender.SetSerialPort(_context.SerialPort)

            AddHandler _context.CanSender.OnFrameSent, Sub(frame)
                                                           Try
                                                               _context.SafeAddLog?.Invoke("TX: " & frame)
                                                           Catch
                                                           End Try
                                                       End Sub
            AddHandler _context.CanSender.OnSendError, Sub(msg) _context.SafeAddLog?.Invoke("TX hata: " & msg)

            AddDashboardHandlers()

            AddHandler _context.LearningEngine.OnByteChange, Sub(id, index, oldVal, newVal)
                                                                 _context.OnLearningByteChange?.Invoke(id, index, oldVal, newVal)
                                                             End Sub

            AddHandler _context.FilterManager.OnFilterChanged, Sub(enabled, mode) _context.OnFilterChanged?.Invoke(enabled, mode)
            AddHandler _context.FilterManager.OnFilterError, Sub(msg) _context.OnFilterError?.Invoke(msg)

            AddHandler _context.CommandRepository.OnDataLoaded, Sub() _context.SafeAddLog?.Invoke("Komut verisi yüklendi.")
            AddHandler _context.CommandRepository.OnDataSaved, Sub() _context.SafeAddLog?.Invoke("Komut verisi kaydedildi.")
            AddHandler _context.CommandRepository.OnError, Sub(msg) _context.SafeAddLog?.Invoke("Komut repo hata: " & msg)

            _context.AdvancedUdsEngine.SetSender(_context.CanSender)
            AddHandler _context.AdvancedUdsEngine.OnResponse, Sub(response) _context.SafeAddLog?.Invoke("UDS: " & response.ToString())
            AddHandler _context.AdvancedUdsEngine.OnError, Sub(msg) _context.SafeAddLog?.Invoke("UDS hata: " & msg)
            AddHandler _context.AdvancedUdsEngine.OnSessionChanged, Sub(session)
                                                                        _context.SafeAddLog?.Invoke("UDS Oturum: " & session.ToString())
                                                                        _context.OnSessionChanged?.Invoke()
                                                                    End Sub

            _context.ObdService.SetSender(_context.CanSender)
            AddHandler _context.ObdService.OnPIDUpdated, Sub(pid, value) _context.OnPidUpdated?.Invoke(pid, value)
            AddHandler _context.ObdService.OnO2SensorUpdated, Sub(index, voltage) _context.OnO2SensorUpdated?.Invoke(index, voltage)
            AddHandler _context.ObdService.OnError, Sub(msg) _context.SafeAddLog?.Invoke("OBD hata: " & msg)

            AddHandler _context.ObdService.OnPendingDTCReceived, Sub(dtcs) _context.OnPendingDTC?.Invoke(dtcs)
            AddHandler _context.ObdService.OnStoredDTCReceived, Sub(dtcs) _context.OnStoredDTC?.Invoke(dtcs)
            AddHandler _context.ObdService.OnFreezeFrameReceived, Sub(dtc, data) _context.OnFreezeFrame?.Invoke(dtc, data)
            AddHandler _context.ObdService.OnMode06Received, Sub(tests) _context.OnMode06?.Invoke(tests)
            AddHandler _context.ObdService.OnVehicleInfoReceived, Sub(info) _context.OnVehicleInfoReceived?.Invoke(info)
            AddHandler _context.ObdService.OnAutoScanCompleted, Sub(report) _context.OnAutoScanComplete?.Invoke(report)
            AddHandler _context.ObdService.OnDTCCleared, Sub()
                                                             _context.OnDtcCleared?.Invoke()
                                                             _context.SafeAddLog?.Invoke("✅ DTC'ler temizlendi.")
                                                         End Sub

            AddObdDashboardHandlers()

            AddVehicleProfileHandlers()

            InitializeScraper()

            AddLearningEngineHandlers()
        Catch ex As Exception
            MessageBox.Show("Servis başlatma hatası: " & ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AddDashboardHandlers()
        AddHandler _context.DashboardManager.OnRPMChanged, Sub(v)
                                                               _context.SafeUpdateLabel?.Invoke(_context.RpmLabel, v.ToString())
                                                               _context.SafeUpdateProgressBar?.Invoke(_context.RpmProgress, v)
                                                               _context.AddRpmDataPoint?.Invoke(v)
                                                           End Sub
        AddHandler _context.DashboardManager.OnSpeedChanged, Sub(v)
                                                                 _context.SafeUpdateLabel?.Invoke(_context.SpeedLabel, v.ToString() & " km/h")
                                                                 _context.SafeUpdateProgressBar?.Invoke(_context.SpeedProgress, v)
                                                                 _context.AddSpeedDataPoint?.Invoke(v)
                                                             End Sub
        AddHandler _context.DashboardManager.OnTemperatureChanged, Sub(v)
                                                                       _context.SafeUpdateLabel?.Invoke(_context.TempLabel, v.ToString() & " °C")
                                                                       _context.SafeUpdateProgressBar?.Invoke(_context.TempProgress, v)
                                                                       _context.AddTempDataPoint?.Invoke(v)
                                                                   End Sub
        AddHandler _context.DashboardManager.OnDoorStateChanged, Sub(isOpen)
                                                                     _context.SafeUpdateLabel?.Invoke(_context.DoorLabel, If(isOpen, "Açık", "Kapalı"))
                                                                     _context.SafeUpdatePanel?.Invoke(_context.DoorIndicator, If(isOpen, Color.FromArgb(231, 76, 60), Color.FromArgb(46, 204, 113)))
                                                                 End Sub
        AddHandler _context.DashboardManager.OnLightStateChanged, Sub(isOn)
                                                                      _context.SafeUpdateLabel?.Invoke(_context.LightLabel, If(isOn, "Açık", "Kapalı"))
                                                                      _context.SafeUpdatePanel?.Invoke(_context.LightIndicator, If(isOn, Color.FromArgb(241, 196, 15), Color.Gray))
                                                                  End Sub
    End Sub

    Private Sub AddObdDashboardHandlers()
        AddHandler _context.DashboardManager.OnEngineLoadChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.EngineLoadLabel, v.ToString("F1") & " %")
        AddHandler _context.DashboardManager.OnThrottleChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.ThrottleLabel, v.ToString("F1") & " %")
        AddHandler _context.DashboardManager.OnIntakeTempChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.IntakeTempLabel, v.ToString() & " °C")
        AddHandler _context.DashboardManager.OnAmbientTempChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.AmbientTempLabel, v.ToString() & " °C")
        AddHandler _context.DashboardManager.OnFuelLevelChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.FuelLevelLabel, v.ToString("F1") & " %")
        AddHandler _context.DashboardManager.OnMAPChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.MAPLabel, v.ToString() & " kPa")
        AddHandler _context.DashboardManager.OnMAFChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.MAFLabel, v.ToString("F2") & " g/s"))
        AddHandler _context.DashboardManager.OnShortFuelTrimChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.ShortTrimLabel, v.ToString("F1") & " %"))
        AddHandler _context.DashboardManager.OnLongFuelTrimChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.LongTrimLabel, v.ToString("F1") & " %"))
        AddHandler _context.DashboardManager.OnBarometricChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.BarometricLabel, v.ToString() & " kPa"))
        AddHandler _context.DashboardManager.OnModuleVoltageChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.VoltageLabel, v.ToString("F2") & " V"))
        AddHandler _context.DashboardManager.OnFuelRateChanged, Sub(v) _context.SafeUpdateLabel?.Invoke(_context.FuelRateLabel, v.ToString("F2") & " L/h"))
    End Sub

    Private Sub AddVehicleProfileHandlers()
        AddHandler _context.VehicleProfileManager.OnProfilesLoaded, Sub()
                                                                        _context.SafeAddLog?.Invoke($"🚗 Araç profilleri yüklendi ({_context.VehicleProfileManager.GetProfileCount()} profil)")
                                                                    End Sub
        AddHandler _context.VehicleProfileManager.OnProfileSaved, Sub(vin)
                                                                      _context.SafeAddLog?.Invoke($"💾 Araç profili kaydedildi: {vin}")
                                                                  End Sub
        AddHandler _context.VehicleProfileManager.OnError, Sub(msg) _context.SafeAddLog?.Invoke("❌ Profil hata: " & msg)
    End Sub

    Private Sub InitializeScraper()
        Dim scraper = New OnlineCarDbScraper(_context.CommandRepository)
        AddHandler scraper.OnScrapeProgress, Sub(msg) _context.SafeAddLog?.Invoke("🌐 Scraper: " & msg)
        AddHandler scraper.OnScrapeCompleted, Sub(count)
                                                  _context.SafeAddLog?.Invoke($"✅ Scraping tamamlandı: {count} komut eklendi")
                                              End Sub
        AddHandler scraper.OnScrapeError, Sub(msg) _context.SafeAddLog?.Invoke("❌ Scraper hata: " & msg)
        AddHandler scraper.OnDbEnrichmentStarted, Sub() _context.SafeAddLog?.Invoke("📚 DB zenginleştirme başladı...")
        AddHandler scraper.OnDbEnrichmentCompleted, Sub(count)
                                                        _context.SafeAddLog?.Invoke($"✅ DB zenginleştirme tamamlandı: {count} komut eklendi")
                                                    End Sub
        _context.AssignOnlineCarDbScraper?.Invoke(scraper)
    End Sub

    Private Sub AddLearningEngineHandlers()
        AddHandler _context.LearningEngine.OnSignalClassifiedEx, Sub(signal) _context.OnSignalClassified?.Invoke(signal)
        AddHandler _context.LearningEngine.OnCommandPredicted, Sub(cmd) _context.OnCommandPredicted?.Invoke(cmd)
        AddHandler _context.LearningEngine.OnPatternDetected, Sub(pattern) _context.OnPatternDetected?.Invoke(pattern)
    End Sub
End Class
