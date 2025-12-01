' EcuScannerForm.vb
' ECU Tarama ve Yönetim Formu
' Araçtaki tüm ECU'ları tarar ve detaylarını gösterir

Imports System.Windows.Forms
Imports System.Drawing
Imports System.Threading.Tasks
Imports CarCanReader1._0.Services
Imports CarCanReader1._0.Models

Namespace Forms

    Public Class EcuScannerForm
        Inherits Form

#Region "UI Controls"

        Private pnlToolbar As Panel
        Private btnScan As Button
        Private btnQuickScan As Button
        Private btnStop As Button
        Private progressBar As ProgressBar
        Private lblEcuCount As Label
        Private lblStatus As Label

        Private splitContainer As SplitContainer
        Private treeViewEcus As TreeView
        Private pnlDetails As Panel
        Private lblEcuName As Label
        Private lblEcuAddress As Label
        Private lblEcuType As Label
        Private lblSoftwareVersion As Label
        Private lblHardwareVersion As Label
        Private lblPartNumber As Label
        Private lblSerialNumber As Label
        Private lblDtcCount As Label
        Private btnReadDTCs As Button
        Private btnClearDTCs As Button
        Private btnRefresh As Button
        Private btnClose As Button

        Private grpEcuInfo As GroupBox
        Private grpActions As GroupBox

#End Region

#Region "Private Fields"

        Private _ecuScanner As EcuScanner
        Private _canSender As CANSender
        Private _udsEngine As AdvancedUdsEngine
        Private _isScanning As Boolean = False
        Private _cancellationTokenSource As Threading.CancellationTokenSource
        Private _ecuNodes As New Dictionary(Of EcuInfo, TreeNode)()

#End Region

#Region "Constructor"

        Public Sub New(canSender As CANSender, udsEngine As AdvancedUdsEngine)
            _canSender = canSender
            _udsEngine = udsEngine
            _ecuScanner = New EcuScanner(canSender, udsEngine)
            InitializeComponent()
            SetupEventHandlers()
        End Sub

#End Region

#Region "Initialize Component"

        Private Sub InitializeComponent()
            Me.Text = "ECU Tarama ve Yönetim"
            Me.Size = New Size(1000, 700)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.MinimumSize = New Size(800, 600)
            Me.BackColor = Color.FromArgb(25, 25, 35)
            Me.Font = New Font("Segoe UI", 9.0!)

            ' Toolbar
            CreateToolbar()

            ' Split Container
            splitContainer = New SplitContainer()
            splitContainer.Dock = DockStyle.Fill
            splitContainer.SplitterDistance = 350
            splitContainer.Panel1MinSize = 200
            splitContainer.Panel2MinSize = 300
            splitContainer.BackColor = Color.FromArgb(25, 25, 35)

            ' TreeView (Sol Panel)
            CreateTreeView()

            ' Details Panel (Sağ Panel)
            CreateDetailsPanel()

            ' Layout
            Me.Controls.Add(splitContainer)
            Me.Controls.Add(pnlToolbar)

            ' Başlangıç durumu
            UpdateUIState(False)
        End Sub

        Private Sub CreateToolbar()
            pnlToolbar = New Panel()
            pnlToolbar.Dock = DockStyle.Top
            pnlToolbar.Height = 60
            pnlToolbar.BackColor = Color.FromArgb(35, 35, 50)
            pnlToolbar.Padding = New Padding(10, 5, 10, 5)

            ' Tara Butonu
            btnScan = New Button()
            btnScan.Text = "🔍 Tam Tara"
            btnScan.Size = New Size(120, 35)
            btnScan.Location = New Point(10, 12)
            btnScan.BackColor = Color.FromArgb(46, 204, 113)
            btnScan.FlatStyle = FlatStyle.Flat
            btnScan.ForeColor = Color.White
            btnScan.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlToolbar.Controls.Add(btnScan)

            ' Hızlı Tarama Butonu
            btnQuickScan = New Button()
            btnQuickScan.Text = "⚡ Hızlı Tara"
            btnQuickScan.Size = New Size(120, 35)
            btnQuickScan.Location = New Point(140, 12)
            btnQuickScan.BackColor = Color.FromArgb(52, 152, 219)
            btnQuickScan.FlatStyle = FlatStyle.Flat
            btnQuickScan.ForeColor = Color.White
            btnQuickScan.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlToolbar.Controls.Add(btnQuickScan)

            ' Durdur Butonu
            btnStop = New Button()
            btnStop.Text = "⏹ Durdur"
            btnStop.Size = New Size(100, 35)
            btnStop.Location = New Point(270, 12)
            btnStop.BackColor = Color.FromArgb(231, 76, 60)
            btnStop.FlatStyle = FlatStyle.Flat
            btnStop.ForeColor = Color.White
            btnStop.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            btnStop.Enabled = False
            pnlToolbar.Controls.Add(btnStop)

            ' Progress Bar
            progressBar = New ProgressBar()
            progressBar.Size = New Size(200, 25)
            progressBar.Location = New Point(380, 17)
            progressBar.Style = ProgressBarStyle.Continuous
            pnlToolbar.Controls.Add(progressBar)

            ' ECU Sayacı
            lblEcuCount = New Label()
            lblEcuCount.Text = "0 ECU bulundu"
            lblEcuCount.Size = New Size(150, 25)
            lblEcuCount.Location = New Point(590, 17)
            lblEcuCount.ForeColor = Color.White
            lblEcuCount.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlToolbar.Controls.Add(lblEcuCount)

            ' Status Label
            lblStatus = New Label()
            lblStatus.Text = "Hazır"
            lblStatus.Size = New Size(200, 25)
            lblStatus.Location = New Point(750, 17)
            lblStatus.ForeColor = Color.FromArgb(150, 150, 170)
            lblStatus.Font = New Font("Segoe UI", 9.0!)
            pnlToolbar.Controls.Add(lblStatus)
        End Sub

        Private Sub CreateTreeView()
            treeViewEcus = New TreeView()
            treeViewEcus.Dock = DockStyle.Fill
            treeViewEcus.BackColor = Color.FromArgb(30, 30, 40)
            treeViewEcus.ForeColor = Color.White
            treeViewEcus.Font = New Font("Segoe UI", 9.0!)
            treeViewEcus.BorderStyle = BorderStyle.None
            treeViewEcus.HideSelection = False
            splitContainer.Panel1.Controls.Add(treeViewEcus)
        End Sub

        Private Sub CreateDetailsPanel()
            pnlDetails = New Panel()
            pnlDetails.Dock = DockStyle.Fill
            pnlDetails.BackColor = Color.FromArgb(25, 25, 35)
            pnlDetails.AutoScroll = True
            pnlDetails.Padding = New Padding(20)

            ' ECU Info GroupBox
            grpEcuInfo = New GroupBox()
            grpEcuInfo.Text = "ECU Bilgileri"
            grpEcuInfo.Size = New Size(600, 350)
            grpEcuInfo.Location = New Point(20, 20)
            grpEcuInfo.ForeColor = Color.White
            grpEcuInfo.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
            grpEcuInfo.BackColor = Color.FromArgb(35, 35, 50)
            pnlDetails.Controls.Add(grpEcuInfo)

            ' ECU Name
            Dim lblNameLabel As New Label()
            lblNameLabel.Text = "ECU Adı:"
            lblNameLabel.Location = New Point(15, 30)
            lblNameLabel.Size = New Size(120, 25)
            lblNameLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblNameLabel)

            lblEcuName = New Label()
            lblEcuName.Text = "-"
            lblEcuName.Location = New Point(140, 30)
            lblEcuName.Size = New Size(400, 25)
            lblEcuName.ForeColor = Color.White
            lblEcuName.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
            grpEcuInfo.Controls.Add(lblEcuName)

            ' ECU Address
            Dim lblAddressLabel As New Label()
            lblAddressLabel.Text = "Adres:"
            lblAddressLabel.Location = New Point(15, 65)
            lblAddressLabel.Size = New Size(120, 25)
            lblAddressLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblAddressLabel)

            lblEcuAddress = New Label()
            lblEcuAddress.Text = "-"
            lblEcuAddress.Location = New Point(140, 65)
            lblEcuAddress.Size = New Size(200, 25)
            lblEcuAddress.ForeColor = Color.White
            lblEcuAddress.Font = New Font("Consolas", 10.0!)
            grpEcuInfo.Controls.Add(lblEcuAddress)

            ' ECU Type
            Dim lblTypeLabel As New Label()
            lblTypeLabel.Text = "Tip:"
            lblTypeLabel.Location = New Point(15, 100)
            lblTypeLabel.Size = New Size(120, 25)
            lblTypeLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblTypeLabel)

            lblEcuType = New Label()
            lblEcuType.Text = "-"
            lblEcuType.Location = New Point(140, 100)
            lblEcuType.Size = New Size(200, 25)
            lblEcuType.ForeColor = Color.White
            grpEcuInfo.Controls.Add(lblEcuType)

            ' Software Version
            Dim lblSwLabel As New Label()
            lblSwLabel.Text = "Yazılım Versiyonu:"
            lblSwLabel.Location = New Point(15, 135)
            lblSwLabel.Size = New Size(120, 25)
            lblSwLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblSwLabel)

            lblSoftwareVersion = New Label()
            lblSoftwareVersion.Text = "-"
            lblSoftwareVersion.Location = New Point(140, 135)
            lblSoftwareVersion.Size = New Size(400, 25)
            lblSoftwareVersion.ForeColor = Color.White
            grpEcuInfo.Controls.Add(lblSoftwareVersion)

            ' Hardware Version
            Dim lblHwLabel As New Label()
            lblHwLabel.Text = "Donanım Versiyonu:"
            lblHwLabel.Location = New Point(15, 170)
            lblHwLabel.Size = New Size(120, 25)
            lblHwLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblHwLabel)

            lblHardwareVersion = New Label()
            lblHardwareVersion.Text = "-"
            lblHardwareVersion.Location = New Point(140, 170)
            lblHardwareVersion.Size = New Size(400, 25)
            lblHardwareVersion.ForeColor = Color.White
            grpEcuInfo.Controls.Add(lblHardwareVersion)

            ' Part Number
            Dim lblPartLabel As New Label()
            lblPartLabel.Text = "Part Number:"
            lblPartLabel.Location = New Point(15, 205)
            lblPartLabel.Size = New Size(120, 25)
            lblPartLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblPartLabel)

            lblPartNumber = New Label()
            lblPartNumber.Text = "-"
            lblPartNumber.Location = New Point(140, 205)
            lblPartNumber.Size = New Size(400, 25)
            lblPartNumber.ForeColor = Color.White
            lblPartNumber.Font = New Font("Consolas", 9.0!)
            grpEcuInfo.Controls.Add(lblPartNumber)

            ' Serial Number
            Dim lblSerialLabel As New Label()
            lblSerialLabel.Text = "Seri Numarası:"
            lblSerialLabel.Location = New Point(15, 240)
            lblSerialLabel.Size = New Size(120, 25)
            lblSerialLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblSerialLabel)

            lblSerialNumber = New Label()
            lblSerialNumber.Text = "-"
            lblSerialNumber.Location = New Point(140, 240)
            lblSerialNumber.Size = New Size(400, 25)
            lblSerialNumber.ForeColor = Color.White
            lblSerialNumber.Font = New Font("Consolas", 9.0!)
            grpEcuInfo.Controls.Add(lblSerialNumber)

            ' DTC Count
            Dim lblDtcLabel As New Label()
            lblDtcLabel.Text = "DTC Sayısı:"
            lblDtcLabel.Location = New Point(15, 275)
            lblDtcLabel.Size = New Size(120, 25)
            lblDtcLabel.ForeColor = Color.FromArgb(150, 150, 170)
            grpEcuInfo.Controls.Add(lblDtcLabel)

            lblDtcCount = New Label()
            lblDtcCount.Text = "0"
            lblDtcCount.Location = New Point(140, 275)
            lblDtcCount.Size = New Size(200, 25)
            lblDtcCount.ForeColor = Color.FromArgb(231, 76, 60)
            lblDtcCount.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
            grpEcuInfo.Controls.Add(lblDtcCount)

            ' Actions GroupBox
            grpActions = New GroupBox()
            grpActions.Text = "İşlemler"
            grpActions.Size = New Size(600, 120)
            grpActions.Location = New Point(20, 390)
            grpActions.ForeColor = Color.White
            grpActions.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
            grpActions.BackColor = Color.FromArgb(35, 35, 50)
            pnlDetails.Controls.Add(grpActions)

            ' Read DTCs Button
            btnReadDTCs = New Button()
            btnReadDTCs.Text = "📋 DTC Oku"
            btnReadDTCs.Size = New Size(140, 40)
            btnReadDTCs.Location = New Point(15, 30)
            btnReadDTCs.BackColor = Color.FromArgb(52, 152, 219)
            btnReadDTCs.FlatStyle = FlatStyle.Flat
            btnReadDTCs.ForeColor = Color.White
            btnReadDTCs.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            btnReadDTCs.Enabled = False
            grpActions.Controls.Add(btnReadDTCs)

            ' Clear DTCs Button
            btnClearDTCs = New Button()
            btnClearDTCs.Text = "🗑️ DTC Sil"
            btnClearDTCs.Size = New Size(140, 40)
            btnClearDTCs.Location = New Point(165, 30)
            btnClearDTCs.BackColor = Color.FromArgb(231, 76, 60)
            btnClearDTCs.FlatStyle = FlatStyle.Flat
            btnClearDTCs.ForeColor = Color.White
            btnClearDTCs.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            btnClearDTCs.Enabled = False
            grpActions.Controls.Add(btnClearDTCs)

            ' Refresh Button
            btnRefresh = New Button()
            btnRefresh.Text = "🔄 Yenile"
            btnRefresh.Size = New Size(140, 40)
            btnRefresh.Location = New Point(315, 30)
            btnRefresh.BackColor = Color.FromArgb(46, 204, 113)
            btnRefresh.FlatStyle = FlatStyle.Flat
            btnRefresh.ForeColor = Color.White
            btnRefresh.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            btnRefresh.Enabled = False
            grpActions.Controls.Add(btnRefresh)

            ' Close Button
            btnClose = New Button()
            btnClose.Text = "❌ Kapat"
            btnClose.Size = New Size(120, 40)
            btnClose.Location = New Point(465, 30)
            btnClose.BackColor = Color.FromArgb(149, 165, 166)
            btnClose.FlatStyle = FlatStyle.Flat
            btnClose.ForeColor = Color.White
            btnClose.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            grpActions.Controls.Add(btnClose)

            splitContainer.Panel2.Controls.Add(pnlDetails)
        End Sub

        Private Sub SetupEventHandlers()
            AddHandler btnScan.Click, AddressOf BtnScan_Click
            AddHandler btnQuickScan.Click, AddressOf BtnQuickScan_Click
            AddHandler btnStop.Click, AddressOf BtnStop_Click
            AddHandler btnReadDTCs.Click, AddressOf BtnReadDTCs_Click
            AddHandler btnClearDTCs.Click, AddressOf BtnClearDTCs_Click
            AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click
            AddHandler btnClose.Click, AddressOf BtnClose_Click
            AddHandler treeViewEcus.AfterSelect, AddressOf TreeViewEcus_AfterSelect
            AddHandler _ecuScanner.OnEcuFound, AddressOf EcuScanner_OnEcuFound
            AddHandler _ecuScanner.OnScanProgress, AddressOf EcuScanner_OnScanProgress
            AddHandler _ecuScanner.OnScanComplete, AddressOf EcuScanner_OnScanComplete
            AddHandler _ecuScanner.OnError, AddressOf EcuScanner_OnError
        End Sub

#End Region

#Region "Event Handlers"

        Private Async Sub BtnScan_Click(sender As Object, e As EventArgs)
            Try
                If Not _canSender.IsConnected() Then
                    MessageBox.Show("CAN adaptörüne bağlı değilsiniz. Önce bağlantı kurun.", "Bağlantı Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                _isScanning = True
                UpdateUIState(True)
                treeViewEcus.Nodes.Clear()
                _ecuNodes.Clear()
                progressBar.Value = 0
                lblStatus.Text = "Tarama başlatılıyor..."

                _cancellationTokenSource = New Threading.CancellationTokenSource()

                ' Taramayı başlat
                Dim ecus = Await _ecuScanner.ScanAllEcus()

                lblStatus.Text = $"Tarama tamamlandı. {ecus.Count} ECU bulundu."

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.BtnScan_Click")
                MessageBox.Show($"Tarama hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                _isScanning = False
                UpdateUIState(False)
            End Try
        End Sub

        Private Async Sub BtnQuickScan_Click(sender As Object, e As EventArgs)
            Try
                If Not _canSender.IsConnected() Then
                    MessageBox.Show("CAN adaptörüne bağlı değilsiniz. Önce bağlantı kurun.", "Bağlantı Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                _isScanning = True
                UpdateUIState(True)
                treeViewEcus.Nodes.Clear()
                _ecuNodes.Clear()
                progressBar.Value = 0
                lblStatus.Text = "Hızlı tarama başlatılıyor..."

                ' Bilinen adresleri tara (örnek: 7E0, 7E1, 7A0, 7B0, 7C0, 7D0, 7F0)
                Dim knownAddresses = New Integer() {&H7E0, &H7E1, &H7E8, &H7A0, &H7B0, &H7C0, &H7D0, &H7F0}
                Dim foundCount = 0

                For i = 0 To knownAddresses.Length - 1
                    Dim address = knownAddresses(i)
                    progressBar.Value = CInt((i + 1) / knownAddresses.Length * 100)
                    lblStatus.Text = $"Taranıyor: {address:X3}..."

                    Dim ecu = Await _ecuScanner.TestEcuAddress(address)
                    If ecu IsNot Nothing AndAlso ecu.IsOnline Then
                        Await _ecuScanner.GetEcuDetails(ecu)
                        AddEcuToTree(ecu)
                        foundCount += 1
                    End If

                    Await Task.Delay(100)
                Next

                lblStatus.Text = $"Hızlı tarama tamamlandı. {foundCount} ECU bulundu."
                UpdateEcuCount()

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.BtnQuickScan_Click")
                MessageBox.Show($"Hızlı tarama hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                _isScanning = False
                UpdateUIState(False)
            End Try
        End Sub

        Private Sub BtnStop_Click(sender As Object, e As EventArgs)
            Try
                If _cancellationTokenSource IsNot Nothing Then
                    _cancellationTokenSource.Cancel()
                End If
                _isScanning = False
                UpdateUIState(False)
                lblStatus.Text = "Tarama durduruldu."
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.BtnStop_Click")
            End Try
        End Sub

        Private Async Sub BtnReadDTCs_Click(sender As Object, e As EventArgs)
            Try
                Dim selectedNode = treeViewEcus.SelectedNode
                If selectedNode Is Nothing OrElse selectedNode.Tag Is Nothing Then
                    Return
                End If

                Dim ecu = TryCast(selectedNode.Tag, EcuInfo)
                If ecu Is Nothing Then
                    Return
                End If

                lblStatus.Text = "DTC'ler okunuyor..."
                Dim dtcs = Await _ecuScanner.ReadEcuDTCs(ecu.Address)
                ecu.DTCCount = dtcs.Count
                UpdateEcuDetails(ecu)
                UpdateEcuNode(ecu)
                lblStatus.Text = $"{dtcs.Count} DTC bulundu."

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.BtnReadDTCs_Click")
                MessageBox.Show($"DTC okuma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnClearDTCs_Click(sender As Object, e As EventArgs)
            Try
                Dim selectedNode = treeViewEcus.SelectedNode
                If selectedNode Is Nothing OrElse selectedNode.Tag Is Nothing Then
                    Return
                End If

                Dim ecu = TryCast(selectedNode.Tag, EcuInfo)
                If ecu Is Nothing Then
                    Return
                End If

                Dim result = MessageBox.Show($"ECU {ecu.Name} ({ecu.Address:X3}) için tüm DTC'leri silmek istediğinizden emin misiniz?", "DTC Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If result <> DialogResult.Yes Then
                    Return
                End If

                ' TODO: UDS ClearDTC implementasyonu
                lblStatus.Text = "DTC silme özelliği yakında eklenecek."
                MessageBox.Show("DTC silme özelliği henüz implement edilmedi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.BtnClearDTCs_Click")
                MessageBox.Show($"DTC silme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Async Sub BtnRefresh_Click(sender As Object, e As EventArgs)
            Try
                Dim selectedNode = treeViewEcus.SelectedNode
                If selectedNode Is Nothing OrElse selectedNode.Tag Is Nothing Then
                    Return
                End If

                Dim ecu = TryCast(selectedNode.Tag, EcuInfo)
                If ecu Is Nothing Then
                    Return
                End If

                lblStatus.Text = "ECU bilgileri yenileniyor..."
                Await _ecuScanner.GetEcuDetails(ecu)
                UpdateEcuDetails(ecu)
                UpdateEcuNode(ecu)
                lblStatus.Text = "Bilgiler güncellendi."

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.BtnRefresh_Click")
                MessageBox.Show($"Yenileme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnClose_Click(sender As Object, e As EventArgs)
            Me.Close()
        End Sub

        Private Sub TreeViewEcus_AfterSelect(sender As Object, e As TreeViewEventArgs)
            Try
                If e.Node Is Nothing OrElse e.Node.Tag Is Nothing Then
                    ClearEcuDetails()
                    Return
                End If

                Dim ecu = TryCast(e.Node.Tag, EcuInfo)
                If ecu Is Nothing Then
                    ClearEcuDetails()
                    Return
                End If

                UpdateEcuDetails(ecu)
                btnReadDTCs.Enabled = True
                btnClearDTCs.Enabled = True
                btnRefresh.Enabled = True

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScannerForm.TreeViewEcus_AfterSelect")
            End Try
        End Sub

        Private Sub EcuScanner_OnEcuFound(ecu As EcuInfo)
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of EcuInfo)(AddressOf EcuScanner_OnEcuFound), ecu)
                Return
            End If

            AddEcuToTree(ecu)
            UpdateEcuCount()
        End Sub

        Private Sub EcuScanner_OnScanProgress(current As Integer, total As Integer)
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of Integer, Integer)(AddressOf EcuScanner_OnScanProgress), current, total)
                Return
            End If

            If total > 0 Then
                progressBar.Value = CInt((current / total) * 100)
                lblStatus.Text = $"Taranıyor: {current}/{total} adres..."
            End If
        End Sub

        Private Sub EcuScanner_OnScanComplete(ecus As List(Of EcuInfo))
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of List(Of EcuInfo))(AddressOf EcuScanner_OnScanComplete), ecus)
                Return
            End If

            _isScanning = False
            UpdateUIState(False)
            progressBar.Value = 100
            UpdateEcuCount()
            lblStatus.Text = $"Tarama tamamlandı. {ecus.Count} ECU bulundu."
        End Sub

        Private Sub EcuScanner_OnError(message As String)
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of String)(AddressOf EcuScanner_OnError), message)
                Return
            End If

            lblStatus.Text = $"Hata: {message}"
            ErrorHandler.Instance.LogWarning(message)
        End Sub

#End Region

#Region "Helper Methods"

        Private Sub AddEcuToTree(ecu As EcuInfo)
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of EcuInfo)(AddressOf AddEcuToTree), ecu)
                Return
            End If

            Dim icon = GetEcuIcon(ecu)
            Dim nodeText = $"{icon} {ecu.Name} ({ecu.Address:X3})"

            Dim node As New TreeNode(nodeText)
            node.Tag = ecu
            node.ImageIndex = 0
            node.SelectedImageIndex = 0

            ' Alt node'lar
            Dim dtcNode As New TreeNode($"📋 DTC'ler ({ecu.DTCCount})")
            dtcNode.Tag = "DTC"
            node.Nodes.Add(dtcNode)

            Dim infoNode As New TreeNode("ℹ️ Bilgiler")
            infoNode.Tag = "INFO"
            node.Nodes.Add(infoNode)

            Dim liveDataNode As New TreeNode("📊 Canlı Veri")
            liveDataNode.Tag = "LIVEDATA"
            node.Nodes.Add(liveDataNode)

            treeViewEcus.Nodes.Add(node)
            _ecuNodes(ecu) = node
            node.Expand()
        End Sub

        Private Sub UpdateEcuNode(ecu As EcuInfo)
            If Not _ecuNodes.ContainsKey(ecu) Then
                Return
            End If

            Dim node = _ecuNodes(ecu)
            Dim icon = GetEcuIcon(ecu)
            node.Text = $"{icon} {ecu.Name} ({ecu.Address:X3})"

            ' DTC node'unu güncelle
            If node.Nodes.Count > 0 Then
                node.Nodes(0).Text = $"📋 DTC'ler ({ecu.DTCCount})"
            End If
        End Sub

        Private Sub UpdateEcuDetails(ecu As EcuInfo)
            lblEcuName.Text = ecu.Name
            lblEcuAddress.Text = $"0x{ecu.Address:X3}"
            lblEcuType.Text = GetEcuTypeText(ecu.Type)
            lblSoftwareVersion.Text = If(String.IsNullOrEmpty(ecu.SoftwareVersion), "-", ecu.SoftwareVersion)
            lblHardwareVersion.Text = If(String.IsNullOrEmpty(ecu.HardwareVersion), "-", ecu.HardwareVersion)
            lblPartNumber.Text = If(String.IsNullOrEmpty(ecu.PartNumber), "-", ecu.PartNumber)
            lblSerialNumber.Text = If(String.IsNullOrEmpty(ecu.SerialNumber), "-", ecu.SerialNumber)
            lblDtcCount.Text = ecu.DTCCount.ToString()
            lblDtcCount.ForeColor = If(ecu.DTCCount > 0, Color.FromArgb(231, 76, 60), Color.FromArgb(46, 204, 113))
        End Sub

        Private Sub ClearEcuDetails()
            lblEcuName.Text = "-"
            lblEcuAddress.Text = "-"
            lblEcuType.Text = "-"
            lblSoftwareVersion.Text = "-"
            lblHardwareVersion.Text = "-"
            lblPartNumber.Text = "-"
            lblSerialNumber.Text = "-"
            lblDtcCount.Text = "0"
            btnReadDTCs.Enabled = False
            btnClearDTCs.Enabled = False
            btnRefresh.Enabled = False
        End Sub

        Private Sub UpdateUIState(isScanning As Boolean)
            btnScan.Enabled = Not isScanning
            btnQuickScan.Enabled = Not isScanning
            btnStop.Enabled = isScanning
        End Sub

        Private Sub UpdateEcuCount()
            Dim count = treeViewEcus.Nodes.Count
            lblEcuCount.Text = $"{count} ECU bulundu"
        End Sub

        Private Function GetEcuIcon(ecu As EcuInfo) As String
            ' DTC varsa kırmızı işaret
            If ecu.DTCCount > 0 Then
                Return "🔴"
            End If

            ' ECU tipine göre ikon
            Select Case ecu.Type
                Case EcuType.Engine
                    Return "🔧"
                Case EcuType.Transmission
                    Return "⚙️"
                Case EcuType.ABS
                    Return "🛞"
                Case EcuType.Airbag
                    Return "🎈"
                Case EcuType.BodyControl
                    Return "🚗"
                Case EcuType.Instrument
                    Return "📊"
                Case EcuType.Climate
                    Return "❄️"
                Case EcuType.Steering
                    Return "🔄"
                Case Else
                    Return "📦"
            End Select
        End Function

        Private Function GetEcuTypeText(ecuType As EcuType) As String
            Select Case ecuType
                Case EcuType.Engine
                    Return "Motor Kontrol Modülü"
                Case EcuType.Transmission
                    Return "Şanzıman Kontrol Modülü"
                Case EcuType.ABS
                    Return "ABS/Fren Kontrol Modülü"
                Case EcuType.Airbag
                    Return "Hava Yastığı Kontrol Modülü"
                Case EcuType.BodyControl
                    Return "Gövde Kontrol Modülü"
                Case EcuType.Instrument
                    Return "Kombi/Instrument Panel"
                Case EcuType.Climate
                    Return "Klima Kontrol Modülü"
                Case EcuType.Steering
                    Return "Direksiyon Kontrol Modülü"
                Case Else
                    Return "Bilinmeyen"
            End Select
        End Function

#End Region

    End Class

End Namespace

