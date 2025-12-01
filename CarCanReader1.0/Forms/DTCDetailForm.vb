' DTCDetailForm.vb
' DTC Detay Penceresi - Arıza kodu hakkında detaylı bilgi gösterir

Imports System.Windows.Forms
Imports System.Drawing
Imports CarCanReader1._0.Models

Public Class DTCDetailForm
    Inherits Form

    Private _dtcInfo As DTCInfo
    Private lblCode As Label
    Private lblDescription As Label
    Private lblCategory As Label
    Private lblSeverity As Label
    Private lblStatus As Label
    Private grpSymptoms As GroupBox
    Private lstSymptoms As ListBox
    Private grpCauses As GroupBox
    Private lstCauses As ListBox
    Private grpSolutions As GroupBox
    Private lstSolutions As ListBox
    Private btnCopy As Button
    Private btnSearchInternet As Button
    Private btnClose As Button
    Private pnlHeader As Panel
    Private pnlSeverityIndicator As Panel

    Public Sub New(dtcInfo As DTCInfo)
        _dtcInfo = dtcInfo
        InitializeComponent()
        LoadDTCData()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = $"DTC Detay - {_dtcInfo.Code}"
        Me.Size = New Size(650, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.FromArgb(25, 25, 35)
        Me.Font = New Font("Segoe UI", 9.0!)

        ' Header Panel
        pnlHeader = New Panel()
        pnlHeader.Location = New Point(0, 0)
        pnlHeader.Size = New Size(650, 120)
        pnlHeader.BackColor = Color.FromArgb(35, 35, 50)

        ' Severity Indicator
        pnlSeverityIndicator = New Panel()
        pnlSeverityIndicator.Location = New Point(0, 0)
        pnlSeverityIndicator.Size = New Size(8, 120)
        pnlSeverityIndicator.BackColor = GetSeverityColor(_dtcInfo.Severity)
        pnlHeader.Controls.Add(pnlSeverityIndicator)

        ' DTC Code Label
        lblCode = New Label()
        lblCode.Text = _dtcInfo.Code
        lblCode.Location = New Point(25, 15)
        lblCode.Size = New Size(200, 45)
        lblCode.Font = New Font("Consolas", 28.0!, FontStyle.Bold)
        lblCode.ForeColor = GetSeverityColor(_dtcInfo.Severity)
        pnlHeader.Controls.Add(lblCode)

        ' Category Label
        lblCategory = New Label()
        lblCategory.Text = GetCategoryText(_dtcInfo.Category)
        lblCategory.Location = New Point(230, 20)
        lblCategory.Size = New Size(180, 25)
        lblCategory.Font = New Font("Segoe UI", 11.0!, FontStyle.Bold)
        lblCategory.ForeColor = Color.FromArgb(150, 150, 170)
        pnlHeader.Controls.Add(lblCategory)

        ' Severity Label
        lblSeverity = New Label()
        lblSeverity.Text = GetSeverityText(_dtcInfo.Severity)
        lblSeverity.Location = New Point(230, 45)
        lblSeverity.Size = New Size(180, 25)
        lblSeverity.Font = New Font("Segoe UI", 10.0!)
        lblSeverity.ForeColor = GetSeverityColor(_dtcInfo.Severity)
        pnlHeader.Controls.Add(lblSeverity)

        ' Status Label
        lblStatus = New Label()
        If _dtcInfo.IsPending Then
            lblStatus.Text = "⏳ Beklemede"
        ElseIf _dtcInfo.IsStored Then
            lblStatus.Text = "💾 Kayıtlı"
        ElseIf _dtcInfo.IsPermanent Then
            lblStatus.Text = "🔒 Kalıcı"
        Else
            lblStatus.Text = ""
        End If
        lblStatus.Location = New Point(420, 20)
        lblStatus.Size = New Size(200, 25)
        lblStatus.Font = New Font("Segoe UI", 10.0!)
        lblStatus.ForeColor = Color.FromArgb(200, 200, 220)
        pnlHeader.Controls.Add(lblStatus)

        ' Description Label
        lblDescription = New Label()
        lblDescription.Text = If(String.IsNullOrEmpty(_dtcInfo.DescriptionTR), _dtcInfo.DescriptionEN, _dtcInfo.DescriptionTR)
        lblDescription.Location = New Point(25, 70)
        lblDescription.Size = New Size(590, 40)
        lblDescription.Font = New Font("Segoe UI", 11.0!)
        lblDescription.ForeColor = Color.White
        pnlHeader.Controls.Add(lblDescription)

        Me.Controls.Add(pnlHeader)

        ' Symptoms Group
        grpSymptoms = New GroupBox()
        grpSymptoms.Text = "🔍 Belirtiler"
        grpSymptoms.Location = New Point(15, 135)
        grpSymptoms.Size = New Size(295, 150)
        grpSymptoms.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        grpSymptoms.ForeColor = Color.FromArgb(0, 200, 180)
        grpSymptoms.BackColor = Color.Transparent

        lstSymptoms = New ListBox()
        lstSymptoms.Location = New Point(10, 25)
        lstSymptoms.Size = New Size(275, 115)
        lstSymptoms.BackColor = Color.FromArgb(35, 35, 50)
        lstSymptoms.ForeColor = Color.FromArgb(220, 220, 240)
        lstSymptoms.BorderStyle = BorderStyle.None
        lstSymptoms.Font = New Font("Segoe UI", 9.5!)
        grpSymptoms.Controls.Add(lstSymptoms)
        Me.Controls.Add(grpSymptoms)

        ' Causes Group
        grpCauses = New GroupBox()
        grpCauses.Text = "⚠️ Olası Nedenler"
        grpCauses.Location = New Point(325, 135)
        grpCauses.Size = New Size(295, 150)
        grpCauses.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        grpCauses.ForeColor = Color.FromArgb(255, 180, 0)
        grpCauses.BackColor = Color.Transparent

        lstCauses = New ListBox()
        lstCauses.Location = New Point(10, 25)
        lstCauses.Size = New Size(275, 115)
        lstCauses.BackColor = Color.FromArgb(35, 35, 50)
        lstCauses.ForeColor = Color.FromArgb(220, 220, 240)
        lstCauses.BorderStyle = BorderStyle.None
        lstCauses.Font = New Font("Segoe UI", 9.5!)
        grpCauses.Controls.Add(lstCauses)
        Me.Controls.Add(grpCauses)

        ' Solutions Group
        grpSolutions = New GroupBox()
        grpSolutions.Text = "✅ Çözüm Önerileri"
        grpSolutions.Location = New Point(15, 295)
        grpSolutions.Size = New Size(605, 180)
        grpSolutions.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        grpSolutions.ForeColor = Color.FromArgb(100, 220, 100)
        grpSolutions.BackColor = Color.Transparent

        lstSolutions = New ListBox()
        lstSolutions.Location = New Point(10, 25)
        lstSolutions.Size = New Size(585, 145)
        lstSolutions.BackColor = Color.FromArgb(35, 35, 50)
        lstSolutions.ForeColor = Color.FromArgb(220, 220, 240)
        lstSolutions.BorderStyle = BorderStyle.None
        lstSolutions.Font = New Font("Segoe UI", 9.5!)
        grpSolutions.Controls.Add(lstSolutions)
        Me.Controls.Add(grpSolutions)

        ' Additional Info Panel
        Dim pnlInfo As New Panel()
        pnlInfo.Location = New Point(15, 485)
        pnlInfo.Size = New Size(605, 110)
        pnlInfo.BackColor = Color.FromArgb(35, 35, 50)
        pnlInfo.BorderStyle = BorderStyle.FixedSingle

        ' Subcategory
        Dim lblSubcatTitle As New Label()
        lblSubcatTitle.Text = "Alt Kategori:"
        lblSubcatTitle.Location = New Point(15, 15)
        lblSubcatTitle.Size = New Size(100, 20)
        lblSubcatTitle.ForeColor = Color.FromArgb(150, 150, 170)
        pnlInfo.Controls.Add(lblSubcatTitle)

        Dim lblSubcatValue As New Label()
        lblSubcatValue.Text = If(String.IsNullOrEmpty(_dtcInfo.Subcategory), "-", _dtcInfo.Subcategory)
        lblSubcatValue.Location = New Point(120, 15)
        lblSubcatValue.Size = New Size(200, 20)
        lblSubcatValue.ForeColor = Color.White
        lblSubcatValue.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        pnlInfo.Controls.Add(lblSubcatValue)

        ' English Description
        Dim lblEngTitle As New Label()
        lblEngTitle.Text = "English:"
        lblEngTitle.Location = New Point(15, 45)
        lblEngTitle.Size = New Size(100, 20)
        lblEngTitle.ForeColor = Color.FromArgb(150, 150, 170)
        pnlInfo.Controls.Add(lblEngTitle)

        Dim lblEngValue As New Label()
        lblEngValue.Text = If(String.IsNullOrEmpty(_dtcInfo.DescriptionEN), "-", _dtcInfo.DescriptionEN)
        lblEngValue.Location = New Point(120, 45)
        lblEngValue.Size = New Size(470, 40)
        lblEngValue.ForeColor = Color.FromArgb(200, 200, 220)
        pnlInfo.Controls.Add(lblEngValue)

        ' Detection Count
        If _dtcInfo.DetectionCount > 0 Then
            Dim lblCountTitle As New Label()
            lblCountTitle.Text = "Algılama Sayısı:"
            lblCountTitle.Location = New Point(340, 15)
            lblCountTitle.Size = New Size(120, 20)
            lblCountTitle.ForeColor = Color.FromArgb(150, 150, 170)
            pnlInfo.Controls.Add(lblCountTitle)

            Dim lblCountValue As New Label()
            lblCountValue.Text = _dtcInfo.DetectionCount.ToString()
            lblCountValue.Location = New Point(465, 15)
            lblCountValue.Size = New Size(50, 20)
            lblCountValue.ForeColor = Color.White
            lblCountValue.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            pnlInfo.Controls.Add(lblCountValue)
        End If

        Me.Controls.Add(pnlInfo)

        ' Buttons Panel
        Dim pnlButtons As New Panel()
        pnlButtons.Location = New Point(0, 605)
        pnlButtons.Size = New Size(650, 60)
        pnlButtons.BackColor = Color.FromArgb(35, 35, 50)

        ' Copy Button
        btnCopy = New Button()
        btnCopy.Text = "📋 Kopyala"
        btnCopy.Location = New Point(15, 12)
        btnCopy.Size = New Size(130, 38)
        btnCopy.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        btnCopy.BackColor = Color.FromArgb(52, 152, 219)
        btnCopy.ForeColor = Color.White
        btnCopy.FlatStyle = FlatStyle.Flat
        btnCopy.FlatAppearance.BorderSize = 0
        btnCopy.Cursor = Cursors.Hand
        AddHandler btnCopy.Click, AddressOf BtnCopy_Click
        pnlButtons.Controls.Add(btnCopy)

        ' Search Internet Button
        btnSearchInternet = New Button()
        btnSearchInternet.Text = "🌐 İnternette Ara"
        btnSearchInternet.Location = New Point(160, 12)
        btnSearchInternet.Size = New Size(160, 38)
        btnSearchInternet.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        btnSearchInternet.BackColor = Color.FromArgb(155, 89, 182)
        btnSearchInternet.ForeColor = Color.White
        btnSearchInternet.FlatStyle = FlatStyle.Flat
        btnSearchInternet.FlatAppearance.BorderSize = 0
        btnSearchInternet.Cursor = Cursors.Hand
        AddHandler btnSearchInternet.Click, AddressOf BtnSearchInternet_Click
        pnlButtons.Controls.Add(btnSearchInternet)

        ' Close Button
        btnClose = New Button()
        btnClose.Text = "Kapat"
        btnClose.Location = New Point(500, 12)
        btnClose.Size = New Size(120, 38)
        btnClose.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        btnClose.BackColor = Color.FromArgb(100, 100, 120)
        btnClose.ForeColor = Color.White
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.FlatAppearance.BorderSize = 0
        btnClose.Cursor = Cursors.Hand
        AddHandler btnClose.Click, Sub() Me.Close()
        pnlButtons.Controls.Add(btnClose)

        Me.Controls.Add(pnlButtons)

        ' Accept/Cancel buttons
        Me.AcceptButton = btnClose
        Me.CancelButton = btnClose
    End Sub

    Private Sub LoadDTCData()
        ' Load symptoms
        lstSymptoms.Items.Clear()
        If _dtcInfo.Symptoms IsNot Nothing AndAlso _dtcInfo.Symptoms.Count > 0 Then
            For Each symptom In _dtcInfo.Symptoms
                lstSymptoms.Items.Add("• " & symptom)
            Next
        Else
            lstSymptoms.Items.Add("• Belirti bilgisi mevcut değil")
        End If

        ' Load causes
        lstCauses.Items.Clear()
        If _dtcInfo.Causes IsNot Nothing AndAlso _dtcInfo.Causes.Count > 0 Then
            For Each cause In _dtcInfo.Causes
                lstCauses.Items.Add("• " & cause)
            Next
        Else
            lstCauses.Items.Add("• Neden bilgisi mevcut değil")
        End If

        ' Load solutions
        lstSolutions.Items.Clear()
        If _dtcInfo.Solutions IsNot Nothing AndAlso _dtcInfo.Solutions.Count > 0 Then
            Dim i As Integer = 1
            For Each solution In _dtcInfo.Solutions
                lstSolutions.Items.Add($"{i}. {solution}")
                i += 1
            Next
        Else
            lstSolutions.Items.Add("1. Yetkili servise başvurun")
        End If
    End Sub

    Private Sub BtnCopy_Click(sender As Object, e As EventArgs)
        Try
            Dim text As New System.Text.StringBuilder()
            text.AppendLine($"DTC Kodu: {_dtcInfo.Code}")
            text.AppendLine($"Açıklama: {If(String.IsNullOrEmpty(_dtcInfo.DescriptionTR), _dtcInfo.DescriptionEN, _dtcInfo.DescriptionTR)}")
            text.AppendLine($"Kategori: {GetCategoryText(_dtcInfo.Category)}")
            text.AppendLine($"Şiddet: {GetSeverityText(_dtcInfo.Severity)}")
            text.AppendLine()

            If _dtcInfo.Symptoms IsNot Nothing AndAlso _dtcInfo.Symptoms.Count > 0 Then
                text.AppendLine("Belirtiler:")
                For Each symptom In _dtcInfo.Symptoms
                    text.AppendLine($"  - {symptom}")
                Next
                text.AppendLine()
            End If

            If _dtcInfo.Causes IsNot Nothing AndAlso _dtcInfo.Causes.Count > 0 Then
                text.AppendLine("Olası Nedenler:")
                For Each cause In _dtcInfo.Causes
                    text.AppendLine($"  - {cause}")
                Next
                text.AppendLine()
            End If

            If _dtcInfo.Solutions IsNot Nothing AndAlso _dtcInfo.Solutions.Count > 0 Then
                text.AppendLine("Çözüm Önerileri:")
                Dim i As Integer = 1
                For Each solution In _dtcInfo.Solutions
                    text.AppendLine($"  {i}. {solution}")
                    i += 1
                Next
            End If

            Clipboard.SetText(text.ToString())
            MessageBox.Show("DTC bilgileri panoya kopyalandı!", "Kopyalandı",
                          MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Kopyalama sırasında hata oluştu: " & ex.Message, "Hata",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnSearchInternet_Click(sender As Object, e As EventArgs)
        Try
            Dim searchQuery As String = $"{_dtcInfo.Code} {If(String.IsNullOrEmpty(_dtcInfo.DescriptionEN), _dtcInfo.DescriptionTR, _dtcInfo.DescriptionEN)}"
            Dim url As String = $"https://www.google.com/search?q={Uri.EscapeDataString(searchQuery)}"
            Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("Tarayıcı açılırken hata oluştu: " & ex.Message, "Hata",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetSeverityColor(severity As String) As Color
        Select Case severity?.ToLower()
            Case "critical"
                Return Color.FromArgb(231, 76, 60)  ' Red
            Case "high"
                Return Color.FromArgb(230, 126, 34) ' Orange
            Case "medium"
                Return Color.FromArgb(241, 196, 15) ' Yellow
            Case "low"
                Return Color.FromArgb(46, 204, 113) ' Green
            Case Else
                Return Color.FromArgb(149, 165, 166) ' Gray
        End Select
    End Function

    Private Function GetSeverityText(severity As String) As String
        Select Case severity?.ToLower()
            Case "critical"
                Return "🔴 Kritik - Acil Müdahale"
            Case "high"
                Return "🟠 Yüksek - Onarım Gerekli"
            Case "medium"
                Return "🟡 Orta - Dikkat"
            Case "low"
                Return "🟢 Düşük - Bilgilendirme"
            Case Else
                Return "⚪ Bilinmiyor"
        End Select
    End Function

    Private Function GetCategoryText(category As String) As String
        Select Case category?.ToLower()
            Case "powertrain"
                Return "⚙️ Powertrain (Güç Aktarma)"
            Case "chassis"
                Return "🚗 Chassis (Şasi)"
            Case "body"
                Return "🚪 Body (Gövde)"
            Case "network"
                Return "🌐 Network (Ağ)"
            Case Else
                Return category
        End Select
    End Function

End Class

