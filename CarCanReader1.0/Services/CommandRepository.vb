' CommandRepository.vb
' JSON komut veritabanı yönetim servisi
' Unified /data/ folder for all configurations
'
' SAFE JSON HANDLING:
'   - Centralized path via Application.StartupPath + /data/
'   - Atomic writes with temp file + validation
'   - Automatic backup before overwrite
'   - Never corrupts existing data on invalid save
'
' JSON Yapısı (commands.json):
'   {
'     "Brand1": {
'       "Model1": {
'         "Module1": {
'           "Command1": "t1238AABBCCDD",
'           "Command2": { "type": "toggle", "on": "...", "off": "..." }
'         }
'       }
'     }
'   }
'
' Kullanım:
'   Dim repo As New CommandRepository()
'   repo.Load()
'   Dim brands = repo.GetBrands()
'   repo.AddCommand("VW", "Golf", "BCM", "UnlockDoors", "t1234FF")
'   repo.Save()

Imports Newtonsoft.Json.Linq
Imports System.IO
Imports System.Windows.Forms
Imports Services.Interfaces
Imports Services.Providers

Namespace Services

Public Class CommandRepository

#Region "Olaylar (Events)"

    ''' <summary>
    ''' JSON verisi başarıyla yüklendiğinde tetiklenir
    ''' </summary>
    Public Event OnDataLoaded()

    ''' <summary>
    ''' JSON verisi başarıyla kaydedildiğinde tetiklenir
    ''' </summary>
    Public Event OnDataSaved()

    ''' <summary>
    ''' Hata oluştuğunda tetiklenir
    ''' </summary>
    Public Event OnError(message As String)

    ''' <summary>
    ''' Komut eklendiğinde tetiklenir
    ''' </summary>
    Public Event OnCommandAdded(brand As String, model As String, moduleName As String, commandName As String)

    ''' <summary>
    ''' Komut güncellendiğinde tetiklenir
    ''' </summary>
    Public Event OnCommandUpdated(brand As String, model As String, moduleName As String, commandName As String)

    ''' <summary>
    ''' Komut silindiğinde tetiklenir
    ''' </summary>
    Public Event OnCommandDeleted(brand As String, model As String, moduleName As String, commandName As String)

    ''' <summary>
    ''' Veri değiştiğinde tetiklenir (genel)
    ''' </summary>
    Public Event OnDataChanged()

#End Region

#Region "Özel Alanlar"

    ' JSON veri nesnesi
    Private _commandsData As JObject

    ' JSON dosya yolu (absolute path)
    Private _jsonFilePath As String

    ' JSON sağlayıcısı (dosya, bellek vb.)
    Private _provider As ICommandsProvider

    ' Veri değişti mi flag
    Private _hasUnsavedChanges As Boolean = False

    ' Thread-safety lock
    Private ReadOnly _lock As New Object()

#End Region

#Region "Sabitler"

    ' Data klasörü adı
    Private Const DATA_FOLDER As String = "data"

    ' Varsayılan JSON dosya adı
    Private Const DEFAULT_JSON_FILE As String = "commands.json"

    ' Boş JSON şablonu
    Private Const EMPTY_JSON_TEMPLATE As String = "{ }"

    ' Minimum valid JSON size (en az bir brand olmalı)
    Private Const MIN_VALID_JSON_SIZE As Integer = 3

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Varsayılan JSON dosyası ile CommandRepository oluşturur
    ''' Unified /data/ folder kullanır
    ''' </summary>
    Public Sub New()
        Me.New(New FileCommandsProvider(GetUnifiedDataPath(DEFAULT_JSON_FILE)))
    End Sub

    ''' <summary>
    ''' Belirtilen JSON dosyası ile CommandRepository oluşturur
    ''' </summary>
    ''' <param name="jsonFilePath">JSON dosya yolu (relative ise /data/ altına yerleştirilir)</param>
    Public Sub New(jsonFilePath As String)
        Dim resolvedPath As String
        If Path.IsPathRooted(jsonFilePath) Then
            resolvedPath = jsonFilePath
        Else
            resolvedPath = GetUnifiedDataPath(jsonFilePath)
        End If
        Me.New(New FileCommandsProvider(resolvedPath))
    End Sub

    ''' <summary>
    ''' Sağlayıcı tabanlı CommandRepository oluşturur
    ''' </summary>
    Public Sub New(provider As ICommandsProvider)
        If provider Is Nothing Then Throw New ArgumentNullException(NameOf(provider))
        _provider = provider
        _jsonFilePath = provider.GetLocation()
        _commandsData = New JObject()
        EnsureDataFolderExists()
    End Sub

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
            If _provider IsNot Nothing AndAlso Not TypeOf _provider Is FileCommandsProvider Then
                Return
            End If

            If String.IsNullOrWhiteSpace(_jsonFilePath) Then
                Return
            End If

            Dim dataFolder As String = Path.GetDirectoryName(_jsonFilePath)
            If Not String.IsNullOrWhiteSpace(dataFolder) AndAlso Not Directory.Exists(dataFolder) Then
                Directory.CreateDirectory(dataFolder)
                Debug.WriteLine($"Data klasörü oluşturuldu: {dataFolder}")
            End If
        Catch ex As Exception
            RaiseEvent OnError("Data klasörü oluşturulamadı: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Data klasörü yolunu döndürür
    ''' </summary>
    Public Shared Function GetDataFolderPath() As String
        Return Path.Combine(Application.StartupPath, DATA_FOLDER)
    End Function

#End Region

#Region "Özellikler (Properties)"

    ''' <summary>
    ''' Kaydedilmemiş değişiklik var mı
    ''' </summary>
    Public ReadOnly Property HasUnsavedChanges As Boolean
        Get
            Return _hasUnsavedChanges
        End Get
    End Property

    ''' <summary>
    ''' JSON dosya yolu (absolute path)
    ''' </summary>
    Public Property JsonFilePath As String
        Get
            Return _jsonFilePath
        End Get
        Set(value As String)
            If Path.IsPathRooted(value) Then
                _jsonFilePath = value
            Else
                _jsonFilePath = GetUnifiedDataPath(value)
            End If

            If _provider Is Nothing OrElse TypeOf _provider Is FileCommandsProvider Then
                _provider = New FileCommandsProvider(_jsonFilePath)
            End If
        End Set
    End Property

    ''' <summary>
    ''' Veri yüklü mü
    ''' </summary>
    Public ReadOnly Property IsLoaded As Boolean
        Get
            Return _commandsData IsNot Nothing AndAlso _commandsData.HasValues
        End Get
    End Property

    ''' <summary>
    ''' Ham JObject verisini döndürür (readonly erişim için)
    ''' </summary>
    Public ReadOnly Property RawData As JObject
        Get
            Return _commandsData
        End Get
    End Property

#End Region

#Region "Dosya İşlemleri"

    ''' <summary>
    ''' JSON dosyasının var olmasını sağlar, yoksa boş valid JSON oluşturur
    ''' </summary>
    Private Sub EnsureJsonExists()
        SyncLock _lock
            Try
                If _provider IsNot Nothing Then
                    EnsureProviderDataExists()
                Else
                    EnsureDataFolderExists()

                    If Not File.Exists(_jsonFilePath) Then
                        ' Boş ama valid JSON oluştur
                        AtomicWrite(_jsonFilePath, EMPTY_JSON_TEMPLATE)
                        Debug.WriteLine($"JSON dosyası oluşturuldu: {_jsonFilePath}")
                    End If
                End If
            Catch ex As Exception
                RaiseEvent OnError("JSON dosyası oluşturulamadı: " & ex.Message)
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Sağlayıcı tabanlı depoyu boş bir JSON ile hazırlar
    ''' </summary>
    Private Sub EnsureProviderDataExists()
        If _provider Is Nothing Then Return
        If _provider.Exists() Then Return

        Dim emptyObj As JObject = JObject.Parse(EMPTY_JSON_TEMPLATE)
        _provider.Save(emptyObj)
    End Sub

    ''' <summary>
    ''' JSON dosyasını yükler
    ''' </summary>
    Public Sub Load()
        SyncLock _lock
            Try
                ' Dosyanın var olmasını sağla
                EnsureJsonExists()

                If _provider IsNot Nothing Then
                    Dim loaded As JObject = _provider.Load()
                    If loaded Is Nothing Then
                        loaded = JObject.Parse(EMPTY_JSON_TEMPLATE)
                    End If

                    _commandsData = loaded
                    _hasUnsavedChanges = False
                    Debug.WriteLine($"CommandRepository loaded from provider: {_provider.GetLocation()}")
                    RaiseEvent OnDataLoaded()
                    Return
                End If

                ' JSON dosyasını oku
                Dim jsonText As String = File.ReadAllText(_jsonFilePath)

                ' Boş dosya kontrolü
                If String.IsNullOrWhiteSpace(jsonText) Then
                    jsonText = EMPTY_JSON_TEMPLATE
                End If

                ' JSON parse et
                _commandsData = JObject.Parse(jsonText)
                _hasUnsavedChanges = False

                Debug.WriteLine($"CommandRepository loaded: {_jsonFilePath} ({_commandsData.Properties().Count()} brands)")

                ' Olayı tetikle
                RaiseEvent OnDataLoaded()

            Catch ex As Newtonsoft.Json.JsonReaderException
                ' JSON bozuk - backup'tan restore dene
                If TryRestoreFromBackup() Then
                    RaiseEvent OnError("JSON bozuktu, backup'tan geri yüklendi")
                Else
                    _commandsData = New JObject()
                    AtomicWrite(_jsonFilePath, EMPTY_JSON_TEMPLATE)
                    RaiseEvent OnError("JSON bozuk, sıfırlandı: " & ex.Message)
                End If
                RaiseEvent OnDataLoaded()

            Catch ex As Exception
                _commandsData = New JObject()
                RaiseEvent OnError("JSON yükleme hatası: " & ex.Message)
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Backup dosyasından geri yüklemeyi dener
    ''' </summary>
    Private Function TryRestoreFromBackup() As Boolean
        Try
            Dim backupPath As String = _jsonFilePath & ".bak"
            If Not File.Exists(backupPath) Then Return False

            Dim backupText As String = File.ReadAllText(backupPath)
            If String.IsNullOrWhiteSpace(backupText) Then Return False

            ' Backup'ı validate et
            Dim testParse As JObject = JObject.Parse(backupText)
            If testParse Is Nothing Then Return False

            ' Backup geçerli - geri yükle
            _commandsData = testParse
            AtomicWrite(_jsonFilePath, backupText)
            _hasUnsavedChanges = False

            Debug.WriteLine("Backup'tan restore edildi")
            Return True

        Catch ex As Exception
            Debug.WriteLine($"Backup restore hatası: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' JSON dosyasını ATOMIC olarak kaydeder
    ''' Temp dosyaya yazar, validate eder, sonra replace eder
    ''' </summary>
    Public Sub Save()
        SyncLock _lock
            Try
                If _commandsData Is Nothing Then
                    RaiseEvent OnError("Kaydedilecek veri yok")
                    Return
                End If

                ' JSON'u formatlı olarak serialize et
                Dim jsonText As String = _commandsData.ToString(Newtonsoft.Json.Formatting.Indented)

                ' Validasyon: boş veya invalid JSON kaydetme
                If Not ValidateJsonBeforeSave(jsonText) Then
                    RaiseEvent OnError("JSON validasyon hatası - kayıt iptal edildi")
                    Return
                End If

                If _provider IsNot Nothing Then
                    _provider.Save(_commandsData)
                    _hasUnsavedChanges = False
                    Debug.WriteLine($"CommandRepository saved via provider: {_provider.GetLocation()}")
                    RaiseEvent OnDataSaved()
                Else
                    ' ATOMIC WRITE
                    If AtomicWrite(_jsonFilePath, jsonText) Then
                        _hasUnsavedChanges = False
                        Debug.WriteLine($"CommandRepository saved: {_jsonFilePath}")
                        RaiseEvent OnDataSaved()
                    Else
                        RaiseEvent OnError("Atomic write başarısız - mevcut dosya korundu")
                    End If
                End If

            Catch ex As Exception
                RaiseEvent OnError("JSON kaydetme hatası: " & ex.Message)
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Kaydetmeden önce JSON'u validate eder
    ''' </summary>
    Private Function ValidateJsonBeforeSave(jsonText As String) As Boolean
        Try
            ' Boş veya çok kısa JSON reddet
            If String.IsNullOrWhiteSpace(jsonText) Then
                Debug.WriteLine("ValidateJsonBeforeSave: Empty JSON rejected")
                Return False
            End If

            If jsonText.Length < MIN_VALID_JSON_SIZE Then
                Debug.WriteLine("ValidateJsonBeforeSave: JSON too short")
                Return False
            End If

            ' Parse edilebildiğini kontrol et
            Dim testParse As JToken = JToken.Parse(jsonText)
            If testParse Is Nothing Then
                Debug.WriteLine("ValidateJsonBeforeSave: Parse returned null")
                Return False
            End If

            ' JObject olduğunu kontrol et
            If testParse.Type <> JTokenType.Object Then
                Debug.WriteLine("ValidateJsonBeforeSave: Not a JObject")
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
    ''' Başarısız olursa mevcut dosya korunur
    ''' </summary>
    Private Function AtomicWrite(targetPath As String, content As String) As Boolean
        Dim tempPath As String = targetPath & ".tmp"
        Dim backupPath As String = targetPath & ".bak"

        Try
            ' 1. Temp dosyaya yaz
            File.WriteAllText(tempPath, content)

            ' 2. Temp dosyayı validate et (tekrar parse ederek)
            Try
                Dim validation As JToken = JToken.Parse(content)
                If validation Is Nothing Then
                    Throw New Exception("Validation parse returned null")
                End If
            Catch validEx As Exception
                ' Validation başarısız - temp dosyayı sil, mevcut dosyayı koru
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
            ' Hata durumunda temp dosyayı temizle
            SafeDeleteFile(tempPath)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Dosyayı güvenli şekilde siler (hata fırlatmaz)
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
    ''' Farklı bir dosyaya kaydeder
    ''' </summary>
    Public Sub SaveAs(filePath As String)
        SyncLock _lock
            Dim targetPath As String
            If Path.IsPathRooted(filePath) Then
                targetPath = filePath
            Else
                targetPath = GetUnifiedDataPath(filePath)
            End If

            Dim originalPath = _jsonFilePath
            Dim originalProvider = _provider

            If originalProvider Is Nothing OrElse TypeOf originalProvider Is FileCommandsProvider Then
                _jsonFilePath = targetPath
                _provider = New FileCommandsProvider(targetPath)
                Save()
                _jsonFilePath = originalPath
                _provider = originalProvider
            Else
                Dim tempProvider As New FileCommandsProvider(targetPath)
                tempProvider.Save(_commandsData)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Farklı bir dosyadan yükler
    ''' </summary>
    Public Sub LoadFrom(filePath As String)
        SyncLock _lock
            Dim sourcePath As String
            If Path.IsPathRooted(filePath) Then
                sourcePath = filePath
            Else
                sourcePath = GetUnifiedDataPath(filePath)
            End If

            If _provider Is Nothing OrElse TypeOf _provider Is FileCommandsProvider Then
                _jsonFilePath = sourcePath
                _provider = New FileCommandsProvider(sourcePath)
                Load()
            Else
                Dim tempProvider As New FileCommandsProvider(sourcePath)
                Dim loaded = tempProvider.Load()
                If loaded IsNot Nothing Then
                    _commandsData = loaded
                    _hasUnsavedChanges = False
                    RaiseEvent OnDataLoaded()
                End If
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Mevcut dosya yolunu yeniler (Reload)
    ''' </summary>
    Public Sub Reload()
        Load()
    End Sub

#End Region

#Region "Listeleme Metodları"

    ''' <summary>
    ''' Tüm markaları döndürür
    ''' </summary>
    Public Function GetBrands() As List(Of String)
        Dim brands As New List(Of String)()

        Try
            If _commandsData Is Nothing Then Return brands

            For Each prop As JProperty In _commandsData.Properties()
                brands.Add(prop.Name)
            Next

        Catch ex As Exception
            RaiseEvent OnError("Marka listesi alınamadı: " & ex.Message)
        End Try

        Return brands
    End Function

    ''' <summary>
    ''' Belirtilen markanın modellerini döndürür
    ''' </summary>
    Public Function GetModels(brand As String) As List(Of String)
        Dim models As New List(Of String)()

        Try
            If _commandsData Is Nothing Then Return models
            If String.IsNullOrWhiteSpace(brand) Then Return models

            Dim brandObj = TryCast(_commandsData(brand), JObject)
            If brandObj Is Nothing Then Return models

            For Each prop As JProperty In brandObj.Properties()
                models.Add(prop.Name)
            Next

        Catch ex As Exception
            RaiseEvent OnError("Model listesi alınamadı: " & ex.Message)
        End Try

        Return models
    End Function

    ''' <summary>
    ''' Belirtilen marka ve modelin modüllerini döndürür
    ''' </summary>
    Public Function GetModules(brand As String, model As String) As List(Of String)
        Dim modules As New List(Of String)()

        Try
            If _commandsData Is Nothing Then Return modules
            If String.IsNullOrWhiteSpace(brand) OrElse String.IsNullOrWhiteSpace(model) Then Return modules

            Dim modelObj = TryCast(_commandsData(brand)?(model), JObject)
            If modelObj Is Nothing Then Return modules

            For Each prop As JProperty In modelObj.Properties()
                modules.Add(prop.Name)
            Next

        Catch ex As Exception
            RaiseEvent OnError("Modül listesi alınamadı: " & ex.Message)
        End Try

        Return modules
    End Function

    ''' <summary>
    ''' Belirtilen marka, model ve modülün komutlarını döndürür
    ''' </summary>
    Public Function GetCommands(brand As String, model As String, moduleName As String) As List(Of String)
        Dim commands As New List(Of String)()

        Try
            If _commandsData Is Nothing Then Return commands
            If String.IsNullOrWhiteSpace(brand) OrElse
               String.IsNullOrWhiteSpace(model) OrElse
               String.IsNullOrWhiteSpace(moduleName) Then Return commands

            Dim moduleObj = TryCast(_commandsData(brand)?(model)?(moduleName), JObject)
            If moduleObj Is Nothing Then Return commands

            For Each prop As JProperty In moduleObj.Properties()
                commands.Add(prop.Name)
            Next

        Catch ex As Exception
            RaiseEvent OnError("Komut listesi alınamadı: " & ex.Message)
        End Try

        Return commands
    End Function

#End Region

#Region "Komut Okuma"

    ''' <summary>
    ''' Belirtilen komutu döndürür (string olarak)
    ''' </summary>
    Public Function GetCommand(brand As String, model As String, moduleName As String, commandName As String) As String
        Try
            If _commandsData Is Nothing Then Return Nothing

            Dim token = _commandsData(brand)?(model)?(moduleName)?(commandName)
            If token Is Nothing Then Return Nothing

            ' JValue ise string olarak döndür
            If TypeOf token Is JValue Then
                Return token.ToString()
            End If

            ' JObject ise JSON string olarak döndür
            If TypeOf token Is JObject Then
                Return token.ToString()
            End If

            Return token.ToString()

        Catch ex As Exception
            RaiseEvent OnError("Komut alınamadı: " & ex.Message)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Belirtilen komutu JToken olarak döndürür (toggle komutları için)
    ''' </summary>
    Public Function GetCommandToken(brand As String, model As String, moduleName As String, commandName As String) As JToken
        Try
            If _commandsData Is Nothing Then Return Nothing
            Return _commandsData(brand)?(model)?(moduleName)?(commandName)

        Catch ex As Exception
            RaiseEvent OnError("Komut token alınamadı: " & ex.Message)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Komutun var olup olmadığını kontrol eder
    ''' </summary>
    Public Function CommandExists(brand As String, model As String, moduleName As String, commandName As String) As Boolean
        Return GetCommandToken(brand, model, moduleName, commandName) IsNot Nothing
    End Function

#End Region

#Region "Komut CRUD İşlemleri"

    ''' <summary>
    ''' Yeni komut ekler
    ''' </summary>
    Public Sub AddCommand(brand As String, model As String, moduleName As String, commandName As String, frame As String)
        Try
            ' Parametre kontrolü
            If String.IsNullOrWhiteSpace(brand) OrElse
               String.IsNullOrWhiteSpace(model) OrElse
               String.IsNullOrWhiteSpace(moduleName) OrElse
               String.IsNullOrWhiteSpace(commandName) Then
                RaiseEvent OnError("Komut eklenemedi: Boş alan var")
                Return
            End If

            ' Hiyerarşiyi oluştur
            EnsurePath(brand, model, moduleName)

            ' Komutu ekle
            Dim moduleObj = CType(_commandsData(brand)(model)(moduleName), JObject)
            moduleObj(commandName) = frame

            _hasUnsavedChanges = True

            ' Olayları tetikle
            RaiseEvent OnCommandAdded(brand, model, moduleName, commandName)
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Komut ekleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Komut günceller
    ''' </summary>
    Public Sub UpdateCommand(brand As String, model As String, moduleName As String, oldName As String, newName As String, frame As String)
        Try
            ' Parametre kontrolü
            If String.IsNullOrWhiteSpace(brand) OrElse
               String.IsNullOrWhiteSpace(model) OrElse
               String.IsNullOrWhiteSpace(moduleName) OrElse
               String.IsNullOrWhiteSpace(oldName) OrElse
               String.IsNullOrWhiteSpace(newName) Then
                RaiseEvent OnError("Komut güncellenemedi: Boş alan var")
                Return
            End If

            Dim moduleObj = TryCast(_commandsData(brand)?(model)?(moduleName), JObject)
            If moduleObj Is Nothing Then
                RaiseEvent OnError("Komut güncellenemedi: Modül bulunamadı")
                Return
            End If

            ' İsim değiştiyse eski komutu sil
            If oldName <> newName Then
                moduleObj.Remove(oldName)
            End If

            ' Yeni değeri ayarla
            moduleObj(newName) = frame

            _hasUnsavedChanges = True

            ' Olayları tetikle
            RaiseEvent OnCommandUpdated(brand, model, moduleName, newName)
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Komut güncelleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Komut siler
    ''' </summary>
    Public Sub DeleteCommand(brand As String, model As String, moduleName As String, commandName As String)
        Try
            If String.IsNullOrWhiteSpace(commandName) Then
                RaiseEvent OnError("Komut silinemedi: Komut adı boş")
                Return
            End If

            Dim moduleObj = TryCast(_commandsData(brand)?(model)?(moduleName), JObject)
            If moduleObj Is Nothing Then
                RaiseEvent OnError("Komut silinemedi: Modül bulunamadı")
                Return
            End If

            moduleObj.Remove(commandName)

            _hasUnsavedChanges = True

            ' Olayları tetikle
            RaiseEvent OnCommandDeleted(brand, model, moduleName, commandName)
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Komut silme hatası: " & ex.Message)
        End Try
    End Sub

#End Region

#Region "Hiyerarşi Yönetimi"

    ''' <summary>
    ''' Brand/Model/Module yolunun var olmasını sağlar
    ''' </summary>
    Private Sub EnsurePath(brand As String, model As String, moduleName As String)
        ' Brand yoksa oluştur
        If _commandsData(brand) Is Nothing Then
            _commandsData(brand) = New JObject()
        End If

        ' Model yoksa oluştur
        Dim brandObj = CType(_commandsData(brand), JObject)
        If brandObj(model) Is Nothing Then
            brandObj(model) = New JObject()
        End If

        ' Module yoksa oluştur
        Dim modelObj = CType(brandObj(model), JObject)
        If modelObj(moduleName) Is Nothing Then
            modelObj(moduleName) = New JObject()
        End If
    End Sub

    ''' <summary>
    ''' Yeni marka ekler
    ''' </summary>
    Public Sub AddBrand(brandName As String)
        Try
            If String.IsNullOrWhiteSpace(brandName) Then
                RaiseEvent OnError("Marka adı boş olamaz")
                Return
            End If

            If _commandsData(brandName) IsNot Nothing Then
                RaiseEvent OnError("Bu marka zaten mevcut: " & brandName)
                Return
            End If

            _commandsData(brandName) = New JObject()
            _hasUnsavedChanges = True
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Marka ekleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Yeni model ekler
    ''' </summary>
    Public Sub AddModel(brand As String, modelName As String)
        Try
            If String.IsNullOrWhiteSpace(brand) OrElse String.IsNullOrWhiteSpace(modelName) Then
                RaiseEvent OnError("Marka veya model adı boş olamaz")
                Return
            End If

            EnsurePath(brand, modelName, "_temp")
            ' _temp modülünü sil (sadece yol oluşturmak için kullandık)
            Dim modelObj = CType(_commandsData(brand)(modelName), JObject)
            modelObj.Remove("_temp")

            _hasUnsavedChanges = True
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Model ekleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Yeni modül ekler
    ''' </summary>
    Public Sub AddModule(brand As String, model As String, moduleName As String)
        Try
            If String.IsNullOrWhiteSpace(brand) OrElse
               String.IsNullOrWhiteSpace(model) OrElse
               String.IsNullOrWhiteSpace(moduleName) Then
                RaiseEvent OnError("Boş alan var")
                Return
            End If

            EnsurePath(brand, model, moduleName)

            _hasUnsavedChanges = True
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Modül ekleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Markayı siler (tüm alt öğelerle birlikte)
    ''' </summary>
    Public Sub DeleteBrand(brandName As String)
        Try
            If _commandsData(brandName) Is Nothing Then Return

            _commandsData.Remove(brandName)
            _hasUnsavedChanges = True
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Marka silme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Modeli siler (tüm alt öğelerle birlikte)
    ''' </summary>
    Public Sub DeleteModel(brand As String, modelName As String)
        Try
            Dim brandObj = TryCast(_commandsData(brand), JObject)
            If brandObj Is Nothing Then Return

            brandObj.Remove(modelName)
            _hasUnsavedChanges = True
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Model silme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Modülü siler (tüm alt öğelerle birlikte)
    ''' </summary>
    Public Sub DeleteModule(brand As String, model As String, moduleName As String)
        Try
            Dim modelObj = TryCast(_commandsData(brand)?(model), JObject)
            If modelObj Is Nothing Then Return

            modelObj.Remove(moduleName)
            _hasUnsavedChanges = True
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Modül silme hatası: " & ex.Message)
        End Try
    End Sub

#End Region

#Region "Toggle Komut Desteği"

    ''' <summary>
    ''' Toggle tipinde komut ekler
    ''' </summary>
    Public Sub AddToggleCommand(brand As String, model As String, moduleName As String,
                                 commandName As String, onFrame As String, offFrame As String,
                                 Optional byteIndex As Integer = -1)
        Try
            EnsurePath(brand, model, moduleName)

            Dim toggleObj As New JObject()
            toggleObj("type") = "toggle"
            toggleObj("on") = onFrame
            toggleObj("off") = offFrame

            If byteIndex >= 0 Then
                toggleObj("byte") = byteIndex
            End If

            Dim moduleObj = CType(_commandsData(brand)(model)(moduleName), JObject)
            moduleObj(commandName) = toggleObj

            _hasUnsavedChanges = True
            RaiseEvent OnCommandAdded(brand, model, moduleName, commandName)
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("Toggle komut ekleme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Komutun toggle tipinde olup olmadığını kontrol eder
    ''' </summary>
    Public Function IsToggleCommand(brand As String, model As String, moduleName As String, commandName As String) As Boolean
        Try
            Dim token = GetCommandToken(brand, model, moduleName, commandName)
            If token Is Nothing Then Return False

            If TypeOf token Is JObject Then
                Dim obj = CType(token, JObject)
                Dim typeVal = obj("type")?.ToString()
                Return typeVal = "toggle"
            End If

            Return False

        Catch ex As Exception
            Debug.WriteLine($"IsToggleCommand error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Toggle komutun ON frame'ini döndürür
    ''' </summary>
    Public Function GetToggleOnFrame(brand As String, model As String, moduleName As String, commandName As String) As String
        Try
            Dim token = GetCommandToken(brand, model, moduleName, commandName)
            If token Is Nothing OrElse Not TypeOf token Is JObject Then Return Nothing

            Return CType(token, JObject)("on")?.ToString()

        Catch ex As Exception
            Debug.WriteLine($"GetToggleOnFrame error: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Toggle komutun OFF frame'ini döndürür
    ''' </summary>
    Public Function GetToggleOffFrame(brand As String, model As String, moduleName As String, commandName As String) As String
        Try
            Dim token = GetCommandToken(brand, model, moduleName, commandName)
            If token Is Nothing OrElse Not TypeOf token Is JObject Then Return Nothing

            Return CType(token, JObject)("off")?.ToString()

        Catch ex As Exception
            Debug.WriteLine($"GetToggleOffFrame error: {ex.Message}")
            Return Nothing
        End Try
    End Function

#End Region

#Region "İstatistikler"

    ''' <summary>
    ''' Toplam marka sayısını döndürür
    ''' </summary>
    Public Function GetBrandCount() As Integer
        Return GetBrands().Count
    End Function

    ''' <summary>
    ''' Toplam komut sayısını döndürür
    ''' </summary>
    Public Function GetTotalCommandCount() As Integer
        Dim count As Integer = 0

        For Each brand In GetBrands()
            For Each model In GetModels(brand)
                For Each moduleName In GetModules(brand, model)
                    count += GetCommands(brand, model, moduleName).Count
                Next
            Next
        Next

        Return count
    End Function

    ''' <summary>
    ''' Özet istatistikleri döndürür
    ''' </summary>
    Public Function GetStatistics() As Dictionary(Of String, Integer)
        Dim stats As New Dictionary(Of String, Integer)()

        Dim brandCount = GetBrands().Count
        Dim modelCount = 0
        Dim moduleCount = 0
        Dim commandCount = 0

        For Each brand In GetBrands()
            Dim models = GetModels(brand)
            modelCount += models.Count

            For Each model In models
                Dim modules = GetModules(brand, model)
                moduleCount += modules.Count

                For Each moduleName In modules
                    commandCount += GetCommands(brand, model, moduleName).Count
                Next
            Next
        Next

        stats("brands") = brandCount
        stats("models") = modelCount
        stats("modules") = moduleCount
        stats("commands") = commandCount

        Return stats
    End Function

#End Region

#Region "Arama"

    ''' <summary>
    ''' Komut adına göre arama yapar
    ''' </summary>
    Public Function SearchCommands(searchTerm As String) As List(Of Tuple(Of String, String, String, String))
        Dim results As New List(Of Tuple(Of String, String, String, String))()

        If String.IsNullOrWhiteSpace(searchTerm) Then Return results

        searchTerm = searchTerm.ToLower()

        For Each brand In GetBrands()
            For Each model In GetModels(brand)
                For Each moduleName In GetModules(brand, model)
                    For Each cmd In GetCommands(brand, model, moduleName)
                        If cmd.ToLower().Contains(searchTerm) Then
                            results.Add(Tuple.Create(brand, model, moduleName, cmd))
                        End If
                    Next
                Next
            Next
        Next

        Return results
    End Function

#End Region

#Region "Import/Export"

    ''' <summary>
    ''' JSON string olarak döndürür
    ''' </summary>
    Public Function ExportToJson() As String
        If _commandsData Is Nothing Then Return EMPTY_JSON_TEMPLATE
        Return _commandsData.ToString(Newtonsoft.Json.Formatting.Indented)
    End Function

    ''' <summary>
    ''' JSON string'den yükler
    ''' </summary>
    Public Sub ImportFromJson(jsonString As String)
        Try
            _commandsData = JObject.Parse(jsonString)
            _hasUnsavedChanges = True
            RaiseEvent OnDataLoaded()
            RaiseEvent OnDataChanged()

        Catch ex As Exception
            RaiseEvent OnError("JSON import hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Başka bir repository ile birleştirir
    ''' </summary>
    Public Sub Merge(other As CommandRepository)
        Try
            For Each brand In other.GetBrands()
                For Each model In other.GetModels(brand)
                    For Each moduleName In other.GetModules(brand, model)
                        For Each cmd In other.GetCommands(brand, model, moduleName)
                            Dim frame = other.GetCommand(brand, model, moduleName, cmd)
                            AddCommand(brand, model, moduleName, cmd, frame)
                        Next
                    Next
                Next
            Next

        Catch ex As Exception
            RaiseEvent OnError("Merge hatası: " & ex.Message)
        End Try
    End Sub

#End Region

#Region "Durum Raporu"

    ''' <summary>
    ''' Repository durumunu özetleyen rapor döndürür
    ''' </summary>
    Public Function GetStatusReport() As String
        Dim sb As New System.Text.StringBuilder()
        Dim stats = GetStatistics()

        sb.AppendLine("=== Command Repository Durumu ===")
        sb.AppendLine($"Dosya: {_jsonFilePath}")
        sb.AppendLine($"Yüklü: {If(IsLoaded, "Evet", "Hayır")}")
        sb.AppendLine($"Kaydedilmemiş değişiklik: {If(_hasUnsavedChanges, "Evet", "Hayır")}")
        sb.AppendLine()
        sb.AppendLine($"Marka sayısı: {stats("brands")}")
        sb.AppendLine($"Model sayısı: {stats("models")}")
        sb.AppendLine($"Modül sayısı: {stats("modules")}")
        sb.AppendLine($"Komut sayısı: {stats("commands")}")

        Return sb.ToString()
    End Function

#End Region

End Class

End Namespace

