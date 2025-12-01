' ConnectionWizard.vb
' Bağlantı kurulum wizard'ı
' Kullanıcıya adım adım bağlantı kurma rehberi sunar

Imports System.IO.Ports
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports CarCanReader1._0.Services

Public Class ConnectionWizard
    Inherits Form

#Region "Wizard State"

    Private _currentStep As Integer = 0
    Private _totalSteps As Integer = 5

#End Region

#Region "Collected Information"

    Public Property SelectedPort As String = ""
    Public Property SelectedBaudRate As Integer = 500000
    
    Private _detectedProtocolName As String = ""
    Public Property DetectedProtocolName As String
        Get
            Return _detectedProtocolName
        End Get
        Set(value As String)
            _detectedProtocolName = value
        End Set
    End Property
    
    Public Property ConnectionSuccessful As Boolean = False

#End Region

#Region "UI Elements"

    ' Ana container
    Private pnlMain As Panel
    Private pnlLeftSidebar As Panel
    Private pnlRightContent As Panel
    Private pnlBottomButtons As Panel

    ' Sol sidebar - Adım listesi
    Private lstSteps As ListBox

    ' Sağ content - Adım içeriği
    Private pnlWizardContent As Panel
    Private lblStepTitle As Label
    Private lblStepDescription As Label

    ' Alt butonlar
    Private btnBack As Button
    Private btnNext As Button
    Private btnCancel As Button
    Private prgProgress As ProgressBar

    ' Adım 1 - Hoşgeldin
    Private pnlStep1 As Panel
    Private lblWelcomeTitle As Label
    Private lblWelcomeDescription As Label
    Private picAdapterIcon As PictureBox
    Private lstRequirements As ListBox

    ' Adım 2 - Port Seçimi
    Private pnlStep2 As Panel
    Private lblPortSelectionTitle As Label
    Private lstPorts As ListBox
    Private btnRefreshPorts As Button
    Private btnAutoDetect As Button
    Private lblBaudRate As Label
    Private cmbBaudRate As ComboBox
    Private lblPortInfo As Label

    ' Adım 3 - Araç Kontrolü
    Private pnlStep3 As Panel
    Private lblVehicleCheckTitle As Label
    Private lblVehicleCheckDescription As Label
    Private picWaitingIcon As PictureBox
    Private prgWaiting As ProgressBar
    Private btnReady As Button
    Private lblTimeoutInfo As Label
    Private _waitingTimer As System.Windows.Forms.Timer
    Private _timeoutCounter As Integer = 30

    ' Adım 4 - Protokol Tespiti
    Private pnlStep4 As Panel
    Private lblProtocolTitle As Label
    Private lstProtocols As ListBox
    Private lblProtocolStatus As Label
    Private _protocolDetector As ProtocolDetector
    Private _testedProtocols As New List(Of ProtocolDetector.ProtocolInfo)

    ' Adım 5 - Tamamlandı
    Private pnlStep5 As Panel
    Private lblCompleteTitle As Label
    Private picCompleteIcon As PictureBox
    Private lblCompleteMessage As Label
    Private grpConnectionSummary As GroupBox
    Private lblSummaryPort As Label
    Private lblSummaryBaudRate As Label
    Private lblSummaryProtocol As Label
    Private chkSaveConnection As CheckBox
    Private btnFinish As Button

#End Region

#Region "Step Data"

    Private _availablePorts As New List(Of PortInfo)
    Private _detectedCANablePort As String = ""
    Private _detectedProtocolInfo As ProtocolDetector.ProtocolInfo = Nothing

#End Region

#Region "Port Info Class"

    Private Class PortInfo
        Public Property PortName As String
        Public Property Description As String
        Public Property VID As String = ""
        Public Property PID As String = ""
        Public Property IsCANable As Boolean = False

        Public Overrides Function ToString() As String
            Dim result As String = PortName
            If IsCANable Then
                result &= " [CANable]"
            End If
            If Not String.IsNullOrEmpty(Description) Then
                result &= $" - {Description}"
            End If
            Return result
        End Function
    End Class

#End Region

#Region "Constructor"

    Public Sub New()
        InitializeComponent()
        LoadStep1()
    End Sub

#End Region

#Region "Initialize Component"

    Private Sub InitializeComponent()
        Me.Text = "Bağlantı Wizard'ı"
        Me.Size = New Size(900, 650)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.FromArgb(30, 30, 40)
        Me.Font = New Font("Segoe UI", 9.0!)

        ' Ana panel
        pnlMain = New Panel()
        pnlMain.Dock = DockStyle.Fill
        pnlMain.BackColor = Color.FromArgb(30, 30, 40)

        ' Sol sidebar
        pnlLeftSidebar = New Panel()
        pnlLeftSidebar.Size = New Size(250, 0)
        pnlLeftSidebar.Dock = DockStyle.Left
        pnlLeftSidebar.BackColor = Color.FromArgb(40, 40, 55)
        pnlLeftSidebar.Padding = New Padding(15)

        ' Adım listesi
        lstSteps = New ListBox()
        lstSteps.Dock = DockStyle.Fill
        lstSteps.BackColor = Color.FromArgb(40, 40, 55)
        lstSteps.ForeColor = Color.White
        lstSteps.BorderStyle = BorderStyle.None
        lstSteps.Font = New Font("Segoe UI", 10.0!)
        lstSteps.Enabled = False
        lstSteps.Items.Add("1. Hoşgeldin")
        lstSteps.Items.Add("2. Adaptör Seçimi")
        lstSteps.Items.Add("3. Araç Bağlantısı")
        lstSteps.Items.Add("4. Protokol Tespiti")
        lstSteps.Items.Add("5. Bağlantı Testi")
        lstSteps.Items.Add("6. Tamamlandı")
        lstSteps.SelectedIndex = 0

        pnlLeftSidebar.Controls.Add(lstSteps)

        ' Sağ content
        pnlRightContent = New Panel()
        pnlRightContent.Dock = DockStyle.Fill
        pnlRightContent.BackColor = Color.FromArgb(30, 30, 40)
        pnlRightContent.Padding = New Padding(30)

        ' Wizard content panel
        pnlWizardContent = New Panel()
        pnlWizardContent.Dock = DockStyle.Fill
        pnlWizardContent.BackColor = Color.FromArgb(30, 30, 40)

        ' Başlık
        lblStepTitle = New Label()
        lblStepTitle.Font = New Font("Segoe UI", 18.0!, FontStyle.Bold)
        lblStepTitle.ForeColor = Color.White
        lblStepTitle.Location = New Point(0, 0)
        lblStepTitle.Size = New Size(600, 40)
        lblStepTitle.AutoSize = False

        ' Açıklama
        lblStepDescription = New Label()
        lblStepDescription.Font = New Font("Segoe UI", 10.0!)
        lblStepDescription.ForeColor = Color.FromArgb(200, 200, 220)
        lblStepDescription.Location = New Point(0, 50)
        lblStepDescription.Size = New Size(600, 60)
        lblStepDescription.AutoSize = False

        pnlWizardContent.Controls.Add(lblStepTitle)
        pnlWizardContent.Controls.Add(lblStepDescription)

        pnlRightContent.Controls.Add(pnlWizardContent)

        ' Alt butonlar paneli
        pnlBottomButtons = New Panel()
        pnlBottomButtons.Height = 80
        pnlBottomButtons.Dock = DockStyle.Bottom
        pnlBottomButtons.BackColor = Color.FromArgb(40, 40, 55)
        pnlBottomButtons.Padding = New Padding(20)

        ' Progress bar
        prgProgress = New ProgressBar()
        prgProgress.Location = New Point(20, 10)
        prgProgress.Size = New Size(600, 20)
        prgProgress.Style = ProgressBarStyle.Continuous
        prgProgress.Maximum = _totalSteps
        prgProgress.Value = 0

        ' Butonlar
        btnCancel = New Button()
        btnCancel.Text = "İptal"
        btnCancel.Size = New Size(100, 35)
        btnCancel.Location = New Point(750, 20)
        btnCancel.BackColor = Color.FromArgb(100, 100, 120)
        btnCancel.ForeColor = Color.White
        btnCancel.FlatStyle = FlatStyle.Flat
        btnCancel.Font = New Font("Segoe UI", 9.0!)
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click

        btnBack = New Button()
        btnBack.Text = "← Geri"
        btnBack.Size = New Size(100, 35)
        btnBack.Location = New Point(520, 20)
        btnBack.BackColor = Color.FromArgb(52, 152, 219)
        btnBack.ForeColor = Color.White
        btnBack.FlatStyle = FlatStyle.Flat
        btnBack.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btnBack.Enabled = False
        AddHandler btnBack.Click, AddressOf BtnBack_Click

        btnNext = New Button()
        btnNext.Text = "İleri →"
        btnNext.Size = New Size(100, 35)
        btnNext.Location = New Point(630, 20)
        btnNext.BackColor = Color.FromArgb(46, 204, 113)
        btnNext.ForeColor = Color.White
        btnNext.FlatStyle = FlatStyle.Flat
        btnNext.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        AddHandler btnNext.Click, AddressOf BtnNext_Click

        pnlBottomButtons.Controls.Add(prgProgress)
        pnlBottomButtons.Controls.Add(btnCancel)
        pnlBottomButtons.Controls.Add(btnBack)
        pnlBottomButtons.Controls.Add(btnNext)

        ' Form'a ekle
        pnlMain.Controls.Add(pnlRightContent)
        pnlMain.Controls.Add(pnlLeftSidebar)
        Me.Controls.Add(pnlMain)
        Me.Controls.Add(pnlBottomButtons)
    End Sub

#End Region

#Region "Step Loading"

    Private Sub LoadStep1()
        _currentStep = 0
        UpdateStepUI()

        ' Adım 1 içeriğini oluştur
        pnlStep1 = New Panel()
        pnlStep1.Dock = DockStyle.Fill
        pnlStep1.BackColor = Color.FromArgb(30, 30, 40)
        pnlStep1.Padding = New Padding(20)

        ' Başlık
        lblWelcomeTitle = New Label()
        lblWelcomeTitle.Text = "CarCanReader'a Hoşgeldiniz"
        lblWelcomeTitle.Font = New Font("Segoe UI", 24.0!, FontStyle.Bold)
        lblWelcomeTitle.ForeColor = Color.White
        lblWelcomeTitle.Location = New Point(20, 20)
        lblWelcomeTitle.AutoSize = True

        ' Açıklama
        lblWelcomeDescription = New Label()
        lblWelcomeDescription.Text = "Bu wizard size CAN adaptörünüze bağlanmanızda yardımcı olacak." & vbCrLf &
                                     "Aşağıdaki gereksinimleri kontrol edin ve hazır olduğunuzda 'İleri' butonuna tıklayın."
        lblWelcomeDescription.Font = New Font("Segoe UI", 11.0!)
        lblWelcomeDescription.ForeColor = Color.FromArgb(200, 200, 220)
        lblWelcomeDescription.Location = New Point(20, 80)
        lblWelcomeDescription.Size = New Size(600, 80)
        lblWelcomeDescription.AutoSize = False

        ' Adaptör ikonu (basit bir label)
        picAdapterIcon = New PictureBox()
        picAdapterIcon.Size = New Size(64, 64)
        picAdapterIcon.Location = New Point(20, 180)
        picAdapterIcon.BackColor = Color.FromArgb(50, 50, 70)
        picAdapterIcon.BorderStyle = BorderStyle.FixedSingle
        Dim iconLabel As New Label()
        iconLabel.Text = "🔌"
        iconLabel.Font = New Font("Segoe UI", 32.0!)
        iconLabel.ForeColor = Color.White
        iconLabel.Location = New Point(16, 16)
        iconLabel.Size = New Size(32, 32)
        iconLabel.TextAlign = ContentAlignment.MiddleCenter
        picAdapterIcon.Controls.Add(iconLabel)

        ' Gereksinimler listesi
        lstRequirements = New ListBox()
        lstRequirements.Location = New Point(20, 260)
        lstRequirements.Size = New Size(600, 200)
        lstRequirements.BackColor = Color.FromArgb(40, 40, 55)
        lstRequirements.ForeColor = Color.White
        lstRequirements.BorderStyle = BorderStyle.FixedSingle
        lstRequirements.Font = New Font("Segoe UI", 10.0!)
        lstRequirements.Items.Add("✓ CANable veya uyumlu adaptör USB'ye takılı olmalı")
        lstRequirements.Items.Add("✓ Adaptör OBD-II portuna bağlı olmalı")
        lstRequirements.Items.Add("✓ Araç kontağı açık olmalı (motor çalışmasına gerek yok)")
        lstRequirements.Items.Add("✓ Gerekli sürücüler yüklü olmalı")

        pnlStep1.Controls.Add(lblWelcomeTitle)
        pnlStep1.Controls.Add(lblWelcomeDescription)
        pnlStep1.Controls.Add(picAdapterIcon)
        pnlStep1.Controls.Add(lstRequirements)

        pnlWizardContent.Controls.Clear()
        pnlWizardContent.Controls.Add(pnlStep1)
    End Sub

    Private Sub LoadStep2()
        _currentStep = 1
        UpdateStepUI()

        ' Adım 2 içeriğini oluştur
        pnlStep2 = New Panel()
        pnlStep2.Dock = DockStyle.Fill
        pnlStep2.BackColor = Color.FromArgb(30, 30, 40)
        pnlStep2.Padding = New Padding(20)

        ' Başlık
        lblPortSelectionTitle = New Label()
        lblPortSelectionTitle.Text = "COM Port Seçimi"
        lblPortSelectionTitle.Font = New Font("Segoe UI", 16.0!, FontStyle.Bold)
        lblPortSelectionTitle.ForeColor = Color.White
        lblPortSelectionTitle.Location = New Point(20, 20)
        lblPortSelectionTitle.AutoSize = True

        ' Port listesi
        lstPorts = New ListBox()
        lstPorts.Location = New Point(20, 70)
        lstPorts.Size = New Size(500, 300)
        lstPorts.BackColor = Color.FromArgb(40, 40, 55)
        lstPorts.ForeColor = Color.White
        lstPorts.BorderStyle = BorderStyle.FixedSingle
        lstPorts.Font = New Font("Consolas", 10.0!)
        AddHandler lstPorts.SelectedIndexChanged, AddressOf LstPorts_SelectedIndexChanged

        ' Yenile butonu
        btnRefreshPorts = New Button()
        btnRefreshPorts.Text = "🔄 Yenile"
        btnRefreshPorts.Size = New Size(120, 35)
        btnRefreshPorts.Location = New Point(540, 70)
        btnRefreshPorts.BackColor = Color.FromArgb(52, 152, 219)
        btnRefreshPorts.ForeColor = Color.White
        btnRefreshPorts.FlatStyle = FlatStyle.Flat
        btnRefreshPorts.Font = New Font("Segoe UI", 9.0!)
        AddHandler btnRefreshPorts.Click, AddressOf BtnRefreshPorts_Click

        ' Otomatik algıla butonu
        btnAutoDetect = New Button()
        btnAutoDetect.Text = "🔍 Otomatik Algıla"
        btnAutoDetect.Size = New Size(120, 35)
        btnAutoDetect.Location = New Point(540, 115)
        btnAutoDetect.BackColor = Color.FromArgb(155, 89, 182)
        btnAutoDetect.ForeColor = Color.White
        btnAutoDetect.FlatStyle = FlatStyle.Flat
        btnAutoDetect.Font = New Font("Segoe UI", 9.0!)
        AddHandler btnAutoDetect.Click, AddressOf BtnAutoDetect_Click

        ' Baud rate
        lblBaudRate = New Label()
        lblBaudRate.Text = "Baud Rate:"
        lblBaudRate.Font = New Font("Segoe UI", 10.0!)
        lblBaudRate.ForeColor = Color.White
        lblBaudRate.Location = New Point(20, 390)
        lblBaudRate.AutoSize = True

        cmbBaudRate = New ComboBox()
        cmbBaudRate.Location = New Point(120, 387)
        cmbBaudRate.Size = New Size(150, 30)
        cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList
        cmbBaudRate.BackColor = Color.FromArgb(40, 40, 55)
        cmbBaudRate.ForeColor = Color.White
        cmbBaudRate.Font = New Font("Segoe UI", 10.0!)
        cmbBaudRate.Items.Add("115200")
        cmbBaudRate.Items.Add("250000")
        cmbBaudRate.Items.Add("500000")
        cmbBaudRate.Items.Add("1000000")
        cmbBaudRate.SelectedIndex = 2 ' 500000 varsayılan
        AddHandler cmbBaudRate.SelectedIndexChanged, AddressOf CmbBaudRate_SelectedIndexChanged

        ' Port bilgisi
        lblPortInfo = New Label()
        lblPortInfo.Text = "Bir port seçin veya otomatik algılamayı deneyin."
        lblPortInfo.Font = New Font("Segoe UI", 9.0!)
        lblPortInfo.ForeColor = Color.FromArgb(150, 150, 170)
        lblPortInfo.Location = New Point(20, 430)
        lblPortInfo.Size = New Size(600, 40)
        lblPortInfo.AutoSize = False

        pnlStep2.Controls.Add(lblPortSelectionTitle)
        pnlStep2.Controls.Add(lstPorts)
        pnlStep2.Controls.Add(btnRefreshPorts)
        pnlStep2.Controls.Add(btnAutoDetect)
        pnlStep2.Controls.Add(lblBaudRate)
        pnlStep2.Controls.Add(cmbBaudRate)
        pnlStep2.Controls.Add(lblPortInfo)

        ' Portları yükle
        LoadAvailablePorts()

        pnlWizardContent.Controls.Clear()
        pnlWizardContent.Controls.Add(pnlStep2)
    End Sub

    Private Sub LoadStep3()
        _currentStep = 2
        UpdateStepUI()

        ' Adım 3 içeriğini oluştur
        pnlStep3 = New Panel()
        pnlStep3.Dock = DockStyle.Fill
        pnlStep3.BackColor = Color.FromArgb(30, 30, 40)
        pnlStep3.Padding = New Padding(20)

        ' Başlık
        lblVehicleCheckTitle = New Label()
        lblVehicleCheckTitle.Text = "Araç Kontağını Kontrol Edin"
        lblVehicleCheckTitle.Font = New Font("Segoe UI", 18.0!, FontStyle.Bold)
        lblVehicleCheckTitle.ForeColor = Color.White
        lblVehicleCheckTitle.Location = New Point(20, 20)
        lblVehicleCheckTitle.AutoSize = True

        ' Açıklama
        lblVehicleCheckDescription = New Label()
        lblVehicleCheckDescription.Text = "Lütfen aracın kontağını açın (motor çalışmasa da olur)." & vbCrLf &
                                          "Kontak açık olduğunda 'Hazırım, kontağı açtım' butonuna tıklayın."
        lblVehicleCheckDescription.Font = New Font("Segoe UI", 11.0!)
        lblVehicleCheckDescription.ForeColor = Color.FromArgb(200, 200, 220)
        lblVehicleCheckDescription.Location = New Point(20, 70)
        lblVehicleCheckDescription.Size = New Size(600, 60)
        lblVehicleCheckDescription.AutoSize = False

        ' Bekleme ikonu (animasyonlu)
        picWaitingIcon = New PictureBox()
        picWaitingIcon.Size = New Size(80, 80)
        picWaitingIcon.Location = New Point(20, 150)
        picWaitingIcon.BackColor = Color.FromArgb(50, 50, 70)
        picWaitingIcon.BorderStyle = BorderStyle.FixedSingle
        Dim iconLabel As New Label()
        iconLabel.Text = "⏳"
        iconLabel.Font = New Font("Segoe UI", 40.0!)
        iconLabel.ForeColor = Color.White
        iconLabel.Location = New Point(20, 20)
        iconLabel.Size = New Size(40, 40)
        iconLabel.TextAlign = ContentAlignment.MiddleCenter
        picWaitingIcon.Controls.Add(iconLabel)

        ' Progress bar (animasyon için)
        prgWaiting = New ProgressBar()
        prgWaiting.Location = New Point(20, 250)
        prgWaiting.Size = New Size(600, 25)
        prgWaiting.Style = ProgressBarStyle.Marquee
        prgWaiting.MarqueeAnimationSpeed = 30

        ' Hazır butonu
        btnReady = New Button()
        btnReady.Text = "✅ Hazırım, kontağı açtım"
        btnReady.Size = New Size(250, 50)
        btnReady.Location = New Point(20, 300)
        btnReady.BackColor = Color.FromArgb(46, 204, 113)
        btnReady.ForeColor = Color.White
        btnReady.FlatStyle = FlatStyle.Flat
        btnReady.Font = New Font("Segoe UI", 11.0!, FontStyle.Bold)
        AddHandler btnReady.Click, AddressOf BtnReady_Click

        ' Timeout bilgisi
        lblTimeoutInfo = New Label()
        lblTimeoutInfo.Text = "30 saniye içinde butona tıklamazsanız otomatik olarak ilerleyecektir."
        lblTimeoutInfo.Font = New Font("Segoe UI", 9.0!)
        lblTimeoutInfo.ForeColor = Color.FromArgb(150, 150, 170)
        lblTimeoutInfo.Location = New Point(20, 360)
        lblTimeoutInfo.Size = New Size(600, 40)
        lblTimeoutInfo.AutoSize = False

        pnlStep3.Controls.Add(lblVehicleCheckTitle)
        pnlStep3.Controls.Add(lblVehicleCheckDescription)
        pnlStep3.Controls.Add(picWaitingIcon)
        pnlStep3.Controls.Add(prgWaiting)
        pnlStep3.Controls.Add(btnReady)
        pnlStep3.Controls.Add(lblTimeoutInfo)

        ' Timeout timer başlat
        _timeoutCounter = 30
        _waitingTimer = New System.Windows.Forms.Timer()
        _waitingTimer.Interval = 1000
        AddHandler _waitingTimer.Tick, AddressOf WaitingTimer_Tick
        _waitingTimer.Start()

        pnlWizardContent.Controls.Clear()
        pnlWizardContent.Controls.Add(pnlStep3)
    End Sub

    Private Sub LoadStep4()
        _currentStep = 3
        UpdateStepUI()
        btnNext.Enabled = False ' Protokol algılama bitene kadar devre dışı

        ' Adım 4 içeriğini oluştur
        pnlStep4 = New Panel()
        pnlStep4.Dock = DockStyle.Fill
        pnlStep4.BackColor = Color.FromArgb(30, 30, 40)
        pnlStep4.Padding = New Padding(20)

        ' Başlık
        lblProtocolTitle = New Label()
        lblProtocolTitle.Text = "Protokol Tespiti"
        lblProtocolTitle.Font = New Font("Segoe UI", 18.0!, FontStyle.Bold)
        lblProtocolTitle.ForeColor = Color.White
        lblProtocolTitle.Location = New Point(20, 20)
        lblProtocolTitle.AutoSize = True

        ' Protokol listesi
        lstProtocols = New ListBox()
        lstProtocols.Location = New Point(20, 70)
        lstProtocols.Size = New Size(600, 300)
        lstProtocols.BackColor = Color.FromArgb(40, 40, 55)
        lstProtocols.ForeColor = Color.White
        lstProtocols.BorderStyle = BorderStyle.FixedSingle
        lstProtocols.Font = New Font("Segoe UI", 10.0!)
        lstProtocols.Items.Add("□ CAN 500K (High Speed)")
        lstProtocols.Items.Add("□ CAN 250K (Low Speed)")
        lstProtocols.Items.Add("□ CAN 125K")

        ' Durum
        lblProtocolStatus = New Label()
        lblProtocolStatus.Text = "Protokol algılama başlatılıyor..."
        lblProtocolStatus.Font = New Font("Segoe UI", 10.0!)
        lblProtocolStatus.ForeColor = Color.FromArgb(200, 200, 220)
        lblProtocolStatus.Location = New Point(20, 380)
        lblProtocolStatus.Size = New Size(600, 40)
        lblProtocolStatus.AutoSize = False

        pnlStep4.Controls.Add(lblProtocolTitle)
        pnlStep4.Controls.Add(lstProtocols)
        pnlStep4.Controls.Add(lblProtocolStatus)

        ' Protokol algılamayı başlat
        StartProtocolDetection()

        pnlWizardContent.Controls.Clear()
        pnlWizardContent.Controls.Add(pnlStep4)
    End Sub

    Private Sub LoadStep5()
        _currentStep = 4
        UpdateStepUI()
        btnNext.Visible = False
        btnFinish.Visible = True

        ' Adım 5 içeriğini oluştur
        pnlStep5 = New Panel()
        pnlStep5.Dock = DockStyle.Fill
        pnlStep5.BackColor = Color.FromArgb(30, 30, 40)
        pnlStep5.Padding = New Padding(20)

        ' Başlık
        lblCompleteTitle = New Label()
        If ConnectionSuccessful Then
            lblCompleteTitle.Text = "✅ Bağlantı Başarılı!"
            lblCompleteTitle.ForeColor = Color.FromArgb(46, 204, 113)
        Else
            lblCompleteTitle.Text = "❌ Bağlantı Kurulamadı"
            lblCompleteTitle.ForeColor = Color.FromArgb(231, 76, 60)
        End If
        lblCompleteTitle.Font = New Font("Segoe UI", 24.0!, FontStyle.Bold)
        lblCompleteTitle.Location = New Point(20, 20)
        lblCompleteTitle.AutoSize = True

        ' İkon
        picCompleteIcon = New PictureBox()
        picCompleteIcon.Size = New Size(100, 100)
        picCompleteIcon.Location = New Point(20, 80)
        picCompleteIcon.BackColor = Color.FromArgb(50, 50, 70)
        picCompleteIcon.BorderStyle = BorderStyle.FixedSingle
        Dim iconLabel As New Label()
        iconLabel.Text = If(ConnectionSuccessful, "✅", "❌")
        iconLabel.Font = New Font("Segoe UI", 50.0!)
        iconLabel.ForeColor = If(ConnectionSuccessful, Color.FromArgb(46, 204, 113), Color.FromArgb(231, 76, 60))
        iconLabel.Location = New Point(25, 25)
        iconLabel.Size = New Size(50, 50)
        iconLabel.TextAlign = ContentAlignment.MiddleCenter
        picCompleteIcon.Controls.Add(iconLabel)

        ' Mesaj
        lblCompleteMessage = New Label()
        If ConnectionSuccessful Then
            lblCompleteMessage.Text = "Bağlantı başarıyla kuruldu. Artık CAN verilerini görebilirsiniz."
        Else
            lblCompleteMessage.Text = "Bağlantı kurulamadı. Lütfen aşağıdaki önerileri kontrol edin:" & vbCrLf &
                                      "• Adaptörünüzün USB'ye takılı olduğundan emin olun" & vbCrLf &
                                      "• Araç kontağının açık olduğunu kontrol edin" & vbCrLf &
                                      "• Adaptörün OBD-II portuna bağlı olduğunu doğrulayın"
        End If
        lblCompleteMessage.Font = New Font("Segoe UI", 11.0!)
        lblCompleteMessage.ForeColor = Color.FromArgb(200, 200, 220)
        lblCompleteMessage.Location = New Point(20, 200)
        lblCompleteMessage.Size = New Size(600, 100)
        lblCompleteMessage.AutoSize = False

        ' Bağlantı özeti
        grpConnectionSummary = New GroupBox()
        grpConnectionSummary.Text = "Bağlantı Özeti"
        grpConnectionSummary.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        grpConnectionSummary.ForeColor = Color.White
        grpConnectionSummary.Location = New Point(20, 320)
        grpConnectionSummary.Size = New Size(600, 150)
        grpConnectionSummary.BackColor = Color.FromArgb(40, 40, 55)

        lblSummaryPort = New Label()
        lblSummaryPort.Text = $"Port: {SelectedPort}"
        lblSummaryPort.Font = New Font("Segoe UI", 10.0!)
        lblSummaryPort.ForeColor = Color.White
        lblSummaryPort.Location = New Point(15, 30)
        lblSummaryPort.AutoSize = True

        lblSummaryBaudRate = New Label()
        lblSummaryBaudRate.Text = $"Baud Rate: {SelectedBaudRate}"
        lblSummaryBaudRate.Font = New Font("Segoe UI", 10.0!)
        lblSummaryBaudRate.ForeColor = Color.White
        lblSummaryBaudRate.Location = New Point(15, 60)
        lblSummaryBaudRate.AutoSize = True

        lblSummaryProtocol = New Label()
        lblSummaryProtocol.Text = $"Protokol: {If(Not String.IsNullOrEmpty(DetectedProtocolName), DetectedProtocolName, "Tespit edilemedi")}"
        lblSummaryProtocol.Font = New Font("Segoe UI", 10.0!)
        lblSummaryProtocol.ForeColor = Color.White
        lblSummaryProtocol.Location = New Point(15, 90)
        lblSummaryProtocol.AutoSize = True

        grpConnectionSummary.Controls.Add(lblSummaryPort)
        grpConnectionSummary.Controls.Add(lblSummaryBaudRate)
        grpConnectionSummary.Controls.Add(lblSummaryProtocol)

        ' Kaydet checkbox
        chkSaveConnection = New CheckBox()
        chkSaveConnection.Text = "Bu bağlantı ayarlarını kaydet"
        chkSaveConnection.Font = New Font("Segoe UI", 10.0!)
        chkSaveConnection.ForeColor = Color.White
        chkSaveConnection.Location = New Point(20, 490)
        chkSaveConnection.Size = New Size(300, 30)
        chkSaveConnection.Checked = True

        ' Bitir butonu
        btnFinish = New Button()
        btnFinish.Text = "Bitir"
        btnFinish.Size = New Size(150, 40)
        btnFinish.Location = New Point(470, 485)
        btnFinish.BackColor = Color.FromArgb(46, 204, 113)
        btnFinish.ForeColor = Color.White
        btnFinish.FlatStyle = FlatStyle.Flat
        btnFinish.Font = New Font("Segoe UI", 11.0!, FontStyle.Bold)
        btnFinish.Visible = False
        AddHandler btnFinish.Click, AddressOf BtnFinish_Click

        pnlStep5.Controls.Add(lblCompleteTitle)
        pnlStep5.Controls.Add(picCompleteIcon)
        pnlStep5.Controls.Add(lblCompleteMessage)
        pnlStep5.Controls.Add(grpConnectionSummary)
        pnlStep5.Controls.Add(chkSaveConnection)
        pnlStep5.Controls.Add(btnFinish)

        pnlWizardContent.Controls.Clear()
        pnlWizardContent.Controls.Add(pnlStep5)
    End Sub

    Private Sub UpdateStepUI()
        lstSteps.SelectedIndex = _currentStep
        prgProgress.Value = _currentStep + 1

        btnBack.Enabled = (_currentStep > 0)
        btnNext.Enabled = True

        Select Case _currentStep
            Case 0
                lblStepTitle.Text = "Hoşgeldiniz"
                lblStepDescription.Text = "CarCanReader bağlantı kurulum sihirbazına hoşgeldiniz."
            Case 1
                lblStepTitle.Text = "Adaptör Seçimi"
                lblStepDescription.Text = "Mevcut COM portlarını listeleyin ve CANable adaptörünüzü seçin."
            Case 2
                lblStepTitle.Text = "Araç Bağlantısı"
                lblStepDescription.Text = "Araç kontağının açık olduğundan emin olun."
            Case 3
                lblStepTitle.Text = "Protokol Tespiti"
                lblStepDescription.Text = "CAN protokolü otomatik olarak tespit ediliyor..."
            Case 4
                lblStepTitle.Text = "Bağlantı Testi"
                lblStepDescription.Text = "Bağlantı test ediliyor..."
        End Select
    End Sub

#End Region

#Region "Port Management"

    Private Sub LoadAvailablePorts()
        Try
            _availablePorts.Clear()
            lstPorts.Items.Clear()

            ' COM portlarını al
            Dim portNames() As String = SerialPort.GetPortNames()

            If portNames.Length = 0 Then
                lstPorts.Items.Add("Hiç COM port bulunamadı")
                lblPortInfo.Text = "❌ Hiç COM port bulunamadı. Adaptörünüzün USB'ye takılı olduğundan emin olun."
                Return
            End If

            ' Her port için bilgi topla
            For Each portName As String In portNames
                Dim portInfo As New PortInfo()
                portInfo.PortName = portName
                portInfo.Description = GetPortDescription(portName)
                portInfo.IsCANable = DetectCANable(portName)
                _availablePorts.Add(portInfo)
            Next

            ' CANable portları önce göster
            Dim sortedPorts = _availablePorts.OrderByDescending(Function(p) p.IsCANable).ThenBy(Function(p) p.PortName)

            For Each portInfo In sortedPorts
                lstPorts.Items.Add(portInfo.ToString())
            Next

            ' CANable varsa otomatik seç
            Dim canablePort = _availablePorts.FirstOrDefault(Function(p) p.IsCANable)
            If canablePort IsNot Nothing Then
                ' Port string'ini bul ve seç
                For i As Integer = 0 To lstPorts.Items.Count - 1
                    If lstPorts.Items(i).ToString().StartsWith(canablePort.PortName) Then
                        lstPorts.SelectedIndex = i
                        Exit For
                    End If
                Next
                _detectedCANablePort = canablePort.PortName
                lblPortInfo.Text = $"✅ CANable adaptör algılandı: {canablePort.PortName}"
            ElseIf _availablePorts.Count > 0 Then
                lstPorts.SelectedIndex = 0
                lblPortInfo.Text = "⚠️ CANable otomatik algılanamadı. Manuel olarak seçin."
            End If

        Catch ex As Exception
            lblPortInfo.Text = $"❌ Port listesi alınırken hata: {ex.Message}"
        End Try
    End Sub

    Private Function GetPortDescription(portName As String) As String
        ' Basit port açıklaması (WMI olmadan)
        ' Gerçek uygulamada WMI kullanılabilir ama referans gerektirir
        Return "Seri Port"
    End Function

    Private Function DetectCANable(portName As String) As Boolean
        Try
            Dim description As String = GetPortDescription(portName)
            ' CANable genellikle açıklamada "CANable" veya "USB Serial" içerir
            If description.ToLower().Contains("canable") OrElse
               description.ToLower().Contains("usb serial") OrElse
               description.ToLower().Contains("ch340") OrElse
               description.ToLower().Contains("cp210") Then
                Return True
            End If
        Catch
        End Try
        Return False
    End Function

#End Region

#Region "Event Handlers"

    Private Sub BtnNext_Click(sender As Object, e As EventArgs)
        ' Mevcut adımı doğrula
        If Not ValidateCurrentStep() Then
            Return
        End If

        ' Sonraki adıma geç
        _currentStep += 1

        Select Case _currentStep
            Case 1
                LoadStep2()
            Case 2
                LoadStep3()
            Case 3
                LoadStep4()
            Case 4
                LoadStep5()
            Case Else
                ' Wizard tamamlandı
                Me.DialogResult = DialogResult.OK
                Me.Close()
        End Select
    End Sub

    Private Sub BtnBack_Click(sender As Object, e As EventArgs)
        _currentStep -= 1

        Select Case _currentStep
            Case 0
                LoadStep1()
            Case 1
                LoadStep2()
            Case 2
                LoadStep3()
            Case 3
                LoadStep4()
        End Select
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub BtnRefreshPorts_Click(sender As Object, e As EventArgs)
        LoadAvailablePorts()
    End Sub

    Private Sub BtnAutoDetect_Click(sender As Object, e As EventArgs)
        LoadAvailablePorts()
        Dim canablePort = _availablePorts.FirstOrDefault(Function(p) p.IsCANable)
        If canablePort IsNot Nothing Then
            ' Port string'ini bul ve seç
            For i As Integer = 0 To lstPorts.Items.Count - 1
                If lstPorts.Items(i).ToString().StartsWith(canablePort.PortName) Then
                    lstPorts.SelectedIndex = i
                    Exit For
                End If
            Next
            lblPortInfo.Text = $"✅ CANable adaptör algılandı: {canablePort.PortName}"
        Else
            MessageBox.Show("CANable adaptör otomatik olarak algılanamadı. Lütfen manuel olarak seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub LstPorts_SelectedIndexChanged(sender As Object, e As EventArgs)
        If lstPorts.SelectedItem IsNot Nothing Then
            Dim selectedText As String = lstPorts.SelectedItem.ToString()
            ' Port ismini çıkar (ilk boşluk veya [ işaretinden önce)
            Dim portName As String = selectedText.Split(" "c)(0)
            SelectedPort = portName
            
            ' Port bilgisini bul
            Dim portInfo = _availablePorts.FirstOrDefault(Function(p) p.PortName = portName)
            If portInfo IsNot Nothing Then
                lblPortInfo.Text = $"Seçili port: {portInfo.PortName}"
                If portInfo.IsCANable Then
                    lblPortInfo.Text &= " [CANable algılandı]"
                End If
            Else
                lblPortInfo.Text = $"Seçili port: {portName}"
            End If
        End If
    End Sub

    Private Sub CmbBaudRate_SelectedIndexChanged(sender As Object, e As EventArgs)
        If cmbBaudRate.SelectedItem IsNot Nothing Then
            Dim baudRateStr As String = cmbBaudRate.SelectedItem.ToString()
            If Integer.TryParse(baudRateStr, SelectedBaudRate) Then
                ' Baud rate güncellendi
            End If
        End If
    End Sub

    Private Function ValidateCurrentStep() As Boolean
        Select Case _currentStep
            Case 0
                ' Adım 1 - Her zaman geçerli
                Return True
            Case 1
                ' Adım 2 - Port seçilmeli
                If String.IsNullOrEmpty(SelectedPort) Then
                    MessageBox.Show("Lütfen bir COM port seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return False
                End If
                Return True
            Case 2
                ' Adım 3 - Her zaman geçerli (buton tıklanınca geçer)
                Return True
            Case 3
                ' Adım 4 - Protokol algılama bitene kadar beklenir
                Return False ' Protokol algılama bitene kadar Next devre dışı
            Case 4
                ' Adım 5 - Her zaman geçerli
                Return True
            Case Else
                Return True
        End Select
    End Function

#End Region

#Region "Step 3 - Vehicle Check"

    Private Sub BtnReady_Click(sender As Object, e As EventArgs)
        If _waitingTimer IsNot Nothing Then
            _waitingTimer.Stop()
            _waitingTimer.Dispose()
        End If
        BtnNext_Click(sender, e)
    End Sub

    Private Sub WaitingTimer_Tick(sender As Object, e As EventArgs)
        _timeoutCounter -= 1
        If _timeoutCounter <= 0 Then
            _waitingTimer.Stop()
            ' Otomatik olarak ilerle
            BtnReady_Click(sender, e)
        Else
            lblTimeoutInfo.Text = $"{_timeoutCounter} saniye içinde butona tıklamazsanız otomatik olarak ilerleyecektir."
        End If
    End Sub

#End Region

#Region "Step 4 - Protocol Detection"

    Private Async Sub StartProtocolDetection()
        Try
            _protocolDetector = New ProtocolDetector()
            AddHandler _protocolDetector.OnProtocolTested, AddressOf ProtocolDetector_OnProtocolTested
            AddHandler _protocolDetector.OnProtocolDetected, AddressOf ProtocolDetector_OnProtocolDetected
            AddHandler _protocolDetector.OnError, AddressOf ProtocolDetector_OnError

            lblProtocolStatus.Text = "Protokol algılama başlatılıyor..."
            lblProtocolStatus.ForeColor = Color.FromArgb(200, 200, 220)

            Dim detectedProtocol = Await _protocolDetector.DetectProtocolAsync(SelectedPort, SelectedBaudRate)

            If detectedProtocol IsNot Nothing Then
                _detectedProtocolInfo = detectedProtocol
                Dim protocolName As String = detectedProtocol.Name
                DetectedProtocolName = protocolName
                ConnectionSuccessful = True
                lblProtocolStatus.Text = $"✅ Protokol bulundu: {protocolName}"
                lblProtocolStatus.ForeColor = Color.FromArgb(46, 204, 113)
            Else
                ConnectionSuccessful = False
                lblProtocolStatus.Text = "❌ Protokol tespit edilemedi"
                lblProtocolStatus.ForeColor = Color.FromArgb(231, 76, 60)
            End If

            ' Next butonunu aktif et
            btnNext.Enabled = True

        Catch ex As Exception
            ConnectionSuccessful = False
            lblProtocolStatus.Text = $"❌ Hata: {ex.Message}"
            lblProtocolStatus.ForeColor = Color.FromArgb(231, 76, 60)
            btnNext.Enabled = True
        End Try
    End Sub

    Private Sub ProtocolDetector_OnProtocolTested(protocol As ProtocolDetector.ProtocolInfo)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ProtocolDetector_OnProtocolTested(protocol))
            Return
        End If

        _testedProtocols.Add(protocol)
        Dim index As Integer = _testedProtocols.Count - 1

        ' ListBox'ı güncelle
        If index < lstProtocols.Items.Count Then
            Dim status As String = If(protocol.IsDetected, "✓", "✗")
            Dim color As Color = If(protocol.IsDetected, Color.FromArgb(46, 204, 113), Color.FromArgb(231, 76, 60))
            lstProtocols.Items(index) = $"{status} {protocol.Name}"
        End If

        lblProtocolStatus.Text = $"Protokol deneniyor: {protocol.Name}..."
    End Sub

    Private Sub ProtocolDetector_OnProtocolDetected(protocol As ProtocolDetector.ProtocolInfo)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ProtocolDetector_OnProtocolDetected(protocol))
            Return
        End If

        ' Başarılı protokolü vurgula
        Dim index As Integer = -1
        For i As Integer = 0 To _testedProtocols.Count - 1
            If _testedProtocols(i).Name = protocol.Name Then
                index = i
                Exit For
            End If
        Next

        If index >= 0 AndAlso index < lstProtocols.Items.Count Then
            lstProtocols.Items(index) = $"✓ {protocol.Name} [BAŞARILI]"
        End If
    End Sub

    Private Sub ProtocolDetector_OnError(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ProtocolDetector_OnError(message))
            Return
        End If

        lblProtocolStatus.Text = $"⚠️ {message}"
        lblProtocolStatus.ForeColor = Color.FromArgb(230, 126, 34)
    End Sub

#End Region

#Region "Step 5 - Complete"

    Private Sub BtnFinish_Click(sender As Object, e As EventArgs)
        ' Bağlantı ayarlarını kaydet
        If chkSaveConnection.Checked AndAlso ConnectionSuccessful Then
            Try
                ' ConfigManager'a kaydet (varsa)
                ' Şimdilik sadece property'lerde tutuluyor
            Catch
            End Try
        End If

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

#End Region

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        Try
            If _waitingTimer IsNot Nothing Then
                _waitingTimer.Stop()
                _waitingTimer.Dispose()
            End If
            If _protocolDetector IsNot Nothing Then
                _protocolDetector.Dispose()
            End If
        Catch
        End Try
        MyBase.OnFormClosing(e)
    End Sub

End Class

