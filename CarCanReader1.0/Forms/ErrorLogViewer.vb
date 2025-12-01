' ErrorLogViewer.vb
' Hata log görüntüleyici form
'
' F12 ile açılır
' Tüm hataları listeler ve detay gösterir

Imports System.Windows.Forms
Imports System.Drawing
Imports CarCanReader1._0.Services

Namespace Forms

    Public Class ErrorLogViewer
        Inherits Form

#Region "Controls"

        Private WithEvents lstErrors As ListView
        Private WithEvents txtDetails As TextBox
        Private WithEvents btnRefresh As Button
        Private WithEvents btnClear As Button
        Private WithEvents btnOpenLogFile As Button
        Private WithEvents btnOpenFolder As Button
        Private WithEvents btnCopyDetails As Button
        Private WithEvents cmbFilter As ComboBox
        Private lblStatus As Label
        Private splitContainer As SplitContainer

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
            LoadErrors()

            ' ErrorHandler event'ine bağlan
            AddHandler ErrorHandler.Instance.OnErrorLogged, AddressOf HandleNewError
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Hata Log Görüntüleyici (F12)"
            Me.Size = New Size(900, 600)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.MinimumSize = New Size(600, 400)
            Me.BackColor = Color.FromArgb(30, 30, 45)
            Me.ForeColor = Color.White
            Me.KeyPreview = True
            Me.Icon = Nothing

            ' SplitContainer
            splitContainer = New SplitContainer()
            splitContainer.Dock = DockStyle.Fill
            splitContainer.Orientation = Orientation.Horizontal
            splitContainer.SplitterDistance = 350
            splitContainer.BackColor = Color.FromArgb(30, 30, 45)
            splitContainer.Panel1.BackColor = Color.FromArgb(30, 30, 45)
            splitContainer.Panel2.BackColor = Color.FromArgb(30, 30, 45)

            ' Toolbar panel
            Dim pnlToolbar As New Panel()
            pnlToolbar.Dock = DockStyle.Top
            pnlToolbar.Height = 40
            pnlToolbar.BackColor = Color.FromArgb(40, 40, 55)
            pnlToolbar.Padding = New Padding(5)

            ' Filter ComboBox
            cmbFilter = New ComboBox()
            cmbFilter.Location = New Point(10, 8)
            cmbFilter.Size = New Size(150, 25)
            cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList
            cmbFilter.BackColor = Color.FromArgb(50, 50, 65)
            cmbFilter.ForeColor = Color.White
            cmbFilter.FlatStyle = FlatStyle.Flat
            cmbFilter.Items.AddRange({"Tümü", "Sadece Hatalar", "Sadece Uyarılar", "Kritik"})
            cmbFilter.SelectedIndex = 0
            pnlToolbar.Controls.Add(cmbFilter)

            ' Refresh Button
            btnRefresh = New Button()
            btnRefresh.Text = "🔄 Yenile"
            btnRefresh.Location = New Point(170, 7)
            btnRefresh.Size = New Size(80, 26)
            btnRefresh.FlatStyle = FlatStyle.Flat
            btnRefresh.BackColor = Color.FromArgb(60, 60, 80)
            btnRefresh.ForeColor = Color.White
            pnlToolbar.Controls.Add(btnRefresh)

            ' Clear Button
            btnClear = New Button()
            btnClear.Text = "🗑️ Temizle"
            btnClear.Location = New Point(260, 7)
            btnClear.Size = New Size(80, 26)
            btnClear.FlatStyle = FlatStyle.Flat
            btnClear.BackColor = Color.FromArgb(60, 60, 80)
            btnClear.ForeColor = Color.White
            pnlToolbar.Controls.Add(btnClear)

            ' Open Log File Button
            btnOpenLogFile = New Button()
            btnOpenLogFile.Text = "📄 Log Dosyası"
            btnOpenLogFile.Location = New Point(350, 7)
            btnOpenLogFile.Size = New Size(100, 26)
            btnOpenLogFile.FlatStyle = FlatStyle.Flat
            btnOpenLogFile.BackColor = Color.FromArgb(60, 60, 80)
            btnOpenLogFile.ForeColor = Color.White
            pnlToolbar.Controls.Add(btnOpenLogFile)

            ' Open Folder Button
            btnOpenFolder = New Button()
            btnOpenFolder.Text = "📁 Klasör"
            btnOpenFolder.Location = New Point(460, 7)
            btnOpenFolder.Size = New Size(80, 26)
            btnOpenFolder.FlatStyle = FlatStyle.Flat
            btnOpenFolder.BackColor = Color.FromArgb(60, 60, 80)
            btnOpenFolder.ForeColor = Color.White
            pnlToolbar.Controls.Add(btnOpenFolder)

            ' ListView for errors
            lstErrors = New ListView()
            lstErrors.Dock = DockStyle.Fill
            lstErrors.View = View.Details
            lstErrors.FullRowSelect = True
            lstErrors.GridLines = True
            lstErrors.BackColor = Color.FromArgb(35, 35, 50)
            lstErrors.ForeColor = Color.White
            lstErrors.BorderStyle = BorderStyle.None
            lstErrors.Font = New Font("Consolas", 9)

            lstErrors.Columns.Add("Zaman", 100)
            lstErrors.Columns.Add("Seviye", 70)
            lstErrors.Columns.Add("Bağlam", 120)
            lstErrors.Columns.Add("Mesaj", 500)

            splitContainer.Panel1.Controls.Add(lstErrors)
            splitContainer.Panel1.Controls.Add(pnlToolbar)

            ' Details panel
            Dim pnlDetailsHeader As New Panel()
            pnlDetailsHeader.Dock = DockStyle.Top
            pnlDetailsHeader.Height = 35
            pnlDetailsHeader.BackColor = Color.FromArgb(40, 40, 55)

            Dim lblDetails As New Label()
            lblDetails.Text = "📋 Detaylar"
            lblDetails.Location = New Point(10, 8)
            lblDetails.AutoSize = True
            lblDetails.ForeColor = Color.White
            pnlDetailsHeader.Controls.Add(lblDetails)

            btnCopyDetails = New Button()
            btnCopyDetails.Text = "📋 Kopyala"
            btnCopyDetails.Location = New Point(100, 5)
            btnCopyDetails.Size = New Size(80, 25)
            btnCopyDetails.FlatStyle = FlatStyle.Flat
            btnCopyDetails.BackColor = Color.FromArgb(60, 60, 80)
            btnCopyDetails.ForeColor = Color.White
            pnlDetailsHeader.Controls.Add(btnCopyDetails)

            txtDetails = New TextBox()
            txtDetails.Dock = DockStyle.Fill
            txtDetails.Multiline = True
            txtDetails.ScrollBars = ScrollBars.Both
            txtDetails.ReadOnly = True
            txtDetails.BackColor = Color.FromArgb(35, 35, 50)
            txtDetails.ForeColor = Color.LightGray
            txtDetails.BorderStyle = BorderStyle.None
            txtDetails.Font = New Font("Consolas", 9)
            txtDetails.WordWrap = False

            splitContainer.Panel2.Controls.Add(txtDetails)
            splitContainer.Panel2.Controls.Add(pnlDetailsHeader)

            ' Status bar
            lblStatus = New Label()
            lblStatus.Dock = DockStyle.Bottom
            lblStatus.Height = 25
            lblStatus.BackColor = Color.FromArgb(40, 40, 55)
            lblStatus.ForeColor = Color.LightGray
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            lblStatus.Padding = New Padding(10, 0, 0, 0)

            Me.Controls.Add(splitContainer)
            Me.Controls.Add(lblStatus)
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub LoadErrors()
            Try
                lstErrors.Items.Clear()

                Dim logs = ErrorHandler.Instance.GetAllLogs()

                ' Filter uygula
                If cmbFilter.SelectedIndex = 1 Then
                    logs = logs.Where(Function(l) l.Severity >= ErrorSeverity.Error).ToList()
                ElseIf cmbFilter.SelectedIndex = 2 Then
                    logs = logs.Where(Function(l) l.Severity = ErrorSeverity.Warning).ToList()
                ElseIf cmbFilter.SelectedIndex = 3 Then
                    logs = logs.Where(Function(l) l.Severity = ErrorSeverity.Critical).ToList()
                End If

                For Each entry In logs.OrderByDescending(Function(e) e.Timestamp)
                    Dim item As New ListViewItem(entry.Timestamp.ToString("HH:mm:ss.fff"))
                    item.SubItems.Add(entry.Severity.ToString())
                    item.SubItems.Add(entry.Context)
                    item.SubItems.Add(entry.Message)
                    item.Tag = entry

                    ' Renklendirme
                    Select Case entry.Severity
                        Case ErrorSeverity.Critical
                            item.ForeColor = Color.Red
                            item.BackColor = Color.FromArgb(60, 30, 30)
                        Case ErrorSeverity.Error
                            item.ForeColor = Color.Orange
                        Case ErrorSeverity.Warning
                            item.ForeColor = Color.Yellow
                        Case ErrorSeverity.Info
                            item.ForeColor = Color.LightBlue
                        Case ErrorSeverity.Debug
                            item.ForeColor = Color.Gray
                    End Select

                    lstErrors.Items.Add(item)
                Next

                UpdateStatus()

            Catch ex As Exception
                Debug.WriteLine($"LoadErrors error: {ex.Message}")
            End Try
        End Sub

        Private Sub UpdateStatus()
            Dim totalErrors = ErrorHandler.Instance.TotalErrorCount
            Dim totalWarnings = ErrorHandler.Instance.TotalWarningCount
            lblStatus.Text = $"Toplam: {lstErrors.Items.Count} kayıt | Hatalar: {totalErrors} | Uyarılar: {totalWarnings} | Log: {ErrorHandler.Instance.LogFilePath}"
        End Sub

        Private Sub lstErrors_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstErrors.SelectedIndexChanged
            If lstErrors.SelectedItems.Count > 0 Then
                Dim entry = TryCast(lstErrors.SelectedItems(0).Tag, ErrorLogEntry)
                If entry IsNot Nothing Then
                    txtDetails.Text = entry.FullDetails
                End If
            Else
                txtDetails.Text = ""
            End If
        End Sub

        Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
            LoadErrors()
        End Sub

        Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
            If MessageBox.Show("Bellekteki tüm loglar temizlenecek. Devam edilsin mi?",
                              "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                ErrorHandler.Instance.ClearMemoryLogs()
                LoadErrors()
            End If
        End Sub

        Private Sub btnOpenLogFile_Click(sender As Object, e As EventArgs) Handles btnOpenLogFile.Click
            ErrorHandler.Instance.OpenLogFile()
        End Sub

        Private Sub btnOpenFolder_Click(sender As Object, e As EventArgs) Handles btnOpenFolder.Click
            ErrorHandler.Instance.OpenLogFolder()
        End Sub

        Private Sub btnCopyDetails_Click(sender As Object, e As EventArgs) Handles btnCopyDetails.Click
            If Not String.IsNullOrEmpty(txtDetails.Text) Then
                Clipboard.SetText(txtDetails.Text)
                MessageBox.Show("Detaylar panoya kopyalandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Sub

        Private Sub cmbFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFilter.SelectedIndexChanged
            LoadErrors()
        End Sub

        Private Sub HandleNewError(entry As ErrorLogEntry)
            ' Thread-safe güncelleme
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() LoadErrors())
            Else
                LoadErrors()
            End If
        End Sub

        Private Sub ErrorLogViewer_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
            If e.KeyCode = Keys.F12 OrElse e.KeyCode = Keys.Escape Then
                Me.Close()
            ElseIf e.KeyCode = Keys.F5 Then
                LoadErrors()
            End If
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            RemoveHandler ErrorHandler.Instance.OnErrorLogged, AddressOf HandleNewError
            MyBase.OnFormClosing(e)
        End Sub

#End Region

    End Class

End Namespace

