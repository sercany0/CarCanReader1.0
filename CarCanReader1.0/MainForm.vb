Imports Newtonsoft.Json.Linq
Imports CarCanReader1._0.Services
Imports CarCanReader1._0.Forms
Imports CarCanReader1._0.Models
Imports CarCanReader1._0.Localization
Imports System.Threading
Imports System.Collections.Concurrent
Imports System.IO

Public Class MainForm

    ' ========================================
    ' SERVİS TANIMLARI
    ' ========================================
    Private canParser As New CANParser()
    Private canSender As New CANSender()
    Private filterManager As New FilterManager()
    Private dashboardManager As New DashboardManager()
    Private learningEngine As New LearningEngine()
    Private commandRepository As New CommandRepository()
    Private codingEngine As New CodingEngine()
    Private logAnalyzer As New LogAnalyzer()
    Private advancedUdsEngine As New AdvancedUdsEngine()
    Private obdService As New OBDService()
    Private WithEvents sessionRecorder As SessionRecorder = Nothing

    ' ========================================
    ' YENİ SERVİSLER (VIN Auto Profile & AI-Finder)
    ' ========================================
    Private vehicleProfileManager As New VehicleProfileManager()
    Private onlineCarDbScraper As OnlineCarDbScraper = Nothing ' Initialized in InitializeServices
    Private _vinAutoProfileEnabled As Boolean = True
    Private _currentVehicleProfile As VehicleProfile = Nothing
    Private _aiFinderEnabled As Boolean = False
    
    ' ========================================
    ' THREAD SAFETY & STABILITY
    ' ========================================
    Private _isInitialized As Boolean = False
    Private _isClosing As Boolean = False
    Private ReadOnly _uiLock As New Object()
    Private ReadOnly _logRateLimiter As New Services.RateLimiter(10) ' Log için 10ms minimum aralık
    
    ' ========================================
    ' CONFIGURATION MANAGER (Merkezi Ayarlar)
    ' ========================================
    Private ReadOnly Property Config As ConfigManager
        Get
            Return ConfigManager.Instance
        End Get
    End Property
    
    ' ========================================
    ' UI POLISH COMPONENTS
    ' ========================================
    Private WithEvents mainToolTip As New ToolTip()
    
    ' ========================================
    ' DTC LIST MANAGEMENT
    ' ========================================
    Private _allDTCs As New List(Of DTCInfo)()
    Private _filteredDTCs As New List(Of DTCInfo)()

    ' ========================================
    ' CONNECTION MONITORING
    ' ========================================
    Private _lastDataReceivedTime As DateTime = DateTime.MinValue
    Private _connectionMonitorTimer As System.Windows.Forms.Timer = Nothing

    ' ========================================
    ' SESSION RECORDING
    ' ========================================
    Private _isRecording As Boolean = False
    Private _recordingStartTime As DateTime
    Private _recordingBlinkTimer As System.Windows.Forms.Timer = Nothing
    Private _recentSessions As New List(Of String)()

    ' ========================================
    ' MEVCUT DEĞİŞKENLER
    ' ========================================
    ' (Eski diff/filter değişkenleri kaldırıldı - artık servisler kullanılıyor)

    ' ========================================
    ' THREAD-SAFE UI YARDIMCILARI
    ' ========================================
    
    ''' <summary>
    ''' Label'ı thread-safe günceller
    ''' </summary>
    Private Sub SafeUpdateLabel(lbl As Label, text As String)
        If _isClosing Then Return
        If lbl Is Nothing OrElse lbl.IsDisposed Then Return
        
        Try
            If lbl.InvokeRequired Then
                lbl.BeginInvoke(Sub()
                                    If Not lbl.IsDisposed Then lbl.Text = text
                                End Sub)
            Else
                lbl.Text = text
            End If
        Catch ex As ObjectDisposedException
            ' Ignore
        Catch ex As InvalidOperationException
            ' Ignore
        End Try
    End Sub

    ''' <summary>
    ''' ListBox'a thread-safe item ekler
    ''' </summary>
    Private Sub SafeAddListItem(lst As ListBox, item As String)
        If _isClosing Then Return
        If lst Is Nothing OrElse lst.IsDisposed Then Return
        
        Try
            If lst.InvokeRequired Then
                lst.BeginInvoke(Sub()
                                    If Not lst.IsDisposed Then lst.Items.Add(item)
                                End Sub)
            Else
                lst.Items.Add(item)
            End If
        Catch ex As ObjectDisposedException
        Catch ex As InvalidOperationException
        End Try
    End Sub

    ''' <summary>
    ''' Thread-safe log yazımı
    ''' </summary>
    Private Sub SafeAddLog(message As String)
        If _isClosing Then Return
        If lstLog Is Nothing OrElse lstLog.IsDisposed Then Return
        
        Try
            If lstLog.InvokeRequired Then
                lstLog.BeginInvoke(Sub()
                                       If Not lstLog.IsDisposed Then AddLog(message)
                                   End Sub)
            Else
                AddLog(message)
            End If
        Catch ex As ObjectDisposedException
        Catch ex As InvalidOperationException
        End Try
    End Sub

    ''' <summary>
    ''' Thread-safe ProgressBar güncellemesi
    ''' </summary>
    Private Sub SafeUpdateProgressBar(prg As ProgressBar, value As Integer)
        If _isClosing Then Return
        If prg Is Nothing OrElse prg.IsDisposed Then Return
        
        Try
            Dim clampedValue = Math.Max(prg.Minimum, Math.Min(prg.Maximum, value))
            If prg.InvokeRequired Then
                prg.BeginInvoke(Sub()
                                    If Not prg.IsDisposed Then prg.Value = clampedValue
                                End Sub)
            Else
                prg.Value = clampedValue
            End If
        Catch ex As ObjectDisposedException
        Catch ex As InvalidOperationException
        End Try
    End Sub

    ''' <summary>
    ''' Thread-safe Panel güncellemesi
    ''' </summary>
    Private Sub SafeUpdatePanel(pnl As Panel, color As Color)
        If _isClosing Then Return
        If pnl Is Nothing OrElse pnl.IsDisposed Then Return
        
        Try
            If pnl.InvokeRequired Then
                pnl.BeginInvoke(Sub()
                                    If Not pnl.IsDisposed Then pnl.BackColor = color
                                End Sub)
            Else
                pnl.BackColor = color
            End If
        Catch ex As ObjectDisposedException
        Catch ex As InvalidOperationException
        End Try
    End Sub

    ' NOTE: EnsureJsonExists removed - CommandRepository handles this automatically
    ' with unified /data/ folder and atomic writes

    Private Sub btnTestLog_Click(sender As Object, e As EventArgs) Handles btnTestLog.Click
        AddLog("RX: t1238AABBCC")
        AddLog("RX: t4501FF")
        AddLog("RX: t3211000A")
        AddLog("RX: t1004DEADBEEF")
    End Sub




    ' Uygulama açıldığında çalışacak
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            ' Servisleri başlat
            InitializeServices()

            ' ConfigManager yükle (singleton, ilk erişimde yüklenir)
            Config.EnsureAllFolders()
            AddHandler Config.OnConfigChanged, AddressOf HandleConfigChanged
            
            ' JSON veritabanlarını yükle (CommandRepository ve VehicleProfileManager)
            ' Bu servisler unified /data/ klasörünü kullanır
            commandRepository.Load()
            vehicleProfileManager.LoadProfiles()
            
            ' DTC veritabanını yükle
            DTCDatabase.Instance.LoadDatabase()
            AddLog($"📋 DTC veritabanı yüklendi: {DTCDatabase.Instance.TotalCount} kod")
            
            ' DTC DataGridView kolonlarını ayarla
            InitializeDTCGrid()
            
            ' Grafikleri başlat
            InitializeCharts()
            
            ' ErrorHandler event'lerine bağlan
            AddHandler ErrorHandler.Instance.OnErrorLogged, AddressOf HandleGlobalError
            AddHandler ErrorHandler.Instance.OnCriticalError, AddressOf HandleCriticalError
            
            ' F11/F12 kısayolları için KeyPreview aktif
            Me.KeyPreview = True
            
            ' Session recorder başlat
            sessionRecorder = New SessionRecorder()
            AddHandler sessionRecorder.OnRecordingStarted, AddressOf HandleRecordingStarted
            AddHandler sessionRecorder.OnRecordingStopped, AddressOf HandleRecordingStopped
            AddHandler sessionRecorder.OnFrameRecorded, AddressOf HandleFrameRecorded
            
            ' Recording blink timer
            _recordingBlinkTimer = New System.Windows.Forms.Timer()
            _recordingBlinkTimer.Interval = 500 ' 500ms blink
            AddHandler _recordingBlinkTimer.Tick, AddressOf RecordingBlinkTimer_Tick
            
            ' Son kayıtları yükle
            LoadRecentSessions()
            
            ' LocalizationManager'ı başlat
            ' Instance'a erişerek yüklenmesini garanti et
            Dim locMgr = LocalizationManager.Instance
            AddHandler locMgr.OnLanguageChanged, AddressOf HandleLanguageChanged
            
            lblStatus.Text = "⚪ Durum: Bağlı değil"
            lblDiffStatus.Text = "⚪ Takip Durumu: Kapalı"
            AddLog("🚀 CarCANReader v1.0 başlatıldı.")
            AddLog("📋 CAN bağlantısı için 'Bağlan' butonuna tıklayın.")
            AddLog($"📂 Veri klasörü: {CommandRepository.GetDataFolderPath()}")
            AddLog("💡 İpucu: F11=Ayarlar, F12=Hata Logları")
            
            ' UI'yı repository'den güncelle
            RefreshCommandsUI()
            
            ' UI Polish: ToolTips ve Görsel İyileştirmeler
            InitializeUIPolish()
            
            ' UI string'lerini güncelle (tüm kontroller oluşturulduktan sonra)
            UpdateUIStrings()
            
            ' F1 tuşu ile yardım
            AddHandler Me.KeyDown, AddressOf MainForm_KeyDown
            If mnuHelpViewer IsNot Nothing Then
                AddHandler mnuHelpViewer.Click, AddressOf MnuHelpViewer_Click
            End If
            If mnuToolsEcuScanner IsNot Nothing Then
                AddHandler mnuToolsEcuScanner.Click, AddressOf MnuToolsEcuScanner_Click
            End If
            If mnuToolsReport IsNot Nothing Then
                AddHandler mnuToolsReport.Click, AddressOf MnuToolsReport_Click
            End If
            If mnuToolsSessionPlayer IsNot Nothing Then
                AddHandler mnuToolsSessionPlayer.Click, AddressOf MnuToolsSessionPlayer_Click
            End If
            
            ' Bağlantı kontrolü ve wizard
            CheckAndShowWizardIfNeeded()
            
            ' Bağlantı izleme timer'ını başlat
            InitializeConnectionMonitor()
            
            _isInitialized = True
            
        Catch ex As Exception
            ErrorHandler.Instance.LogCritical(ex, "MainForm.Load")
            MessageBox.Show("Uygulama başlatma hatası: " & ex.Message & vbCrLf & vbCrLf & "Detay: " & ex.StackTrace,
                           "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    ''' <summary>
    ''' Kısayol tuşları: F11=Ayarlar, F12=Hata Logları
    ''' </summary>
    Private Sub MainForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.F1
                ShowHelp(GetContextHelpTopic())
                e.Handled = True
            Case Keys.F12
                ShowErrorLogViewer()
                e.Handled = True
            Case Keys.F11
                ShowSettingsForm()
                e.Handled = True
        End Select
    End Sub
    
    ''' <summary>
    ''' Ayarlar penceresini açar
    ''' </summary>
    Private Sub ShowSettingsForm()
        Try
            Using settings As New SettingsForm()
                settings.ShowDialog(Me)
            End Using
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.ShowSettingsForm")
        End Try
    End Sub
    
    ''' <summary>
    ''' Config değiştiğinde çağrılır
    ''' </summary>
    Private Sub HandleConfigChanged(section As String)
        Try
            SafeAddLog($"⚙️ Ayar değişti: {section}")
            
            ' OBD ayarları değiştiyse
            If section = "obd" Then
                obdService.SetPollingInterval(Config.OBD.PollingInterval)
            End If
            
            ' UI ayarları değiştiyse - gelecekte kullanılabilir
            ' If section = "ui" Then
            '     ' Dashboard veya diğer UI bileşenleri güncellenebilir
            ' End If
            
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.HandleConfigChanged")
        End Try
    End Sub
    
    ''' <summary>
    ''' Hata log görüntüleyiciyi açar
    ''' </summary>
    Private Sub ShowErrorLogViewer()
        Try
            Using viewer As New ErrorLogViewer()
                viewer.ShowDialog(Me)
            End Using
        Catch ex As Exception
            Debug.WriteLine($"ShowErrorLogViewer error: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Global hata yakalandığında çağrılır
    ''' </summary>
    Private Sub HandleGlobalError(entry As ErrorLogEntry)
        If _isClosing Then Return
        
        ' StatusBar'da son hatayı göster
        SafeInvoke(Sub()
                       If lblStatus IsNot Nothing Then
                           lblStatus.Text = $"⚠️ Son Hata: {entry.UserFriendlyMessage}"
                           lblStatus.ForeColor = Color.Orange
                       End If
                   End Sub)
    End Sub
    
    ''' <summary>
    ''' Kritik hata yakalandığında çağrılır
    ''' </summary>
    Private Sub HandleCriticalError(entry As ErrorLogEntry)
        If _isClosing Then Return
        
        ' Kritik hata popup göster
        SafeInvoke(Sub()
                       MessageBox.Show(
                           entry.UserFriendlyMessage & vbCrLf & vbCrLf & 
                           "Teknik detaylar için F12 tuşuna basın.",
                           "Kritik Hata",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error)
                   End Sub)
    End Sub
    
    ''' <summary>
    ''' Form kapanırken kaynakları düzgün temizler
    ''' </summary>
    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            _isClosing = True
            
            ' ErrorHandler event'lerini kaldır
            Try
                RemoveHandler ErrorHandler.Instance.OnErrorLogged, AddressOf HandleGlobalError
                RemoveHandler ErrorHandler.Instance.OnCriticalError, AddressOf HandleCriticalError
            Catch
            End Try
            
            ' OBD polling'i durdur
            If obdService IsNot Nothing Then
                obdService.StopPolling()
            End If
            
            ' Learning Engine'i durdur
            If learningEngine IsNot Nothing AndAlso learningEngine.IsRunning Then
                learningEngine.Stop()
            End If
            
            ' SerialPort'u kapat
            If SerialPort1 IsNot Nothing AndAlso SerialPort1.IsOpen Then
                Try
                    SerialPort1.Close()
                Catch closeEx As Exception
                    Debug.WriteLine($"Port kapatma hatası: {closeEx.Message}")
                End Try
            End If
            
            ' Kaydedilmemiş değişiklikleri kontrol et
            If commandRepository IsNot Nothing AndAlso commandRepository.HasUnsavedChanges Then
                Dim result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?",
                                            "Kaydedilmemiş Değişiklikler",
                                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)
                If result = DialogResult.Yes Then
                    commandRepository.Save()
                ElseIf result = DialogResult.Cancel Then
                    e.Cancel = True
                    _isClosing = False
                    Return
                End If
            End If
            
            AddLog("👋 Uygulama kapatılıyor...")
            
        Catch ex As Exception
            ' Kapatma sırasında hata - sessizce devam et
            Debug.WriteLine("Form closing error: " & ex.Message)
        End Try
    End Sub

    ' ========================================
    ' UI POLISH: TOOLTIPS & VISUAL IMPROVEMENTS
    ' ========================================
    
    ''' <summary>
    ''' Initializes tooltips and visual polish for a modern, professional UI
    ''' </summary>
    Private Sub InitializeUIPolish()
        Try
            ' Configure ToolTip properties for better UX
            mainToolTip.AutoPopDelay = 8000
            mainToolTip.InitialDelay = 500
            mainToolTip.ReshowDelay = 200
            mainToolTip.ShowAlways = True
            mainToolTip.IsBalloon = False
            mainToolTip.BackColor = Color.FromArgb(45, 45, 60)
            mainToolTip.ForeColor = Color.White
            
            ' ========================================
            ' CONNECTION TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnConnect, "CANable Pro cihazına bağlan." & vbCrLf & "Otomatik olarak uygun COM portunu bulur.")
            mainToolTip.SetToolTip(btnSend, "Girilen CAN ID ve Data'yı araç bus'a gönder." & vbCrLf & "Format: ID=3 hex, Data=max 16 hex")
            mainToolTip.SetToolTip(btnSaveLog, "Mevcut log kayıtlarını bir .txt dosyasına kaydet.")
            mainToolTip.SetToolTip(btnClearLog, "Tüm log kayıtlarını temizle.")
            mainToolTip.SetToolTip(btnTestLog, "Test amaçlı örnek CAN frame'leri ekle.")
            mainToolTip.SetToolTip(btnApplyFilter, "Seçilen filtreyi aktif et." & vbCrLf & "Sadece eşleşen ID'ler gösterilir.")
            mainToolTip.SetToolTip(btnClearFilter, "Tüm filtreleri kaldır ve tüm ID'leri göster.")
            mainToolTip.SetToolTip(cmbFilterMode, "Filtre modu seçin:" & vbCrLf & "• Tek ID göster/gizle" & vbCrLf & "• Aralık göster/gizle")
            
            ' ========================================
            ' DASHBOARD TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnStartOBD, "OBD-II PID polling başlat." & vbCrLf & "Araç verilerini periyodik olarak okur.")
            mainToolTip.SetToolTip(btnStopOBD, "OBD-II polling durdur.")
            mainToolTip.SetToolTip(prgRPM, "Motor devri (RPM) göstergesi" & vbCrLf & "Max: 8000 RPM")
            mainToolTip.SetToolTip(prgSpeed, "Araç hızı göstergesi" & vbCrLf & "Max: 260 km/h")
            mainToolTip.SetToolTip(prgTemp, "Motor sıcaklığı göstergesi" & vbCrLf & "Max: 150°C")
            
            ' ========================================
            ' DIAGNOSTICS TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnReadVehicleInfo, "Araç bilgilerini oku (Mode 09):" & vbCrLf & "VIN, Calibration ID, CVN, ECU Name")
            mainToolTip.SetToolTip(btnReadPendingDTC, "Bekleyen arıza kodlarını oku (Mode 07)." & vbCrLf & "MIL yanmamış geçici arızalar.")
            mainToolTip.SetToolTip(btnClearDTC, "Tüm arıza kodlarını temizle (Mode 04)." & vbCrLf & "⚠️ Dikkat: MIL lambası sıfırlanır!")
            mainToolTip.SetToolTip(btnReadFreezeFrame, "Freeze Frame verisi oku (Mode 02)." & vbCrLf & "Arıza anındaki anlık değerler.")
            mainToolTip.SetToolTip(btnReadMode06, "On-Board Monitoring test sonuçları (Mode 06).")
            mainToolTip.SetToolTip(btnAutoScanPIDs, "Desteklenen tüm PID'leri tarar ve listeler.")
            mainToolTip.SetToolTip(btnEnrichDatabase, "Online kaynaklardan veritabanını zenginleştir." & vbCrLf & "OpenDBC, NHTSA API vb. kullanır.")
            
            ' ========================================
            ' VIN PROFILE TOOLTIPS
            ' ========================================
            If chkVINAutoProfile IsNot Nothing Then
                mainToolTip.SetToolTip(chkVINAutoProfile, "VIN Otomatik Profil Yükleme" & vbCrLf & _
                    "✓ Aktif: Araç bağlandığında VIN okunur ve profil otomatik yüklenir" & vbCrLf & _
                    "✗ Pasif: Manuel profil yönetimi")
            End If
            If btnLoadVINProfile IsNot Nothing Then
                mainToolTip.SetToolTip(btnLoadVINProfile, "Kayıtlı araç profilini yükle.")
            End If
            If btnSaveVINProfile IsNot Nothing Then
                mainToolTip.SetToolTip(btnSaveVINProfile, "Mevcut araç profilini kaydet.")
            End If
            
            ' ========================================
            ' COMMANDS TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnSendCommand, "Seçili komutu araç bus'a gönder." & vbCrLf & "Komut JSON veritabanından alınır.")
            mainToolTip.SetToolTip(cmbBrand, "Araç markası seçin.")
            mainToolTip.SetToolTip(cmbModel, "Araç modeli seçin.")
            mainToolTip.SetToolTip(cmbModule, "Hedef ECU modülünü seçin.")
            mainToolTip.SetToolTip(cmbCommand, "Göndermek istediğiniz komutu seçin.")
            
            ' ========================================
            ' CODING TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnCodingOn, "Toggle komutu için ON state gönder.")
            mainToolTip.SetToolTip(btnCodingOff, "Toggle komutu için OFF state gönder.")
            mainToolTip.SetToolTip(btnCodeSend, "Manuel CAN frame gönder (Coding).")
            mainToolTip.SetToolTip(btnCodeSaveFromLog, "Log'dan seçili frame'i kaydet.")
            
            ' ========================================
            ' LEARNING TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnStartDiff, "Byte değişim takibini başlat." & vbCrLf & "Araçtaki değişiklikleri otomatik algılar.")
            mainToolTip.SetToolTip(btnStopDiff, "Byte takibini durdur.")
            mainToolTip.SetToolTip(btnExportLearning, "Öğrenilen verileri dışa aktar.")
            mainToolTip.SetToolTip(btnClearLearning, "Öğrenme verilerini temizle.")
            
            ' ========================================
            ' AI-FINDER TOOLTIPS
            ' ========================================
            If chkAIFinderEnabled IsNot Nothing Then
                mainToolTip.SetToolTip(chkAIFinderEnabled, "AI-Finder Otomatik Keşif Sistemi" & vbCrLf & _
                    "✓ Aktif: Pattern analizi, sinyal sınıflandırma, komut tahmini" & vbCrLf & _
                    "✗ Pasif: Sadece manuel learning mode")
            End If
            mainToolTip.SetToolTip(btnStartAIFinder, "AI-Finder analiz motorunu başlat." & vbCrLf & "Otomatik pattern ve komut keşfi yapar.")
            mainToolTip.SetToolTip(btnStopAIFinder, "AI-Finder analizi durdur.")
            mainToolTip.SetToolTip(btnAIFinderSaveAll, "Bulunan tüm komutları veritabanına kaydet.")
            mainToolTip.SetToolTip(btnSnapshotBefore, "Karşılaştırma için 'Önce' snapshot al." & vbCrLf & "Bir işlem yapmadan önce tıklayın.")
            mainToolTip.SetToolTip(btnSnapshotAfter, "Karşılaştırma için 'Sonra' snapshot al." & vbCrLf & "İşlemi yaptıktan sonra tıklayın.")
            mainToolTip.SetToolTip(btnAutoDiff, "Before/After snapshot'ları karşılaştır." & vbCrLf & "Değişen byte'ları otomatik tespit eder.")
            mainToolTip.SetToolTip(cmbAIFinderCategory, "Sonuçları kategoriye göre filtrele.")
            
            ' ========================================
            ' LOG ANALYSIS TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnLoadLogFile, "Önceden kaydedilmiş log dosyası yükle.")
            mainToolTip.SetToolTip(btnAnalyzeLog, "Mevcut log'u analiz et." & vbCrLf & "ID frekansları, pattern'ler, busy ID'ler.")
            
            ' ========================================
            ' JSON EDITOR TAB TOOLTIPS
            ' ========================================
            mainToolTip.SetToolTip(btnAddCommand, "Yeni komut ekle.")
            mainToolTip.SetToolTip(btnUpdateCommand, "Seçili komutu güncelle.")
            mainToolTip.SetToolTip(btnDeleteCommand, "Seçili komutu sil.")
            mainToolTip.SetToolTip(btnSaveJson, "Tüm değişiklikleri JSON dosyasına kaydet.")
            mainToolTip.SetToolTip(btnNewBrand, "Yeni marka ekle.")
            mainToolTip.SetToolTip(btnNewModel, "Yeni model ekle.")
            mainToolTip.SetToolTip(btnNewModule, "Yeni modül ekle.")
            
            AddLog("✨ UI polish uygulandı (tooltips aktif)")
            
        Catch ex As Exception
            Debug.WriteLine($"UI Polish initialization error: {ex.Message}")
        End Try
    End Sub
    
    ' ========================================
    ' SERVİS BAŞLATMA VE OLAY BAĞLAMA
    ' ========================================
    Private Sub InitializeServices()
        Try
            ' Recording butonlarını oluştur (Designer'da yoksa)
            InitializeRecordingButtons()
            
            ' CANSender'a SerialPort bağla
            canSender.SetSerialPort(SerialPort1)

            ' CANSender olayları
            AddHandler canSender.OnFrameSent, Sub(frame)
                                                  Try
                                                      AddLog("TX: " & frame)
                                                  Catch : End Try
                                              End Sub
            AddHandler canSender.OnSendError, Sub(msg) SafeAddLog("TX hata: " & msg)

            ' DashboardManager olayları (thread-safe)
            AddHandler dashboardManager.OnRPMChanged, Sub(v)
                                                          SafeUpdateLabel(lblRPMValue, v.ToString())
                                                          SafeUpdateProgressBar(prgRPM, v)
                                                          ' Grafiğe ekle
                                                          If liveChartRPM IsNot Nothing Then
                                                              liveChartRPM.AddDataPoint(v)
                                                          End If
                                                      End Sub
            AddHandler dashboardManager.OnSpeedChanged, Sub(v)
                                                            SafeUpdateLabel(lblSpeedValue, v.ToString() & " km/h")
                                                            SafeUpdateProgressBar(prgSpeed, v)
                                                            ' Grafiğe ekle
                                                            If liveChartSpeed IsNot Nothing Then
                                                                liveChartSpeed.AddDataPoint(v)
                                                            End If
                                                        End Sub
            AddHandler dashboardManager.OnTemperatureChanged, Sub(v)
                                                                  SafeUpdateLabel(lblTempValue, v.ToString() & " °C")
                                                                  SafeUpdateProgressBar(prgTemp, v)
                                                                  ' Grafiğe ekle
                                                                  If liveChartTemp IsNot Nothing Then
                                                                      liveChartTemp.AddDataPoint(v)
                                                                  End If
                                                              End Sub
            AddHandler dashboardManager.OnDoorStateChanged, Sub(isOpen)
                                                                SafeUpdateLabel(lblDoorValue, If(isOpen, "Açık", "Kapalı"))
                                                                SafeUpdatePanel(pnlDoorIndicator, If(isOpen, Color.FromArgb(231, 76, 60), Color.FromArgb(46, 204, 113)))
                                                            End Sub
            AddHandler dashboardManager.OnLightStateChanged, Sub(isOn)
                                                                 SafeUpdateLabel(lblLightValue, If(isOn, "Açık", "Kapalı"))
                                                                 SafeUpdatePanel(pnlLightIndicator, If(isOn, Color.FromArgb(241, 196, 15), Color.Gray))
                                                             End Sub

            ' LearningEngine olayları
            AddHandler learningEngine.OnByteChange, AddressOf HandleLearningByteChange

            ' FilterManager olayları
            AddHandler filterManager.OnFilterChanged, Sub(enabled, mode) SafeAddLog("Filtre: " & filterManager.GetStatusText())
            AddHandler filterManager.OnFilterError, Sub(msg) SafeAddLog("Filtre hata: " & msg)

            ' CommandRepository olayları
            AddHandler commandRepository.OnDataLoaded, Sub() SafeAddLog("Komut verisi yüklendi.")
            AddHandler commandRepository.OnDataSaved, Sub() SafeAddLog("Komut verisi kaydedildi.")
            AddHandler commandRepository.OnError, Sub(msg) SafeAddLog("Komut repo hata: " & msg)

            ' AdvancedUdsEngine kurulumu
            advancedUdsEngine.SetSender(canSender)
            AddHandler advancedUdsEngine.OnResponse, Sub(response)
                                                         SafeAddLog("UDS: " & response.ToString())
                                                     End Sub
            AddHandler advancedUdsEngine.OnError, Sub(msg) SafeAddLog("UDS hata: " & msg)
            AddHandler advancedUdsEngine.OnSessionChanged, Sub(session)
                                                               SafeAddLog("UDS Oturum: " & session.ToString())
                                                           End Sub

            ' OBDService kurulumu
            obdService.SetSender(canSender)
            AddHandler obdService.OnPIDUpdated, AddressOf HandleOBDPIDUpdated
            AddHandler obdService.OnO2SensorUpdated, AddressOf HandleO2SensorUpdated
            AddHandler obdService.OnError, Sub(msg) SafeAddLog("OBD hata: " & msg)

            ' OBD Advanced Mode olayları (Mode 02-09)
            AddHandler obdService.OnPendingDTCReceived, AddressOf HandlePendingDTC
            AddHandler obdService.OnStoredDTCReceived, AddressOf HandleStoredDTC
            AddHandler obdService.OnFreezeFrameReceived, AddressOf HandleFreezeFrame
            AddHandler obdService.OnMode06Received, AddressOf HandleMode06
            AddHandler obdService.OnVehicleInfoReceived, AddressOf HandleVehicleInfo
            AddHandler obdService.OnAutoScanCompleted, AddressOf HandleAutoScanComplete
            AddHandler obdService.OnDTCCleared, Sub()
                                                   _allDTCs.Clear()
                                                   ApplyDTCFilter()
                                                   SafeAddLog("✅ DTC'ler temizlendi.")
                                               End Sub

            ' DashboardManager OBD olayları
            AddHandler dashboardManager.OnEngineLoadChanged, Sub(v) SafeUpdateLabel(lblEngineLoadValue, v.ToString("F1") & " %")
            AddHandler dashboardManager.OnThrottleChanged, Sub(v) SafeUpdateLabel(lblThrottleValue, v.ToString("F1") & " %")
            AddHandler dashboardManager.OnIntakeTempChanged, Sub(v) SafeUpdateLabel(lblIntakeTempValue, v.ToString() & " °C")
            AddHandler dashboardManager.OnAmbientTempChanged, Sub(v) SafeUpdateLabel(lblAmbientTempValue, v.ToString() & " °C")
            AddHandler dashboardManager.OnFuelLevelChanged, Sub(v) SafeUpdateLabel(lblFuelLevelValue, v.ToString("F1") & " %")
            AddHandler dashboardManager.OnMAPChanged, Sub(v) SafeUpdateLabel(lblMAPValue, v.ToString() & " kPa")
            AddHandler dashboardManager.OnMAFChanged, Sub(v) SafeUpdateLabel(lblMAFValue, v.ToString("F2") & " g/s")
            AddHandler dashboardManager.OnShortFuelTrimChanged, Sub(v) SafeUpdateLabel(lblShortTrimValue, v.ToString("F1") & " %")
            AddHandler dashboardManager.OnLongFuelTrimChanged, Sub(v) SafeUpdateLabel(lblLongTrimValue, v.ToString("F1") & " %")
            AddHandler dashboardManager.OnBarometricChanged, Sub(v) SafeUpdateLabel(lblBarometricValue, v.ToString() & " kPa")
            AddHandler dashboardManager.OnModuleVoltageChanged, Sub(v) SafeUpdateLabel(lblVoltageValue, v.ToString("F2") & " V")
            AddHandler dashboardManager.OnFuelRateChanged, Sub(v) SafeUpdateLabel(lblFuelRateValue, v.ToString("F2") & " L/h")

            ' ========================================
            ' VIN AUTO PROFILE SERVİSİ KURULUMU
            ' ========================================
            AddHandler vehicleProfileManager.OnProfilesLoaded, Sub()
                                                                   SafeAddLog($"🚗 Araç profilleri yüklendi ({vehicleProfileManager.GetProfileCount()} profil)")
                                                               End Sub
            AddHandler vehicleProfileManager.OnProfileSaved, Sub(vin)
                                                                 SafeAddLog($"💾 Araç profili kaydedildi: {vin}")
                                                             End Sub
            AddHandler vehicleProfileManager.OnError, Sub(msg) SafeAddLog("❌ Profil hata: " & msg)

            ' ========================================
            ' ONLINE CAR DB SCRAPER KURULUMU
            ' ========================================
            onlineCarDbScraper = New OnlineCarDbScraper(commandRepository)
            AddHandler onlineCarDbScraper.OnScrapeProgress, Sub(msg) SafeAddLog("🌐 Scraper: " & msg)
            AddHandler onlineCarDbScraper.OnScrapeCompleted, Sub(count)
                                                                 SafeAddLog($"✅ Scraping tamamlandı: {count} komut eklendi")
                                                             End Sub
            AddHandler onlineCarDbScraper.OnScrapeError, Sub(msg) SafeAddLog("❌ Scraper hata: " & msg)
            AddHandler onlineCarDbScraper.OnDbEnrichmentStarted, Sub() SafeAddLog("📚 DB zenginleştirme başladı...")
            AddHandler onlineCarDbScraper.OnDbEnrichmentCompleted, Sub(count)
                                                                       SafeAddLog($"✅ DB zenginleştirme tamamlandı: {count} komut eklendi")
                                                                   End Sub

            ' ========================================
            ' AI-FINDER (LEARNING ENGINE) KURULUMU
            ' ========================================
            AddHandler learningEngine.OnSignalClassifiedEx, AddressOf HandleAIFinderSignalClassified
            AddHandler learningEngine.OnCommandPredicted, AddressOf HandleAIFinderCommandPredicted
            AddHandler learningEngine.OnPatternDetected, AddressOf HandleAIFinderPatternDetected

        Catch ex As Exception
            ' Kritik hata - servisleri başlatamadık
            MessageBox.Show("Servis başlatma hatası: " & ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' LearningEngine byte değişikliği olayı
    Private Sub HandleLearningByteChange(id As Integer, byteIndex As Integer, oldValue As Byte, newValue As Byte)
        Dim line As String = $"ID 0x{id:X3} Byte[{byteIndex}] {oldValue:X2} → {newValue:X2}"
        SafeAddListItem(lstDiff, line)
    End Sub

    ' ========================================
    ' AI-FINDER EVENT HANDLERLARI
    ' ========================================

    ''' <summary>
    ''' AI-Finder sinyal sınıflandırıldığında çağrılır
    ''' </summary>
    Private Sub HandleAIFinderSignalClassified(signal As ClassifiedSignal)
        If _isClosing Then Return
        
        Try
            ' Null check
            If signal Is Nothing Then
                SafeAddLog("⚠️ AI-Finder: Null signal received")
                Return
            End If
            
            Dim line As String = $"🏷️ ID 0x{signal.CanId:X3}[{signal.ByteIndex}] → {signal.SignalType} ({signal.Confidence:P0})"
            SafeAddListItem(lstAIFinderResults, line)

            ' Otomatik kaydetme aktifse
            If _aiFinderEnabled AndAlso signal.Confidence > 0.8 Then
                SafeAddLog($"🧠 AI-Finder: Yüksek güvenilirlikli sinyal bulundu - {signal.SignalType}")
            End If
        Catch ex As Exception
            Debug.WriteLine($"AI-Finder signal error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder komut tahmini yaptığında çağrılır
    ''' </summary>
    Private Sub HandleAIFinderCommandPredicted(prediction As CommandPrediction)
        If _isClosing Then Return
        
        Try
            ' Null check
            If prediction Is Nothing Then
                SafeAddLog("⚠️ AI-Finder: Null prediction received")
                Return
            End If
            
            Dim predictedName As String = If(String.IsNullOrEmpty(prediction.PredictedName), "Unknown", prediction.PredictedName)
            Dim category As String = If(String.IsNullOrEmpty(prediction.Category), "Unknown", prediction.Category)
            
            Dim line As String = $"💡 {predictedName} (ID: 0x{prediction.CanId:X3}) - {category} ({prediction.Confidence:P0})"
            SafeAddListItem(lstAIFinderResults, line)

            If _aiFinderEnabled AndAlso prediction.Confidence > 0.85 Then
                SafeAddLog($"🎯 AI-Finder: Yeni komut tahmini - {predictedName}")
            End If
        Catch ex As Exception
            Debug.WriteLine($"AI-Finder prediction error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder pattern tespit ettiğinde çağrılır
    ''' </summary>
    Private Sub HandleAIFinderPatternDetected(pattern As DetectedPattern)
        If _isClosing Then Return
        
        Try
            ' Null check
            If pattern Is Nothing Then
                SafeAddLog("⚠️ AI-Finder: Null pattern received")
                Return
            End If
            
            Dim patternType As String = If(String.IsNullOrEmpty(pattern.PatternType), "Unknown", pattern.PatternType)
            Dim line As String = $"🔄 Pattern: {patternType} @ ID 0x{pattern.CanId:X3} ({pattern.Confidence:P0})"
            SafeAddListItem(lstAIFinderResults, line)

            If _aiFinderEnabled Then
                SafeAddLog($"📊 AI-Finder: Pattern tespit edildi - {patternType}")
            End If
        Catch ex As Exception
            Debug.WriteLine($"AI-Finder pattern error: {ex.Message}")
        End Try
    End Sub

    ' ========================================
    ' OBD-II EVENT HANDLERLARI
    ' ========================================

    ''' <summary>
    ''' OBD PID değeri güncellendiğinde çağrılır
    ''' </summary>
    Private Sub HandleOBDPIDUpdated(pid As Byte, value As Double, unit As String)
        Try
            ' PID'e göre DashboardManager'ı güncelle
            Select Case pid
                Case OBDService.PID_ENGINE_LOAD
                    dashboardManager.UpdateEngineLoad(value)
                Case OBDService.PID_THROTTLE
                    dashboardManager.UpdateThrottle(value)
                Case OBDService.PID_INTAKE_TEMP
                    dashboardManager.UpdateIntakeTemp(CInt(value))
                Case OBDService.PID_AMBIENT_TEMP
                    dashboardManager.UpdateAmbientTemp(CInt(value))
                Case OBDService.PID_FUEL_LEVEL
                    dashboardManager.UpdateFuelLevel(value)
                Case OBDService.PID_MAP
                    dashboardManager.UpdateMAP(CInt(value))
                Case OBDService.PID_MAF
                    dashboardManager.UpdateMAF(value)
                Case OBDService.PID_SHORT_FUEL_TRIM
                    dashboardManager.UpdateShortFuelTrim(value)
                Case OBDService.PID_LONG_FUEL_TRIM
                    dashboardManager.UpdateLongFuelTrim(value)
                Case OBDService.PID_BAROMETRIC
                    dashboardManager.UpdateBarometric(CInt(value))
                Case OBDService.PID_ECU_VOLTAGE
                    dashboardManager.UpdateModuleVoltage(value)
                Case OBDService.PID_FUEL_RATE
                    dashboardManager.UpdateFuelRate(value)
            End Select
        Catch
            ' Silently ignore
        End Try
    End Sub

    ''' <summary>
    ''' O2 sensör değeri güncellendiğinde çağrılır
    ''' </summary>
    Private Sub HandleO2SensorUpdated(sensorIndex As Integer, voltage As Double)
        Try
            dashboardManager.UpdateO2Sensor(sensorIndex, voltage)
        Catch
            ' Silently ignore
        End Try
    End Sub

    ' ========================================
    ' OBD-II ADVANCED EVENT HANDLERLARI (Mode 02-09)
    ' ========================================

    ''' <summary>
    ''' DTC DataGridView kolonlarını ve olaylarını ayarlar
    ''' </summary>
    Private Sub InitializeDTCGrid()
        Try
            dgvDTCList.Columns.Clear()
            
            ' Kod kolonu
            Dim colCode As New DataGridViewTextBoxColumn()
            colCode.Name = "Code"
            colCode.HeaderText = "Kod"
            colCode.Width = 80
            colCode.ReadOnly = True
            dgvDTCList.Columns.Add(colCode)
            
            ' Açıklama kolonu
            Dim colDesc As New DataGridViewTextBoxColumn()
            colDesc.Name = "Description"
            colDesc.HeaderText = "Açıklama"
            colDesc.Width = 380
            colDesc.ReadOnly = True
            dgvDTCList.Columns.Add(colDesc)
            
            ' Kategori kolonu
            Dim colCat As New DataGridViewTextBoxColumn()
            colCat.Name = "Category"
            colCat.HeaderText = "Kategori"
            colCat.Width = 100
            colCat.ReadOnly = True
            dgvDTCList.Columns.Add(colCat)
            
            ' Şiddet kolonu
            Dim colSev As New DataGridViewTextBoxColumn()
            colSev.Name = "Severity"
            colSev.HeaderText = "Şiddet"
            colSev.Width = 90
            colSev.ReadOnly = True
            dgvDTCList.Columns.Add(colSev)
            
            ' Durum kolonu
            Dim colStatus As New DataGridViewTextBoxColumn()
            colStatus.Name = "Status"
            colStatus.HeaderText = "Durum"
            colStatus.Width = 70
            colStatus.ReadOnly = True
            dgvDTCList.Columns.Add(colStatus)
            
            ' Event handlers
            AddHandler dgvDTCList.CellDoubleClick, AddressOf DgvDTCList_CellDoubleClick
            AddHandler dgvDTCList.CellFormatting, AddressOf DgvDTCList_CellFormatting
            AddHandler txtDTCSearch.TextChanged, AddressOf TxtDTCSearch_TextChanged
            AddHandler cmbDTCCategory.SelectedIndexChanged, AddressOf CmbDTCCategory_SelectedIndexChanged
            
        Catch ex As Exception
            Debug.WriteLine($"InitializeDTCGrid error: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' DTC listesini günceller
    ''' </summary>
    Private Sub RefreshDTCGrid()
        Try
            If dgvDTCList.InvokeRequired Then
                dgvDTCList.Invoke(Sub() RefreshDTCGridInternal())
            Else
                RefreshDTCGridInternal()
            End If
        Catch ex As Exception
            Debug.WriteLine($"RefreshDTCGrid error: {ex.Message}")
        End Try
    End Sub
    
    Private Sub RefreshDTCGridInternal()
        dgvDTCList.Rows.Clear()
        
        For Each dtc In _filteredDTCs
            Dim rowIndex = dgvDTCList.Rows.Add()
            Dim row = dgvDTCList.Rows(rowIndex)
            row.Cells("Code").Value = dtc.Code
            row.Cells("Description").Value = If(String.IsNullOrEmpty(dtc.DescriptionTR), dtc.DescriptionEN, dtc.DescriptionTR)
            row.Cells("Category").Value = dtc.Category
            row.Cells("Severity").Value = GetSeverityDisplayText(dtc.Severity)
            row.Cells("Status").Value = GetStatusDisplayText(dtc)
            row.Tag = dtc ' Store the DTCInfo object for later use
        Next
        
        lblDTCCount.Text = $"{_filteredDTCs.Count} arıza kodu"
    End Sub
    
    Private Function GetSeverityDisplayText(severity As String) As String
        Select Case severity?.ToLower()
            Case "critical" : Return "🔴 Kritik"
            Case "high" : Return "🟠 Yüksek"
            Case "medium" : Return "🟡 Orta"
            Case "low" : Return "🟢 Düşük"
            Case Else : Return "⚪ -"
        End Select
    End Function
    
    Private Function GetStatusDisplayText(dtc As DTCInfo) As String
        If dtc.IsPending Then Return "⏳"
        If dtc.IsStored Then Return "💾"
        If dtc.IsPermanent Then Return "🔒"
        Return ""
    End Function
    
    ''' <summary>
    ''' DTC filtreleme uygular
    ''' </summary>
    Private Sub ApplyDTCFilter()
        Try
            Dim searchText = If(txtDTCSearch.Text, "").ToLower().Trim()
            Dim categoryFilter = If(cmbDTCCategory.SelectedIndex > 0, cmbDTCCategory.SelectedItem.ToString(), "")
            
            _filteredDTCs = _allDTCs.Where(Function(dtc)
                ' Arama filtresi
                Dim matchesSearch = String.IsNullOrEmpty(searchText) OrElse
                                   dtc.Code.ToLower().Contains(searchText) OrElse
                                   (dtc.DescriptionTR IsNot Nothing AndAlso dtc.DescriptionTR.ToLower().Contains(searchText)) OrElse
                                   (dtc.DescriptionEN IsNot Nothing AndAlso dtc.DescriptionEN.ToLower().Contains(searchText))
                
                ' Kategori filtresi
                Dim matchesCategory = String.IsNullOrEmpty(categoryFilter) OrElse
                                     categoryFilter.StartsWith(dtc.Category, StringComparison.OrdinalIgnoreCase)
                
                Return matchesSearch AndAlso matchesCategory
            End Function).ToList()
            
            RefreshDTCGrid()
        Catch ex As Exception
            Debug.WriteLine($"ApplyDTCFilter error: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' DTC satırına çift tıklama - detay penceresi açar
    ''' </summary>
    Private Sub DgvDTCList_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex < 0 Then Return
            
            Dim dtc = TryCast(dgvDTCList.Rows(e.RowIndex).Tag, DTCInfo)
            If dtc IsNot Nothing Then
                Dim detailForm As New DTCDetailForm(dtc)
                detailForm.ShowDialog(Me)
            End If
        Catch ex As Exception
            Debug.WriteLine($"DgvDTCList_CellDoubleClick error: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Şiddete göre satır renklendirme
    ''' </summary>
    Private Sub DgvDTCList_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        Try
            If e.RowIndex < 0 Then Return
            
            Dim dtc = TryCast(dgvDTCList.Rows(e.RowIndex).Tag, DTCInfo)
            If dtc Is Nothing Then Return
            
            Dim row = dgvDTCList.Rows(e.RowIndex)
            
            Select Case dtc.Severity?.ToLower()
                Case "critical"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(231, 76, 60)
                Case "high"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(230, 126, 34)
                Case "medium"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(241, 196, 15)
                Case "low"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(46, 204, 113)
                Case Else
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 240)
            End Select
        Catch ex As Exception
            ' Ignore formatting errors
        End Try
    End Sub
    
    ''' <summary>
    ''' DTC arama kutusu değiştiğinde
    ''' </summary>
    Private Sub TxtDTCSearch_TextChanged(sender As Object, e As EventArgs)
        ApplyDTCFilter()
    End Sub
    
    ''' <summary>
    ''' Kategori filtresi değiştiğinde
    ''' </summary>
    Private Sub CmbDTCCategory_SelectedIndexChanged(sender As Object, e As EventArgs)
        ApplyDTCFilter()
    End Sub

    ''' <summary>
    ''' Pending DTC'ler alındığında çağrılır (Mode 07)
    ''' </summary>
    Private Sub HandlePendingDTC(dtcs As List(Of DTCInfo))
        Try
            ' Mark all as pending
            For Each dtc In dtcs
                dtc.IsPending = True
                dtc.IsStored = False
            Next
            
            ' Add to all DTCs list (avoid duplicates)
            For Each dtc In dtcs
                Dim existing = _allDTCs.FirstOrDefault(Function(d) d.Code = dtc.Code)
                If existing IsNot Nothing Then
                    _allDTCs.Remove(existing)
                End If
                _allDTCs.Add(dtc)
            Next
            
            ApplyDTCFilter()
            
            If dtcs.Count = 0 Then
                SafeAddLog("✅ Bekleyen DTC bulunamadı")
            Else
                SafeAddLog($"⚠️ {dtcs.Count} bekleyen DTC algılandı")
            End If
            
            dashboardManager.UpdatePendingDTC(dtcs)
        Catch ex As Exception
            Debug.WriteLine($"HandlePendingDTC error: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Stored DTC'ler alındığında çağrılır (Mode 03)
    ''' </summary>
    Private Sub HandleStoredDTC(dtcs As List(Of DTCInfo))
        Try
            ' Mark all as stored
            For Each dtc In dtcs
                dtc.IsPending = False
                dtc.IsStored = True
            Next
            
            ' Add to all DTCs list (avoid duplicates)
            For Each dtc In dtcs
                Dim existing = _allDTCs.FirstOrDefault(Function(d) d.Code = dtc.Code)
                If existing IsNot Nothing Then
                    _allDTCs.Remove(existing)
                End If
                _allDTCs.Add(dtc)
            Next
            
            ApplyDTCFilter()
            
            If dtcs.Count = 0 Then
                SafeAddLog("✅ Kayıtlı DTC bulunamadı")
            Else
                SafeAddLog($"🔴 {dtcs.Count} kayıtlı DTC algılandı")
            End If
            
            dashboardManager.UpdateStoredDTC(dtcs)
        Catch ex As Exception
            Debug.WriteLine($"HandleStoredDTC error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Freeze Frame verisi alındığında çağrılır (Mode 02)
    ''' </summary>
    Private Sub HandleFreezeFrame(dtc As String, data As Dictionary(Of Byte, Double))
        Try
            If lstFreezeFrame.InvokeRequired Then
                lstFreezeFrame.Invoke(Sub()
                                          lstFreezeFrame.Items.Clear()
                                          lstFreezeFrame.Items.Add($"DTC: {dtc}")
                                          lstFreezeFrame.Items.Add("─────────────────────")
                                          For Each kvp In data
                                              Dim pidName = obdService.GetPIDName(kvp.Key)
                                              Dim unit = obdService.GetPIDUnit(kvp.Key)
                                              lstFreezeFrame.Items.Add($"{pidName}: {kvp.Value:F2} {unit}")
                                          Next
                                      End Sub)
            Else
                lstFreezeFrame.Items.Clear()
                lstFreezeFrame.Items.Add($"DTC: {dtc}")
                lstFreezeFrame.Items.Add("─────────────────────")
                For Each kvp In data
                    Dim pidName = obdService.GetPIDName(kvp.Key)
                    Dim unit = obdService.GetPIDUnit(kvp.Key)
                    lstFreezeFrame.Items.Add($"{pidName}: {kvp.Value:F2} {unit}")
                Next
            End If
            dashboardManager.UpdateFreezeFrame(data)
        Catch ex As Exception
            Debug.WriteLine($"HandleFreezeFrame error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Mode 06 test sonuçları alındığında çağrılır
    ''' </summary>
    Private Sub HandleMode06(tests As List(Of Mode06TestResult))
        Try
            If lstMode06.InvokeRequired Then
                lstMode06.Invoke(Sub()
                                     lstMode06.Items.Clear()
                                     For Each test In tests
                                         Dim status = If(test.Passed, "✅", "❌")
                                         lstMode06.Items.Add($"{status} Test 0x{test.TestId:X2}: {test.TestValue:F2} ({test.MinLimit:F2}-{test.MaxLimit:F2})")
                                     Next
                                     If tests.Count = 0 Then
                                         lstMode06.Items.Add("ℹ️ Test sonucu yok")
                                     End If
                                 End Sub)
            Else
                lstMode06.Items.Clear()
                For Each test In tests
                    Dim status = If(test.Passed, "✅", "❌")
                    lstMode06.Items.Add($"{status} Test 0x{test.TestId:X2}: {test.TestValue:F2} ({test.MinLimit:F2}-{test.MaxLimit:F2})")
                Next
                If tests.Count = 0 Then
                    lstMode06.Items.Add("ℹ️ Test sonucu yok")
                End If
            End If
            dashboardManager.UpdateMode06(tests)
        Catch ex As Exception
            Debug.WriteLine($"HandleMode06 error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Araç bilgisi alındığında çağrılır (Mode 09)
    ''' </summary>
    Private Sub HandleVehicleInfo(info As VehicleInfoModel)
        Try
            SafeUpdateLabel(lblVINValue, If(String.IsNullOrEmpty(info.VIN), "---", info.VIN))
            SafeUpdateLabel(lblCalibrationIDValue, If(String.IsNullOrEmpty(info.CalibrationID), "---", info.CalibrationID))
            SafeUpdateLabel(lblCVNValue, If(String.IsNullOrEmpty(info.CVN), "---", info.CVN))
            SafeUpdateLabel(lblECUNameValue, If(String.IsNullOrEmpty(info.ECUName), "---", info.ECUName))
            dashboardManager.UpdateVehicleInfo(info)

            ' ========================================
            ' VIN AUTO PROFILE INTEGRATION
            ' ========================================
            If _vinAutoProfileEnabled AndAlso Not String.IsNullOrEmpty(info.VIN) Then
                TryLoadVehicleProfile(info.VIN)
            End If
        Catch ex As Exception
            Debug.WriteLine($"HandleVehicleInfo error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' VIN'e göre araç profilini yüklemeye çalışır
    ''' </summary>
    Private Sub TryLoadVehicleProfile(vin As String)
        Try
            Dim profile = vehicleProfileManager.GetProfileByVIN(vin)
            If profile IsNot Nothing Then
                _currentVehicleProfile = profile
                SafeAddLog($"🚗 Araç profili yüklendi: {profile.Brand} {profile.Model} ({profile.Year})")
                ApplyVehicleProfile(profile)
            Else
                ' Profil bulunamadı - VIN'den bilgi çıkarmaya çalış
                Dim decodedProfile = DecodeVINToProfile(vin)
                If decodedProfile IsNot Nothing Then
                    _currentVehicleProfile = decodedProfile
                    SafeAddLog($"🔍 VIN'den profil oluşturuldu: {decodedProfile.Brand} {decodedProfile.Model}")
                    ' Yeni profili kaydet
                    vehicleProfileManager.AddOrUpdateProfile(decodedProfile)
                    ApplyVehicleProfile(decodedProfile)

                    ' Arka planda veritabanı zenginleştirme başlat
                    TriggerBackgroundEnrichment(vin)
                Else
                    SafeAddLog($"ℹ️ VIN profili bulunamadı: {vin}")
                End If
            End If
            UpdateVINProfileUI()
        Catch ex As Exception
            SafeAddLog("❌ Profil yükleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Arka planda veritabanı zenginleştirme başlatır
    ''' </summary>
    Private Async Sub TriggerBackgroundEnrichment(vin As String)
        ' Null ve closed checks
        If _isClosing OrElse onlineCarDbScraper Is Nothing Then Return
        If String.IsNullOrWhiteSpace(vin) Then Return
        
        Try
            SafeAddLog("📚 Arka planda veritabanı zenginleştirme başlıyor...")

            ' VIN bazlı zenginleştirme
            Dim addedCount As Integer = 0
            Try
                addedCount = Await onlineCarDbScraper.EnrichForVINAsync(vin).ConfigureAwait(False)
            Catch httpEx As Net.Http.HttpRequestException
                SafeAddLog("⚠️ Zenginleştirme: İnternet bağlantısı yok veya sunucu yanıt vermiyor")
                Return
            Catch timeoutEx As TimeoutException
                SafeAddLog("⚠️ Zenginleştirme: İstek zaman aşımına uğradı")
                Return
            End Try

            If _isClosing Then Return
            
            If addedCount > 0 Then
                SafeAddLog($"✅ Zenginleştirme tamamlandı: {addedCount} yeni komut eklendi")
                ' Komut listelerini yenile (UI thread'de)
                SafeInvoke(Sub() RefreshCommandsUI())
            Else
                SafeAddLog("ℹ️ Zenginleştirme: Yeni veri bulunamadı")
            End If

        Catch ex As Exception
            If Not _isClosing Then
                SafeAddLog("⚠️ Zenginleştirme hatası: " & ex.Message)
                Debug.WriteLine($"Background enrichment error: {ex}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Manuel veritabanı zenginleştirme
    ''' </summary>
    Public Async Sub ManualEnrichDatabase()
        ' Null and closed checks
        If _isClosing OrElse onlineCarDbScraper Is Nothing Then
            SafeAddLog("⚠️ Zenginleştirme servisi hazır değil")
            Return
        End If
        
        Try
            SafeAddLog("📚 Manuel veritabanı zenginleştirme başlıyor...")
            SafeAddLog("   Bu işlem internet bağlantısı gerektirir ve birkaç dakika sürebilir.")

            ' Standart OBD-II PID'leri ekle
            onlineCarDbScraper.AddStandardOBDPIDs()

            ' Tam zenginleştirme
            Dim addedCount As Integer = 0
            Try
                addedCount = Await onlineCarDbScraper.EnrichDatabaseAsync().ConfigureAwait(False)
            Catch httpEx As Net.Http.HttpRequestException
                SafeAddLog("❌ Zenginleştirme hatası: İnternet bağlantısı kurulamadı")
                Return
            Catch timeoutEx As TimeoutException
                SafeAddLog("❌ Zenginleştirme hatası: İstek zaman aşımına uğradı")
                Return
            End Try

            If _isClosing Then Return
            
            SafeAddLog($"✅ Zenginleştirme tamamlandı!")
            SafeAddLog(onlineCarDbScraper.GetStatusReport())

            ' Komut listelerini yenile (UI thread'de)
            SafeInvoke(Sub() RefreshCommandsUI())

        Catch ex As Exception
            If Not _isClosing Then
                SafeAddLog("❌ Zenginleştirme hatası: " & ex.Message)
                Debug.WriteLine($"Manual enrichment error: {ex}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' VIN kodundan araç bilgilerini çıkarır (basit WMI decoder)
    ''' </summary>
    Private Function DecodeVINToProfile(vin As String) As VehicleProfile
        If String.IsNullOrEmpty(vin) OrElse vin.Length < 11 Then Return Nothing

        Dim profile As New VehicleProfile()
        profile.VIN = vin.ToUpper()

        ' WMI (World Manufacturer Identifier) - ilk 3 karakter
        Dim wmi = vin.Substring(0, 3).ToUpper()

        ' Yaygın WMI kodları
        Select Case wmi
            Case "WVW", "WV2", "WV1"
                profile.Brand = "Volkswagen"
            Case "WAU", "WA1"
                profile.Brand = "Audi"
            Case "WBA", "WBS", "WBY"
                profile.Brand = "BMW"
            Case "WDB", "WDC", "WDD"
                profile.Brand = "Mercedes-Benz"
            Case "WF0", "WF1"
                profile.Brand = "Ford"
            Case "1G1", "1G2", "2G1"
                profile.Brand = "Chevrolet"
            Case "1FA", "1FB", "1FM"
                profile.Brand = "Ford"
            Case "1HG", "2HG", "5J6"
                profile.Brand = "Honda"
            Case "JT2", "JTD", "JTE"
                profile.Brand = "Toyota"
            Case "VF1", "VF3", "VF7"
                profile.Brand = "Renault"
            Case "ZFA"
                profile.Brand = "Fiat"
            Case "VSS"
                profile.Brand = "SEAT"
            Case "TMB"
                profile.Brand = "Skoda"
            Case "TRU"
                profile.Brand = "Audi"
            Case Else
                profile.Brand = "Unknown"
        End Select

        ' Model yılı - 10. karakter (A=2010, B=2011, ..., Y=2030, 1-9=2031-2039)
        Dim yearCode = vin.Chars(9)
        profile.Year = DecodeVINYear(yearCode)

        ' Varsayılan protokol
        profile.Protocol = "CAN 500kbps"
        profile.Model = "Auto-detected"
        profile.Source = "VIN Decode"
        profile.Confidence = 0.7

        Return profile
    End Function

    ''' <summary>
    ''' VIN yıl kodunu yıla çevirir
    ''' </summary>
    Private Function DecodeVINYear(yearCode As Char) As String
        Select Case yearCode
            Case "A"c : Return "2010"
            Case "B"c : Return "2011"
            Case "C"c : Return "2012"
            Case "D"c : Return "2013"
            Case "E"c : Return "2014"
            Case "F"c : Return "2015"
            Case "G"c : Return "2016"
            Case "H"c : Return "2017"
            Case "J"c : Return "2018"
            Case "K"c : Return "2019"
            Case "L"c : Return "2020"
            Case "M"c : Return "2021"
            Case "N"c : Return "2022"
            Case "P"c : Return "2023"
            Case "R"c : Return "2024"
            Case "S"c : Return "2025"
            Case "T"c : Return "2026"
            Case "V"c : Return "2027"
            Case "W"c : Return "2028"
            Case "X"c : Return "2029"
            Case "Y"c : Return "2030"
            Case "1"c To "9"c : Return (2030 + Integer.Parse(yearCode)).ToString()
            Case Else : Return "Unknown"
        End Select
    End Function

    ''' <summary>
    ''' Araç profilini uygular - UI'ı ve servisleri günceller
    ''' </summary>
    Private Sub ApplyVehicleProfile(profile As VehicleProfile)
        Try
            ' Commands tab'da marka/model seçimlerini güncelle
            If cmbBrand.Items.Contains(profile.Brand) Then
                SafeInvoke(Sub()
                               cmbBrand.SelectedItem = profile.Brand
                           End Sub)
            End If

            ' Coding tab'da da güncelle
            If cmbCodeBrand.Items.Contains(profile.Brand) Then
                SafeInvoke(Sub()
                               cmbCodeBrand.SelectedItem = profile.Brand
                           End Sub)
            End If

            ' TODO: Profildeki KnownCommands listesini kullanarak özel komutları yükle
        Catch ex As Exception
            SafeAddLog("❌ Profil uygulama hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' VIN profil UI'ını günceller
    ''' </summary>
    Private Sub UpdateVINProfileUI()
        Try
            If _currentVehicleProfile IsNot Nothing Then
                SafeUpdateLabel(lblVINProfileStatus, $"✅ {_currentVehicleProfile.Brand} {_currentVehicleProfile.Model} ({_currentVehicleProfile.Year})")
            Else
                SafeUpdateLabel(lblVINProfileStatus, "⚪ Profil yüklenmedi")
            End If
        Catch
            ' UI güncellemesi başarısız - sessizce devam et
        End Try
    End Sub

    ''' <summary>
    ''' Thread-safe UI güncellemesi için yardımcı
    ''' BeginInvoke kullanarak UI thread'ini bloklamaz
    ''' </summary>
    Private Sub SafeInvoke(action As Action)
        If action Is Nothing Then Return
        If _isClosing Then Return
        
        Try
            If Me.IsDisposed OrElse Me.Disposing Then Return
            If Not Me.IsHandleCreated Then Return
            
            If Me.InvokeRequired Then
                Try
                    ' BeginInvoke kullan - UI thread'i bloklama
                    Me.BeginInvoke(action)
                Catch ex As ObjectDisposedException
                    ' Form kapanıyor - ignore
                Catch ex As InvalidOperationException
                    ' Handle zaten geçersiz - ignore
                End Try
            Else
                action()
            End If
        Catch ex As Exception
            Debug.WriteLine($"SafeInvoke error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Grafik legend güncelleme timer'ı
    ''' </summary>
    Private WithEvents _chartLegendTimer As New System.Windows.Forms.Timer()

    ''' <summary>
    ''' Grafikleri başlat ve yapılandır
    ''' </summary>
    Private Sub InitializeCharts()
        Try
            ' RPM grafiği
            liveChartRPM.Title = "RPM"
            liveChartRPM.Unit = "rpm"
            liveChartRPM.MinValue = 0
            liveChartRPM.MaxValue = 8000
            liveChartRPM.AutoScale = True
            liveChartRPM.LineColor = Color.FromArgb(255, 193, 7) ' Sarı

            ' Hız grafiği
            liveChartSpeed.Title = "Hız"
            liveChartSpeed.Unit = "km/h"
            liveChartSpeed.MinValue = 0
            liveChartSpeed.MaxValue = 300
            liveChartSpeed.AutoScale = True
            liveChartSpeed.LineColor = Color.FromArgb(52, 152, 219) ' Mavi

            ' Sıcaklık grafiği
            liveChartTemp.Title = "Soğutma Sıcaklığı"
            liveChartTemp.Unit = "°C"
            liveChartTemp.MinValue = -40
            liveChartTemp.MaxValue = 150
            liveChartTemp.AutoScale = True
            liveChartTemp.LineColor = Color.FromArgb(231, 76, 60) ' Kırmızı

            ' Zaman aralığı varsayılanı
            cmbChartTimeRange.SelectedIndex = 1 ' 60 saniye

            ' Grafik legend'larını başlat (timer ile güncelle)
            InitializeChartLegends()

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.InitializeCharts")
        End Try
    End Sub

    ''' <summary>
    ''' Grafik legend'larını başlat
    ''' </summary>
    Private Sub InitializeChartLegends()
        Try
            ' Legend güncelleme timer'ı (1 saniyede bir)
            _chartLegendTimer.Interval = 1000
            AddHandler _chartLegendTimer.Tick, AddressOf ChartLegendTimer_Tick
            _chartLegendTimer.Start()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.InitializeChartLegends")
        End Try
    End Sub

    ''' <summary>
    ''' Grafik legend'larını güncelle
    ''' </summary>
    Private Sub ChartLegendTimer_Tick(sender As Object, e As EventArgs) Handles _chartLegendTimer.Tick
        Try
            If liveChartRPM Is Nothing OrElse liveChartSpeed Is Nothing OrElse liveChartTemp Is Nothing Then Return

            ' Legend bilgilerini güncelle (chart title'larına ekle)
            UpdateChartTitle(liveChartRPM, "RPM")
            UpdateChartTitle(liveChartSpeed, "Hız")
            UpdateChartTitle(liveChartTemp, "Soğutma Sıcaklığı")
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Grafik başlığını güncelle (legend bilgileri ile)
    ''' </summary>
    Private Sub UpdateChartTitle(chart As LiveChart, baseTitle As String)
        Try
            Dim current = chart.CurrentValue.ToString("F0")
            Dim min = chart.MinDataValue.ToString("F0")
            Dim max = chart.MaxDataValue.ToString("F0")
            Dim avg = chart.AverageValue.ToString("F0")
            chart.Title = $"{baseTitle} | Mevcut: {current} | Min: {min} | Max: {max} | Ort: {avg}"
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Thread-safe UI güncellemesi - senkron versiyonu (sonuç bekler)
    ''' </summary>
    Private Sub SafeInvokeSync(action As Action)
        If action Is Nothing Then Return
        If _isClosing Then Return
        
        Try
            If Me.IsDisposed OrElse Me.Disposing Then Return
            If Not Me.IsHandleCreated Then Return
            
            If Me.InvokeRequired Then
                Try
                    Me.Invoke(action)
                Catch ex As ObjectDisposedException
                Catch ex As InvalidOperationException
                End Try
            Else
                action()
            End If
        Catch ex As Exception
            Debug.WriteLine($"SafeInvokeSync error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Thread-safe control güncellemesi
    ''' </summary>
    Private Sub SafeUpdateControl(control As Control, action As Action)
        If action Is Nothing OrElse control Is Nothing Then Return
        If _isClosing Then Return
        
        Try
            If control.IsDisposed OrElse control.Disposing Then Return
            If Not control.IsHandleCreated Then Return
            
            If control.InvokeRequired Then
                Try
                    control.BeginInvoke(action)
                Catch ex As ObjectDisposedException
                Catch ex As InvalidOperationException
                End Try
            Else
                action()
            End If
        Catch ex As Exception
            Debug.WriteLine($"SafeUpdateControl error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' PID Auto-Scan tamamlandığında çağrılır
    ''' </summary>
    Private Sub HandleAutoScanComplete(supportedPIDs As Boolean())
        Try
            If lstSupportedPIDs.InvokeRequired Then
                lstSupportedPIDs.Invoke(Sub()
                                            lstSupportedPIDs.Items.Clear()
                                            Dim count = 0
                                            For i As Integer = 1 To 255
                                                If supportedPIDs(i) Then
                                                    Dim pidName = obdService.GetPIDName(CByte(i))
                                                    lstSupportedPIDs.Items.Add($"0x{i:X2} - {pidName}")
                                                    count += 1
                                                End If
                                            Next
                                            lblScanStatus.Text = $"✅ {count} PID bulundu"
                                        End Sub)
            Else
                lstSupportedPIDs.Items.Clear()
                Dim count = 0
                For i As Integer = 1 To 255
                    If supportedPIDs(i) Then
                        Dim pidName = obdService.GetPIDName(CByte(i))
                        lstSupportedPIDs.Items.Add($"0x{i:X2} - {pidName}")
                        count += 1
                    End If
                Next
                lblScanStatus.Text = $"✅ {count} PID bulundu"
            End If
            dashboardManager.UpdateSupportedPIDFlags(supportedPIDs)
        Catch ex As Exception
            Debug.WriteLine($"HandleAutoScanComplete error: {ex.Message}")
        End Try
    End Sub

    ' ========================================
    ' DİAGNOSTİCS TAB BUTON HANDLERLARI
    ' ========================================

    ''' <summary>
    ''' Bekleyen DTC'leri oku (Mode 07)
    ''' </summary>
    Private Sub btnReadPendingDTC_Click(sender As Object, e As EventArgs) Handles btnReadPendingDTC.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            SafeAddLog("⏳ Bekleyen DTC'ler okunuyor (Mode 07)...")
            obdService.RequestPendingDTC()
        Catch ex As Exception
            SafeAddLog("❌ DTC okuma hatası: " & ex.Message)
        End Try
    End Sub
    
    ''' <summary>
    ''' Kayıtlı DTC'leri oku (Mode 03)
    ''' </summary>
    Private Sub btnReadStoredDTC_Click(sender As Object, e As EventArgs) Handles btnReadStoredDTC.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            SafeAddLog("⏳ Kayıtlı DTC'ler okunuyor (Mode 03)...")
            obdService.RequestStoredDTC()
        Catch ex As Exception
            SafeAddLog("❌ DTC okuma hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' DTC'leri temizle (Mode 04)
    ''' </summary>
    Private Sub btnClearDTC_Click(sender As Object, e As EventArgs) Handles btnClearDTC.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            If MessageBox.Show("Tüm DTC'ler temizlenecek. Bu işlem geri alınamaz!" & vbCrLf &
                              "Emin misiniz?", "DTC Temizle",
                              MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = DialogResult.Yes Then
                obdService.ClearDTC()
                SafeAddLog("🗑️ DTC temizleme isteği gönderildi (Mode 04)")
            End If
        Catch ex As Exception
            SafeAddLog("❌ DTC temizleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Freeze Frame oku (Mode 02)
    ''' </summary>
    Private Sub btnReadFreezeFrame_Click(sender As Object, e As EventArgs) Handles btnReadFreezeFrame.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            lstFreezeFrame.Items.Clear()
            lstFreezeFrame.Items.Add("⏳ Freeze Frame okunuyor...")
            obdService.RequestFreezeFrame(0)
            SafeAddLog("❄️ Freeze Frame isteği gönderildi (Mode 02)")
        Catch ex As Exception
            SafeAddLog("❌ Freeze Frame okuma hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Mode 06 test sonuçlarını oku
    ''' </summary>
    Private Sub btnReadMode06_Click(sender As Object, e As EventArgs) Handles btnReadMode06.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            lstMode06.Items.Clear()
            lstMode06.Items.Add("⏳ Test sonuçları okunuyor...")
            obdService.RequestMode06(0)
            SafeAddLog("📊 Mode 06 isteği gönderildi")
        Catch ex As Exception
            SafeAddLog("❌ Mode 06 okuma hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' ISO-TP Test aracını aç
    ''' </summary>
    Private Sub btnIsoTpTest_Click(sender As Object, e As EventArgs) Handles btnIsoTpTest.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                MessageBox.Show("ISO-TP test aracını kullanmak için önce CAN adaptörüne bağlanmanız gerekiyor.", "Bağlantı Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim testForm As New IsoTpTestForm(canSender, canParser)
            testForm.ShowDialog()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.btnIsoTpTest_Click")
            MessageBox.Show($"ISO-TP test aracı açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Araç bilgilerini oku (Mode 09)
    ''' </summary>
    Private Sub btnReadVehicleInfo_Click(sender As Object, e As EventArgs) Handles btnReadVehicleInfo.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            SafeUpdateLabel(lblVINValue, "⏳ Okunuyor...")
            SafeUpdateLabel(lblCalibrationIDValue, "⏳ Okunuyor...")
            SafeUpdateLabel(lblCVNValue, "⏳ Okunuyor...")
            SafeUpdateLabel(lblECUNameValue, "⏳ Okunuyor...")
            obdService.RequestVehicleInfo()
            SafeAddLog("🚗 Araç bilgisi isteği gönderildi (Mode 09)")
        Catch ex As Exception
            SafeAddLog("❌ Araç bilgisi okuma hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Desteklenen PID'leri otomatik tara
    ''' </summary>
    Private Sub btnAutoScanPIDs_Click(sender As Object, e As EventArgs) Handles btnAutoScanPIDs.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If
            lstSupportedPIDs.Items.Clear()
            lstSupportedPIDs.Items.Add("⏳ PID taraması yapılıyor...")
            lblScanStatus.Text = "🔍 Taranıyor..."
            obdService.AutoScanPIDs()
            SafeAddLog("🔍 PID Auto-Scan başlatıldı")
        Catch ex As Exception
            SafeAddLog("❌ PID tarama hatası: " & ex.Message)
        End Try
    End Sub

    ' ========================================
    ' VIN AUTO PROFILE BUTON HANDLERLARI
    ' ========================================

    ''' <summary>
    ''' VIN Auto Profile toggle değiştiğinde
    ''' </summary>
    Private Sub chkVINAutoProfile_CheckedChanged(sender As Object, e As EventArgs) Handles chkVINAutoProfile.CheckedChanged
        _vinAutoProfileEnabled = chkVINAutoProfile.Checked
        SafeAddLog($"🚗 VIN Auto Profile: {If(_vinAutoProfileEnabled, "Aktif", "Kapalı")}")
    End Sub

    ''' <summary>
    ''' Profil manuel yükleme
    ''' </summary>
    Private Sub btnLoadVINProfile_Click(sender As Object, e As EventArgs) Handles btnLoadVINProfile.Click
        Try
            Dim vin = lblVINValue.Text.Trim()
            If String.IsNullOrEmpty(vin) OrElse vin = "---" OrElse vin.StartsWith("⏳") Then
                SafeAddLog("❌ Önce VIN bilgisini okuyun.")
                Return
            End If
            TryLoadVehicleProfile(vin)
        Catch ex As Exception
            SafeAddLog("❌ Profil yükleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Mevcut profili kaydet
    ''' </summary>
    Private Sub btnSaveVINProfile_Click(sender As Object, e As EventArgs) Handles btnSaveVINProfile.Click
        Try
            If _currentVehicleProfile Is Nothing Then
                SafeAddLog("❌ Kaydedilecek profil yok.")
                Return
            End If
            vehicleProfileManager.AddOrUpdateProfile(_currentVehicleProfile)
            SafeAddLog($"💾 Profil kaydedildi: {_currentVehicleProfile.VIN}")
        Catch ex As Exception
            SafeAddLog("❌ Profil kaydetme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Veritabanı zenginleştirme butonu
    ''' </summary>
    Private Sub btnEnrichDatabase_Click(sender As Object, e As EventArgs) Handles btnEnrichDatabase.Click
        ManualEnrichDatabase()
    End Sub

    ' ========================================
    ' AI-FINDER BUTON HANDLERLARI
    ' ========================================

    ''' <summary>
    ''' AI-Finder toggle değiştiğinde
    ''' </summary>
    Private Sub chkAIFinderEnabled_CheckedChanged(sender As Object, e As EventArgs) Handles chkAIFinderEnabled.CheckedChanged
        _aiFinderEnabled = chkAIFinderEnabled.Checked
        lblAIFinderStatus.Text = If(_aiFinderEnabled, "🟢 Durum: Aktif", "⚪ Durum: Kapalı")
        SafeAddLog($"🤖 AI-Finder: {If(_aiFinderEnabled, "Aktif", "Kapalı")}")
    End Sub

    ''' <summary>
    ''' AI-Finder başlat
    ''' </summary>
    Private Sub btnStartAIFinder_Click(sender As Object, e As EventArgs) Handles btnStartAIFinder.Click
        Try
            If Not SerialPort1.IsOpen Then
                SafeAddLog("❌ Önce CAN adaptörüne bağlanın.")
                Return
            End If

            ' Learning Engine'i de başlat
            If Not learningEngine.IsRunning Then
                learningEngine.Start()
            End If

            _aiFinderEnabled = True
            chkAIFinderEnabled.Checked = True
            lblAIFinderStatus.Text = "🟢 Durum: Aktif"
            lstAIFinderResults.Items.Clear()
            lstAIFinderResults.Items.Add("🚀 AI-Finder başlatıldı...")
            SafeAddLog("🤖 AI-Finder başlatıldı - otomatik keşif aktif")
        Catch ex As Exception
            SafeAddLog("❌ AI-Finder başlatma hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder durdur
    ''' </summary>
    Private Sub btnStopAIFinder_Click(sender As Object, e As EventArgs) Handles btnStopAIFinder.Click
        Try
            _aiFinderEnabled = False
            chkAIFinderEnabled.Checked = False
            lblAIFinderStatus.Text = "⚪ Durum: Kapalı"
            lstAIFinderResults.Items.Add("⏹️ AI-Finder durduruldu")
            SafeAddLog("🤖 AI-Finder durduruldu")
        Catch ex As Exception
            SafeAddLog("❌ AI-Finder durdurma hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder bulgularını kaydet
    ''' </summary>
    Private Sub btnAIFinderSaveAll_Click(sender As Object, e As EventArgs) Handles btnAIFinderSaveAll.Click
        Try
            Dim predictions = learningEngine.GetCommandPredictions()
            If predictions.Count = 0 Then
                SafeAddLog("ℹ️ Kaydedilecek tahmin bulunamadı.")
                Return
            End If

            Dim savedCount As Integer = 0
            For Each prediction In predictions
                If prediction.Confidence > 0.7 Then
                    ' CommandRepository'ye ekle
                    Dim brand = If(_currentVehicleProfile?.Brand, "Unknown")
                    Dim model = If(_currentVehicleProfile?.Model, "Auto-detected")
                    commandRepository.AddCommand(brand, model, prediction.Category, prediction.PredictedName, prediction.FramePattern)
                    savedCount += 1
                End If
            Next

            If savedCount > 0 Then
                commandRepository.Save()
                SafeAddLog($"💾 {savedCount} komut tahmini veritabanına kaydedildi")
            Else
                SafeAddLog("ℹ️ Yeterli güvenilirlikte tahmin bulunamadı (>70%)")
            End If
        Catch ex As Exception
            SafeAddLog("❌ AI-Finder kaydetme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder kategori filtresi değiştiğinde
    ''' </summary>
    Private Sub cmbAIFinderCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbAIFinderCategory.SelectedIndexChanged
        ' TODO: Kategori filtreleme mantığı eklenecek
        SafeAddLog($"🏷️ AI-Finder kategori filtresi: {cmbAIFinderCategory.SelectedItem}")
    End Sub

    ''' <summary>
    ''' AI-Finder: Aksiyon öncesi snapshot yakala
    ''' </summary>
    Private Sub btnSnapshotBefore_Click(sender As Object, e As EventArgs) Handles btnSnapshotBefore.Click
        Try
            learningEngine.CaptureSnapshotBefore()
            SafeAddLog("📷 AI-Finder: Before snapshot yakalandı")
            lstAIFinderResults.Items.Add("📷 Before snapshot yakalandı - Şimdi aracınızda bir aksiyon yapın")
        Catch ex As Exception
            SafeAddLog("❌ Snapshot hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder: Aksiyon sonrası snapshot yakala
    ''' </summary>
    Private Sub btnSnapshotAfter_Click(sender As Object, e As EventArgs) Handles btnSnapshotAfter.Click
        Try
            learningEngine.CaptureSnapshotAfter()
            SafeAddLog("📷 AI-Finder: After snapshot yakalandı")
            lstAIFinderResults.Items.Add("📷 After snapshot yakalandı - Auto-Diff için hazır")
        Catch ex As Exception
            SafeAddLog("❌ Snapshot hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' AI-Finder: Auto-Diff çalıştır
    ''' </summary>
    Private Sub btnAutoDiff_Click(sender As Object, e As EventArgs) Handles btnAutoDiff.Click
        Try
            If Not learningEngine.AreSnapshotsReady() Then
                SafeAddLog("⚠️ Önce Before ve After snapshot'ları yakalayın")
                lstAIFinderResults.Items.Add("⚠️ Önce Before ve After snapshot'ları yakalayın!")
                Return
            End If

            lstAIFinderResults.Items.Add("🔍 Auto-Diff çalıştırılıyor...")
            Dim predictions = learningEngine.AutoDiff()

            If predictions.Count = 0 Then
                lstAIFinderResults.Items.Add("ℹ️ Farklılık bulunamadı")
                SafeAddLog("ℹ️ AI-Finder: Auto-Diff farklılık bulamadı")
            Else
                SafeAddLog($"🎯 AI-Finder: {predictions.Count} adet komut tahmini bulundu")
                For Each pred In predictions
                    lstAIFinderResults.Items.Add($"💡 {pred.Category} | ID 0x{pred.IdHex} Byte[{pred.ByteIndex}] | {pred.Description} ({pred.Confidence}%)")
                Next
            End If

            ' Snapshot'ları temizle
            learningEngine.ClearSnapshots()

        Catch ex As Exception
            SafeAddLog("❌ Auto-Diff hatası: " & ex.Message)
        End Try
    End Sub

    ' OBD Başlat butonu
    Private Sub btnStartOBD_Click(sender As Object, e As EventArgs) Handles btnStartOBD.Click
        Try
            obdService.StartPolling()
            lblOBDStatus.Text = "🟢 OBD Polling: Aktif"
            lblOBDStatus.ForeColor = Color.FromArgb(46, 204, 113)
            AddLog("📡 OBD-II polling başlatıldı.")
        Catch ex As Exception
            AddLog("❌ OBD başlatma hatası: " & ex.Message)
        End Try
    End Sub

    ' OBD Durdur butonu
    Private Sub btnStopOBD_Click(sender As Object, e As EventArgs) Handles btnStopOBD.Click
        Try
            obdService.StopPolling()
            lblOBDStatus.Text = "⚪ OBD Polling: Kapalı"
            lblOBDStatus.ForeColor = Color.Gray
            AddLog("⏹️ OBD-II polling durduruldu.")
        Catch ex As Exception
            AddLog("❌ OBD durdurma hatası: " & ex.Message)
        End Try
    End Sub

    ' Bağlan butonuna tıklanınca çalışacak
    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        AddLog("Portlar taranıyor...")

        Try
            ' Zaten bağlıysa önce kapat
            If SerialPort1 IsNot Nothing AndAlso SerialPort1.IsOpen Then
                Try
                    SerialPort1.Close()
                    Threading.Thread.Sleep(100) ' Port'un kapanmasını bekle
                    AddLog("⚠️ Mevcut bağlantı kapatıldı.")
                Catch closeEx As Exception
                    AddLog($"⚠️ Port kapatma hatası: {closeEx.Message}")
                End Try
            End If
            
            Dim portlar() As String = Nothing
            Try
                portlar = IO.Ports.SerialPort.GetPortNames()
            Catch portEx As Exception
                AddLog($"❌ Port listesi alınamadı: {portEx.Message}")
                UpdateConnectionStatus(False, "", "")
                ' Başarısız olursa wizard öner
                SuggestWizard("Port listesi alınamadı")
                Return
            End Try

            If portlar Is Nothing OrElse portlar.Length = 0 Then
                AddLog("❌ Hiç COM port bulunamadı.")
                UpdateConnectionStatus(False, "", "")
                SuggestWizard("Hiç COM port bulunamadı")
                Return
            End If

            ' Sistem portlarını filtrele
            Dim adayPort As String = Nothing

            For Each p In portlar
                If p.ToUpper() <> "COM1" AndAlso p.ToUpper() <> "COM2" Then
                    adayPort = p
                    Exit For
                End If
            Next

            If adayPort Is Nothing Then
                AddLog("❌ Uygun COM port bulunamadı.")
                UpdateConnectionStatus(False, "", "")
                SuggestWizard("Uygun COM port bulunamadı")
                Return
            End If

            ' Port ayarlarını yap
            SerialPort1.PortName = adayPort
            SerialPort1.BaudRate = 115200
            SerialPort1.ReadTimeout = 2000
            SerialPort1.WriteTimeout = 2000
            SerialPort1.DtrEnable = True
            SerialPort1.RtsEnable = True
            
            ' Portu aç
            Try
                SerialPort1.Open()
            Catch openEx As UnauthorizedAccessException
                AddLog($"❌ Port erişim engellendi: {adayPort} başka bir uygulama tarafından kullanılıyor olabilir.")
                UpdateConnectionStatus(False, "", "")
                SuggestWizard("Port erişim engellendi")
                Return
            Catch openEx As IO.IOException
                AddLog($"❌ Port I/O hatası: {openEx.Message}")
                UpdateConnectionStatus(False, "", "")
                SuggestWizard("Port I/O hatası")
                Return
            End Try
            
            ' SLCAN modunu aç
            Try
                SerialPort1.WriteLine("O")
                AddLog("📡 CAN dinleme modu açıldı.")
            Catch writeEx As Exception
                AddLog($"⚠️ SLCAN modu açma hatası: {writeEx.Message}")
            End Try

            AddLog("✅ Bağlandı: " & adayPort)
            UpdateConnectionStatus(True, adayPort, "115200")
            _lastDataReceivedTime = DateTime.Now ' İlk bağlantı zamanını kaydet
            
            ' Bağlantı bilgilerini kaydet
            SaveConnectionInfo(adayPort, 115200, "SLCAN")

        Catch ex As Exception
            AddLog("❌ Bağlantı hatası: " & ex.Message)
            UpdateConnectionStatus(False, "", "")
            SuggestWizard("Bağlantı hatası: " & ex.Message)
            Debug.WriteLine($"Connect error: {ex}")
        End Try

    End Sub



    ' Log yazmak için yardımcı fonksiyon
    Private Sub AddLog(message As String)
        Dim satir As String = DateTime.Now.ToString("HH:mm:ss") & " - " & message
        lstLog.Items.Add(satir)
    End Sub
    Private Sub SerialPort1_DataReceived(sender As Object, e As IO.Ports.SerialDataReceivedEventArgs) Handles SerialPort1.DataReceived
        Try
            Dim line As String = SerialPort1.ReadLine()
            ProcessSlcanFrame(line)
            ' Bağlantı aktif - son veri alma zamanını güncelle
            _lastDataReceivedTime = DateTime.Now
        Catch ex As Exception
            ' Sessizce ignore edebiliriz
        End Try
    End Sub

    Private Sub SerialPort1_ErrorReceived(sender As Object, e As IO.Ports.SerialErrorReceivedEventArgs) Handles SerialPort1.ErrorReceived
        Try
            AddLog($"⚠️ Seri port hatası: {e.EventType}")
            ' Bağlantı hatası - durumu güncelle
            UpdateConnectionStatus(False, "", "")
        Catch
        End Try
    End Sub

    Private Sub ProcessSlcanFrame(frame As String)
        If String.IsNullOrWhiteSpace(frame) Then Return

        ' CANParser ile frame'i parse et
        Dim parsed As CANFrame = canParser.Parse(frame)

        ' Geçersiz frame ise çık
        If Not parsed.IsValid Then Exit Sub

        Dim id As Integer = parsed.Id
        Dim data As Byte() = parsed.Data

        ' ========================================
        ' OBD-II CEVAPLARI (0x7E8-0x7EF)
        ' ========================================
        If obdService.IsOBDResponse(id) Then
            ' ProcessFrameAdvanced tüm modları destekler (01-09)
            obdService.ProcessFrameAdvanced(id, data)
        End If

        ' ========================================
        ' ÖNEMLİ: Dashboard ve LearningEngine FİLTREDEN ETKİLENMEZ
        ' project-spec.md Section 10'a göre filtre SADECE log'u etkiler
        ' ========================================

        ' Dashboard'u her zaman güncelle (filtreden bağımsız)
        dashboardManager.ProcessFrame(id, data)

        ' Session kaydı (filtreden bağımsız)
        RecordCANFrame(id, data)

        ' LearningEngine'e her zaman gönder (filtreden bağımsız)
        If learningEngine.IsRunning Then
            learningEngine.AnalyzeFrame(id, data)
        End If

        ' ========================================
        ' FİLTRE SADECE LOG ÇIKTISINI ETKİLER
        ' (OBD cevapları her zaman gösterilir - FilterManager'da whitelist)
        ' ========================================
        Dim shouldLog As Boolean = filterManager.ShouldShowInLog(id)

        If shouldLog Then
            ' Thread-safe log yazımı
            If lstLog.InvokeRequired Then
                lstLog.Invoke(Sub() AddLog("RX: " & parsed.RawFrame))
            Else
                AddLog("RX: " & parsed.RawFrame)
            End If
        End If

    End Sub


    Private Sub btnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        Try
            Dim id As String = If(txtID?.Text, "").Trim()
            Dim data As String = If(txtData?.Text, "").Trim()

            If String.IsNullOrEmpty(id) OrElse String.IsNullOrEmpty(data) Then
                AddLog("TX hata: ID veya DATA boş.")
                Return
            End If

            ' Validate hex ID (3 or 8 characters for standard/extended)
            If Not System.Text.RegularExpressions.Regex.IsMatch(id, "^[0-9A-Fa-f]{3}$|^[0-9A-Fa-f]{8}$") Then
                AddLog("TX hata: ID geçersiz format (3 veya 8 hex karakter olmalı).")
                Return
            End If
            
            ' Validate hex data (must be pairs of hex characters)
            If Not System.Text.RegularExpressions.Regex.IsMatch(data, "^([0-9A-Fa-f]{2})+$") Then
                AddLog("TX hata: DATA geçersiz format (hex byte çiftleri olmalı).")
                Return
            End If
            
            ' Check data length (max 8 bytes = 16 hex chars)
            If data.Length > 16 Then
                AddLog("TX hata: DATA çok uzun (max 8 byte).")
                Return
            End If
            
            ' Check if connected
            If canSender Is Nothing OrElse Not canSender.IsConnected() Then
                AddLog("TX hata: CAN bağlantısı yok.")
                Return
            End If

            ' CANSender servisi ile gönder
            canSender.Send(id, data)

        Catch ex As Exception
            AddLog("TX hata: " & ex.Message)
            Debug.WriteLine($"Send error: {ex}")
        End Try
    End Sub
    Private Sub btnSaveLog_Click(sender As Object, e As EventArgs) Handles btnSaveLog.Click
        Try
            Dim sfd As New SaveFileDialog()
            sfd.Filter = "Text Files|*.txt"
            sfd.Title = "CAN Log Kaydet"

            If sfd.ShowDialog() = DialogResult.OK Then
                Dim lines As New List(Of String)

                For Each item As String In lstLog.Items
                    lines.Add(item)
                Next

                IO.File.WriteAllLines(sfd.FileName, lines)
                AddLog("Log başarıyla kaydedildi: " & sfd.FileName)
            End If

        Catch ex As Exception
            AddLog("Log kaydetme hatası: " & ex.Message)
        End Try
    End Sub
    Private Sub btnStartDiff_Click(sender As Object, e As EventArgs) Handles btnStartDiff.Click
        learningEngine.Reset()
        learningEngine.Start()
        lstDiff.Items.Clear()
        lblDiffStatus.Text = "🟢 Takip Durumu: Aktif"
        lblDiffStatus.ForeColor = Color.FromArgb(46, 204, 113)
        AddLog("Byte fark takibi açıldı.")
    End Sub

    Private Sub btnStopDiff_Click(sender As Object, e As EventArgs) Handles btnStopDiff.Click
        learningEngine.Stop()
        lblDiffStatus.Text = "⚪ Takip Durumu: Kapalı"
        lblDiffStatus.ForeColor = Color.Gray
        AddLog("Byte fark takibi kapatıldı.")
    End Sub

    Private Sub cmbBrand_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBrand.SelectedIndexChanged
        Try
            cmbModel.Items.Clear()
            cmbModule.Items.Clear()
            cmbCommand.Items.Clear()

            If cmbBrand.SelectedItem Is Nothing Then Return
            If Not commandRepository.IsLoaded Then Return
            
            Dim brandName As String = cmbBrand.SelectedItem.ToString()
            
            Dim brandObj = commandRepository.RawData(brandName)
            If brandObj Is Nothing OrElse brandObj.Type <> JTokenType.Object Then Return

            For Each model In CType(brandObj, JObject).Properties()
                cmbModel.Items.Add(model.Name)
            Next
        Catch ex As Exception
            AddLog($"Marka seçim hatası: {ex.Message}")
        End Try
    End Sub
    
    Private Sub cmbModel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbModel.SelectedIndexChanged
        Try
            cmbModule.Items.Clear()
            cmbCommand.Items.Clear()

            If cmbBrand.SelectedItem Is Nothing OrElse cmbModel.SelectedItem Is Nothing Then Return
            If Not commandRepository.IsLoaded Then Return
            
            Dim brand As String = cmbBrand.SelectedItem.ToString()
            Dim model As String = cmbModel.SelectedItem.ToString()

            Dim modelObj = commandRepository.RawData(brand)?(model)
            If modelObj Is Nothing OrElse modelObj.Type <> JTokenType.Object Then Return

            For Each moduleName In CType(modelObj, JObject).Properties()
                cmbModule.Items.Add(moduleName.Name)
            Next
        Catch ex As Exception
            AddLog($"Model seçim hatası: {ex.Message}")
        End Try
    End Sub
    
    Private Sub cmbModule_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbModule.SelectedIndexChanged
        Try
            cmbCommand.Items.Clear()
            txtCommandPreview.Text = ""

            If cmbBrand.SelectedItem Is Nothing OrElse 
               cmbModel.SelectedItem Is Nothing OrElse 
               cmbModule.SelectedItem Is Nothing Then Return
            If Not commandRepository.IsLoaded Then Return
            
            Dim brand As String = cmbBrand.SelectedItem.ToString()
            Dim model As String = cmbModel.SelectedItem.ToString()
            Dim moduleName As String = cmbModule.SelectedItem.ToString()

            Dim moduleObj = commandRepository.RawData(brand)?(model)?(moduleName)
            If moduleObj Is Nothing OrElse moduleObj.Type <> JTokenType.Object Then Return

            For Each commandName In CType(moduleObj, JObject).Properties()
                cmbCommand.Items.Add(commandName.Name)
            Next
        Catch ex As Exception
            AddLog($"Modül seçim hatası: {ex.Message}")
        End Try
    End Sub

    Private Sub cmbCommand_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCommand.SelectedIndexChanged
        Try
            If cmbBrand.SelectedItem Is Nothing OrElse
               cmbModel.SelectedItem Is Nothing OrElse
               cmbModule.SelectedItem Is Nothing OrElse
               cmbCommand.SelectedItem Is Nothing Then
                txtCommandPreview.Text = ""
                Return
            End If

            Dim brand As String = cmbBrand.SelectedItem.ToString()
            Dim model As String = cmbModel.SelectedItem.ToString()
            Dim moduleName As String = cmbModule.SelectedItem.ToString()
            Dim commandName As String = cmbCommand.SelectedItem.ToString()

            Dim frame As String = commandRepository.RawData(brand)(model)(moduleName)(commandName).ToString()
            txtCommandPreview.Text = "Komut: " & commandName & vbCrLf & "Frame: " & frame
        Catch ex As Exception
            txtCommandPreview.Text = "(Önizleme yüklenemedi)"
            Debug.WriteLine($"Command preview error: {ex.Message}")
        End Try
    End Sub
    Private Sub btnSendCommand_Click(sender As Object, e As EventArgs) Handles btnSendCommand.Click
        Try
            If cmbBrand.SelectedItem Is Nothing OrElse
               cmbModel.SelectedItem Is Nothing OrElse
               cmbModule.SelectedItem Is Nothing OrElse
               cmbCommand.SelectedItem Is Nothing Then
                AddLog("Hazır komut hata: Seçim eksik.")
                Return
            End If

            Dim brand As String = cmbBrand.SelectedItem.ToString()
            Dim model As String = cmbModel.SelectedItem.ToString()
            Dim moduleName As String = cmbModule.SelectedItem.ToString()
            Dim command As String = cmbCommand.SelectedItem.ToString()

            ' Frame string al ve gönder
            Dim frame As String = commandRepository.RawData(brand)(model)(moduleName)(command).ToString()
            canSender.SendRaw(frame)
            AddLog("TX (hazır komut): " & command & " → " & frame)

        Catch ex As Exception
            AddLog("Hazır komut hata: " & ex.Message)
        End Try
    End Sub
    Private Sub btnAnalyzeLog_Click(sender As Object, e As EventArgs) Handles btnAnalyzeLog.Click
        Try
            ' LogAnalyzer servisi ile analiz
            Dim frames = logAnalyzer.ExtractFrames(lstLog.Items)
            Dim idStats = logAnalyzer.CountIds(frames)
            Dim byteStats = logAnalyzer.ComputeByteStats(frames)

            lstIdStats.Items.Clear()
            txtAnalysisResult.Clear()

            If idStats.Count = 0 Then
                lstIdStats.Items.Add("Hiç RX frame bulunamadı.")
                txtAnalysisResult.Text = "❌ Log'da analiz edilecek RX frame bulunamadı." & vbCrLf & vbCrLf
                txtAnalysisResult.AppendText("💡 İpucu:" & vbCrLf)
                txtAnalysisResult.AppendText("1. CAN bağlantısı yapın" & vbCrLf)
                txtAnalysisResult.AppendText("2. Veya 'Test Veri' butonu ile örnek veri ekleyin" & vbCrLf)
                txtAnalysisResult.AppendText("3. Veya log dosyası yükleyin")
                AddLog("📊 Log analizi: RX frame yok.")
                Return
            End If

            ' ID istatistiklerini yazdır (sıralı)
            Dim sortedStats = logAnalyzer.GetSortedIdCounts(idStats)
            For Each kvp In sortedStats
                Dim line As String = "ID 0x" & kvp.Key.ToString("X3") & "  →  " & kvp.Value & " frame"
                lstIdStats.Items.Add(line)
            Next

            ' İçgörüleri al ve göster
            Dim insights = logAnalyzer.BuildInsights(idStats, byteStats)
            
            ' Sonuçları TextBox'a yaz
            txtAnalysisResult.AppendText("═══════════════════════════════════════" & vbCrLf)
            txtAnalysisResult.AppendText("  📊 CAN LOG ANALİZ RAPORU" & vbCrLf)
            txtAnalysisResult.AppendText("═══════════════════════════════════════" & vbCrLf & vbCrLf)

            txtAnalysisResult.AppendText("📈 GENEL İSTATİSTİKLER" & vbCrLf)
            txtAnalysisResult.AppendText("─────────────────────────────────────" & vbCrLf)
            txtAnalysisResult.AppendText("• Toplam frame sayısı: " & frames.Count & vbCrLf)
            txtAnalysisResult.AppendText("• Benzersiz ID sayısı: " & idStats.Count & vbCrLf & vbCrLf)

            txtAnalysisResult.AppendText("🔍 İÇGÖRÜLER" & vbCrLf)
            txtAnalysisResult.AppendText("─────────────────────────────────────" & vbCrLf)
            For Each insight In insights
                txtAnalysisResult.AppendText("• " & insight & vbCrLf)
            Next

            txtAnalysisResult.AppendText(vbCrLf & "═══════════════════════════════════════" & vbCrLf)

            ' Özet bilgi
            Dim summary = logAnalyzer.GetSummary(idStats, byteStats)
            AddLog("📊 " & summary)

        Catch ex As Exception
            AddLog("❌ Log analizi hatası: " & ex.Message)
        End Try
    End Sub




    ' NOTE: commandRepository.RawData field REMOVED - use commandRepository.RawData instead
    ' All JSON access must go through CommandRepository for atomic writes and validation
    
    ' JSON Editör Paneli verileri
    Private editSelectedBrand As String = ""
    Private editSelectedModel As String = ""
    Private editSelectedModule As String = ""

    ''' <summary>
    ''' Repository'den komut verilerini yükler ve tüm UI'ları günceller
    ''' Bu metod LoadCommandsJson yerine kullanılır
    ''' </summary>
    Public Sub RefreshCommandsUI()
        Try
            ' Repository zaten yüklü değilse yükle
            If Not commandRepository.IsLoaded Then
                commandRepository.Load()
            End If
            
            ' Tüm marka combobox'larını güncelle
            UpdateBrandComboBoxes()

            Dim brandCount = commandRepository.GetBrands().Count
            AddLog($"📋 Komut veritabanı yüklendi ({brandCount} marka).")

        Catch ex As Exception
            AddLog($"❌ Komut UI yenileme hatası: {ex.Message}")
            Debug.WriteLine($"RefreshCommandsUI error: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Tüm marka combobox'larını repository'den günceller
    ''' </summary>
    Private Sub UpdateBrandComboBoxes()
        Try
            Dim brands = commandRepository.GetBrands()
            
            ' --- KOMUT GÖNDERİCİ PANEL MARKALARI ---
            cmbBrand.Items.Clear()
            For Each brand In brands
                cmbBrand.Items.Add(brand)
            Next

            ' --- JSON EDİTÖR PANELİ MARKALARI ---
            cmbEditBrand.Items.Clear()
            For Each brand In brands
                cmbEditBrand.Items.Add(brand)
            Next

            ' --- CODING PANELİ MARKALARI ---
            cmbCodeBrand.Items.Clear()
            For Each brand In brands
                cmbCodeBrand.Items.Add(brand)
            Next
        Catch ex As Exception
            Debug.WriteLine($"UpdateBrandComboBoxes error: {ex.Message}")
        End Try
    End Sub

    Private Sub btnApplyFilter_Click(sender As Object, e As EventArgs) Handles btnApplyFilter.Click
        Try
            If cmbFilterMode.SelectedIndex = -1 Then
                AddLog("Filtre seçilmedi.")
                Return
            End If

            Dim selectedMode As String = cmbFilterMode.SelectedItem.ToString()

            Select Case selectedMode

                Case "Sadece bu ID"
                    If txtFilterId.Text.Trim() = "" Then
                        AddLog("Filtre uygulanamadı: ID boş.")
                        Return
                    End If
                    Dim idVal As Integer
                    If FilterManager.TryParseHexId(txtFilterId.Text, idVal) Then
                        filterManager.ApplyFilter(FilterMode.ShowOnlyId, idVal)
                    End If

                Case "Bu ID'yi gizle"
                    If txtFilterId.Text.Trim() = "" Then
                        AddLog("Filtre uygulanamadı: ID boş.")
                        Return
                    End If
                    Dim idVal As Integer
                    If FilterManager.TryParseHexId(txtFilterId.Text, idVal) Then
                        filterManager.ApplyFilter(FilterMode.HideId, idVal)
                    End If

                Case "ID aralığını göster"
                    If txtFilterFrom.Text.Trim() = "" Or txtFilterTo.Text.Trim() = "" Then
                        AddLog("Filtre uygulanamadı: aralık eksik.")
                        Return
                    End If
                    Dim fromVal, toVal As Integer
                    If FilterManager.TryParseHexId(txtFilterFrom.Text, fromVal) AndAlso
                       FilterManager.TryParseHexId(txtFilterTo.Text, toVal) Then
                        filterManager.ApplyRangeFilter(FilterMode.ShowRange, fromVal, toVal)
                    End If

                Case "ID aralığını gizle"
                    If txtFilterFrom.Text.Trim() = "" Or txtFilterTo.Text.Trim() = "" Then
                        AddLog("Filtre uygulanamadı: aralık eksik.")
                        Return
                    End If
                    Dim fromVal, toVal As Integer
                    If FilterManager.TryParseHexId(txtFilterFrom.Text, fromVal) AndAlso
                       FilterManager.TryParseHexId(txtFilterTo.Text, toVal) Then
                        filterManager.ApplyRangeFilter(FilterMode.HideRange, fromVal, toVal)
                    End If

            End Select

            AddLog("Filtre aktif: " & selectedMode)

        Catch ex As Exception
            AddLog("Filtre uygulama hatası: " & ex.Message)
        End Try
    End Sub

    Private Sub btnClearFilter_Click(sender As Object, e As EventArgs) Handles btnClearFilter.Click
        filterManager.ClearFilter()
        AddLog("Filtre kapatıldı.")
    End Sub

    Private Sub cmbEditBrand_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbEditBrand.SelectedIndexChanged
        Try
            cmbEditModel.Items.Clear()
            cmbEditModule.Items.Clear()
            lstEditCommands.Items.Clear()
            txtEditCommandName.Text = ""
            txtEditCommandFrame.Text = ""

            editSelectedBrand = cmbEditBrand.SelectedItem.ToString()

            ' Brand altındaki modelleri oku
            Dim brandObj As JObject = CType(commandRepository.RawData(editSelectedBrand), JObject)

            For Each modelProp As JProperty In brandObj.Properties()
                cmbEditModel.Items.Add(modelProp.Name)
            Next

        Catch ex As Exception
            AddLog("Editör – Model yükleme hata: " & ex.Message)
        End Try
    End Sub



    Private Sub cmbEditModel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbEditModel.SelectedIndexChanged
        Try
            cmbEditModule.Items.Clear()
            lstEditCommands.Items.Clear()
            txtEditCommandName.Text = ""
            txtEditCommandFrame.Text = ""

            editSelectedModel = cmbEditModel.SelectedItem.ToString()

            ' Seçilen brand + model altındaki modülleri oku
            Dim modelObj As JObject = CType(commandRepository.RawData(editSelectedBrand)(editSelectedModel), JObject)

            For Each moduleProp As JProperty In modelObj.Properties()
                cmbEditModule.Items.Add(moduleProp.Name)
            Next

        Catch ex As Exception
            AddLog("Editör – Modül yükleme hata: " & ex.Message)
        End Try
    End Sub


    Private Sub cmbEditModule_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbEditModule.SelectedIndexChanged
        Try
            lstEditCommands.Items.Clear()
            txtEditCommandName.Text = ""
            txtEditCommandFrame.Text = ""

            editSelectedModule = cmbEditModule.SelectedItem.ToString()

            ' Seçilen brand + model + modül altındaki komutları oku
            Dim moduleObj As JObject = CType(commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule), JObject)

            For Each cmdProp As JProperty In moduleObj.Properties()
                lstEditCommands.Items.Add(cmdProp.Name)
            Next

        Catch ex As Exception
            AddLog("Editör – Komut yükleme hata: " & ex.Message)
        End Try
    End Sub


    Private Sub lstEditCommands_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstEditCommands.SelectedIndexChanged
        Try
            If lstEditCommands.SelectedItem Is Nothing Then Return

            Dim cmdName As String = lstEditCommands.SelectedItem.ToString()
            txtEditCommandName.Text = cmdName

            Dim frame As String = commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule)(cmdName).ToString()

            txtEditCommandFrame.Text = frame

        Catch ex As Exception
            AddLog("Editör – Komut seçme hata: " & ex.Message)
        End Try
    End Sub
    Private Sub btnAddCommand_Click(sender As Object, e As EventArgs) Handles btnAddCommand.Click
        Try
            Dim name As String = txtEditCommandName.Text.Trim()
            Dim frame As String = txtEditCommandFrame.Text.Trim()

            If name = "" Or frame = "" Then
                AddLog("Editör – Boş alan var.")
                Return
            End If

            commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule)(name) = frame

            lstEditCommands.Items.Add(name)
            AddLog("Editör – Komut eklendi: " & name)

        Catch ex As Exception
            AddLog("Editör – Komut ekleme hata: " & ex.Message)
        End Try
    End Sub
    Private Sub btnUpdateCommand_Click(sender As Object, e As EventArgs) Handles btnUpdateCommand.Click



        Try
            If lstEditCommands.SelectedItem Is Nothing Then
                AddLog("Editör – Güncellenecek komut seçilmedi.")
                Return
            End If

            Dim oldName As String = lstEditCommands.SelectedItem.ToString()
            Dim newName As String = txtEditCommandName.Text.Trim()
            Dim newFrame As String = txtEditCommandFrame.Text.Trim()

            ' İsim değiştiyse yeniden eklememiz gerekiyor
            If oldName <> newName Then
                Dim moduleObj As JObject = CType(commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule), JObject)
                moduleObj.Remove(oldName)

                commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule)(newName) = newFrame

                Dim idx = lstEditCommands.SelectedIndex
                lstEditCommands.Items(idx) = newName
            Else
                commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule)(newName) = newFrame
            End If

            AddLog("Editör – Komut güncellendi: " & newName)

        Catch ex As Exception
            AddLog("Editör – Güncelleme hata: " & ex.Message)
        End Try
    End Sub
    Private Sub btnDeleteCommand_Click(sender As Object, e As EventArgs) Handles btnDeleteCommand.Click

        Try
            If lstEditCommands.SelectedItem Is Nothing Then Return

            Dim name As String = lstEditCommands.SelectedItem.ToString()

            Dim moduleObj As JObject = CType(commandRepository.RawData(editSelectedBrand)(editSelectedModel)(editSelectedModule), JObject)
            moduleObj.Remove(name)

            lstEditCommands.Items.Remove(name)

            txtEditCommandName.Text = ""
            txtEditCommandFrame.Text = ""

            AddLog("Editör – Komut silindi: " & name)

        Catch ex As Exception
            AddLog("Editör – Silme hata: " & ex.Message)
        End Try
    End Sub
    Private Sub btnSaveJson_Click(sender As Object, e As EventArgs) Handles btnSaveJson.Click
        Try
            ' ATOMIC SAVE via CommandRepository
            commandRepository.Save()
            AddLog("Editör – JSON başarıyla kaydedildi (atomic write).")

        Catch ex As Exception
            AddLog("Editör – JSON kaydetme hata: " & ex.Message)
        End Try
    End Sub

    ' ========================================
    ' CODING SYSTEM - Gizli Özellik Yönetimi
    ' ========================================

    ' Coding paneli için seçili değerler
    Private codeSelectedBrand As String = ""
    Private codeSelectedModel As String = ""
    Private codeSelectedModule As String = ""
    Private codeSelectedCommand As String = ""
    Private codeCurrentFrame As Byte() = Nothing

    ' Coding panel marka seçimi
    Private Sub cmbCodeBrand_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCodeBrand.SelectedIndexChanged
        Try
            cmbCodeModel.Items.Clear()
            cmbCodeModule.Items.Clear()
            lstCodeCommands.Items.Clear()
            txtCodePreview.Text = ""
            codeCurrentFrame = Nothing

            If cmbCodeBrand.SelectedItem Is Nothing Then Return

            codeSelectedBrand = cmbCodeBrand.SelectedItem.ToString()

            ' Modelleri yükle
            Dim models = codingEngine.LoadAvailableCommands(commandRepository.RawData, codeSelectedBrand, "", "")
            If commandRepository.RawData(codeSelectedBrand) IsNot Nothing Then
                Dim brandObj As JObject = CType(commandRepository.RawData(codeSelectedBrand), JObject)
                For Each modelProp As JProperty In brandObj.Properties()
                    cmbCodeModel.Items.Add(modelProp.Name)
                Next
            End If

        Catch ex As Exception
            AddLog("Coding – Marka seçim hata: " & ex.Message)
        End Try
    End Sub

    ' Coding panel model seçimi
    Private Sub cmbCodeModel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCodeModel.SelectedIndexChanged
        Try
            cmbCodeModule.Items.Clear()
            lstCodeCommands.Items.Clear()
            txtCodePreview.Text = ""
            codeCurrentFrame = Nothing

            If cmbCodeModel.SelectedItem Is Nothing Then Return

            codeSelectedModel = cmbCodeModel.SelectedItem.ToString()

            ' Modülleri yükle
            If commandRepository.RawData(codeSelectedBrand)?(codeSelectedModel) IsNot Nothing Then
                Dim modelObj As JObject = CType(commandRepository.RawData(codeSelectedBrand)(codeSelectedModel), JObject)
                For Each moduleProp As JProperty In modelObj.Properties()
                    cmbCodeModule.Items.Add(moduleProp.Name)
                Next
            End If

        Catch ex As Exception
            AddLog("Coding – Model seçim hata: " & ex.Message)
        End Try
    End Sub

    ' Coding panel modül seçimi
    Private Sub cmbCodeModule_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCodeModule.SelectedIndexChanged
        Try
            lstCodeCommands.Items.Clear()
            txtCodePreview.Text = ""
            codeCurrentFrame = Nothing

            If cmbCodeModule.SelectedItem Is Nothing Then Return

            codeSelectedModule = cmbCodeModule.SelectedItem.ToString()

            ' Komutları yükle (CodingEngine kullanarak)
            Dim commands = codingEngine.LoadAvailableCommands(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule)
            For Each cmd In commands
                lstCodeCommands.Items.Add(cmd)
            Next

        Catch ex As Exception
            AddLog("Coding – Modül seçim hata: " & ex.Message)
        End Try
    End Sub

    ' Coding panel komut seçimi
    Private Sub lstCodeCommands_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstCodeCommands.SelectedIndexChanged
        Try
            If lstCodeCommands.SelectedItem Is Nothing Then Return

            codeSelectedCommand = lstCodeCommands.SelectedItem.ToString()

            ' CodingEngine ile frame al
            codeCurrentFrame = codingEngine.GetCommandFrame(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand)

            ' Komut tipini kontrol et
            Dim isToggle = codingEngine.IsToggleCommand(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand)

            ' Önizlemeyi güncelle
            UpdateCodingPreview()

            ' Toggle ise byte index'i al
            If isToggle Then
                Dim byteIdx = codingEngine.GetToggleByteIndex(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand)
                If byteIdx >= 0 Then
                    txtCodeByteIndex.Text = byteIdx.ToString()
                End If
                If cmbCodeType.Items.Contains("Toggle") Then
                    cmbCodeType.SelectedItem = "Toggle"
                End If
            Else
                If cmbCodeType.Items.Contains("Single") Then
                    cmbCodeType.SelectedItem = "Single"
                End If
            End If

            ' ID'yi ayarla (frame'den çıkar)
            Dim cmdToken = commandRepository.RawData(codeSelectedBrand)?(codeSelectedModel)?(codeSelectedModule)?(codeSelectedCommand)
            If cmdToken IsNot Nothing Then
                Dim cmdStr = cmdToken.ToString()
                If cmdStr.StartsWith("t") AndAlso cmdStr.Length >= 4 Then
                    txtCodeID.Text = cmdStr.Substring(1, 3)
                End If
            End If

        Catch ex As Exception
            AddLog("Coding – Komut seçim hata: " & ex.Message)
        End Try
    End Sub

    ' Önizleme güncelleme
    Private Sub UpdateCodingPreview()
        Try
            If codeCurrentFrame Is Nothing OrElse codeCurrentFrame.Length = 0 Then
                txtCodePreview.Text = "(Frame yok)"
                Return
            End If

            ' Byte index varsa highlight et
            Dim highlightIdx As Integer = -1
            If Not String.IsNullOrWhiteSpace(txtCodeByteIndex.Text) Then
                Integer.TryParse(txtCodeByteIndex.Text, highlightIdx)
            End If

            ' CodingEngine ile önizleme oluştur
            Dim preview = codingEngine.BuildPreviewWithId(txtCodeID.Text, codeCurrentFrame, highlightIdx)
            txtCodePreview.Text = preview

        Catch ex As Exception
            txtCodePreview.Text = "(Önizleme hatası)"
        End Try
    End Sub

    ' ON butonu - Toggle açık
    Private Sub btnCodingOn_Click(sender As Object, e As EventArgs) Handles btnCodingOn.Click
        Try
            If codeCurrentFrame Is Nothing OrElse codeCurrentFrame.Length = 0 Then
                AddLog("Coding – Frame yüklenmedi.")
                Return
            End If

            ' Byte index'i al
            Dim byteIdx As Integer = -1
            If Not String.IsNullOrWhiteSpace(txtCodeByteIndex.Text) Then
                Integer.TryParse(txtCodeByteIndex.Text, byteIdx)
            End If

            Dim frameToSend As Byte()

            ' Toggle komut mu kontrol et
            If codingEngine.IsToggleCommand(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand) Then
                ' Toggle ON frame'i al
                frameToSend = codingEngine.GetToggleOnFrame(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand)
            ElseIf byteIdx >= 0 Then
                ' Manuel byte değiştirme ile ON frame oluştur
                frameToSend = codingEngine.BuildOnOffFrame(codeCurrentFrame, byteIdx, True)
            Else
                ' Doğrudan mevcut frame'i gönder
                frameToSend = codeCurrentFrame
            End If

            ' SLCAN frame oluştur ve gönder
            Dim slcanFrame = codingEngine.BuildSlcanFrame(txtCodeID.Text, frameToSend)
            If Not String.IsNullOrWhiteSpace(slcanFrame) Then
                canSender.SendRaw(slcanFrame)
                AddLog("Coding ON: " & codeSelectedCommand & " → " & slcanFrame)
            Else
                AddLog("Coding – Frame oluşturulamadı.")
            End If

        Catch ex As Exception
            AddLog("Coding ON hata: " & ex.Message)
        End Try
    End Sub

    ' OFF butonu - Toggle kapalı
    Private Sub btnCodingOff_Click(sender As Object, e As EventArgs) Handles btnCodingOff.Click
        Try
            If codeCurrentFrame Is Nothing OrElse codeCurrentFrame.Length = 0 Then
                AddLog("Coding – Frame yüklenmedi.")
                Return
            End If

            ' Byte index'i al
            Dim byteIdx As Integer = -1
            If Not String.IsNullOrWhiteSpace(txtCodeByteIndex.Text) Then
                Integer.TryParse(txtCodeByteIndex.Text, byteIdx)
            End If

            Dim frameToSend As Byte()

            ' Toggle komut mu kontrol et
            If codingEngine.IsToggleCommand(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand) Then
                ' Toggle OFF frame'i al
                frameToSend = codingEngine.GetToggleOffFrame(commandRepository.RawData, codeSelectedBrand, codeSelectedModel, codeSelectedModule, codeSelectedCommand)
            ElseIf byteIdx >= 0 Then
                ' Manuel byte değiştirme ile OFF frame oluştur
                frameToSend = codingEngine.BuildOnOffFrame(codeCurrentFrame, byteIdx, False)
            Else
                ' OFF için frame oluşturulamaz
                AddLog("Coding – OFF için byte index gerekli.")
                Return
            End If

            ' SLCAN frame oluştur ve gönder
            Dim slcanFrame = codingEngine.BuildSlcanFrame(txtCodeID.Text, frameToSend)
            If Not String.IsNullOrWhiteSpace(slcanFrame) Then
                canSender.SendRaw(slcanFrame)
                AddLog("Coding OFF: " & codeSelectedCommand & " → " & slcanFrame)
            Else
                AddLog("Coding – Frame oluşturulamadı.")
            End If

        Catch ex As Exception
            AddLog("Coding OFF hata: " & ex.Message)
        End Try
    End Sub

    ' Gönder butonu - Manuel frame gönderimi
    Private Sub btnCodeSend_Click(sender As Object, e As EventArgs) Handles btnCodeSend.Click
        Try
            Dim id As String = txtCodeID.Text.Trim()
            Dim data As String = txtCodeData.Text.Trim()

            ' ID kontrolü
            If String.IsNullOrWhiteSpace(id) Then
                AddLog("Coding – ID boş.")
                Return
            End If

            Dim frameToSend As Byte()

            ' Data varsa onu kullan, yoksa mevcut frame'i kullan
            If Not String.IsNullOrWhiteSpace(data) Then
                frameToSend = codingEngine.SafeParseHexArray(data)
            ElseIf codeCurrentFrame IsNot Nothing AndAlso codeCurrentFrame.Length > 0 Then
                ' Byte index varsa değiştir
                Dim byteIdx As Integer = -1
                If Not String.IsNullOrWhiteSpace(txtCodeByteIndex.Text) Then
                    Integer.TryParse(txtCodeByteIndex.Text, byteIdx)
                End If

                If byteIdx >= 0 AndAlso Not String.IsNullOrWhiteSpace(txtCodeData.Text) Then
                    frameToSend = codingEngine.BuildModifiedFrame(codeCurrentFrame, byteIdx, txtCodeData.Text)
                Else
                    frameToSend = codeCurrentFrame
                End If
            Else
                AddLog("Coding – Data veya seçili komut gerekli.")
                Return
            End If

            ' SLCAN frame oluştur ve gönder
            Dim slcanFrame = codingEngine.BuildSlcanFrame(id, frameToSend)
            If Not String.IsNullOrWhiteSpace(slcanFrame) Then
                canSender.SendRaw(slcanFrame)
                AddLog("Coding TX: " & slcanFrame)
            Else
                AddLog("Coding – Frame oluşturulamadı.")
            End If

        Catch ex As Exception
            AddLog("Coding Send hata: " & ex.Message)
        End Try
    End Sub

    ' Komut kaydetme
    Private Sub SaveCodingCommand()
        Try
            Dim id As String = txtCodeID.Text.Trim()
            Dim data As String = txtCodeData.Text.Trim()
            Dim byteIdxStr As String = txtCodeByteIndex.Text.Trim()

            ' Doğrulama
            If String.IsNullOrWhiteSpace(codeSelectedBrand) OrElse
               String.IsNullOrWhiteSpace(codeSelectedModel) OrElse
               String.IsNullOrWhiteSpace(codeSelectedModule) Then
                AddLog("Coding – Kaydetmek için marka/model/modül seçin.")
                Return
            End If

            If String.IsNullOrWhiteSpace(id) Then
                AddLog("Coding – ID boş.")
                Return
            End If

            ' Komut adı oluştur (varsa seçili, yoksa yeni)
            Dim cmdName As String = codeSelectedCommand
            If String.IsNullOrWhiteSpace(cmdName) Then
                cmdName = "NewCommand_" & DateTime.Now.ToString("HHmmss")
            End If

            ' Komut tipine göre kaydet
            Dim cmdType As String = ""
            If cmbCodeType.SelectedItem IsNot Nothing Then
                cmdType = cmbCodeType.SelectedItem.ToString()
            End If

            If cmdType = "Toggle" AndAlso Not String.IsNullOrWhiteSpace(byteIdxStr) Then
                ' Toggle komut olarak kaydet
                Dim byteIdx As Integer
                If Integer.TryParse(byteIdxStr, byteIdx) Then
                    ' ON ve OFF frame'leri oluştur
                    Dim baseFrame = codingEngine.SafeParseHexArray(data)
                    If baseFrame.Length = 0 AndAlso codeCurrentFrame IsNot Nothing Then
                        baseFrame = codeCurrentFrame
                    End If

                    Dim onFrame = codingEngine.BuildOnOffFrame(baseFrame, byteIdx, True)
                    Dim offFrame = codingEngine.BuildOnOffFrame(baseFrame, byteIdx, False)

                    Dim onSlcan = codingEngine.BuildSlcanFrame(id, onFrame)
                    Dim offSlcan = codingEngine.BuildSlcanFrame(id, offFrame)

                    ' Toggle objesi oluştur
                    Dim toggleObj As New JObject()
                    toggleObj("type") = "toggle"
                    toggleObj("on") = onSlcan
                    toggleObj("off") = offSlcan
                    toggleObj("byte") = byteIdx

                    ' Yolu oluştur ve kaydet
                    EnsureCodePath()
                    Dim moduleObj As JObject = CType(commandRepository.RawData(codeSelectedBrand)(codeSelectedModel)(codeSelectedModule), JObject)
                    moduleObj(cmdName) = toggleObj

                    AddLog("Coding – Toggle komut kaydedildi: " & cmdName)
                End If
            Else
                ' Tekli frame olarak kaydet
                Dim frameBytes = codingEngine.SafeParseHexArray(data)
                If frameBytes.Length = 0 AndAlso codeCurrentFrame IsNot Nothing Then
                    frameBytes = codeCurrentFrame
                End If

                Dim slcanFrame = codingEngine.BuildSlcanFrame(id, frameBytes)

                ' Yolu oluştur ve kaydet
                EnsureCodePath()
                commandRepository.RawData(codeSelectedBrand)(codeSelectedModel)(codeSelectedModule)(cmdName) = slcanFrame

                AddLog("Coding – Komut kaydedildi: " & cmdName)
            End If

            ' Listeyi güncelle
            If Not lstCodeCommands.Items.Contains(cmdName) Then
                lstCodeCommands.Items.Add(cmdName)
            End If

            ' JSON dosyasını ATOMIC olarak kaydet
            commandRepository.Save()
            AddLog("Coding – Komut kaydedildi (atomic write).")

        Catch ex As Exception
            AddLog("Coding kaydetme hata: " & ex.Message)
        End Try
    End Sub

    ' Coding path oluşturma
    Private Sub EnsureCodePath()
        ' Brand yoksa oluştur
        If commandRepository.RawData(codeSelectedBrand) Is Nothing Then
            commandRepository.RawData(codeSelectedBrand) = New JObject()
        End If

        ' Model yoksa oluştur
        Dim brandObj = CType(commandRepository.RawData(codeSelectedBrand), JObject)
        If brandObj(codeSelectedModel) Is Nothing Then
            brandObj(codeSelectedModel) = New JObject()
        End If

        ' Module yoksa oluştur
        Dim modelObj = CType(brandObj(codeSelectedModel), JObject)
        If modelObj(codeSelectedModule) Is Nothing Then
            modelObj(codeSelectedModule) = New JObject()
        End If
    End Sub

    ' Log'dan komut kaydetme butonu
    Private Sub btnCodeSaveFromLog_Click(sender As Object, e As EventArgs) Handles btnCodeSaveFromLog.Click
        SaveCodingCommand()
    End Sub

    ' Byte index değiştiğinde önizlemeyi güncelle
    Private Sub txtCodeByteIndex_TextChanged(sender As Object, e As EventArgs) Handles txtCodeByteIndex.TextChanged
        UpdateCodingPreview()
    End Sub

    ' Data değiştiğinde önizlemeyi güncelle
    Private Sub txtCodeData_TextChanged(sender As Object, e As EventArgs) Handles txtCodeData.TextChanged
        Try
            ' Data girildiyse frame'i güncelle
            If Not String.IsNullOrWhiteSpace(txtCodeData.Text) Then
                Dim newFrame = codingEngine.SafeParseHexArray(txtCodeData.Text)
                If newFrame.Length > 0 Then
                    codeCurrentFrame = newFrame
                    UpdateCodingPreview()
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine($"Code data text changed error: {ex.Message}")
        End Try
    End Sub

    ' ========================================
    ' YENİ UI KONTROL EVENT HANDLERLARI
    ' ========================================

    ' Log temizleme
    Private Sub btnClearLog_Click(sender As Object, e As EventArgs) Handles btnClearLog.Click
        lstLog.Items.Clear()
        AddLog("Log temizlendi.")
    End Sub

    ' Log dosyası yükleme
    Private Sub btnLoadLogFile_Click(sender As Object, e As EventArgs) Handles btnLoadLogFile.Click
        Try
            Dim ofd As New OpenFileDialog()
            ofd.Filter = "Text Files|*.txt|Log Files|*.log|All Files|*.*"
            ofd.Title = "CAN Log Dosyası Aç"

            If ofd.ShowDialog() = DialogResult.OK Then
                Dim lines = IO.File.ReadAllLines(ofd.FileName)
                lstLog.Items.Clear()

                For Each line In lines
                    lstLog.Items.Add(line)
                Next

                AddLog("Log dosyası yüklendi: " & IO.Path.GetFileName(ofd.FileName) & " (" & lines.Length & " satır)")
            End If
        Catch ex As Exception
            AddLog("Log dosyası yükleme hatası: " & ex.Message)
        End Try
    End Sub

    ' Learning verisini dışa aktar
    Private Sub btnExportLearning_Click(sender As Object, e As EventArgs) Handles btnExportLearning.Click
        Try
            If lstDiff.Items.Count = 0 Then
                AddLog("Dışa aktarılacak veri yok.")
                Return
            End If

            Dim sfd As New SaveFileDialog()
            sfd.Filter = "Text Files|*.txt|CSV Files|*.csv"
            sfd.Title = "Öğrenme Verisini Kaydet"
            sfd.FileName = "learning_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")

            If sfd.ShowDialog() = DialogResult.OK Then
                Dim lines As New List(Of String)
                lines.Add("=== CAN Learning Engine Export ===")
                lines.Add("Tarih: " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                lines.Add("Toplam değişiklik: " & lstDiff.Items.Count)
                lines.Add("")

                For Each item In lstDiff.Items
                    lines.Add(item.ToString())
                Next

                IO.File.WriteAllLines(sfd.FileName, lines)
                AddLog("Öğrenme verisi kaydedildi: " & IO.Path.GetFileName(sfd.FileName))
            End If
        Catch ex As Exception
            AddLog("Dışa aktarma hatası: " & ex.Message)
        End Try
    End Sub

    ' Learning verisini temizle
    Private Sub btnClearLearning_Click(sender As Object, e As EventArgs) Handles btnClearLearning.Click
        lstDiff.Items.Clear()
        learningEngine.Reset()
        lblDiffStatus.Text = "⚪ Takip Durumu: Temizlendi"
        AddLog("Öğrenme verisi temizlendi.")
    End Sub

    ' Learning başlatma güncellemesi
    Private Sub UpdateLearningStatus()
        If learningEngine.IsRunning Then
            lblDiffStatus.Text = "🟢 Takip Durumu: Aktif"
            lblDiffStatus.ForeColor = Color.FromArgb(46, 204, 113)
        Else
            lblDiffStatus.Text = "⚪ Takip Durumu: Kapalı"
            lblDiffStatus.ForeColor = Color.Gray
        End If
    End Sub

    ' Yeni marka ekleme
    Private Sub btnNewBrand_Click(sender As Object, e As EventArgs) Handles btnNewBrand.Click
        Try
            Dim brandName = InputBox("Yeni marka adı girin:", "Yeni Marka", "")
            If String.IsNullOrWhiteSpace(brandName) Then Return

            ' Var mı kontrol et
            If commandRepository.RawData(brandName) IsNot Nothing Then
                AddLog("Editör – Bu marka zaten mevcut: " & brandName)
                Return
            End If

            ' Yeni boş marka oluştur
            commandRepository.RawData(brandName) = New JObject()

            ' Combobox'lara ekle
            cmbBrand.Items.Add(brandName)
            cmbEditBrand.Items.Add(brandName)
            cmbCodeBrand.Items.Add(brandName)

            ' Seç
            cmbEditBrand.SelectedItem = brandName

            AddLog("Editör – Yeni marka oluşturuldu: " & brandName)
        Catch ex As Exception
            AddLog("Editör – Marka oluşturma hatası: " & ex.Message)
        End Try
    End Sub

    ' Yeni model ekleme
    Private Sub btnNewModel_Click(sender As Object, e As EventArgs) Handles btnNewModel.Click
        Try
            If String.IsNullOrWhiteSpace(editSelectedBrand) Then
                AddLog("Editör – Önce marka seçin.")
                Return
            End If

            Dim modelName = InputBox("Yeni model adı girin:", "Yeni Model", "")
            If String.IsNullOrWhiteSpace(modelName) Then Return

            ' Var mı kontrol et
            Dim brandObj = CType(commandRepository.RawData(editSelectedBrand), JObject)
            If brandObj(modelName) IsNot Nothing Then
                AddLog("Editör – Bu model zaten mevcut: " & modelName)
                Return
            End If

            ' Yeni boş model oluştur
            brandObj(modelName) = New JObject()

            ' Combobox'a ekle ve seç
            cmbEditModel.Items.Add(modelName)
            cmbEditModel.SelectedItem = modelName

            AddLog("Editör – Yeni model oluşturuldu: " & modelName)
        Catch ex As Exception
            AddLog("Editör – Model oluşturma hatası: " & ex.Message)
        End Try
    End Sub

    ' Yeni modül ekleme
    Private Sub btnNewModule_Click(sender As Object, e As EventArgs) Handles btnNewModule.Click
        Try
            If String.IsNullOrWhiteSpace(editSelectedBrand) OrElse String.IsNullOrWhiteSpace(editSelectedModel) Then
                AddLog("Editör – Önce marka ve model seçin.")
                Return
            End If

            Dim moduleName = InputBox("Yeni modül adı girin:", "Yeni Modül", "")
            If String.IsNullOrWhiteSpace(moduleName) Then Return

            ' Var mı kontrol et
            Dim modelObj = CType(commandRepository.RawData(editSelectedBrand)(editSelectedModel), JObject)
            If modelObj(moduleName) IsNot Nothing Then
                AddLog("Editör – Bu modül zaten mevcut: " & moduleName)
                Return
            End If

            ' Yeni boş modül oluştur
            modelObj(moduleName) = New JObject()

            ' Combobox'a ekle ve seç
            cmbEditModule.Items.Add(moduleName)
            cmbEditModule.SelectedItem = moduleName

            AddLog("Editör – Yeni modül oluşturuldu: " & moduleName)
        Catch ex As Exception
            AddLog("Editör – Modül oluşturma hatası: " & ex.Message)
        End Try
    End Sub

    ' Diff takibi başlatma/durdurma durumu güncellemesi
    Private Sub btnStartDiff_Updated(sender As Object, e As EventArgs)
        UpdateLearningStatus()
    End Sub

#Region "Connection Wizard Integration"

    ''' <summary>
    ''' İlk açılışta veya kayıtlı bağlantı yoksa wizard göster
    ''' </summary>
    Private Sub CheckAndShowWizardIfNeeded()
        Try
            ' Son bağlantı bilgilerini kontrol et
            Dim lastPort = Config.Serial.LastPort
            Dim lastBaudRate = Config.Serial.LastBaudRate
            Dim lastProtocol = Config.Serial.LastProtocol
            Dim lastSuccess = Config.Serial.LastConnectionSuccessful

            ' Eğer kayıtlı bağlantı yoksa veya başarısızsa wizard göster
            If String.IsNullOrEmpty(lastPort) OrElse Not lastSuccess Then
                ' İlk açılış - wizard göster
                Dim result = MessageBox.Show(
                    "Bağlantı kurulum sihirbazını başlatmak ister misiniz?" & vbCrLf &
                    "Wizard size adım adım bağlantı kurmanızda yardımcı olacak.",
                    "Bağlantı Wizard'ı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question)

                If result = DialogResult.Yes Then
                    ShowConnectionWizard()
                End If
            Else
                ' Kayıtlı bağlantı var - otomatik bağlanmayı dene
                TryQuickConnect()
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.CheckAndShowWizardIfNeeded")
        End Try
    End Sub

    ''' <summary>
    ''' Bağlantı wizard'ını göster
    ''' </summary>
    Private Sub ShowConnectionWizard()
        Try
            Dim wizard As New ConnectionWizard()
            If wizard.ShowDialog() = DialogResult.OK Then
                ' Wizard'dan gelen bilgilerle bağlan
                If wizard.ConnectionSuccessful AndAlso Not String.IsNullOrEmpty(wizard.SelectedPort) Then
                    ConnectWithWizardSettings(wizard.SelectedPort, wizard.SelectedBaudRate, wizard.DetectedProtocolName)
                    
                    ' Bağlantı bilgilerini kaydet
                    If wizard.ConnectionSuccessful Then
                        SaveConnectionInfo(wizard.SelectedPort, wizard.SelectedBaudRate, wizard.DetectedProtocolName)
                    End If
                End If
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.ShowConnectionWizard")
            MessageBox.Show($"Wizard açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Hızlı bağlan (son başarılı bağlantıyı kullan)
    ''' </summary>
    Private Sub TryQuickConnect()
        Try
            Dim lastPort = Config.Serial.LastPort
            Dim lastBaudRate = Config.Serial.LastBaudRate
            Dim lastProtocol = Config.Serial.LastProtocol

            If String.IsNullOrEmpty(lastPort) Then
                Return
            End If

            AddLog($"🔄 Son bağlantı deneniyor: {lastPort} ({lastBaudRate})")

            ' 5 saniye timeout ile bağlan
            Dim connectTask = Task.Run(Sub()
                                           Try
                                               If SerialPort1 IsNot Nothing AndAlso SerialPort1.IsOpen Then
                                                   SerialPort1.Close()
                                                   Threading.Thread.Sleep(100)
                                               End If

                                               SerialPort1.PortName = lastPort
                                               SerialPort1.BaudRate = lastBaudRate
                                               SerialPort1.ReadTimeout = 2000
                                               SerialPort1.WriteTimeout = 2000
                                               SerialPort1.DtrEnable = True
                                               SerialPort1.RtsEnable = True

                                               SerialPort1.Open()
                                               Threading.Thread.Sleep(100)
                                               SerialPort1.WriteLine("O")
                                               Threading.Thread.Sleep(100)

                                               ' Başarılı
                                               Me.Invoke(Sub()
                                                            UpdateConnectionStatus(True, lastPort, lastBaudRate.ToString())
                                                            _lastDataReceivedTime = DateTime.Now
                                                            AddLog($"✅ Hızlı bağlantı başarılı: {lastPort}")
                                                        End Sub)
                                           Catch ex As Exception
                                               ' Başarısız - wizard öner
                                               Me.Invoke(Sub()
                                                            UpdateConnectionStatus(False, "", "")
                                                            AddLog($"❌ Hızlı bağlantı başarısız: {ex.Message}")
                                                            SuggestWizard("Hızlı bağlantı başarısız oldu")
                                                        End Sub)
                                           End Try
                                       End Sub)

            ' 5 saniye timeout
            If Not connectTask.Wait(5000) Then
                AddLog("⏱️ Hızlı bağlantı timeout")
                UpdateConnectionStatus(False, "", "")
                SuggestWizard("Bağlantı timeout")
            End If

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.TryQuickConnect")
            SuggestWizard("Hızlı bağlantı hatası")
        End Try
    End Sub

    ''' <summary>
    ''' Wizard ayarlarıyla bağlan
    ''' </summary>
    Private Sub ConnectWithWizardSettings(port As String, baudRate As Integer, protocol As String)
        Try
            If SerialPort1 IsNot Nothing AndAlso SerialPort1.IsOpen Then
                SerialPort1.Close()
                Threading.Thread.Sleep(100)
            End If

            SerialPort1.PortName = port
            SerialPort1.BaudRate = baudRate
            SerialPort1.ReadTimeout = 2000
            SerialPort1.WriteTimeout = 2000
            SerialPort1.DtrEnable = True
            SerialPort1.RtsEnable = True

            SerialPort1.Open()
            Threading.Thread.Sleep(100)
            SerialPort1.WriteLine("O")
            Threading.Thread.Sleep(100)

            ' Protokol hızını ayarla (varsa)
            If Not String.IsNullOrEmpty(protocol) Then
                If protocol.Contains("500K") Then
                    SerialPort1.WriteLine("S5")
                ElseIf protocol.Contains("250K") Then
                    SerialPort1.WriteLine("S3")
                ElseIf protocol.Contains("125K") Then
                    SerialPort1.WriteLine("S2")
                End If
            End If

            UpdateConnectionStatus(True, port, baudRate.ToString())
            _lastDataReceivedTime = DateTime.Now ' İlk bağlantı zamanını kaydet
            AddLog($"✅ Wizard ile bağlandı: {port} ({baudRate}, {protocol})")

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.ConnectWithWizardSettings")
            UpdateConnectionStatus(False, "", "")
            AddLog($"❌ Wizard bağlantısı başarısız: {ex.Message}")
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Bağlantı bilgilerini kaydet
    ''' </summary>
    Private Sub SaveConnectionInfo(port As String, baudRate As Integer, protocol As String)
        Try
            Config.Serial.LastPort = port
            Config.Serial.LastBaudRate = baudRate
            Config.Serial.LastProtocol = protocol
            Config.Serial.LastConnectionSuccessful = True
            Config.Serial.LastConnectionTime = DateTime.Now
            Config.Save()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.SaveConnectionInfo")
        End Try
    End Sub

    ''' <summary>
    ''' Bağlantı durumunu güncelle
    ''' </summary>
    Private Sub UpdateConnectionStatus(isConnected As Boolean, port As String, baudRate As String)
        Try
            If isConnected AndAlso Not String.IsNullOrEmpty(port) Then
                Dim protocol = Config.Serial.LastProtocol
                If Not String.IsNullOrEmpty(protocol) Then
                    lblStatus.Text = $"🟢 Bağlı ({port}, {baudRate}, {protocol})"
                Else
                    lblStatus.Text = $"🟢 Bağlı ({port}, {baudRate})"
                End If
                lblStatus.ForeColor = Color.FromArgb(46, 204, 113)
            Else
                lblStatus.Text = "🔴 Bağlı Değil"
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60)
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.UpdateConnectionStatus")
        End Try
    End Sub

    ''' <summary>
    ''' Bağlantı başarısız olursa wizard öner
    ''' </summary>
    Private Sub SuggestWizard(reason As String)
        Try
            Dim result = MessageBox.Show(
                $"Bağlantı kurulamadı: {reason}" & vbCrLf & vbCrLf &
                "Bağlantı kurulum sihirbazını kullanmak ister misiniz?",
                "Bağlantı Hatası",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ShowConnectionWizard()
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.SuggestWizard")
        End Try
    End Sub

    ''' <summary>
    ''' Wizard butonu tıklama
    ''' </summary>
    Private Sub btnConnectionWizard_Click(sender As Object, e As EventArgs) Handles btnConnectionWizard.Click
        ShowConnectionWizard()
    End Sub

    ''' <summary>
    ''' Hızlı bağlan butonu
    ''' </summary>
    Private Sub btnQuickConnect_Click(sender As Object, e As EventArgs) Handles btnQuickConnect.Click
        TryQuickConnect()
    End Sub

    ''' <summary>
    ''' Bağlantı izleme timer'ını başlat
    ''' </summary>
    Private Sub InitializeConnectionMonitor()
        Try
            _connectionMonitorTimer = New System.Windows.Forms.Timer()
            _connectionMonitorTimer.Interval = 5000 ' 5 saniye
            AddHandler _connectionMonitorTimer.Tick, AddressOf ConnectionMonitor_Tick
            _connectionMonitorTimer.Start()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.InitializeConnectionMonitor")
        End Try
    End Sub

    ''' <summary>
    ''' Bağlantı izleme timer tick
    ''' </summary>
    Private Sub ConnectionMonitor_Tick(sender As Object, e As EventArgs)
        Try
            ' Eğer port açıksa ama 10 saniyedir veri gelmiyorsa bağlantı kopmuş olabilir
            If SerialPort1 IsNot Nothing AndAlso SerialPort1.IsOpen Then
                If _lastDataReceivedTime <> DateTime.MinValue Then
                    Dim timeSinceLastData = (DateTime.Now - _lastDataReceivedTime).TotalSeconds
                    If timeSinceLastData > 10 Then
                        ' Bağlantı kopmuş olabilir
                        AddLog("⚠️ Bağlantı kopmuş olabilir (10 saniyedir veri gelmiyor)")
                        UpdateConnectionStatus(False, "", "")
                        
                        ' Port'u kapat
                        Try
                            SerialPort1.Close()
                        Catch
                        End Try
                        
                        ' Yeniden bağlan seçeneği sun
                        Dim result = MessageBox.Show(
                            "Bağlantı kopmuş görünüyor. Yeniden bağlanmak ister misiniz?",
                            "Bağlantı Koptu",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning)

                        If result = DialogResult.Yes Then
                            TryQuickConnect()
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.ConnectionMonitor_Tick")
        End Try
    End Sub

#End Region

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        Try
            If _connectionMonitorTimer IsNot Nothing Then
                _connectionMonitorTimer.Stop()
                _connectionMonitorTimer.Dispose()
            End If
        Catch
        End Try
        MyBase.OnFormClosing(e)
    End Sub

#Region "Chart Controls"

    ''' <summary>
    ''' Grafik duraklat/devam butonu
    ''' </summary>
    Private Sub BtnChartPause_Click(sender As Object, e As EventArgs) Handles btnChartPause.Click
        Try
            If liveChartRPM IsNot Nothing AndAlso Not liveChartRPM.IsPaused Then
                liveChartRPM.Pause()
                liveChartSpeed.Pause()
                liveChartTemp.Pause()
                btnChartPause.Text = "▶️ Devam"
                btnChartPause.BackColor = Color.FromArgb(46, 204, 113)
            Else
                liveChartRPM.Resume()
                liveChartSpeed.Resume()
                liveChartTemp.Resume()
                btnChartPause.Text = "⏸️ Duraklat"
                btnChartPause.BackColor = Color.FromArgb(230, 126, 34)
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.BtnChartPause_Click")
        End Try
    End Sub

    ''' <summary>
    ''' Grafik temizle butonu
    ''' </summary>
    Private Sub BtnChartClear_Click(sender As Object, e As EventArgs) Handles btnChartClear.Click
        Try
            If MessageBox.Show("Tüm grafikleri temizlemek istediğinize emin misiniz?", "Grafik Temizle", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                liveChartRPM.Clear()
                liveChartSpeed.Clear()
                liveChartTemp.Clear()
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.BtnChartClear_Click")
        End Try
    End Sub

    ''' <summary>
    ''' Grafik CSV export butonu
    ''' </summary>
    Private Sub BtnChartExport_Click(sender As Object, e As EventArgs) Handles btnChartExport.Click
        Try
            Using saveDialog As New SaveFileDialog()
                saveDialog.Filter = "CSV Dosyaları (*.csv)|*.csv|Tüm Dosyalar (*.*)|*.*"
                saveDialog.FilterIndex = 1
                saveDialog.FileName = $"Dashboard_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                saveDialog.InitialDirectory = Path.Combine(Application.StartupPath, "data", "exports")

                If saveDialog.ShowDialog() = DialogResult.OK Then
                    ' Tüm grafikleri tek CSV'ye birleştir
                    Using writer As New StreamWriter(saveDialog.FileName, False, System.Text.Encoding.UTF8)
                        writer.WriteLine("Zaman,RPM (rpm),Hız (km/h),Sıcaklık (°C)")

                        ' En uzun veri setini bul
                        Dim maxCount = Math.Max(Math.Max(liveChartRPM.DataPointCount, liveChartSpeed.DataPointCount), liveChartTemp.DataPointCount)

                        ' Geçici dosyalara export et
                        Dim tempRPM = Path.GetTempFileName()
                        Dim tempSpeed = Path.GetTempFileName()
                        Dim tempTemp = Path.GetTempFileName()

                        liveChartRPM.ExportToCsv(tempRPM)
                        liveChartSpeed.ExportToCsv(tempSpeed)
                        liveChartTemp.ExportToCsv(tempTemp)

                        ' CSV'leri oku ve birleştir
                        Dim rpmData = ReadCsvData(tempRPM)
                        Dim speedData = ReadCsvData(tempSpeed)
                        Dim tempData = ReadCsvData(tempTemp)

                        ' Birleştir ve yaz
                        For i As Integer = 0 To maxCount - 1
                            Dim time = If(i < rpmData.Count, rpmData(i).Time, "")
                            Dim rpm = If(i < rpmData.Count, rpmData(i).Value.ToString("F2"), "")
                            Dim speed = If(i < speedData.Count, speedData(i).Value.ToString("F2"), "")
                            Dim temp = If(i < tempData.Count, tempData(i).Value.ToString("F2"), "")
                            writer.WriteLine($"{time},{rpm},{speed},{temp}")
                        Next

                        ' Geçici dosyaları sil
                        Try
                            File.Delete(tempRPM)
                            File.Delete(tempSpeed)
                            File.Delete(tempTemp)
                        Catch
                        End Try
                    End Using

                    MessageBox.Show("Grafikler başarıyla dışa aktarıldı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.BtnChartExport_Click")
            MessageBox.Show($"Export hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' CSV verisini okur
    ''' </summary>
    Private Function ReadCsvData(filePath As String) As List(Of (Time As String, Value As Double))
        Dim result As New List(Of (Time As String, Value As Double))
        Try
            Using reader As New StreamReader(filePath, System.Text.Encoding.UTF8)
                reader.ReadLine() ' Header'ı atla
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If Not String.IsNullOrEmpty(line) Then
                        Dim parts = line.Split(","c)
                        If parts.Length >= 2 Then
                            Dim time = parts(0)
                            Dim value As Double
                            If Double.TryParse(parts(1), value) Then
                                result.Add((time, value))
                            End If
                        End If
                    End If
                End While
            End Using
        Catch
        End Try
        Return result
    End Function

    ''' <summary>
    ''' Zaman aralığı değişti
    ''' </summary>
    Private Sub CmbChartTimeRange_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbChartTimeRange.SelectedIndexChanged
        Try
            Dim selectedText = cmbChartTimeRange.SelectedItem.ToString()
            Dim seconds As Integer = 60

            If selectedText.Contains("30") Then
                seconds = 30
            ElseIf selectedText.Contains("60") Then
                seconds = 60
            ElseIf selectedText.Contains("120") Then
                seconds = 120
            ElseIf selectedText.Contains("300") Then
                seconds = 300
            End If

            liveChartRPM.SetTimeRange(seconds)
            liveChartSpeed.SetTimeRange(seconds)
            liveChartTemp.SetTimeRange(seconds)
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.CmbChartTimeRange_SelectedIndexChanged")
        End Try
    End Sub

#End Region

#Region "Help System"

    ''' <summary>
    ''' ECU Tarama menü item'ı
    ''' </summary>
    Private Sub MnuToolsEcuScanner_Click(sender As Object, e As EventArgs) Handles mnuToolsEcuScanner.Click
        Try
            If Not SerialPort1.IsOpen Then
                MessageBox.Show("ECU tarama özelliğini kullanmak için önce CAN adaptörüne bağlanmanız gerekiyor.", "Bağlantı Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim scannerForm As New EcuScannerForm(canSender, advancedUdsEngine)
            scannerForm.ShowDialog(Me)
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.MnuToolsEcuScanner_Click")
            MessageBox.Show($"ECU tarama penceresi açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' PDF Rapor menü item'ı
    ''' </summary>
    Private Sub MnuToolsReport_Click(sender As Object, e As EventArgs) Handles mnuToolsReport.Click
        Try
            ' Mevcut verileri topla
            Dim vehicleInfo = obdService.GetVehicleInfo()
            Dim allDTCs = New List(Of DTCInfo)(_allDTCs)
            
            ' ECU listesi (EcuScanner'dan alınabilir, şimdilik boş)
            Dim ecus As List(Of EcuInfo) = Nothing
            
            ' Canlı veri snapshot'ı
            Dim liveData As New Dictionary(Of String, Double)()
            If dashboardManager IsNot Nothing Then
                liveData("Motor Devri") = dashboardManager.RPM
                liveData("Hız") = dashboardManager.Speed
                liveData("Soğutucu Sıcaklık") = dashboardManager.Temperature
                liveData("Motor Yükü") = dashboardManager.EngineLoad
                liveData("Gaz Kelebeği") = dashboardManager.Throttle
                liveData("Yakıt Seviyesi") = dashboardManager.FuelLevel
            End If

            Dim reportForm As New ReportForm(vehicleInfo, ecus, allDTCs, liveData)
            reportForm.ShowDialog(Me)
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.MnuToolsReport_Click")
            MessageBox.Show($"PDF rapor penceresi açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Yardım menü item'ı
    ''' </summary>
    Private Sub MnuHelpViewer_Click(sender As Object, e As EventArgs) Handles mnuHelpViewer.Click
        ShowHelp()
    End Sub

    ''' <summary>
    ''' Yardım penceresini göster
    ''' </summary>
    Private Sub ShowHelp(Optional topic As String = "")
        Try
            Dim helpViewer As New HelpViewer(topic)
            helpViewer.Show()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.ShowHelp")
            MessageBox.Show("Yardım penceresi açılamadı: " & ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Mevcut tab'a göre context help topic'i döndür
    ''' </summary>
    Private Function GetContextHelpTopic() As String
        Try
            If tabMain.SelectedTab Is Nothing Then Return "getting-started.html"

            Select Case tabMain.SelectedTab.Name
                Case "tabConnection"
                    Return "connection.html"
                Case "tabDashboard"
                    Return "obd-reading.html"
                Case "tabDiagnostics"
                    Return "dtc-reading.html"
                Case "tabCommands"
                    Return "can-analysis.html"
                Case "tabCoding"
                    Return "coding.html"
                Case "tabLearning"
                    Return "learning-engine.html"
                Case Else
                    Return "getting-started.html"
            End Select
        Catch
            Return "getting-started.html"
        End Try
    End Function

    ''' <summary>
    ''' Bağlantı sekmesi yardım butonu
    ''' </summary>
    Private Sub BtnHelpConnection_Click(sender As Object, e As EventArgs) Handles btnHelpConnection.Click
        ShowHelp("connection.html")
    End Sub

    ''' <summary>
    ''' Dashboard sekmesi yardım butonu
    ''' </summary>
    Private Sub BtnHelpDashboard_Click(sender As Object, e As EventArgs) Handles btnHelpDashboard.Click
        ShowHelp("obd-reading.html")
    End Sub

    ''' <summary>
    ''' Teşhis sekmesi yardım butonu
    ''' </summary>
    Private Sub BtnHelpDiagnostics_Click(sender As Object, e As EventArgs) Handles btnHelpDiagnostics.Click
        ShowHelp("dtc-reading.html")
    End Sub

#End Region

#Region "Localization"

    ''' <summary>
    ''' Dil değiştiğinde UI'ı güncelle
    ''' </summary>
    Private Sub HandleLanguageChanged(newLanguage As String)
        UpdateUIStrings()
    End Sub

    ''' <summary>
    ''' Tüm UI string'lerini güncelle
    ''' </summary>
    Private Sub UpdateUIStrings()
        Try
            ' LocalizationManager'ın yüklendiğinden emin ol
            Dim locMgr = LocalizationManager.Instance
            If locMgr Is Nothing Then
                ErrorHandler.Instance.LogWarning("LocalizationManager yüklenemedi, UI string'leri güncellenemedi")
                Return
            End If

            ' Helper metod: Güvenli string güncelleme (Control için)
            Dim SafeSetText = Sub(control As Control, key As String)
                                  If control IsNot Nothing AndAlso Not control.IsDisposed Then
                                      Try
                                          Dim text = LocalizationManager.T(key)
                                          ' Eğer key döndüyse (başında [ varsa), fallback kullan
                                          If text.StartsWith("[") AndAlso text.EndsWith("]") Then
                                              ErrorHandler.Instance.LogWarning($"Localization key bulunamadı: {key}")
                                              Return ' Fallback olarak mevcut text'i koru
                                          End If
                                          control.Text = text
                                      Catch ex As Exception
                                          ErrorHandler.Instance.LogError(ex, $"UpdateUIStrings: {key}")
                                      End Try
                                  End If
                              End Sub

            ' Helper metod: Güvenli string güncelleme (ToolStripItem için)
            Dim SafeSetMenuItemText = Sub(item As ToolStripItem, key As String)
                                          If item IsNot Nothing Then
                                              Try
                                                  Dim text = LocalizationManager.T(key)
                                                  ' Eğer key döndüyse (başında [ varsa), fallback kullan
                                                  If text.StartsWith("[") AndAlso text.EndsWith("]") Then
                                                      ErrorHandler.Instance.LogWarning($"Localization key bulunamadı: {key}")
                                                      Return ' Fallback olarak mevcut text'i koru
                                                  End If
                                                  item.Text = text
                                              Catch ex As Exception
                                                  ErrorHandler.Instance.LogError(ex, $"UpdateUIStrings: {key}")
                                              End Try
                                          End If
                                      End Sub

            ' Form başlığı
            Me.Text = LocalizationManager.T("app.title")

            ' Menü öğeleri
            SafeSetMenuItemText(mnuTools, "menu.tools")
            SafeSetMenuItemText(mnuToolsEcuScanner, "menu.ecu_scanner")
            SafeSetMenuItemText(mnuToolsReport, "menu.report")
            SafeSetMenuItemText(mnuToolsSessionPlayer, "menu.session_player")
            SafeSetMenuItemText(mnuHelp, "menu.help")
            SafeSetMenuItemText(mnuHelpViewer, "menu.help_viewer")

            ' Tab başlıkları
            SafeSetText(tabConnection, "tabs.connection")
            SafeSetText(tabDashboard, "tabs.dashboard")
            SafeSetText(tabDiagnostics, "tabs.diagnostics")
            SafeSetText(tabCommands, "tabs.commands")
            SafeSetText(tabCoding, "tabs.coding")
            SafeSetText(tabLearning, "tabs.learning")

            ' Bağlantı butonları
            SafeSetText(btnConnect, "connection.connect")
            SafeSetText(btnQuickConnect, "connection.quick_connect")

            ' Kayıt butonları
            SafeSetText(btnStartRecording, "recording.start")
            SafeSetText(btnStopRecording, "recording.stop")

            ' Dashboard label'ları (emoji'ler localization'da)
            SafeSetText(lblRPM, "dashboard.rpm")
            SafeSetText(lblSpeed, "dashboard.speed")
            SafeSetText(lblTemp, "dashboard.coolant_temp")
            SafeSetText(lblDoor, "dashboard.door")
            SafeSetText(lblLight, "dashboard.light")
            SafeSetText(lblThrottle, "dashboard.throttle")
            SafeSetText(lblEngineLoad, "dashboard.engine_load")
            SafeSetText(lblFuelLevel, "dashboard.fuel_level")
            SafeSetText(lblVoltage, "dashboard.voltage")
            SafeSetText(lblIntakeTemp, "dashboard.intake_temp")
            SafeSetText(lblAmbientTemp, "dashboard.ambient_temp")
            SafeSetText(lblMAP, "dashboard.map")
            SafeSetText(lblMAF, "dashboard.maf")
            SafeSetText(lblShortTrim, "dashboard.short_trim")
            SafeSetText(lblLongTrim, "dashboard.long_trim")
            SafeSetText(lblBarometric, "dashboard.barometric")
            SafeSetText(lblFuelRate, "dashboard.fuel_rate")

            ' Teşhis butonları
            SafeSetText(btnReadPendingDTC, "diagnostics.read_dtc")
            SafeSetText(btnReadStoredDTC, "diagnostics.read_dtc")
            SafeSetText(btnClearDTC, "diagnostics.clear_dtc")

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.UpdateUIStrings")
        End Try
    End Sub

#End Region

#Region "Session Recording"

    ''' <summary>
    ''' Kayıt butonlarını başlat
    ''' </summary>
    Private Sub InitializeRecordingButtons()
        Try
            ' Menü öğelerini oluştur (yoksa)
            If mnuToolsSessionPlayer Is Nothing Then
                mnuToolsSessionPlayer = New ToolStripMenuItem()
                mnuToolsSessionPlayer.Text = "Oturum Oynatıcı"
                mnuTools.DropDownItems.Add(mnuToolsSessionPlayer)
            End If

            ' Kayıt butonlarını oluştur (yoksa)
            If btnStartRecording Is Nothing Then
                btnStartRecording = New Button()
                btnStartRecording.Text = "🔴 Kaydet"
                btnStartRecording.Size = New Size(80, 30)
                btnStartRecording.Location = New Point(10, Me.Height - 70)
                btnStartRecording.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
                btnStartRecording.BackColor = Color.FromArgb(60, 60, 60)
                btnStartRecording.ForeColor = Color.White
                btnStartRecording.FlatStyle = FlatStyle.Flat
                Me.Controls.Add(btnStartRecording)
                btnStartRecording.BringToFront()
            End If

            If btnStopRecording Is Nothing Then
                btnStopRecording = New Button()
                btnStopRecording.Text = "⏹ Durdur"
                btnStopRecording.Size = New Size(80, 30)
                btnStopRecording.Location = New Point(95, Me.Height - 70)
                btnStopRecording.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
                btnStopRecording.BackColor = Color.FromArgb(60, 60, 60)
                btnStopRecording.ForeColor = Color.White
                btnStopRecording.FlatStyle = FlatStyle.Flat
                btnStopRecording.Enabled = False
                Me.Controls.Add(btnStopRecording)
                btnStopRecording.BringToFront()
            End If

            ' StatusBar label'ı oluştur (yoksa)
            If lblRecordingStatus Is Nothing Then
                lblRecordingStatus = New ToolStripStatusLabel()
                lblRecordingStatus.Text = LocalizationManager.T("recording.recording_ready")
                lblRecordingStatus.Spring = True
                lblRecordingStatus.TextAlign = ContentAlignment.MiddleLeft
                ' StatusStrip yoksa oluştur
                If Me.Controls.OfType(Of StatusStrip)().Count() = 0 Then
                    Dim statusStrip As New StatusStrip()
                    statusStrip.BackColor = Color.FromArgb(37, 37, 38)
                    statusStrip.ForeColor = Color.White
                    statusStrip.Items.Add(lblRecordingStatus)
                    Me.Controls.Add(statusStrip)
                End If
            End If

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.InitializeRecordingButtons")
        End Try
    End Sub

    ''' <summary>
    ''' Kayıt başlat butonu
    ''' </summary>
    Private Sub BtnStartRecording_Click(sender As Object, e As EventArgs) Handles btnStartRecording.Click
        If _isRecording Then Return

        Try
            ' Dosya adı öner
            Dim defaultFileName = $"session_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ccrec"
            Dim sessionsFolder = Path.Combine(Application.StartupPath, "data", "sessions")
            If Not Directory.Exists(sessionsFolder) Then
                Directory.CreateDirectory(sessionsFolder)
            End If

                Using sfd As New SaveFileDialog()
                    sfd.Filter = $"{LocalizationManager.T("recording.format_binary")}|*.ccrec|{LocalizationManager.T("recording.format_csv")}|*.csv"
                    sfd.FileName = defaultFileName
                    sfd.InitialDirectory = sessionsFolder
                    sfd.Title = LocalizationManager.T("recording.select_file")

                If sfd.ShowDialog() = DialogResult.OK Then
                    ' Format belirle
                    Dim format = If(sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase),
                                   SessionRecorder.RecordFormat.CSV,
                                   SessionRecorder.RecordFormat.Binary)

                    sessionRecorder.StartRecording(sfd.FileName, format)
                    _isRecording = True
                    _recordingStartTime = DateTime.Now
                    _recordingBlinkTimer.Start()

                    AddLog($"🔴 Kayıt başlatıldı: {Path.GetFileName(sfd.FileName)}")
                    
                    ' Son kayıtlara ekle
                    AddToRecentSessions(sfd.FileName)
                End If
            End Using

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.BtnStartRecording_Click")
            MessageBox.Show($"Kayıt başlatılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Kayıt durdur butonu
    ''' </summary>
    Private Sub BtnStopRecording_Click(sender As Object, e As EventArgs) Handles btnStopRecording.Click
        If Not _isRecording Then Return

        Try
            sessionRecorder.StopRecording()
            _isRecording = False
            _recordingBlinkTimer.Stop()

            ' StatusBar'ı güncelle
            If lblRecordingStatus IsNot Nothing Then
                SafeInvoke(Sub() lblRecordingStatus.Text = "Kayıt durduruldu")
            End If

            AddLog($"⏹ Kayıt durduruldu: {sessionRecorder.RecordedFrameCount} frame")

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.BtnStopRecording_Click")
        End Try
    End Sub

    ''' <summary>
    ''' Oturum oynatıcı aç
    ''' </summary>
    Private Sub MnuToolsSessionPlayer_Click(sender As Object, e As EventArgs) Handles mnuToolsSessionPlayer.Click
        Try
            Dim playerForm As New SessionPlayerForm()
            playerForm.Show()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.MnuToolsSessionPlayer_Click")
            MessageBox.Show($"Oynatıcı açılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Kayıt başladığında
    ''' </summary>
    Private Sub HandleRecordingStarted(filePath As String)
        SafeInvoke(Sub()
                       If lblRecordingStatus IsNot Nothing Then
                           lblRecordingStatus.Text = $"{LocalizationManager.T("recording.recording")}: {Path.GetFileName(filePath)}"
                           lblRecordingStatus.ForeColor = Color.Red
                       End If
                       
                       If btnStartRecording IsNot Nothing Then
                           btnStartRecording.Enabled = False
                       End If
                       If btnStopRecording IsNot Nothing Then
                           btnStopRecording.Enabled = True
                       End If
                   End Sub)
    End Sub

    ''' <summary>
    ''' Kayıt durduğunda
    ''' </summary>
    Private Sub HandleRecordingStopped(frameCount As Long)
        SafeInvoke(Sub()
                       If lblRecordingStatus IsNot Nothing Then
                           lblRecordingStatus.Text = $"{LocalizationManager.T("recording.recording_stopped")}: {frameCount:N0} frame"
                           lblRecordingStatus.ForeColor = Color.White
                       End If
                       
                       If btnStartRecording IsNot Nothing Then
                           btnStartRecording.Enabled = True
                       End If
                       If btnStopRecording IsNot Nothing Then
                           btnStopRecording.Enabled = False
                       End If
                   End Sub)
    End Sub

    ''' <summary>
    ''' Frame kaydedildiğinde (her 100 frame'de bir güncelle)
    ''' </summary>
    Private Sub HandleFrameRecorded(frameNumber As Long)
        If frameNumber Mod 100 <> 0 Then Return ' Performans için

        SafeInvoke(Sub()
                       If lblRecordingStatus IsNot Nothing Then
                           Dim duration = DateTime.Now - _recordingStartTime
                           lblRecordingStatus.Text = LocalizationManager.T("recording.recording_status", duration.ToString("hh\:mm\:ss"), frameNumber.ToString("N0"))
                       End If
                   End Sub)
    End Sub

    ''' <summary>
    ''' Kayıt butonu yanıp sönsün
    ''' </summary>
    Private Sub RecordingBlinkTimer_Tick(sender As Object, e As EventArgs)
        If btnStartRecording Is Nothing OrElse btnStartRecording.IsDisposed Then Return

        SafeInvoke(Sub()
                       If btnStartRecording.BackColor = Color.Red Then
                           btnStartRecording.BackColor = Color.DarkRed
                       Else
                           btnStartRecording.BackColor = Color.Red
                       End If
                   End Sub)
    End Sub

    ''' <summary>
    ''' CAN frame'i alındığında kayda ekle
    ''' </summary>
    Private Sub RecordCANFrame(frameId As Integer, data As Byte())
        If _isRecording AndAlso sessionRecorder IsNot Nothing Then
            Try
                sessionRecorder.RecordFrame(frameId, data)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "MainForm.RecordCANFrame")
            End Try
        End If
    End Sub

    ''' <summary>
    ''' Son kayıtları yükle
    ''' </summary>
    Private Sub LoadRecentSessions()
        Try
            Dim recentFile = Path.Combine(Application.StartupPath, "data", "recent_sessions.txt")
            If File.Exists(recentFile) Then
                _recentSessions.Clear()
                _recentSessions.AddRange(File.ReadAllLines(recentFile).Take(10))
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.LoadRecentSessions")
        End Try
    End Sub

    ''' <summary>
    ''' Son kayıtlara ekle
    ''' </summary>
    Private Sub AddToRecentSessions(filePath As String)
        Try
            ' Listeye ekle (en başa)
            _recentSessions.Remove(filePath) ' Varsa çıkar
            _recentSessions.Insert(0, filePath)

            ' En fazla 10 kayıt tut
            If _recentSessions.Count > 10 Then
                _recentSessions.RemoveRange(10, _recentSessions.Count - 10)
            End If

            ' Dosyaya kaydet
            Dim recentFile = Path.Combine(Application.StartupPath, "data", "recent_sessions.txt")
            File.WriteAllLines(recentFile, _recentSessions)

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "MainForm.AddToRecentSessions")
        End Try
    End Sub

#End Region

End Class
