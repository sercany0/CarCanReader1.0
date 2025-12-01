' IsoTpTestForm.vb
' ISO-TP protokol test aracı
' Multi-frame mesaj gönderme/alma testi için

Imports System.Text
Imports System.Threading.Tasks
Imports CarCanReader1._0.Services

Public Class IsoTpTestForm
    Inherits Form

    ' Kontroller
    Private lblEcuId As Label
    Private txtEcuId As TextBox
    Private lblData As Label
    Private txtData As TextBox
    Private btnSend As Button
    Private grpTestCommands As GroupBox
    Private btnTestVIN As Button
    Private btnTestCalibrationID As Button
    Private btnTestDTC As Button
    Private btnTestECUName As Button
    Private grpSentFrames As GroupBox
    Private lstSentFrames As ListBox
    Private grpReceivedFrames As GroupBox
    Private lstReceivedFrames As ListBox
    Private grpResponse As GroupBox
    Private txtResponseHex As TextBox
    Private txtResponseAscii As TextBox
    Private prgProgress As ProgressBar
    Private lblProgress As Label
    Private btnClear As Button
    Private btnClose As Button

    ' Servisler
    Private _canSender As CANSender
    Private _canParser As CANParser
    Private _isoTpHandler As IsoTpHandler

    ' Yanıt bekleme
    Private _responseReceived As Boolean = False
    Private _responseData As Byte() = Nothing
    Private ReadOnly _responseLock As New Object()
    Private ReadOnly _responseEvent As New Threading.ManualResetEventSlim(False)

    ' Frame listeleri
    Private _sentFrames As New List(Of String)
    Private _receivedFrames As New List(Of String)

    Public Sub New(canSender As CANSender, canParser As CANParser)
        _canSender = canSender
        _canParser = canParser
        InitializeComponent()
        InitializeIsoTpHandler()
        InitializeFrameListener()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "ISO-TP Test Aracı"
        Me.Size = New Size(1000, 750)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.BackColor = Color.FromArgb(245, 245, 250)
        Me.Font = New Font("Segoe UI", 9.0!)

        ' ECU ID
        lblEcuId = New Label()
        lblEcuId.Text = "ECU ID (Hex):"
        lblEcuId.Location = New Point(15, 15)
        lblEcuId.Size = New Size(100, 25)
        lblEcuId.AutoSize = True

        txtEcuId = New TextBox()
        txtEcuId.Location = New Point(120, 12)
        txtEcuId.Size = New Size(120, 25)
        txtEcuId.Text = "7DF"
        txtEcuId.Font = New Font("Consolas", 10.0!)

        ' Data
        lblData = New Label()
        lblData.Text = "Data (Hex):"
        lblData.Location = New Point(15, 50)
        lblData.Size = New Size(100, 25)
        lblData.AutoSize = True

        txtData = New TextBox()
        txtData.Location = New Point(120, 47)
        txtData.Size = New Size(400, 25)
        txtData.Font = New Font("Consolas", 10.0!)
        txtData.Text = "020902"

        ' Gönder butonu
        btnSend = New Button()
        btnSend.Text = "📤 Gönder"
        btnSend.Location = New Point(530, 12)
        btnSend.Size = New Size(120, 60)
        btnSend.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        btnSend.BackColor = Color.FromArgb(46, 204, 113)
        btnSend.ForeColor = Color.White
        btnSend.FlatStyle = FlatStyle.Flat
        btnSend.Cursor = Cursors.Hand
        AddHandler btnSend.Click, AddressOf BtnSend_Click

        ' Test komutları grubu
        grpTestCommands = New GroupBox()
        grpTestCommands.Text = "🧪 Hazır Test Komutları"
        grpTestCommands.Location = New Point(15, 85)
        grpTestCommands.Size = New Size(635, 80)
        grpTestCommands.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)

        btnTestVIN = New Button()
        btnTestVIN.Text = "VIN Oku (09 02)"
        btnTestVIN.Location = New Point(10, 25)
        btnTestVIN.Size = New Size(140, 40)
        btnTestVIN.Font = New Font("Segoe UI", 9.0!)
        btnTestVIN.BackColor = Color.FromArgb(52, 152, 219)
        btnTestVIN.ForeColor = Color.White
        btnTestVIN.FlatStyle = FlatStyle.Flat
        AddHandler btnTestVIN.Click, AddressOf BtnTestVIN_Click

        btnTestCalibrationID = New Button()
        btnTestCalibrationID.Text = "Calibration ID (09 04)"
        btnTestCalibrationID.Location = New Point(160, 25)
        btnTestCalibrationID.Size = New Size(140, 40)
        btnTestCalibrationID.Font = New Font("Segoe UI", 9.0!)
        btnTestCalibrationID.BackColor = Color.FromArgb(52, 152, 219)
        btnTestCalibrationID.ForeColor = Color.White
        btnTestCalibrationID.FlatStyle = FlatStyle.Flat
        AddHandler btnTestCalibrationID.Click, AddressOf BtnTestCalibrationID_Click

        btnTestDTC = New Button()
        btnTestDTC.Text = "Tüm DTC'ler (03)"
        btnTestDTC.Location = New Point(310, 25)
        btnTestDTC.Size = New Size(140, 40)
        btnTestDTC.Font = New Font("Segoe UI", 9.0!)
        btnTestDTC.BackColor = Color.FromArgb(52, 152, 219)
        btnTestDTC.ForeColor = Color.White
        btnTestDTC.FlatStyle = FlatStyle.Flat
        AddHandler btnTestDTC.Click, AddressOf BtnTestDTC_Click

        btnTestECUName = New Button()
        btnTestECUName.Text = "ECU Bilgisi (09 0A)"
        btnTestECUName.Location = New Point(460, 25)
        btnTestECUName.Size = New Size(140, 40)
        btnTestECUName.Font = New Font("Segoe UI", 9.0!)
        btnTestECUName.BackColor = Color.FromArgb(52, 152, 219)
        btnTestECUName.ForeColor = Color.White
        btnTestECUName.FlatStyle = FlatStyle.Flat
        AddHandler btnTestECUName.Click, AddressOf BtnTestECUName_Click

        grpTestCommands.Controls.Add(btnTestVIN)
        grpTestCommands.Controls.Add(btnTestCalibrationID)
        grpTestCommands.Controls.Add(btnTestDTC)
        grpTestCommands.Controls.Add(btnTestECUName)

        ' Progress
        prgProgress = New ProgressBar()
        prgProgress.Location = New Point(15, 175)
        prgProgress.Size = New Size(635, 25)
        prgProgress.Style = ProgressBarStyle.Continuous

        lblProgress = New Label()
        lblProgress.Text = "Hazır"
        lblProgress.Location = New Point(15, 205)
        lblProgress.Size = New Size(635, 20)
        lblProgress.ForeColor = Color.FromArgb(100, 100, 120)

        ' Gönderilen frame'ler
        grpSentFrames = New GroupBox()
        grpSentFrames.Text = "📤 Gönderilen Frame'ler"
        grpSentFrames.Location = New Point(15, 235)
        grpSentFrames.Size = New Size(310, 200)
        grpSentFrames.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)

        lstSentFrames = New ListBox()
        lstSentFrames.Location = New Point(10, 25)
        lstSentFrames.Size = New Size(290, 165)
        lstSentFrames.Font = New Font("Consolas", 9.0!)
        lstSentFrames.BackColor = Color.FromArgb(30, 30, 40)
        lstSentFrames.ForeColor = Color.FromArgb(0, 255, 128)
        grpSentFrames.Controls.Add(lstSentFrames)

        ' Alınan frame'ler
        grpReceivedFrames = New GroupBox()
        grpReceivedFrames.Text = "📥 Alınan Frame'ler"
        grpReceivedFrames.Location = New Point(340, 235)
        grpReceivedFrames.Size = New Size(310, 200)
        grpReceivedFrames.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)

        lstReceivedFrames = New ListBox()
        lstReceivedFrames.Location = New Point(10, 25)
        lstReceivedFrames.Size = New Size(290, 165)
        lstReceivedFrames.Font = New Font("Consolas", 9.0!)
        lstReceivedFrames.BackColor = Color.FromArgb(30, 30, 40)
        lstReceivedFrames.ForeColor = Color.FromArgb(0, 200, 255)
        grpReceivedFrames.Controls.Add(lstReceivedFrames)

        ' Yanıt
        grpResponse = New GroupBox()
        grpResponse.Text = "📋 Birleştirilmiş Yanıt"
        grpResponse.Location = New Point(15, 445)
        grpResponse.Size = New Size(635, 200)
        grpResponse.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)

        Dim lblHex As New Label()
        lblHex.Text = "Hex:"
        lblHex.Location = New Point(10, 25)
        lblHex.Size = New Size(50, 20)

        txtResponseHex = New TextBox()
        txtResponseHex.Location = New Point(10, 50)
        txtResponseHex.Size = New Size(615, 60)
        txtResponseHex.Multiline = True
        txtResponseHex.ScrollBars = ScrollBars.Vertical
        txtResponseHex.Font = New Font("Consolas", 9.0!)
        txtResponseHex.BackColor = Color.FromArgb(30, 30, 40)
        txtResponseHex.ForeColor = Color.White
        txtResponseHex.ReadOnly = True

        Dim lblAscii As New Label()
        lblAscii.Text = "ASCII:"
        lblAscii.Location = New Point(10, 120)
        lblAscii.Size = New Size(50, 20)

        txtResponseAscii = New TextBox()
        txtResponseAscii.Location = New Point(10, 145)
        txtResponseAscii.Size = New Size(615, 45)
        txtResponseAscii.Multiline = True
        txtResponseAscii.Font = New Font("Consolas", 9.0!)
        txtResponseAscii.BackColor = Color.FromArgb(30, 30, 40)
        txtResponseAscii.ForeColor = Color.White
        txtResponseAscii.ReadOnly = True

        grpResponse.Controls.Add(lblHex)
        grpResponse.Controls.Add(txtResponseHex)
        grpResponse.Controls.Add(lblAscii)
        grpResponse.Controls.Add(txtResponseAscii)

        ' Butonlar
        btnClear = New Button()
        btnClear.Text = "🗑️ Temizle"
        btnClear.Location = New Point(470, 655)
        btnClear.Size = New Size(100, 35)
        btnClear.Font = New Font("Segoe UI", 9.0!)
        btnClear.BackColor = Color.FromArgb(149, 165, 166)
        btnClear.ForeColor = Color.White
        btnClear.FlatStyle = FlatStyle.Flat
        AddHandler btnClear.Click, AddressOf BtnClear_Click

        btnClose = New Button()
        btnClose.Text = "Kapat"
        btnClose.Location = New Point(580, 655)
        btnClose.Size = New Size(70, 35)
        btnClose.Font = New Font("Segoe UI", 9.0!)
        btnClose.BackColor = Color.FromArgb(100, 100, 120)
        btnClose.ForeColor = Color.White
        btnClose.FlatStyle = FlatStyle.Flat
        AddHandler btnClose.Click, Sub() Me.Close()

        ' Form'a ekle
        Me.Controls.Add(lblEcuId)
        Me.Controls.Add(txtEcuId)
        Me.Controls.Add(lblData)
        Me.Controls.Add(txtData)
        Me.Controls.Add(btnSend)
        Me.Controls.Add(grpTestCommands)
        Me.Controls.Add(prgProgress)
        Me.Controls.Add(lblProgress)
        Me.Controls.Add(grpSentFrames)
        Me.Controls.Add(grpReceivedFrames)
        Me.Controls.Add(grpResponse)
        Me.Controls.Add(btnClear)
        Me.Controls.Add(btnClose)

        Me.AcceptButton = btnSend
        Me.CancelButton = btnClose
    End Sub

    Private Sub InitializeIsoTpHandler()
        If _canSender IsNot Nothing Then
            _isoTpHandler = New IsoTpHandler(_canSender)
            _isoTpHandler.Timeout = 5000
            AddHandler _isoTpHandler.OnMessageReceived, AddressOf HandleIsoTpMessageReceived
            AddHandler _isoTpHandler.OnError, AddressOf HandleIsoTpError
            AddHandler _isoTpHandler.OnProgress, AddressOf HandleIsoTpProgress
            AddHandler _isoTpHandler.OnMessageSent, AddressOf HandleIsoTpMessageSent
        End If
    End Sub

    Private Sub InitializeFrameListener()
        ' CANParser'da OnFrameReceived event'i yok
        ' Frame'ler MainForm'dan ProcessSlcanFrame üzerinden gelir
        ' Bu form şimdilik sadece gönderme yapar, almayı MainForm yönetir
    End Sub

    Private Sub HandleIsoTpMessageSent(targetId As Integer, success As Boolean)
        ' Gönderim tamamlandı
        If Me.InvokeRequired Then
            Me.Invoke(Sub() lblProgress.Text = If(success, "✅ Gönderim tamamlandı", "❌ Gönderim başarısız"))
        Else
            lblProgress.Text = If(success, "✅ Gönderim tamamlandı", "❌ Gönderim başarısız")
        End If
    End Sub

    Private Sub HandleIsoTpMessageReceived(sourceId As Integer, data As Byte())
        SyncLock _responseLock
            _responseData = data
            _responseReceived = True
            _responseEvent.Set()
        End SyncLock

        ' UI güncelle
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateResponseUI(data))
        Else
            UpdateResponseUI(data)
        End If
    End Sub

    Private Sub HandleIsoTpError(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() lblProgress.Text = $"❌ Hata: {message}")
        Else
            lblProgress.Text = $"❌ Hata: {message}"
        End If

        SyncLock _responseLock
            _responseReceived = False
            _responseData = Nothing
            _responseEvent.Set()
        End SyncLock
    End Sub

    Private Sub HandleIsoTpProgress(current As Integer, total As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub()
                         prgProgress.Maximum = total
                         prgProgress.Value = current
                         lblProgress.Text = $"📊 İlerleme: {current}/{total} byte ({current * 100 \ total}%)"
                     End Sub)
        Else
            prgProgress.Maximum = total
            prgProgress.Value = current
            lblProgress.Text = $"📊 İlerleme: {current}/{total} byte ({current * 100 \ total}%)"
        End If
    End Sub

    Private Sub BtnSend_Click(sender As Object, e As EventArgs)
        Try
            ' ECU ID parse et
            Dim ecuIdHex As String = txtEcuId.Text.Trim().Replace("0x", "").Replace("&H", "")
            Dim ecuId As Integer
            If Not Integer.TryParse(ecuIdHex, Globalization.NumberStyles.HexNumber, Nothing, ecuId) Then
                MessageBox.Show("Geçersiz ECU ID formatı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Data parse et
            Dim dataHex As String = txtData.Text.Trim().Replace(" ", "").Replace("-", "").Replace(":", "")
            If dataHex.Length Mod 2 <> 0 Then
                MessageBox.Show("Data hex string çift sayıda karakter olmalı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Dim data As New List(Of Byte)
            For i As Integer = 0 To dataHex.Length - 1 Step 2
                Dim byteStr As String = dataHex.Substring(i, 2)
                Dim byteVal As Byte
                If Byte.TryParse(byteStr, Globalization.NumberStyles.HexNumber, Nothing, byteVal) Then
                    data.Add(byteVal)
                End If
            Next

            If data.Count = 0 Then
                MessageBox.Show("Geçersiz data formatı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Gönder
            SendIsoTpMessage(ecuId, data.ToArray())

        Catch ex As Exception
            MessageBox.Show($"Gönderme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub SendIsoTpMessage(ecuId As Integer, data As Byte())
        Try
            btnSend.Enabled = False
            prgProgress.Value = 0
            lblProgress.Text = "⏳ Gönderiliyor..."

            ' Gönderilen frame'i logla
            Dim sentFrame As String = $"[{DateTime.Now:HH:mm:ss.fff}] ECU: 0x{ecuId:X3}, Data: {BitConverter.ToString(data).Replace("-", " ")}"
            _sentFrames.Add(sentFrame)
            If lstSentFrames.InvokeRequired Then
                lstSentFrames.Invoke(Sub() lstSentFrames.Items.Add(sentFrame))
            Else
                lstSentFrames.Items.Add(sentFrame)
            End If

            ' ISO-TP ile gönder
            SyncLock _responseLock
                _responseReceived = False
                _responseData = Nothing
                _responseEvent.Reset()
            End SyncLock

            Dim success As Boolean = Await _isoTpHandler.SendMessage(ecuId, data)

            If success Then
                ' Yanıt bekle
                Dim received As Boolean = _responseEvent.Wait(5000)

                If received AndAlso _responseReceived AndAlso _responseData IsNot Nothing Then
                    lblProgress.Text = $"✅ Yanıt alındı: {_responseData.Length} byte"
                Else
                    lblProgress.Text = "⏱️ Timeout - Yanıt alınamadı"
                End If
            Else
                lblProgress.Text = "❌ Gönderme başarısız"
            End If

        Catch ex As Exception
            lblProgress.Text = $"❌ Hata: {ex.Message}"
        Finally
            btnSend.Enabled = True
        End Try
    End Sub

    Private Sub UpdateResponseUI(data As Byte())
        Try
            ' Hex göster
            Dim hexStr As String = BitConverter.ToString(data).Replace("-", " ")
            txtResponseHex.Text = hexStr

            ' ASCII göster
            Dim asciiStr As New StringBuilder()
            For Each b As Byte In data
                If b >= 32 AndAlso b <= 126 Then
                    asciiStr.Append(Chr(b))
                Else
                    asciiStr.Append(".")
                End If
            Next
            txtResponseAscii.Text = asciiStr.ToString()

            ' Alınan frame'i logla
            Dim receivedFrame As String = $"[{DateTime.Now:HH:mm:ss.fff}] {data.Length} byte: {hexStr}"
            _receivedFrames.Add(receivedFrame)
            If lstReceivedFrames.InvokeRequired Then
                lstReceivedFrames.Invoke(Sub() lstReceivedFrames.Items.Add(receivedFrame))
            Else
                lstReceivedFrames.Items.Add(receivedFrame)
            End If

        Catch ex As Exception
            MessageBox.Show($"UI güncelleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnTestVIN_Click(sender As Object, e As EventArgs)
        txtEcuId.Text = "7DF"
        txtData.Text = "020902"
        BtnSend_Click(sender, e)
    End Sub

    Private Sub BtnTestCalibrationID_Click(sender As Object, e As EventArgs)
        txtEcuId.Text = "7DF"
        txtData.Text = "020904"
        BtnSend_Click(sender, e)
    End Sub

    Private Sub BtnTestDTC_Click(sender As Object, e As EventArgs)
        txtEcuId.Text = "7DF"
        txtData.Text = "0103"
        BtnSend_Click(sender, e)
    End Sub

    Private Sub BtnTestECUName_Click(sender As Object, e As EventArgs)
        txtEcuId.Text = "7DF"
        txtData.Text = "02090A"
        BtnSend_Click(sender, e)
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs)
        lstSentFrames.Items.Clear()
        lstReceivedFrames.Items.Clear()
        txtResponseHex.Clear()
        txtResponseAscii.Clear()
        prgProgress.Value = 0
        lblProgress.Text = "Hazır"
        _sentFrames.Clear()
        _receivedFrames.Clear()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        Try
            If _isoTpHandler IsNot Nothing Then
                RemoveHandler _isoTpHandler.OnMessageReceived, AddressOf HandleIsoTpMessageReceived
                RemoveHandler _isoTpHandler.OnError, AddressOf HandleIsoTpError
                RemoveHandler _isoTpHandler.OnProgress, AddressOf HandleIsoTpProgress
                RemoveHandler _isoTpHandler.OnMessageSent, AddressOf HandleIsoTpMessageSent
                _isoTpHandler.Dispose()
            End If
            If _responseEvent IsNot Nothing Then
                _responseEvent.Dispose()
            End If
        Catch
        End Try
        MyBase.OnFormClosing(e)
    End Sub

End Class

