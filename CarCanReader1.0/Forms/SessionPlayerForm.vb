Imports System.IO
Imports CarCanReader1._0.Services

Namespace Forms
    ''' <summary>
    ''' Oturum kayıtlarını oynatma ve analiz formu
    ''' Video player benzeri arayüz
    ''' </summary>
    Public Class SessionPlayerForm
        Inherits Form

#Region "UI Controls"
        ' Toolbar
        Private WithEvents btnOpen As Button
        Private WithEvents btnPlay As Button
        Private WithEvents btnPause As Button
        Private WithEvents btnStop As Button
        Private WithEvents btnRewind As Button
        Private WithEvents btnFastForward As Button
        Private WithEvents cmbSpeed As ComboBox
        Private WithEvents btnExportCSV As Button
        Private WithEvents btnExportASC As Button

        ' Timeline
        Private WithEvents trackSeek As TrackBar
        Private lblCurrentTime As Label
        Private lblTotalTime As Label
        Private pnlTimeline As Panel

        ' Frame list
        Private WithEvents dgvFrames As DataGridView
        Private WithEvents txtFilterId As TextBox
        Private lblFilterId As Label
        Private WithEvents btnClearFilter As Button

        ' Info panel
        Private grpInfo As GroupBox
        Private lblRecordDate As Label
        Private lblDuration As Label
        Private lblFrameCount As Label
        Private lblFileSize As Label

        ' Layout panels
        Private pnlTop As Panel
        Private pnlBottom As Panel
        Private splitContainer As SplitContainer
#End Region

#Region "Private Fields"
        Private WithEvents _recorder As SessionRecorder
        Private _isPlaying As Boolean = False
        Private _updateTimer As Timer
        Private _currentFilePath As String = ""
        Private _filteredFrameId As Integer = -1
#End Region

#Region "Constructor"
        Public Sub New()
            InitializeComponent()
            _recorder = New SessionRecorder()
            AddHandler _recorder.OnPlaybackFrame, AddressOf HandlePlaybackFrame
            AddHandler _recorder.OnPlaybackProgress, AddressOf HandlePlaybackProgress
            AddHandler _recorder.OnPlaybackStarted, AddressOf HandlePlaybackStarted
            AddHandler _recorder.OnPlaybackStopped, AddressOf HandlePlaybackStopped
            AddHandler _recorder.OnError, AddressOf HandleError

            ' UI güncelleme timer'ı
            _updateTimer = New Timer()
            _updateTimer.Interval = 100 ' 100ms
            AddHandler _updateTimer.Tick, AddressOf UpdateTimerTick
        End Sub
#End Region

#Region "Form Initialization"
        Private Sub InitializeComponent()
            Me.Text = "Oturum Oynatıcı"
            Me.Size = New Size(1200, 700)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = Color.FromArgb(45, 45, 48)
            Me.ForeColor = Color.White

            ' Ana layout
            CreateTopPanel()
            CreateBottomPanel()

            ' Split container
            splitContainer = New SplitContainer()
            splitContainer.Dock = DockStyle.Fill
            splitContainer.Orientation = Orientation.Horizontal
            splitContainer.SplitterDistance = 150
            splitContainer.BackColor = Color.FromArgb(45, 45, 48)
            splitContainer.Panel1.Controls.Add(pnlTop)
            splitContainer.Panel2.Controls.Add(pnlBottom)

            Me.Controls.Add(splitContainer)

            UpdateButtonStates()
        End Sub

        Private Sub CreateTopPanel()
            pnlTop = New Panel()
            pnlTop.Dock = DockStyle.Fill
            pnlTop.BackColor = Color.FromArgb(45, 45, 48)

            ' Toolbar
            Dim toolbar As New Panel()
            toolbar.Dock = DockStyle.Top
            toolbar.Height = 50
            toolbar.BackColor = Color.FromArgb(37, 37, 38)

            ' Butonlar
            Dim xPos = 10

            btnOpen = CreateButton("📁 Aç", xPos, 10)
            xPos += 80

            btnRewind = CreateButton("⏪", xPos, 10, 40)
            xPos += 45

            btnPlay = CreateButton("▶", xPos, 10, 40)
            xPos += 45

            btnPause = CreateButton("⏸", xPos, 10, 40)
            xPos += 45

            btnStop = CreateButton("⏹", xPos, 10, 40)
            xPos += 45

            btnFastForward = CreateButton("⏩", xPos, 10, 40)
            xPos += 50

            ' Hız seçici
            Dim lblSpeed As New Label()
            lblSpeed.Text = "Hız:"
            lblSpeed.Location = New Point(xPos, 15)
            lblSpeed.Size = New Size(35, 20)
            lblSpeed.ForeColor = Color.White
            toolbar.Controls.Add(lblSpeed)
            xPos += 40

            cmbSpeed = New ComboBox()
            cmbSpeed.Location = New Point(xPos, 12)
            cmbSpeed.Size = New Size(80, 25)
            cmbSpeed.DropDownStyle = ComboBoxStyle.DropDownList
            cmbSpeed.Items.AddRange(New String() {"0.25x", "0.5x", "1x", "2x", "4x", "10x"})
            cmbSpeed.SelectedIndex = 2 ' 1x
            cmbSpeed.BackColor = Color.FromArgb(60, 60, 60)
            cmbSpeed.ForeColor = Color.White
            toolbar.Controls.Add(cmbSpeed)
            xPos += 90

            ' Export butonları
            btnExportCSV = CreateButton("CSV", xPos, 10, 60)
            xPos += 65

            btnExportASC = CreateButton("ASC", xPos, 10, 60)

            toolbar.Controls.Add(btnOpen)
            toolbar.Controls.Add(btnRewind)
            toolbar.Controls.Add(btnPlay)
            toolbar.Controls.Add(btnPause)
            toolbar.Controls.Add(btnStop)
            toolbar.Controls.Add(btnFastForward)
            toolbar.Controls.Add(btnExportCSV)
            toolbar.Controls.Add(btnExportASC)

            ' Timeline panel
            pnlTimeline = New Panel()
            pnlTimeline.Dock = DockStyle.Top
            pnlTimeline.Height = 60
            pnlTimeline.BackColor = Color.FromArgb(45, 45, 48)

            lblCurrentTime = New Label()
            lblCurrentTime.Text = "00:00:00"
            lblCurrentTime.Location = New Point(10, 10)
            lblCurrentTime.Size = New Size(70, 20)
            lblCurrentTime.ForeColor = Color.White
            pnlTimeline.Controls.Add(lblCurrentTime)

            trackSeek = New TrackBar()
            trackSeek.Location = New Point(85, 5)
            trackSeek.Size = New Size(Me.Width - 180, 45)
            trackSeek.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            trackSeek.Minimum = 0
            trackSeek.Maximum = 100
            trackSeek.TickStyle = TickStyle.None
            trackSeek.BackColor = Color.FromArgb(45, 45, 48)
            pnlTimeline.Controls.Add(trackSeek)

            lblTotalTime = New Label()
            lblTotalTime.Text = "00:00:00"
            lblTotalTime.Location = New Point(Me.Width - 80, 10)
            lblTotalTime.Size = New Size(70, 20)
            lblTotalTime.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            lblTotalTime.ForeColor = Color.White
            lblTotalTime.TextAlign = ContentAlignment.TopRight
            pnlTimeline.Controls.Add(lblTotalTime)

            ' Info panel
            grpInfo = New GroupBox()
            grpInfo.Text = "Kayıt Bilgileri"
            grpInfo.Dock = DockStyle.Fill
            grpInfo.ForeColor = Color.White
            grpInfo.Padding = New Padding(10)

            Dim yPos = 25

            lblRecordDate = CreateInfoLabel("Kayıt Tarihi: -", 10, yPos)
            yPos += 25
            lblDuration = CreateInfoLabel("Süre: -", 10, yPos)
            yPos += 25
            lblFrameCount = CreateInfoLabel("Frame Sayısı: -", 10, yPos)
            yPos += 25
            lblFileSize = CreateInfoLabel("Dosya Boyutu: -", 10, yPos)

            grpInfo.Controls.Add(lblRecordDate)
            grpInfo.Controls.Add(lblDuration)
            grpInfo.Controls.Add(lblFrameCount)
            grpInfo.Controls.Add(lblFileSize)

            pnlTop.Controls.Add(grpInfo)
            pnlTop.Controls.Add(pnlTimeline)
            pnlTop.Controls.Add(toolbar)
        End Sub

        Private Sub CreateBottomPanel()
            pnlBottom = New Panel()
            pnlBottom.Dock = DockStyle.Fill
            pnlBottom.BackColor = Color.FromArgb(45, 45, 48)

            ' Filter panel
            Dim pnlFilter As New Panel()
            pnlFilter.Dock = DockStyle.Top
            pnlFilter.Height = 40
            pnlFilter.BackColor = Color.FromArgb(37, 37, 38)

            lblFilterId = New Label()
            lblFilterId.Text = "Frame ID Filtresi:"
            lblFilterId.Location = New Point(10, 12)
            lblFilterId.Size = New Size(110, 20)
            lblFilterId.ForeColor = Color.White
            pnlFilter.Controls.Add(lblFilterId)

            txtFilterId = New TextBox()
            txtFilterId.Location = New Point(125, 10)
            txtFilterId.Size = New Size(100, 20)
            txtFilterId.BackColor = Color.FromArgb(60, 60, 60)
            txtFilterId.ForeColor = Color.White
            pnlFilter.Controls.Add(txtFilterId)

            btnClearFilter = New Button()
            btnClearFilter.Text = "Temizle"
            btnClearFilter.Location = New Point(230, 9)
            btnClearFilter.Size = New Size(70, 23)
            btnClearFilter.BackColor = Color.FromArgb(60, 60, 60)
            btnClearFilter.ForeColor = Color.White
            btnClearFilter.FlatStyle = FlatStyle.Flat
            pnlFilter.Controls.Add(btnClearFilter)

            ' DataGridView
            dgvFrames = New DataGridView()
            dgvFrames.Dock = DockStyle.Fill
            dgvFrames.BackgroundColor = Color.FromArgb(45, 45, 48)
            dgvFrames.ForeColor = Color.White
            dgvFrames.GridColor = Color.FromArgb(60, 60, 60)
            dgvFrames.BorderStyle = BorderStyle.None
            dgvFrames.AllowUserToAddRows = False
            dgvFrames.AllowUserToDeleteRows = False
            dgvFrames.ReadOnly = True
            dgvFrames.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            dgvFrames.MultiSelect = False
            dgvFrames.RowHeadersVisible = False
            dgvFrames.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            ' Kolonlar
            dgvFrames.Columns.Add("colTime", "Zaman (ms)")
            dgvFrames.Columns.Add("colId", "Frame ID")
            dgvFrames.Columns.Add("colDLC", "DLC")
            dgvFrames.Columns.Add("colData", "Data")

            dgvFrames.Columns("colTime").Width = 100
            dgvFrames.Columns("colId").Width = 80
            dgvFrames.Columns("colDLC").Width = 50
            dgvFrames.Columns("colData").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            ' Stil
            Dim cellStyle As New DataGridViewCellStyle()
            cellStyle.BackColor = Color.FromArgb(45, 45, 48)
            cellStyle.ForeColor = Color.White
            cellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204)
            cellStyle.SelectionForeColor = Color.White
            dgvFrames.DefaultCellStyle = cellStyle

            Dim headerStyle As New DataGridViewCellStyle()
            headerStyle.BackColor = Color.FromArgb(37, 37, 38)
            headerStyle.ForeColor = Color.White
            headerStyle.Font = New Font(dgvFrames.Font, FontStyle.Bold)
            dgvFrames.ColumnHeadersDefaultCellStyle = headerStyle

            pnlBottom.Controls.Add(dgvFrames)
            pnlBottom.Controls.Add(pnlFilter)
        End Sub

        Private Function CreateButton(text As String, x As Integer, y As Integer, Optional width As Integer = 75) As Button
            Dim btn As New Button()
            btn.Text = text
            btn.Location = New Point(x, y)
            btn.Size = New Size(width, 30)
            btn.BackColor = Color.FromArgb(60, 60, 60)
            btn.ForeColor = Color.White
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80)
            Return btn
        End Function

        Private Function CreateInfoLabel(text As String, x As Integer, y As Integer) As Label
            Dim lbl As New Label()
            lbl.Text = text
            lbl.Location = New Point(x, y)
            lbl.Size = New Size(400, 20)
            lbl.ForeColor = Color.White
            Return lbl
        End Function
#End Region

#Region "Event Handlers - Buttons"
        Private Sub BtnOpen_Click(sender As Object, e As EventArgs) Handles btnOpen.Click
            Using ofd As New OpenFileDialog()
                ofd.Filter = "CarCanReader Kayıt (*.ccrec)|*.ccrec|CSV Dosyası (*.csv)|*.csv|Tüm Dosyalar (*.*)|*.*"
                ofd.Title = "Oturum Dosyası Aç"

                If ofd.ShowDialog() = DialogResult.OK Then
                    LoadSession(ofd.FileName)
                End If
            End Using
        End Sub

        Private Sub BtnPlay_Click(sender As Object, e As EventArgs) Handles btnPlay.Click
            If _recorder.TotalFrameCount = 0 Then
                MessageBox.Show("Lütfen önce bir oturum dosyası açın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                Dim speed = GetSelectedSpeed()
                _recorder.StartPlayback(speed)
                _isPlaying = True
                _updateTimer.Start()
                UpdateButtonStates()
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionPlayerForm.BtnPlay_Click")
                MessageBox.Show($"Oynatma başlatılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub BtnPause_Click(sender As Object, e As EventArgs) Handles btnPause.Click
            _recorder.PausePlayback()
            _isPlaying = False
            _updateTimer.Stop()
            UpdateButtonStates()
        End Sub

        Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
            _recorder.StopPlayback()
            _isPlaying = False
            _updateTimer.Stop()
            trackSeek.Value = 0
            UpdateButtonStates()
        End Sub

        Private Sub BtnRewind_Click(sender As Object, e As EventArgs) Handles btnRewind.Click
            If _recorder.TotalFrameCount = 0 Then Return

            Dim newFrame = Math.Max(0, _recorder.CurrentPlaybackFrame - 100)
            _recorder.SeekTo(newFrame)
            UpdateSeekBar()
        End Sub

        Private Sub BtnFastForward_Click(sender As Object, e As EventArgs) Handles btnFastForward.Click
            If _recorder.TotalFrameCount = 0 Then Return

            Dim newFrame = Math.Min(_recorder.TotalFrameCount - 1, _recorder.CurrentPlaybackFrame + 100)
            _recorder.SeekTo(newFrame)
            UpdateSeekBar()
        End Sub

        Private Sub BtnExportCSV_Click(sender As Object, e As EventArgs) Handles btnExportCSV.Click
            If _recorder.TotalFrameCount = 0 Then
                MessageBox.Show("Yüklenmiş oturum yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using sfd As New SaveFileDialog()
                sfd.Filter = "CSV Dosyası (*.csv)|*.csv"
                sfd.FileName = Path.GetFileNameWithoutExtension(_currentFilePath) & ".csv"

                If sfd.ShowDialog() = DialogResult.OK Then
                    Try
                        _recorder.ExportToCSV(sfd.FileName)
                        MessageBox.Show("CSV dışa aktarma başarılı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        ErrorHandler.Instance.LogError(ex, "SessionPlayerForm.BtnExportCSV_Click")
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Sub BtnExportASC_Click(sender As Object, e As EventArgs) Handles btnExportASC.Click
            If _recorder.TotalFrameCount = 0 Then
                MessageBox.Show("Yüklenmiş oturum yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using sfd As New SaveFileDialog()
                sfd.Filter = "ASC Dosyası (*.asc)|*.asc"
                sfd.FileName = Path.GetFileNameWithoutExtension(_currentFilePath) & ".asc"

                If sfd.ShowDialog() = DialogResult.OK Then
                    Try
                        _recorder.ExportToASC(sfd.FileName)
                        MessageBox.Show("ASC dışa aktarma başarılı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        ErrorHandler.Instance.LogError(ex, "SessionPlayerForm.BtnExportASC_Click")
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Sub CmbSpeed_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSpeed.SelectedIndexChanged
            If _isPlaying Then
                ' Oynatma devam ediyorsa hızı güncelle
                Dim speed = GetSelectedSpeed()
                _recorder.StopPlayback()
                _recorder.StartPlayback(speed)
            End If
        End Sub

        Private Sub BtnClearFilter_Click(sender As Object, e As EventArgs) Handles btnClearFilter.Click
            txtFilterId.Clear()
            _filteredFrameId = -1
        End Sub

        Private Sub TxtFilterId_TextChanged(sender As Object, e As EventArgs) Handles txtFilterId.TextChanged
            If String.IsNullOrWhiteSpace(txtFilterId.Text) Then
                _filteredFrameId = -1
            Else
                Integer.TryParse(txtFilterId.Text, Globalization.NumberStyles.HexNumber, Nothing, _filteredFrameId)
            End If
        End Sub
#End Region

#Region "Event Handlers - Seek"
        Private Sub TrackSeek_Scroll(sender As Object, e As EventArgs) Handles trackSeek.Scroll
            If _recorder.TotalFrameCount = 0 Then Return

            Dim frameNumber = CLng((trackSeek.Value / 100.0) * (_recorder.TotalFrameCount - 1))
            _recorder.SeekTo(frameNumber)
        End Sub

        Private Sub DgvFrames_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvFrames.CellDoubleClick
            If e.RowIndex < 0 OrElse e.RowIndex >= dgvFrames.Rows.Count Then Return

            Try
                Dim timeMs = CLng(dgvFrames.Rows(e.RowIndex).Cells("colTime").Value)
                ' Frame numarasını time'dan tahmin et (yaklaşık)
                Dim frameNumber = CLng((timeMs / 1000.0) * 100) ' Yaklaşık 100 frame/saniye varsayımı
                frameNumber = Math.Min(frameNumber, _recorder.TotalFrameCount - 1)

                _recorder.SeekTo(frameNumber)
                UpdateSeekBar()
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionPlayerForm.DgvFrames_CellDoubleClick")
            End Try
        End Sub
#End Region

#Region "Event Handlers - Recorder"
        Private Sub HandlePlaybackFrame(timestamp As Long, frameId As Integer, data As Byte())
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Sub() HandlePlaybackFrame(timestamp, frameId, data)))
                Return
            End If

            ' Filtreleme kontrolü
            If _filteredFrameId >= 0 AndAlso frameId <> _filteredFrameId Then
                Return
            End If

            ' DataGridView'a ekle (son 1000 frame'i tut)
            If dgvFrames.Rows.Count > 1000 Then
                dgvFrames.Rows.RemoveAt(0)
            End If

            Dim dataHex = BitConverter.ToString(data).Replace("-", " ")
            dgvFrames.Rows.Add(timestamp, $"{frameId:X3}", data.Length, dataHex)

            ' Son satıra scroll
            If dgvFrames.Rows.Count > 0 Then
                dgvFrames.FirstDisplayedScrollingRowIndex = dgvFrames.Rows.Count - 1
            End If
        End Sub

        Private Sub HandlePlaybackProgress(current As Long, total As Long)
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Sub() HandlePlaybackProgress(current, total)))
                Return
            End If

            UpdateSeekBar()
        End Sub

        Private Sub HandlePlaybackStarted()
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(AddressOf HandlePlaybackStarted))
                Return
            End If

            _isPlaying = True
            UpdateButtonStates()
        End Sub

        Private Sub HandlePlaybackStopped()
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(AddressOf HandlePlaybackStopped))
                Return
            End If

            _isPlaying = False
            _updateTimer.Stop()
            UpdateButtonStates()
        End Sub

        Private Sub HandleError(message As String)
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Sub() HandleError(message)))
                Return
            End If

            MessageBox.Show(message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Sub
#End Region

#Region "Private Methods"
        Private Sub LoadSession(filePath As String)
            Try
                _currentFilePath = filePath
                _recorder.LoadSession(filePath)

                ' Bilgileri güncelle
                Dim fileInfo As New FileInfo(filePath)
                lblRecordDate.Text = $"Kayıt Tarihi: {fileInfo.CreationTime:dd.MM.yyyy HH:mm:ss}"
                lblFileSize.Text = $"Dosya Boyutu: {FormatFileSize(fileInfo.Length)}"
                lblFrameCount.Text = $"Frame Sayısı: {_recorder.TotalFrameCount:N0}"

                ' Süreyi hesapla (yaklaşık)
                Dim durationSeconds = _recorder.TotalFrameCount / 100.0 ' Yaklaşık 100 frame/saniye
                Dim duration = TimeSpan.FromSeconds(durationSeconds)
                lblDuration.Text = $"Süre: {duration:hh\:mm\:ss}"
                lblTotalTime.Text = $"{duration:hh\:mm\:ss}"

                ' Seek bar'ı ayarla
                trackSeek.Maximum = 100
                trackSeek.Value = 0

                ' Frame listesini temizle
                dgvFrames.Rows.Clear()

                UpdateButtonStates()

                ErrorHandler.Instance.LogInfo($"Oturum yüklendi: {filePath}")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionPlayerForm.LoadSession")
                MessageBox.Show($"Oturum yüklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub UpdateButtonStates()
            Dim hasSession = _recorder.TotalFrameCount > 0

            btnPlay.Enabled = hasSession AndAlso Not _isPlaying
            btnPause.Enabled = _isPlaying
            btnStop.Enabled = _isPlaying
            btnRewind.Enabled = hasSession
            btnFastForward.Enabled = hasSession
            cmbSpeed.Enabled = hasSession
            trackSeek.Enabled = hasSession
            btnExportCSV.Enabled = hasSession
            btnExportASC.Enabled = hasSession
        End Sub

        Private Sub UpdateSeekBar()
            If _recorder.TotalFrameCount = 0 Then Return

            Dim progress = (_recorder.CurrentPlaybackFrame / CDbl(_recorder.TotalFrameCount)) * 100
            trackSeek.Value = CInt(Math.Min(100, Math.Max(0, progress)))

            ' Geçen süreyi güncelle
            Dim currentSeconds = _recorder.CurrentPlaybackFrame / 100.0
            Dim currentTime = TimeSpan.FromSeconds(currentSeconds)
            lblCurrentTime.Text = $"{currentTime:hh\:mm\:ss}"
        End Sub

        Private Sub UpdateTimerTick(sender As Object, e As EventArgs)
            If _isPlaying Then
                UpdateSeekBar()
            End If
        End Sub

        Private Function GetSelectedSpeed() As Double
            Select Case cmbSpeed.SelectedIndex
                Case 0 : Return 0.25
                Case 1 : Return 0.5
                Case 2 : Return 1.0
                Case 3 : Return 2.0
                Case 4 : Return 4.0
                Case 5 : Return 10.0
                Case Else : Return 1.0
            End Select
        End Function

        Private Function FormatFileSize(bytes As Long) As String
            If bytes < 1024 Then
                Return $"{bytes} B"
            ElseIf bytes < 1024 * 1024 Then
                Return $"{bytes / 1024.0:F2} KB"
            ElseIf bytes < 1024 * 1024 * 1024 Then
                Return $"{bytes / (1024.0 * 1024.0):F2} MB"
            Else
                Return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
            End If
        End Function
#End Region

#Region "Form Events"
        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            MyBase.OnFormClosing(e)

            If _isPlaying Then
                _recorder.StopPlayback()
            End If

            If _updateTimer IsNot Nothing Then
                _updateTimer.Stop()
                _updateTimer.Dispose()
            End If

            If _recorder IsNot Nothing Then
                _recorder.Dispose()
            End If
        End Sub
#End Region
    End Class
End Namespace

