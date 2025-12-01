' HelpViewer.vb
' In-app yardım görüntüleyici
' HTML içerik gösterimi ve arama desteği

Imports System.IO
Imports System.Text
Imports Newtonsoft.Json.Linq
Imports CarCanReader1._0.Services

Public Class HelpViewer
    Inherits Form

#Region "Private Fields"

    Private _helpContentPath As String
    Private _indexData As JObject = Nothing
    Private _currentTopic As String = ""

#End Region

#Region "UI Components"

    Private pnlMain As Panel
    Private splitContainer As SplitContainer
    Private pnlSearch As Panel
    Private txtSearch As TextBox
    Private btnSearch As Button
    Private btnClearSearch As Button
    Private treeViewTopics As TreeView
    Private webBrowser As WebBrowser
    Private btnPrint As Button
    Private btnClose As Button
    Private statusLabel As Label

#End Region

#Region "Constructor"

    Public Sub New(Optional initialTopic As String = "")
        _helpContentPath = Path.Combine(Application.StartupPath, "data", "help")
        _currentTopic = initialTopic
        InitializeComponent()
        LoadHelpIndex()
        PopulateTreeView()
        If Not String.IsNullOrEmpty(initialTopic) Then
            NavigateToTopic(initialTopic)
        Else
            NavigateToTopic("getting-started")
        End If
    End Sub

#End Region

#Region "Initialize Component"

    Private Sub InitializeComponent()
        Me.Text = "CarCanReader - Yardım"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(800, 600)
        Me.BackColor = Color.FromArgb(30, 30, 40)

        ' Ana panel
        pnlMain = New Panel()
        pnlMain.Dock = DockStyle.Fill
        pnlMain.Padding = New Padding(10)

        ' Arama paneli
        pnlSearch = New Panel()
        pnlSearch.Height = 50
        pnlSearch.Dock = DockStyle.Top
        pnlSearch.BackColor = Color.FromArgb(35, 35, 50)

        txtSearch = New TextBox()
        txtSearch.Location = New Point(10, 12)
        txtSearch.Size = New Size(300, 25)
        txtSearch.Font = New Font("Segoe UI", 10.0!)
        txtSearch.BackColor = Color.FromArgb(50, 50, 65)
        txtSearch.ForeColor = Color.White
        txtSearch.BorderStyle = BorderStyle.FixedSingle
        AddHandler txtSearch.KeyDown, AddressOf TxtSearch_KeyDown

        btnSearch = New Button()
        btnSearch.Text = "🔍 Ara"
        btnSearch.Location = New Point(320, 10)
        btnSearch.Size = New Size(80, 30)
        btnSearch.BackColor = Color.FromArgb(52, 152, 219)
        btnSearch.FlatStyle = FlatStyle.Flat
        btnSearch.ForeColor = Color.White
        btnSearch.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        AddHandler btnSearch.Click, AddressOf BtnSearch_Click

        btnClearSearch = New Button()
        btnClearSearch.Text = "❌ Temizle"
        btnClearSearch.Location = New Point(410, 10)
        btnClearSearch.Size = New Size(80, 30)
        btnClearSearch.BackColor = Color.FromArgb(231, 76, 60)
        btnClearSearch.FlatStyle = FlatStyle.Flat
        btnClearSearch.ForeColor = Color.White
        btnClearSearch.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        AddHandler btnClearSearch.Click, AddressOf BtnClearSearch_Click

        pnlSearch.Controls.Add(txtSearch)
        pnlSearch.Controls.Add(btnSearch)
        pnlSearch.Controls.Add(btnClearSearch)

        ' SplitContainer
        splitContainer = New SplitContainer()
        splitContainer.Dock = DockStyle.Fill
        splitContainer.Orientation = Orientation.Horizontal
        splitContainer.SplitterDistance = Me.Height - 100
        splitContainer.Panel1MinSize = 200
        splitContainer.Panel2MinSize = 50

        ' Alt panel (butonlar ve status)
        Dim pnlBottom As New Panel()
        pnlBottom.Height = 50
        pnlBottom.Dock = DockStyle.Bottom
        pnlBottom.BackColor = Color.FromArgb(35, 35, 50)

        btnPrint = New Button()
        btnPrint.Text = "🖨️ Yazdır"
        btnPrint.Location = New Point(10, 10)
        btnPrint.Size = New Size(100, 30)
        btnPrint.BackColor = Color.FromArgb(46, 204, 113)
        btnPrint.FlatStyle = FlatStyle.Flat
        btnPrint.ForeColor = Color.White
        btnPrint.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        AddHandler btnPrint.Click, AddressOf BtnPrint_Click

        btnClose = New Button()
        btnClose.Text = "✖️ Kapat"
        btnClose.Location = New Point(120, 10)
        btnClose.Size = New Size(100, 30)
        btnClose.BackColor = Color.FromArgb(231, 76, 60)
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.ForeColor = Color.White
        btnClose.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        AddHandler btnClose.Click, AddressOf BtnClose_Click

        statusLabel = New Label()
        statusLabel.Text = "Hazır"
        statusLabel.Location = New Point(230, 15)
        statusLabel.Size = New Size(500, 20)
        statusLabel.ForeColor = Color.White
        statusLabel.Font = New Font("Segoe UI", 9.0!)

        pnlBottom.Controls.Add(btnPrint)
        pnlBottom.Controls.Add(btnClose)
        pnlBottom.Controls.Add(statusLabel)

        ' TreeView (sol panel)
        treeViewTopics = New TreeView()
        treeViewTopics.Dock = DockStyle.Fill
        treeViewTopics.BackColor = Color.FromArgb(40, 40, 55)
        treeViewTopics.ForeColor = Color.White
        treeViewTopics.Font = New Font("Segoe UI", 10.0!)
        treeViewTopics.BorderStyle = BorderStyle.None
        treeViewTopics.ShowLines = True
        treeViewTopics.ShowRootLines = True
        treeViewTopics.HideSelection = False
        AddHandler treeViewTopics.AfterSelect, AddressOf TreeViewTopics_AfterSelect

        ' WebBrowser (sağ panel)
        webBrowser = New WebBrowser()
        webBrowser.Dock = DockStyle.Fill
        webBrowser.IsWebBrowserContextMenuEnabled = False
        webBrowser.ScriptErrorsSuppressed = True

        ' SplitContainer içeriği
        Dim splitMain As New SplitContainer()
        splitMain.Dock = DockStyle.Fill
        splitMain.Orientation = Orientation.Vertical
        splitMain.SplitterDistance = 300
        splitMain.Panel1MinSize = 200
        splitMain.Panel2MinSize = 400

        splitMain.Panel1.Controls.Add(treeViewTopics)
        splitMain.Panel2.Controls.Add(webBrowser)

        splitContainer.Panel1.Controls.Add(splitMain)
        splitContainer.Panel2.Controls.Add(pnlBottom)

        ' Ana panel düzeni
        pnlMain.Controls.Add(splitContainer)
        pnlMain.Controls.Add(pnlSearch)

        Me.Controls.Add(pnlMain)

        ' F1 tuşu desteği
        Me.KeyPreview = True
        AddHandler Me.KeyDown, AddressOf HelpViewer_KeyDown
    End Sub

#End Region

#Region "Load Help Index"

    ''' <summary>
    ''' Yardım index dosyasını yükle
    ''' </summary>
    Private Sub LoadHelpIndex()
        Try
            Dim indexPath = Path.Combine(_helpContentPath, "index.json")
            If File.Exists(indexPath) Then
                Dim jsonContent = File.ReadAllText(indexPath, Encoding.UTF8)
                _indexData = JObject.Parse(jsonContent)
            Else
                ' Varsayılan index oluştur
                CreateDefaultIndex()
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "HelpViewer.LoadHelpIndex")
            CreateDefaultIndex()
        End Try
    End Sub

    ''' <summary>
    ''' Varsayılan index oluştur
    ''' </summary>
    Private Sub CreateDefaultIndex()
        _indexData = New JObject(
            New JProperty("topics", New JArray(
                New JObject(
                    New JProperty("category", "Başlarken"),
                    New JProperty("icon", "📘"),
                    New JProperty("items", New JArray(
                        New JObject(New JProperty("title", "Gereksinimler"), New JProperty("file", "getting-started.html")),
                        New JObject(New JProperty("title", "Kurulum"), New JProperty("file", "getting-started.html#kurulum")),
                        New JObject(New JProperty("title", "İlk Bağlantı"), New JProperty("file", "connection.html"))
                    ))
                ),
                New JObject(
                    New JProperty("category", "Temel Kullanım"),
                    New JProperty("icon", "📘"),
                    New JProperty("items", New JArray(
                        New JObject(New JProperty("title", "Araç Bağlantısı"), New JProperty("file", "connection.html")),
                        New JObject(New JProperty("title", "OBD Veri Okuma"), New JProperty("file", "obd-reading.html")),
                        New JObject(New JProperty("title", "Hata Kodu Okuma"), New JProperty("file", "dtc-reading.html")),
                        New JObject(New JProperty("title", "Hata Kodu Silme"), New JProperty("file", "dtc-clearing.html"))
                    ))
                ),
                New JObject(
                    New JProperty("category", "Gelişmiş Özellikler"),
                    New JProperty("icon", "📘"),
                    New JProperty("items", New JArray(
                        New JObject(New JProperty("title", "Coding Sistemi"), New JProperty("file", "coding.html")),
                        New JObject(New JProperty("title", "Learning Engine"), New JProperty("file", "learning-engine.html")),
                        New JObject(New JProperty("title", "CAN Analizi"), New JProperty("file", "can-analysis.html"))
                    ))
                ),
                New JObject(
                    New JProperty("category", "Sorun Giderme"),
                    New JProperty("icon", "📘"),
                    New JProperty("items", New JArray(
                        New JObject(New JProperty("title", "Bağlantı Sorunları"), New JProperty("file", "troubleshooting.html#connection")),
                        New JObject(New JProperty("title", "Veri Okuma Sorunları"), New JProperty("file", "troubleshooting.html#data")),
                        New JObject(New JProperty("title", "Sık Sorulan Sorular"), New JProperty("file", "troubleshooting.html#faq"))
                    ))
                )
            ))
        )
    End Sub

#End Region

#Region "Populate TreeView"

    ''' <summary>
    ''' TreeView'ı konu ağacı ile doldur
    ''' </summary>
    Private Sub PopulateTreeView()
        Try
            treeViewTopics.Nodes.Clear()

            If _indexData Is Nothing OrElse _indexData("topics") Is Nothing Then Return

            For Each topic In _indexData("topics")
                Dim categoryName = topic("category").ToString()
                Dim icon = If(topic("icon") IsNot Nothing, topic("icon").ToString(), "📘")
                Dim categoryNode As New TreeNode($"{icon} {categoryName}")
                categoryNode.Tag = Nothing

                If topic("items") IsNot Nothing Then
                    For Each item In topic("items")
                        Dim itemTitle = item("title").ToString()
                        Dim itemFile = item("file").ToString()
                        Dim itemNode As New TreeNode(itemTitle)
                        itemNode.Tag = itemFile
                        categoryNode.Nodes.Add(itemNode)
                    Next
                End If

                treeViewTopics.Nodes.Add(categoryNode)
                categoryNode.Expand()
            Next

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "HelpViewer.PopulateTreeView")
        End Try
    End Sub

#End Region

#Region "Navigation"

    ''' <summary>
    ''' Belirtilen konuya git
    ''' </summary>
    Private Sub NavigateToTopic(topicFile As String)
        Try
            If String.IsNullOrEmpty(topicFile) Then Return

            ' Anchor varsa ayır
            Dim filePart = topicFile
            Dim anchorPart = ""
            If topicFile.Contains("#") Then
                Dim parts = topicFile.Split("#"c)
                filePart = parts(0)
                anchorPart = parts(1)
            End If

            Dim filePath = Path.Combine(_helpContentPath, filePart)
            If Not File.Exists(filePath) Then
                ' Dosya yoksa varsayılan içerik göster
                ShowDefaultContent(filePart)
                Return
            End If

            ' HTML içeriğini yükle
            Dim htmlContent = File.ReadAllText(filePath, Encoding.UTF8)

            ' Base path ekle (resimler için)
            htmlContent = htmlContent.Replace("src=""images/", $"src=""file:///{_helpContentPath.Replace("\", "/")}/images/")

            ' Anchor varsa ekle
            If Not String.IsNullOrEmpty(anchorPart) Then
                htmlContent = htmlContent.Replace("</body>", $"<script>window.location.hash = '{anchorPart}';</script></body>")
            End If

            ' WebBrowser'a yükle
            webBrowser.DocumentText = htmlContent
            _currentTopic = topicFile

            statusLabel.Text = $"Konu: {Path.GetFileNameWithoutExtension(filePart)}"

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "HelpViewer.NavigateToTopic")
            statusLabel.Text = "Hata: Konu yüklenemedi"
        End Try
    End Sub

    ''' <summary>
    ''' Varsayılan içerik göster
    ''' </summary>
    Private Sub ShowDefaultContent(topicName As String)
        Dim defaultHtml = $"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{topicName}</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #1e1e2e; color: #ffffff; padding: 20px; }}
        h1 {{ color: #3498db; }}
        h2 {{ color: #2ecc71; margin-top: 30px; }}
        p {{ line-height: 1.6; }}
        code {{ background: #2d2d3e; padding: 2px 6px; border-radius: 3px; }}
    </style>
</head>
<body>
    <h1>{topicName}</h1>
    <p>Bu konu için içerik henüz hazırlanmamış.</p>
    <p>Yakında eklenecektir.</p>
</body>
</html>"
        webBrowser.DocumentText = defaultHtml
    End Sub

#End Region

#Region "Search"

    ''' <summary>
    ''' Arama yap
    ''' </summary>
    Private Sub PerformSearch(searchText As String)
        Try
            If String.IsNullOrWhiteSpace(searchText) Then
                PopulateTreeView()
                Return
            End If

            ' TreeView'da arama
            treeViewTopics.Nodes.Clear()
            Dim searchLower = searchText.ToLowerInvariant()

            If _indexData Is Nothing OrElse _indexData("topics") Is Nothing Then Return

            For Each topic In _indexData("topics")
                Dim categoryName = topic("category").ToString()
                Dim icon = If(topic("icon") IsNot Nothing, topic("icon").ToString(), "📘")
                Dim categoryNode As New TreeNode($"{icon} {categoryName}")
                categoryNode.Tag = Nothing
                Dim hasMatchingItems = False

                If topic("items") IsNot Nothing Then
                    For Each item In topic("items")
                        Dim itemTitle = item("title").ToString()
                        Dim itemFile = item("file").ToString()

                        ' Başlıkta veya dosya adında arama
                        If itemTitle.ToLowerInvariant().Contains(searchLower) OrElse
                           itemFile.ToLowerInvariant().Contains(searchLower) Then
                            Dim itemNode As New TreeNode(itemTitle)
                            itemNode.Tag = itemFile
                            categoryNode.Nodes.Add(itemNode)
                            hasMatchingItems = True
                        End If
                    Next
                End If

                ' Kategori adında arama
                If categoryName.ToLowerInvariant().Contains(searchLower) OrElse hasMatchingItems Then
                    treeViewTopics.Nodes.Add(categoryNode)
                    categoryNode.Expand()
                End If
            Next

            statusLabel.Text = $"Arama: '{searchText}' - {treeViewTopics.GetNodeCount(True)} sonuç"

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "HelpViewer.PerformSearch")
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub TreeViewTopics_AfterSelect(sender As Object, e As TreeViewEventArgs)
        Try
            If e.Node.Tag IsNot Nothing Then
                NavigateToTopic(e.Node.Tag.ToString())
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "HelpViewer.TreeViewTopics_AfterSelect")
        End Try
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs)
        PerformSearch(txtSearch.Text)
    End Sub

    Private Sub BtnClearSearch_Click(sender As Object, e As EventArgs)
        txtSearch.Clear()
        PopulateTreeView()
        statusLabel.Text = "Hazır"
    End Sub

    Private Sub TxtSearch_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            PerformSearch(txtSearch.Text)
            e.Handled = True
        End If
    End Sub

    Private Sub BtnPrint_Click(sender As Object, e As EventArgs)
        Try
            webBrowser.ShowPrintDialog()
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "HelpViewer.BtnPrint_Click")
            MessageBox.Show("Yazdırma hatası: " & ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub HelpViewer_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub

#End Region

#Region "Public Methods"

    ''' <summary>
    ''' Belirli bir konuya git (dışarıdan çağrılabilir)
    ''' </summary>
    Public Sub ShowTopic(topicFile As String)
        NavigateToTopic(topicFile)
    End Sub

#End Region

End Class
