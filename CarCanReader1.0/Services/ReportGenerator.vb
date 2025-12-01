' ReportGenerator.vb
' PDF rapor oluşturma servisi (iTextSharp kullanarak)
' Araç tanı raporları, ECU tarama raporları ve DTC raporları oluşturur
'
' Kullanım:
'   Dim generator As New ReportGenerator()
'   generator.CompanyName = "CarCanReader"
'   Dim data As New ReportData()
'   generator.GenerateReport(data, "rapor.pdf")

Imports System.Collections.Generic
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports iTextSharpText = iTextSharp.text
Imports iTextSharpPdf = iTextSharp.text.pdf
Imports CarCanReader1._0.Models
Imports CarCanReader1._0.Services
Imports Services = CarCanReader1._0.Services

Namespace Services

    ''' <summary>
    ''' Rapor veri modeli
    ''' </summary>
    Public Class ReportData
        Public Property VehicleInfo As VehicleInfoModel
        Public Property ScanDate As DateTime
        Public Property ScannedEcus As List(Of EcuInfo)
        Public Property FoundDTCs As List(Of DTCInfo)
        Public Property LiveDataSnapshot As Dictionary(Of String, Double)
        Public Property TechnicianName As String
        Public Property CustomerName As String
        Public Property CustomerLicensePlate As String
        Public Property CustomerMileage As Integer
        Public Property Notes As String

        ' Rapor içerik seçenekleri
        Public Property IncludeEcuSummary As Boolean = True
        Public Property IncludeDTCList As Boolean = True
        Public Property IncludeLiveData As Boolean = True
        Public Property IncludeCharts As Boolean = False

        Public Sub New()
            VehicleInfo = Nothing
            ScanDate = DateTime.Now
            ScannedEcus = New List(Of EcuInfo)()
            FoundDTCs = New List(Of DTCInfo)()
            LiveDataSnapshot = New Dictionary(Of String, Double)()
            TechnicianName = ""
            CustomerName = ""
            CustomerLicensePlate = ""
            CustomerMileage = 0
            Notes = ""
        End Sub
    End Class

    ''' <summary>
    ''' PDF rapor oluşturucu
    ''' </summary>
    Public Class ReportGenerator

#Region "Şablon Ayarları"

        Public Property CompanyName As String = "CarCanReader"
        Public Property CompanyLogo As Image = Nothing
        Public Property CompanyAddress As String = ""
        Public Property CompanyPhone As String = ""
        Public Property CompanyEmail As String = ""

        ' Font ayarları (Türkçe karakter desteği için BaseFont kullanılacak)
        Private _titleFont As iTextSharpText.Font
        Private _headingFont As iTextSharpText.Font
        Private _normalFont As iTextSharpText.Font
        Private _smallFont As iTextSharpText.Font
        Private _boldFont As iTextSharpText.Font

        ' Renkler
        Private ReadOnly _headerColor As iTextSharpText.BaseColor = iTextSharpText.BaseColor.BLUE
        Private ReadOnly _textColor As iTextSharpText.BaseColor = iTextSharpText.BaseColor.BLACK
        Private ReadOnly _borderColor As iTextSharpText.BaseColor = iTextSharpText.BaseColor.LIGHT_GRAY
        Private ReadOnly _successColor As iTextSharpText.BaseColor = New iTextSharpText.BaseColor(46, 204, 113)
        Private ReadOnly _errorColor As iTextSharpText.BaseColor = New iTextSharpText.BaseColor(231, 76, 60)

        ' Sayfa ayarları
        Private Const PAGE_MARGIN As Single = 50.0F
        Private Const LINE_HEIGHT As Single = 20.0F
        Private Const SECTION_SPACING As Single = 30.0F

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeFonts()
        End Sub

        ''' <summary>
        ''' Font'ları initialize et (Türkçe karakter desteği)
        ''' </summary>
        Private Sub InitializeFonts()
            Try
                ' Arial Unicode MS veya Arial kullan (Türkçe karakter desteği için)
                Dim baseFont = iTextSharpText.pdf.BaseFont.CreateFont(iTextSharpText.pdf.BaseFont.HELVETICA, iTextSharpText.pdf.BaseFont.WINANSI, iTextSharpText.pdf.BaseFont.NOT_EMBEDDED)
                _titleFont = New iTextSharpText.Font(baseFont, 18, iTextSharpText.Font.BOLD)
                _headingFont = New iTextSharpText.Font(baseFont, 12, iTextSharpText.Font.BOLD)
                _normalFont = New iTextSharpText.Font(baseFont, 10, iTextSharpText.Font.NORMAL)
                _smallFont = New iTextSharpText.Font(baseFont, 8, iTextSharpText.Font.NORMAL)
                _boldFont = New iTextSharpText.Font(baseFont, 10, iTextSharpText.Font.BOLD)
            Catch
                ' Fallback: Standart font'lar
                _titleFont = iTextSharpText.FontFactory.GetFont(iTextSharpText.FontFactory.HELVETICA, 18, iTextSharpText.Font.BOLD)
                _headingFont = iTextSharpText.FontFactory.GetFont(iTextSharpText.FontFactory.HELVETICA, 12, iTextSharpText.Font.BOLD)
                _normalFont = iTextSharpText.FontFactory.GetFont(iTextSharpText.FontFactory.HELVETICA, 10, iTextSharpText.Font.NORMAL)
                _smallFont = iTextSharpText.FontFactory.GetFont(iTextSharpText.FontFactory.HELVETICA, 8, iTextSharpText.Font.NORMAL)
                _boldFont = iTextSharpText.FontFactory.GetFont(iTextSharpText.FontFactory.HELVETICA, 10, iTextSharpText.Font.BOLD)
            End Try
        End Sub

#End Region

#Region "Ana Rapor Oluşturma"

        ''' <summary>
        ''' PDF raporu oluşturur
        ''' </summary>
        ''' <param name="data">Rapor verisi</param>
        ''' <param name="outputPath">Çıktı dosya yolu</param>
        ''' <returns>Başarılı ise True</returns>
        Public Function GenerateReport(data As ReportData, outputPath As String) As Boolean
            Try
                Dim document As New iTextSharpText.Document(iTextSharpText.PageSize.A4, PAGE_MARGIN, PAGE_MARGIN, PAGE_MARGIN, PAGE_MARGIN)
                Dim writer = iTextSharpPdf.PdfWriter.GetInstance(document, New FileStream(outputPath, FileMode.Create))
                document.Open()

                ' Header
                AddHeader(document, writer)

                ' Araç Bilgileri
                If data.VehicleInfo IsNot Nothing Then
                    AddVehicleInfo(document, data.VehicleInfo, data.CustomerLicensePlate, data.CustomerMileage)
                End If

                ' Tarama Bilgileri
                AddScanInfo(document, data)

                ' ECU Özeti
                If data.IncludeEcuSummary AndAlso data.ScannedEcus IsNot Nothing AndAlso data.ScannedEcus.Count > 0 Then
                    AddEcuSummary(document, data.ScannedEcus)
                End If

                ' DTC Tablosu
                If data.IncludeDTCList AndAlso data.FoundDTCs IsNot Nothing AndAlso data.FoundDTCs.Count > 0 Then
                    AddDTCTable(document, data.FoundDTCs)
                End If

                ' Canlı Veri
                If data.IncludeLiveData AndAlso data.LiveDataSnapshot IsNot Nothing AndAlso data.LiveDataSnapshot.Count > 0 Then
                    AddLiveData(document, data.LiveDataSnapshot)
                End If

                ' Notlar
                If Not String.IsNullOrEmpty(data.Notes) Then
                    AddNotes(document, data.Notes)
                End If

                ' Footer
                AddFooter(document, writer)

                document.Close()
                Return True

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.GenerateReport")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Rapor önizlemesi oluşturur (Image array)
        ''' </summary>
        ''' <param name="data">Rapor verisi</param>
        ''' <returns>Sayfa görüntüleri</returns>
        Public Function GenerateReportPreview(data As ReportData) As Image()
            Try
                ' Geçici PDF oluştur
                Dim tempPath = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid()}.pdf")
                If GenerateReport(data, tempPath) Then
                    ' PDF'i image'e çevir (basit implementasyon)
                    ' Bu özellik için ek kütüphane gerekebilir
                    ' Şimdilik boş array döndür
                    Return New Image() {}
                End If

                Return New Image() {}

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.GenerateReportPreview")
                Return New Image() {}
            End Try
        End Function

#End Region

#Region "Rapor Bölümleri"

        ''' <summary>
        ''' Header ekler
        ''' </summary>
        Private Sub AddHeader(document As iTextSharpText.Document, writer As iTextSharpPdf.PdfWriter)
            Try
                ' Logo (varsa)
                If CompanyLogo IsNot Nothing Then
                    Try
                        Using ms As New MemoryStream()
                            CompanyLogo.Save(ms, ImageFormat.Png)
                            ms.Position = 0
                            Dim logoImage = iTextSharpText.Image.GetInstance(ms.ToArray())
                            logoImage.ScaleToFit(100, 50)
                            logoImage.Alignment = iTextSharpText.Element.ALIGN_LEFT
                            document.Add(logoImage)
                        End Using
                    Catch
                        ' Logo yüklenemedi, devam et
                    End Try
                End If

                ' Başlık
                Dim titlePara As New iTextSharpText.Paragraph("ARAÇ TANI RAPORU", _titleFont)
                titlePara.Alignment = iTextSharpText.Element.ALIGN_CENTER
                titlePara.SpacingAfter = 10
                document.Add(titlePara)

                ' Firma bilgileri
                If Not String.IsNullOrEmpty(CompanyName) Then
                    Dim companyPara As New iTextSharpText.Paragraph(CompanyName, _headingFont)
                    companyPara.Alignment = iTextSharpText.Element.ALIGN_CENTER
                    document.Add(companyPara)
                End If

                Dim companyInfo = ""
                If Not String.IsNullOrEmpty(CompanyAddress) Then
                    companyInfo = CompanyAddress
                End If
                If Not String.IsNullOrEmpty(CompanyPhone) Then
                    If Not String.IsNullOrEmpty(companyInfo) Then companyInfo += " - "
                    companyInfo += $"Tel: {CompanyPhone}"
                End If
                If Not String.IsNullOrEmpty(CompanyEmail) Then
                    If Not String.IsNullOrEmpty(companyInfo) Then companyInfo += " - "
                    companyInfo += $"E-posta: {CompanyEmail}"
                End If

                If Not String.IsNullOrEmpty(companyInfo) Then
                    Dim infoPara As New iTextSharpText.Paragraph(companyInfo, _smallFont)
                    infoPara.Alignment = iTextSharpText.Element.ALIGN_CENTER
                    infoPara.SpacingAfter = 20
                    document.Add(infoPara)
                End If

                ' Çizgi
                Dim line As New iTextSharpText.pdf.draw.LineSeparator(1.0F, 100.0F, _borderColor, iTextSharpText.Element.ALIGN_CENTER, -1)
                document.Add(New iTextSharpText.Chunk(line))
                document.Add(New iTextSharpText.Paragraph(" "))

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddHeader")
            End Try
        End Sub

        ''' <summary>
        ''' Araç bilgileri ekler
        ''' </summary>
        Private Sub AddVehicleInfo(document As iTextSharpText.Document, info As VehicleInfoModel, licensePlate As String, mileage As Integer)
            Try
                Dim heading As New iTextSharpText.Paragraph("Araç Bilgileri", _headingFont)
                heading.SpacingAfter = 10
                document.Add(heading)

                If Not String.IsNullOrEmpty(info.VIN) Then
                    document.Add(New iTextSharpText.Paragraph($"VIN: {info.VIN}", _normalFont))
                End If

                If Not String.IsNullOrEmpty(info.ECUName) Then
                    document.Add(New iTextSharpText.Paragraph($"ECU Adı: {info.ECUName}", _normalFont))
                End If

                If Not String.IsNullOrEmpty(info.CalibrationID) Then
                    document.Add(New iTextSharpText.Paragraph($"Calibration ID: {info.CalibrationID}", _normalFont))
                End If

                If Not String.IsNullOrEmpty(info.CVN) Then
                    document.Add(New iTextSharpText.Paragraph($"CVN: {info.CVN}", _normalFont))
                End If

                If Not String.IsNullOrEmpty(info.OBDStandard) Then
                    document.Add(New iTextSharpText.Paragraph($"OBD Standard: {info.OBDStandard}", _normalFont))
                End If

                If Not String.IsNullOrEmpty(licensePlate) Then
                    document.Add(New iTextSharpText.Paragraph($"Plaka: {licensePlate}", _normalFont))
                End If

                If mileage > 0 Then
                    document.Add(New iTextSharpText.Paragraph($"Kilometre: {mileage:N0} km", _normalFont))
                End If

                document.Add(New iTextSharpText.Paragraph(" "))

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddVehicleInfo")
            End Try
        End Sub

        ''' <summary>
        ''' Tarama bilgileri ekler
        ''' </summary>
        Private Sub AddScanInfo(document As iTextSharpText.Document, data As ReportData)
            Try
                Dim heading As New iTextSharpText.Paragraph("Tarama Bilgileri", _headingFont)
                heading.SpacingAfter = 10
                document.Add(heading)

                document.Add(New iTextSharpText.Paragraph($"Tarih: {data.ScanDate:dd.MM.yyyy HH:mm}", _normalFont))

                If Not String.IsNullOrEmpty(data.TechnicianName) Then
                    document.Add(New iTextSharpText.Paragraph($"Teknisyen: {data.TechnicianName}", _normalFont))
                End If

                If Not String.IsNullOrEmpty(data.CustomerName) Then
                    document.Add(New iTextSharpText.Paragraph($"Müşteri: {data.CustomerName}", _normalFont))
                End If

                document.Add(New iTextSharpText.Paragraph(" "))

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddScanInfo")
            End Try
        End Sub

        ''' <summary>
        ''' ECU özeti ekler
        ''' </summary>
        Private Sub AddEcuSummary(document As iTextSharpText.Document, ecus As List(Of EcuInfo))
            Try
                Dim heading As New iTextSharpText.Paragraph("Taranan ECU'lar", _headingFont)
                heading.SpacingAfter = 10
                document.Add(heading)

                ' Tablo oluştur
                Dim table As New iTextSharpPdf.PdfPTable(3)
                table.WidthPercentage = 100
                table.SetWidths(New Single() {50.0F, 25.0F, 25.0F})

                ' Başlık satırı
                Dim headerFont = _boldFont
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("ECU Adı", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Adres", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("DTC", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})

                ' ECU listesi
                For Each ecu In ecus
                    Dim statusText = If(ecu.DTCCount > 0, $"{ecu.DTCCount} DTC", "Hata yok")
                    Dim statusColor = If(ecu.DTCCount > 0, _errorColor, _successColor)

                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(ecu.Name, _normalFont)))
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase($"0x{ecu.Address:X3}", _normalFont)))
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(statusText, _normalFont)) With {.BackgroundColor = statusColor})
                Next

                document.Add(table)
                document.Add(New iTextSharpText.Paragraph(" "))

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddEcuSummary")
            End Try
        End Sub

        ''' <summary>
        ''' DTC tablosu ekler
        ''' </summary>
        Private Sub AddDTCTable(document As iTextSharpText.Document, dtcs As List(Of DTCInfo))
            Try
                Dim heading As New iTextSharpText.Paragraph("Bulunan Hata Kodları", _headingFont)
                heading.SpacingAfter = 10
                document.Add(heading)

                ' Tablo oluştur
                Dim table As New iTextSharpPdf.PdfPTable(4)
                table.WidthPercentage = 100
                table.SetWidths(New Single() {15.0F, 45.0F, 20.0F, 20.0F})

                ' Başlık satırı
                Dim headerFont = _boldFont
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Kod", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Açıklama", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Şiddet", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Öneri", headerFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})

                ' DTC listesi
                For Each dtc In dtcs
                    Dim description = dtc.GetFullDescription("tr")
                    If description.Length > 50 Then
                        description = description.Substring(0, 47) & "..."
                    End If

                    Dim severityText = GetSeverityText(dtc.Severity)
                    Dim severityColor = GetSeverityColor(dtc.Severity)
                    Dim solution = If(dtc.Solutions IsNot Nothing AndAlso dtc.Solutions.Count > 0, dtc.Solutions(0), "Kontrol edilmeli")
                    If solution.Length > 30 Then
                        solution = solution.Substring(0, 27) & "..."
                    End If

                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(dtc.Code, _boldFont)))
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(description, _normalFont)))
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(severityText, _normalFont)) With {.BackgroundColor = severityColor})
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(solution, _smallFont)))
                Next

                document.Add(table)
                document.Add(New iTextSharpText.Paragraph(" "))

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddDTCTable")
            End Try
        End Sub

        ''' <summary>
        ''' Canlı veri ekler
        ''' </summary>
        Private Sub AddLiveData(document As iTextSharpText.Document, data As Dictionary(Of String, Double))
            Try
                Dim heading As New iTextSharpText.Paragraph("Canlı Veri (Tarama Anı)", _headingFont)
                heading.SpacingAfter = 10
                document.Add(heading)

                ' Tablo oluştur
                Dim table As New iTextSharpPdf.PdfPTable(2)
                table.WidthPercentage = 100
                table.SetWidths(New Single() {50.0F, 50.0F})

                ' Başlık satırı
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Parametre", _boldFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})
                table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase("Değer", _boldFont)) With {.BackgroundColor = iTextSharpText.BaseColor.LIGHT_GRAY})

                ' Veri listesi
                For Each kvp In data
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(kvp.Key, _normalFont)))
                    table.AddCell(New iTextSharpPdf.PdfPCell(New iTextSharpText.Phrase(kvp.Value.ToString("F2"), _normalFont)))
                Next

                document.Add(table)
                document.Add(New iTextSharpText.Paragraph(" "))

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddLiveData")
            End Try
        End Sub

        ''' <summary>
        ''' Notlar ekler
        ''' </summary>
        Private Sub AddNotes(document As iTextSharpText.Document, notes As String)
            Try
                Dim heading As New iTextSharpText.Paragraph("Notlar", _headingFont)
                heading.SpacingAfter = 10
                document.Add(heading)

                Dim notesPara As New iTextSharpText.Paragraph(notes, _normalFont)
                notesPara.SpacingAfter = 20
                document.Add(notesPara)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddNotes")
            End Try
        End Sub

        ''' <summary>
        ''' Footer ekler
        ''' </summary>
        Private Sub AddFooter(document As iTextSharpText.Document, writer As iTextSharpPdf.PdfWriter)
            Try
                Dim footer As New iTextSharpText.Paragraph($"Rapor Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}", _smallFont)
                footer.Alignment = iTextSharpText.Element.ALIGN_CENTER
                footer.SpacingBefore = 20
                document.Add(footer)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ReportGenerator.AddFooter")
            End Try
        End Sub

#End Region

#Region "Yardımcı Metodlar"

        Private Function GetSeverityText(severity As String) As String
            Select Case severity.ToLower()
                Case "critical"
                    Return "Kritik"
                Case "high"
                    Return "Yüksek"
                Case "medium"
                    Return "Orta"
                Case "low"
                    Return "Düşük"
                Case Else
                    Return "Bilinmeyen"
            End Select
        End Function

        Private Function GetSeverityColor(severity As String) As iTextSharpText.BaseColor
            Select Case severity.ToLower()
                Case "critical"
                    Return iTextSharpText.BaseColor.RED
                Case "high"
                    Return New iTextSharpText.BaseColor(255, 69, 0) ' OrangeRed
                Case "medium"
                    Return iTextSharpText.BaseColor.ORANGE
                Case "low"
                    Return iTextSharpText.BaseColor.GREEN
                Case Else
                    Return iTextSharpText.BaseColor.GRAY
            End Select
        End Function

#End Region

    End Class

End Namespace
