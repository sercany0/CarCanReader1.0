' VehicleProfileManager.vb
' VIN bazlı araç profili yönetim servisi
' Unified /data/ folder for all configurations
'
' SAFE JSON HANDLING:
'   - Centralized path via Application.StartupPath + /data/
'   - Atomic writes with temp file + validation
'   - Automatic backup before overwrite
'   - Never corrupts existing data on invalid save
'
' Özellikler:
'   - VIN → Marka/Model/Yıl eşleştirme
'   - Protokol ve komut seti yükleme
'   - vehicleProfiles.json ile persistans
'   - Otomatik veya manuel profil yükleme
'
' Kullanım:
'   Dim profileMgr As New VehicleProfileManager()
'   AddHandler profileMgr.OnProfileLoaded, Sub(p) Console.WriteLine("Loaded: " & p.Brand)
'   profileMgr.LoadProfiles()
'   
'   ' VIN algılandığında:
'   If profileMgr.TryLoadProfileByVin(vinString) Then
'       ' Profil yüklendi
'   End If

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.IO
Imports System.Windows.Forms

Namespace Services

#Region "VehicleProfile Model"

    ''' <summary>
    ''' Araç profil bilgilerini tutan model sınıfı
    ''' </summary>
    Public Class VehicleProfile
        ''' <summary>
        ''' 17 karakterli VIN numarası
        ''' </summary>
        Public Property VIN As String = ""

        ''' <summary>
        ''' Araç markası (örn: Volkswagen, BMW)
        ''' </summary>
        Public Property Brand As String = ""

        ''' <summary>
        ''' Araç modeli (örn: Golf, F30)
        ''' </summary>
        Public Property Model As String = ""

        ''' <summary>
        ''' Üretim yılı
        ''' </summary>
        Public Property Year As Integer = 0

        ''' <summary>
        ''' CAN protokolü (örn: "CAN_HS", "CAN_LS", "ISO-TP")
        ''' </summary>
        Public Property Protocol As String = ""

        ''' <summary>
        ''' Bilinen komut kategorileri listesi
        ''' </summary>
        Public Property KnownModules As New List(Of String)

        ''' <summary>
        ''' Ek notlar ve açıklamalar
        ''' </summary>
        Public Property Notes As String = ""

        ''' <summary>
        ''' Veri kaynağı (örn: "user", "obd_scan", "online_scrape")
        ''' </summary>
        Public Property Source As String = "user"

        ''' <summary>
        ''' Veri güvenilirlik skoru (0-100)
        ''' </summary>
        Public Property Confidence As Integer = 100

        ''' <summary>
        ''' Profilin oluşturulma tarihi
        ''' </summary>
        Public Property CreatedAt As DateTime = DateTime.Now

        ''' <summary>
        ''' Profilin son güncellenme tarihi
        ''' </summary>
        Public Property UpdatedAt As DateTime = DateTime.Now

        ''' <summary>
        ''' VIN'in WMI (World Manufacturer Identifier) kısmı - ilk 3 karakter
        ''' </summary>
        Public ReadOnly Property WMI As String
            Get
                If String.IsNullOrEmpty(VIN) OrElse VIN.Length < 3 Then Return ""
                Return VIN.Substring(0, 3)
            End Get
        End Property

        ''' <summary>
        ''' VIN'in VDS (Vehicle Descriptor Section) kısmı - 4-9 karakterler
        ''' </summary>
        Public ReadOnly Property VDS As String
            Get
                If String.IsNullOrEmpty(VIN) OrElse VIN.Length < 9 Then Return ""
                Return VIN.Substring(3, 6)
            End Get
        End Property

        ''' <summary>
        ''' VIN'in VIS (Vehicle Identifier Section) kısmı - 10-17 karakterler
        ''' </summary>
        Public ReadOnly Property VIS As String
            Get
                If String.IsNullOrEmpty(VIN) OrElse VIN.Length < 17 Then Return ""
                Return VIN.Substring(9, 8)
            End Get
        End Property

        ''' <summary>
        ''' Profili okunabilir string olarak döndürür
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"{Brand} {Model} ({Year}) - VIN: {VIN}"
        End Function
    End Class

#End Region

#Region "VehicleProfileManager"

    ''' <summary>
    ''' VIN bazlı araç profil yönetim servisi
    ''' </summary>
    Public Class VehicleProfileManager

#Region "Olaylar (Events)"

        ''' <summary>
        ''' Profil başarıyla yüklendiğinde tetiklenir
        ''' </summary>
        Public Event OnProfileLoaded(profile As VehicleProfile)

        ''' <summary>
        ''' Yeni profil oluşturulduğunda tetiklenir
        ''' </summary>
        Public Event OnProfileCreated(profile As VehicleProfile)

        ''' <summary>
        ''' Profil güncellendiğinde tetiklenir
        ''' </summary>
        Public Event OnProfileUpdated(profile As VehicleProfile)

        ''' <summary>
        ''' VIN tanınmadığında tetiklenir
        ''' </summary>
        Public Event OnUnknownVin(vin As String)

        ''' <summary>
        ''' Otomatik profil yükleme devre dışı olduğunda tetiklenir
        ''' </summary>
        Public Event OnAutoLoadDisabled()

        ''' <summary>
        ''' Veritabanı yüklendiğinde tetiklenir
        ''' </summary>
        Public Event OnDatabaseLoaded(profileCount As Integer)

        ''' <summary>
        ''' Veritabanı kaydedildiğinde tetiklenir
        ''' </summary>
        Public Event OnDatabaseSaved()

        ''' <summary>
        ''' Hata oluştuğunda tetiklenir
        ''' </summary>
        Public Event OnError(message As String)

        ''' <summary>
        ''' Tüm profiller yüklendiğinde tetiklenir (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnProfilesLoaded()

        ''' <summary>
        ''' Profil kaydedildiğinde tetiklenir (MainForm uyumluluğu)
        ''' </summary>
        Public Event OnProfileSaved(vin As String)

#End Region

#Region "Sabitler"

        ' Data klasörü adı
        Private Const DATA_FOLDER As String = "data"

        Private Const DEFAULT_PROFILES_FILE As String = "vehicleProfiles.json"
        Private Const VIN_LENGTH As Integer = 17

        ' Boş profil listesi JSON şablonu
        Private Const EMPTY_PROFILES_TEMPLATE As String = "[]"

#End Region

#Region "Özel Alanlar"

        ' Profil veritabanı: VIN → VehicleProfile
        Private _profiles As New Dictionary(Of String, VehicleProfile)

        ' Profil dosya yolu (absolute path)
        Private _profilesFilePath As String

        ' Otomatik yükleme etkin mi?
        Private _autoLoadEnabled As Boolean = True

        ' Mevcut yüklü profil
        Private _currentProfile As VehicleProfile = Nothing

        ' Thread-safety lock
        Private ReadOnly _lock As New Object()

#End Region

#Region "Unified Path Management"

        ''' <summary>
        ''' Application.StartupPath + /data/ + filename şeklinde unified path oluşturur
        ''' </summary>
        Private Shared Function GetUnifiedDataPath(fileName As String) As String
            Dim basePath As String = Application.StartupPath
            Dim dataPath As String = Path.Combine(basePath, DATA_FOLDER)
            Return Path.Combine(dataPath, fileName)
        End Function

        ''' <summary>
        ''' Data klasörünün var olmasını sağlar
        ''' </summary>
        Private Sub EnsureDataFolderExists()
            Try
                Dim dataFolder As String = Path.GetDirectoryName(_profilesFilePath)
                If Not Directory.Exists(dataFolder) Then
                    Directory.CreateDirectory(dataFolder)
                    Debug.WriteLine($"Data klasörü oluşturuldu: {dataFolder}")
                End If
            Catch ex As Exception
                RaiseEvent OnError("Data klasörü oluşturulamadı: " & ex.Message)
            End Try
        End Sub

#End Region

#Region "Özellikler (Properties)"

        ''' <summary>
        ''' Otomatik profil yükleme etkin mi?
        ''' </summary>
        Public Property AutoLoadEnabled As Boolean
            Get
                Return _autoLoadEnabled
            End Get
            Set(value As Boolean)
                _autoLoadEnabled = value
                If Not value Then
                    RaiseEvent OnAutoLoadDisabled()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Mevcut yüklü profil
        ''' </summary>
        Public ReadOnly Property CurrentProfile As VehicleProfile
            Get
                Return _currentProfile
            End Get
        End Property

        ''' <summary>
        ''' Veritabanındaki profil sayısı
        ''' </summary>
        Public ReadOnly Property ProfileCount As Integer
            Get
                SyncLock _lock
                    Return _profiles.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Profil dosya yolu (absolute path)
        ''' </summary>
        Public ReadOnly Property ProfilesFilePath As String
            Get
                Return _profilesFilePath
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Varsayılan profil dosyası ile oluşturur
        ''' Unified /data/ folder kullanır
        ''' </summary>
        Public Sub New()
            _profilesFilePath = GetUnifiedDataPath(DEFAULT_PROFILES_FILE)
            EnsureDataFolderExists()
        End Sub

        ''' <summary>
        ''' Belirtilen profil dosyası ile oluşturur
        ''' </summary>
        Public Sub New(profilesFilePath As String)
            If Path.IsPathRooted(profilesFilePath) Then
                _profilesFilePath = profilesFilePath
            Else
                _profilesFilePath = GetUnifiedDataPath(profilesFilePath)
            End If
            EnsureDataFolderExists()
        End Sub

#End Region

#Region "Dosya İşlemleri"

        ''' <summary>
        ''' Profil veritabanını dosyadan yükler
        ''' </summary>
        Public Sub LoadProfiles()
            SyncLock _lock
                Try
                    _profiles.Clear()

                    If Not File.Exists(_profilesFilePath) Then
                        ' Dosya yoksa boş veritabanı oluştur
                        EnsureDataFolderExists()
                        AtomicWrite(_profilesFilePath, EMPTY_PROFILES_TEMPLATE)
                        RaiseEvent OnDatabaseLoaded(0)
                        RaiseEvent OnProfilesLoaded()
                        Return
                    End If

                    Dim jsonText As String = File.ReadAllText(_profilesFilePath)
                    If String.IsNullOrWhiteSpace(jsonText) Then
                        RaiseEvent OnDatabaseLoaded(0)
                        RaiseEvent OnProfilesLoaded()
                        Return
                    End If

                    Dim profileList = JsonConvert.DeserializeObject(Of List(Of VehicleProfile))(jsonText)
                    If profileList IsNot Nothing Then
                        For Each profile In profileList
                            If Not String.IsNullOrEmpty(profile.VIN) Then
                                _profiles(profile.VIN.ToUpper()) = profile
                            End If
                        Next
                    End If

                    Debug.WriteLine($"VehicleProfileManager loaded: {_profilesFilePath} ({_profiles.Count} profiles)")

                    RaiseEvent OnDatabaseLoaded(_profiles.Count)
                    RaiseEvent OnProfilesLoaded()

                Catch ex As JsonReaderException
                    ' JSON bozuk - backup'tan restore dene
                    If TryRestoreFromBackup() Then
                        RaiseEvent OnError("Profil JSON bozuktu, backup'tan geri yüklendi")
                    Else
                        _profiles.Clear()
                        AtomicWrite(_profilesFilePath, EMPTY_PROFILES_TEMPLATE)
                        RaiseEvent OnError("Profil JSON bozuk, sıfırlandı: " & ex.Message)
                    End If
                    RaiseEvent OnDatabaseLoaded(_profiles.Count)
                    RaiseEvent OnProfilesLoaded()

                Catch ex As Exception
                    RaiseEvent OnError("Profil veritabanı yükleme hatası: " & ex.Message)
                    _profiles.Clear()
                End Try
            End SyncLock
        End Sub

        ''' <summary>
        ''' Backup dosyasından geri yüklemeyi dener
        ''' </summary>
        Private Function TryRestoreFromBackup() As Boolean
            Try
                Dim backupPath As String = _profilesFilePath & ".bak"
                If Not File.Exists(backupPath) Then Return False

                Dim backupText As String = File.ReadAllText(backupPath)
                If String.IsNullOrWhiteSpace(backupText) Then Return False

                ' Backup'ı validate et
                Dim testParse = JsonConvert.DeserializeObject(Of List(Of VehicleProfile))(backupText)
                If testParse Is Nothing Then Return False

                ' Backup geçerli - dictionary'e yükle
                _profiles.Clear()
                For Each profile In testParse
                    If Not String.IsNullOrEmpty(profile.VIN) Then
                        _profiles(profile.VIN.ToUpper()) = profile
                    End If
                Next

                ' Ana dosyayı da düzelt
                AtomicWrite(_profilesFilePath, backupText)

                Debug.WriteLine("VehicleProfiles backup'tan restore edildi")
                Return True

            Catch ex As Exception
                Debug.WriteLine($"VehicleProfiles backup restore hatası: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Profil veritabanını ATOMIC olarak dosyaya kaydeder
        ''' </summary>
        Public Sub SaveProfiles()
            SyncLock _lock
                Try
                    Dim profileList = _profiles.Values.ToList()
                    Dim jsonText As String = JsonConvert.SerializeObject(profileList, Formatting.Indented)

                    ' Validasyon
                    If Not ValidateJsonBeforeSave(jsonText) Then
                        RaiseEvent OnError("Profil JSON validasyon hatası - kayıt iptal edildi")
                        Return
                    End If

                    ' ATOMIC WRITE
                    If AtomicWrite(_profilesFilePath, jsonText) Then
                        Debug.WriteLine($"VehicleProfileManager saved: {_profilesFilePath}")
                        RaiseEvent OnDatabaseSaved()
                    Else
                        RaiseEvent OnError("Atomic write başarısız - mevcut dosya korundu")
                    End If

                Catch ex As Exception
                    RaiseEvent OnError("Profil veritabanı kaydetme hatası: " & ex.Message)
                End Try
            End SyncLock
        End Sub

        ''' <summary>
        ''' Kaydetmeden önce JSON'u validate eder
        ''' </summary>
        Private Function ValidateJsonBeforeSave(jsonText As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(jsonText) Then
                    Return False
                End If

                ' Parse edilebildiğini kontrol et
                Dim testParse As JToken = JToken.Parse(jsonText)
                If testParse Is Nothing Then
                    Return False
                End If

                ' Array olduğunu kontrol et (profil listesi)
                If testParse.Type <> JTokenType.Array Then
                    Return False
                End If

                Return True

            Catch ex As Exception
                Debug.WriteLine($"ValidateJsonBeforeSave error: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' ATOMIC WRITE: Temp dosyaya yaz, validate et, replace et
        ''' </summary>
        Private Function AtomicWrite(targetPath As String, content As String) As Boolean
            Dim tempPath As String = targetPath & ".tmp"
            Dim backupPath As String = targetPath & ".bak"

            Try
                ' Data klasörünün var olduğundan emin ol
                EnsureDataFolderExists()

                ' 1. Temp dosyaya yaz
                File.WriteAllText(tempPath, content)

                ' 2. Temp dosyayı validate et
                Try
                    Dim validation As JToken = JToken.Parse(content)
                    If validation Is Nothing Then
                        Throw New Exception("Validation parse returned null")
                    End If
                Catch validEx As Exception
                    Debug.WriteLine($"AtomicWrite validation failed: {validEx.Message}")
                    SafeDeleteFile(tempPath)
                    Return False
                End Try

                ' 3. Mevcut dosya varsa backup al ve replace et
                If File.Exists(targetPath) Then
                    File.Replace(tempPath, targetPath, backupPath, True)
                Else
                    File.Move(tempPath, targetPath)
                End If

                Return True

            Catch ex As Exception
                Debug.WriteLine($"AtomicWrite error: {ex.Message}")
                SafeDeleteFile(tempPath)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Dosyayı güvenli şekilde siler
        ''' </summary>
        Private Shared Sub SafeDeleteFile(path As String)
            Try
                If File.Exists(path) Then
                    File.Delete(path)
                End If
            Catch
                ' Ignore deletion errors
            End Try
        End Sub

        ''' <summary>
        ''' Veritabanını yeniden yükler
        ''' </summary>
        Public Sub Reload()
            LoadProfiles()
        End Sub

#End Region

#Region "Profil Yükleme/Arama"

        ''' <summary>
        ''' VIN ile profil yüklemeyi dener
        ''' </summary>
        ''' <param name="vin">VIN numarası</param>
        ''' <returns>Profil bulundu ve yüklendi ise True</returns>
        Public Function TryLoadProfileByVin(vin As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(vin) Then
                    RaiseEvent OnError("VIN boş olamaz")
                    Return False
                End If

                vin = NormalizeVin(vin)

                ' Otomatik yükleme kapalıysa
                If Not _autoLoadEnabled Then
                    RaiseEvent OnAutoLoadDisabled()
                    Return False
                End If

                SyncLock _lock
                    If _profiles.ContainsKey(vin) Then
                        _currentProfile = _profiles(vin)
                        RaiseEvent OnProfileLoaded(_currentProfile)
                        Return True
                    Else
                        ' Kısmi eşleşme dene (WMI + VDS bazlı)
                        Dim partialMatch = FindPartialMatch(vin)
                        If partialMatch IsNot Nothing Then
                            _currentProfile = partialMatch
                            RaiseEvent OnProfileLoaded(_currentProfile)
                            Return True
                        End If

                        RaiseEvent OnUnknownVin(vin)
                        Return False
                    End If
                End SyncLock

            Catch ex As Exception
                RaiseEvent OnError("Profil yükleme hatası: " & ex.Message)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' VIN'e göre profil arar (yüklemez)
        ''' </summary>
        Public Function FindProfileByVin(vin As String) As VehicleProfile
            Try
                If String.IsNullOrWhiteSpace(vin) Then Return Nothing

                vin = NormalizeVin(vin)

                SyncLock _lock
                    If _profiles.ContainsKey(vin) Then
                        Return _profiles(vin)
                    End If
                End SyncLock

                Return Nothing

            Catch ex As Exception
                Debug.WriteLine($"FindExactMatch error: {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Kısmi VIN eşleşmesi arar (WMI + VDS bazlı)
        ''' </summary>
        Private Function FindPartialMatch(vin As String) As VehicleProfile
            Try
                If vin.Length < 9 Then Return Nothing

                Dim targetWmiVds As String = vin.Substring(0, 9)

                For Each profile In _profiles.Values
                    If Not String.IsNullOrEmpty(profile.VIN) AndAlso profile.VIN.Length >= 9 Then
                        Dim profileWmiVds As String = profile.VIN.Substring(0, 9)
                        If targetWmiVds = profileWmiVds Then
                            Return profile
                        End If
                    End If
                Next

                Return Nothing

            Catch ex As Exception
                Debug.WriteLine($"FindPartialMatch error: {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Markaya göre profilleri arar
        ''' </summary>
        Public Function FindProfilesByBrand(brand As String) As List(Of VehicleProfile)
            Dim results As New List(Of VehicleProfile)

            Try
                If String.IsNullOrWhiteSpace(brand) Then Return results

                brand = brand.ToLower()

                SyncLock _lock
                    For Each profile In _profiles.Values
                        If profile.Brand.ToLower().Contains(brand) Then
                            results.Add(profile)
                        End If
                    Next
                End SyncLock

            Catch ex As Exception
                Debug.WriteLine($"FindProfilesByBrand error: {ex.Message}")
                results.Clear()
            End Try

            Return results
        End Function

        ''' <summary>
        ''' Tüm profilleri döndürür
        ''' </summary>
        Public Function GetAllProfiles() As List(Of VehicleProfile)
            SyncLock _lock
                Return _profiles.Values.ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Profil sayısını döndürür (MainForm uyumluluğu)
        ''' </summary>
        Public Function GetProfileCount() As Integer
            Return ProfileCount
        End Function

        ''' <summary>
        ''' VIN ile profil döndürür (MainForm uyumluluğu)
        ''' </summary>
        Public Function GetProfileByVIN(vin As String) As VehicleProfile
            Return FindProfileByVin(vin)
        End Function

#End Region

#Region "Profil CRUD İşlemleri"

        ''' <summary>
        ''' Yeni profil ekler veya mevcudu günceller
        ''' </summary>
        Public Sub AddOrUpdateProfile(profile As VehicleProfile)
            Try
                If profile Is Nothing Then
                    RaiseEvent OnError("Profil null olamaz")
                    Return
                End If

                If String.IsNullOrWhiteSpace(profile.VIN) Then
                    RaiseEvent OnError("VIN boş olamaz")
                    Return
                End If

                profile.VIN = NormalizeVin(profile.VIN)
                profile.UpdatedAt = DateTime.Now

                SyncLock _lock
                    Dim isNew As Boolean = Not _profiles.ContainsKey(profile.VIN)
                    
                    If isNew Then
                        profile.CreatedAt = DateTime.Now
                    End If

                    _profiles(profile.VIN) = profile

                    If isNew Then
                        RaiseEvent OnProfileCreated(profile)
                    Else
                        RaiseEvent OnProfileUpdated(profile)
                    End If
                End SyncLock

                ' Otomatik kaydet
                SaveProfiles()
                RaiseEvent OnProfileSaved(profile.VIN)

            Catch ex As Exception
                RaiseEvent OnError("Profil kaydetme hatası: " & ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' VIN ile profil siler
        ''' </summary>
        Public Function DeleteProfile(vin As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(vin) Then Return False

                vin = NormalizeVin(vin)

                SyncLock _lock
                    If _profiles.ContainsKey(vin) Then
                        _profiles.Remove(vin)
                        SaveProfiles()
                        Return True
                    End If
                End SyncLock

                Return False

            Catch ex As Exception
                RaiseEvent OnError("Profil silme hatası: " & ex.Message)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Profil var mı kontrol eder
        ''' </summary>
        Public Function ProfileExists(vin As String) As Boolean
            If String.IsNullOrWhiteSpace(vin) Then Return False
            vin = NormalizeVin(vin)

            SyncLock _lock
                Return _profiles.ContainsKey(vin)
            End SyncLock
        End Function

#End Region

#Region "Profil Oluşturma Yardımcıları"

        ''' <summary>
        ''' OBD verilerinden profil oluşturur
        ''' </summary>
        Public Function CreateProfileFromOBD(vin As String, 
                                              brand As String, 
                                              model As String,
                                              Optional year As Integer = 0) As VehicleProfile
            Dim profile As New VehicleProfile()
            profile.VIN = NormalizeVin(vin)
            profile.Brand = brand
            profile.Model = model
            profile.Year = If(year > 0, year, ExtractYearFromVin(vin))
            profile.Source = "obd_scan"
            profile.Confidence = 90
            profile.Notes = "OBD-II taraması ile otomatik oluşturuldu"

            Return profile
        End Function

        ''' <summary>
        ''' VIN'den yıl bilgisini çıkarır (10. karakter = model yılı kodu)
        ''' </summary>
        Public Function ExtractYearFromVin(vin As String) As Integer
            Try
                If String.IsNullOrEmpty(vin) OrElse vin.Length < 10 Then Return 0

                Dim yearChar As Char = vin(9)
                Return DecodeVinYearCharacter(yearChar)

            Catch ex As Exception
                Debug.WriteLine($"ExtractYearFromVin error: {ex.Message}")
                Return 0
            End Try
        End Function

        ''' <summary>
        ''' VIN yıl karakterini yıl değerine çevirir
        ''' </summary>
        Private Function DecodeVinYearCharacter(yearChar As Char) As Integer
            ' VIN 10. karakter model yılı kodlaması
            ' A=2010, B=2011, ..., J=2018 (I, O, Q, U, Z hariç)
            ' 1=2001, 2=2002, ..., 9=2009
            ' 1980-1999 ve 2001-2009 için aynı karakterler kullanılır

            Dim yearMap As New Dictionary(Of Char, Integer)() From {
                {"A"c, 2010}, {"B"c, 2011}, {"C"c, 2012}, {"D"c, 2013}, {"E"c, 2014},
                {"F"c, 2015}, {"G"c, 2016}, {"H"c, 2017}, {"J"c, 2018}, {"K"c, 2019},
                {"L"c, 2020}, {"M"c, 2021}, {"N"c, 2022}, {"P"c, 2023}, {"R"c, 2024},
                {"S"c, 2025}, {"T"c, 2026}, {"V"c, 2027}, {"W"c, 2028}, {"X"c, 2029},
                {"Y"c, 2030},
                {"1"c, 2031}, {"2"c, 2032}, {"3"c, 2033}, {"4"c, 2034}, {"5"c, 2035},
                {"6"c, 2036}, {"7"c, 2037}, {"8"c, 2038}, {"9"c, 2039}
            }

            yearChar = Char.ToUpper(yearChar)

            If yearMap.ContainsKey(yearChar) Then
                ' 2010 sonrası araçlar için
                Return yearMap(yearChar)
            End If

            Return 0
        End Function

        ''' <summary>
        ''' VIN'den marka tahmin eder (WMI bazlı)
        ''' </summary>
        Public Function GuessBrandFromVin(vin As String) As String
            Try
                If String.IsNullOrEmpty(vin) OrElse vin.Length < 3 Then Return ""

                Dim wmi As String = vin.Substring(0, 3).ToUpper()

                ' Yaygın WMI kodları
                Dim wmiMap As New Dictionary(Of String, String)() From {
                    {"WVW", "Volkswagen"}, {"WVG", "Volkswagen"},
                    {"WAU", "Audi"}, {"WUA", "Audi"},
                    {"WBA", "BMW"}, {"WBS", "BMW M"},
                    {"WDB", "Mercedes-Benz"}, {"WDC", "Mercedes-Benz"},
                    {"WP0", "Porsche"}, {"WP1", "Porsche"},
                    {"TRU", "Audi"}, {"ZFF", "Ferrari"},
                    {"SAL", "Land Rover"}, {"SAJ", "Jaguar"},
                    {"YV1", "Volvo"}, {"YV4", "Volvo"},
                    {"VF1", "Renault"}, {"VF3", "Peugeot"}, {"VF7", "Citroen"},
                    {"SFA", "Ford UK"}, {"WF0", "Ford"},
                    {"ZAR", "Alfa Romeo"}, {"ZAP", "Fiat"}, {"ZFA", "Fiat"},
                    {"SCC", "Lotus"}, {"SCF", "Aston Martin"},
                    {"JHM", "Honda"}, {"JN1", "Nissan"}, {"JT", "Toyota"},
                    {"JMZ", "Mazda"}, {"JS", "Suzuki"}, {"JF", "Subaru"},
                    {"KMH", "Hyundai"}, {"KNA", "Kia"}, {"KL", "GM Korea"},
                    {"1G", "General Motors"}, {"1GC", "Chevrolet"}, {"1GT", "GMC"},
                    {"1FA", "Ford"}, {"1FT", "Ford Truck"},
                    {"2G", "General Motors"}, {"3G", "General Motors Mexico"}
                }

                ' Tam 3 karakter eşleşme
                If wmiMap.ContainsKey(wmi) Then
                    Return wmiMap(wmi)
                End If

                ' İlk 2 karakter eşleşme
                Dim wmi2 As String = wmi.Substring(0, 2)
                For Each kvp In wmiMap
                    If kvp.Key.StartsWith(wmi2) Then
                        Return kvp.Value
                    End If
                Next

                Return ""

            Catch ex As Exception
                Debug.WriteLine($"GuessBrandFromVin error: {ex.Message}")
                Return ""
            End Try
        End Function

#End Region

#Region "Yardımcı Metodlar"

        ''' <summary>
        ''' VIN'i normalize eder (büyük harf, temizlik)
        ''' </summary>
        Private Function NormalizeVin(vin As String) As String
            If String.IsNullOrWhiteSpace(vin) Then Return ""

            ' Boşlukları ve tire/tire karakterlerini kaldır
            vin = vin.Replace(" ", "").Replace("-", "").Replace("_", "")

            ' Büyük harfe çevir
            vin = vin.ToUpper()

            Return vin
        End Function

        ''' <summary>
        ''' VIN geçerli mi kontrol eder
        ''' </summary>
        Public Function IsValidVin(vin As String) As Boolean
            If String.IsNullOrWhiteSpace(vin) Then Return False

            vin = NormalizeVin(vin)

            ' 17 karakter olmalı
            If vin.Length <> VIN_LENGTH Then Return False

            ' I, O, Q harfleri kullanılmaz
            If vin.Contains("I") OrElse vin.Contains("O") OrElse vin.Contains("Q") Then
                Return False
            End If

            ' Sadece alfanumerik karakterler
            For Each c As Char In vin
                If Not Char.IsLetterOrDigit(c) Then Return False
            Next

            Return True
        End Function

        ''' <summary>
        ''' Mevcut profili temizler
        ''' </summary>
        Public Sub ClearCurrentProfile()
            _currentProfile = Nothing
        End Sub

        ''' <summary>
        ''' Durum raporu döndürür
        ''' </summary>
        Public Function GetStatusReport() As String
            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine("=== VehicleProfileManager Durumu ===")
            sb.AppendLine($"Veritabanı dosyası: {_profilesFilePath}")
            sb.AppendLine($"Profil sayısı: {ProfileCount}")
            sb.AppendLine($"Otomatik yükleme: {If(_autoLoadEnabled, "Açık", "Kapalı")}")

            If _currentProfile IsNot Nothing Then
                sb.AppendLine()
                sb.AppendLine("Yüklü Profil:")
                sb.AppendLine($"  VIN: {_currentProfile.VIN}")
                sb.AppendLine($"  Araç: {_currentProfile.Brand} {_currentProfile.Model} ({_currentProfile.Year})")
            End If

            Return sb.ToString()
        End Function

#End Region

    End Class

#End Region

End Namespace

