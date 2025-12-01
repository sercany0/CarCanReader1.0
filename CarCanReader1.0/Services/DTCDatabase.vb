' DTCDatabase.vb
' DTC (Diagnostic Trouble Code) veritabanı servisi
'
' Özellikler:
'   - Singleton pattern
'   - JSON dosyalarından yükleme
'   - Arama ve filtreleme
'   - Kategori bazlı organizasyon
'
' Dosya Yapısı:
'   data/dtc/
'   ├── powertrain.json    (P0000-P3999)
'   ├── chassis.json       (C0000-C3999)
'   ├── body.json          (B0000-B3999)
'   └── network.json       (U0000-U3999)

Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports CarCanReader1._0.Models

Namespace Services

    ''' <summary>
    ''' DTC veritabanı servisi (Singleton)
    ''' </summary>
    Public NotInheritable Class DTCDatabase

#Region "Singleton"

        Private Shared _instance As DTCDatabase
        Private Shared ReadOnly _instanceLock As New Object()

        ''' <summary>
        ''' Singleton instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As DTCDatabase
            Get
                If _instance Is Nothing Then
                    SyncLock _instanceLock
                        If _instance Is Nothing Then
                            _instance = New DTCDatabase()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        Private Sub New()
            _dtcData = New Dictionary(Of String, DTCInfo)(StringComparer.OrdinalIgnoreCase)
            _categoryCounts = New Dictionary(Of String, Integer)()
        End Sub

#End Region

#Region "Events"

        ''' <summary>
        ''' Veritabanı yüklendiğinde
        ''' </summary>
        Public Event OnDatabaseLoaded(count As Integer)

        ''' <summary>
        ''' DTC bulunduğunda
        ''' </summary>
        Public Event OnDTCFound(dtc As DTCInfo)

        ''' <summary>
        ''' Hata oluştuğunda
        ''' </summary>
        Public Event OnError(message As String)

#End Region

#Region "Constants"

        Private Const DTC_FOLDER As String = "dtc"
        Private Const DATA_FOLDER As String = "data"

#End Region

#Region "Fields"

        Private ReadOnly _dtcData As Dictionary(Of String, DTCInfo)
        Private ReadOnly _categoryCounts As Dictionary(Of String, Integer)
        Private ReadOnly _lock As New Object()
        Private _isLoaded As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>
        ''' Toplam DTC sayısı
        ''' </summary>
        Public ReadOnly Property TotalCount As Integer
            Get
                SyncLock _lock
                    Return _dtcData.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Kategori bazında sayılar
        ''' </summary>
        Public ReadOnly Property CategoryCounts As Dictionary(Of String, Integer)
            Get
                SyncLock _lock
                    Return New Dictionary(Of String, Integer)(_categoryCounts)
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Veritabanı yüklü mü
        ''' </summary>
        Public ReadOnly Property IsLoaded As Boolean
            Get
                Return _isLoaded
            End Get
        End Property

        ''' <summary>
        ''' DTC klasör yolu
        ''' </summary>
        Public ReadOnly Property DTCFolderPath As String
            Get
                Return Path.Combine(Application.StartupPath, DATA_FOLDER, DTC_FOLDER)
            End Get
        End Property

#End Region

#Region "Load Database"

        ''' <summary>
        ''' Tüm DTC JSON dosyalarını yükler
        ''' </summary>
        Public Sub LoadDatabase()
            Try
                SyncLock _lock
                    _dtcData.Clear()
                    _categoryCounts.Clear()

                    ' Klasörü oluştur
                    EnsureDTCFolder()

                    ' Her kategori dosyasını yükle
                    LoadCategoryFile("powertrain.json", "Powertrain")
                    LoadCategoryFile("chassis.json", "Chassis")
                    LoadCategoryFile("body.json", "Body")
                    LoadCategoryFile("network.json", "Network")

                    ' Eğer hiç veri yoksa varsayılan DTC'leri oluştur
                    If _dtcData.Count = 0 Then
                        CreateDefaultDTCFiles()
                        ' Tekrar yükle
                        LoadCategoryFile("powertrain.json", "Powertrain")
                        LoadCategoryFile("chassis.json", "Chassis")
                        LoadCategoryFile("body.json", "Body")
                        LoadCategoryFile("network.json", "Network")
                    End If

                    _isLoaded = True
                End SyncLock

                RaiseEvent OnDatabaseLoaded(_dtcData.Count)
                Debug.WriteLine($"DTCDatabase loaded: {_dtcData.Count} codes")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "DTCDatabase.LoadDatabase")
                RaiseEvent OnError($"DTC veritabanı yükleme hatası: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Kategori dosyasını yükler
        ''' </summary>
        Private Sub LoadCategoryFile(fileName As String, categoryName As String)
            Try
                Dim filePath = Path.Combine(DTCFolderPath, fileName)
                If Not File.Exists(filePath) Then Return

                Dim jsonText = File.ReadAllText(filePath)
                If String.IsNullOrWhiteSpace(jsonText) Then Return

                Dim dtcArray = JArray.Parse(jsonText)
                Dim count = 0

                For Each item As JObject In dtcArray
                    Try
                        Dim dtc As New DTCInfo()
                        dtc.Code = If(item.Value(Of String)("code"), "")
                        dtc.Category = If(item.Value(Of String)("category"), categoryName)
                        dtc.Subcategory = If(item.Value(Of String)("subcategory"), "")
                        dtc.DescriptionTR = If(item.Value(Of String)("description_tr"), "")
                        dtc.DescriptionEN = If(item.Value(Of String)("description_en"), "")
                        dtc.Severity = If(item.Value(Of String)("severity"), "medium")

                        ' Liste alanları
                        If item("symptoms") IsNot Nothing Then
                            For Each s In item("symptoms")
                                dtc.Symptoms.Add(s.ToString())
                            Next
                        End If

                        If item("causes") IsNot Nothing Then
                            For Each c In item("causes")
                                dtc.Causes.Add(c.ToString())
                            Next
                        End If

                        If item("solutions") IsNot Nothing Then
                            For Each sol In item("solutions")
                                dtc.Solutions.Add(sol.ToString())
                            Next
                        End If

                        ' Dictionary'e ekle
                        If Not String.IsNullOrEmpty(dtc.Code) AndAlso Not _dtcData.ContainsKey(dtc.Code) Then
                            _dtcData(dtc.Code) = dtc
                            count += 1
                        End If

                    Catch itemEx As Exception
                        Debug.WriteLine($"DTC parse error: {itemEx.Message}")
                    End Try
                Next

                _categoryCounts(categoryName) = count

            Catch ex As Exception
                Debug.WriteLine($"LoadCategoryFile error ({fileName}): {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' DTC klasörünü oluşturur
        ''' </summary>
        Private Sub EnsureDTCFolder()
            Try
                If Not Directory.Exists(DTCFolderPath) Then
                    Directory.CreateDirectory(DTCFolderPath)
                End If
            Catch ex As Exception
                Debug.WriteLine($"EnsureDTCFolder error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Varsayılan DTC dosyalarını oluşturur
        ''' </summary>
        Private Sub CreateDefaultDTCFiles()
            Try
                ' Powertrain - En yaygın P kodları
                CreatePowertrainDefaults()

                ' Chassis
                CreateChassisDefaults()

                ' Body
                CreateBodyDefaults()

                ' Network
                CreateNetworkDefaults()

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "DTCDatabase.CreateDefaultDTCFiles")
            End Try
        End Sub

        Private Sub CreatePowertrainDefaults()
            Dim dtcList As New List(Of Object)

            ' Misfire codes
            dtcList.Add(New With {
                .code = "P0300",
                .category = "Powertrain",
                .subcategory = "Misfire",
                .description_tr = "Rastgele/Çoklu Silindir Ateşleme Hatası Tespit Edildi",
                .description_en = "Random/Multiple Cylinder Misfire Detected",
                .severity = "high",
                .symptoms = New String() {"Titreşim", "Güç kaybı", "Rölantide sarsıntı"},
                .causes = New String() {"Buji arızası", "Enjektör arızası", "Bobin arızası"},
                .solutions = New String() {"Bujileri kontrol et", "Enjektörleri test et", "Bobinleri kontrol et"}
            })

            For i = 1 To 8
                dtcList.Add(New With {
                    .code = $"P030{i}",
                    .category = "Powertrain",
                    .subcategory = "Misfire",
                    .description_tr = $"{i}. Silindir Ateşleme Hatası Tespit Edildi",
                    .description_en = $"Cylinder {i} Misfire Detected",
                    .severity = "high",
                    .symptoms = New String() {"Titreşim", "Güç kaybı"},
                    .causes = New String() {$"Silindir {i} buji arızası", $"Silindir {i} enjektör arızası"},
                    .solutions = New String() {$"Silindir {i} bujisini kontrol et", "Kompresyon testi yap"}
                })
            Next

            ' Fuel System
            dtcList.Add(New With {
                .code = "P0171",
                .category = "Powertrain",
                .subcategory = "Fuel System",
                .description_tr = "Yakıt Sistemi Çok Fakir (Bank 1)",
                .description_en = "System Too Lean (Bank 1)",
                .severity = "medium",
                .symptoms = New String() {"Güç kaybı", "Sert rölanti"},
                .causes = New String() {"Hava kaçağı", "Yakıt pompası zayıf", "MAF sensör kirli"},
                .solutions = New String() {"Vakum kaçaklarını kontrol et", "MAF sensörü temizle"}
            })

            dtcList.Add(New With {
                .code = "P0172",
                .category = "Powertrain",
                .subcategory = "Fuel System",
                .description_tr = "Yakıt Sistemi Çok Zengin (Bank 1)",
                .description_en = "System Too Rich (Bank 1)",
                .severity = "medium",
                .symptoms = New String() {"Fazla yakıt tüketimi", "Siyah egzoz dumanı"},
                .causes = New String() {"Enjektör sızıntısı", "O2 sensör arızası"},
                .solutions = New String() {"Enjektörleri kontrol et", "O2 sensörlerini test et"}
            })

            ' O2 Sensors
            dtcList.Add(New With {
                .code = "P0130",
                .category = "Powertrain",
                .subcategory = "O2 Sensor",
                .description_tr = "O2 Sensör Devresi Arızası (Bank 1, Sensör 1)",
                .description_en = "O2 Sensor Circuit Malfunction (Bank 1, Sensor 1)",
                .severity = "medium",
                .symptoms = New String() {"Check Engine ışığı", "Yakıt ekonomisi kötü"},
                .causes = New String() {"O2 sensör arızası", "Kablo hasarı"},
                .solutions = New String() {"O2 sensörünü değiştir", "Kabloları kontrol et"}
            })

            dtcList.Add(New With {
                .code = "P0420",
                .category = "Powertrain",
                .subcategory = "Catalyst",
                .description_tr = "Katalizör Sistemi Verimliliği Eşik Altında (Bank 1)",
                .description_en = "Catalyst System Efficiency Below Threshold (Bank 1)",
                .severity = "high",
                .symptoms = New String() {"Check Engine ışığı", "Emisyon testi başarısız"},
                .causes = New String() {"Katalitik konvertör arızası", "O2 sensör arızası"},
                .solutions = New String() {"Katalitik konvertörü kontrol et", "O2 sensörlerini test et"}
            })

            ' Coolant
            dtcList.Add(New With {
                .code = "P0115",
                .category = "Powertrain",
                .subcategory = "Coolant",
                .description_tr = "Motor Soğutma Suyu Sıcaklık Sensörü Devresi",
                .description_en = "Engine Coolant Temperature Circuit",
                .severity = "medium",
                .symptoms = New String() {"Yanlış sıcaklık göstergesi", "Kötü rölanti"},
                .causes = New String() {"ECT sensör arızası", "Kablo sorunu"},
                .solutions = New String() {"ECT sensörünü test et", "Kabloları kontrol et"}
            })

            ' Throttle
            dtcList.Add(New With {
                .code = "P0120",
                .category = "Powertrain",
                .subcategory = "Throttle",
                .description_tr = "Gaz Kelebeği Konum Sensörü Devresi",
                .description_en = "Throttle Position Sensor Circuit",
                .severity = "high",
                .symptoms = New String() {"Hızlanma sorunları", "Sert vitesler"},
                .causes = New String() {"TPS sensör arızası", "Bağlantı sorunu"},
                .solutions = New String() {"TPS sensörünü değiştir", "Bağlantıları kontrol et"}
            })

            SaveCategoryFile("powertrain.json", dtcList)
        End Sub

        Private Sub CreateChassisDefaults()
            Dim dtcList As New List(Of Object)

            dtcList.Add(New With {
                .code = "C0035",
                .category = "Chassis",
                .subcategory = "ABS",
                .description_tr = "Sol Ön Tekerlek Hız Sensörü Devresi",
                .description_en = "Left Front Wheel Speed Sensor Circuit",
                .severity = "high",
                .symptoms = New String() {"ABS ışığı yanık", "ABS çalışmıyor"},
                .causes = New String() {"Tekerlek hız sensörü arızası", "Kablo hasarı"},
                .solutions = New String() {"Sensörü test et", "Kabloları kontrol et"}
            })

            dtcList.Add(New With {
                .code = "C0040",
                .category = "Chassis",
                .subcategory = "ABS",
                .description_tr = "Sağ Ön Tekerlek Hız Sensörü Devresi",
                .description_en = "Right Front Wheel Speed Sensor Circuit",
                .severity = "high",
                .symptoms = New String() {"ABS ışığı yanık", "ABS çalışmıyor"},
                .causes = New String() {"Tekerlek hız sensörü arızası", "Kablo hasarı"},
                .solutions = New String() {"Sensörü test et", "Kabloları kontrol et"}
            })

            SaveCategoryFile("chassis.json", dtcList)
        End Sub

        Private Sub CreateBodyDefaults()
            Dim dtcList As New List(Of Object)

            dtcList.Add(New With {
                .code = "B0001",
                .category = "Body",
                .subcategory = "Airbag",
                .description_tr = "Sürücü Hava Yastığı Devresi Arızası",
                .description_en = "Driver Airbag Circuit Malfunction",
                .severity = "critical",
                .symptoms = New String() {"Airbag ışığı yanık"},
                .causes = New String() {"Airbag modül arızası", "Saat yayı arızası"},
                .solutions = New String() {"Airbag sistemini kontrol ettir", "Yetkili servise gidin"}
            })

            dtcList.Add(New With {
                .code = "B1000",
                .category = "Body",
                .subcategory = "General",
                .description_tr = "ECU Genel Arıza",
                .description_en = "ECU General Fault",
                .severity = "medium",
                .symptoms = New String() {"Çeşitli elektrik sorunları"},
                .causes = New String() {"ECU yazılım hatası", "Voltaj sorunu"},
                .solutions = New String() {"ECU'yu resetle", "Akü voltajını kontrol et"}
            })

            SaveCategoryFile("body.json", dtcList)
        End Sub

        Private Sub CreateNetworkDefaults()
            Dim dtcList As New List(Of Object)

            dtcList.Add(New With {
                .code = "U0100",
                .category = "Network",
                .subcategory = "Communication",
                .description_tr = "ECM/PCM ile İletişim Kaybı",
                .description_en = "Lost Communication With ECM/PCM",
                .severity = "critical",
                .symptoms = New String() {"Motor çalışmıyor", "Çoklu uyarı lambaları"},
                .causes = New String() {"CAN bus arızası", "ECU arızası"},
                .solutions = New String() {"CAN bus kablolarını kontrol et", "ECU bağlantılarını kontrol et"}
            })

            dtcList.Add(New With {
                .code = "U0101",
                .category = "Network",
                .subcategory = "Communication",
                .description_tr = "TCM ile İletişim Kaybı",
                .description_en = "Lost Communication With TCM",
                .severity = "high",
                .symptoms = New String() {"Şanzıman limp mode", "Vites değiştirme sorunları"},
                .causes = New String() {"CAN bus arızası", "TCM arızası"},
                .solutions = New String() {"TCM bağlantılarını kontrol et", "CAN bus test et"}
            })

            dtcList.Add(New With {
                .code = "U0140",
                .category = "Network",
                .subcategory = "Communication",
                .description_tr = "BCM ile İletişim Kaybı",
                .description_en = "Lost Communication With BCM",
                .severity = "medium",
                .symptoms = New String() {"Gövde elektrik sorunları", "Merkezi kilit sorunları"},
                .causes = New String() {"BCM arızası", "Kablo hasarı"},
                .solutions = New String() {"BCM bağlantılarını kontrol et"}
            })

            SaveCategoryFile("network.json", dtcList)
        End Sub

        Private Sub SaveCategoryFile(fileName As String, dtcList As List(Of Object))
            Try
                Dim filePath = Path.Combine(DTCFolderPath, fileName)
                Dim json = JsonConvert.SerializeObject(dtcList, Formatting.Indented)
                File.WriteAllText(filePath, json)
            Catch ex As Exception
                Debug.WriteLine($"SaveCategoryFile error ({fileName}): {ex.Message}")
            End Try
        End Sub

#End Region

#Region "Query Methods"

        ''' <summary>
        ''' DTC koduna göre bilgi döndürür
        ''' </summary>
        Public Function GetDTC(code As String) As DTCInfo
            If String.IsNullOrEmpty(code) Then Return Nothing

            SyncLock _lock
                If _dtcData.ContainsKey(code) Then
                    Dim dtc = _dtcData(code).Clone()
                    RaiseEvent OnDTCFound(dtc)
                    Return dtc
                End If
            End SyncLock

            ' Bulunamazsa temel bilgilerle oluştur
            Dim basicDtc = DTCInfo.FromCode(code)
            basicDtc.DescriptionTR = "Tanım bulunamadı"
            basicDtc.DescriptionEN = "Description not found"
            Return basicDtc
        End Function

        ''' <summary>
        ''' DTC kodunun veritabanında olup olmadığını kontrol eder
        ''' </summary>
        Public Function HasDTC(code As String) As Boolean
            If String.IsNullOrEmpty(code) Then Return False

            SyncLock _lock
                Return _dtcData.ContainsKey(code)
            End SyncLock
        End Function

        ''' <summary>
        ''' Anahtar kelimeye göre arama yapar
        ''' </summary>
        Public Function SearchDTC(keyword As String) As List(Of DTCInfo)
            Dim results As New List(Of DTCInfo)

            If String.IsNullOrEmpty(keyword) Then Return results

            SyncLock _lock
                For Each dtc In _dtcData.Values
                    If dtc.ContainsKeyword(keyword) Then
                        results.Add(dtc.Clone())
                    End If
                Next
            End SyncLock

            Return results
        End Function

        ''' <summary>
        ''' Kategoriye göre DTC'leri döndürür
        ''' </summary>
        Public Function GetByCategory(category As String) As List(Of DTCInfo)
            Dim results As New List(Of DTCInfo)

            If String.IsNullOrEmpty(category) Then Return results

            SyncLock _lock
                For Each dtc In _dtcData.Values
                    If dtc.Category.Equals(category, StringComparison.OrdinalIgnoreCase) Then
                        results.Add(dtc.Clone())
                    End If
                Next
            End SyncLock

            Return results
        End Function

        ''' <summary>
        ''' Ciddiyet seviyesine göre DTC'leri döndürür
        ''' </summary>
        Public Function GetBySeverity(severity As String) As List(Of DTCInfo)
            Dim results As New List(Of DTCInfo)

            If String.IsNullOrEmpty(severity) Then Return results

            SyncLock _lock
                For Each dtc In _dtcData.Values
                    If dtc.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase) Then
                        results.Add(dtc.Clone())
                    End If
                Next
            End SyncLock

            Return results
        End Function

        ''' <summary>
        ''' Alt kategoriye göre DTC'leri döndürür
        ''' </summary>
        Public Function GetBySubcategory(subcategory As String) As List(Of DTCInfo)
            Dim results As New List(Of DTCInfo)

            If String.IsNullOrEmpty(subcategory) Then Return results

            SyncLock _lock
                For Each dtc In _dtcData.Values
                    If dtc.Subcategory.Equals(subcategory, StringComparison.OrdinalIgnoreCase) Then
                        results.Add(dtc.Clone())
                    End If
                Next
            End SyncLock

            Return results
        End Function

        ''' <summary>
        ''' Tüm DTC'leri döndürür
        ''' </summary>
        Public Function GetAllDTCs() As List(Of DTCInfo)
            SyncLock _lock
                Return _dtcData.Values.Select(Function(d) d.Clone()).ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Tüm kategorileri döndürür
        ''' </summary>
        Public Function GetCategories() As List(Of String)
            Return New List(Of String)({"Powertrain", "Chassis", "Body", "Network"})
        End Function

        ''' <summary>
        ''' Tüm alt kategorileri döndürür
        ''' </summary>
        Public Function GetSubcategories() As List(Of String)
            Dim subcats As New HashSet(Of String)

            SyncLock _lock
                For Each dtc In _dtcData.Values
                    If Not String.IsNullOrEmpty(dtc.Subcategory) Then
                        subcats.Add(dtc.Subcategory)
                    End If
                Next
            End SyncLock

            Return subcats.OrderBy(Function(s) s).ToList()
        End Function

#End Region

#Region "Utility Methods"

        ''' <summary>
        ''' Veritabanı özetini döndürür
        ''' </summary>
        Public Function GetDatabaseSummary() As String
            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine("═══════════════════════════════════")
            sb.AppendLine("      DTC VERİTABANI ÖZETİ")
            sb.AppendLine("═══════════════════════════════════")
            sb.AppendLine()
            sb.AppendLine($"📊 Toplam DTC: {TotalCount}")
            sb.AppendLine()
            sb.AppendLine("📁 Kategoriler:")

            For Each kvp In CategoryCounts
                sb.AppendLine($"   • {kvp.Key}: {kvp.Value} kod")
            Next

            sb.AppendLine()
            sb.AppendLine($"📂 Konum: {DTCFolderPath}")

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Veritabanını yeniden yükler
        ''' </summary>
        Public Sub Reload()
            LoadDatabase()
        End Sub

#End Region

    End Class

End Namespace

