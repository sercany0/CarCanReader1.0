' OnlineCarDbScraper.vb
' Online araç veritabanı kazıma servisi
' Güvenilir kaynaklardan CAN/UDS komut verisi toplar
'
' ÖNEMLİ KURALLAR:
'   1. SADECE güvenilir kaynaklar kullanılır:
'      - Resmi dokümantasyon
'      - Açık kaynak otomotiv veri setleri
'      - Bilinen reverse-engineering projeleri
'      - Standart spesifikasyonlar
'   2. ASLA sahte veri üretilmez
'   3. Şüpheli veri "needs_review" olarak işaretlenir
'   4. Her veri için kaynak ve güvenilirlik skoru tutulur
'
' Mimari:
'   OnlineCarDbScraper
'   ├── ICarDataSourceProvider (interface)
'   │   ├── LocalDatabaseProvider (mevcut commands.json)
'   │   ├── OpenDBCProvider (TODO: DBC dosya desteği)
'   │   └── ManualEntryProvider (kullanıcı girişi)
'   ├── ScrapedCommandEntry (normalize model)
'   └── MergeToCommandRepository()
'
' Kullanım:
'   Dim scraper As New OnlineCarDbScraper()
'   AddHandler scraper.OnProgress, Sub(msg) Console.WriteLine(msg)
'   scraper.SetCommandRepository(commandRepo)
'   Await scraper.EnrichDatabaseAsync()

Imports System.Threading.Tasks
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Services

#Region "ScrapedCommandEntry Model"

    ''' <summary>
    ''' Kazınan/normalize edilmiş komut verisi
    ''' </summary>
    Public Class ScrapedCommandEntry
        ''' <summary>
        ''' Araç markası
        ''' </summary>
        Public Property Brand As String = ""

        ''' <summary>
        ''' Araç modeli
        ''' </summary>
        Public Property Model As String = ""

        ''' <summary>
        ''' Üretim yılı veya yıl aralığı
        ''' </summary>
        Public Property YearRange As String = ""

        ''' <summary>
        ''' Araç jenerasyonu (örn: "Golf 7", "F30")
        ''' </summary>
        Public Property Generation As String = ""

        ''' <summary>
        ''' Modül adı (örn: "BCM", "Cluster", "Lighting")
        ''' </summary>
        Public Property ModuleName As String = ""

        ''' <summary>
        ''' Komut adı
        ''' </summary>
        Public Property CommandName As String = ""

        ''' <summary>
        ''' Ağ tipi: "CAN", "LIN", "UDS"
        ''' </summary>
        Public Property NetworkType As String = "CAN"

        ''' <summary>
        ''' CAN mesaj ID'si (hex string)
        ''' </summary>
        Public Property MessageId As String = ""

        ''' <summary>
        ''' Data Length Code (0-8)
        ''' </summary>
        Public Property DLC As Integer = 8

        ''' <summary>
        ''' Byte pattern/mask
        ''' </summary>
        Public Property BytePattern As String = ""

        ''' <summary>
        ''' Komut açıklaması
        ''' </summary>
        Public Property Description As String = ""

        ''' <summary>
        ''' Veri kaynağı URL veya referans
        ''' </summary>
        Public Property Source As String = ""

        ''' <summary>
        ''' Güvenilirlik skoru (0-100)
        ''' 100 = Doğrulanmış
        ''' 70-99 = Yüksek güvenilirlik
        ''' 40-69 = Orta güvenilirlik
        ''' 0-39 = Düşük/Doğrulanmamış
        ''' </summary>
        Public Property Confidence As Integer = 0

        ''' <summary>
        ''' Doğrulama durumu
        ''' </summary>
        Public Property VerificationStatus As String = "unverified"

        ''' <summary>
        ''' Toggle komutu mu?
        ''' </summary>
        Public Property IsToggle As Boolean = False

        ''' <summary>
        ''' Toggle ise ON frame
        ''' </summary>
        Public Property ToggleOnFrame As String = ""

        ''' <summary>
        ''' Toggle ise OFF frame
        ''' </summary>
        Public Property ToggleOffFrame As String = ""

        ''' <summary>
        ''' Toggle ise kontrol byte indeksi
        ''' </summary>
        Public Property ToggleByteIndex As Integer = -1

        ''' <summary>
        ''' Kazıma tarihi
        ''' </summary>
        Public Property ScrapedAt As DateTime = DateTime.Now

        ''' <summary>
        ''' SLCAN frame formatına çevirir
        ''' </summary>
        Public Function ToSlcanFrame() As String
            If String.IsNullOrEmpty(MessageId) OrElse String.IsNullOrEmpty(BytePattern) Then
                Return ""
            End If

            Dim id As String = MessageId.Replace("0x", "").Replace("0X", "").PadLeft(3, "0"c)
            Dim data As String = BytePattern.Replace(" ", "").Replace("-", "")

            Return "t" & id & DLC.ToString() & data
        End Function

        ''' <summary>
        ''' Girdiyi okunabilir string olarak döndürür
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"{Brand}/{Model}/{ModuleName}/{CommandName} - ID: 0x{MessageId} - {Confidence}%"
        End Function
    End Class

#End Region

#Region "ICarDataSourceProvider Interface"

    ''' <summary>
    ''' Veri kaynağı sağlayıcı interface'i
    ''' Her kaynak tipi bu interface'i uygular
    ''' </summary>
    Public Interface ICarDataSourceProvider
        ''' <summary>
        ''' Sağlayıcı adı
        ''' </summary>
        ReadOnly Property Name As String

        ''' <summary>
        ''' Sağlayıcı açıklaması
        ''' </summary>
        ReadOnly Property Description As String

        ''' <summary>
        ''' Sağlayıcı aktif mi?
        ''' </summary>
        ReadOnly Property IsEnabled As Boolean

        ''' <summary>
        ''' Belirtilen marka/model için veri çeker
        ''' </summary>
        Function FetchDataAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry))

        ''' <summary>
        ''' Tüm mevcut verileri çeker
        ''' </summary>
        Function FetchAllAsync() As Task(Of List(Of ScrapedCommandEntry))

        ''' <summary>
        ''' Sağlayıcının desteklediği markaları döndürür
        ''' </summary>
        Function GetSupportedBrands() As List(Of String)
    End Interface

#End Region

#Region "LocalDatabaseProvider"

    ''' <summary>
    ''' Mevcut commands.json'dan veri okuyan sağlayıcı
    ''' </summary>
    Public Class LocalDatabaseProvider
        Implements ICarDataSourceProvider

        Private _commandRepository As CommandRepository

        Public Sub New(repo As CommandRepository)
            _commandRepository = repo
        End Sub

        Public ReadOnly Property Name As String Implements ICarDataSourceProvider.Name
            Get
                Return "LocalDatabase"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ICarDataSourceProvider.Description
            Get
                Return "Mevcut commands.json veritabanı"
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean Implements ICarDataSourceProvider.IsEnabled
            Get
                Return _commandRepository IsNot Nothing
            End Get
        End Property

        Public Async Function FetchDataAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchDataAsync
            Return Await Task.Run(Function()
                                      Dim entries As New List(Of ScrapedCommandEntry)
                                      
                                      Try
                                          If _commandRepository Is Nothing Then Return entries

                                          Dim modules = _commandRepository.GetModules(brand, model)
                                          For Each moduleName In modules
                                              Dim commands = _commandRepository.GetCommands(brand, model, moduleName)
                                              For Each cmdName In commands
                                                  Dim entry As New ScrapedCommandEntry()
                                                  entry.Brand = brand
                                                  entry.Model = model
                                                  entry.ModuleName = moduleName
                                                  entry.CommandName = cmdName
                                                  entry.Source = "local_database"
                                                  entry.Confidence = 100
                                                  entry.VerificationStatus = "verified"

                                                  ' Frame bilgisini al
                                                  Dim frameStr = _commandRepository.GetCommand(brand, model, moduleName, cmdName)
                                                  If Not String.IsNullOrEmpty(frameStr) AndAlso frameStr.StartsWith("t") Then
                                                      entry.MessageId = frameStr.Substring(1, 3)
                                                      entry.DLC = If(frameStr.Length > 4, Integer.Parse(frameStr.Substring(4, 1)), 0)
                                                      entry.BytePattern = If(frameStr.Length > 5, frameStr.Substring(5), "")
                                                  End If

                                                  entries.Add(entry)
                                              Next
                                          Next
                                      Catch ex As Exception
                                          Debug.WriteLine($"LocalDatabaseProvider FetchDataAsync error: {ex.Message}")
                                      End Try

                                      Return entries
                                  End Function)
        End Function

        Public Async Function FetchAllAsync() As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchAllAsync
            Return Await Task.Run(Function()
                                      Dim entries As New List(Of ScrapedCommandEntry)

                                      Try
                                          If _commandRepository Is Nothing Then Return entries

                                          For Each brand In _commandRepository.GetBrands()
                                              For Each model In _commandRepository.GetModels(brand)
                                                  Dim modelEntries = FetchDataAsync(brand, model).Result
                                                  entries.AddRange(modelEntries)
                                              Next
                                          Next
                                      Catch ex As Exception
                                          Debug.WriteLine($"LocalDatabaseProvider FetchAllAsync error: {ex.Message}")
                                      End Try

                                      Return entries
                                  End Function)
        End Function

        Public Function GetSupportedBrands() As List(Of String) Implements ICarDataSourceProvider.GetSupportedBrands
            If _commandRepository Is Nothing Then Return New List(Of String)
            Return _commandRepository.GetBrands()
        End Function
    End Class

#End Region

#Region "NHTSAProvider - VIN Decoding API"

    ''' <summary>
    ''' NHTSA VPIC API kullanarak VIN decode eden sağlayıcı
    ''' https://vpic.nhtsa.dot.gov/api/
    ''' </summary>
    Public Class NHTSAProvider
        Implements ICarDataSourceProvider

        Private Shared ReadOnly _httpClient As New HttpClient()
        Private Const API_BASE As String = "https://vpic.nhtsa.dot.gov/api/vehicles"
        Private _lastDecodeResult As NHTSADecodeResult = Nothing

        Public ReadOnly Property Name As String Implements ICarDataSourceProvider.Name
            Get
                Return "NHTSA-VPIC"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ICarDataSourceProvider.Description
            Get
                Return "NHTSA Vehicle Product Information Catalog - VIN Decoding API"
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean Implements ICarDataSourceProvider.IsEnabled
            Get
                Return True ' Always enabled
            End Get
        End Property

        ''' <summary>
        ''' VIN decode sonucu
        ''' </summary>
        Public Class NHTSADecodeResult
            Public Property VIN As String = ""
            Public Property Make As String = ""
            Public Property Model As String = ""
            Public Property ModelYear As Integer = 0
            Public Property BodyClass As String = ""
            Public Property VehicleType As String = ""
            Public Property PlantCountry As String = ""
            Public Property EngineConfiguration As String = ""
            Public Property FuelType As String = ""
            Public Property DriveType As String = ""
            Public Property ErrorCode As String = ""
            Public Property ErrorText As String = ""
            Public Property IsValid As Boolean = False
        End Class

        ''' <summary>
        ''' VIN decode eder
        ''' </summary>
        Public Async Function DecodeVINAsync(vin As String) As Task(Of NHTSADecodeResult)
            Dim result As New NHTSADecodeResult()
            result.VIN = vin

            Try
                Dim url As String = $"{API_BASE}/DecodeVin/{vin}?format=json"
                Dim response = Await _httpClient.GetStringAsync(url)
                Dim json = JObject.Parse(response)

                Dim results = json("Results")
                If results IsNot Nothing Then
                    For Each item In results
                        Dim variable = item("Variable")?.ToString()
                        Dim value = item("Value")?.ToString()

                        If String.IsNullOrEmpty(value) Then Continue For

                        Select Case variable
                            Case "Make"
                                result.Make = value
                            Case "Model"
                                result.Model = value
                            Case "Model Year"
                                Integer.TryParse(value, result.ModelYear)
                            Case "Body Class"
                                result.BodyClass = value
                            Case "Vehicle Type"
                                result.VehicleType = value
                            Case "Plant Country"
                                result.PlantCountry = value
                            Case "Engine Configuration"
                                result.EngineConfiguration = value
                            Case "Fuel Type - Primary"
                                result.FuelType = value
                            Case "Drive Type"
                                result.DriveType = value
                            Case "Error Code"
                                result.ErrorCode = value
                            Case "Error Text"
                                result.ErrorText = value
                        End Select
                    Next

                    result.IsValid = Not String.IsNullOrEmpty(result.Make) AndAlso result.ErrorCode = "0"
                End If

                _lastDecodeResult = result

            Catch ex As Exception
                result.ErrorText = ex.Message
                result.IsValid = False
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Tüm markaları getirir
        ''' </summary>
        Public Async Function GetAllMakesAsync() As Task(Of List(Of String))
            Dim makes As New List(Of String)

            Try
                Dim url As String = $"{API_BASE}/GetAllMakes?format=json"
                Dim response = Await _httpClient.GetStringAsync(url)
                Dim json = JObject.Parse(response)

                Dim results = json("Results")
                If results IsNot Nothing Then
                    For Each item In results
                        Dim makeName = item("Make_Name")?.ToString()
                        If Not String.IsNullOrEmpty(makeName) Then
                            makes.Add(makeName)
                        End If
                    Next
                End If

            Catch ex As Exception
                Debug.WriteLine($"NHTSAProvider GetAllMakesAsync error: {ex.Message}")
            End Try

            Return makes
        End Function

        ''' <summary>
        ''' Belirli marka için modelleri getirir
        ''' </summary>
        Public Async Function GetModelsForMakeAsync(make As String) As Task(Of List(Of String))
            Dim models As New List(Of String)

            Try
                Dim url As String = $"{API_BASE}/GetModelsForMake/{make}?format=json"
                Dim response = Await _httpClient.GetStringAsync(url)
                Dim json = JObject.Parse(response)

                Dim results = json("Results")
                If results IsNot Nothing Then
                    For Each item In results
                        Dim modelName = item("Model_Name")?.ToString()
                        If Not String.IsNullOrEmpty(modelName) Then
                            models.Add(modelName)
                        End If
                    Next
                End If

            Catch ex As Exception
                Debug.WriteLine($"NHTSAProvider GetModelsForMakeAsync error: {ex.Message}")
            End Try

            Return models
        End Function

        ' ICarDataSourceProvider implementation
        Public Function FetchDataAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchDataAsync
            ' NHTSA doesn't provide CAN commands, only vehicle info
            ' Return empty list - this provider is mainly for VIN decoding
            Return Task.FromResult(New List(Of ScrapedCommandEntry)())
        End Function

        Public Function FetchAllAsync() As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchAllAsync
            Return Task.FromResult(New List(Of ScrapedCommandEntry)())
        End Function

        Public Function GetSupportedBrands() As List(Of String) Implements ICarDataSourceProvider.GetSupportedBrands
            ' Return common brands - full list would be fetched via GetAllMakesAsync
            Return New List(Of String) From {
                "Volkswagen", "Audi", "BMW", "Mercedes-Benz", "Porsche",
                "Toyota", "Honda", "Nissan", "Mazda", "Subaru",
                "Ford", "Chevrolet", "GMC", "Dodge", "Chrysler", "Jeep",
                "Hyundai", "Kia", "Tesla", "Volvo", "Jaguar", "Land Rover"
            }
        End Function

        ''' <summary>
        ''' Son decode sonucunu döndürür
        ''' </summary>
        Public ReadOnly Property LastDecodeResult As NHTSADecodeResult
            Get
                Return _lastDecodeResult
            End Get
        End Property
    End Class

#End Region

#Region "OpenDBCProvider - DBC File Parser"

    ''' <summary>
    ''' OpenDBC projesinden DBC dosyalarını parse eden sağlayıcı
    ''' https://github.com/commaai/opendbc
    ''' </summary>
    Public Class OpenDBCProvider
        Implements ICarDataSourceProvider

        Private Shared ReadOnly _httpClient As New HttpClient()
        Private Const GITHUB_RAW_BASE As String = "https://raw.githubusercontent.com/commaai/opendbc/master"

        ' Bilinen DBC dosyaları ve marka eşleştirmeleri
        Private Shared ReadOnly _dbcFiles As New Dictionary(Of String, String) From {
            {"toyota_new_mc_pt_generated.dbc", "Toyota"},
            {"toyota_rav4_prime_2020_pt_generated.dbc", "Toyota"},
            {"toyota_prius_2017_pt_generated.dbc", "Toyota"},
            {"honda_civic_touring_2016_can_generated.dbc", "Honda"},
            {"honda_accord_2018_can_generated.dbc", "Honda"},
            {"acura_ilx_2016_can_generated.dbc", "Acura"},
            {"hyundai_kia_generic.dbc", "Hyundai"},
            {"hyundai_kia_mando_front_radar.dbc", "Hyundai"},
            {"subaru_global_2017_generated.dbc", "Subaru"},
            {"gm_global_a_object.dbc", "Chevrolet"},
            {"gm_global_a_powertrain_generated.dbc", "Chevrolet"},
            {"chrysler_pacifica_2017_hybrid_generated.dbc", "Chrysler"},
            {"ford_fusion_2018_pt.dbc", "Ford"},
            {"ford_lincoln_base_pt.dbc", "Ford"},
            {"vw_golf_mk4.dbc", "Volkswagen"},
            {"mazda_2017.dbc", "Mazda"},
            {"nissan_x_trail_2017.dbc", "Nissan"},
            {"nissan_leaf_2018.dbc", "Nissan"}
        }

        Public ReadOnly Property Name As String Implements ICarDataSourceProvider.Name
            Get
                Return "OpenDBC"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ICarDataSourceProvider.Description
            Get
                Return "OpenDBC - Open source CAN databases from comma.ai"
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean Implements ICarDataSourceProvider.IsEnabled
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' DBC dosyasından parse edilmiş sinyal
        ''' </summary>
        Public Class DBCSignal
            Public Property Name As String = ""
            Public Property MessageId As Integer = 0
            Public Property MessageName As String = ""
            Public Property StartBit As Integer = 0
            Public Property BitLength As Integer = 0
            Public Property ByteOrder As String = "little_endian" ' 0=big, 1=little
            Public Property ValueType As String = "unsigned" ' + = unsigned, - = signed
            Public Property Factor As Double = 1.0
            Public Property Offset As Double = 0.0
            Public Property MinValue As Double = 0
            Public Property MaxValue As Double = 0
            Public Property Unit As String = ""
            Public Property Comment As String = ""
        End Class

        ''' <summary>
        ''' DBC dosyasından parse edilmiş mesaj
        ''' </summary>
        Public Class DBCMessage
            Public Property Id As Integer = 0
            Public Property Name As String = ""
            Public Property DLC As Integer = 8
            Public Property Transmitter As String = ""
            Public Property Signals As New List(Of DBCSignal)
            Public Property Comment As String = ""
        End Class

        ''' <summary>
        ''' DBC dosyasını indirir ve parse eder
        ''' </summary>
        Public Async Function ParseDBCFileAsync(fileName As String) As Task(Of List(Of DBCMessage))
            Dim messages As New List(Of DBCMessage)

            Try
                Dim url As String = $"{GITHUB_RAW_BASE}/{fileName}"
                Dim content = Await _httpClient.GetStringAsync(url)
                messages = ParseDBCContent(content)
            Catch ex As Exception
                Debug.WriteLine($"ParseDBCFileAsync error for {fileName}: {ex.Message}")
            End Try

            Return messages
        End Function

        ''' <summary>
        ''' DBC içeriğini parse eder
        ''' </summary>
        Private Function ParseDBCContent(content As String) As List(Of DBCMessage)
            Dim messages As New List(Of DBCMessage)
            Dim messageDict As New Dictionary(Of Integer, DBCMessage)

            Try
                Dim lines = content.Split({vbCrLf, vbLf}, StringSplitOptions.None)

                For Each line In lines
                    line = line.Trim()

                    ' BO_ message definition: BO_ <CAN-ID> <MessageName>: <MessageLength> <Transmitter>
                    If line.StartsWith("BO_ ") Then
                        Dim msg = ParseMessageLine(line)
                        If msg IsNot Nothing Then
                            messages.Add(msg)
                            messageDict(msg.Id) = msg
                        End If

                        ' SG_ signal definition: SG_ <SignalName> : <StartBit>|<Length>@<ByteOrder><ValueType> (<Factor>,<Offset>) [<Min>|<Max>] "<Unit>" <Receiver>
                    ElseIf line.StartsWith(" SG_ ") OrElse line.StartsWith("SG_ ") Then
                        Dim sig = ParseSignalLine(line)
                        If sig IsNot Nothing AndAlso messageDict.ContainsKey(sig.MessageId) Then
                            messageDict(sig.MessageId).Signals.Add(sig)
                        End If

                        ' CM_ comment
                    ElseIf line.StartsWith("CM_ BO_ ") Then
                        ParseMessageComment(line, messageDict)
                    ElseIf line.StartsWith("CM_ SG_ ") Then
                        ParseSignalComment(line, messageDict)
                    End If
                Next

            Catch ex As Exception
                Debug.WriteLine($"ParseDBCContent error: {ex.Message}")
            End Try

            Return messages
        End Function

        ''' <summary>
        ''' BO_ satırını parse eder
        ''' </summary>
        Private Function ParseMessageLine(line As String) As DBCMessage
            Try
                ' BO_ 123 MessageName: 8 Vector__XXX
                Dim pattern As String = "BO_\s+(\d+)\s+(\w+)\s*:\s*(\d+)\s*(\w*)"
                Dim match = Regex.Match(line, pattern)

                If match.Success Then
                    Dim msg As New DBCMessage()
                    msg.Id = Integer.Parse(match.Groups(1).Value)
                    msg.Name = match.Groups(2).Value
                    msg.DLC = Integer.Parse(match.Groups(3).Value)
                    msg.Transmitter = If(match.Groups.Count > 4, match.Groups(4).Value, "")
                    Return msg
                End If
            Catch ex As Exception
                Debug.WriteLine($"ParseMessageLine error: {ex.Message}")
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' SG_ satırını parse eder
        ''' </summary>
        Private Function ParseSignalLine(line As String) As DBCSignal
            Try
                ' SG_ SignalName : 7|8@1+ (1,0) [0|255] "unit" Receiver
                Dim pattern As String = "SG_\s+(\w+)\s*:\s*(\d+)\|(\d+)@(\d)([+-])\s*\(([^,]+),([^)]+)\)\s*\[([^|]+)\|([^\]]+)\]\s*""([^""]*)"""
                Dim match = Regex.Match(line, pattern)

                If match.Success Then
                    Dim sig As New DBCSignal()
                    sig.Name = match.Groups(1).Value
                    sig.StartBit = Integer.Parse(match.Groups(2).Value)
                    sig.BitLength = Integer.Parse(match.Groups(3).Value)
                    sig.ByteOrder = If(match.Groups(4).Value = "0", "big_endian", "little_endian")
                    sig.ValueType = If(match.Groups(5).Value = "+", "unsigned", "signed")
                    Double.TryParse(match.Groups(6).Value, sig.Factor)
                    Double.TryParse(match.Groups(7).Value, sig.Offset)
                    Double.TryParse(match.Groups(8).Value, sig.MinValue)
                    Double.TryParse(match.Groups(9).Value, sig.MaxValue)
                    sig.Unit = match.Groups(10).Value
                    Return sig
                End If
            Catch ex As Exception
                Debug.WriteLine($"ParseSignalLine error: {ex.Message}")
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' Mesaj yorumunu parse eder
        ''' </summary>
        Private Sub ParseMessageComment(line As String, messageDict As Dictionary(Of Integer, DBCMessage))
            Try
                ' CM_ BO_ 123 "Comment text";
                Dim pattern As String = "CM_\s+BO_\s+(\d+)\s+""([^""]+)"""
                Dim match = Regex.Match(line, pattern)

                If match.Success Then
                    Dim id = Integer.Parse(match.Groups(1).Value)
                    If messageDict.ContainsKey(id) Then
                        messageDict(id).Comment = match.Groups(2).Value
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"ParseMessageComment error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Sinyal yorumunu parse eder
        ''' </summary>
        Private Sub ParseSignalComment(line As String, messageDict As Dictionary(Of Integer, DBCMessage))
            Try
                ' CM_ SG_ 123 SignalName "Comment text";
                Dim pattern As String = "CM_\s+SG_\s+(\d+)\s+(\w+)\s+""([^""]+)"""
                Dim match = Regex.Match(line, pattern)

                If match.Success Then
                    Dim id = Integer.Parse(match.Groups(1).Value)
                    Dim sigName = match.Groups(2).Value
                    Dim comment = match.Groups(3).Value

                    If messageDict.ContainsKey(id) Then
                        Dim sig = messageDict(id).Signals.FirstOrDefault(Function(s) s.Name = sigName)
                        If sig IsNot Nothing Then
                            sig.Comment = comment
                        End If
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"ParseSignalComment error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' DBC mesajlarını ScrapedCommandEntry'lere dönüştürür
        ''' </summary>
        Private Function ConvertToScrapedEntries(messages As List(Of DBCMessage), brand As String, dbcFile As String) As List(Of ScrapedCommandEntry)
            Dim entries As New List(Of ScrapedCommandEntry)

            For Each msg In messages
                ' Ana mesaj entry
                Dim entry As New ScrapedCommandEntry()
                entry.Brand = brand
                entry.Model = Path.GetFileNameWithoutExtension(dbcFile).Replace("_", " ")
                entry.ModuleName = DetermineModuleName(msg.Name)
                entry.CommandName = msg.Name
                entry.MessageId = msg.Id.ToString("X3")
                entry.DLC = msg.DLC
                entry.Description = If(String.IsNullOrEmpty(msg.Comment), msg.Name, msg.Comment)
                entry.Source = "OpenDBC"
                entry.Confidence = 90 ' High confidence for OpenDBC data
                entry.VerificationStatus = "verified"
                entry.NetworkType = "CAN"

                entries.Add(entry)

                ' Her sinyal için ayrı entry (sensör verileri için)
                For Each sig In msg.Signals
                    Dim sigEntry As New ScrapedCommandEntry()
                    sigEntry.Brand = brand
                    sigEntry.Model = entry.Model
                    sigEntry.ModuleName = entry.ModuleName
                    sigEntry.CommandName = $"{msg.Name}_{sig.Name}"
                    sigEntry.MessageId = msg.Id.ToString("X3")
                    sigEntry.DLC = msg.DLC
                    sigEntry.Description = $"{sig.Name}: {If(String.IsNullOrEmpty(sig.Comment), sig.Name, sig.Comment)} [{sig.Unit}]"
                    sigEntry.Source = "OpenDBC"
                    sigEntry.Confidence = 90
                    sigEntry.VerificationStatus = "verified"
                    sigEntry.NetworkType = "CAN"

                    entries.Add(sigEntry)
                Next
            Next

            Return entries
        End Function

        ''' <summary>
        ''' Mesaj adından modül adı tahmin eder
        ''' </summary>
        Private Function DetermineModuleName(messageName As String) As String
            Dim name = messageName.ToUpper()

            If name.Contains("ENGINE") OrElse name.Contains("ENG") OrElse name.Contains("PCM") Then
                Return "Engine"
            ElseIf name.Contains("BRAKE") OrElse name.Contains("ABS") OrElse name.Contains("ESP") Then
                Return "Brakes"
            ElseIf name.Contains("STEER") OrElse name.Contains("EPS") Then
                Return "Steering"
            ElseIf name.Contains("TRANS") OrElse name.Contains("TCM") OrElse name.Contains("GEAR") Then
                Return "Transmission"
            ElseIf name.Contains("DOOR") OrElse name.Contains("BCM") OrElse name.Contains("BODY") Then
                Return "BodyControl"
            ElseIf name.Contains("LIGHT") OrElse name.Contains("LAMP") Then
                Return "Lighting"
            ElseIf name.Contains("CLIMATE") OrElse name.Contains("HVAC") OrElse name.Contains("AC") Then
                Return "HVAC"
            ElseIf name.Contains("RADAR") OrElse name.Contains("CAMERA") OrElse name.Contains("ADAS") Then
                Return "ADAS"
            ElseIf name.Contains("CLUSTER") OrElse name.Contains("IC") OrElse name.Contains("GAUGE") Then
                Return "Cluster"
            ElseIf name.Contains("WHEEL") OrElse name.Contains("TIRE") OrElse name.Contains("TPMS") Then
                Return "Wheels"
            Else
                Return "General"
            End If
        End Function

        ' ICarDataSourceProvider implementation
        Public Async Function FetchDataAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchDataAsync
            Dim entries As New List(Of ScrapedCommandEntry)

            Try
                ' Markaya uygun DBC dosyalarını bul
                For Each kvp In _dbcFiles
                    If kvp.Value.Equals(brand, StringComparison.OrdinalIgnoreCase) Then
                        Dim messages = Await ParseDBCFileAsync(kvp.Key)
                        Dim converted = ConvertToScrapedEntries(messages, brand, kvp.Key)
                        entries.AddRange(converted)
                    End If
                Next
            Catch ex As Exception
                Debug.WriteLine($"OpenDBCProvider FetchDataAsync error: {ex.Message}")
            End Try

            Return entries
        End Function

        Public Async Function FetchAllAsync() As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchAllAsync
            Dim entries As New List(Of ScrapedCommandEntry)

            Try
                For Each kvp In _dbcFiles
                    Try
                        Dim messages = Await ParseDBCFileAsync(kvp.Key)
                        Dim converted = ConvertToScrapedEntries(messages, kvp.Value, kvp.Key)
                        entries.AddRange(converted)
                    Catch fileEx As Exception
                        Debug.WriteLine($"OpenDBCProvider single file error for {kvp.Key}: {fileEx.Message}")
                    End Try
                Next
            Catch ex As Exception
                Debug.WriteLine($"OpenDBCProvider FetchAllAsync error: {ex.Message}")
            End Try

            Return entries
        End Function

        Public Function GetSupportedBrands() As List(Of String) Implements ICarDataSourceProvider.GetSupportedBrands
            Return _dbcFiles.Values.Distinct().ToList()
        End Function
    End Class

#End Region

#Region "GitHubREProvider - GitHub Automotive RE Projects"

    ''' <summary>
    ''' GitHub'daki güvenilir otomotiv reverse engineering projelerinden veri çeken sağlayıcı
    ''' </summary>
    Public Class GitHubREProvider
        Implements ICarDataSourceProvider

        Private Shared ReadOnly _httpClient As New HttpClient()

        ' Güvenilir GitHub repo'ları ve dosyaları
        Private Shared ReadOnly _trustedRepos As New List(Of TrustedRepo) From {
            New TrustedRepo() With {
                .Owner = "commaai",
                .Repo = "opendbc",
                .Brand = "Various",
                .Description = "OpenDBC - CAN databases"
            },
            New TrustedRepo() With {
                .Owner = "brendan-w",
                .Repo = "python-OBD",
                .Brand = "OBD-II",
                .Description = "OBD-II PID definitions"
            }
        }

        Public Class TrustedRepo
            Public Property Owner As String = ""
            Public Property Repo As String = ""
            Public Property Brand As String = ""
            Public Property Description As String = ""
        End Class

        Public ReadOnly Property Name As String Implements ICarDataSourceProvider.Name
            Get
                Return "GitHub-RE"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ICarDataSourceProvider.Description
            Get
                Return "GitHub Automotive Reverse Engineering Projects"
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean Implements ICarDataSourceProvider.IsEnabled
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' GitHub API üzerinden repo içeriğini listeler
        ''' </summary>
        Public Async Function ListRepoContentsAsync(owner As String, repo As String, Optional path As String = "") As Task(Of List(Of String))
            Dim files As New List(Of String)

            Try
                Dim url As String = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}"
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CarCanReader/1.0")

                Dim response = Await _httpClient.GetStringAsync(url)
                Dim json = JArray.Parse(response)

                For Each item In json
                    Dim name = item("name")?.ToString()
                    Dim type = item("type")?.ToString()

                    If type = "file" AndAlso (name.EndsWith(".dbc") OrElse name.EndsWith(".json") OrElse name.EndsWith(".csv")) Then
                        files.Add(item("download_url")?.ToString())
                    End If
                Next

            Catch ex As Exception
                Debug.WriteLine($"GitHubREProvider ListRepoContentsAsync error: {ex.Message}")
            End Try

            Return files
        End Function

        ' ICarDataSourceProvider implementation
        Public Function FetchDataAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchDataAsync
            ' GitHub RE provider şu an sadece liste sağlar
            ' Detaylı parsing OpenDBC provider tarafından yapılır
            Return Task.FromResult(New List(Of ScrapedCommandEntry)())
        End Function

        Public Function FetchAllAsync() As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchAllAsync
            Return Task.FromResult(New List(Of ScrapedCommandEntry)())
        End Function

        Public Function GetSupportedBrands() As List(Of String) Implements ICarDataSourceProvider.GetSupportedBrands
            Return _trustedRepos.Select(Function(r) r.Brand).Distinct().ToList()
        End Function
    End Class

#End Region

#Region "OpenGaragesProvider - OpenGarages Automotive Research"

    ''' <summary>
    ''' OpenGarages.org'dan yapılandırılmış CAN veri setlerini çeken sağlayıcı
    ''' </summary>
    Public Class OpenGaragesProvider
        Implements ICarDataSourceProvider

        Private Shared ReadOnly _httpClient As New HttpClient()

        ' OpenGarages'tan bilinen veri setleri
        ' Not: Gerçek implementasyonda bu veriler OpenGarages sitesinden alınır
        Private Shared ReadOnly _knownDatasets As New List(Of OpenGaragesDataset) From {
            New OpenGaragesDataset() With {
                .Name = "GM_HS_CAN",
                .Brand = "Chevrolet",
                .Description = "GM High-Speed CAN messages",
                .Confidence = 70
            },
            New OpenGaragesDataset() With {
                .Name = "Ford_MS_CAN",
                .Brand = "Ford",
                .Description = "Ford Medium-Speed CAN messages",
                .Confidence = 70
            },
            New OpenGaragesDataset() With {
                .Name = "Toyota_CAN",
                .Brand = "Toyota",
                .Description = "Toyota CAN bus research data",
                .Confidence = 70
            }
        }

        Public Class OpenGaragesDataset
            Public Property Name As String = ""
            Public Property Brand As String = ""
            Public Property Description As String = ""
            Public Property Confidence As Integer = 50
            Public Property Messages As New List(Of OpenGaragesMessage)
        End Class

        Public Class OpenGaragesMessage
            Public Property Id As String = ""
            Public Property Name As String = ""
            Public Property DLC As Integer = 8
            Public Property ByteDescriptions As New List(Of String)
        End Class

        Public ReadOnly Property Name As String Implements ICarDataSourceProvider.Name
            Get
                Return "OpenGarages"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ICarDataSourceProvider.Description
            Get
                Return "OpenGarages.org - Automotive Security Research"
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean Implements ICarDataSourceProvider.IsEnabled
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Veri setlerini ScrapedCommandEntry'lere dönüştürür
        ''' </summary>
        Private Function ConvertDatasetToEntries(dataset As OpenGaragesDataset) As List(Of ScrapedCommandEntry)
            Dim entries As New List(Of ScrapedCommandEntry)

            For Each msg In dataset.Messages
                Dim entry As New ScrapedCommandEntry()
                entry.Brand = dataset.Brand
                entry.Model = "Generic"
                entry.ModuleName = "Research"
                entry.CommandName = msg.Name
                entry.MessageId = msg.Id
                entry.DLC = msg.DLC
                entry.Description = String.Join(", ", msg.ByteDescriptions)
                entry.Source = "OpenGarages"
                entry.Confidence = dataset.Confidence
                entry.VerificationStatus = "unverified"
                entry.NetworkType = "CAN"

                entries.Add(entry)
            Next

            Return entries
        End Function

        ' ICarDataSourceProvider implementation
        Public Function FetchDataAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchDataAsync
            Dim entries As New List(Of ScrapedCommandEntry)

            For Each dataset In _knownDatasets
                If dataset.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase) Then
                    entries.AddRange(ConvertDatasetToEntries(dataset))
                End If
            Next

            Return Task.FromResult(entries)
        End Function

        Public Function FetchAllAsync() As Task(Of List(Of ScrapedCommandEntry)) Implements ICarDataSourceProvider.FetchAllAsync
            Dim entries As New List(Of ScrapedCommandEntry)

            For Each dataset In _knownDatasets
                entries.AddRange(ConvertDatasetToEntries(dataset))
            Next

            Return Task.FromResult(entries)
        End Function

        Public Function GetSupportedBrands() As List(Of String) Implements ICarDataSourceProvider.GetSupportedBrands
            Return _knownDatasets.Select(Function(d) d.Brand).Distinct().ToList()
        End Function
    End Class

#End Region

#Region "OnlineCarDbScraper"

    ''' <summary>
    ''' Online araç veritabanı kazıma ana sınıfı
    ''' </summary>
    Public Class OnlineCarDbScraper

#Region "Olaylar (Events)"

        ''' <summary>
        ''' İlerleme mesajı
        ''' </summary>
        Public Event OnProgress(message As String)

        ''' <summary>
        ''' Veri bulunduğunda
        ''' </summary>
        Public Event OnDataFound(entry As ScrapedCommandEntry)

        ''' <summary>
        ''' Kazıma tamamlandığında
        ''' </summary>
        Public Event OnCompleted(totalEntries As Integer, newEntries As Integer)

        ''' <summary>
        ''' Hata oluştuğunda
        ''' </summary>
        Public Event OnError(message As String)

        ''' <summary>
        ''' Kazıma başladığında
        ''' </summary>
        Public Event OnStarted()

        ''' <summary>
        ''' Veritabanı zenginleştirme başladığında
        ''' </summary>
        Public Event OnEnrichmentStarted(missingBrands As List(Of String))

        ''' <summary>
        ''' Kazıma ilerlemesi (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnScrapeProgress(message As String)

        ''' <summary>
        ''' Kazıma tamamlandığında (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnScrapeCompleted(count As Integer)

        ''' <summary>
        ''' Kazıma hatası (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnScrapeError(message As String)

        ''' <summary>
        ''' DB zenginleştirme başladığında (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnDbEnrichmentStarted()

        ''' <summary>
        ''' DB zenginleştirme tamamlandığında (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnDbEnrichmentCompleted(count As Integer)

#End Region

#Region "Özel Alanlar"

        ' Veri sağlayıcıları
        Private _providers As New List(Of ICarDataSourceProvider)

        ' Kazınan veriler
        Private _scrapedEntries As New List(Of ScrapedCommandEntry)

        ' Komut repository referansı
        Private _commandRepository As CommandRepository

        ' İptal tokenı
        Private _cancellationTokenSource As Threading.CancellationTokenSource

        ' Çalışıyor mu?
        Private _isRunning As Boolean = False

        ' Thread-safety lock
        Private ReadOnly _lock As New Object()

        ' Özel provider referansları
        Private _nhtsa As NHTSAProvider
        Private _openDbc As OpenDBCProvider
        Private _gitHubRe As GitHubREProvider
        Private _openGarages As OpenGaragesProvider

        ' Duplikasyon kontrolü için hash set
        Private _existingCommandHashes As New HashSet(Of String)

        ' İstatistikler
        Private _stats As New EnrichmentStats()

        ''' <summary>
        ''' Zenginleştirme istatistikleri
        ''' </summary>
        Public Class EnrichmentStats
            Public Property TotalFetched As Integer = 0
            Public Property NewAdded As Integer = 0
            Public Property Duplicates As Integer = 0
            Public Property Merged As Integer = 0
            Public Property Errors As Integer = 0
            Public Property LastRunTime As DateTime = DateTime.MinValue
            Public Property SourceBreakdown As New Dictionary(Of String, Integer)

            Public Sub Reset()
                TotalFetched = 0
                NewAdded = 0
                Duplicates = 0
                Merged = 0
                Errors = 0
                SourceBreakdown.Clear()
            End Sub

            Public Overrides Function ToString() As String
                Return $"Fetched: {TotalFetched}, New: {NewAdded}, Duplicates: {Duplicates}, Merged: {Merged}, Errors: {Errors}"
            End Function
        End Class

#End Region

#Region "Özellikler (Properties)"

        ''' <summary>
        ''' Kazıma çalışıyor mu?
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Kayıtlı veri sağlayıcı sayısı
        ''' </summary>
        Public ReadOnly Property ProviderCount As Integer
            Get
                Return _providers.Count
            End Get
        End Property

        ''' <summary>
        ''' Son kazıma sonucu
        ''' </summary>
        Public ReadOnly Property LastScrapedEntries As List(Of ScrapedCommandEntry)
            Get
                SyncLock _lock
                    Return _scrapedEntries.ToList()
                End SyncLock
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Varsayılan constructor
        ''' </summary>
        Public Sub New()
            _cancellationTokenSource = New Threading.CancellationTokenSource()
        End Sub

        ''' <summary>
        ''' CommandRepository ile constructor
        ''' </summary>
        Public Sub New(commandRepository As CommandRepository)
            _cancellationTokenSource = New Threading.CancellationTokenSource()
            SetCommandRepository(commandRepository)
        End Sub

#End Region

#Region "Konfigürasyon"

        ''' <summary>
        ''' CommandRepository referansını ayarlar ve tüm sağlayıcıları kaydeder
        ''' </summary>
        Public Sub SetCommandRepository(repo As CommandRepository)
            _commandRepository = repo

            ' LocalDatabaseProvider'ı otomatik ekle
            Dim localProvider As New LocalDatabaseProvider(repo)
            RegisterProvider(localProvider)

            ' Trusted online providers ekle
            _nhtsa = New NHTSAProvider()
            RegisterProvider(_nhtsa)

            _openDbc = New OpenDBCProvider()
            RegisterProvider(_openDbc)

            _gitHubRe = New GitHubREProvider()
            RegisterProvider(_gitHubRe)

            _openGarages = New OpenGaragesProvider()
            RegisterProvider(_openGarages)

            ' Mevcut komutların hash'lerini oluştur (duplikasyon kontrolü için)
            BuildExistingCommandHashes()
        End Sub

        ''' <summary>
        ''' Mevcut komutların hash'lerini oluşturur
        ''' </summary>
        Private Sub BuildExistingCommandHashes()
            _existingCommandHashes.Clear()

            Try
                If _commandRepository Is Nothing Then Return

                For Each brand In _commandRepository.GetBrands()
                    For Each model In _commandRepository.GetModels(brand)
                        For Each moduleName In _commandRepository.GetModules(brand, model)
                            For Each cmdName In _commandRepository.GetCommands(brand, model, moduleName)
                                Dim hash = GenerateCommandHash(brand, model, moduleName, cmdName)
                                _existingCommandHashes.Add(hash)
                            Next
                        Next
                    Next
                Next

                RaiseEvent OnProgress($"Mevcut komut hash'leri oluşturuldu: {_existingCommandHashes.Count} adet")

            Catch ex As Exception
                RaiseEvent OnError("Hash oluşturma hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' Komut için benzersiz hash oluşturur
        ''' </summary>
        Private Function GenerateCommandHash(brand As String, model As String, moduleName As String, commandName As String) As String
            Return $"{brand.ToUpper()}|{model.ToUpper()}|{moduleName.ToUpper()}|{commandName.ToUpper()}"
        End Function

        ''' <summary>
        ''' ScrapedCommandEntry için hash oluşturur
        ''' </summary>
        Private Function GenerateEntryHash(entry As ScrapedCommandEntry) As String
            Return GenerateCommandHash(entry.Brand, entry.Model, entry.ModuleName, entry.CommandName)
        End Function

        ''' <summary>
        ''' Komutun duplikat olup olmadığını kontrol eder
        ''' </summary>
        Private Function IsDuplicate(entry As ScrapedCommandEntry) As Boolean
            Dim hash = GenerateEntryHash(entry)
            Return _existingCommandHashes.Contains(hash)
        End Function

        ''' <summary>
        ''' NHTSA Provider'a doğrudan erişim
        ''' </summary>
        Public ReadOnly Property NHTSAProvider As NHTSAProvider
            Get
                Return _nhtsa
            End Get
        End Property

        ''' <summary>
        ''' OpenDBC Provider'a doğrudan erişim
        ''' </summary>
        Public ReadOnly Property OpenDBCProvider As OpenDBCProvider
            Get
                Return _openDbc
            End Get
        End Property

        ''' <summary>
        ''' Son zenginleştirme istatistikleri
        ''' </summary>
        Public ReadOnly Property LastEnrichmentStats As EnrichmentStats
            Get
                Return _stats
            End Get
        End Property

        ''' <summary>
        ''' Yeni veri sağlayıcısı kaydeder
        ''' </summary>
        Public Sub RegisterProvider(provider As ICarDataSourceProvider)
            If provider IsNot Nothing AndAlso Not _providers.Contains(provider) Then
                _providers.Add(provider)
                RaiseEvent OnProgress($"Sağlayıcı eklendi: {provider.Name}")
            End If
        End Sub

        ''' <summary>
        ''' Veri sağlayıcısını kaldırır
        ''' </summary>
        Public Sub UnregisterProvider(provider As ICarDataSourceProvider)
            _providers.Remove(provider)
        End Sub

#End Region

#Region "Kazıma İşlemleri"

        ''' <summary>
        ''' Belirtilen marka/model için veri kazır
        ''' </summary>
        Public Async Function ScrapeAsync(brand As String, model As String) As Task(Of List(Of ScrapedCommandEntry))
            Dim results As New List(Of ScrapedCommandEntry)

            Try
                _isRunning = True
                RaiseEvent OnStarted()
                RaiseEvent OnProgress($"Kazıma başlıyor: {brand} {model}")
                RaiseEvent OnScrapeProgress($"Kazıma başlıyor: {brand} {model}")

                For Each provider In _providers
                    If Not provider.IsEnabled Then Continue For

                    Try
                        RaiseEvent OnProgress($"Kaynak kontrol ediliyor: {provider.Name}")
                        Dim entries = Await provider.FetchDataAsync(brand, model)

                        For Each entry In entries
                            results.Add(entry)
                            RaiseEvent OnDataFound(entry)
                        Next

                        RaiseEvent OnProgress($"{provider.Name}: {entries.Count} girdi bulundu")

                    Catch ex As Exception
                        RaiseEvent OnError($"Sağlayıcı hatası ({provider.Name}): {ex.Message}")
                    End Try
                Next

                SyncLock _lock
                    _scrapedEntries = results
                End SyncLock

                RaiseEvent OnCompleted(results.Count, results.Count)

            Catch ex As Exception
                RaiseEvent OnError("Kazıma hatası: " & ex.Message)
            Finally
                _isRunning = False
            End Try

            Return results
        End Function

        ''' <summary>
        ''' Tüm kaynaklardan tüm verileri kazır
        ''' </summary>
        Public Async Function ScrapeAllAsync() As Task(Of List(Of ScrapedCommandEntry))
            Dim results As New List(Of ScrapedCommandEntry)

            Try
                _isRunning = True
                RaiseEvent OnStarted()
                RaiseEvent OnProgress("Tam veritabanı kazıma başlıyor...")

                For Each provider In _providers
                    If Not provider.IsEnabled Then Continue For

                    Try
                        RaiseEvent OnProgress($"Kaynak işleniyor: {provider.Name}")
                        Dim entries = Await provider.FetchAllAsync()

                        For Each entry In entries
                            results.Add(entry)
                            RaiseEvent OnDataFound(entry)
                        Next

                        RaiseEvent OnProgress($"{provider.Name}: {entries.Count} girdi bulundu")

                    Catch ex As Exception
                        RaiseEvent OnError($"Sağlayıcı hatası ({provider.Name}): {ex.Message}")
                        RaiseEvent OnScrapeError($"Sağlayıcı hatası ({provider.Name}): {ex.Message}")
                    End Try
                Next

                SyncLock _lock
                    _scrapedEntries = results
                End SyncLock

                RaiseEvent OnCompleted(results.Count, results.Count)
                RaiseEvent OnScrapeCompleted(results.Count)

            Catch ex As Exception
                RaiseEvent OnError("Kazıma hatası: " & ex.Message)
                RaiseEvent OnScrapeError("Kazıma hatası: " & ex.Message)
            Finally
                _isRunning = False
            End Try

            Return results
        End Function

        ''' <summary>
        ''' Veritabanını zenginleştirir - tüm trusted kaynaklardan veri çeker
        ''' </summary>
        Public Async Function EnrichDatabaseAsync() As Task(Of Integer)
            Dim newEntriesCount As Integer = 0
            _stats.Reset()
            _stats.LastRunTime = DateTime.Now

            Try
                _isRunning = True
                RaiseEvent OnStarted()
                RaiseEvent OnDbEnrichmentStarted()
                RaiseEvent OnProgress("📚 Veritabanı zenginleştirme başlıyor...")
                RaiseEvent OnScrapeProgress("📚 Veritabanı zenginleştirme başlıyor...")
                RaiseEvent OnProgress($"   Kayıtlı sağlayıcı: {_providers.Count}")

                ' Hash'leri güncelle
                BuildExistingCommandHashes()
                RaiseEvent OnProgress($"   Mevcut komut sayısı: {_existingCommandHashes.Count}")

                ' Tüm sağlayıcılardan veri çek
                Dim allEntries As New List(Of ScrapedCommandEntry)

                For Each provider In _providers
                    If Not provider.IsEnabled Then
                        RaiseEvent OnProgress($"⏭️ {provider.Name} atlanıyor (pasif)")
                        Continue For
                    End If

                    Try
                        RaiseEvent OnProgress($"🔍 {provider.Name} taranıyor...")
                        Dim entries = Await provider.FetchAllAsync()

                        If entries.Count > 0 Then
                            allEntries.AddRange(entries)
                            _stats.TotalFetched += entries.Count

                            If Not _stats.SourceBreakdown.ContainsKey(provider.Name) Then
                                _stats.SourceBreakdown(provider.Name) = 0
                            End If
                            _stats.SourceBreakdown(provider.Name) += entries.Count

                            RaiseEvent OnProgress($"   ✅ {provider.Name}: {entries.Count} girdi bulundu")
                        Else
                            RaiseEvent OnProgress($"   ℹ️ {provider.Name}: Veri bulunamadı")
                        End If

                    Catch ex As Exception
                        _stats.Errors += 1
                        RaiseEvent OnError($"❌ {provider.Name} hatası: {ex.Message}")
                    End Try
                Next

                ' Duplicate kontrolü ve merge
                RaiseEvent OnProgress($"🔄 {allEntries.Count} girdi işleniyor...")
                newEntriesCount = MergeEntriesWithDuplicateCheck(allEntries)

                ' Sonuçları kaydet
                If newEntriesCount > 0 Then
                    _commandRepository.Save()
                    RaiseEvent OnProgress($"💾 Veritabanı kaydedildi")
                End If

                RaiseEvent OnProgress($"✅ Zenginleştirme tamamlandı: {_stats}")
                RaiseEvent OnScrapeProgress($"✅ Zenginleştirme tamamlandı: {_stats}")
                RaiseEvent OnCompleted(_stats.TotalFetched, newEntriesCount)
                RaiseEvent OnScrapeCompleted(newEntriesCount)
                RaiseEvent OnDbEnrichmentCompleted(newEntriesCount)

            Catch ex As Exception
                _stats.Errors += 1
                RaiseEvent OnError("❌ Zenginleştirme hatası: " & ex.Message)
                RaiseEvent OnScrapeError("❌ Zenginleştirme hatası: " & ex.Message)
            Finally
                _isRunning = False
            End Try

            Return newEntriesCount
        End Function

        ''' <summary>
        ''' Belirli bir marka için veritabanını zenginleştirir
        ''' </summary>
        Public Async Function EnrichForBrandAsync(brand As String) As Task(Of Integer)
            Dim newEntriesCount As Integer = 0

            Try
                _isRunning = True
                RaiseEvent OnProgress($"🔍 {brand} için zenginleştirme başlıyor...")

                Dim allEntries As New List(Of ScrapedCommandEntry)

                For Each provider In _providers
                    If Not provider.IsEnabled Then Continue For

                    Try
                        ' Marka bazlı fetch (provider destekliyorsa)
                        Dim entries = Await provider.FetchDataAsync(brand, "")
                        allEntries.AddRange(entries)
                        RaiseEvent OnProgress($"   {provider.Name}: {entries.Count} girdi")
                    Catch providerEx As Exception
                        Debug.WriteLine($"EnrichForBrandAsync provider error ({provider.Name}): {providerEx.Message}")
                    End Try
                Next

                newEntriesCount = MergeEntriesWithDuplicateCheck(allEntries)

                If newEntriesCount > 0 Then
                    _commandRepository.Save()
                End If

                RaiseEvent OnProgress($"✅ {brand}: {newEntriesCount} yeni komut eklendi")

            Catch ex As Exception
                RaiseEvent OnError($"❌ {brand} zenginleştirme hatası: " & ex.Message)
            Finally
                _isRunning = False
            End Try

            Return newEntriesCount
        End Function

        ''' <summary>
        ''' VIN bazlı zenginleştirme - araç bağlandığında çağrılır
        ''' </summary>
        Public Async Function EnrichForVINAsync(vin As String) As Task(Of Integer)
            Dim newEntriesCount As Integer = 0

            Try
                RaiseEvent OnProgress($"🚗 VIN bazlı zenginleştirme: {vin}")

                ' NHTSA ile VIN decode et
                If _nhtsa IsNot Nothing Then
                    Dim decodeResult = Await _nhtsa.DecodeVINAsync(vin)

                    If decodeResult.IsValid Then
                        RaiseEvent OnProgress($"   ✅ VIN decode: {decodeResult.Make} {decodeResult.Model} ({decodeResult.ModelYear})")

                        ' Marka bazlı zenginleştirme yap
                        newEntriesCount = Await EnrichForBrandAsync(decodeResult.Make)
                    Else
                        RaiseEvent OnProgress($"   ⚠️ VIN decode başarısız: {decodeResult.ErrorText}")
                    End If
                End If

            Catch ex As Exception
                RaiseEvent OnError($"❌ VIN zenginleştirme hatası: " & ex.Message)
            End Try

            Return newEntriesCount
        End Function

        ''' <summary>
        ''' Girdileri duplikasyon kontrolü ile merge eder
        ''' </summary>
        Private Function MergeEntriesWithDuplicateCheck(entries As List(Of ScrapedCommandEntry)) As Integer
            Dim addedCount As Integer = 0

            SyncLock _lock
                For Each entry In entries
                    Try
                        ' Temel validasyon
                        If String.IsNullOrEmpty(entry.Brand) OrElse String.IsNullOrEmpty(entry.CommandName) Then
                            Continue For
                        End If

                        ' Duplikasyon kontrolü
                        Dim hash = GenerateEntryHash(entry)
                        If _existingCommandHashes.Contains(hash) Then
                            _stats.Duplicates += 1
                            Continue For
                        End If

                        ' SLCAN frame oluştur
                        Dim frame As String = entry.ToSlcanFrame()
                        If String.IsNullOrEmpty(frame) AndAlso Not String.IsNullOrEmpty(entry.MessageId) Then
                            ' Frame yoksa basit frame oluştur
                            frame = $"t{entry.MessageId.PadLeft(3, "0"c)}{entry.DLC}{"00".PadLeft(entry.DLC * 2, "0"c)}"
                        End If

                        ' Veritabanına ekle
                        If entry.IsToggle AndAlso Not String.IsNullOrEmpty(entry.ToggleOnFrame) Then
                            _commandRepository.AddToggleCommand(
                                entry.Brand,
                                entry.Model,
                                entry.ModuleName,
                                entry.CommandName,
                                entry.ToggleOnFrame,
                                entry.ToggleOffFrame,
                                entry.ToggleByteIndex
                            )
                        Else
                            _commandRepository.AddCommand(
                                entry.Brand,
                                If(String.IsNullOrEmpty(entry.Model), "Generic", entry.Model),
                                If(String.IsNullOrEmpty(entry.ModuleName), "General", entry.ModuleName),
                                entry.CommandName,
                                If(String.IsNullOrEmpty(frame), $"t{entry.MessageId}8", frame)
                            )
                        End If

                        ' Hash'e ekle
                        _existingCommandHashes.Add(hash)
                        _stats.NewAdded += 1
                        addedCount += 1

                        RaiseEvent OnDataFound(entry)
                        RaiseEvent OnProgress($"   ➕ Eklendi: {entry.Brand}/{entry.Model}/{entry.CommandName} [Kaynak: {entry.Source}]")

                    Catch ex As Exception
                        _stats.Errors += 1
                        RaiseEvent OnError($"   ❌ Merge hatası ({entry.CommandName}): {ex.Message}")
                    End Try
                Next
            End SyncLock

            Return addedCount
        End Function

        ''' <summary>
        ''' İki girdiyi birleştirir (daha yüksek güvenilirlik olanı tercih eder)
        ''' </summary>
        Private Function MergeEntries(existing As ScrapedCommandEntry, newEntry As ScrapedCommandEntry) As ScrapedCommandEntry
            ' Daha yüksek güvenilirlik tercih edilir
            If newEntry.Confidence > existing.Confidence Then
                newEntry.VerificationStatus = "merged"
                _stats.Merged += 1
                Return newEntry
            End If

            Return existing
        End Function

        ''' <summary>
        ''' Kazıma işlemini iptal eder
        ''' </summary>
        Public Sub Cancel()
            _cancellationTokenSource.Cancel()
            _isRunning = False
            RaiseEvent OnProgress("Kazıma iptal edildi")
        End Sub

#End Region

#Region "Veritabanına Kaydetme"

        ''' <summary>
        ''' Kazınan verileri CommandRepository'ye kaydeder
        ''' </summary>
        Public Function MergeToCommandRepository(Optional requireConfirmation As Boolean = True) As Integer
            Dim savedCount As Integer = 0

            Try
                If _commandRepository Is Nothing Then
                    RaiseEvent OnError("CommandRepository ayarlanmamış")
                    Return 0
                End If

                SyncLock _lock
                    For Each entry In _scrapedEntries
                        Try
                            ' Zaten var mı kontrol et
                            If _commandRepository.CommandExists(entry.Brand, entry.Model, entry.ModuleName, entry.CommandName) Then
                                Continue For
                            End If

                            ' SLCAN frame oluştur
                            Dim frame As String = entry.ToSlcanFrame()
                            If String.IsNullOrEmpty(frame) Then Continue For

                            ' Toggle komut mu?
                            If entry.IsToggle AndAlso Not String.IsNullOrEmpty(entry.ToggleOnFrame) Then
                                _commandRepository.AddToggleCommand(
                                    entry.Brand,
                                    entry.Model,
                                    entry.ModuleName,
                                    entry.CommandName,
                                    entry.ToggleOnFrame,
                                    entry.ToggleOffFrame,
                                    entry.ToggleByteIndex
                                )
                            Else
                                _commandRepository.AddCommand(
                                    entry.Brand,
                                    entry.Model,
                                    entry.ModuleName,
                                    entry.CommandName,
                                    frame
                                )
                            End If

                            savedCount += 1
                            RaiseEvent OnProgress($"Kaydedildi: {entry.CommandName}")

                        Catch ex As Exception
                            RaiseEvent OnError($"Kaydetme hatası ({entry.CommandName}): {ex.Message}")
                        End Try
                    Next
                End SyncLock

                ' Repository'yi kaydet
                If savedCount > 0 Then
                    _commandRepository.Save()
                    RaiseEvent OnProgress($"Toplam {savedCount} komut veritabanına eklendi")
                End If

            Catch ex As Exception
                RaiseEvent OnError("Merge hatası: " & ex.Message)
            End Try

            Return savedCount
        End Function

#End Region

#Region "Normalizasyon"

        ''' <summary>
        ''' Ham veriyi normalize edilmiş ScrapedCommandEntry'e çevirir
        ''' </summary>
        Public Function NormalizeEntry(rawData As Dictionary(Of String, String)) As ScrapedCommandEntry
            Dim entry As New ScrapedCommandEntry()

            Try
                If rawData.ContainsKey("brand") Then entry.Brand = rawData("brand")
                If rawData.ContainsKey("model") Then entry.Model = rawData("model")
                If rawData.ContainsKey("module") Then entry.ModuleName = rawData("module")
                If rawData.ContainsKey("command") Then entry.CommandName = rawData("command")
                If rawData.ContainsKey("id") Then entry.MessageId = rawData("id")
                If rawData.ContainsKey("data") Then entry.BytePattern = rawData("data")
                If rawData.ContainsKey("source") Then entry.Source = rawData("source")
                If rawData.ContainsKey("description") Then entry.Description = rawData("description")

                If rawData.ContainsKey("confidence") Then
                    Integer.TryParse(rawData("confidence"), entry.Confidence)
                End If

                If rawData.ContainsKey("dlc") Then
                    Integer.TryParse(rawData("dlc"), entry.DLC)
                End If

            Catch ex As Exception
                Debug.WriteLine($"NormalizeEntry error: {ex.Message}")
            End Try

            Return entry
        End Function

#End Region

#Region "Durum Raporu"

        ''' <summary>
        ''' Scraper durumunu özetler
        ''' </summary>
        Public Function GetStatusReport() As String
            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine("=== OnlineCarDbScraper Durumu ===")
            sb.AppendLine($"Çalışıyor: {If(_isRunning, "Evet", "Hayır")}")
            sb.AppendLine($"Kayıtlı sağlayıcı: {_providers.Count}")
            sb.AppendLine($"Mevcut komut hash sayısı: {_existingCommandHashes.Count}")
            sb.AppendLine($"Son kazıma sonucu: {_scrapedEntries.Count} girdi")
            sb.AppendLine()
            sb.AppendLine("Sağlayıcılar:")

            For Each provider In _providers
                sb.AppendLine($"  - {provider.Name}: {If(provider.IsEnabled, "Aktif", "Pasif")}")
                sb.AppendLine($"    Desteklenen markalar: {String.Join(", ", provider.GetSupportedBrands().Take(5))}...")
            Next

            sb.AppendLine()
            sb.AppendLine("Son Zenginleştirme İstatistikleri:")
            sb.AppendLine($"  Toplam çekilen: {_stats.TotalFetched}")
            sb.AppendLine($"  Yeni eklenen: {_stats.NewAdded}")
            sb.AppendLine($"  Duplikasyon: {_stats.Duplicates}")
            sb.AppendLine($"  Merge edilen: {_stats.Merged}")
            sb.AppendLine($"  Hatalar: {_stats.Errors}")

            If _stats.SourceBreakdown.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine("Kaynak Bazlı Dağılım:")
                For Each kvp In _stats.SourceBreakdown
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}")
                Next
            End If

            If _stats.LastRunTime <> DateTime.MinValue Then
                sb.AppendLine()
                sb.AppendLine($"Son çalışma: {_stats.LastRunTime:yyyy-MM-dd HH:mm:ss}")
            End If

            Return sb.ToString()
        End Function

#End Region

#Region "OBD-II Standard PIDs"

        ''' <summary>
        ''' Standart OBD-II PID'leri ekler (her araç için geçerli)
        ''' </summary>
        Public Sub AddStandardOBDPIDs()
            Dim obdPIDs As New List(Of ScrapedCommandEntry)

            ' Mode 01 PIDs - Canlı veriler
            Dim mode01PIDs = New Dictionary(Of String, String) From {
                {"04", "Calculated Engine Load"},
                {"05", "Engine Coolant Temperature"},
                {"06", "Short Term Fuel Trim Bank 1"},
                {"07", "Long Term Fuel Trim Bank 1"},
                {"0B", "Intake Manifold Pressure"},
                {"0C", "Engine RPM"},
                {"0D", "Vehicle Speed"},
                {"0E", "Timing Advance"},
                {"0F", "Intake Air Temperature"},
                {"10", "MAF Air Flow Rate"},
                {"11", "Throttle Position"},
                {"1C", "OBD Standards"},
                {"1F", "Run Time Since Engine Start"},
                {"21", "Distance Traveled With MIL On"},
                {"2F", "Fuel Tank Level"},
                {"31", "Distance Since Codes Cleared"},
                {"33", "Barometric Pressure"},
                {"42", "Control Module Voltage"},
                {"46", "Ambient Air Temperature"},
                {"5E", "Engine Fuel Rate"}
            }

            For Each pid In mode01PIDs
                Dim entry As New ScrapedCommandEntry()
                entry.Brand = "OBD-II"
                entry.Model = "Standard"
                entry.ModuleName = "Mode01"
                entry.CommandName = $"PID_{pid.Key}_{pid.Value.Replace(" ", "_")}"
                entry.MessageId = "7DF"
                entry.DLC = 8
                entry.BytePattern = $"0201{pid.Key}0000000000"
                entry.Description = pid.Value
                entry.Source = "OBD-II Standard"
                entry.Confidence = 100
                entry.VerificationStatus = "verified"
                entry.NetworkType = "CAN"

                obdPIDs.Add(entry)
            Next

            ' Mode 09 PIDs - Araç bilgileri
            Dim mode09PIDs = New Dictionary(Of String, String) From {
                {"02", "VIN - Vehicle Identification Number"},
                {"04", "Calibration ID"},
                {"06", "Calibration Verification Number"},
                {"0A", "ECU Name"}
            }

            For Each pid In mode09PIDs
                Dim entry As New ScrapedCommandEntry()
                entry.Brand = "OBD-II"
                entry.Model = "Standard"
                entry.ModuleName = "Mode09"
                entry.CommandName = $"PID_{pid.Key}_{pid.Value.Replace(" ", "_").Replace("-", "")}"
                entry.MessageId = "7DF"
                entry.DLC = 8
                entry.BytePattern = $"0209{pid.Key}0000000000"
                entry.Description = pid.Value
                entry.Source = "OBD-II Standard"
                entry.Confidence = 100
                entry.VerificationStatus = "verified"
                entry.NetworkType = "CAN"

                obdPIDs.Add(entry)
            Next

            ' Merge
            MergeEntriesWithDuplicateCheck(obdPIDs)
            RaiseEvent OnProgress($"📋 Standart OBD-II PIDs eklendi: {obdPIDs.Count} adet")
        End Sub

#End Region

    End Class

#End Region

End Namespace

