<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form, bileşen listesini temizlemeyi bırakmayı geçersiz kılar.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows Form Tasarımcısı tarafından gerektirilir
    Private components As System.ComponentModel.IContainer


    'NOT: Aşağıdaki yordam Windows Form Tasarımcısı için gereklidir
    'Windows Form Tasarımcısı kullanılarak değiştirilebilir.  
    'Kod düzenleyicisini kullanarak değiştirmeyin.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.mnuMain = New System.Windows.Forms.MenuStrip()
        Me.mnuTools = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuToolsEcuScanner = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuToolsReport = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuToolsSessionPlayer = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuHelp = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuHelpViewer = New System.Windows.Forms.ToolStripMenuItem()
        Me.tabMain = New System.Windows.Forms.TabControl()
        Me.tabConnection = New System.Windows.Forms.TabPage()
        Me.btnConnect = New System.Windows.Forms.Button()
        Me.btnConnectionWizard = New System.Windows.Forms.Button()
        Me.btnQuickConnect = New System.Windows.Forms.Button()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.lstLog = New System.Windows.Forms.ListBox()
        Me.btnSaveLog = New System.Windows.Forms.Button()
        Me.btnClearLog = New System.Windows.Forms.Button()
        Me.btnTestLog = New System.Windows.Forms.Button()
        Me.grpManualSend = New System.Windows.Forms.GroupBox()
        Me.lblID = New System.Windows.Forms.Label()
        Me.txtID = New System.Windows.Forms.TextBox()
        Me.lblData = New System.Windows.Forms.Label()
        Me.txtData = New System.Windows.Forms.TextBox()
        Me.btnSend = New System.Windows.Forms.Button()
        Me.grpFilter = New System.Windows.Forms.GroupBox()
        Me.cmbFilterMode = New System.Windows.Forms.ComboBox()
        Me.lblFilterId = New System.Windows.Forms.Label()
        Me.txtFilterId = New System.Windows.Forms.TextBox()
        Me.lblFilterFrom = New System.Windows.Forms.Label()
        Me.txtFilterFrom = New System.Windows.Forms.TextBox()
        Me.lblFilterTo = New System.Windows.Forms.Label()
        Me.txtFilterTo = New System.Windows.Forms.TextBox()
        Me.btnApplyFilter = New System.Windows.Forms.Button()
        Me.btnClearFilter = New System.Windows.Forms.Button()
        Me.tabDashboard = New System.Windows.Forms.TabPage()
        Me.btnHelpDashboard = New System.Windows.Forms.Button()
        Me.grpCanAnalysis = New System.Windows.Forms.GroupBox()
        Me.lblRPM = New System.Windows.Forms.Label()
        Me.lblRPMValue = New System.Windows.Forms.Label()
        Me.prgRPM = New System.Windows.Forms.ProgressBar()
        Me.lblSpeed = New System.Windows.Forms.Label()
        Me.lblSpeedValue = New System.Windows.Forms.Label()
        Me.prgSpeed = New System.Windows.Forms.ProgressBar()
        Me.lblTemp = New System.Windows.Forms.Label()
        Me.lblTempValue = New System.Windows.Forms.Label()
        Me.prgTemp = New System.Windows.Forms.ProgressBar()
        Me.lblDoor = New System.Windows.Forms.Label()
        Me.lblDoorValue = New System.Windows.Forms.Label()
        Me.pnlDoorIndicator = New System.Windows.Forms.Panel()
        Me.lblLight = New System.Windows.Forms.Label()
        Me.lblLightValue = New System.Windows.Forms.Label()
        Me.pnlLightIndicator = New System.Windows.Forms.Panel()
        Me.grpOBD = New System.Windows.Forms.GroupBox()
        Me.lblOBDStatus = New System.Windows.Forms.Label()
        Me.btnStartOBD = New System.Windows.Forms.Button()
        Me.btnStopOBD = New System.Windows.Forms.Button()
        Me.lblThrottle = New System.Windows.Forms.Label()
        Me.lblThrottleValue = New System.Windows.Forms.Label()
        Me.lblEngineLoad = New System.Windows.Forms.Label()
        Me.lblEngineLoadValue = New System.Windows.Forms.Label()
        Me.lblIntakeTemp = New System.Windows.Forms.Label()
        Me.lblIntakeTempValue = New System.Windows.Forms.Label()
        Me.lblAmbientTemp = New System.Windows.Forms.Label()
        Me.lblAmbientTempValue = New System.Windows.Forms.Label()
        Me.lblFuelLevel = New System.Windows.Forms.Label()
        Me.lblFuelLevelValue = New System.Windows.Forms.Label()
        Me.lblMAP = New System.Windows.Forms.Label()
        Me.lblMAPValue = New System.Windows.Forms.Label()
        Me.lblMAF = New System.Windows.Forms.Label()
        Me.lblMAFValue = New System.Windows.Forms.Label()
        Me.lblShortTrim = New System.Windows.Forms.Label()
        Me.lblShortTrimValue = New System.Windows.Forms.Label()
        Me.lblLongTrim = New System.Windows.Forms.Label()
        Me.lblLongTrimValue = New System.Windows.Forms.Label()
        Me.lblBarometric = New System.Windows.Forms.Label()
        Me.lblBarometricValue = New System.Windows.Forms.Label()
        Me.lblVoltage = New System.Windows.Forms.Label()
        Me.lblVoltageValue = New System.Windows.Forms.Label()
        Me.lblFuelRate = New System.Windows.Forms.Label()
        Me.lblFuelRateValue = New System.Windows.Forms.Label()
        Me.splitContainerCharts = New System.Windows.Forms.SplitContainer()
        Me.grpChartControls = New System.Windows.Forms.GroupBox()
        Me.btnChartPause = New System.Windows.Forms.Button()
        Me.btnChartClear = New System.Windows.Forms.Button()
        Me.btnChartExport = New System.Windows.Forms.Button()
        Me.cmbChartTimeRange = New System.Windows.Forms.ComboBox()
        Me.lblChartTimeRange = New System.Windows.Forms.Label()
        Me.tabDiagnostics = New System.Windows.Forms.TabPage()
        Me.btnHelpDiagnostics = New System.Windows.Forms.Button()
        Me.grpVINProfile = New System.Windows.Forms.GroupBox()
        Me.chkVINAutoProfile = New System.Windows.Forms.CheckBox()
        Me.lblVINProfileStatus = New System.Windows.Forms.Label()
        Me.lblProfileBrand = New System.Windows.Forms.Label()
        Me.lblProfileBrandValue = New System.Windows.Forms.Label()
        Me.lblProfileModel = New System.Windows.Forms.Label()
        Me.lblProfileModelValue = New System.Windows.Forms.Label()
        Me.lblProfileYear = New System.Windows.Forms.Label()
        Me.lblProfileYearValue = New System.Windows.Forms.Label()
        Me.btnLoadVINProfile = New System.Windows.Forms.Button()
        Me.btnSaveVINProfile = New System.Windows.Forms.Button()
        Me.btnEnrichDatabase = New System.Windows.Forms.Button()
        Me.grpPendingDTC = New System.Windows.Forms.GroupBox()
        Me.lblDTCSearch = New System.Windows.Forms.Label()
        Me.txtDTCSearch = New System.Windows.Forms.TextBox()
        Me.lblDTCCategory = New System.Windows.Forms.Label()
        Me.cmbDTCCategory = New System.Windows.Forms.ComboBox()
        Me.lblDTCCount = New System.Windows.Forms.Label()
        Me.dgvDTCList = New System.Windows.Forms.DataGridView()
        Me.btnReadPendingDTC = New System.Windows.Forms.Button()
        Me.btnReadStoredDTC = New System.Windows.Forms.Button()
        Me.btnClearDTC = New System.Windows.Forms.Button()
        Me.grpFreezeFrame = New System.Windows.Forms.GroupBox()
        Me.lstFreezeFrame = New System.Windows.Forms.ListBox()
        Me.btnReadFreezeFrame = New System.Windows.Forms.Button()
        Me.grpMode06 = New System.Windows.Forms.GroupBox()
        Me.lstMode06 = New System.Windows.Forms.ListBox()
        Me.btnReadMode06 = New System.Windows.Forms.Button()
        Me.grpVehicleInfo = New System.Windows.Forms.GroupBox()
        Me.lblVIN = New System.Windows.Forms.Label()
        Me.lblVINValue = New System.Windows.Forms.Label()
        Me.lblCalibrationID = New System.Windows.Forms.Label()
        Me.lblCalibrationIDValue = New System.Windows.Forms.Label()
        Me.lblCVN = New System.Windows.Forms.Label()
        Me.lblCVNValue = New System.Windows.Forms.Label()
        Me.lblECUName = New System.Windows.Forms.Label()
        Me.lblECUNameValue = New System.Windows.Forms.Label()
        Me.btnReadVehicleInfo = New System.Windows.Forms.Button()
        Me.btnIsoTpTest = New System.Windows.Forms.Button()
        Me.grpPIDAutoScan = New System.Windows.Forms.GroupBox()
        Me.lstSupportedPIDs = New System.Windows.Forms.ListBox()
        Me.btnAutoScanPIDs = New System.Windows.Forms.Button()
        Me.lblScanStatus = New System.Windows.Forms.Label()
        Me.tabCommands = New System.Windows.Forms.TabPage()
        Me.grpCommandSender = New System.Windows.Forms.GroupBox()
        Me.lblBrand = New System.Windows.Forms.Label()
        Me.cmbBrand = New System.Windows.Forms.ComboBox()
        Me.lblModel = New System.Windows.Forms.Label()
        Me.cmbModel = New System.Windows.Forms.ComboBox()
        Me.lblModule = New System.Windows.Forms.Label()
        Me.cmbModule = New System.Windows.Forms.ComboBox()
        Me.lblCommand = New System.Windows.Forms.Label()
        Me.cmbCommand = New System.Windows.Forms.ComboBox()
        Me.txtCommandPreview = New System.Windows.Forms.TextBox()
        Me.btnSendCommand = New System.Windows.Forms.Button()
        Me.tabCoding = New System.Windows.Forms.TabPage()
        Me.grpCoding = New System.Windows.Forms.GroupBox()
        Me.lblCodeBrand = New System.Windows.Forms.Label()
        Me.cmbCodeBrand = New System.Windows.Forms.ComboBox()
        Me.lblCodeModel = New System.Windows.Forms.Label()
        Me.cmbCodeModel = New System.Windows.Forms.ComboBox()
        Me.lblCodeModule = New System.Windows.Forms.Label()
        Me.cmbCodeModule = New System.Windows.Forms.ComboBox()
        Me.lstCodeCommands = New System.Windows.Forms.ListBox()
        Me.lblCodeID = New System.Windows.Forms.Label()
        Me.txtCodeID = New System.Windows.Forms.TextBox()
        Me.lblCodeType = New System.Windows.Forms.Label()
        Me.cmbCodeType = New System.Windows.Forms.ComboBox()
        Me.lblCodeByteIndex = New System.Windows.Forms.Label()
        Me.txtCodeByteIndex = New System.Windows.Forms.TextBox()
        Me.lblCodeData = New System.Windows.Forms.Label()
        Me.txtCodeData = New System.Windows.Forms.TextBox()
        Me.txtCodePreview = New System.Windows.Forms.TextBox()
        Me.btnCodingOn = New System.Windows.Forms.Button()
        Me.btnCodingOff = New System.Windows.Forms.Button()
        Me.btnCodeSend = New System.Windows.Forms.Button()
        Me.btnCodeSaveFromLog = New System.Windows.Forms.Button()
        Me.tabLearning = New System.Windows.Forms.TabPage()
        Me.grpDiff = New System.Windows.Forms.GroupBox()
        Me.lblDiffStatus = New System.Windows.Forms.Label()
        Me.btnStartDiff = New System.Windows.Forms.Button()
        Me.btnStopDiff = New System.Windows.Forms.Button()
        Me.btnClearLearning = New System.Windows.Forms.Button()
        Me.btnExportLearning = New System.Windows.Forms.Button()
        Me.lstDiff = New System.Windows.Forms.ListBox()
        Me.grpAIFinder = New System.Windows.Forms.GroupBox()
        Me.btnSnapshotBefore = New System.Windows.Forms.Button()
        Me.btnSnapshotAfter = New System.Windows.Forms.Button()
        Me.btnAutoDiff = New System.Windows.Forms.Button()
        Me.chkAIFinderEnabled = New System.Windows.Forms.CheckBox()
        Me.lblAIFinderStatus = New System.Windows.Forms.Label()
        Me.btnStartAIFinder = New System.Windows.Forms.Button()
        Me.btnStopAIFinder = New System.Windows.Forms.Button()
        Me.btnAIFinderSaveAll = New System.Windows.Forms.Button()
        Me.lblAIFinderCategory = New System.Windows.Forms.Label()
        Me.cmbAIFinderCategory = New System.Windows.Forms.ComboBox()
        Me.lstAIFinderResults = New System.Windows.Forms.ListBox()
        Me.tabLogAnalysis = New System.Windows.Forms.TabPage()
        Me.grpLogAnalysis = New System.Windows.Forms.GroupBox()
        Me.btnLoadLogFile = New System.Windows.Forms.Button()
        Me.btnAnalyzeLog = New System.Windows.Forms.Button()
        Me.lblIdStats = New System.Windows.Forms.Label()
        Me.lstIdStats = New System.Windows.Forms.ListBox()
        Me.txtAnalysisResult = New System.Windows.Forms.TextBox()
        Me.tabJsonEditor = New System.Windows.Forms.TabPage()
        Me.grpJsonEditor = New System.Windows.Forms.GroupBox()
        Me.lblEditBrand = New System.Windows.Forms.Label()
        Me.cmbEditBrand = New System.Windows.Forms.ComboBox()
        Me.btnNewBrand = New System.Windows.Forms.Button()
        Me.lblEditModel = New System.Windows.Forms.Label()
        Me.cmbEditModel = New System.Windows.Forms.ComboBox()
        Me.btnNewModel = New System.Windows.Forms.Button()
        Me.lblEditModule = New System.Windows.Forms.Label()
        Me.cmbEditModule = New System.Windows.Forms.ComboBox()
        Me.btnNewModule = New System.Windows.Forms.Button()
        Me.lblEditCommands = New System.Windows.Forms.Label()
        Me.lstEditCommands = New System.Windows.Forms.ListBox()
        Me.lblEditCommandName = New System.Windows.Forms.Label()
        Me.txtEditCommandName = New System.Windows.Forms.TextBox()
        Me.lblEditCommandFrame = New System.Windows.Forms.Label()
        Me.txtEditCommandFrame = New System.Windows.Forms.TextBox()
        Me.btnAddCommand = New System.Windows.Forms.Button()
        Me.btnUpdateCommand = New System.Windows.Forms.Button()
        Me.btnDeleteCommand = New System.Windows.Forms.Button()
        Me.btnSaveJson = New System.Windows.Forms.Button()
        Me.btnHelpConnection = New System.Windows.Forms.Button()
        Me.SerialPort1 = New System.IO.Ports.SerialPort(Me.components)
        Me.liveChartTemp = New CarCanReader1._0.LiveChart()
        Me.liveChartSpeed = New CarCanReader1._0.LiveChart()
        Me.liveChartRPM = New CarCanReader1._0.LiveChart()
        Me.mnuMain.SuspendLayout()
        Me.tabMain.SuspendLayout()
        Me.tabConnection.SuspendLayout()
        Me.grpManualSend.SuspendLayout()
        Me.grpFilter.SuspendLayout()
        Me.tabDashboard.SuspendLayout()
        Me.grpCanAnalysis.SuspendLayout()
        Me.grpOBD.SuspendLayout()
        CType(Me.splitContainerCharts, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.splitContainerCharts.Panel2.SuspendLayout()
        Me.splitContainerCharts.SuspendLayout()
        Me.grpChartControls.SuspendLayout()
        Me.tabDiagnostics.SuspendLayout()
        Me.grpVINProfile.SuspendLayout()
        Me.grpPendingDTC.SuspendLayout()
        CType(Me.dgvDTCList, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.grpFreezeFrame.SuspendLayout()
        Me.grpMode06.SuspendLayout()
        Me.grpVehicleInfo.SuspendLayout()
        Me.grpPIDAutoScan.SuspendLayout()
        Me.tabCommands.SuspendLayout()
        Me.grpCommandSender.SuspendLayout()
        Me.tabCoding.SuspendLayout()
        Me.grpCoding.SuspendLayout()
        Me.tabLearning.SuspendLayout()
        Me.grpDiff.SuspendLayout()
        Me.grpAIFinder.SuspendLayout()
        Me.tabLogAnalysis.SuspendLayout()
        Me.grpLogAnalysis.SuspendLayout()
        Me.tabJsonEditor.SuspendLayout()
        Me.grpJsonEditor.SuspendLayout()
        Me.SuspendLayout()
        '
        'mnuMain
        '
        Me.mnuMain.BackColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.mnuMain.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuTools, Me.mnuHelp})
        Me.mnuMain.Location = New System.Drawing.Point(0, 0)
        Me.mnuMain.Name = "mnuMain"
        Me.mnuMain.Size = New System.Drawing.Size(1234, 24)
        Me.mnuMain.TabIndex = 0
        Me.mnuMain.Text = "MenuStrip1"
        '
        'mnuTools
        '
        Me.mnuTools.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuToolsEcuScanner, Me.mnuToolsReport, Me.mnuToolsSessionPlayer})
        Me.mnuTools.ForeColor = System.Drawing.Color.White
        Me.mnuTools.Name = "mnuTools"
        Me.mnuTools.Size = New System.Drawing.Size(46, 20)
        Me.mnuTools.Text = "Araçlar"
        '
        'mnuToolsEcuScanner
        '
        Me.mnuToolsEcuScanner.Name = "mnuToolsEcuScanner"
        Me.mnuToolsEcuScanner.Size = New System.Drawing.Size(180, 22)
        Me.mnuToolsEcuScanner.Text = "ECU Tarama"
        '
        'mnuToolsReport
        '
        Me.mnuToolsReport.Name = "mnuToolsReport"
        Me.mnuToolsReport.Size = New System.Drawing.Size(180, 22)
        Me.mnuToolsReport.Text = "PDF Rapor"
        '
        'mnuToolsSessionPlayer
        '
        Me.mnuToolsSessionPlayer.Name = "mnuToolsSessionPlayer"
        Me.mnuToolsSessionPlayer.Size = New System.Drawing.Size(180, 22)
        Me.mnuToolsSessionPlayer.Text = "Oturum Oynatıcı"
        '
        'mnuHelp
        '
        Me.mnuHelp.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuHelpViewer})
        Me.mnuHelp.ForeColor = System.Drawing.Color.White
        Me.mnuHelp.Name = "mnuHelp"
        Me.mnuHelp.Size = New System.Drawing.Size(44, 20)
        Me.mnuHelp.Text = "Yardım"
        '
        'mnuHelpViewer
        '
        Me.mnuHelpViewer.Name = "mnuHelpViewer"
        Me.mnuHelpViewer.Size = New System.Drawing.Size(134, 22)
        Me.mnuHelpViewer.Text = "Yardım (F1)"
        '
        'tabMain
        '
        Me.tabMain.Controls.Add(Me.tabConnection)
        Me.tabMain.Controls.Add(Me.tabDashboard)
        Me.tabMain.Controls.Add(Me.tabDiagnostics)
        Me.tabMain.Controls.Add(Me.tabCommands)
        Me.tabMain.Controls.Add(Me.tabCoding)
        Me.tabMain.Controls.Add(Me.tabLearning)
        Me.tabMain.Controls.Add(Me.tabLogAnalysis)
        Me.tabMain.Controls.Add(Me.tabJsonEditor)
        Me.tabMain.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabMain.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.tabMain.Location = New System.Drawing.Point(0, 0)
        Me.tabMain.Name = "tabMain"
        Me.tabMain.SelectedIndex = 0
        Me.tabMain.Size = New System.Drawing.Size(1234, 711)
        Me.tabMain.TabIndex = 0
        '
        'tabConnection
        '
        Me.tabConnection.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.tabConnection.Controls.Add(Me.btnConnect)
        Me.tabConnection.Controls.Add(Me.btnConnectionWizard)
        Me.tabConnection.Controls.Add(Me.btnQuickConnect)
        Me.tabConnection.Controls.Add(Me.lblStatus)
        Me.tabConnection.Controls.Add(Me.lstLog)
        Me.tabConnection.Controls.Add(Me.btnSaveLog)
        Me.tabConnection.Controls.Add(Me.btnClearLog)
        Me.tabConnection.Controls.Add(Me.btnTestLog)
        Me.tabConnection.Controls.Add(Me.grpManualSend)
        Me.tabConnection.Controls.Add(Me.grpFilter)
        Me.tabConnection.Location = New System.Drawing.Point(4, 26)
        Me.tabConnection.Name = "tabConnection"
        Me.tabConnection.Padding = New System.Windows.Forms.Padding(10)
        Me.tabConnection.Size = New System.Drawing.Size(1226, 681)
        Me.tabConnection.TabIndex = 0
        Me.tabConnection.Text = "🔌 Bağlantı"
        '
        'btnConnect
        '
        Me.btnConnect.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnConnect.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnConnect.ForeColor = System.Drawing.Color.White
        Me.btnConnect.Location = New System.Drawing.Point(15, 15)
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.Size = New System.Drawing.Size(120, 35)
        Me.btnConnect.TabIndex = 0
        Me.btnConnect.Text = "🔗 Bağlan"
        Me.btnConnect.UseVisualStyleBackColor = False
        '
        'btnConnectionWizard
        '
        Me.btnConnectionWizard.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnConnectionWizard.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnConnectionWizard.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnConnectionWizard.ForeColor = System.Drawing.Color.White
        Me.btnConnectionWizard.Location = New System.Drawing.Point(145, 15)
        Me.btnConnectionWizard.Name = "btnConnectionWizard"
        Me.btnConnectionWizard.Size = New System.Drawing.Size(140, 35)
        Me.btnConnectionWizard.TabIndex = 10
        Me.btnConnectionWizard.Text = "🧙 Wizard"
        Me.btnConnectionWizard.UseVisualStyleBackColor = False
        '
        'btnQuickConnect
        '
        Me.btnQuickConnect.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnQuickConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnQuickConnect.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnQuickConnect.ForeColor = System.Drawing.Color.White
        Me.btnQuickConnect.Location = New System.Drawing.Point(295, 15)
        Me.btnQuickConnect.Name = "btnQuickConnect"
        Me.btnQuickConnect.Size = New System.Drawing.Size(120, 35)
        Me.btnQuickConnect.TabIndex = 11
        Me.btnQuickConnect.Text = "⚡ Hızlı Bağlan"
        Me.btnQuickConnect.UseVisualStyleBackColor = False
        '
        'lblStatus
        '
        Me.lblStatus.AutoSize = True
        Me.lblStatus.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblStatus.Location = New System.Drawing.Point(430, 22)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(144, 19)
        Me.lblStatus.TabIndex = 1
        Me.lblStatus.Text = "⚪ Durum: Bağlı değil"
        '
        'lstLog
        '
        Me.lstLog.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lstLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lstLog.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.lstLog.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lstLog.FormattingEnabled = True
        Me.lstLog.ItemHeight = 14
        Me.lstLog.Location = New System.Drawing.Point(15, 60)
        Me.lstLog.Name = "lstLog"
        Me.lstLog.Size = New System.Drawing.Size(550, 410)
        Me.lstLog.TabIndex = 2
        '
        'btnSaveLog
        '
        Me.btnSaveLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnSaveLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSaveLog.ForeColor = System.Drawing.Color.White
        Me.btnSaveLog.Location = New System.Drawing.Point(15, 490)
        Me.btnSaveLog.Name = "btnSaveLog"
        Me.btnSaveLog.Size = New System.Drawing.Size(130, 30)
        Me.btnSaveLog.TabIndex = 3
        Me.btnSaveLog.Text = "💾 Log Kaydet"
        Me.btnSaveLog.UseVisualStyleBackColor = False
        '
        'btnClearLog
        '
        Me.btnClearLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClearLog.ForeColor = System.Drawing.Color.White
        Me.btnClearLog.Location = New System.Drawing.Point(155, 490)
        Me.btnClearLog.Name = "btnClearLog"
        Me.btnClearLog.Size = New System.Drawing.Size(130, 30)
        Me.btnClearLog.TabIndex = 4
        Me.btnClearLog.Text = "🗑️ Temizle"
        Me.btnClearLog.UseVisualStyleBackColor = False
        '
        'btnTestLog
        '
        Me.btnTestLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnTestLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnTestLog.ForeColor = System.Drawing.Color.White
        Me.btnTestLog.Location = New System.Drawing.Point(295, 490)
        Me.btnTestLog.Name = "btnTestLog"
        Me.btnTestLog.Size = New System.Drawing.Size(130, 30)
        Me.btnTestLog.TabIndex = 5
        Me.btnTestLog.Text = "🧪 Test Veri"
        Me.btnTestLog.UseVisualStyleBackColor = False
        '
        'grpManualSend
        '
        Me.grpManualSend.Controls.Add(Me.lblID)
        Me.grpManualSend.Controls.Add(Me.txtID)
        Me.grpManualSend.Controls.Add(Me.lblData)
        Me.grpManualSend.Controls.Add(Me.txtData)
        Me.grpManualSend.Controls.Add(Me.btnSend)
        Me.grpManualSend.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpManualSend.Location = New System.Drawing.Point(580, 60)
        Me.grpManualSend.Name = "grpManualSend"
        Me.grpManualSend.Size = New System.Drawing.Size(280, 180)
        Me.grpManualSend.TabIndex = 6
        Me.grpManualSend.TabStop = False
        Me.grpManualSend.Text = "📤 Manuel CAN Gönder"
        '
        'lblID
        '
        Me.lblID.AutoSize = True
        Me.lblID.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblID.Location = New System.Drawing.Point(15, 35)
        Me.lblID.Name = "lblID"
        Me.lblID.Size = New System.Drawing.Size(81, 15)
        Me.lblID.TabIndex = 0
        Me.lblID.Text = "CAN ID (Hex):"
        '
        'txtID
        '
        Me.txtID.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtID.Font = New System.Drawing.Font("Consolas", 11.0!)
        Me.txtID.Location = New System.Drawing.Point(120, 32)
        Me.txtID.MaxLength = 3
        Me.txtID.Name = "txtID"
        Me.txtID.Size = New System.Drawing.Size(140, 25)
        Me.txtID.TabIndex = 1
        '
        'lblData
        '
        Me.lblData.AutoSize = True
        Me.lblData.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblData.Location = New System.Drawing.Point(15, 70)
        Me.lblData.Name = "lblData"
        Me.lblData.Size = New System.Drawing.Size(66, 15)
        Me.lblData.TabIndex = 2
        Me.lblData.Text = "Data (Hex):"
        '
        'txtData
        '
        Me.txtData.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtData.Font = New System.Drawing.Font("Consolas", 11.0!)
        Me.txtData.Location = New System.Drawing.Point(120, 67)
        Me.txtData.MaxLength = 16
        Me.txtData.Name = "txtData"
        Me.txtData.Size = New System.Drawing.Size(140, 25)
        Me.txtData.TabIndex = 3
        '
        'btnSend
        '
        Me.btnSend.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSend.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnSend.ForeColor = System.Drawing.Color.White
        Me.btnSend.Location = New System.Drawing.Point(15, 120)
        Me.btnSend.Name = "btnSend"
        Me.btnSend.Size = New System.Drawing.Size(245, 40)
        Me.btnSend.TabIndex = 4
        Me.btnSend.Text = "📨 Gönder"
        Me.btnSend.UseVisualStyleBackColor = False
        '
        'grpFilter
        '
        Me.grpFilter.Controls.Add(Me.cmbFilterMode)
        Me.grpFilter.Controls.Add(Me.lblFilterId)
        Me.grpFilter.Controls.Add(Me.txtFilterId)
        Me.grpFilter.Controls.Add(Me.lblFilterFrom)
        Me.grpFilter.Controls.Add(Me.txtFilterFrom)
        Me.grpFilter.Controls.Add(Me.lblFilterTo)
        Me.grpFilter.Controls.Add(Me.txtFilterTo)
        Me.grpFilter.Controls.Add(Me.btnApplyFilter)
        Me.grpFilter.Controls.Add(Me.btnClearFilter)
        Me.grpFilter.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpFilter.Location = New System.Drawing.Point(580, 250)
        Me.grpFilter.Name = "grpFilter"
        Me.grpFilter.Size = New System.Drawing.Size(280, 230)
        Me.grpFilter.TabIndex = 7
        Me.grpFilter.TabStop = False
        Me.grpFilter.Text = "🔍 CAN Filtresi"
        '
        'cmbFilterMode
        '
        Me.cmbFilterMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbFilterMode.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.cmbFilterMode.Items.AddRange(New Object() {"Sadece bu ID", "Bu ID'yi gizle", "ID aralığını göster", "ID aralığını gizle"})
        Me.cmbFilterMode.Location = New System.Drawing.Point(15, 30)
        Me.cmbFilterMode.Name = "cmbFilterMode"
        Me.cmbFilterMode.Size = New System.Drawing.Size(245, 23)
        Me.cmbFilterMode.TabIndex = 0
        '
        'lblFilterId
        '
        Me.lblFilterId.AutoSize = True
        Me.lblFilterId.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblFilterId.Location = New System.Drawing.Point(15, 65)
        Me.lblFilterId.Name = "lblFilterId"
        Me.lblFilterId.Size = New System.Drawing.Size(21, 15)
        Me.lblFilterId.TabIndex = 1
        Me.lblFilterId.Text = "ID:"
        '
        'txtFilterId
        '
        Me.txtFilterId.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtFilterId.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtFilterId.Location = New System.Drawing.Point(60, 62)
        Me.txtFilterId.Name = "txtFilterId"
        Me.txtFilterId.Size = New System.Drawing.Size(80, 23)
        Me.txtFilterId.TabIndex = 2
        '
        'lblFilterFrom
        '
        Me.lblFilterFrom.AutoSize = True
        Me.lblFilterFrom.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblFilterFrom.Location = New System.Drawing.Point(15, 100)
        Me.lblFilterFrom.Name = "lblFilterFrom"
        Me.lblFilterFrom.Size = New System.Drawing.Size(60, 15)
        Me.lblFilterFrom.TabIndex = 3
        Me.lblFilterFrom.Text = "Başlangıç:"
        '
        'txtFilterFrom
        '
        Me.txtFilterFrom.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtFilterFrom.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtFilterFrom.Location = New System.Drawing.Point(90, 97)
        Me.txtFilterFrom.Name = "txtFilterFrom"
        Me.txtFilterFrom.Size = New System.Drawing.Size(70, 23)
        Me.txtFilterFrom.TabIndex = 4
        '
        'lblFilterTo
        '
        Me.lblFilterTo.AutoSize = True
        Me.lblFilterTo.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblFilterTo.Location = New System.Drawing.Point(170, 100)
        Me.lblFilterTo.Name = "lblFilterTo"
        Me.lblFilterTo.Size = New System.Drawing.Size(32, 15)
        Me.lblFilterTo.TabIndex = 5
        Me.lblFilterTo.Text = "Bitiş:"
        '
        'txtFilterTo
        '
        Me.txtFilterTo.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtFilterTo.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtFilterTo.Location = New System.Drawing.Point(205, 97)
        Me.txtFilterTo.Name = "txtFilterTo"
        Me.txtFilterTo.Size = New System.Drawing.Size(55, 23)
        Me.txtFilterTo.TabIndex = 6
        '
        'btnApplyFilter
        '
        Me.btnApplyFilter.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnApplyFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnApplyFilter.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnApplyFilter.ForeColor = System.Drawing.Color.White
        Me.btnApplyFilter.Location = New System.Drawing.Point(15, 140)
        Me.btnApplyFilter.Name = "btnApplyFilter"
        Me.btnApplyFilter.Size = New System.Drawing.Size(120, 35)
        Me.btnApplyFilter.TabIndex = 7
        Me.btnApplyFilter.Text = "✅ Uygula"
        Me.btnApplyFilter.UseVisualStyleBackColor = False
        '
        'btnClearFilter
        '
        Me.btnClearFilter.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnClearFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClearFilter.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnClearFilter.ForeColor = System.Drawing.Color.White
        Me.btnClearFilter.Location = New System.Drawing.Point(145, 140)
        Me.btnClearFilter.Name = "btnClearFilter"
        Me.btnClearFilter.Size = New System.Drawing.Size(115, 35)
        Me.btnClearFilter.TabIndex = 8
        Me.btnClearFilter.Text = "❌ Temizle"
        Me.btnClearFilter.UseVisualStyleBackColor = False
        '
        'tabDashboard
        '
        Me.tabDashboard.BackColor = System.Drawing.Color.FromArgb(CType(CType(25, Byte), Integer), CType(CType(25, Byte), Integer), CType(CType(35, Byte), Integer))
        Me.tabDashboard.Controls.Add(Me.btnHelpDashboard)
        Me.tabDashboard.Controls.Add(Me.grpCanAnalysis)
        Me.tabDashboard.Controls.Add(Me.grpOBD)
        Me.tabDashboard.Controls.Add(Me.splitContainerCharts)
        Me.tabDashboard.Controls.Add(Me.grpChartControls)
        Me.tabDashboard.Location = New System.Drawing.Point(4, 26)
        Me.tabDashboard.Name = "tabDashboard"
        Me.tabDashboard.Size = New System.Drawing.Size(1226, 681)
        Me.tabDashboard.TabIndex = 1
        Me.tabDashboard.Text = "📊 Dashboard"
        '
        'btnHelpDashboard
        '
        Me.btnHelpDashboard.Location = New System.Drawing.Point(0, 0)
        Me.btnHelpDashboard.Name = "btnHelpDashboard"
        Me.btnHelpDashboard.Size = New System.Drawing.Size(75, 23)
        Me.btnHelpDashboard.TabIndex = 0
        '
        'grpCanAnalysis
        '
        Me.grpCanAnalysis.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.grpCanAnalysis.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(35, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.grpCanAnalysis.Controls.Add(Me.lblRPM)
        Me.grpCanAnalysis.Controls.Add(Me.lblRPMValue)
        Me.grpCanAnalysis.Controls.Add(Me.prgRPM)
        Me.grpCanAnalysis.Controls.Add(Me.lblSpeed)
        Me.grpCanAnalysis.Controls.Add(Me.lblSpeedValue)
        Me.grpCanAnalysis.Controls.Add(Me.prgSpeed)
        Me.grpCanAnalysis.Controls.Add(Me.lblTemp)
        Me.grpCanAnalysis.Controls.Add(Me.lblTempValue)
        Me.grpCanAnalysis.Controls.Add(Me.prgTemp)
        Me.grpCanAnalysis.Controls.Add(Me.lblDoor)
        Me.grpCanAnalysis.Controls.Add(Me.lblDoorValue)
        Me.grpCanAnalysis.Controls.Add(Me.pnlDoorIndicator)
        Me.grpCanAnalysis.Controls.Add(Me.lblLight)
        Me.grpCanAnalysis.Controls.Add(Me.lblLightValue)
        Me.grpCanAnalysis.Controls.Add(Me.pnlLightIndicator)
        Me.grpCanAnalysis.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.grpCanAnalysis.ForeColor = System.Drawing.Color.White
        Me.grpCanAnalysis.Location = New System.Drawing.Point(20, 20)
        Me.grpCanAnalysis.Name = "grpCanAnalysis"
        Me.grpCanAnalysis.Size = New System.Drawing.Size(500, 280)
        Me.grpCanAnalysis.TabIndex = 0
        Me.grpCanAnalysis.TabStop = False
        Me.grpCanAnalysis.Text = "🚗 Araç Verileri"
        '
        'lblRPM
        '
        Me.lblRPM.AutoSize = True
        Me.lblRPM.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblRPM.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(193, Byte), Integer), CType(CType(7, Byte), Integer))
        Me.lblRPM.Location = New System.Drawing.Point(20, 40)
        Me.lblRPM.Name = "lblRPM"
        Me.lblRPM.Size = New System.Drawing.Size(85, 25)
        Me.lblRPM.TabIndex = 0
        Me.lblRPM.Text = "⚡ Motor Devri:"
        '
        'lblRPMValue
        '
        Me.lblRPMValue.AutoSize = True
        Me.lblRPMValue.Font = New System.Drawing.Font("Segoe UI", 24.0!, System.Drawing.FontStyle.Bold)
        Me.lblRPMValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblRPMValue.Location = New System.Drawing.Point(120, 40)
        Me.lblRPMValue.Name = "lblRPMValue"
        Me.lblRPMValue.Size = New System.Drawing.Size(38, 45)
        Me.lblRPMValue.TabIndex = 1
        Me.lblRPMValue.Text = "0"
        '
        'prgRPM
        '
        Me.prgRPM.Location = New System.Drawing.Point(20, 85)
        Me.prgRPM.Maximum = 8000
        Me.prgRPM.Name = "prgRPM"
        Me.prgRPM.Size = New System.Drawing.Size(450, 20)
        Me.prgRPM.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        Me.prgRPM.TabIndex = 2
        '
        'lblSpeed
        '
        Me.lblSpeed.AutoSize = True
        Me.lblSpeed.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblSpeed.ForeColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.lblSpeed.Location = New System.Drawing.Point(20, 120)
        Me.lblSpeed.Name = "lblSpeed"
        Me.lblSpeed.Size = New System.Drawing.Size(73, 25)
        Me.lblSpeed.TabIndex = 3
        Me.lblSpeed.Text = "🏎️ Hız:"
        '
        'lblSpeedValue
        '
        Me.lblSpeedValue.AutoSize = True
        Me.lblSpeedValue.Font = New System.Drawing.Font("Segoe UI", 24.0!, System.Drawing.FontStyle.Bold)
        Me.lblSpeedValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblSpeedValue.Location = New System.Drawing.Point(120, 120)
        Me.lblSpeedValue.Name = "lblSpeedValue"
        Me.lblSpeedValue.Size = New System.Drawing.Size(127, 45)
        Me.lblSpeedValue.TabIndex = 4
        Me.lblSpeedValue.Text = "0 km/h"
        '
        'prgSpeed
        '
        Me.prgSpeed.Location = New System.Drawing.Point(20, 165)
        Me.prgSpeed.Maximum = 260
        Me.prgSpeed.Name = "prgSpeed"
        Me.prgSpeed.Size = New System.Drawing.Size(450, 20)
        Me.prgSpeed.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        Me.prgSpeed.TabIndex = 5
        '
        'lblTemp
        '
        Me.lblTemp.AutoSize = True
        Me.lblTemp.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblTemp.ForeColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.lblTemp.Location = New System.Drawing.Point(20, 200)
        Me.lblTemp.Name = "lblTemp"
        Me.lblTemp.Size = New System.Drawing.Size(103, 25)
        Me.lblTemp.TabIndex = 6
        Me.lblTemp.Text = "🌡️ Soğutucu Sıcaklığı:"
        '
        'lblTempValue
        '
        Me.lblTempValue.AutoSize = True
        Me.lblTempValue.Font = New System.Drawing.Font("Segoe UI", 24.0!, System.Drawing.FontStyle.Bold)
        Me.lblTempValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblTempValue.Location = New System.Drawing.Point(150, 200)
        Me.lblTempValue.Name = "lblTempValue"
        Me.lblTempValue.Size = New System.Drawing.Size(79, 45)
        Me.lblTempValue.TabIndex = 7
        Me.lblTempValue.Text = "0 °C"
        '
        'prgTemp
        '
        Me.prgTemp.Location = New System.Drawing.Point(20, 245)
        Me.prgTemp.Maximum = 150
        Me.prgTemp.Name = "prgTemp"
        Me.prgTemp.Size = New System.Drawing.Size(450, 20)
        Me.prgTemp.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        Me.prgTemp.TabIndex = 8
        '
        'lblDoor
        '
        Me.lblDoor.AutoSize = True
        Me.lblDoor.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblDoor.ForeColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.lblDoor.Location = New System.Drawing.Point(20, 290)
        Me.lblDoor.Name = "lblDoor"
        Me.lblDoor.Size = New System.Drawing.Size(78, 25)
        Me.lblDoor.TabIndex = 9
        Me.lblDoor.Text = "🚪 Kapı:"
        '
        'lblDoorValue
        '
        Me.lblDoorValue.AutoSize = True
        Me.lblDoorValue.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Bold)
        Me.lblDoorValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblDoorValue.Location = New System.Drawing.Point(120, 290)
        Me.lblDoorValue.Name = "lblDoorValue"
        Me.lblDoorValue.Size = New System.Drawing.Size(85, 32)
        Me.lblDoorValue.TabIndex = 10
        Me.lblDoorValue.Text = "Kapalı"
        '
        'pnlDoorIndicator
        '
        Me.pnlDoorIndicator.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.pnlDoorIndicator.Location = New System.Drawing.Point(250, 290)
        Me.pnlDoorIndicator.Name = "pnlDoorIndicator"
        Me.pnlDoorIndicator.Size = New System.Drawing.Size(30, 30)
        Me.pnlDoorIndicator.TabIndex = 11
        '
        'lblLight
        '
        Me.lblLight.AutoSize = True
        Me.lblLight.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblLight.ForeColor = System.Drawing.Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(196, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.lblLight.Location = New System.Drawing.Point(20, 340)
        Me.lblLight.Name = "lblLight"
        Me.lblLight.Size = New System.Drawing.Size(66, 25)
        Me.lblLight.TabIndex = 12
        Me.lblLight.Text = "💡 Far:"
        '
        'lblLightValue
        '
        Me.lblLightValue.AutoSize = True
        Me.lblLightValue.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Bold)
        Me.lblLightValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblLightValue.Location = New System.Drawing.Point(120, 340)
        Me.lblLightValue.Name = "lblLightValue"
        Me.lblLightValue.Size = New System.Drawing.Size(85, 32)
        Me.lblLightValue.TabIndex = 13
        Me.lblLightValue.Text = "Kapalı"
        '
        'pnlLightIndicator
        '
        Me.pnlLightIndicator.BackColor = System.Drawing.Color.Gray
        Me.pnlLightIndicator.Location = New System.Drawing.Point(250, 340)
        Me.pnlLightIndicator.Name = "pnlLightIndicator"
        Me.pnlLightIndicator.Size = New System.Drawing.Size(30, 30)
        Me.pnlLightIndicator.TabIndex = 14
        '
        'grpOBD
        '
        Me.grpOBD.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpOBD.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(35, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.grpOBD.Controls.Add(Me.lblOBDStatus)
        Me.grpOBD.Controls.Add(Me.btnStartOBD)
        Me.grpOBD.Controls.Add(Me.btnStopOBD)
        Me.grpOBD.Controls.Add(Me.lblThrottle)
        Me.grpOBD.Controls.Add(Me.lblThrottleValue)
        Me.grpOBD.Controls.Add(Me.lblEngineLoad)
        Me.grpOBD.Controls.Add(Me.lblEngineLoadValue)
        Me.grpOBD.Controls.Add(Me.lblIntakeTemp)
        Me.grpOBD.Controls.Add(Me.lblIntakeTempValue)
        Me.grpOBD.Controls.Add(Me.lblAmbientTemp)
        Me.grpOBD.Controls.Add(Me.lblAmbientTempValue)
        Me.grpOBD.Controls.Add(Me.lblFuelLevel)
        Me.grpOBD.Controls.Add(Me.lblFuelLevelValue)
        Me.grpOBD.Controls.Add(Me.lblMAP)
        Me.grpOBD.Controls.Add(Me.lblMAPValue)
        Me.grpOBD.Controls.Add(Me.lblMAF)
        Me.grpOBD.Controls.Add(Me.lblMAFValue)
        Me.grpOBD.Controls.Add(Me.lblShortTrim)
        Me.grpOBD.Controls.Add(Me.lblShortTrimValue)
        Me.grpOBD.Controls.Add(Me.lblLongTrim)
        Me.grpOBD.Controls.Add(Me.lblLongTrimValue)
        Me.grpOBD.Controls.Add(Me.lblBarometric)
        Me.grpOBD.Controls.Add(Me.lblBarometricValue)
        Me.grpOBD.Controls.Add(Me.lblVoltage)
        Me.grpOBD.Controls.Add(Me.lblVoltageValue)
        Me.grpOBD.Controls.Add(Me.lblFuelRate)
        Me.grpOBD.Controls.Add(Me.lblFuelRateValue)
        Me.grpOBD.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.grpOBD.ForeColor = System.Drawing.Color.White
        Me.grpOBD.Location = New System.Drawing.Point(530, 20)
        Me.grpOBD.Name = "grpOBD"
        Me.grpOBD.Size = New System.Drawing.Size(674, 280)
        Me.grpOBD.TabIndex = 1
        Me.grpOBD.TabStop = False
        Me.grpOBD.Text = "📡 OBD-II Sensörleri"
        '
        'lblOBDStatus
        '
        Me.lblOBDStatus.AutoSize = True
        Me.lblOBDStatus.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblOBDStatus.ForeColor = System.Drawing.Color.Gray
        Me.lblOBDStatus.Location = New System.Drawing.Point(20, 30)
        Me.lblOBDStatus.Name = "lblOBDStatus"
        Me.lblOBDStatus.Size = New System.Drawing.Size(148, 19)
        Me.lblOBDStatus.TabIndex = 0
        Me.lblOBDStatus.Text = "⚪ OBD Polling: Kapalı"
        '
        'btnStartOBD
        '
        Me.btnStartOBD.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnStartOBD.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStartOBD.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnStartOBD.ForeColor = System.Drawing.Color.White
        Me.btnStartOBD.Location = New System.Drawing.Point(250, 25)
        Me.btnStartOBD.Name = "btnStartOBD"
        Me.btnStartOBD.Size = New System.Drawing.Size(100, 30)
        Me.btnStartOBD.TabIndex = 1
        Me.btnStartOBD.Text = "▶ Başlat"
        Me.btnStartOBD.UseVisualStyleBackColor = False
        '
        'btnStopOBD
        '
        Me.btnStopOBD.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnStopOBD.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStopOBD.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnStopOBD.ForeColor = System.Drawing.Color.White
        Me.btnStopOBD.Location = New System.Drawing.Point(360, 25)
        Me.btnStopOBD.Name = "btnStopOBD"
        Me.btnStopOBD.Size = New System.Drawing.Size(100, 30)
        Me.btnStopOBD.TabIndex = 2
        Me.btnStopOBD.Text = "⏹ Durdur"
        Me.btnStopOBD.UseVisualStyleBackColor = False
        '
        'lblThrottle
        '
        Me.lblThrottle.AutoSize = True
        Me.lblThrottle.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblThrottle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(196, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.lblThrottle.Location = New System.Drawing.Point(20, 70)
        Me.lblThrottle.Name = "lblThrottle"
        Me.lblThrottle.Size = New System.Drawing.Size(108, 19)
        Me.lblThrottle.TabIndex = 3
        Me.lblThrottle.Text = "🎚️ Gaz Kelebeği:"
        '
        'lblThrottleValue
        '
        Me.lblThrottleValue.AutoSize = True
        Me.lblThrottleValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblThrottleValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblThrottleValue.Location = New System.Drawing.Point(160, 70)
        Me.lblThrottleValue.Name = "lblThrottleValue"
        Me.lblThrottleValue.Size = New System.Drawing.Size(37, 21)
        Me.lblThrottleValue.TabIndex = 4
        Me.lblThrottleValue.Text = "0 %"
        '
        'lblEngineLoad
        '
        Me.lblEngineLoad.AutoSize = True
        Me.lblEngineLoad.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEngineLoad.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(193, Byte), Integer), CType(CType(7, Byte), Integer))
        Me.lblEngineLoad.Location = New System.Drawing.Point(20, 105)
        Me.lblEngineLoad.Name = "lblEngineLoad"
        Me.lblEngineLoad.Size = New System.Drawing.Size(109, 19)
        Me.lblEngineLoad.TabIndex = 5
        Me.lblEngineLoad.Text = "⚙️ Motor Yükü:"
        '
        'lblEngineLoadValue
        '
        Me.lblEngineLoadValue.AutoSize = True
        Me.lblEngineLoadValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblEngineLoadValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblEngineLoadValue.Location = New System.Drawing.Point(160, 105)
        Me.lblEngineLoadValue.Name = "lblEngineLoadValue"
        Me.lblEngineLoadValue.Size = New System.Drawing.Size(37, 21)
        Me.lblEngineLoadValue.TabIndex = 6
        Me.lblEngineLoadValue.Text = "0 %"
        '
        'lblIntakeTemp
        '
        Me.lblIntakeTemp.AutoSize = True
        Me.lblIntakeTemp.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblIntakeTemp.ForeColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.lblIntakeTemp.Location = New System.Drawing.Point(20, 140)
        Me.lblIntakeTemp.Name = "lblIntakeTemp"
        Me.lblIntakeTemp.Size = New System.Drawing.Size(108, 19)
        Me.lblIntakeTemp.TabIndex = 7
        Me.lblIntakeTemp.Text = "🌬️ Emme Hava:"
        '
        'lblIntakeTempValue
        '
        Me.lblIntakeTempValue.AutoSize = True
        Me.lblIntakeTempValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblIntakeTempValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblIntakeTempValue.Location = New System.Drawing.Point(160, 140)
        Me.lblIntakeTempValue.Name = "lblIntakeTempValue"
        Me.lblIntakeTempValue.Size = New System.Drawing.Size(39, 21)
        Me.lblIntakeTempValue.TabIndex = 8
        Me.lblIntakeTempValue.Text = "0 °C"
        '
        'lblAmbientTemp
        '
        Me.lblAmbientTemp.AutoSize = True
        Me.lblAmbientTemp.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblAmbientTemp.ForeColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.lblAmbientTemp.Location = New System.Drawing.Point(20, 175)
        Me.lblAmbientTemp.Name = "lblAmbientTemp"
        Me.lblAmbientTemp.Size = New System.Drawing.Size(67, 19)
        Me.lblAmbientTemp.TabIndex = 9
        Me.lblAmbientTemp.Text = "🌡️ Ortam:"
        '
        'lblAmbientTempValue
        '
        Me.lblAmbientTempValue.AutoSize = True
        Me.lblAmbientTempValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblAmbientTempValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblAmbientTempValue.Location = New System.Drawing.Point(160, 175)
        Me.lblAmbientTempValue.Name = "lblAmbientTempValue"
        Me.lblAmbientTempValue.Size = New System.Drawing.Size(39, 21)
        Me.lblAmbientTempValue.TabIndex = 10
        Me.lblAmbientTempValue.Text = "0 °C"
        '
        'lblFuelLevel
        '
        Me.lblFuelLevel.AutoSize = True
        Me.lblFuelLevel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblFuelLevel.ForeColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(126, Byte), Integer), CType(CType(34, Byte), Integer))
        Me.lblFuelLevel.Location = New System.Drawing.Point(20, 210)
        Me.lblFuelLevel.Name = "lblFuelLevel"
        Me.lblFuelLevel.Size = New System.Drawing.Size(60, 19)
        Me.lblFuelLevel.TabIndex = 11
        Me.lblFuelLevel.Text = "⛽ Yakıt:"
        '
        'lblFuelLevelValue
        '
        Me.lblFuelLevelValue.AutoSize = True
        Me.lblFuelLevelValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblFuelLevelValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblFuelLevelValue.Location = New System.Drawing.Point(160, 210)
        Me.lblFuelLevelValue.Name = "lblFuelLevelValue"
        Me.lblFuelLevelValue.Size = New System.Drawing.Size(37, 21)
        Me.lblFuelLevelValue.TabIndex = 12
        Me.lblFuelLevelValue.Text = "0 %"
        '
        'lblMAP
        '
        Me.lblMAP.AutoSize = True
        Me.lblMAP.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblMAP.ForeColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.lblMAP.Location = New System.Drawing.Point(20, 245)
        Me.lblMAP.Name = "lblMAP"
        Me.lblMAP.Size = New System.Drawing.Size(64, 19)
        Me.lblMAP.TabIndex = 13
        Me.lblMAP.Text = "📊 MAP:"
        '
        'lblMAPValue
        '
        Me.lblMAPValue.AutoSize = True
        Me.lblMAPValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblMAPValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblMAPValue.Location = New System.Drawing.Point(160, 245)
        Me.lblMAPValue.Name = "lblMAPValue"
        Me.lblMAPValue.Size = New System.Drawing.Size(51, 21)
        Me.lblMAPValue.TabIndex = 14
        Me.lblMAPValue.Text = "0 kPa"
        '
        'lblMAF
        '
        Me.lblMAF.AutoSize = True
        Me.lblMAF.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblMAF.ForeColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.lblMAF.Location = New System.Drawing.Point(320, 70)
        Me.lblMAF.Name = "lblMAF"
        Me.lblMAF.Size = New System.Drawing.Size(64, 19)
        Me.lblMAF.TabIndex = 15
        Me.lblMAF.Text = "💨 MAF:"
        '
        'lblMAFValue
        '
        Me.lblMAFValue.AutoSize = True
        Me.lblMAFValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblMAFValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblMAFValue.Location = New System.Drawing.Point(460, 70)
        Me.lblMAFValue.Name = "lblMAFValue"
        Me.lblMAFValue.Size = New System.Drawing.Size(47, 21)
        Me.lblMAFValue.TabIndex = 16
        Me.lblMAFValue.Text = "0 g/s"
        '
        'lblShortTrim
        '
        Me.lblShortTrim.AutoSize = True
        Me.lblShortTrim.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblShortTrim.ForeColor = System.Drawing.Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(196, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.lblShortTrim.Location = New System.Drawing.Point(320, 105)
        Me.lblShortTrim.Name = "lblShortTrim"
        Me.lblShortTrim.Size = New System.Drawing.Size(62, 19)
        Me.lblShortTrim.TabIndex = 17
        Me.lblShortTrim.Text = "📈 STFT:"
        '
        'lblShortTrimValue
        '
        Me.lblShortTrimValue.AutoSize = True
        Me.lblShortTrimValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblShortTrimValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblShortTrimValue.Location = New System.Drawing.Point(460, 105)
        Me.lblShortTrimValue.Name = "lblShortTrimValue"
        Me.lblShortTrimValue.Size = New System.Drawing.Size(37, 21)
        Me.lblShortTrimValue.TabIndex = 18
        Me.lblShortTrimValue.Text = "0 %"
        '
        'lblLongTrim
        '
        Me.lblLongTrim.AutoSize = True
        Me.lblLongTrim.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblLongTrim.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(193, Byte), Integer), CType(CType(7, Byte), Integer))
        Me.lblLongTrim.Location = New System.Drawing.Point(320, 140)
        Me.lblLongTrim.Name = "lblLongTrim"
        Me.lblLongTrim.Size = New System.Drawing.Size(61, 19)
        Me.lblLongTrim.TabIndex = 19
        Me.lblLongTrim.Text = "📉 LTFT:"
        '
        'lblLongTrimValue
        '
        Me.lblLongTrimValue.AutoSize = True
        Me.lblLongTrimValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblLongTrimValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblLongTrimValue.Location = New System.Drawing.Point(460, 140)
        Me.lblLongTrimValue.Name = "lblLongTrimValue"
        Me.lblLongTrimValue.Size = New System.Drawing.Size(37, 21)
        Me.lblLongTrimValue.TabIndex = 20
        Me.lblLongTrimValue.Text = "0 %"
        '
        'lblBarometric
        '
        Me.lblBarometric.AutoSize = True
        Me.lblBarometric.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblBarometric.ForeColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.lblBarometric.Location = New System.Drawing.Point(320, 175)
        Me.lblBarometric.Name = "lblBarometric"
        Me.lblBarometric.Size = New System.Drawing.Size(90, 19)
        Me.lblBarometric.TabIndex = 21
        Me.lblBarometric.Text = "🌍 Atmosfer:"
        '
        'lblBarometricValue
        '
        Me.lblBarometricValue.AutoSize = True
        Me.lblBarometricValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblBarometricValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblBarometricValue.Location = New System.Drawing.Point(460, 175)
        Me.lblBarometricValue.Name = "lblBarometricValue"
        Me.lblBarometricValue.Size = New System.Drawing.Size(51, 21)
        Me.lblBarometricValue.TabIndex = 22
        Me.lblBarometricValue.Text = "0 kPa"
        '
        'lblVoltage
        '
        Me.lblVoltage.AutoSize = True
        Me.lblVoltage.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblVoltage.ForeColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.lblVoltage.Location = New System.Drawing.Point(320, 210)
        Me.lblVoltage.Name = "lblVoltage"
        Me.lblVoltage.Size = New System.Drawing.Size(93, 19)
        Me.lblVoltage.TabIndex = 23
        Me.lblVoltage.Text = "🔋 ECU Voltaj:"
        '
        'lblVoltageValue
        '
        Me.lblVoltageValue.AutoSize = True
        Me.lblVoltageValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblVoltageValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblVoltageValue.Location = New System.Drawing.Point(460, 210)
        Me.lblVoltageValue.Name = "lblVoltageValue"
        Me.lblVoltageValue.Size = New System.Drawing.Size(34, 21)
        Me.lblVoltageValue.TabIndex = 24
        Me.lblVoltageValue.Text = "0 V"
        '
        'lblFuelRate
        '
        Me.lblFuelRate.AutoSize = True
        Me.lblFuelRate.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblFuelRate.ForeColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(126, Byte), Integer), CType(CType(34, Byte), Integer))
        Me.lblFuelRate.Location = New System.Drawing.Point(320, 245)
        Me.lblFuelRate.Name = "lblFuelRate"
        Me.lblFuelRate.Size = New System.Drawing.Size(80, 19)
        Me.lblFuelRate.TabIndex = 25
        Me.lblFuelRate.Text = "⛽ Tüketim:"
        '
        'lblFuelRateValue
        '
        Me.lblFuelRateValue.AutoSize = True
        Me.lblFuelRateValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblFuelRateValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblFuelRateValue.Location = New System.Drawing.Point(460, 245)
        Me.lblFuelRateValue.Name = "lblFuelRateValue"
        Me.lblFuelRateValue.Size = New System.Drawing.Size(48, 21)
        Me.lblFuelRateValue.TabIndex = 26
        Me.lblFuelRateValue.Text = "0 L/h"
        '
        'splitContainerCharts
        '
        Me.splitContainerCharts.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.splitContainerCharts.Location = New System.Drawing.Point(20, 310)
        Me.splitContainerCharts.Name = "splitContainerCharts"
        Me.splitContainerCharts.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.splitContainerCharts.Panel1MinSize = 60
        '
        'splitContainerCharts.Panel1 (Grafik kontrolleri)
        '
        Me.splitContainerCharts.Panel1.Controls.Add(Me.grpChartControls)
        '
        'splitContainerCharts.Panel2 (Grafikler)
        '
        Me.splitContainerCharts.Panel2.Controls.Add(Me.liveChartTemp)
        Me.splitContainerCharts.Panel2.Controls.Add(Me.liveChartSpeed)
        Me.splitContainerCharts.Panel2.Controls.Add(Me.liveChartRPM)
        Me.splitContainerCharts.Size = New System.Drawing.Size(1184, 350)
        Me.splitContainerCharts.SplitterDistance = 80
        Me.splitContainerCharts.TabIndex = 2
        '
        'grpChartControls
        '
        Me.grpChartControls.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(35, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.grpChartControls.Controls.Add(Me.btnChartPause)
        Me.grpChartControls.Controls.Add(Me.btnChartClear)
        Me.grpChartControls.Controls.Add(Me.btnChartExport)
        Me.grpChartControls.Controls.Add(Me.cmbChartTimeRange)
        Me.grpChartControls.Controls.Add(Me.lblChartTimeRange)
        Me.grpChartControls.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grpChartControls.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpChartControls.ForeColor = System.Drawing.Color.White
        Me.grpChartControls.Location = New System.Drawing.Point(0, 0)
        Me.grpChartControls.Name = "grpChartControls"
        Me.grpChartControls.Size = New System.Drawing.Size(1184, 80)
        Me.grpChartControls.TabIndex = 0
        Me.grpChartControls.TabStop = False
        Me.grpChartControls.Text = "📈 Grafik Kontrolleri"
        '
        'btnChartPause
        '
        Me.btnChartPause.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(126, Byte), Integer), CType(CType(34, Byte), Integer))
        Me.btnChartPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnChartPause.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnChartPause.ForeColor = System.Drawing.Color.White
        Me.btnChartPause.Location = New System.Drawing.Point(20, 35)
        Me.btnChartPause.Name = "btnChartPause"
        Me.btnChartPause.Size = New System.Drawing.Size(120, 35)
        Me.btnChartPause.TabIndex = 0
        Me.btnChartPause.Text = "⏸️ Duraklat"
        Me.btnChartPause.UseVisualStyleBackColor = False
        '
        'btnChartClear
        '
        Me.btnChartClear.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnChartClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnChartClear.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnChartClear.ForeColor = System.Drawing.Color.White
        Me.btnChartClear.Location = New System.Drawing.Point(150, 35)
        Me.btnChartClear.Name = "btnChartClear"
        Me.btnChartClear.Size = New System.Drawing.Size(120, 35)
        Me.btnChartClear.TabIndex = 1
        Me.btnChartClear.Text = "🗑️ Temizle"
        Me.btnChartClear.UseVisualStyleBackColor = False
        '
        'btnChartExport
        '
        Me.btnChartExport.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnChartExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnChartExport.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnChartExport.ForeColor = System.Drawing.Color.White
        Me.btnChartExport.Location = New System.Drawing.Point(280, 35)
        Me.btnChartExport.Name = "btnChartExport"
        Me.btnChartExport.Size = New System.Drawing.Size(140, 35)
        Me.btnChartExport.TabIndex = 2
        Me.btnChartExport.Text = "💾 Dışa Aktar (CSV)"
        Me.btnChartExport.UseVisualStyleBackColor = False
        '
        'cmbChartTimeRange
        '
        Me.cmbChartTimeRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbChartTimeRange.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.cmbChartTimeRange.FormattingEnabled = True
        Me.cmbChartTimeRange.Items.AddRange(New Object() {"30 saniye", "60 saniye", "120 saniye", "300 saniye"})
        Me.cmbChartTimeRange.Location = New System.Drawing.Point(550, 39)
        Me.cmbChartTimeRange.Name = "cmbChartTimeRange"
        Me.cmbChartTimeRange.Size = New System.Drawing.Size(120, 23)
        Me.cmbChartTimeRange.TabIndex = 4
        '
        'lblChartTimeRange
        '
        Me.lblChartTimeRange.AutoSize = True
        Me.lblChartTimeRange.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblChartTimeRange.ForeColor = System.Drawing.Color.White
        Me.lblChartTimeRange.Location = New System.Drawing.Point(440, 42)
        Me.lblChartTimeRange.Name = "lblChartTimeRange"
        Me.lblChartTimeRange.Size = New System.Drawing.Size(84, 15)
        Me.lblChartTimeRange.TabIndex = 3
        Me.lblChartTimeRange.Text = "Zaman Aralığı:"
        '
        'tabDiagnostics
        '
        Me.tabDiagnostics.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.tabDiagnostics.Controls.Add(Me.btnHelpDiagnostics)
        Me.tabDiagnostics.Controls.Add(Me.grpVINProfile)
        Me.tabDiagnostics.Controls.Add(Me.grpPendingDTC)
        Me.tabDiagnostics.Controls.Add(Me.grpFreezeFrame)
        Me.tabDiagnostics.Controls.Add(Me.grpMode06)
        Me.tabDiagnostics.Controls.Add(Me.grpVehicleInfo)
        Me.tabDiagnostics.Controls.Add(Me.grpPIDAutoScan)
        Me.tabDiagnostics.Location = New System.Drawing.Point(4, 26)
        Me.tabDiagnostics.Name = "tabDiagnostics"
        Me.tabDiagnostics.Size = New System.Drawing.Size(1226, 681)
        Me.tabDiagnostics.TabIndex = 2
        Me.tabDiagnostics.Text = "🔧 Teşhis"
        '
        'btnHelpDiagnostics
        '
        Me.btnHelpDiagnostics.Location = New System.Drawing.Point(0, 0)
        Me.btnHelpDiagnostics.Name = "btnHelpDiagnostics"
        Me.btnHelpDiagnostics.Size = New System.Drawing.Size(75, 23)
        Me.btnHelpDiagnostics.TabIndex = 0
        '
        'grpVINProfile
        '
        Me.grpVINProfile.Controls.Add(Me.chkVINAutoProfile)
        Me.grpVINProfile.Controls.Add(Me.lblVINProfileStatus)
        Me.grpVINProfile.Controls.Add(Me.lblProfileBrand)
        Me.grpVINProfile.Controls.Add(Me.lblProfileBrandValue)
        Me.grpVINProfile.Controls.Add(Me.lblProfileModel)
        Me.grpVINProfile.Controls.Add(Me.lblProfileModelValue)
        Me.grpVINProfile.Controls.Add(Me.lblProfileYear)
        Me.grpVINProfile.Controls.Add(Me.lblProfileYearValue)
        Me.grpVINProfile.Controls.Add(Me.btnLoadVINProfile)
        Me.grpVINProfile.Controls.Add(Me.btnSaveVINProfile)
        Me.grpVINProfile.Controls.Add(Me.btnEnrichDatabase)
        Me.grpVINProfile.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpVINProfile.Location = New System.Drawing.Point(800, 230)
        Me.grpVINProfile.Name = "grpVINProfile"
        Me.grpVINProfile.Size = New System.Drawing.Size(370, 240)
        Me.grpVINProfile.TabIndex = 0
        Me.grpVINProfile.TabStop = False
        Me.grpVINProfile.Text = "🚗 VIN Otomatik Profil & Veritabanı"
        '
        'chkVINAutoProfile
        '
        Me.chkVINAutoProfile.AutoSize = True
        Me.chkVINAutoProfile.Checked = True
        Me.chkVINAutoProfile.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkVINAutoProfile.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.chkVINAutoProfile.Location = New System.Drawing.Point(15, 30)
        Me.chkVINAutoProfile.Name = "chkVINAutoProfile"
        Me.chkVINAutoProfile.Size = New System.Drawing.Size(139, 19)
        Me.chkVINAutoProfile.TabIndex = 0
        Me.chkVINAutoProfile.Text = "VIN Auto Profile Aktif"
        '
        'lblVINProfileStatus
        '
        Me.lblVINProfileStatus.AutoSize = True
        Me.lblVINProfileStatus.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblVINProfileStatus.ForeColor = System.Drawing.Color.Gray
        Me.lblVINProfileStatus.Location = New System.Drawing.Point(180, 31)
        Me.lblVINProfileStatus.Name = "lblVINProfileStatus"
        Me.lblVINProfileStatus.Size = New System.Drawing.Size(115, 15)
        Me.lblVINProfileStatus.TabIndex = 1
        Me.lblVINProfileStatus.Text = "⚪ Profil yüklenmedi"
        '
        'lblProfileBrand
        '
        Me.lblProfileBrand.AutoSize = True
        Me.lblProfileBrand.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblProfileBrand.Location = New System.Drawing.Point(15, 60)
        Me.lblProfileBrand.Name = "lblProfileBrand"
        Me.lblProfileBrand.Size = New System.Drawing.Size(43, 15)
        Me.lblProfileBrand.TabIndex = 2
        Me.lblProfileBrand.Text = "Marka:"
        '
        'lblProfileBrandValue
        '
        Me.lblProfileBrandValue.AutoSize = True
        Me.lblProfileBrandValue.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblProfileBrandValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.lblProfileBrandValue.Location = New System.Drawing.Point(100, 60)
        Me.lblProfileBrandValue.Name = "lblProfileBrandValue"
        Me.lblProfileBrandValue.Size = New System.Drawing.Size(22, 15)
        Me.lblProfileBrandValue.TabIndex = 3
        Me.lblProfileBrandValue.Text = "---"
        '
        'lblProfileModel
        '
        Me.lblProfileModel.AutoSize = True
        Me.lblProfileModel.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblProfileModel.Location = New System.Drawing.Point(15, 85)
        Me.lblProfileModel.Name = "lblProfileModel"
        Me.lblProfileModel.Size = New System.Drawing.Size(44, 15)
        Me.lblProfileModel.TabIndex = 4
        Me.lblProfileModel.Text = "Model:"
        '
        'lblProfileModelValue
        '
        Me.lblProfileModelValue.AutoSize = True
        Me.lblProfileModelValue.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblProfileModelValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.lblProfileModelValue.Location = New System.Drawing.Point(100, 85)
        Me.lblProfileModelValue.Name = "lblProfileModelValue"
        Me.lblProfileModelValue.Size = New System.Drawing.Size(22, 15)
        Me.lblProfileModelValue.TabIndex = 5
        Me.lblProfileModelValue.Text = "---"
        '
        'lblProfileYear
        '
        Me.lblProfileYear.AutoSize = True
        Me.lblProfileYear.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblProfileYear.Location = New System.Drawing.Point(15, 110)
        Me.lblProfileYear.Name = "lblProfileYear"
        Me.lblProfileYear.Size = New System.Drawing.Size(23, 15)
        Me.lblProfileYear.TabIndex = 6
        Me.lblProfileYear.Text = "Yıl:"
        '
        'lblProfileYearValue
        '
        Me.lblProfileYearValue.AutoSize = True
        Me.lblProfileYearValue.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblProfileYearValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.lblProfileYearValue.Location = New System.Drawing.Point(100, 110)
        Me.lblProfileYearValue.Name = "lblProfileYearValue"
        Me.lblProfileYearValue.Size = New System.Drawing.Size(22, 15)
        Me.lblProfileYearValue.TabIndex = 7
        Me.lblProfileYearValue.Text = "---"
        '
        'btnLoadVINProfile
        '
        Me.btnLoadVINProfile.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnLoadVINProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnLoadVINProfile.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnLoadVINProfile.ForeColor = System.Drawing.Color.White
        Me.btnLoadVINProfile.Location = New System.Drawing.Point(15, 150)
        Me.btnLoadVINProfile.Name = "btnLoadVINProfile"
        Me.btnLoadVINProfile.Size = New System.Drawing.Size(160, 35)
        Me.btnLoadVINProfile.TabIndex = 8
        Me.btnLoadVINProfile.Text = "📂 Profil Yükle"
        Me.btnLoadVINProfile.UseVisualStyleBackColor = False
        '
        'btnSaveVINProfile
        '
        Me.btnSaveVINProfile.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnSaveVINProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSaveVINProfile.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnSaveVINProfile.ForeColor = System.Drawing.Color.White
        Me.btnSaveVINProfile.Location = New System.Drawing.Point(190, 150)
        Me.btnSaveVINProfile.Name = "btnSaveVINProfile"
        Me.btnSaveVINProfile.Size = New System.Drawing.Size(160, 35)
        Me.btnSaveVINProfile.TabIndex = 9
        Me.btnSaveVINProfile.Text = "💾 Profil Kaydet"
        Me.btnSaveVINProfile.UseVisualStyleBackColor = False
        '
        'btnEnrichDatabase
        '
        Me.btnEnrichDatabase.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnEnrichDatabase.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnEnrichDatabase.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnEnrichDatabase.ForeColor = System.Drawing.Color.White
        Me.btnEnrichDatabase.Location = New System.Drawing.Point(15, 195)
        Me.btnEnrichDatabase.Name = "btnEnrichDatabase"
        Me.btnEnrichDatabase.Size = New System.Drawing.Size(335, 35)
        Me.btnEnrichDatabase.TabIndex = 10
        Me.btnEnrichDatabase.Text = "📚 Veritabanını Zenginleştir (Online)"
        Me.btnEnrichDatabase.UseVisualStyleBackColor = False
        '
        'grpPendingDTC
        '
        Me.grpPendingDTC.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.grpPendingDTC.Controls.Add(Me.lblDTCSearch)
        Me.grpPendingDTC.Controls.Add(Me.txtDTCSearch)
        Me.grpPendingDTC.Controls.Add(Me.lblDTCCategory)
        Me.grpPendingDTC.Controls.Add(Me.cmbDTCCategory)
        Me.grpPendingDTC.Controls.Add(Me.lblDTCCount)
        Me.grpPendingDTC.Controls.Add(Me.dgvDTCList)
        Me.grpPendingDTC.Controls.Add(Me.btnReadPendingDTC)
        Me.grpPendingDTC.Controls.Add(Me.btnReadStoredDTC)
        Me.grpPendingDTC.Controls.Add(Me.btnClearDTC)
        Me.grpPendingDTC.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpPendingDTC.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.grpPendingDTC.Location = New System.Drawing.Point(20, 20)
        Me.grpPendingDTC.Name = "grpPendingDTC"
        Me.grpPendingDTC.Size = New System.Drawing.Size(760, 420)
        Me.grpPendingDTC.TabIndex = 1
        Me.grpPendingDTC.TabStop = False
        Me.grpPendingDTC.Text = "🔧 Arıza Kodları (DTC)"
        '
        'lblDTCSearch
        '
        Me.lblDTCSearch.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblDTCSearch.ForeColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(80, Byte), Integer))
        Me.lblDTCSearch.Location = New System.Drawing.Point(15, 28)
        Me.lblDTCSearch.Name = "lblDTCSearch"
        Me.lblDTCSearch.Size = New System.Drawing.Size(50, 20)
        Me.lblDTCSearch.TabIndex = 0
        Me.lblDTCSearch.Text = "🔍 Ara:"
        '
        'txtDTCSearch
        '
        Me.txtDTCSearch.BackColor = System.Drawing.Color.White
        Me.txtDTCSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtDTCSearch.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtDTCSearch.Location = New System.Drawing.Point(70, 25)
        Me.txtDTCSearch.Name = "txtDTCSearch"
        Me.txtDTCSearch.Size = New System.Drawing.Size(200, 25)
        Me.txtDTCSearch.TabIndex = 1
        '
        'lblDTCCategory
        '
        Me.lblDTCCategory.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblDTCCategory.ForeColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(80, Byte), Integer))
        Me.lblDTCCategory.Location = New System.Drawing.Point(290, 28)
        Me.lblDTCCategory.Name = "lblDTCCategory"
        Me.lblDTCCategory.Size = New System.Drawing.Size(60, 20)
        Me.lblDTCCategory.TabIndex = 2
        Me.lblDTCCategory.Text = "Kategori:"
        '
        'cmbDTCCategory
        '
        Me.cmbDTCCategory.BackColor = System.Drawing.Color.White
        Me.cmbDTCCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbDTCCategory.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.cmbDTCCategory.Items.AddRange(New Object() {"Tümü", "Powertrain (P)", "Chassis (C)", "Body (B)", "Network (U)"})
        Me.cmbDTCCategory.Location = New System.Drawing.Point(355, 24)
        Me.cmbDTCCategory.Name = "cmbDTCCategory"
        Me.cmbDTCCategory.Size = New System.Drawing.Size(160, 23)
        Me.cmbDTCCategory.TabIndex = 3
        '
        'lblDTCCount
        '
        Me.lblDTCCount.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblDTCCount.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(120, Byte), Integer))
        Me.lblDTCCount.Location = New System.Drawing.Point(530, 28)
        Me.lblDTCCount.Name = "lblDTCCount"
        Me.lblDTCCount.Size = New System.Drawing.Size(200, 20)
        Me.lblDTCCount.TabIndex = 4
        Me.lblDTCCount.Text = "0 arıza kodu"
        '
        'dgvDTCList
        '
        Me.dgvDTCList.AllowUserToAddRows = False
        Me.dgvDTCList.AllowUserToDeleteRows = False
        Me.dgvDTCList.AllowUserToResizeRows = False
        DataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(55, Byte), Integer))
        Me.dgvDTCList.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        Me.dgvDTCList.BackgroundColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.dgvDTCList.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.dgvDTCList.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal
        Me.dgvDTCList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None
        DataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(65, Byte), Integer))
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        DataGridViewCellStyle2.ForeColor = System.Drawing.Color.White
        DataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(65, Byte), Integer))
        Me.dgvDTCList.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle2
        Me.dgvDTCList.ColumnHeadersHeight = 35
        DataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(35, Byte), Integer), CType(CType(50, Byte), Integer))
        DataGridViewCellStyle3.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        DataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(60, Byte), Integer))
        DataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        DataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White
        DataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.dgvDTCList.DefaultCellStyle = DataGridViewCellStyle3
        Me.dgvDTCList.EnableHeadersVisualStyles = False
        Me.dgvDTCList.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.dgvDTCList.GridColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(70, Byte), Integer))
        Me.dgvDTCList.Location = New System.Drawing.Point(15, 55)
        Me.dgvDTCList.MultiSelect = False
        Me.dgvDTCList.Name = "dgvDTCList"
        Me.dgvDTCList.ReadOnly = True
        Me.dgvDTCList.RowHeadersVisible = False
        Me.dgvDTCList.RowTemplate.Height = 28
        Me.dgvDTCList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvDTCList.Size = New System.Drawing.Size(730, 310)
        Me.dgvDTCList.TabIndex = 5
        '
        'btnReadPendingDTC
        '
        Me.btnReadPendingDTC.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(126, Byte), Integer), CType(CType(34, Byte), Integer))
        Me.btnReadPendingDTC.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnReadPendingDTC.FlatAppearance.BorderSize = 0
        Me.btnReadPendingDTC.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReadPendingDTC.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnReadPendingDTC.ForeColor = System.Drawing.Color.White
        Me.btnReadPendingDTC.Location = New System.Drawing.Point(15, 375)
        Me.btnReadPendingDTC.Name = "btnReadPendingDTC"
        Me.btnReadPendingDTC.Size = New System.Drawing.Size(140, 35)
        Me.btnReadPendingDTC.TabIndex = 6
        Me.btnReadPendingDTC.Text = "📋 Bekleyen (07)"
        Me.btnReadPendingDTC.UseVisualStyleBackColor = False
        '
        'btnReadStoredDTC
        '
        Me.btnReadStoredDTC.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnReadStoredDTC.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnReadStoredDTC.FlatAppearance.BorderSize = 0
        Me.btnReadStoredDTC.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReadStoredDTC.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnReadStoredDTC.ForeColor = System.Drawing.Color.White
        Me.btnReadStoredDTC.Location = New System.Drawing.Point(165, 375)
        Me.btnReadStoredDTC.Name = "btnReadStoredDTC"
        Me.btnReadStoredDTC.Size = New System.Drawing.Size(140, 35)
        Me.btnReadStoredDTC.TabIndex = 7
        Me.btnReadStoredDTC.Text = "💾 Kayıtlı (03)"
        Me.btnReadStoredDTC.UseVisualStyleBackColor = False
        '
        'btnClearDTC
        '
        Me.btnClearDTC.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnClearDTC.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnClearDTC.FlatAppearance.BorderSize = 0
        Me.btnClearDTC.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClearDTC.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnClearDTC.ForeColor = System.Drawing.Color.White
        Me.btnClearDTC.Location = New System.Drawing.Point(315, 375)
        Me.btnClearDTC.Name = "btnClearDTC"
        Me.btnClearDTC.Size = New System.Drawing.Size(140, 35)
        Me.btnClearDTC.TabIndex = 8
        Me.btnClearDTC.Text = "🗑️ DTC Sil (04)"
        Me.btnClearDTC.UseVisualStyleBackColor = False
        '
        'grpFreezeFrame
        '
        Me.grpFreezeFrame.Controls.Add(Me.lstFreezeFrame)
        Me.grpFreezeFrame.Controls.Add(Me.btnReadFreezeFrame)
        Me.grpFreezeFrame.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpFreezeFrame.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.grpFreezeFrame.Location = New System.Drawing.Point(20, 450)
        Me.grpFreezeFrame.Name = "grpFreezeFrame"
        Me.grpFreezeFrame.Size = New System.Drawing.Size(370, 200)
        Me.grpFreezeFrame.TabIndex = 2
        Me.grpFreezeFrame.TabStop = False
        Me.grpFreezeFrame.Text = "❄️ Freeze Frame (Mode 02)"
        '
        'lstFreezeFrame
        '
        Me.lstFreezeFrame.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lstFreezeFrame.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lstFreezeFrame.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.lstFreezeFrame.ItemHeight = 15
        Me.lstFreezeFrame.Location = New System.Drawing.Point(15, 30)
        Me.lstFreezeFrame.Name = "lstFreezeFrame"
        Me.lstFreezeFrame.Size = New System.Drawing.Size(340, 109)
        Me.lstFreezeFrame.TabIndex = 0
        '
        'btnReadFreezeFrame
        '
        Me.btnReadFreezeFrame.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnReadFreezeFrame.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReadFreezeFrame.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnReadFreezeFrame.ForeColor = System.Drawing.Color.White
        Me.btnReadFreezeFrame.Location = New System.Drawing.Point(15, 158)
        Me.btnReadFreezeFrame.Name = "btnReadFreezeFrame"
        Me.btnReadFreezeFrame.Size = New System.Drawing.Size(160, 32)
        Me.btnReadFreezeFrame.TabIndex = 1
        Me.btnReadFreezeFrame.Text = "❄️ Freeze Oku"
        Me.btnReadFreezeFrame.UseVisualStyleBackColor = False
        '
        'grpMode06
        '
        Me.grpMode06.Controls.Add(Me.lstMode06)
        Me.grpMode06.Controls.Add(Me.btnReadMode06)
        Me.grpMode06.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpMode06.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.grpMode06.Location = New System.Drawing.Point(410, 450)
        Me.grpMode06.Name = "grpMode06"
        Me.grpMode06.Size = New System.Drawing.Size(370, 200)
        Me.grpMode06.TabIndex = 3
        Me.grpMode06.TabStop = False
        Me.grpMode06.Text = "📊 On-Board Monitoring (Mode 06)"
        '
        'lstMode06
        '
        Me.lstMode06.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lstMode06.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lstMode06.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lstMode06.ItemHeight = 15
        Me.lstMode06.Location = New System.Drawing.Point(15, 30)
        Me.lstMode06.Name = "lstMode06"
        Me.lstMode06.Size = New System.Drawing.Size(340, 109)
        Me.lstMode06.TabIndex = 0
        '
        'btnReadMode06
        '
        Me.btnReadMode06.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnReadMode06.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReadMode06.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnReadMode06.ForeColor = System.Drawing.Color.White
        Me.btnReadMode06.Location = New System.Drawing.Point(15, 158)
        Me.btnReadMode06.Name = "btnReadMode06"
        Me.btnReadMode06.Size = New System.Drawing.Size(160, 32)
        Me.btnReadMode06.TabIndex = 1
        Me.btnReadMode06.Text = "📊 Mode 06 Oku"
        Me.btnReadMode06.UseVisualStyleBackColor = False
        '
        'grpVehicleInfo
        '
        Me.grpVehicleInfo.Controls.Add(Me.lblVIN)
        Me.grpVehicleInfo.Controls.Add(Me.lblVINValue)
        Me.grpVehicleInfo.Controls.Add(Me.lblCalibrationID)
        Me.grpVehicleInfo.Controls.Add(Me.lblCalibrationIDValue)
        Me.grpVehicleInfo.Controls.Add(Me.lblCVN)
        Me.grpVehicleInfo.Controls.Add(Me.lblCVNValue)
        Me.grpVehicleInfo.Controls.Add(Me.lblECUName)
        Me.grpVehicleInfo.Controls.Add(Me.lblECUNameValue)
        Me.grpVehicleInfo.Controls.Add(Me.btnReadVehicleInfo)
        Me.grpVehicleInfo.Controls.Add(Me.btnIsoTpTest)
        Me.grpVehicleInfo.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpVehicleInfo.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.grpVehicleInfo.Location = New System.Drawing.Point(800, 20)
        Me.grpVehicleInfo.Name = "grpVehicleInfo"
        Me.grpVehicleInfo.Size = New System.Drawing.Size(370, 200)
        Me.grpVehicleInfo.TabIndex = 4
        Me.grpVehicleInfo.TabStop = False
        Me.grpVehicleInfo.Text = "🚗 Araç Bilgileri (Mode 09)"
        '
        'lblVIN
        '
        Me.lblVIN.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblVIN.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(120, Byte), Integer))
        Me.lblVIN.Location = New System.Drawing.Point(15, 35)
        Me.lblVIN.Name = "lblVIN"
        Me.lblVIN.Size = New System.Drawing.Size(100, 20)
        Me.lblVIN.TabIndex = 0
        Me.lblVIN.Text = "VIN:"
        '
        'lblVINValue
        '
        Me.lblVINValue.Font = New System.Drawing.Font("Consolas", 10.0!, System.Drawing.FontStyle.Bold)
        Me.lblVINValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(100, Byte), Integer))
        Me.lblVINValue.Location = New System.Drawing.Point(120, 35)
        Me.lblVINValue.Name = "lblVINValue"
        Me.lblVINValue.Size = New System.Drawing.Size(235, 20)
        Me.lblVINValue.TabIndex = 1
        Me.lblVINValue.Text = "---"
        '
        'lblCalibrationID
        '
        Me.lblCalibrationID.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblCalibrationID.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(120, Byte), Integer))
        Me.lblCalibrationID.Location = New System.Drawing.Point(15, 60)
        Me.lblCalibrationID.Name = "lblCalibrationID"
        Me.lblCalibrationID.Size = New System.Drawing.Size(100, 20)
        Me.lblCalibrationID.TabIndex = 2
        Me.lblCalibrationID.Text = "Calibration:"
        '
        'lblCalibrationIDValue
        '
        Me.lblCalibrationIDValue.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lblCalibrationIDValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(150, Byte), Integer), CType(CType(150, Byte), Integer), CType(CType(170, Byte), Integer))
        Me.lblCalibrationIDValue.Location = New System.Drawing.Point(120, 60)
        Me.lblCalibrationIDValue.Name = "lblCalibrationIDValue"
        Me.lblCalibrationIDValue.Size = New System.Drawing.Size(235, 20)
        Me.lblCalibrationIDValue.TabIndex = 3
        Me.lblCalibrationIDValue.Text = "---"
        '
        'lblCVN
        '
        Me.lblCVN.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblCVN.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(120, Byte), Integer))
        Me.lblCVN.Location = New System.Drawing.Point(15, 85)
        Me.lblCVN.Name = "lblCVN"
        Me.lblCVN.Size = New System.Drawing.Size(100, 20)
        Me.lblCVN.TabIndex = 4
        Me.lblCVN.Text = "CVN:"
        '
        'lblCVNValue
        '
        Me.lblCVNValue.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lblCVNValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(150, Byte), Integer), CType(CType(150, Byte), Integer), CType(CType(170, Byte), Integer))
        Me.lblCVNValue.Location = New System.Drawing.Point(120, 85)
        Me.lblCVNValue.Name = "lblCVNValue"
        Me.lblCVNValue.Size = New System.Drawing.Size(235, 20)
        Me.lblCVNValue.TabIndex = 5
        Me.lblCVNValue.Text = "---"
        '
        'lblECUName
        '
        Me.lblECUName.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblECUName.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(120, Byte), Integer))
        Me.lblECUName.Location = New System.Drawing.Point(15, 110)
        Me.lblECUName.Name = "lblECUName"
        Me.lblECUName.Size = New System.Drawing.Size(100, 20)
        Me.lblECUName.TabIndex = 6
        Me.lblECUName.Text = "ECU Name:"
        '
        'lblECUNameValue
        '
        Me.lblECUNameValue.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lblECUNameValue.ForeColor = System.Drawing.Color.FromArgb(CType(CType(150, Byte), Integer), CType(CType(150, Byte), Integer), CType(CType(170, Byte), Integer))
        Me.lblECUNameValue.Location = New System.Drawing.Point(120, 110)
        Me.lblECUNameValue.Name = "lblECUNameValue"
        Me.lblECUNameValue.Size = New System.Drawing.Size(235, 20)
        Me.lblECUNameValue.TabIndex = 7
        Me.lblECUNameValue.Text = "---"
        '
        'btnReadVehicleInfo
        '
        Me.btnReadVehicleInfo.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnReadVehicleInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReadVehicleInfo.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnReadVehicleInfo.ForeColor = System.Drawing.Color.White
        Me.btnReadVehicleInfo.Location = New System.Drawing.Point(15, 158)
        Me.btnReadVehicleInfo.Name = "btnReadVehicleInfo"
        Me.btnReadVehicleInfo.Size = New System.Drawing.Size(160, 32)
        Me.btnReadVehicleInfo.TabIndex = 8
        Me.btnReadVehicleInfo.Text = "🚗 Bilgileri Oku"
        Me.btnReadVehicleInfo.UseVisualStyleBackColor = False
        '
        'btnIsoTpTest
        '
        Me.btnIsoTpTest.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnIsoTpTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnIsoTpTest.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnIsoTpTest.ForeColor = System.Drawing.Color.White
        Me.btnIsoTpTest.Location = New System.Drawing.Point(185, 158)
        Me.btnIsoTpTest.Name = "btnIsoTpTest"
        Me.btnIsoTpTest.Size = New System.Drawing.Size(160, 32)
        Me.btnIsoTpTest.TabIndex = 9
        Me.btnIsoTpTest.Text = "🔧 ISO-TP Test"
        Me.btnIsoTpTest.UseVisualStyleBackColor = False
        '
        'grpPIDAutoScan
        '
        Me.grpPIDAutoScan.Controls.Add(Me.lstSupportedPIDs)
        Me.grpPIDAutoScan.Controls.Add(Me.btnAutoScanPIDs)
        Me.grpPIDAutoScan.Controls.Add(Me.lblScanStatus)
        Me.grpPIDAutoScan.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpPIDAutoScan.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.grpPIDAutoScan.Location = New System.Drawing.Point(800, 480)
        Me.grpPIDAutoScan.Name = "grpPIDAutoScan"
        Me.grpPIDAutoScan.Size = New System.Drawing.Size(370, 170)
        Me.grpPIDAutoScan.TabIndex = 5
        Me.grpPIDAutoScan.TabStop = False
        Me.grpPIDAutoScan.Text = "🔍 PID Auto-Scan"
        '
        'lstSupportedPIDs
        '
        Me.lstSupportedPIDs.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lstSupportedPIDs.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.lstSupportedPIDs.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer))
        Me.lstSupportedPIDs.ItemHeight = 14
        Me.lstSupportedPIDs.Location = New System.Drawing.Point(15, 30)
        Me.lstSupportedPIDs.Name = "lstSupportedPIDs"
        Me.lstSupportedPIDs.Size = New System.Drawing.Size(330, 326)
        Me.lstSupportedPIDs.TabIndex = 0
        '
        'btnAutoScanPIDs
        '
        Me.btnAutoScanPIDs.BackColor = System.Drawing.Color.FromArgb(CType(CType(26, Byte), Integer), CType(CType(188, Byte), Integer), CType(CType(156, Byte), Integer))
        Me.btnAutoScanPIDs.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAutoScanPIDs.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnAutoScanPIDs.ForeColor = System.Drawing.Color.White
        Me.btnAutoScanPIDs.Location = New System.Drawing.Point(15, 370)
        Me.btnAutoScanPIDs.Name = "btnAutoScanPIDs"
        Me.btnAutoScanPIDs.Size = New System.Drawing.Size(160, 32)
        Me.btnAutoScanPIDs.TabIndex = 1
        Me.btnAutoScanPIDs.Text = "🔍 PID Tara"
        Me.btnAutoScanPIDs.UseVisualStyleBackColor = False
        '
        'lblScanStatus
        '
        Me.lblScanStatus.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblScanStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(120, Byte), Integer))
        Me.lblScanStatus.Location = New System.Drawing.Point(185, 378)
        Me.lblScanStatus.Name = "lblScanStatus"
        Me.lblScanStatus.Size = New System.Drawing.Size(160, 20)
        Me.lblScanStatus.TabIndex = 2
        Me.lblScanStatus.Text = "Taramaya hazır..."
        '
        'tabCommands
        '
        Me.tabCommands.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.tabCommands.Controls.Add(Me.grpCommandSender)
        Me.tabCommands.Location = New System.Drawing.Point(4, 26)
        Me.tabCommands.Name = "tabCommands"
        Me.tabCommands.Size = New System.Drawing.Size(1226, 681)
        Me.tabCommands.TabIndex = 3
        Me.tabCommands.Text = "📋 Komutlar"
        '
        'grpCommandSender
        '
        Me.grpCommandSender.Controls.Add(Me.lblBrand)
        Me.grpCommandSender.Controls.Add(Me.cmbBrand)
        Me.grpCommandSender.Controls.Add(Me.lblModel)
        Me.grpCommandSender.Controls.Add(Me.cmbModel)
        Me.grpCommandSender.Controls.Add(Me.lblModule)
        Me.grpCommandSender.Controls.Add(Me.cmbModule)
        Me.grpCommandSender.Controls.Add(Me.lblCommand)
        Me.grpCommandSender.Controls.Add(Me.cmbCommand)
        Me.grpCommandSender.Controls.Add(Me.txtCommandPreview)
        Me.grpCommandSender.Controls.Add(Me.btnSendCommand)
        Me.grpCommandSender.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.grpCommandSender.Location = New System.Drawing.Point(20, 20)
        Me.grpCommandSender.Name = "grpCommandSender"
        Me.grpCommandSender.Size = New System.Drawing.Size(400, 350)
        Me.grpCommandSender.TabIndex = 0
        Me.grpCommandSender.TabStop = False
        Me.grpCommandSender.Text = "🚀 Komut Gönderici"
        '
        'lblBrand
        '
        Me.lblBrand.AutoSize = True
        Me.lblBrand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblBrand.Location = New System.Drawing.Point(20, 40)
        Me.lblBrand.Name = "lblBrand"
        Me.lblBrand.Size = New System.Drawing.Size(51, 19)
        Me.lblBrand.TabIndex = 0
        Me.lblBrand.Text = "Marka:"
        '
        'cmbBrand
        '
        Me.cmbBrand.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbBrand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbBrand.Location = New System.Drawing.Point(120, 37)
        Me.cmbBrand.Name = "cmbBrand"
        Me.cmbBrand.Size = New System.Drawing.Size(250, 25)
        Me.cmbBrand.TabIndex = 1
        '
        'lblModel
        '
        Me.lblModel.AutoSize = True
        Me.lblModel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblModel.Location = New System.Drawing.Point(20, 80)
        Me.lblModel.Name = "lblModel"
        Me.lblModel.Size = New System.Drawing.Size(51, 19)
        Me.lblModel.TabIndex = 2
        Me.lblModel.Text = "Model:"
        '
        'cmbModel
        '
        Me.cmbModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbModel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbModel.Location = New System.Drawing.Point(120, 77)
        Me.cmbModel.Name = "cmbModel"
        Me.cmbModel.Size = New System.Drawing.Size(250, 25)
        Me.cmbModel.TabIndex = 3
        '
        'lblModule
        '
        Me.lblModule.AutoSize = True
        Me.lblModule.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblModule.Location = New System.Drawing.Point(20, 120)
        Me.lblModule.Name = "lblModule"
        Me.lblModule.Size = New System.Drawing.Size(52, 19)
        Me.lblModule.TabIndex = 4
        Me.lblModule.Text = "Modül:"
        '
        'cmbModule
        '
        Me.cmbModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbModule.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbModule.Location = New System.Drawing.Point(120, 117)
        Me.cmbModule.Name = "cmbModule"
        Me.cmbModule.Size = New System.Drawing.Size(250, 25)
        Me.cmbModule.TabIndex = 5
        '
        'lblCommand
        '
        Me.lblCommand.AutoSize = True
        Me.lblCommand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCommand.Location = New System.Drawing.Point(20, 160)
        Me.lblCommand.Name = "lblCommand"
        Me.lblCommand.Size = New System.Drawing.Size(53, 19)
        Me.lblCommand.TabIndex = 6
        Me.lblCommand.Text = "Komut:"
        '
        'cmbCommand
        '
        Me.cmbCommand.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbCommand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbCommand.Location = New System.Drawing.Point(120, 157)
        Me.cmbCommand.Name = "cmbCommand"
        Me.cmbCommand.Size = New System.Drawing.Size(250, 25)
        Me.cmbCommand.TabIndex = 7
        '
        'txtCommandPreview
        '
        Me.txtCommandPreview.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.txtCommandPreview.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtCommandPreview.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.txtCommandPreview.Location = New System.Drawing.Point(20, 210)
        Me.txtCommandPreview.Multiline = True
        Me.txtCommandPreview.Name = "txtCommandPreview"
        Me.txtCommandPreview.ReadOnly = True
        Me.txtCommandPreview.Size = New System.Drawing.Size(350, 60)
        Me.txtCommandPreview.TabIndex = 8
        '
        'btnSendCommand
        '
        Me.btnSendCommand.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnSendCommand.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSendCommand.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnSendCommand.ForeColor = System.Drawing.Color.White
        Me.btnSendCommand.Location = New System.Drawing.Point(20, 285)
        Me.btnSendCommand.Name = "btnSendCommand"
        Me.btnSendCommand.Size = New System.Drawing.Size(350, 45)
        Me.btnSendCommand.TabIndex = 9
        Me.btnSendCommand.Text = "📨 Komutu Gönder"
        Me.btnSendCommand.UseVisualStyleBackColor = False
        '
        'tabCoding
        '
        Me.tabCoding.BackColor = System.Drawing.Color.FromArgb(CType(CType(20, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.tabCoding.Controls.Add(Me.grpCoding)
        Me.tabCoding.Location = New System.Drawing.Point(4, 26)
        Me.tabCoding.Name = "tabCoding"
        Me.tabCoding.Size = New System.Drawing.Size(1226, 681)
        Me.tabCoding.TabIndex = 4
        Me.tabCoding.Text = "⚙️ Kodlama"
        '
        'grpCoding
        '
        Me.grpCoding.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(35, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.grpCoding.Controls.Add(Me.lblCodeBrand)
        Me.grpCoding.Controls.Add(Me.cmbCodeBrand)
        Me.grpCoding.Controls.Add(Me.lblCodeModel)
        Me.grpCoding.Controls.Add(Me.cmbCodeModel)
        Me.grpCoding.Controls.Add(Me.lblCodeModule)
        Me.grpCoding.Controls.Add(Me.cmbCodeModule)
        Me.grpCoding.Controls.Add(Me.lstCodeCommands)
        Me.grpCoding.Controls.Add(Me.lblCodeID)
        Me.grpCoding.Controls.Add(Me.txtCodeID)
        Me.grpCoding.Controls.Add(Me.lblCodeType)
        Me.grpCoding.Controls.Add(Me.cmbCodeType)
        Me.grpCoding.Controls.Add(Me.lblCodeByteIndex)
        Me.grpCoding.Controls.Add(Me.txtCodeByteIndex)
        Me.grpCoding.Controls.Add(Me.lblCodeData)
        Me.grpCoding.Controls.Add(Me.txtCodeData)
        Me.grpCoding.Controls.Add(Me.txtCodePreview)
        Me.grpCoding.Controls.Add(Me.btnCodingOn)
        Me.grpCoding.Controls.Add(Me.btnCodingOff)
        Me.grpCoding.Controls.Add(Me.btnCodeSend)
        Me.grpCoding.Controls.Add(Me.btnCodeSaveFromLog)
        Me.grpCoding.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.grpCoding.ForeColor = System.Drawing.Color.White
        Me.grpCoding.Location = New System.Drawing.Point(20, 20)
        Me.grpCoding.Name = "grpCoding"
        Me.grpCoding.Size = New System.Drawing.Size(800, 550)
        Me.grpCoding.TabIndex = 0
        Me.grpCoding.TabStop = False
        Me.grpCoding.Text = "🔧 Gizli Özellik Kodlama"
        '
        'lblCodeBrand
        '
        Me.lblCodeBrand.AutoSize = True
        Me.lblCodeBrand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeBrand.ForeColor = System.Drawing.Color.White
        Me.lblCodeBrand.Location = New System.Drawing.Point(20, 40)
        Me.lblCodeBrand.Name = "lblCodeBrand"
        Me.lblCodeBrand.Size = New System.Drawing.Size(51, 19)
        Me.lblCodeBrand.TabIndex = 0
        Me.lblCodeBrand.Text = "Marka:"
        '
        'cmbCodeBrand
        '
        Me.cmbCodeBrand.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbCodeBrand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbCodeBrand.Location = New System.Drawing.Point(100, 37)
        Me.cmbCodeBrand.Name = "cmbCodeBrand"
        Me.cmbCodeBrand.Size = New System.Drawing.Size(180, 25)
        Me.cmbCodeBrand.TabIndex = 1
        '
        'lblCodeModel
        '
        Me.lblCodeModel.AutoSize = True
        Me.lblCodeModel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeModel.ForeColor = System.Drawing.Color.White
        Me.lblCodeModel.Location = New System.Drawing.Point(300, 40)
        Me.lblCodeModel.Name = "lblCodeModel"
        Me.lblCodeModel.Size = New System.Drawing.Size(51, 19)
        Me.lblCodeModel.TabIndex = 2
        Me.lblCodeModel.Text = "Model:"
        '
        'cmbCodeModel
        '
        Me.cmbCodeModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbCodeModel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbCodeModel.Location = New System.Drawing.Point(360, 37)
        Me.cmbCodeModel.Name = "cmbCodeModel"
        Me.cmbCodeModel.Size = New System.Drawing.Size(180, 25)
        Me.cmbCodeModel.TabIndex = 3
        '
        'lblCodeModule
        '
        Me.lblCodeModule.AutoSize = True
        Me.lblCodeModule.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeModule.ForeColor = System.Drawing.Color.White
        Me.lblCodeModule.Location = New System.Drawing.Point(560, 40)
        Me.lblCodeModule.Name = "lblCodeModule"
        Me.lblCodeModule.Size = New System.Drawing.Size(52, 19)
        Me.lblCodeModule.TabIndex = 4
        Me.lblCodeModule.Text = "Modül:"
        '
        'cmbCodeModule
        '
        Me.cmbCodeModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbCodeModule.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbCodeModule.Location = New System.Drawing.Point(620, 37)
        Me.cmbCodeModule.Name = "cmbCodeModule"
        Me.cmbCodeModule.Size = New System.Drawing.Size(160, 25)
        Me.cmbCodeModule.TabIndex = 5
        '
        'lstCodeCommands
        '
        Me.lstCodeCommands.BackColor = System.Drawing.Color.FromArgb(CType(CType(25, Byte), Integer), CType(CType(25, Byte), Integer), CType(CType(35, Byte), Integer))
        Me.lstCodeCommands.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lstCodeCommands.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lstCodeCommands.ItemHeight = 15
        Me.lstCodeCommands.Location = New System.Drawing.Point(20, 80)
        Me.lstCodeCommands.Name = "lstCodeCommands"
        Me.lstCodeCommands.Size = New System.Drawing.Size(350, 199)
        Me.lstCodeCommands.TabIndex = 6
        '
        'lblCodeID
        '
        Me.lblCodeID.AutoSize = True
        Me.lblCodeID.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeID.ForeColor = System.Drawing.Color.White
        Me.lblCodeID.Location = New System.Drawing.Point(400, 85)
        Me.lblCodeID.Name = "lblCodeID"
        Me.lblCodeID.Size = New System.Drawing.Size(26, 19)
        Me.lblCodeID.TabIndex = 7
        Me.lblCodeID.Text = "ID:"
        '
        'txtCodeID
        '
        Me.txtCodeID.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtCodeID.Font = New System.Drawing.Font("Consolas", 11.0!)
        Me.txtCodeID.Location = New System.Drawing.Point(480, 82)
        Me.txtCodeID.MaxLength = 3
        Me.txtCodeID.Name = "txtCodeID"
        Me.txtCodeID.Size = New System.Drawing.Size(80, 25)
        Me.txtCodeID.TabIndex = 8
        '
        'lblCodeType
        '
        Me.lblCodeType.AutoSize = True
        Me.lblCodeType.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeType.ForeColor = System.Drawing.Color.White
        Me.lblCodeType.Location = New System.Drawing.Point(580, 85)
        Me.lblCodeType.Name = "lblCodeType"
        Me.lblCodeType.Size = New System.Drawing.Size(30, 19)
        Me.lblCodeType.TabIndex = 9
        Me.lblCodeType.Text = "Tip:"
        '
        'cmbCodeType
        '
        Me.cmbCodeType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbCodeType.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbCodeType.Items.AddRange(New Object() {"Single", "Toggle"})
        Me.cmbCodeType.Location = New System.Drawing.Point(620, 82)
        Me.cmbCodeType.Name = "cmbCodeType"
        Me.cmbCodeType.Size = New System.Drawing.Size(120, 25)
        Me.cmbCodeType.TabIndex = 10
        '
        'lblCodeByteIndex
        '
        Me.lblCodeByteIndex.AutoSize = True
        Me.lblCodeByteIndex.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeByteIndex.ForeColor = System.Drawing.Color.White
        Me.lblCodeByteIndex.Location = New System.Drawing.Point(400, 125)
        Me.lblCodeByteIndex.Name = "lblCodeByteIndex"
        Me.lblCodeByteIndex.Size = New System.Drawing.Size(76, 19)
        Me.lblCodeByteIndex.TabIndex = 11
        Me.lblCodeByteIndex.Text = "Byte Index:"
        '
        'txtCodeByteIndex
        '
        Me.txtCodeByteIndex.Font = New System.Drawing.Font("Consolas", 11.0!)
        Me.txtCodeByteIndex.Location = New System.Drawing.Point(480, 122)
        Me.txtCodeByteIndex.Name = "txtCodeByteIndex"
        Me.txtCodeByteIndex.Size = New System.Drawing.Size(60, 25)
        Me.txtCodeByteIndex.TabIndex = 12
        '
        'lblCodeData
        '
        Me.lblCodeData.AutoSize = True
        Me.lblCodeData.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCodeData.ForeColor = System.Drawing.Color.White
        Me.lblCodeData.Location = New System.Drawing.Point(400, 165)
        Me.lblCodeData.Name = "lblCodeData"
        Me.lblCodeData.Size = New System.Drawing.Size(76, 19)
        Me.lblCodeData.TabIndex = 13
        Me.lblCodeData.Text = "Data (Hex):"
        '
        'txtCodeData
        '
        Me.txtCodeData.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtCodeData.Font = New System.Drawing.Font("Consolas", 11.0!)
        Me.txtCodeData.Location = New System.Drawing.Point(480, 162)
        Me.txtCodeData.MaxLength = 16
        Me.txtCodeData.Name = "txtCodeData"
        Me.txtCodeData.Size = New System.Drawing.Size(260, 25)
        Me.txtCodeData.TabIndex = 14
        '
        'txtCodePreview
        '
        Me.txtCodePreview.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(25, Byte), Integer))
        Me.txtCodePreview.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtCodePreview.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer))
        Me.txtCodePreview.Location = New System.Drawing.Point(20, 300)
        Me.txtCodePreview.Multiline = True
        Me.txtCodePreview.Name = "txtCodePreview"
        Me.txtCodePreview.ReadOnly = True
        Me.txtCodePreview.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtCodePreview.Size = New System.Drawing.Size(760, 140)
        Me.txtCodePreview.TabIndex = 15
        '
        'btnCodingOn
        '
        Me.btnCodingOn.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnCodingOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCodingOn.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnCodingOn.ForeColor = System.Drawing.Color.White
        Me.btnCodingOn.Location = New System.Drawing.Point(20, 460)
        Me.btnCodingOn.Name = "btnCodingOn"
        Me.btnCodingOn.Size = New System.Drawing.Size(120, 45)
        Me.btnCodingOn.TabIndex = 16
        Me.btnCodingOn.Text = "🟢 ON"
        Me.btnCodingOn.UseVisualStyleBackColor = False
        '
        'btnCodingOff
        '
        Me.btnCodingOff.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnCodingOff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCodingOff.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnCodingOff.ForeColor = System.Drawing.Color.White
        Me.btnCodingOff.Location = New System.Drawing.Point(160, 460)
        Me.btnCodingOff.Name = "btnCodingOff"
        Me.btnCodingOff.Size = New System.Drawing.Size(120, 45)
        Me.btnCodingOff.TabIndex = 17
        Me.btnCodingOff.Text = "🔴 OFF"
        Me.btnCodingOff.UseVisualStyleBackColor = False
        '
        'btnCodeSend
        '
        Me.btnCodeSend.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnCodeSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCodeSend.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnCodeSend.ForeColor = System.Drawing.Color.White
        Me.btnCodeSend.Location = New System.Drawing.Point(300, 460)
        Me.btnCodeSend.Name = "btnCodeSend"
        Me.btnCodeSend.Size = New System.Drawing.Size(150, 45)
        Me.btnCodeSend.TabIndex = 18
        Me.btnCodeSend.Text = "📨 Gönder"
        Me.btnCodeSend.UseVisualStyleBackColor = False
        '
        'btnCodeSaveFromLog
        '
        Me.btnCodeSaveFromLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnCodeSaveFromLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCodeSaveFromLog.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnCodeSaveFromLog.ForeColor = System.Drawing.Color.White
        Me.btnCodeSaveFromLog.Location = New System.Drawing.Point(470, 460)
        Me.btnCodeSaveFromLog.Name = "btnCodeSaveFromLog"
        Me.btnCodeSaveFromLog.Size = New System.Drawing.Size(180, 45)
        Me.btnCodeSaveFromLog.TabIndex = 19
        Me.btnCodeSaveFromLog.Text = "💾 Komutu Kaydet"
        Me.btnCodeSaveFromLog.UseVisualStyleBackColor = False
        '
        'tabLearning
        '
        Me.tabLearning.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.tabLearning.Controls.Add(Me.grpDiff)
        Me.tabLearning.Controls.Add(Me.grpAIFinder)
        Me.tabLearning.Location = New System.Drawing.Point(4, 26)
        Me.tabLearning.Name = "tabLearning"
        Me.tabLearning.Size = New System.Drawing.Size(1226, 681)
        Me.tabLearning.TabIndex = 5
        Me.tabLearning.Text = "🧠 Öğrenme"
        '
        'grpDiff
        '
        Me.grpDiff.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.grpDiff.Controls.Add(Me.lblDiffStatus)
        Me.grpDiff.Controls.Add(Me.btnStartDiff)
        Me.grpDiff.Controls.Add(Me.btnStopDiff)
        Me.grpDiff.Controls.Add(Me.btnClearLearning)
        Me.grpDiff.Controls.Add(Me.btnExportLearning)
        Me.grpDiff.Controls.Add(Me.lstDiff)
        Me.grpDiff.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.grpDiff.Location = New System.Drawing.Point(20, 20)
        Me.grpDiff.Name = "grpDiff"
        Me.grpDiff.Size = New System.Drawing.Size(600, 511)
        Me.grpDiff.TabIndex = 0
        Me.grpDiff.TabStop = False
        Me.grpDiff.Text = "🔬 Değişen Byte Dedektörü"
        '
        'lblDiffStatus
        '
        Me.lblDiffStatus.AutoSize = True
        Me.lblDiffStatus.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblDiffStatus.Location = New System.Drawing.Point(20, 35)
        Me.lblDiffStatus.Name = "lblDiffStatus"
        Me.lblDiffStatus.Size = New System.Drawing.Size(160, 19)
        Me.lblDiffStatus.TabIndex = 0
        Me.lblDiffStatus.Text = "⚪ Takip Durumu: Kapalı"
        '
        'btnStartDiff
        '
        Me.btnStartDiff.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnStartDiff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStartDiff.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnStartDiff.ForeColor = System.Drawing.Color.White
        Me.btnStartDiff.Location = New System.Drawing.Point(20, 70)
        Me.btnStartDiff.Name = "btnStartDiff"
        Me.btnStartDiff.Size = New System.Drawing.Size(140, 40)
        Me.btnStartDiff.TabIndex = 1
        Me.btnStartDiff.Text = "▶️ Takibi Başlat"
        Me.btnStartDiff.UseVisualStyleBackColor = False
        '
        'btnStopDiff
        '
        Me.btnStopDiff.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnStopDiff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStopDiff.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnStopDiff.ForeColor = System.Drawing.Color.White
        Me.btnStopDiff.Location = New System.Drawing.Point(175, 70)
        Me.btnStopDiff.Name = "btnStopDiff"
        Me.btnStopDiff.Size = New System.Drawing.Size(140, 40)
        Me.btnStopDiff.TabIndex = 2
        Me.btnStopDiff.Text = "⏹️ Takibi Durdur"
        Me.btnStopDiff.UseVisualStyleBackColor = False
        '
        'btnClearLearning
        '
        Me.btnClearLearning.BackColor = System.Drawing.Color.FromArgb(CType(CType(149, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(166, Byte), Integer))
        Me.btnClearLearning.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClearLearning.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnClearLearning.ForeColor = System.Drawing.Color.White
        Me.btnClearLearning.Location = New System.Drawing.Point(330, 70)
        Me.btnClearLearning.Name = "btnClearLearning"
        Me.btnClearLearning.Size = New System.Drawing.Size(120, 40)
        Me.btnClearLearning.TabIndex = 3
        Me.btnClearLearning.Text = "🗑️ Temizle"
        Me.btnClearLearning.UseVisualStyleBackColor = False
        '
        'btnExportLearning
        '
        Me.btnExportLearning.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnExportLearning.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnExportLearning.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnExportLearning.ForeColor = System.Drawing.Color.White
        Me.btnExportLearning.Location = New System.Drawing.Point(460, 70)
        Me.btnExportLearning.Name = "btnExportLearning"
        Me.btnExportLearning.Size = New System.Drawing.Size(120, 40)
        Me.btnExportLearning.TabIndex = 4
        Me.btnExportLearning.Text = "📤 Dışa Aktar"
        Me.btnExportLearning.UseVisualStyleBackColor = False
        '
        'lstDiff
        '
        Me.lstDiff.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstDiff.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lstDiff.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lstDiff.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lstDiff.ItemHeight = 15
        Me.lstDiff.Location = New System.Drawing.Point(20, 130)
        Me.lstDiff.Name = "lstDiff"
        Me.lstDiff.Size = New System.Drawing.Size(560, 349)
        Me.lstDiff.TabIndex = 5
        '
        'grpAIFinder
        '
        Me.grpAIFinder.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpAIFinder.Controls.Add(Me.btnSnapshotBefore)
        Me.grpAIFinder.Controls.Add(Me.btnSnapshotAfter)
        Me.grpAIFinder.Controls.Add(Me.btnAutoDiff)
        Me.grpAIFinder.Controls.Add(Me.chkAIFinderEnabled)
        Me.grpAIFinder.Controls.Add(Me.lblAIFinderStatus)
        Me.grpAIFinder.Controls.Add(Me.btnStartAIFinder)
        Me.grpAIFinder.Controls.Add(Me.btnStopAIFinder)
        Me.grpAIFinder.Controls.Add(Me.btnAIFinderSaveAll)
        Me.grpAIFinder.Controls.Add(Me.lblAIFinderCategory)
        Me.grpAIFinder.Controls.Add(Me.cmbAIFinderCategory)
        Me.grpAIFinder.Controls.Add(Me.lstAIFinderResults)
        Me.grpAIFinder.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.grpAIFinder.Location = New System.Drawing.Point(674, 20)
        Me.grpAIFinder.Name = "grpAIFinder"
        Me.grpAIFinder.Size = New System.Drawing.Size(520, 511)
        Me.grpAIFinder.TabIndex = 1
        Me.grpAIFinder.TabStop = False
        Me.grpAIFinder.Text = "🤖 AI-Finder (Otomatik Keşif)"
        '
        'btnSnapshotBefore
        '
        Me.btnSnapshotBefore.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnSnapshotBefore.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSnapshotBefore.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Bold)
        Me.btnSnapshotBefore.ForeColor = System.Drawing.Color.White
        Me.btnSnapshotBefore.Location = New System.Drawing.Point(340, 122)
        Me.btnSnapshotBefore.Name = "btnSnapshotBefore"
        Me.btnSnapshotBefore.Size = New System.Drawing.Size(75, 28)
        Me.btnSnapshotBefore.TabIndex = 0
        Me.btnSnapshotBefore.Text = "📷 Before"
        Me.btnSnapshotBefore.UseVisualStyleBackColor = False
        '
        'btnSnapshotAfter
        '
        Me.btnSnapshotAfter.BackColor = System.Drawing.Color.FromArgb(CType(CType(39, Byte), Integer), CType(CType(174, Byte), Integer), CType(CType(96, Byte), Integer))
        Me.btnSnapshotAfter.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSnapshotAfter.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Bold)
        Me.btnSnapshotAfter.ForeColor = System.Drawing.Color.White
        Me.btnSnapshotAfter.Location = New System.Drawing.Point(420, 122)
        Me.btnSnapshotAfter.Name = "btnSnapshotAfter"
        Me.btnSnapshotAfter.Size = New System.Drawing.Size(75, 28)
        Me.btnSnapshotAfter.TabIndex = 1
        Me.btnSnapshotAfter.Text = "📷 After"
        Me.btnSnapshotAfter.UseVisualStyleBackColor = False
        '
        'btnAutoDiff
        '
        Me.btnAutoDiff.BackColor = System.Drawing.Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(196, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.btnAutoDiff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAutoDiff.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnAutoDiff.ForeColor = System.Drawing.Color.Black
        Me.btnAutoDiff.Location = New System.Drawing.Point(340, 70)
        Me.btnAutoDiff.Name = "btnAutoDiff"
        Me.btnAutoDiff.Size = New System.Drawing.Size(155, 40)
        Me.btnAutoDiff.TabIndex = 2
        Me.btnAutoDiff.Text = "🔍 Auto-Diff"
        Me.btnAutoDiff.UseVisualStyleBackColor = False
        '
        'chkAIFinderEnabled
        '
        Me.chkAIFinderEnabled.AutoSize = True
        Me.chkAIFinderEnabled.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.chkAIFinderEnabled.Location = New System.Drawing.Point(20, 35)
        Me.chkAIFinderEnabled.Name = "chkAIFinderEnabled"
        Me.chkAIFinderEnabled.Size = New System.Drawing.Size(117, 23)
        Me.chkAIFinderEnabled.TabIndex = 3
        Me.chkAIFinderEnabled.Text = "AI-Finder Aktif"
        '
        'lblAIFinderStatus
        '
        Me.lblAIFinderStatus.AutoSize = True
        Me.lblAIFinderStatus.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblAIFinderStatus.Location = New System.Drawing.Point(180, 36)
        Me.lblAIFinderStatus.Name = "lblAIFinderStatus"
        Me.lblAIFinderStatus.Size = New System.Drawing.Size(118, 19)
        Me.lblAIFinderStatus.TabIndex = 4
        Me.lblAIFinderStatus.Text = "⚪ Durum: Kapalı"
        '
        'btnStartAIFinder
        '
        Me.btnStartAIFinder.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnStartAIFinder.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStartAIFinder.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnStartAIFinder.ForeColor = System.Drawing.Color.White
        Me.btnStartAIFinder.Location = New System.Drawing.Point(20, 70)
        Me.btnStartAIFinder.Name = "btnStartAIFinder"
        Me.btnStartAIFinder.Size = New System.Drawing.Size(140, 40)
        Me.btnStartAIFinder.TabIndex = 5
        Me.btnStartAIFinder.Text = "🚀 AI Başlat"
        Me.btnStartAIFinder.UseVisualStyleBackColor = False
        '
        'btnStopAIFinder
        '
        Me.btnStopAIFinder.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnStopAIFinder.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStopAIFinder.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnStopAIFinder.ForeColor = System.Drawing.Color.White
        Me.btnStopAIFinder.Location = New System.Drawing.Point(175, 70)
        Me.btnStopAIFinder.Name = "btnStopAIFinder"
        Me.btnStopAIFinder.Size = New System.Drawing.Size(140, 40)
        Me.btnStopAIFinder.TabIndex = 6
        Me.btnStopAIFinder.Text = "⏹️ Durdur"
        Me.btnStopAIFinder.UseVisualStyleBackColor = False
        '
        'btnAIFinderSaveAll
        '
        Me.btnAIFinderSaveAll.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnAIFinderSaveAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAIFinderSaveAll.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnAIFinderSaveAll.ForeColor = System.Drawing.Color.White
        Me.btnAIFinderSaveAll.Location = New System.Drawing.Point(330, 70)
        Me.btnAIFinderSaveAll.Name = "btnAIFinderSaveAll"
        Me.btnAIFinderSaveAll.Size = New System.Drawing.Size(170, 40)
        Me.btnAIFinderSaveAll.TabIndex = 7
        Me.btnAIFinderSaveAll.Text = "💾 Tümünü Kaydet"
        Me.btnAIFinderSaveAll.UseVisualStyleBackColor = False
        '
        'lblAIFinderCategory
        '
        Me.lblAIFinderCategory.AutoSize = True
        Me.lblAIFinderCategory.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblAIFinderCategory.Location = New System.Drawing.Point(20, 125)
        Me.lblAIFinderCategory.Name = "lblAIFinderCategory"
        Me.lblAIFinderCategory.Size = New System.Drawing.Size(106, 19)
        Me.lblAIFinderCategory.TabIndex = 8
        Me.lblAIFinderCategory.Text = "Kategori Filtresi:"
        '
        'cmbAIFinderCategory
        '
        Me.cmbAIFinderCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbAIFinderCategory.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbAIFinderCategory.Items.AddRange(New Object() {"Tümü", "Lighting", "Doors", "Windows", "Engine", "Climate", "Sensors", "Unknown"})
        Me.cmbAIFinderCategory.Location = New System.Drawing.Point(140, 122)
        Me.cmbAIFinderCategory.Name = "cmbAIFinderCategory"
        Me.cmbAIFinderCategory.Size = New System.Drawing.Size(180, 25)
        Me.cmbAIFinderCategory.TabIndex = 9
        '
        'lstAIFinderResults
        '
        Me.lstAIFinderResults.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstAIFinderResults.BackColor = System.Drawing.Color.FromArgb(CType(CType(20, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.lstAIFinderResults.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.lstAIFinderResults.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.lstAIFinderResults.ItemHeight = 14
        Me.lstAIFinderResults.Location = New System.Drawing.Point(20, 160)
        Me.lstAIFinderResults.Name = "lstAIFinderResults"
        Me.lstAIFinderResults.Size = New System.Drawing.Size(480, 312)
        Me.lstAIFinderResults.TabIndex = 10
        '
        'tabLogAnalysis
        '
        Me.tabLogAnalysis.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.tabLogAnalysis.Controls.Add(Me.grpLogAnalysis)
        Me.tabLogAnalysis.Location = New System.Drawing.Point(4, 26)
        Me.tabLogAnalysis.Name = "tabLogAnalysis"
        Me.tabLogAnalysis.Size = New System.Drawing.Size(1226, 681)
        Me.tabLogAnalysis.TabIndex = 6
        Me.tabLogAnalysis.Text = "📈 Log Analiz"
        '
        'grpLogAnalysis
        '
        Me.grpLogAnalysis.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpLogAnalysis.Controls.Add(Me.btnLoadLogFile)
        Me.grpLogAnalysis.Controls.Add(Me.btnAnalyzeLog)
        Me.grpLogAnalysis.Controls.Add(Me.lblIdStats)
        Me.grpLogAnalysis.Controls.Add(Me.lstIdStats)
        Me.grpLogAnalysis.Controls.Add(Me.txtAnalysisResult)
        Me.grpLogAnalysis.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.grpLogAnalysis.Location = New System.Drawing.Point(20, 20)
        Me.grpLogAnalysis.Name = "grpLogAnalysis"
        Me.grpLogAnalysis.Size = New System.Drawing.Size(800, 550)
        Me.grpLogAnalysis.TabIndex = 0
        Me.grpLogAnalysis.TabStop = False
        Me.grpLogAnalysis.Text = "📊 Log Analizi"
        '
        'btnLoadLogFile
        '
        Me.btnLoadLogFile.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnLoadLogFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnLoadLogFile.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnLoadLogFile.ForeColor = System.Drawing.Color.White
        Me.btnLoadLogFile.Location = New System.Drawing.Point(20, 35)
        Me.btnLoadLogFile.Name = "btnLoadLogFile"
        Me.btnLoadLogFile.Size = New System.Drawing.Size(160, 40)
        Me.btnLoadLogFile.TabIndex = 0
        Me.btnLoadLogFile.Text = "📂 Log Dosyası Yükle"
        Me.btnLoadLogFile.UseVisualStyleBackColor = False
        '
        'btnAnalyzeLog
        '
        Me.btnAnalyzeLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnAnalyzeLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAnalyzeLog.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnAnalyzeLog.ForeColor = System.Drawing.Color.White
        Me.btnAnalyzeLog.Location = New System.Drawing.Point(200, 35)
        Me.btnAnalyzeLog.Name = "btnAnalyzeLog"
        Me.btnAnalyzeLog.Size = New System.Drawing.Size(160, 40)
        Me.btnAnalyzeLog.TabIndex = 1
        Me.btnAnalyzeLog.Text = "🔍 Mevcut Logu Analiz Et"
        Me.btnAnalyzeLog.UseVisualStyleBackColor = False
        '
        'lblIdStats
        '
        Me.lblIdStats.AutoSize = True
        Me.lblIdStats.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.lblIdStats.Location = New System.Drawing.Point(20, 95)
        Me.lblIdStats.Name = "lblIdStats"
        Me.lblIdStats.Size = New System.Drawing.Size(108, 19)
        Me.lblIdStats.TabIndex = 2
        Me.lblIdStats.Text = "ID İstatistikleri:"
        '
        'lstIdStats
        '
        Me.lstIdStats.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lstIdStats.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lstIdStats.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lstIdStats.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lstIdStats.ItemHeight = 15
        Me.lstIdStats.Location = New System.Drawing.Point(20, 120)
        Me.lstIdStats.Name = "lstIdStats"
        Me.lstIdStats.Size = New System.Drawing.Size(300, 394)
        Me.lstIdStats.TabIndex = 3
        '
        'txtAnalysisResult
        '
        Me.txtAnalysisResult.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtAnalysisResult.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.txtAnalysisResult.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtAnalysisResult.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer))
        Me.txtAnalysisResult.Location = New System.Drawing.Point(340, 120)
        Me.txtAnalysisResult.Multiline = True
        Me.txtAnalysisResult.Name = "txtAnalysisResult"
        Me.txtAnalysisResult.ReadOnly = True
        Me.txtAnalysisResult.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtAnalysisResult.Size = New System.Drawing.Size(440, 400)
        Me.txtAnalysisResult.TabIndex = 4
        '
        'tabJsonEditor
        '
        Me.tabJsonEditor.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.tabJsonEditor.Controls.Add(Me.grpJsonEditor)
        Me.tabJsonEditor.Location = New System.Drawing.Point(4, 26)
        Me.tabJsonEditor.Name = "tabJsonEditor"
        Me.tabJsonEditor.Size = New System.Drawing.Size(1226, 681)
        Me.tabJsonEditor.TabIndex = 7
        Me.tabJsonEditor.Text = "📝 JSON Editör"
        '
        'grpJsonEditor
        '
        Me.grpJsonEditor.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpJsonEditor.Controls.Add(Me.lblEditBrand)
        Me.grpJsonEditor.Controls.Add(Me.cmbEditBrand)
        Me.grpJsonEditor.Controls.Add(Me.btnNewBrand)
        Me.grpJsonEditor.Controls.Add(Me.lblEditModel)
        Me.grpJsonEditor.Controls.Add(Me.cmbEditModel)
        Me.grpJsonEditor.Controls.Add(Me.btnNewModel)
        Me.grpJsonEditor.Controls.Add(Me.lblEditModule)
        Me.grpJsonEditor.Controls.Add(Me.cmbEditModule)
        Me.grpJsonEditor.Controls.Add(Me.btnNewModule)
        Me.grpJsonEditor.Controls.Add(Me.lblEditCommands)
        Me.grpJsonEditor.Controls.Add(Me.lstEditCommands)
        Me.grpJsonEditor.Controls.Add(Me.lblEditCommandName)
        Me.grpJsonEditor.Controls.Add(Me.txtEditCommandName)
        Me.grpJsonEditor.Controls.Add(Me.lblEditCommandFrame)
        Me.grpJsonEditor.Controls.Add(Me.txtEditCommandFrame)
        Me.grpJsonEditor.Controls.Add(Me.btnAddCommand)
        Me.grpJsonEditor.Controls.Add(Me.btnUpdateCommand)
        Me.grpJsonEditor.Controls.Add(Me.btnDeleteCommand)
        Me.grpJsonEditor.Controls.Add(Me.btnSaveJson)
        Me.grpJsonEditor.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.grpJsonEditor.Location = New System.Drawing.Point(20, 20)
        Me.grpJsonEditor.Name = "grpJsonEditor"
        Me.grpJsonEditor.Size = New System.Drawing.Size(834, 561)
        Me.grpJsonEditor.TabIndex = 0
        Me.grpJsonEditor.TabStop = False
        Me.grpJsonEditor.Text = "📦 Komut Veritabanı Editörü"
        '
        'lblEditBrand
        '
        Me.lblEditBrand.AutoSize = True
        Me.lblEditBrand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEditBrand.Location = New System.Drawing.Point(20, 40)
        Me.lblEditBrand.Name = "lblEditBrand"
        Me.lblEditBrand.Size = New System.Drawing.Size(51, 19)
        Me.lblEditBrand.TabIndex = 0
        Me.lblEditBrand.Text = "Marka:"
        '
        'cmbEditBrand
        '
        Me.cmbEditBrand.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbEditBrand.Location = New System.Drawing.Point(100, 37)
        Me.cmbEditBrand.Name = "cmbEditBrand"
        Me.cmbEditBrand.Size = New System.Drawing.Size(200, 25)
        Me.cmbEditBrand.TabIndex = 1
        '
        'btnNewBrand
        '
        Me.btnNewBrand.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnNewBrand.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnNewBrand.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnNewBrand.ForeColor = System.Drawing.Color.White
        Me.btnNewBrand.Location = New System.Drawing.Point(310, 35)
        Me.btnNewBrand.Name = "btnNewBrand"
        Me.btnNewBrand.Size = New System.Drawing.Size(80, 30)
        Me.btnNewBrand.TabIndex = 2
        Me.btnNewBrand.Text = "+ Yeni"
        Me.btnNewBrand.UseVisualStyleBackColor = False
        '
        'lblEditModel
        '
        Me.lblEditModel.AutoSize = True
        Me.lblEditModel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEditModel.Location = New System.Drawing.Point(20, 80)
        Me.lblEditModel.Name = "lblEditModel"
        Me.lblEditModel.Size = New System.Drawing.Size(51, 19)
        Me.lblEditModel.TabIndex = 3
        Me.lblEditModel.Text = "Model:"
        '
        'cmbEditModel
        '
        Me.cmbEditModel.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbEditModel.Location = New System.Drawing.Point(100, 77)
        Me.cmbEditModel.Name = "cmbEditModel"
        Me.cmbEditModel.Size = New System.Drawing.Size(200, 25)
        Me.cmbEditModel.TabIndex = 4
        '
        'btnNewModel
        '
        Me.btnNewModel.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnNewModel.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnNewModel.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnNewModel.ForeColor = System.Drawing.Color.White
        Me.btnNewModel.Location = New System.Drawing.Point(310, 75)
        Me.btnNewModel.Name = "btnNewModel"
        Me.btnNewModel.Size = New System.Drawing.Size(80, 30)
        Me.btnNewModel.TabIndex = 5
        Me.btnNewModel.Text = "+ Yeni"
        Me.btnNewModel.UseVisualStyleBackColor = False
        '
        'lblEditModule
        '
        Me.lblEditModule.AutoSize = True
        Me.lblEditModule.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEditModule.Location = New System.Drawing.Point(20, 120)
        Me.lblEditModule.Name = "lblEditModule"
        Me.lblEditModule.Size = New System.Drawing.Size(52, 19)
        Me.lblEditModule.TabIndex = 6
        Me.lblEditModule.Text = "Modül:"
        '
        'cmbEditModule
        '
        Me.cmbEditModule.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.cmbEditModule.Location = New System.Drawing.Point(100, 117)
        Me.cmbEditModule.Name = "cmbEditModule"
        Me.cmbEditModule.Size = New System.Drawing.Size(200, 25)
        Me.cmbEditModule.TabIndex = 7
        '
        'btnNewModule
        '
        Me.btnNewModule.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnNewModule.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnNewModule.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnNewModule.ForeColor = System.Drawing.Color.White
        Me.btnNewModule.Location = New System.Drawing.Point(310, 115)
        Me.btnNewModule.Name = "btnNewModule"
        Me.btnNewModule.Size = New System.Drawing.Size(80, 30)
        Me.btnNewModule.TabIndex = 8
        Me.btnNewModule.Text = "+ Yeni"
        Me.btnNewModule.UseVisualStyleBackColor = False
        '
        'lblEditCommands
        '
        Me.lblEditCommands.AutoSize = True
        Me.lblEditCommands.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEditCommands.Location = New System.Drawing.Point(20, 160)
        Me.lblEditCommands.Name = "lblEditCommands"
        Me.lblEditCommands.Size = New System.Drawing.Size(68, 19)
        Me.lblEditCommands.TabIndex = 9
        Me.lblEditCommands.Text = "Komutlar:"
        '
        'lstEditCommands
        '
        Me.lstEditCommands.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lstEditCommands.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.lstEditCommands.ItemHeight = 15
        Me.lstEditCommands.Location = New System.Drawing.Point(20, 185)
        Me.lstEditCommands.Name = "lstEditCommands"
        Me.lstEditCommands.Size = New System.Drawing.Size(370, 199)
        Me.lstEditCommands.TabIndex = 10
        '
        'lblEditCommandName
        '
        Me.lblEditCommandName.AutoSize = True
        Me.lblEditCommandName.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEditCommandName.Location = New System.Drawing.Point(20, 400)
        Me.lblEditCommandName.Name = "lblEditCommandName"
        Me.lblEditCommandName.Size = New System.Drawing.Size(77, 19)
        Me.lblEditCommandName.TabIndex = 11
        Me.lblEditCommandName.Text = "Komut Adı:"
        '
        'txtEditCommandName
        '
        Me.txtEditCommandName.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtEditCommandName.Location = New System.Drawing.Point(120, 397)
        Me.txtEditCommandName.Name = "txtEditCommandName"
        Me.txtEditCommandName.Size = New System.Drawing.Size(270, 25)
        Me.txtEditCommandName.TabIndex = 12
        '
        'lblEditCommandFrame
        '
        Me.lblEditCommandFrame.AutoSize = True
        Me.lblEditCommandFrame.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEditCommandFrame.Location = New System.Drawing.Point(20, 440)
        Me.lblEditCommandFrame.Name = "lblEditCommandFrame"
        Me.lblEditCommandFrame.Size = New System.Drawing.Size(50, 19)
        Me.lblEditCommandFrame.TabIndex = 13
        Me.lblEditCommandFrame.Text = "Frame:"
        '
        'txtEditCommandFrame
        '
        Me.txtEditCommandFrame.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtEditCommandFrame.Font = New System.Drawing.Font("Consolas", 10.0!)
        Me.txtEditCommandFrame.Location = New System.Drawing.Point(120, 437)
        Me.txtEditCommandFrame.Name = "txtEditCommandFrame"
        Me.txtEditCommandFrame.Size = New System.Drawing.Size(270, 23)
        Me.txtEditCommandFrame.TabIndex = 14
        '
        'btnAddCommand
        '
        Me.btnAddCommand.BackColor = System.Drawing.Color.FromArgb(CType(CType(46, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(113, Byte), Integer))
        Me.btnAddCommand.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAddCommand.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnAddCommand.ForeColor = System.Drawing.Color.White
        Me.btnAddCommand.Location = New System.Drawing.Point(20, 490)
        Me.btnAddCommand.Name = "btnAddCommand"
        Me.btnAddCommand.Size = New System.Drawing.Size(110, 40)
        Me.btnAddCommand.TabIndex = 15
        Me.btnAddCommand.Text = "➕ Ekle"
        Me.btnAddCommand.UseVisualStyleBackColor = False
        '
        'btnUpdateCommand
        '
        Me.btnUpdateCommand.BackColor = System.Drawing.Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(196, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.btnUpdateCommand.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnUpdateCommand.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnUpdateCommand.ForeColor = System.Drawing.Color.White
        Me.btnUpdateCommand.Location = New System.Drawing.Point(145, 490)
        Me.btnUpdateCommand.Name = "btnUpdateCommand"
        Me.btnUpdateCommand.Size = New System.Drawing.Size(110, 40)
        Me.btnUpdateCommand.TabIndex = 16
        Me.btnUpdateCommand.Text = "✏️ Güncelle"
        Me.btnUpdateCommand.UseVisualStyleBackColor = False
        '
        'btnDeleteCommand
        '
        Me.btnDeleteCommand.BackColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(76, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnDeleteCommand.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnDeleteCommand.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnDeleteCommand.ForeColor = System.Drawing.Color.White
        Me.btnDeleteCommand.Location = New System.Drawing.Point(270, 490)
        Me.btnDeleteCommand.Name = "btnDeleteCommand"
        Me.btnDeleteCommand.Size = New System.Drawing.Size(110, 40)
        Me.btnDeleteCommand.TabIndex = 17
        Me.btnDeleteCommand.Text = "🗑️ Sil"
        Me.btnDeleteCommand.UseVisualStyleBackColor = False
        '
        'btnSaveJson
        '
        Me.btnSaveJson.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnSaveJson.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSaveJson.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnSaveJson.ForeColor = System.Drawing.Color.White
        Me.btnSaveJson.Location = New System.Drawing.Point(600, 490)
        Me.btnSaveJson.Name = "btnSaveJson"
        Me.btnSaveJson.Size = New System.Drawing.Size(180, 45)
        Me.btnSaveJson.TabIndex = 18
        Me.btnSaveJson.Text = "💾 JSON Kaydet"
        Me.btnSaveJson.UseVisualStyleBackColor = False
        '
        'btnHelpConnection
        '
        Me.btnHelpConnection.Location = New System.Drawing.Point(0, 0)
        Me.btnHelpConnection.Name = "btnHelpConnection"
        Me.btnHelpConnection.Size = New System.Drawing.Size(75, 23)
        Me.btnHelpConnection.TabIndex = 0
        '
        'SerialPort1
        '
        '
        'liveChartTemp
        '
        Me.liveChartTemp.AutoScale = True
        Me.liveChartTemp.Dock = System.Windows.Forms.DockStyle.Fill
        Me.liveChartTemp.GridColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.liveChartTemp.LineColor = System.Drawing.Color.Lime
        Me.liveChartTemp.Location = New System.Drawing.Point(788, 0)
        Me.liveChartTemp.MaxValue = 100.0R
        Me.liveChartTemp.MinValue = 0R
        Me.liveChartTemp.Name = "liveChartTemp"
        Me.liveChartTemp.Size = New System.Drawing.Size(396, 270)
        Me.liveChartTemp.TabIndex = 2
        Me.liveChartTemp.Title = "Grafik"
        Me.liveChartTemp.Unit = ""
        '
        'liveChartSpeed
        '
        Me.liveChartSpeed.AutoScale = True
        Me.liveChartSpeed.Dock = System.Windows.Forms.DockStyle.Left
        Me.liveChartSpeed.GridColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.liveChartSpeed.LineColor = System.Drawing.Color.Lime
        Me.liveChartSpeed.Location = New System.Drawing.Point(394, 0)
        Me.liveChartSpeed.MaxValue = 100.0R
        Me.liveChartSpeed.MinValue = 0R
        Me.liveChartSpeed.Name = "liveChartSpeed"
        Me.liveChartSpeed.Size = New System.Drawing.Size(394, 270)
        Me.liveChartSpeed.TabIndex = 1
        Me.liveChartSpeed.Title = "Grafik"
        Me.liveChartSpeed.Unit = ""
        '
        'liveChartRPM
        '
        Me.liveChartRPM.AutoScale = True
        Me.liveChartRPM.Dock = System.Windows.Forms.DockStyle.Left
        Me.liveChartRPM.GridColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.liveChartRPM.LineColor = System.Drawing.Color.Lime
        Me.liveChartRPM.Location = New System.Drawing.Point(0, 0)
        Me.liveChartRPM.MaxValue = 100.0R
        Me.liveChartRPM.MinValue = 0R
        Me.liveChartRPM.Name = "liveChartRPM"
        Me.liveChartRPM.Size = New System.Drawing.Size(394, 270)
        Me.liveChartRPM.TabIndex = 0
        Me.liveChartRPM.Title = "Grafik"
        Me.liveChartRPM.Unit = ""
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(240, Byte), Integer), CType(CType(240, Byte), Integer), CType(CType(245, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1234, 711)
        Me.Controls.Add(Me.tabMain)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.MinimumSize = New System.Drawing.Size(1250, 750)
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "CarCANReader v1.0 - Professional CAN Diagnostic Tool"
        Me.mnuMain.ResumeLayout(False)
        Me.mnuMain.PerformLayout()
        Me.tabMain.ResumeLayout(False)
        Me.tabConnection.ResumeLayout(False)
        Me.tabConnection.PerformLayout()
        Me.grpManualSend.ResumeLayout(False)
        Me.grpManualSend.PerformLayout()
        Me.grpFilter.ResumeLayout(False)
        Me.grpFilter.PerformLayout()
        Me.tabDashboard.ResumeLayout(False)
        Me.grpCanAnalysis.ResumeLayout(False)
        Me.grpCanAnalysis.PerformLayout()
        Me.grpOBD.ResumeLayout(False)
        Me.grpOBD.PerformLayout()
        Me.splitContainerCharts.Panel2.ResumeLayout(False)
        CType(Me.splitContainerCharts, System.ComponentModel.ISupportInitialize).EndInit()
        Me.splitContainerCharts.ResumeLayout(False)
        Me.grpChartControls.ResumeLayout(False)
        Me.grpChartControls.PerformLayout()
        Me.tabDiagnostics.ResumeLayout(False)
        Me.grpVINProfile.ResumeLayout(False)
        Me.grpVINProfile.PerformLayout()
        Me.grpPendingDTC.ResumeLayout(False)
        Me.grpPendingDTC.PerformLayout()
        CType(Me.dgvDTCList, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpFreezeFrame.ResumeLayout(False)
        Me.grpMode06.ResumeLayout(False)
        Me.grpVehicleInfo.ResumeLayout(False)
        Me.grpPIDAutoScan.ResumeLayout(False)
        Me.tabCommands.ResumeLayout(False)
        Me.grpCommandSender.ResumeLayout(False)
        Me.grpCommandSender.PerformLayout()
        Me.tabCoding.ResumeLayout(False)
        Me.grpCoding.ResumeLayout(False)
        Me.grpCoding.PerformLayout()
        Me.tabLearning.ResumeLayout(False)
        Me.grpDiff.ResumeLayout(False)
        Me.grpDiff.PerformLayout()
        Me.grpAIFinder.ResumeLayout(False)
        Me.grpAIFinder.PerformLayout()
        Me.tabLogAnalysis.ResumeLayout(False)
        Me.grpLogAnalysis.ResumeLayout(False)
        Me.grpLogAnalysis.PerformLayout()
        Me.tabJsonEditor.ResumeLayout(False)
        Me.grpJsonEditor.ResumeLayout(False)
        Me.grpJsonEditor.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    ' Tab Control
    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabConnection As TabPage
    Friend WithEvents tabDashboard As TabPage
    Friend WithEvents tabDiagnostics As TabPage
    Friend WithEvents tabCommands As TabPage
    Friend WithEvents tabCoding As TabPage
    Friend WithEvents tabLearning As TabPage
    Friend WithEvents tabLogAnalysis As TabPage
    Friend WithEvents tabJsonEditor As TabPage

    ' Diagnostics Tab - DTC List
    Friend WithEvents grpPendingDTC As GroupBox
    Friend WithEvents dgvDTCList As DataGridView
    Friend WithEvents txtDTCSearch As TextBox
    Friend WithEvents cmbDTCCategory As ComboBox
    Friend WithEvents lblDTCSearch As Label
    Friend WithEvents lblDTCCategory As Label
    Friend WithEvents lblDTCCount As Label
    Friend WithEvents btnReadPendingDTC As Button
    Friend WithEvents btnReadStoredDTC As Button
    Friend WithEvents btnClearDTC As Button

    ' Diagnostics Tab - Freeze Frame
    Friend WithEvents grpFreezeFrame As GroupBox
    Friend WithEvents lstFreezeFrame As ListBox
    Friend WithEvents btnReadFreezeFrame As Button

    ' Diagnostics Tab - Mode 06
    Friend WithEvents grpMode06 As GroupBox
    Friend WithEvents lstMode06 As ListBox
    Friend WithEvents btnReadMode06 As Button

    ' Diagnostics Tab - Vehicle Info
    Friend WithEvents grpVehicleInfo As GroupBox
    Friend WithEvents lblVIN As Label
    Friend WithEvents lblVINValue As Label
    Friend WithEvents lblCalibrationID As Label
    Friend WithEvents lblCalibrationIDValue As Label
    Friend WithEvents lblCVN As Label
    Friend WithEvents lblCVNValue As Label
    Friend WithEvents lblECUName As Label
    Friend WithEvents lblECUNameValue As Label
    Friend WithEvents btnReadVehicleInfo As Button
    Friend WithEvents btnIsoTpTest As Button

    ' Diagnostics Tab - PID Auto-Scan
    Friend WithEvents grpPIDAutoScan As GroupBox
    Friend WithEvents lstSupportedPIDs As ListBox
    Friend WithEvents btnAutoScanPIDs As Button
    Friend WithEvents lblScanStatus As Label

    ' Connection Tab
    Friend WithEvents btnConnect As Button
    Friend WithEvents btnConnectionWizard As Button
    Friend WithEvents btnQuickConnect As Button
    Friend WithEvents lblStatus As Label
    Friend WithEvents lstLog As ListBox
    Friend WithEvents SerialPort1 As IO.Ports.SerialPort
    Friend WithEvents grpManualSend As GroupBox
    Friend WithEvents txtID As TextBox
    Friend WithEvents lblData As Label
    Friend WithEvents lblID As Label
    Friend WithEvents txtData As TextBox
    Friend WithEvents btnSend As Button
    Friend WithEvents btnSaveLog As Button
    Friend WithEvents btnClearLog As Button
    Friend WithEvents btnTestLog As Button

    ' Filter Controls
    Friend WithEvents grpFilter As GroupBox
    Friend WithEvents cmbFilterMode As ComboBox
    Friend WithEvents txtFilterId As TextBox
    Friend WithEvents txtFilterFrom As TextBox
    Friend WithEvents txtFilterTo As TextBox
    Friend WithEvents btnApplyFilter As Button
    Friend WithEvents btnClearFilter As Button
    Friend WithEvents lblFilterId As Label
    Friend WithEvents lblFilterFrom As Label
    Friend WithEvents lblFilterTo As Label

    ' Dashboard Tab
    Friend WithEvents grpCanAnalysis As GroupBox
    Friend WithEvents lblRPM As Label
    Friend WithEvents lblSpeed As Label
    Friend WithEvents lblTemp As Label
    Friend WithEvents lblDoor As Label
    Friend WithEvents lblLight As Label
    Friend WithEvents lblRPMValue As Label
    Friend WithEvents lblSpeedValue As Label
    Friend WithEvents lblTempValue As Label
    Friend WithEvents lblDoorValue As Label
    Friend WithEvents lblLightValue As Label
    Friend WithEvents prgRPM As ProgressBar
    Friend WithEvents prgSpeed As ProgressBar
    Friend WithEvents prgTemp As ProgressBar
    Friend WithEvents pnlDoorIndicator As Panel
    Friend WithEvents pnlLightIndicator As Panel

    ' OBD-II Dashboard Controls
    Friend WithEvents grpOBD As GroupBox
    Friend WithEvents lblThrottle As Label
    Friend WithEvents lblThrottleValue As Label
    Friend WithEvents lblEngineLoad As Label
    Friend WithEvents lblEngineLoadValue As Label
    Friend WithEvents lblIntakeTemp As Label
    Friend WithEvents lblIntakeTempValue As Label
    Friend WithEvents lblAmbientTemp As Label
    Friend WithEvents lblAmbientTempValue As Label
    Friend WithEvents lblFuelLevel As Label
    Friend WithEvents lblFuelLevelValue As Label
    Friend WithEvents lblMAP As Label
    Friend WithEvents lblMAPValue As Label
    Friend WithEvents lblMAF As Label
    Friend WithEvents lblMAFValue As Label
    Friend WithEvents lblShortTrim As Label
    Friend WithEvents lblShortTrimValue As Label
    Friend WithEvents lblLongTrim As Label
    Friend WithEvents lblLongTrimValue As Label
    Friend WithEvents lblBarometric As Label
    Friend WithEvents lblBarometricValue As Label
    Friend WithEvents lblVoltage As Label
    Friend WithEvents lblVoltageValue As Label
    Friend WithEvents lblFuelRate As Label
    Friend WithEvents lblFuelRateValue As Label
    Friend WithEvents btnStartOBD As Button
    Friend WithEvents btnStopOBD As Button

    ' Dashboard Charts
    Friend WithEvents splitContainerCharts As SplitContainer
    Friend WithEvents grpChartControls As GroupBox
    Friend WithEvents btnChartPause As Button
    Friend WithEvents btnChartClear As Button
    Friend WithEvents btnChartExport As Button
    Friend WithEvents cmbChartTimeRange As ComboBox
    Friend WithEvents lblChartTimeRange As Label
    Friend WithEvents liveChartRPM As LiveChart
    Friend WithEvents liveChartSpeed As LiveChart
    Friend WithEvents liveChartTemp As LiveChart
    Friend WithEvents lblOBDStatus As Label

    ' Commands Tab
    Friend WithEvents grpCommandSender As GroupBox
    Friend WithEvents cmbBrand As ComboBox
    Friend WithEvents lblBrand As Label
    Friend WithEvents cmbModel As ComboBox
    Friend WithEvents lblModel As Label
    Friend WithEvents cmbModule As ComboBox
    Friend WithEvents lblModule As Label
    Friend WithEvents cmbCommand As ComboBox
    Friend WithEvents lblCommand As Label
    Friend WithEvents btnSendCommand As Button
    Friend WithEvents txtCommandPreview As TextBox

    ' Coding Tab
    Friend WithEvents grpCoding As GroupBox
    Friend WithEvents cmbCodeBrand As ComboBox
    Friend WithEvents cmbCodeModel As ComboBox
    Friend WithEvents cmbCodeModule As ComboBox
    Friend WithEvents lstCodeCommands As ListBox
    Friend WithEvents txtCodePreview As TextBox
    Friend WithEvents btnCodingOn As Button
    Friend WithEvents btnCodingOff As Button
    Friend WithEvents btnCodeSend As Button
    Friend WithEvents btnCodeSaveFromLog As Button
    Friend WithEvents txtCodeByteIndex As TextBox
    Friend WithEvents txtCodeData As TextBox
    Friend WithEvents txtCodeID As TextBox
    Friend WithEvents cmbCodeType As ComboBox
    Friend WithEvents lblCodeBrand As Label
    Friend WithEvents lblCodeModel As Label
    Friend WithEvents lblCodeModule As Label
    Friend WithEvents lblCodeID As Label
    Friend WithEvents lblCodeByteIndex As Label
    Friend WithEvents lblCodeData As Label
    Friend WithEvents lblCodeType As Label

    ' Learning Tab
    Friend WithEvents grpDiff As GroupBox
    Friend WithEvents btnStartDiff As Button
    Friend WithEvents btnStopDiff As Button
    Friend WithEvents lstDiff As ListBox
    Friend WithEvents lblDiffStatus As Label
    Friend WithEvents btnExportLearning As Button
    Friend WithEvents btnClearLearning As Button

    ' AI-Finder Controls (Learning Tab)
    Friend WithEvents grpAIFinder As GroupBox
    Friend WithEvents chkAIFinderEnabled As CheckBox
    Friend WithEvents btnStartAIFinder As Button
    Friend WithEvents btnStopAIFinder As Button
    Friend WithEvents btnAIFinderSaveAll As Button
    Friend WithEvents lstAIFinderResults As ListBox
    Friend WithEvents lblAIFinderStatus As Label
    Friend WithEvents cmbAIFinderCategory As ComboBox
    Friend WithEvents lblAIFinderCategory As Label
    Friend WithEvents btnSnapshotBefore As Button
    Friend WithEvents btnSnapshotAfter As Button
    Friend WithEvents btnAutoDiff As Button

    ' VIN Auto Profile Controls (Diagnostics Tab)
    Friend WithEvents grpVINProfile As GroupBox
    Friend WithEvents chkVINAutoProfile As CheckBox
    Friend WithEvents lblVINProfileStatus As Label
    Friend WithEvents btnLoadVINProfile As Button
    Friend WithEvents btnSaveVINProfile As Button
    Friend WithEvents lblProfileBrand As Label
    Friend WithEvents lblProfileBrandValue As Label
    Friend WithEvents lblProfileModel As Label
    Friend WithEvents lblProfileModelValue As Label
    Friend WithEvents lblProfileYear As Label
    Friend WithEvents lblProfileYearValue As Label
    Friend WithEvents btnEnrichDatabase As Button

    ' Log Analysis Tab
    Friend WithEvents grpLogAnalysis As GroupBox
    Friend WithEvents btnAnalyzeLog As Button
    Friend WithEvents lblIdStats As Label
    Friend WithEvents lstIdStats As ListBox
    Friend WithEvents txtAnalysisResult As TextBox
    Friend WithEvents btnLoadLogFile As Button

    ' JSON Editor Tab
    Friend WithEvents grpJsonEditor As GroupBox
    Friend WithEvents lblEditBrand As Label
    Friend WithEvents lblEditModel As Label
    Friend WithEvents lblEditModule As Label
    Friend WithEvents lblEditCommands As Label
    Friend WithEvents lblEditCommandName As Label
    Friend WithEvents lblEditCommandFrame As Label
    Friend WithEvents cmbEditBrand As ComboBox
    Friend WithEvents cmbEditModel As ComboBox
    Friend WithEvents cmbEditModule As ComboBox
    Friend WithEvents txtEditCommandName As TextBox
    Friend WithEvents txtEditCommandFrame As TextBox
    Friend WithEvents btnAddCommand As Button
    Friend WithEvents btnUpdateCommand As Button
    Friend WithEvents btnDeleteCommand As Button
    Friend WithEvents btnSaveJson As Button
    Friend WithEvents lstEditCommands As ListBox
    Friend WithEvents btnNewBrand As Button
    Friend WithEvents btnNewModel As Button
    Friend WithEvents btnNewModule As Button

    ' Menu
    Friend WithEvents mnuMain As MenuStrip
    Friend WithEvents mnuTools As ToolStripMenuItem
    Friend WithEvents mnuToolsEcuScanner As ToolStripMenuItem
    Friend WithEvents mnuToolsReport As ToolStripMenuItem
    Friend WithEvents mnuToolsSessionPlayer As ToolStripMenuItem
    Friend WithEvents mnuHelp As ToolStripMenuItem
    Friend WithEvents mnuHelpViewer As ToolStripMenuItem

    ' Help Buttons
    Friend WithEvents btnHelpConnection As Button
    Friend WithEvents btnHelpDashboard As Button
    Friend WithEvents btnHelpDiagnostics As Button

    ' Session Recording Buttons (Toolbar)
    Friend WithEvents btnStartRecording As Button
    Friend WithEvents btnStopRecording As Button

    ' StatusBar for recording info
    Friend WithEvents lblRecordingStatus As ToolStripStatusLabel

End Class
