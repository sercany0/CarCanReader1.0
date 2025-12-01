' ReportForm.vb
' PDF Rapor Oluşturma Formu
' Araç tanı raporları oluşturur, önizler ve kaydeder

Imports System.Windows.Forms
Imports System.Drawing
Imports System.IO
Imports System.Diagnostics
Imports CarCanReader1._0.Services
Imports CarCanReader1._0.Models

Namespace Forms

    Public Class ReportForm
        Inherits Form

#Region "UI Controls"

        Private tabControl As TabControl
        Private tabVehicleInfo As TabPage
        Private tabReportContent As TabPage
        Private tabCompanyInfo As TabPage
        Private tabPreview As TabPage

        ' Tab 1: Araç Bilgileri
        Private txtVIN As TextBox
        Private txtBrand As TextBox
        Private txtModel As TextBox
        Private txtYear As TextBox
        Private txtLicensePlate As TextBox
        Private txtMileage As TextBox
        Private txtTechnicianName As TextBox
        Private txtCustomerName As TextBox
        Private txtNotes As TextBox
        Private btnAutoFill As Button

        ' Tab 2: Rapor İçeriği
        Private chkIncludeEcuSummary As CheckBox
        Private chkIncludeDTCList As CheckBox
        Private chkIncludeLiveData As CheckBox
        Private chkIncludeCharts As CheckBox

        ' Tab 3: Firma Bilgileri
        Private picLogo As PictureBox
        Private btnLoadLogo As Button
        Private txtCompanyName As TextBox
        Private txtCompanyAddress As TextBox
        Private txtCompanyPhone As TextBox
        Private txtCompanyEmail As TextBox
        Private btnSaveCompanyInfo As Button

        ' Tab 4: Önizleme
        Private webBrowserPreview As WebBrowser
        Private lblPreviewPage As Label
        Private btnPrevPage As Button
        Private btnNextPage As Button

        ' Alt butonlar
        Private btnPreview As Button
        Private btnSavePDF As Button
        Private btnPrint As Button
        Private btnEmail As Button
        Private btnClose As Button

        Private pnlButtons As Panel

#End Region

#Region "Private Fields"

        Private _reportGenerator As ReportGenerator
        Private _reportData As ReportData
        Private _previewPdfPath As String = ""
        Private _currentPreviewPage As Integer = 1
        Private _totalPreviewPages As Integer = 1

#End Region

#Region "Constructor"

        Public Sub New(Optional vehicleInfo As Services.VehicleInfoModel = Nothing, Optional ecus As List(Of EcuInfo) = Nothing, Optional dtcs As List(Of DTCInfo) = Nothing, Optional liveData As Dictionary(Of String, Double) = Nothing)
            _reportGenerator = New ReportGenerator()
            _reportData = New ReportData()
            InitializeComponent()
            LoadCompanyInfo()
            LoadVehicleData(vehicleInfo, ecus, dtcs, liveData)
        End Sub

#End Region

#Region "Initialize Component"

        Private Sub InitializeComponent()
            Me.Text = "PDF Rapor Oluştur"
            Me.Size = New Size(900, 700)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.MinimumSize = New Size(800, 600)
            Me.BackColor = Color.FromArgb(25, 25, 35)
            Me.Font = New Font("Segoe UI", 9.0!)

            ' Tab Control
            CreateTabControl()

            ' Butonlar
            CreateButtons()

            ' Layout
            Me.Controls.Add(tabControl)
            Me.Controls.Add(pnlButtons)

        End Sub

        Private Sub CreateTabControl()
            tabControl = New TabControl()
            tabControl.Dock = DockStyle.Fill
            tabControl.Padding = New Point(10, 5)
            tabControl.Font = New Font("Segoe UI", 9.0!)

            ' Tab 1: Araç Bilgileri
            tabVehicleInfo = New TabPage("Araç Bilgileri")
            CreateVehicleInfoTab()
            tabControl.TabPages.Add(tabVehicleInfo)

            ' Tab 2: Rapor İçeriği
            tabReportContent = New TabPage("Rapor İçeriği")
            CreateReportContentTab()
            tabControl.TabPages.Add(tabReportContent)

            ' Tab 3: Firma Bilgileri
            tabCompanyInfo = New TabPage("Firma Bilgileri")
            CreateCompanyInfoTab()
            tabControl.TabPages.Add(tabCompanyInfo)

            ' Tab 4: Önizleme
            tabPreview = New TabPage("Önizleme")
            CreatePreviewTab()
            tabControl.TabPages.Add(tabPreview)
        End Sub

        Private Sub CreateVehicleInfoTab()
            tabVehicleInfo.BackColor = Color.FromArgb(25, 25, 35)
            tabVehicleInfo.Padding = New Padding(20)

            Dim yPos = 20

            ' VIN
            Dim lblVIN As New Label()
            lblVIN.Text = "VIN:"
            lblVIN.Location = New Point(20, yPos)
            lblVIN.Size = New Size(120, 25)
            lblVIN.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblVIN)

            txtVIN = New TextBox()
            txtVIN.Location = New Point(150, yPos)
            txtVIN.Size = New Size(300, 25)
            txtVIN.Font = New Font("Consolas", 10.0!)
            tabVehicleInfo.Controls.Add(txtVIN)

            btnAutoFill = New Button()
            btnAutoFill.Text = "Otomatik Doldur"
            btnAutoFill.Location = New Point(460, yPos)
            btnAutoFill.Size = New Size(120, 25)
            btnAutoFill.BackColor = Color.FromArgb(52, 152, 219)
            btnAutoFill.FlatStyle = FlatStyle.Flat
            btnAutoFill.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(btnAutoFill)
            yPos += 40

            ' Marka
            Dim lblBrand As New Label()
            lblBrand.Text = "Marka:"
            lblBrand.Location = New Point(20, yPos)
            lblBrand.Size = New Size(120, 25)
            lblBrand.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblBrand)

            txtBrand = New TextBox()
            txtBrand.Location = New Point(150, yPos)
            txtBrand.Size = New Size(200, 25)
            tabVehicleInfo.Controls.Add(txtBrand)
            yPos += 40

            ' Model
            Dim lblModel As New Label()
            lblModel.Text = "Model:"
            lblModel.Location = New Point(20, yPos)
            lblModel.Size = New Size(120, 25)
            lblModel.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblModel)

            txtModel = New TextBox()
            txtModel.Location = New Point(150, yPos)
            txtModel.Size = New Size(200, 25)
            tabVehicleInfo.Controls.Add(txtModel)
            yPos += 40

            ' Yıl
            Dim lblYear As New Label()
            lblYear.Text = "Yıl:"
            lblYear.Location = New Point(20, yPos)
            lblYear.Size = New Size(120, 25)
            lblYear.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblYear)

            txtYear = New TextBox()
            txtYear.Location = New Point(150, yPos)
            txtYear.Size = New Size(100, 25)
            tabVehicleInfo.Controls.Add(txtYear)
            yPos += 40

            ' Plaka
            Dim lblLicensePlate As New Label()
            lblLicensePlate.Text = "Plaka:"
            lblLicensePlate.Location = New Point(20, yPos)
            lblLicensePlate.Size = New Size(120, 25)
            lblLicensePlate.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblLicensePlate)

            txtLicensePlate = New TextBox()
            txtLicensePlate.Location = New Point(150, yPos)
            txtLicensePlate.Size = New Size(150, 25)
            tabVehicleInfo.Controls.Add(txtLicensePlate)
            yPos += 40

            ' Kilometre
            Dim lblMileage As New Label()
            lblMileage.Text = "Kilometre:"
            lblMileage.Location = New Point(20, yPos)
            lblMileage.Size = New Size(120, 25)
            lblMileage.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblMileage)

            txtMileage = New TextBox()
            txtMileage.Location = New Point(150, yPos)
            txtMileage.Size = New Size(150, 25)
            tabVehicleInfo.Controls.Add(txtMileage)
            yPos += 40

            ' Teknisyen Adı
            Dim lblTechnician As New Label()
            lblTechnician.Text = "Teknisyen Adı:"
            lblTechnician.Location = New Point(20, yPos)
            lblTechnician.Size = New Size(120, 25)
            lblTechnician.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblTechnician)

            txtTechnicianName = New TextBox()
            txtTechnicianName.Location = New Point(150, yPos)
            txtTechnicianName.Size = New Size(300, 25)
            tabVehicleInfo.Controls.Add(txtTechnicianName)
            yPos += 40

            ' Müşteri Adı
            Dim lblCustomer As New Label()
            lblCustomer.Text = "Müşteri Adı:"
            lblCustomer.Location = New Point(20, yPos)
            lblCustomer.Size = New Size(120, 25)
            lblCustomer.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblCustomer)

            txtCustomerName = New TextBox()
            txtCustomerName.Location = New Point(150, yPos)
            txtCustomerName.Size = New Size(300, 25)
            tabVehicleInfo.Controls.Add(txtCustomerName)
            yPos += 40

            ' Notlar
            Dim lblNotes As New Label()
            lblNotes.Text = "Notlar:"
            lblNotes.Location = New Point(20, yPos)
            lblNotes.Size = New Size(120, 25)
            lblNotes.ForeColor = Color.White
            tabVehicleInfo.Controls.Add(lblNotes)

            txtNotes = New TextBox()
            txtNotes.Location = New Point(150, yPos)
            txtNotes.Size = New Size(500, 100)
            txtNotes.Multiline = True
            txtNotes.ScrollBars = ScrollBars.Vertical
            tabVehicleInfo.Controls.Add(txtNotes)

            AddHandler btnAutoFill.Click, AddressOf BtnAutoFill_Click
        End Sub

        Private Sub CreateReportContentTab()
            tabReportContent.BackColor = Color.FromArgb(25, 25, 35)
            tabReportContent.Padding = New Padding(20)

            Dim yPos = 20

            chkIncludeEcuSummary = New CheckBox()
            chkIncludeEcuSummary.Text = "ECU Özeti Ekle"
            chkIncludeEcuSummary.Location = New Point(20, yPos)
            chkIncludeEcuSummary.Size = New Size(300, 30)
            chkIncludeEcuSummary.ForeColor = Color.White
            chkIncludeEcuSummary.Checked = True
            tabReportContent.Controls.Add(chkIncludeEcuSummary)
            yPos += 40

            chkIncludeDTCList = New CheckBox()
            chkIncludeDTCList.Text = "DTC Listesi Ekle"
            chkIncludeDTCList.Location = New Point(20, yPos)
            chkIncludeDTCList.Size = New Size(300, 30)
            chkIncludeDTCList.ForeColor = Color.White
            chkIncludeDTCList.Checked = True
            tabReportContent.Controls.Add(chkIncludeDTCList)
            yPos += 40

            chkIncludeLiveData = New CheckBox()
            chkIncludeLiveData.Text = "Canlı Veri Ekle"
            chkIncludeLiveData.Location = New Point(20, yPos)
            chkIncludeLiveData.Size = New Size(300, 30)
            chkIncludeLiveData.ForeColor = Color.White
            chkIncludeLiveData.Checked = True
            tabReportContent.Controls.Add(chkIncludeLiveData)
            yPos += 40

            chkIncludeCharts = New CheckBox()
            chkIncludeCharts.Text = "Grafikler Ekle (Yakında)"
            chkIncludeCharts.Location = New Point(20, yPos)
            chkIncludeCharts.Size = New Size(300, 30)
            chkIncludeCharts.ForeColor = Color.White
            chkIncludeCharts.Checked = False
            chkIncludeCharts.Enabled = False
            tabReportContent.Controls.Add(chkIncludeCharts)
        End Sub

        Private Sub CreateCompanyInfoTab()
            tabCompanyInfo.BackColor = Color.FromArgb(25, 25, 35)
            tabCompanyInfo.Padding = New Padding(20)

            Dim yPos = 20

            ' Logo
            Dim lblLogo As New Label()
            lblLogo.Text = "Logo:"
            lblLogo.Location = New Point(20, yPos)
            lblLogo.Size = New Size(120, 25)
            lblLogo.ForeColor = Color.White
            tabCompanyInfo.Controls.Add(lblLogo)

            picLogo = New PictureBox()
            picLogo.Location = New Point(150, yPos)
            picLogo.Size = New Size(200, 100)
            picLogo.BorderStyle = BorderStyle.FixedSingle
            picLogo.BackColor = Color.White
            picLogo.SizeMode = PictureBoxSizeMode.Zoom
            tabCompanyInfo.Controls.Add(picLogo)

            btnLoadLogo = New Button()
            btnLoadLogo.Text = "Logo Yükle"
            btnLoadLogo.Location = New Point(360, yPos)
            btnLoadLogo.Size = New Size(120, 30)
            btnLoadLogo.BackColor = Color.FromArgb(52, 152, 219)
            btnLoadLogo.FlatStyle = FlatStyle.Flat
            btnLoadLogo.ForeColor = Color.White
            tabCompanyInfo.Controls.Add(btnLoadLogo)
            yPos += 120

            ' Firma Adı
            Dim lblCompanyName As New Label()
            lblCompanyName.Text = "Firma Adı:"
            lblCompanyName.Location = New Point(20, yPos)
            lblCompanyName.Size = New Size(120, 25)
            lblCompanyName.ForeColor = Color.White
            tabCompanyInfo.Controls.Add(lblCompanyName)

            txtCompanyName = New TextBox()
            txtCompanyName.Location = New Point(150, yPos)
            txtCompanyName.Size = New Size(400, 25)
            tabCompanyInfo.Controls.Add(txtCompanyName)
            yPos += 40

            ' Adres
            Dim lblAddress As New Label()
            lblAddress.Text = "Adres:"
            lblAddress.Location = New Point(20, yPos)
            lblAddress.Size = New Size(120, 25)
            lblAddress.ForeColor = Color.White
            tabCompanyInfo.Controls.Add(lblAddress)

            txtCompanyAddress = New TextBox()
            txtCompanyAddress.Location = New Point(150, yPos)
            txtCompanyAddress.Size = New Size(400, 60)
            txtCompanyAddress.Multiline = True
            tabCompanyInfo.Controls.Add(txtCompanyAddress)
            yPos += 80

            ' Telefon
            Dim lblPhone As New Label()
            lblPhone.Text = "Telefon:"
            lblPhone.Location = New Point(20, yPos)
            lblPhone.Size = New Size(120, 25)
            lblPhone.ForeColor = Color.White
            tabCompanyInfo.Controls.Add(lblPhone)

            txtCompanyPhone = New TextBox()
            txtCompanyPhone.Location = New Point(150, yPos)
            txtCompanyPhone.Size = New Size(200, 25)
            tabCompanyInfo.Controls.Add(txtCompanyPhone)
            yPos += 40

            ' E-posta
            Dim lblEmail As New Label()
            lblEmail.Text = "E-posta:"
            lblEmail.Location = New Point(20, yPos)
            lblEmail.Size = New Size(120, 25)
            lblEmail.ForeColor = Color.White
            tabCompanyInfo.Controls.Add(lblEmail)

            txtCompanyEmail = New TextBox()
            txtCompanyEmail.Location = New Point(150, yPos)
            txtCompanyEmail.Size = New Size(300, 25)
            tabCompanyInfo.Controls.Add(txtCompanyEmail)
            yPos += 50

            ' Kaydet Butonu
            btnSaveCompanyInfo = New Button()
            btnSaveCompanyInfo.Text = "💾 Firma Bilgilerini Kaydet"
            btnSaveCompanyInfo.Location = New Point(150, yPos)
            btnSaveCompanyInfo.Size = New Size(200, 35)
            btnSaveCompanyInfo.BackColor = Color.FromArgb(46, 204, 113)
            btnSaveCompanyInfo.FlatStyle = FlatStyle.Flat
            btnSaveCompanyInfo.ForeColor = Color.White
            btnSaveCompanyInfo.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            tabCompanyInfo.Controls.Add(btnSaveCompanyInfo)

            AddHandler btnLoadLogo.Click, AddressOf BtnLoadLogo_Click
            AddHandler btnSaveCompanyInfo.Click, AddressOf BtnSaveCompanyInfo_Click
        End Sub

        Private Sub CreatePreviewTab()
            tabPreview.BackColor = Color.FromArgb(25, 25, 35)
            tabPreview.Padding = New Padding(10)

            ' Önizleme kontrolleri
            Dim pnlPreviewControls As New Panel()
            pnlPreviewControls.Dock = DockStyle.Top
            pnlPreviewControls.Height = 40
            pnlPreviewControls.BackColor = Color.FromArgb(35, 35, 50)
            tabPreview.Controls.Add(pnlPreviewControls)

            btnPrevPage = New Button()
            btnPrevPage.Text = "◀ Önceki"
            btnPrevPage.Location = New Point(10, 5)
            btnPrevPage.Size = New Size(100, 30)
            btnPrevPage.BackColor = Color.FromArgb(52, 152, 219)
            btnPrevPage.FlatStyle = FlatStyle.Flat
            btnPrevPage.ForeColor = Color.White
            btnPrevPage.Enabled = False
            pnlPreviewControls.Controls.Add(btnPrevPage)

            lblPreviewPage = New Label()
            lblPreviewPage.Text = "Sayfa 1 / 1"
            lblPreviewPage.Location = New Point(120, 10)
            lblPreviewPage.Size = New Size(150, 25)
            lblPreviewPage.ForeColor = Color.White
            lblPreviewPage.TextAlign = ContentAlignment.MiddleCenter
            pnlPreviewControls.Controls.Add(lblPreviewPage)

            btnNextPage = New Button()
            btnNextPage.Text = "Sonraki ▶"
            btnNextPage.Location = New Point(280, 5)
            btnNextPage.Size = New Size(100, 30)
            btnNextPage.BackColor = Color.FromArgb(52, 152, 219)
            btnNextPage.FlatStyle = FlatStyle.Flat
            btnNextPage.ForeColor = Color.White
            btnNextPage.Enabled = False
            pnlPreviewControls.Controls.Add(btnNextPage)

            ' WebBrowser önizleme
            webBrowserPreview = New WebBrowser()
            webBrowserPreview.Dock = DockStyle.Fill
            webBrowserPreview.IsWebBrowserContextMenuEnabled = False
            tabPreview.Controls.Add(webBrowserPreview)

            AddHandler btnPrevPage.Click, AddressOf BtnPrevPage_Click
            AddHandler btnNextPage.Click, AddressOf BtnNextPage_Click
        End Sub

        Private Sub CreateButtons()
            pnlButtons = New Panel()
            pnlButtons.Dock = DockStyle.Bottom
            pnlButtons.Height = 60
            pnlButtons.BackColor = Color.FromArgb(35, 35, 50)
            pnlButtons.Padding = New Padding(10, 10, 10, 10)

            btnPreview = New Button()
            btnPreview.Text = "👁️ Önizle"
            btnPreview.Size = New Size(120, 40)
            btnPreview.Location = New Point(10, 10)
            btnPreview.BackColor = Color.FromArgb(52, 152, 219)
            btnPreview.FlatStyle = FlatStyle.Flat
            btnPreview.ForeColor = Color.White
            btnPreview.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlButtons.Controls.Add(btnPreview)

            btnSavePDF = New Button()
            btnSavePDF.Text = "💾 PDF Kaydet"
            btnSavePDF.Size = New Size(120, 40)
            btnSavePDF.Location = New Point(140, 10)
            btnSavePDF.BackColor = Color.FromArgb(46, 204, 113)
            btnSavePDF.FlatStyle = FlatStyle.Flat
            btnSavePDF.ForeColor = Color.White
            btnSavePDF.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlButtons.Controls.Add(btnSavePDF)

            btnPrint = New Button()
            btnPrint.Text = "🖨️ Yazdır"
            btnPrint.Size = New Size(120, 40)
            btnPrint.Location = New Point(270, 10)
            btnPrint.BackColor = Color.FromArgb(230, 126, 34)
            btnPrint.FlatStyle = FlatStyle.Flat
            btnPrint.ForeColor = Color.White
            btnPrint.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlButtons.Controls.Add(btnPrint)

            btnEmail = New Button()
            btnEmail.Text = "📧 E-posta Gönder"
            btnEmail.Size = New Size(140, 40)
            btnEmail.Location = New Point(400, 10)
            btnEmail.BackColor = Color.FromArgb(155, 89, 182)
            btnEmail.FlatStyle = FlatStyle.Flat
            btnEmail.ForeColor = Color.White
            btnEmail.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlButtons.Controls.Add(btnEmail)

            btnClose = New Button()
            btnClose.Text = "❌ Kapat"
            btnClose.Size = New Size(100, 40)
            btnClose.Location = New Point(750, 10)
            btnClose.BackColor = Color.FromArgb(149, 165, 166)
            btnClose.FlatStyle = FlatStyle.Flat
            btnClose.ForeColor = Color.White
            btnClose.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlButtons.Controls.Add(btnClose)

            AddHandler btnPreview.Click, AddressOf BtnPreview_Click
            AddHandler btnSavePDF.Click, AddressOf BtnSavePDF_Click
            AddHandler btnPrint.Click, AddressOf BtnPrint_Click
            AddHandler btnEmail.Click, AddressOf BtnEmail_Click
            AddHandler btnClose.Click, AddressOf BtnClose_Click
        End Sub

#End Region

#Region "Data Loading"

        Private Sub LoadCompanyInfo()
            Try
                Dim config = ConfigManager.Instance.Company
                txtCompanyName.Text = config.CompanyName
                txtCompanyAddress.Text = config.CompanyAddress
                txtCompanyPhone.Text = config.CompanyPhone
                txtCompanyEmail.Text = config.CompanyEmail

                _reportGenerator.CompanyName = config.CompanyName
                _reportGenerator.CompanyAddress = config.CompanyAddress
                _reportGenerator.CompanyPhone = config.CompanyPhone
                _reportGenerator.CompanyEmail = config.CompanyEmail

                ' Logo yükle
                If Not String.IsNullOrEmpty(config.CompanyLogoPath) AndAlso File.Exists(config.CompanyLogoPath) Then
                    Try
                        picLogo.Image = Image.FromFile(config.CompanyLogoPath)
                        _reportGenerator.CompanyLogo = picLogo.Image
                    Catch
                        ' Logo yüklenemedi
                    End Try
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.LoadCompanyInfo")
            End Try
        End Sub

        Private Sub LoadVehicleData(vehicleInfo As Services.VehicleInfoModel, ecus As List(Of EcuInfo), dtcs As List(Of DTCInfo), liveData As Dictionary(Of String, Double))
            Try
                If vehicleInfo IsNot Nothing Then
                    txtVIN.Text = vehicleInfo.VIN
                    _reportData.VehicleInfo = vehicleInfo
                End If

                If ecus IsNot Nothing Then
                    _reportData.ScannedEcus = ecus
                End If

                If dtcs IsNot Nothing Then
                    _reportData.FoundDTCs = dtcs
                End If

                If liveData IsNot Nothing Then
                    _reportData.LiveDataSnapshot = liveData
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.LoadVehicleData")
            End Try
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub BtnAutoFill_Click(sender As Object, e As EventArgs)
            Try
                ' OBDService'den araç bilgilerini al (MainForm'dan)
                ' Bu metod MainForm'dan çağrıldığında veri aktarılacak
                ' Şimdilik sadece bilgi mesajı
                MessageBox.Show("Otomatik doldurma özelliği MainForm'dan veri alındığında çalışacak.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnAutoFill_Click")
            End Try
        End Sub

        Private Sub BtnLoadLogo_Click(sender As Object, e As EventArgs)
            Try
                Dim ofd As New OpenFileDialog()
                ofd.Filter = "Resim Dosyaları|*.png;*.jpg;*.jpeg;*.bmp|Tüm Dosyalar|*.*"
                ofd.Title = "Logo Seç"

                If ofd.ShowDialog() = DialogResult.OK Then
                    picLogo.Image = Image.FromFile(ofd.FileName)
                    _reportGenerator.CompanyLogo = picLogo.Image
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnLoadLogo_Click")
                MessageBox.Show($"Logo yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnSaveCompanyInfo_Click(sender As Object, e As EventArgs)
            Try
                Dim config = ConfigManager.Instance.Company
                config.CompanyName = txtCompanyName.Text
                config.CompanyAddress = txtCompanyAddress.Text
                config.CompanyPhone = txtCompanyPhone.Text
                config.CompanyEmail = txtCompanyEmail.Text

                If picLogo.Image IsNot Nothing Then
                    ' Logoyu kaydet
                    Dim logoPath = Path.Combine(Application.StartupPath, "data", "company_logo.png")
                    If Not Directory.Exists(Path.GetDirectoryName(logoPath)) Then
                        Directory.CreateDirectory(Path.GetDirectoryName(logoPath))
                    End If
                    picLogo.Image.Save(logoPath, Imaging.ImageFormat.Png)
                    config.CompanyLogoPath = logoPath
                End If

                ConfigManager.Instance.Save()
                _reportGenerator.CompanyName = config.CompanyName
                _reportGenerator.CompanyAddress = config.CompanyAddress
                _reportGenerator.CompanyPhone = config.CompanyPhone
                _reportGenerator.CompanyEmail = config.CompanyEmail

                MessageBox.Show("Firma bilgileri kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnSaveCompanyInfo_Click")
                MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnPreview_Click(sender As Object, e As EventArgs)
            Try
                ' Rapor verilerini topla
                CollectReportData()

                ' Geçici PDF oluştur
                _previewPdfPath = Path.Combine(Path.GetTempPath(), $"report_preview_{Guid.NewGuid()}.pdf")
                If _reportGenerator.GenerateReport(_reportData, _previewPdfPath) Then
                    ' PDF'i WebBrowser'da göster
                    webBrowserPreview.Navigate(_previewPdfPath)
                    tabControl.SelectedTab = tabPreview
                    UpdatePreviewNavigation()
                Else
                    MessageBox.Show("Rapor oluşturulurken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnPreview_Click")
                MessageBox.Show($"Önizleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnSavePDF_Click(sender As Object, e As EventArgs)
            Try
                ' Rapor verilerini topla
                CollectReportData()

                Dim sfd As New SaveFileDialog()
                sfd.Filter = "PDF Dosyaları|*.pdf|Tüm Dosyalar|*.*"
                sfd.FileName = $"Arac_Tani_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                sfd.Title = "PDF Kaydet"

                If sfd.ShowDialog() = DialogResult.OK Then
                    If _reportGenerator.GenerateReport(_reportData, sfd.FileName) Then
                        MessageBox.Show($"PDF başarıyla kaydedildi: {sfd.FileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Else
                        MessageBox.Show("PDF oluşturulurken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnSavePDF_Click")
                MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnPrint_Click(sender As Object, e As EventArgs)
            Try
                If String.IsNullOrEmpty(_previewPdfPath) OrElse Not File.Exists(_previewPdfPath) Then
                    ' Önce önizleme oluştur
                    BtnPreview_Click(sender, e)
                    If String.IsNullOrEmpty(_previewPdfPath) Then
                        Return
                    End If
                End If

                ' PDF'i yazdır
                Process.Start(_previewPdfPath)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnPrint_Click")
                MessageBox.Show($"Yazdırma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnEmail_Click(sender As Object, e As EventArgs)
            Try
                If String.IsNullOrEmpty(_previewPdfPath) OrElse Not File.Exists(_previewPdfPath) Then
                    ' Önce önizleme oluştur
                    BtnPreview_Click(sender, e)
                    If String.IsNullOrEmpty(_previewPdfPath) Then
                        Return
                    End If
                End If

                ' E-posta gönder (varsayılan mail client)
                Dim mailto = $"mailto:?subject=Araç Tanı Raporu&body=Ekli dosyada araç tanı raporu bulunmaktadır.&attachment={_previewPdfPath}"
                Process.Start(mailto)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.BtnEmail_Click")
                MessageBox.Show($"E-posta gönderme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnClose_Click(sender As Object, e As EventArgs)
            Me.Close()
        End Sub

        Private Sub BtnPrevPage_Click(sender As Object, e As EventArgs)
            ' PDF sayfa navigasyonu (basit implementasyon)
            ' iTextSharp ile sayfa sayısı alınabilir
        End Sub

        Private Sub BtnNextPage_Click(sender As Object, e As EventArgs)
            ' PDF sayfa navigasyonu (basit implementasyon)
        End Sub

        Private Sub UpdatePreviewNavigation()
            ' Sayfa navigasyonunu güncelle
            lblPreviewPage.Text = $"Sayfa {_currentPreviewPage} / {_totalPreviewPages}"
            btnPrevPage.Enabled = (_currentPreviewPage > 1)
            btnNextPage.Enabled = (_currentPreviewPage < _totalPreviewPages)
        End Sub

#End Region

#Region "Helper Methods"

        Private Sub CollectReportData()
            Try
                ' Araç bilgileri
                If _reportData.VehicleInfo Is Nothing Then
                    _reportData.VehicleInfo = New Services.VehicleInfoModel()
                End If
                If Not String.IsNullOrEmpty(txtVIN.Text) Then
                    _reportData.VehicleInfo.VIN = txtVIN.Text
                End If

                ' Müşteri bilgileri
                _reportData.CustomerLicensePlate = txtLicensePlate.Text
                If Not String.IsNullOrEmpty(txtMileage.Text) Then
                    Integer.TryParse(txtMileage.Text, _reportData.CustomerMileage)
                End If

                ' Teknisyen ve müşteri bilgileri
                _reportData.TechnicianName = txtTechnicianName.Text
                _reportData.CustomerName = txtCustomerName.Text
                _reportData.Notes = txtNotes.Text

                ' Rapor içerik seçenekleri
                _reportData.IncludeEcuSummary = chkIncludeEcuSummary.Checked
                _reportData.IncludeDTCList = chkIncludeDTCList.Checked
                _reportData.IncludeLiveData = chkIncludeLiveData.Checked
                _reportData.IncludeCharts = chkIncludeCharts.Checked

                ' Firma bilgileri
                _reportGenerator.CompanyName = txtCompanyName.Text
                _reportGenerator.CompanyAddress = txtCompanyAddress.Text
                _reportGenerator.CompanyPhone = txtCompanyPhone.Text
                _reportGenerator.CompanyEmail = txtCompanyEmail.Text
                If picLogo.Image IsNot Nothing Then
                    _reportGenerator.CompanyLogo = picLogo.Image
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportForm.CollectReportData")
            End Try
        End Sub

#End Region

    End Class

End Namespace
