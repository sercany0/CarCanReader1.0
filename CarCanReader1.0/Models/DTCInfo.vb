' DTCInfo.vb
' DTC (Diagnostic Trouble Code) veri modeli
'
' DTC Kod Yapısı:
'   P0xxx - Powertrain (Motor/Şanzıman) - Genel
'   P1xxx - Powertrain - Üreticiye özel
'   P2xxx - Powertrain - Genel (ek)
'   P3xxx - Powertrain - Üreticiye özel (ek)
'   C0xxx - Chassis (Şasi) - Genel
'   C1xxx - Chassis - Üreticiye özel
'   B0xxx - Body (Gövde) - Genel
'   B1xxx - Body - Üreticiye özel
'   U0xxx - Network (Ağ/İletişim) - Genel
'   U1xxx - Network - Üreticiye özel

Imports System.Drawing

Namespace Models

    ''' <summary>
    ''' DTC Ciddiyet seviyeleri
    ''' </summary>
    Public Enum DTCSeverity
        Low = 0         ' Düşük - Bilgilendirme
        Medium = 1      ' Orta - Dikkat gerektirir
        High = 2        ' Yüksek - Onarım gerekli
        Critical = 3    ' Kritik - Acil müdahale
    End Enum

    ''' <summary>
    ''' DTC Kategorileri
    ''' </summary>
    Public Enum DTCCategory
        Powertrain = 0  ' P - Motor/Şanzıman
        Chassis = 1     ' C - Şasi/Fren/Süspansiyon
        Body = 2        ' B - Gövde/Klima/Aydınlatma
        Network = 3     ' U - Ağ/İletişim
        Unknown = 99
    End Enum

    ''' <summary>
    ''' DTC Durumu
    ''' </summary>
    Public Enum DTCStatus
        None = 0
        Pending = 1     ' Beklemede - Henüz onaylanmamış
        Stored = 2      ' Kayıtlı - Onaylanmış hata
        Permanent = 3   ' Kalıcı - Silinemiyor
    End Enum

    ''' <summary>
    ''' DTC (Diagnostic Trouble Code) bilgi modeli
    ''' </summary>
    Public Class DTCInfo

#Region "Properties"

        ''' <summary>
        ''' DTC Kodu (örn: P0301, C1234, B0010, U0100)
        ''' </summary>
        Public Property Code As String = ""

        ''' <summary>
        ''' Ana kategori (Powertrain, Chassis, Body, Network)
        ''' </summary>
        Public Property Category As String = ""

        ''' <summary>
        ''' Alt kategori (Misfire, Fuel, Transmission, etc.)
        ''' </summary>
        Public Property Subcategory As String = ""

        ''' <summary>
        ''' Türkçe açıklama
        ''' </summary>
        Public Property DescriptionTR As String = ""

        ''' <summary>
        ''' İngilizce açıklama
        ''' </summary>
        Public Property DescriptionEN As String = ""

        ''' <summary>
        ''' Ciddiyet seviyesi (low, medium, high, critical)
        ''' </summary>
        Public Property Severity As String = "medium"

        ''' <summary>
        ''' Belirtiler listesi
        ''' </summary>
        Public Property Symptoms As New List(Of String)

        ''' <summary>
        ''' Olası sebepler listesi
        ''' </summary>
        Public Property Causes As New List(Of String)

        ''' <summary>
        ''' Çözüm önerileri listesi
        ''' </summary>
        Public Property Solutions As New List(Of String)

        ''' <summary>
        ''' Beklemede mi (henüz onaylanmamış)
        ''' </summary>
        Public Property IsPending As Boolean = False

        ''' <summary>
        ''' Kayıtlı mı (onaylanmış hata)
        ''' </summary>
        Public Property IsStored As Boolean = False

        ''' <summary>
        ''' Kalıcı mı (silinemiyor)
        ''' </summary>
        Public Property IsPermanent As Boolean = False

        ''' <summary>
        ''' İlk tespit tarihi
        ''' </summary>
        Public Property FirstDetected As DateTime = DateTime.MinValue

        ''' <summary>
        ''' Son tespit tarihi
        ''' </summary>
        Public Property LastDetected As DateTime = DateTime.MinValue

        ''' <summary>
        ''' Tespit sayısı
        ''' </summary>
        Public Property DetectionCount As Integer = 0

        ''' <summary>
        ''' Freeze frame verisi var mı
        ''' </summary>
        Public Property HasFreezeFrame As Boolean = False

        ''' <summary>
        ''' İlgili ECU adı
        ''' </summary>
        Public Property ECUName As String = ""

#End Region

#Region "Computed Properties"

        ''' <summary>
        ''' Ciddiyet seviyesine göre renk
        ''' </summary>
        Public ReadOnly Property SeverityColor As Color
            Get
                Select Case Severity.ToLower()
                    Case "low"
                        Return Color.FromArgb(46, 204, 113)     ' Yeşil
                    Case "medium"
                        Return Color.FromArgb(241, 196, 15)     ' Sarı
                    Case "high"
                        Return Color.FromArgb(230, 126, 34)     ' Turuncu
                    Case "critical"
                        Return Color.FromArgb(231, 76, 60)      ' Kırmızı
                    Case Else
                        Return Color.FromArgb(149, 165, 166)    ' Gri
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Kategori enum değeri
        ''' </summary>
        Public ReadOnly Property CategoryEnum As DTCCategory
            Get
                If String.IsNullOrEmpty(Code) OrElse Code.Length < 1 Then
                    Return DTCCategory.Unknown
                End If

                Select Case Code(0)
                    Case "P"c
                        Return DTCCategory.Powertrain
                    Case "C"c
                        Return DTCCategory.Chassis
                    Case "B"c
                        Return DTCCategory.Body
                    Case "U"c
                        Return DTCCategory.Network
                    Case Else
                        Return DTCCategory.Unknown
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Ciddiyet enum değeri
        ''' </summary>
        Public ReadOnly Property SeverityEnum As DTCSeverity
            Get
                Select Case Severity.ToLower()
                    Case "low"
                        Return DTCSeverity.Low
                    Case "medium"
                        Return DTCSeverity.Medium
                    Case "high"
                        Return DTCSeverity.High
                    Case "critical"
                        Return DTCSeverity.Critical
                    Case Else
                        Return DTCSeverity.Medium
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Durum enum değeri
        ''' </summary>
        Public ReadOnly Property Status As DTCStatus
            Get
                If IsPermanent Then Return DTCStatus.Permanent
                If IsStored Then Return DTCStatus.Stored
                If IsPending Then Return DTCStatus.Pending
                Return DTCStatus.None
            End Get
        End Property

        ''' <summary>
        ''' Durum ikonu
        ''' </summary>
        Public ReadOnly Property StatusIcon As String
            Get
                Select Case Status
                    Case DTCStatus.Pending
                        Return "⏳"
                    Case DTCStatus.Stored
                        Return "⚠️"
                    Case DTCStatus.Permanent
                        Return "🔒"
                    Case Else
                        Return "❓"
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Kategori ikonu
        ''' </summary>
        Public ReadOnly Property CategoryIcon As String
            Get
                Select Case CategoryEnum
                    Case DTCCategory.Powertrain
                        Return "🔧"
                    Case DTCCategory.Chassis
                        Return "🚗"
                    Case DTCCategory.Body
                        Return "💡"
                    Case DTCCategory.Network
                        Return "📡"
                    Case Else
                        Return "❓"
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Genel (Generic) kod mu? (x0xxx ve x2xxx kodları)
        ''' </summary>
        Public ReadOnly Property IsGeneric As Boolean
            Get
                If String.IsNullOrEmpty(Code) OrElse Code.Length < 2 Then Return False
                Return Code(1) = "0"c OrElse Code(1) = "2"c
            End Get
        End Property

        ''' <summary>
        ''' Üreticiye özel (Manufacturer Specific) kod mu? (x1xxx ve x3xxx kodları)
        ''' </summary>
        Public ReadOnly Property IsManufacturerSpecific As Boolean
            Get
                If String.IsNullOrEmpty(Code) OrElse Code.Length < 2 Then Return False
                Return Code(1) = "1"c OrElse Code(1) = "3"c
            End Get
        End Property

#End Region

#Region "Methods"

        ''' <summary>
        ''' Dile göre tam açıklama döndürür
        ''' </summary>
        Public Function GetFullDescription(Optional language As String = "tr") As String
            Dim desc As String
            If language.ToLower() = "en" Then
                desc = If(String.IsNullOrEmpty(DescriptionEN), DescriptionTR, DescriptionEN)
            Else
                desc = If(String.IsNullOrEmpty(DescriptionTR), DescriptionEN, DescriptionTR)
            End If

            If String.IsNullOrEmpty(desc) Then
                desc = "Açıklama bulunamadı"
            End If

            Return $"{Code}: {desc}"
        End Function

        ''' <summary>
        ''' Kısa özet döndürür
        ''' </summary>
        Public Function GetShortSummary() As String
            Return $"{StatusIcon} {Code} - {If(Not String.IsNullOrEmpty(DescriptionTR), DescriptionTR, DescriptionEN)}"
        End Function

        ''' <summary>
        ''' Detaylı bilgi string'i döndürür
        ''' </summary>
        Public Function GetDetailedInfo(Optional language As String = "tr") As String
            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine($"══════════════════════════════════════")
            sb.AppendLine($" {CategoryIcon} {Code} - {GetFullDescription(language)}")
            sb.AppendLine($"══════════════════════════════════════")
            sb.AppendLine()
            sb.AppendLine($"📋 Kategori: {Category} / {Subcategory}")
            sb.AppendLine($"⚡ Ciddiyet: {Severity.ToUpper()}")
            sb.AppendLine($"📊 Durum: {StatusIcon} {Status}")
            sb.AppendLine()

            If Symptoms.Count > 0 Then
                sb.AppendLine("🔍 Belirtiler:")
                For Each symptom In Symptoms
                    sb.AppendLine($"   • {symptom}")
                Next
                sb.AppendLine()
            End If

            If Causes.Count > 0 Then
                sb.AppendLine("❓ Olası Sebepler:")
                For Each cause In Causes
                    sb.AppendLine($"   • {cause}")
                Next
                sb.AppendLine()
            End If

            If Solutions.Count > 0 Then
                sb.AppendLine("🔧 Çözüm Önerileri:")
                For Each solution In Solutions
                    sb.AppendLine($"   • {solution}")
                Next
                sb.AppendLine()
            End If

            If DetectionCount > 0 Then
                sb.AppendLine($"📈 Tespit Sayısı: {DetectionCount}")
                If FirstDetected <> DateTime.MinValue Then
                    sb.AppendLine($"📅 İlk Tespit: {FirstDetected:yyyy-MM-dd HH:mm}")
                End If
                If LastDetected <> DateTime.MinValue Then
                    sb.AppendLine($"📅 Son Tespit: {LastDetected:yyyy-MM-dd HH:mm}")
                End If
            End If

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Anahtar kelime içeriyor mu kontrol eder
        ''' </summary>
        Public Function ContainsKeyword(keyword As String) As Boolean
            If String.IsNullOrEmpty(keyword) Then Return True

            keyword = keyword.ToLower()

            ' Kod kontrolü
            If Code.ToLower().Contains(keyword) Then Return True

            ' Açıklama kontrolü
            If Not String.IsNullOrEmpty(DescriptionTR) AndAlso DescriptionTR.ToLower().Contains(keyword) Then Return True
            If Not String.IsNullOrEmpty(DescriptionEN) AndAlso DescriptionEN.ToLower().Contains(keyword) Then Return True

            ' Kategori kontrolü
            If Not String.IsNullOrEmpty(Category) AndAlso Category.ToLower().Contains(keyword) Then Return True
            If Not String.IsNullOrEmpty(Subcategory) AndAlso Subcategory.ToLower().Contains(keyword) Then Return True

            ' Belirtiler kontrolü
            For Each symptom In Symptoms
                If symptom.ToLower().Contains(keyword) Then Return True
            Next

            ' Sebepler kontrolü
            For Each cause In Causes
                If cause.ToLower().Contains(keyword) Then Return True
            Next

            Return False
        End Function

        ''' <summary>
        ''' Klonlama
        ''' </summary>
        Public Function Clone() As DTCInfo
            Dim cloned As New DTCInfo() With {
                .Code = Me.Code,
                .Category = Me.Category,
                .Subcategory = Me.Subcategory,
                .DescriptionTR = Me.DescriptionTR,
                .DescriptionEN = Me.DescriptionEN,
                .Severity = Me.Severity,
                .IsPending = Me.IsPending,
                .IsStored = Me.IsStored,
                .IsPermanent = Me.IsPermanent,
                .FirstDetected = Me.FirstDetected,
                .LastDetected = Me.LastDetected,
                .DetectionCount = Me.DetectionCount,
                .HasFreezeFrame = Me.HasFreezeFrame,
                .ECUName = Me.ECUName
            }
            cloned.Symptoms.AddRange(Me.Symptoms)
            cloned.Causes.AddRange(Me.Causes)
            cloned.Solutions.AddRange(Me.Solutions)
            Return cloned
        End Function

        Public Overrides Function ToString() As String
            Return GetShortSummary()
        End Function

#End Region

#Region "Static Factory Methods"

        ''' <summary>
        ''' DTC kodundan temel bilgilerle nesne oluşturur
        ''' </summary>
        Public Shared Function FromCode(code As String) As DTCInfo
            Dim dtc As New DTCInfo()
            dtc.Code = code.ToUpper()

            ' Kategori belirle
            If code.Length >= 1 Then
                Select Case code(0)
                    Case "P"c
                        dtc.Category = "Powertrain"
                    Case "C"c
                        dtc.Category = "Chassis"
                    Case "B"c
                        dtc.Category = "Body"
                    Case "U"c
                        dtc.Category = "Network"
                End Select
            End If

            ' Alt kategori tahmin et (basit)
            If code.Length >= 3 Then
                Dim subCode = code.Substring(1, 2)
                Select Case code(0)
                    Case "P"c
                        Select Case subCode
                            Case "01", "02", "03"
                                dtc.Subcategory = "Fuel/Air"
                            Case "04", "05", "06"
                                dtc.Subcategory = "Emission"
                            Case "07", "08", "09"
                                dtc.Subcategory = "Transmission"
                            Case "0A", "0B", "0C"
                                dtc.Subcategory = "Ignition"
                            Case Else
                                dtc.Subcategory = "General"
                        End Select
                    Case "C"c
                        dtc.Subcategory = "Chassis"
                    Case "B"c
                        dtc.Subcategory = "Body"
                    Case "U"c
                        dtc.Subcategory = "Communication"
                End Select
            End If

            Return dtc
        End Function

        ''' <summary>
        ''' OBD-II raw bytes'tan DTC kodu oluşturur
        ''' </summary>
        Public Shared Function ParseFromBytes(byte1 As Byte, byte2 As Byte) As String
            ' İlk 2 bit: Kategori
            Dim categoryBits = (byte1 >> 6) And &H3
            Dim categoryChar As Char
            Select Case categoryBits
                Case 0 : categoryChar = "P"c
                Case 1 : categoryChar = "C"c
                Case 2 : categoryChar = "B"c
                Case 3 : categoryChar = "U"c
                Case Else : categoryChar = "P"c
            End Select

            ' Sonraki 2 bit: İkinci karakter (0, 1, 2, 3)
            Dim secondDigit = (byte1 >> 4) And &H3

            ' Son 4 bit: Üçüncü karakter
            Dim thirdDigit = byte1 And &HF

            ' byte2: Son 2 karakter (hex)
            Dim fourthDigit = (byte2 >> 4) And &HF
            Dim fifthDigit = byte2 And &HF

            Return $"{categoryChar}{secondDigit:X1}{thirdDigit:X1}{fourthDigit:X1}{fifthDigit:X1}"
        End Function

#End Region

    End Class

End Namespace

