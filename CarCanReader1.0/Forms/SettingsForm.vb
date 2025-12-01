' SettingsForm.vb
' Uygulama ayarları penceresi
'
' Tab'lar: Bağlantı, OBD, Arayüz, Gelişmiş
' Değişiklikler anında uygulanır

Imports System.Windows.Forms
Imports System.Drawing
Imports CarCanReader1._0.Services
Imports CarCanReader1._0.Localization

Namespace Forms

    Public Class SettingsForm
        Inherits Form

#Region "Controls"

        Private tabControl As TabControl
        Private tabSerial As TabPage
        Private tabOBD As TabPage
        Private tabUI As TabPage
        Private tabAdvanced As TabPage

        Private btnSave As Button
        Private btnCancel As Button
        Private btnResetAll As Button
        Private lblStatus As Label

        ' Serial tab controls
        Private cmbBaudRate As ComboBox
        Private numReadTimeout As NumericUpDown
        Private numWriteTimeout As NumericUpDown
        Private numBufferSize As NumericUpDown
        Private chkAutoConnect As CheckBox

        ' OBD tab controls
        Private numRequestTimeout As NumericUpDown
        Private numRetryCount As NumericUpDown
        Private numPollingInterval As NumericUpDown
        Private chkAutoStartPolling As CheckBox
        Private chkEnableFreezeFrame As CheckBox
        Private chkEnablePendingDTC As CheckBox

        ' UI tab controls
        Private cmbTheme As ComboBox
        Private cmbLanguage As ComboBox
        Private numLogMaxLines As NumericUpDown
        Private numDashboardRefresh As NumericUpDown
        Private chkShowToolTips As CheckBox
        Private chkConfirmOnExit As CheckBox
        Private chkRememberPosition As CheckBox

        ' Advanced tab controls
        Private chkDebugMode As CheckBox
        Private chkLearningEngine As CheckBox
        Private chkAIFinder As CheckBox
        Private numMaxCANBuffer As NumericUpDown
        Private chkAutoEnrichment As CheckBox
        Private chkVINAutoProfile As CheckBox

#End Region

#Region "Fields"

        Private _isLoading As Boolean = False
        Private _hasChanges As Boolean = False

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
            LoadSettings()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "⚙️ " & LocalizationManager.T("settings.title")
            Me.Size = New Size(550, 500)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.MinimumSize = New Size(500, 450)
            Me.BackColor = Color.FromArgb(30, 30, 45)
            Me.ForeColor = Color.White
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ' TabControl
            tabControl = New TabControl()
            tabControl.Location = New Point(10, 10)
            tabControl.Size = New Size(515, 380)
            tabControl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom

            ' Tab pages
            tabSerial = New TabPage("🔌 Bağlantı")
            tabOBD = New TabPage("🚗 OBD-II")
            tabUI = New TabPage("🎨 Arayüz")
            tabAdvanced = New TabPage("⚡ Gelişmiş")

            ' Style tabs
            For Each tab As TabPage In {tabSerial, tabOBD, tabUI, tabAdvanced}
                tab.BackColor = Color.FromArgb(35, 35, 50)
                tab.ForeColor = Color.White
                tab.Padding = New Padding(10)
            Next

            tabControl.TabPages.AddRange({tabSerial, tabOBD, tabUI, tabAdvanced})
            Me.Controls.Add(tabControl)

            ' Initialize each tab
            InitializeSerialTab()
            InitializeOBDTab()
            InitializeUITab()
            InitializeAdvancedTab()

            ' Bottom buttons
            btnSave = CreateButton("💾 Kaydet", New Point(280, 405), AddressOf btnSave_Click)
            btnCancel = CreateButton("❌ İptal", New Point(370, 405), AddressOf btnCancel_Click)
            btnResetAll = CreateButton("🔄 Varsayılana Sıfırla", New Point(10, 405), AddressOf btnResetAll_Click)
            btnResetAll.Width = 140

            Me.Controls.AddRange({btnSave, btnCancel, btnResetAll})

            ' Status label
            lblStatus = New Label()
            lblStatus.Location = New Point(160, 410)
            lblStatus.Size = New Size(110, 20)
            lblStatus.ForeColor = Color.Gray
            lblStatus.TextAlign = ContentAlignment.MiddleCenter
            Me.Controls.Add(lblStatus)
        End Sub

#End Region

#Region "Tab Initialization"

        Private Sub InitializeSerialTab()
            Dim y = 20

            ' Baud Rate
            AddLabel(tabSerial, "Varsayılan Baud Rate:", 20, y)
            cmbBaudRate = New ComboBox()
            cmbBaudRate.Location = New Point(200, y)
            cmbBaudRate.Size = New Size(150, 25)
            cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList
            cmbBaudRate.Items.AddRange({"9600", "19200", "38400", "57600", "115200", "250000", "500000", "1000000"})
            cmbBaudRate.BackColor = Color.FromArgb(50, 50, 65)
            cmbBaudRate.ForeColor = Color.White
            AddHandler cmbBaudRate.SelectedIndexChanged, AddressOf OnSettingChanged
            tabSerial.Controls.Add(cmbBaudRate)
            y += 35

            ' Read Timeout
            AddLabel(tabSerial, "Okuma Zaman Aşımı (ms):", 20, y)
            numReadTimeout = CreateNumeric(tabSerial, 200, y, 100, 30000, 1000)
            y += 35

            ' Write Timeout
            AddLabel(tabSerial, "Yazma Zaman Aşımı (ms):", 20, y)
            numWriteTimeout = CreateNumeric(tabSerial, 200, y, 100, 30000, 1000)
            y += 35

            ' Buffer Size
            AddLabel(tabSerial, "Buffer Boyutu:", 20, y)
            numBufferSize = CreateNumeric(tabSerial, 200, y, 1024, 65536, 4096)
            y += 35

            ' Auto Connect
            chkAutoConnect = CreateCheckbox(tabSerial, "Başlangıçta otomatik bağlan", 20, y)
            y += 35

            ' Info
            AddInfoLabel(tabSerial, "💡 Çoğu CANable adaptör için 500000 baud rate önerilir.", 20, y + 20)
        End Sub

        Private Sub InitializeOBDTab()
            Dim y = 20

            ' Request Timeout
            AddLabel(tabOBD, "İstek Zaman Aşımı (ms):", 20, y)
            numRequestTimeout = CreateNumeric(tabOBD, 200, y, 500, 10000, 2000)
            y += 35

            ' Retry Count
            AddLabel(tabOBD, "Yeniden Deneme Sayısı:", 20, y)
            numRetryCount = CreateNumeric(tabOBD, 200, y, 0, 10, 3)
            y += 35

            ' Polling Interval
            AddLabel(tabOBD, "Polling Aralığı (ms):", 20, y)
            numPollingInterval = CreateNumeric(tabOBD, 200, y, 50, 5000, 250)
            y += 35

            ' Auto Start Polling
            chkAutoStartPolling = CreateCheckbox(tabOBD, "Bağlantıda otomatik polling başlat", 20, y)
            y += 30

            ' Enable Freeze Frame
            chkEnableFreezeFrame = CreateCheckbox(tabOBD, "Freeze Frame desteği", 20, y)
            y += 30

            ' Enable Pending DTC
            chkEnablePendingDTC = CreateCheckbox(tabOBD, "Bekleyen DTC desteği", 20, y)
            y += 35

            ' Info
            AddInfoLabel(tabOBD, "💡 Düşük polling aralığı daha hızlı güncelleme sağlar ama yoğunluk artırır.", 20, y + 20)
        End Sub

        Private Sub InitializeUITab()
            Dim y = 20

            ' Theme
            AddLabel(tabUI, "Tema:", 20, y)
            cmbTheme = New ComboBox()
            cmbTheme.Location = New Point(200, y)
            cmbTheme.Size = New Size(150, 25)
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList
            cmbTheme.Items.AddRange({"dark", "light"})
            cmbTheme.BackColor = Color.FromArgb(50, 50, 65)
            cmbTheme.ForeColor = Color.White
            AddHandler cmbTheme.SelectedIndexChanged, AddressOf OnSettingChanged
            tabUI.Controls.Add(cmbTheme)
            y += 35

            ' Language
            AddLabel(tabUI, LocalizationManager.T("settings.language") & ":", 20, y)
            cmbLanguage = New ComboBox()
            cmbLanguage.Location = New Point(200, y)
            cmbLanguage.Size = New Size(200, 25)
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList
            ' Bayrak ikonları ile dil listesi
            cmbLanguage.Items.AddRange({
                "🇹🇷 " & LocalizationManager.T("settings.turkish"),
                "🇬🇧 " & LocalizationManager.T("settings.english"),
                "🇩🇪 " & LocalizationManager.T("settings.german")
            })
            cmbLanguage.BackColor = Color.FromArgb(50, 50, 65)
            cmbLanguage.ForeColor = Color.White
            AddHandler cmbLanguage.SelectedIndexChanged, AddressOf OnLanguageChanged
            tabUI.Controls.Add(cmbLanguage)
            y += 35

            ' Restart uyarısı
            Dim lblRestartWarning As New Label()
            lblRestartWarning.Text = "⚠️ " & LocalizationManager.T("settings.restart_required")
            lblRestartWarning.Location = New Point(200, y)
            lblRestartWarning.Size = New Size(280, 40)
            lblRestartWarning.ForeColor = Color.Orange
            lblRestartWarning.Font = New Font(lblRestartWarning.Font, FontStyle.Italic)
            lblRestartWarning.Visible = False
            tabUI.Controls.Add(lblRestartWarning)
            y += 45

            ' Log Max Lines
            AddLabel(tabUI, "Maksimum Log Satırı:", 20, y)
            numLogMaxLines = CreateNumeric(tabUI, 200, y, 100, 50000, 5000)
            y += 35

            ' Dashboard Refresh
            AddLabel(tabUI, "Dashboard Yenileme (ms):", 20, y)
            numDashboardRefresh = CreateNumeric(tabUI, 200, y, 50, 1000, 100)
            y += 35

            ' Show ToolTips
            chkShowToolTips = CreateCheckbox(tabUI, "Araç ipuçlarını göster", 20, y)
            y += 30

            ' Confirm on Exit
            chkConfirmOnExit = CreateCheckbox(tabUI, "Çıkışta onay iste", 20, y)
            y += 30

            ' Remember Position
            chkRememberPosition = CreateCheckbox(tabUI, "Pencere konumunu hatırla", 20, y)
        End Sub

        Private Sub InitializeAdvancedTab()
            Dim y = 20

            ' Debug Mode
            chkDebugMode = CreateCheckbox(tabAdvanced, "Debug modu (detaylı loglama)", 20, y)
            y += 30

            ' Learning Engine
            chkLearningEngine = CreateCheckbox(tabAdvanced, "Learning Engine aktif", 20, y)
            y += 30

            ' AI Finder
            chkAIFinder = CreateCheckbox(tabAdvanced, "AI-Finder aktif (deneysel)", 20, y)
            y += 35

            ' Max CAN Buffer
            AddLabel(tabAdvanced, "Maksimum CAN Buffer:", 20, y)
            numMaxCANBuffer = CreateNumeric(tabAdvanced, 200, y, 1000, 100000, 10000)
            y += 35

            ' Auto Enrichment
            chkAutoEnrichment = CreateCheckbox(tabAdvanced, "Otomatik veritabanı zenginleştirme", 20, y)
            y += 30

            ' VIN Auto Profile
            chkVINAutoProfile = CreateCheckbox(tabAdvanced, "VIN ile otomatik profil yükleme", 20, y)
            y += 40

            ' Warning
            Dim lblWarning As New Label()
            lblWarning.Text = "⚠️ Gelişmiş ayarlar dikkatli kullanılmalıdır."
            lblWarning.Location = New Point(20, y)
            lblWarning.AutoSize = True
            lblWarning.ForeColor = Color.Orange
            tabAdvanced.Controls.Add(lblWarning)
        End Sub

#End Region

#Region "Helper Methods"

        Private Function CreateButton(text As String, location As Point, handler As EventHandler) As Button
            Dim btn As New Button()
            btn.Text = text
            btn.Location = location
            btn.Size = New Size(80, 30)
            btn.FlatStyle = FlatStyle.Flat
            btn.BackColor = Color.FromArgb(60, 60, 80)
            btn.ForeColor = Color.White
            AddHandler btn.Click, handler
            Return btn
        End Function

        Private Sub AddLabel(parent As Control, text As String, x As Integer, y As Integer)
            Dim lbl As New Label()
            lbl.Text = text
            lbl.Location = New Point(x, y + 3)
            lbl.AutoSize = True
            lbl.ForeColor = Color.White
            parent.Controls.Add(lbl)
        End Sub

        Private Sub AddInfoLabel(parent As Control, text As String, x As Integer, y As Integer)
            Dim lbl As New Label()
            lbl.Text = text
            lbl.Location = New Point(x, y)
            lbl.Size = New Size(450, 40)
            lbl.ForeColor = Color.Gray
            lbl.Font = New Font(lbl.Font.FontFamily, 8)
            parent.Controls.Add(lbl)
        End Sub

        Private Function CreateNumeric(parent As Control, x As Integer, y As Integer, min As Integer, max As Integer, defaultVal As Integer) As NumericUpDown
            Dim num As New NumericUpDown()
            num.Location = New Point(x, y)
            num.Size = New Size(100, 25)
            num.Minimum = min
            num.Maximum = max
            num.Value = defaultVal
            num.BackColor = Color.FromArgb(50, 50, 65)
            num.ForeColor = Color.White
            AddHandler num.ValueChanged, AddressOf OnSettingChanged
            parent.Controls.Add(num)
            Return num
        End Function

        Private Function CreateCheckbox(parent As Control, text As String, x As Integer, y As Integer) As CheckBox
            Dim chk As New CheckBox()
            chk.Text = text
            chk.Location = New Point(x, y)
            chk.AutoSize = True
            chk.ForeColor = Color.White
            AddHandler chk.CheckedChanged, AddressOf OnSettingChanged
            parent.Controls.Add(chk)
            Return chk
        End Function

#End Region

#Region "Load/Save Settings"

        Private Sub LoadSettings()
            _isLoading = True

            Try
                Dim cfg = ConfigManager.Instance

                ' Serial
                cmbBaudRate.SelectedItem = cfg.Serial.DefaultBaudRate.ToString()
                If cmbBaudRate.SelectedIndex < 0 Then cmbBaudRate.SelectedIndex = 6 ' 500000
                numReadTimeout.Value = cfg.Serial.ReadTimeout
                numWriteTimeout.Value = cfg.Serial.WriteTimeout
                numBufferSize.Value = cfg.Serial.BufferSize
                chkAutoConnect.Checked = cfg.Serial.AutoConnect

                ' OBD
                numRequestTimeout.Value = cfg.OBD.RequestTimeout
                numRetryCount.Value = cfg.OBD.RetryCount
                numPollingInterval.Value = cfg.OBD.PollingInterval
                chkAutoStartPolling.Checked = cfg.OBD.AutoStartPolling
                chkEnableFreezeFrame.Checked = cfg.OBD.EnableFreezeFrame
                chkEnablePendingDTC.Checked = cfg.OBD.EnablePendingDTC

                ' UI
                cmbTheme.SelectedItem = cfg.UI.Theme
                If cmbTheme.SelectedIndex < 0 Then cmbTheme.SelectedIndex = 0
                
                ' Dil seçimi - LocalizationManager'dan al
                Dim currentLang = LocalizationManager.Instance.CurrentLanguage
                Select Case currentLang
                    Case "tr"
                        cmbLanguage.SelectedIndex = 0
                    Case "en"
                        cmbLanguage.SelectedIndex = 1
                    Case "de"
                        cmbLanguage.SelectedIndex = 2
                    Case Else
                        cmbLanguage.SelectedIndex = 0
                End Select
                
                numLogMaxLines.Value = cfg.UI.LogMaxLines
                numDashboardRefresh.Value = cfg.UI.DashboardRefreshRate
                chkShowToolTips.Checked = cfg.UI.ShowToolTips
                chkConfirmOnExit.Checked = cfg.UI.ConfirmOnExit
                chkRememberPosition.Checked = cfg.UI.RememberWindowPosition

                ' Advanced
                chkDebugMode.Checked = cfg.Advanced.EnableDebugMode
                chkLearningEngine.Checked = cfg.Advanced.EnableLearningEngine
                chkAIFinder.Checked = cfg.Advanced.EnableAIFinder
                numMaxCANBuffer.Value = cfg.Advanced.MaxCANBufferSize
                chkAutoEnrichment.Checked = cfg.Advanced.EnableAutoEnrichment
                chkVINAutoProfile.Checked = cfg.Advanced.EnableVINAutoProfile

                _hasChanges = False
                UpdateStatus()

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SettingsForm.LoadSettings")
            Finally
                _isLoading = False
            End Try
        End Sub

        Private Sub SaveSettings()
            Try
                Dim cfg = ConfigManager.Instance

                ' Serial
                cfg.Serial.DefaultBaudRate = Integer.Parse(cmbBaudRate.SelectedItem.ToString())
                cfg.Serial.ReadTimeout = CInt(numReadTimeout.Value)
                cfg.Serial.WriteTimeout = CInt(numWriteTimeout.Value)
                cfg.Serial.BufferSize = CInt(numBufferSize.Value)
                cfg.Serial.AutoConnect = chkAutoConnect.Checked

                ' OBD
                cfg.OBD.RequestTimeout = CInt(numRequestTimeout.Value)
                cfg.OBD.RetryCount = CInt(numRetryCount.Value)
                cfg.OBD.PollingInterval = CInt(numPollingInterval.Value)
                cfg.OBD.AutoStartPolling = chkAutoStartPolling.Checked
                cfg.OBD.EnableFreezeFrame = chkEnableFreezeFrame.Checked
                cfg.OBD.EnablePendingDTC = chkEnablePendingDTC.Checked

                ' UI
                cfg.UI.Theme = cmbTheme.SelectedItem.ToString()
                
                ' Dil kaydetme
                Dim langCodes = {"tr", "en", "de"}
                Dim selectedLang = If(cmbLanguage.SelectedIndex >= 0 AndAlso cmbLanguage.SelectedIndex < langCodes.Length,
                                     langCodes(cmbLanguage.SelectedIndex), "tr")
                cfg.UI.Language = selectedLang
                
                ' LocalizationManager'a bildir
                If LocalizationManager.Instance.CurrentLanguage <> selectedLang Then
                    LocalizationManager.Instance.SetLanguage(selectedLang)
                End If
                
                cfg.UI.LogMaxLines = CInt(numLogMaxLines.Value)
                cfg.UI.DashboardRefreshRate = CInt(numDashboardRefresh.Value)
                cfg.UI.ShowToolTips = chkShowToolTips.Checked
                cfg.UI.ConfirmOnExit = chkConfirmOnExit.Checked
                cfg.UI.RememberWindowPosition = chkRememberPosition.Checked

                ' Advanced
                cfg.Advanced.EnableDebugMode = chkDebugMode.Checked
                cfg.Advanced.EnableLearningEngine = chkLearningEngine.Checked
                cfg.Advanced.EnableAIFinder = chkAIFinder.Checked
                cfg.Advanced.MaxCANBufferSize = CInt(numMaxCANBuffer.Value)
                cfg.Advanced.EnableAutoEnrichment = chkAutoEnrichment.Checked
                cfg.Advanced.EnableVINAutoProfile = chkVINAutoProfile.Checked

                ' Save
                If cfg.Save() Then
                    _hasChanges = False
                    lblStatus.Text = "✅ Kaydedildi"
                    lblStatus.ForeColor = Color.LightGreen
                Else
                    lblStatus.Text = "❌ Hata!"
                    lblStatus.ForeColor = Color.Red
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SettingsForm.SaveSettings")
                MessageBox.Show("Ayarlar kaydedilemedi: " & ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnSettingChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            _hasChanges = True
            UpdateStatus()
        End Sub

        Private Sub OnLanguageChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            _hasChanges = True
            UpdateStatus()
            
            ' Dil değiştiğinde uyarı göster
            Dim langCodes = {"tr", "en", "de"}
            If cmbLanguage.SelectedIndex >= 0 AndAlso cmbLanguage.SelectedIndex < langCodes.Length Then
                Dim selectedLang = langCodes(cmbLanguage.SelectedIndex)
                Dim currentLang = LocalizationManager.Instance.CurrentLanguage
                
                If selectedLang <> currentLang Then
                    ' Uyarı label'ını göster
                    Dim lblWarning = tabUI.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Text.Contains("⚠️"))
                    If lblWarning IsNot Nothing Then
                        lblWarning.Visible = True
                    End If
                End If
            End If
        End Sub

        Private Sub UpdateStatus()
            If _hasChanges Then
                lblStatus.Text = "● Değişiklik var"
                lblStatus.ForeColor = Color.Yellow
            Else
                lblStatus.Text = ""
            End If
        End Sub

        Private Sub btnSave_Click(sender As Object, e As EventArgs)
            SaveSettings()
        End Sub

        Private Sub btnCancel_Click(sender As Object, e As EventArgs)
            If _hasChanges Then
                Dim result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Çıkmak istediğinize emin misiniz?",
                                            "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.No Then Return
            End If
            Me.Close()
        End Sub

        Private Sub btnResetAll_Click(sender As Object, e As EventArgs)
            Dim result = MessageBox.Show("Tüm ayarlar varsayılan değerlere sıfırlanacak. Devam edilsin mi?",
                                        "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If result = DialogResult.Yes Then
                ConfigManager.Instance.ResetAllToDefaults()
                LoadSettings()
                lblStatus.Text = "🔄 Sıfırlandı"
                lblStatus.ForeColor = Color.Cyan
            End If
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            If _hasChanges AndAlso e.CloseReason = CloseReason.UserClosing Then
                Dim result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?",
                                            "Onay", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
                If result = DialogResult.Yes Then
                    SaveSettings()
                ElseIf result = DialogResult.Cancel Then
                    e.Cancel = True
                End If
            End If
            MyBase.OnFormClosing(e)
        End Sub

#End Region

    End Class

End Namespace

