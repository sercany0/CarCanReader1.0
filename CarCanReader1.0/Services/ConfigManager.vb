' ConfigManager.vb
' Merkezi konfigürasyon yönetim servisi
'
' Özellikler:
'   - Singleton pattern
'   - data/config.json dosyası
'   - Atomic write (temp → validate → replace)
'   - Varsayılan değerler
'   - Değişiklik event'leri
'
' Kullanım:
'   Dim timeout = ConfigManager.Instance.OBD.RequestTimeout
'   ConfigManager.Instance.Serial.DefaultBaudRate = 250000
'   ConfigManager.Instance.Save()

Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Windows.Forms
Imports Services.Interfaces
Imports Services.Providers

Namespace Services

#Region "Configuration Models"

    ''' <summary>
    ''' Seri port ayarları
    ''' </summary>
    Public Class SerialConfig
        Public Property DefaultBaudRate As Integer = 500000
        Public Property ReadTimeout As Integer = 1000
        Public Property WriteTimeout As Integer = 1000
        Public Property BufferSize As Integer = 4096
        Public Property DefaultPort As String = ""
        Public Property AutoConnect As Boolean = False
        
        ' Son bağlantı bilgileri
        Public Property LastPort As String = ""
        Public Property LastBaudRate As Integer = 500000
        Public Property LastProtocol As String = ""
        Public Property LastConnectionSuccessful As Boolean = False
        Public Property LastConnectionTime As DateTime = DateTime.MinValue

        ''' <summary>
        ''' Varsayılan değerlere sıfırla
        ''' </summary>
        Public Sub ResetToDefaults()
            DefaultBaudRate = 500000
            ReadTimeout = 1000
            WriteTimeout = 1000
            BufferSize = 4096
            DefaultPort = ""
            AutoConnect = False
            LastPort = ""
            LastBaudRate = 500000
            LastProtocol = ""
            LastConnectionSuccessful = False
            LastConnectionTime = DateTime.MinValue
        End Sub

        ''' <summary>
        ''' Değerleri doğrula ve düzelt
        ''' </summary>
        Public Sub Validate()
            If DefaultBaudRate < 9600 OrElse DefaultBaudRate > 1000000 Then DefaultBaudRate = 500000
            If ReadTimeout < 100 OrElse ReadTimeout > 30000 Then ReadTimeout = 1000
            If WriteTimeout < 100 OrElse WriteTimeout > 30000 Then WriteTimeout = 1000
            If BufferSize < 1024 OrElse BufferSize > 65536 Then BufferSize = 4096
        End Sub
    End Class

    ''' <summary>
    ''' OBD-II ayarları
    ''' </summary>
    Public Class OBDConfig
        Public Property RequestTimeout As Integer = 2000
        Public Property RetryCount As Integer = 3
        Public Property PollingInterval As Integer = 250
        Public Property AutoStartPolling As Boolean = False
        Public Property EnableFreezeFrame As Boolean = True
        Public Property EnablePendingDTC As Boolean = True

        Public Sub ResetToDefaults()
            RequestTimeout = 2000
            RetryCount = 3
            PollingInterval = 250
            AutoStartPolling = False
            EnableFreezeFrame = True
            EnablePendingDTC = True
        End Sub

        Public Sub Validate()
            If RequestTimeout < 500 OrElse RequestTimeout > 10000 Then RequestTimeout = 2000
            If RetryCount < 0 OrElse RetryCount > 10 Then RetryCount = 3
            If PollingInterval < 50 OrElse PollingInterval > 5000 Then PollingInterval = 250
        End Sub
    End Class

    ''' <summary>
    ''' UI ayarları
    ''' </summary>
    Public Class UIConfig
        Public Property Theme As String = "dark"
        Public Property Language As String = "tr"
        Public Property LogMaxLines As Integer = 5000
        Public Property DashboardRefreshRate As Integer = 100
        Public Property ShowToolTips As Boolean = True
        Public Property ConfirmOnExit As Boolean = True
        Public Property RememberWindowPosition As Boolean = True
        Public Property WindowX As Integer = -1
        Public Property WindowY As Integer = -1
        Public Property WindowWidth As Integer = -1
        Public Property WindowHeight As Integer = -1
        Public Property WindowMaximized As Boolean = False

        Public Sub ResetToDefaults()
            Theme = "dark"
            Language = "tr"
            LogMaxLines = 5000
            DashboardRefreshRate = 100
            ShowToolTips = True
            ConfirmOnExit = True
            RememberWindowPosition = True
            WindowX = -1
            WindowY = -1
            WindowWidth = -1
            WindowHeight = -1
            WindowMaximized = False
        End Sub

        Public Sub Validate()
            If String.IsNullOrEmpty(Theme) Then Theme = "dark"
            If String.IsNullOrEmpty(Language) Then Language = "tr"
            If LogMaxLines < 100 OrElse LogMaxLines > 50000 Then LogMaxLines = 5000
            If DashboardRefreshRate < 50 OrElse DashboardRefreshRate > 1000 Then DashboardRefreshRate = 100
        End Sub
    End Class

    ''' <summary>
    ''' Dosya yolu ayarları
    ''' </summary>
    Public Class PathsConfig
        Public Property LogsFolder As String = "logs"
        Public Property ExportsFolder As String = "exports"
        Public Property SessionsFolder As String = "sessions"
        Public Property BackupsFolder As String = "backups"

        Public Sub ResetToDefaults()
            LogsFolder = "logs"
            ExportsFolder = "exports"
            SessionsFolder = "sessions"
            BackupsFolder = "backups"
        End Sub

        Public Sub Validate()
            If String.IsNullOrEmpty(LogsFolder) Then LogsFolder = "logs"
            If String.IsNullOrEmpty(ExportsFolder) Then ExportsFolder = "exports"
            If String.IsNullOrEmpty(SessionsFolder) Then SessionsFolder = "sessions"
            If String.IsNullOrEmpty(BackupsFolder) Then BackupsFolder = "backups"
        End Sub

        ''' <summary>
        ''' Tam yolu döndürür
        ''' </summary>
        Public Function GetFullPath(relativePath As String) As String
            Return Path.Combine(Application.StartupPath, "data", relativePath)
        End Function

        ''' <summary>
        ''' Logs klasörünün tam yolu
        ''' </summary>
        Public ReadOnly Property LogsFullPath As String
            Get
                Return GetFullPath(LogsFolder)
            End Get
        End Property

        ''' <summary>
        ''' Exports klasörünün tam yolu
        ''' </summary>
        Public ReadOnly Property ExportsFullPath As String
            Get
                Return GetFullPath(ExportsFolder)
            End Get
        End Property

        ''' <summary>
        ''' Sessions klasörünün tam yolu
        ''' </summary>
        Public ReadOnly Property SessionsFullPath As String
            Get
                Return GetFullPath(SessionsFolder)
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Firma bilgileri (raporlar için)
    ''' </summary>
    Public Class CompanyConfig
        Public Property CompanyName As String = ""
        Public Property CompanyAddress As String = ""
        Public Property CompanyPhone As String = ""
        Public Property CompanyEmail As String = ""
        Public Property CompanyLogoPath As String = ""

        Public Sub ResetToDefaults()
            CompanyName = ""
            CompanyAddress = ""
            CompanyPhone = ""
            CompanyEmail = ""
            CompanyLogoPath = ""
        End Sub

        Public Sub Validate()
            ' Boş bırakılabilir, sadece format kontrolü
        End Sub
    End Class

    ''' <summary>
    ''' Gelişmiş ayarlar
    ''' </summary>
    Public Class AdvancedConfig
        Public Property EnableDebugMode As Boolean = False
        Public Property EnableLearningEngine As Boolean = True
        Public Property EnableAIFinder As Boolean = False
        Public Property MaxCANBufferSize As Integer = 10000
        Public Property EnableAutoEnrichment As Boolean = True
        Public Property EnableVINAutoProfile As Boolean = True

        Public Sub ResetToDefaults()
            EnableDebugMode = False
            EnableLearningEngine = True
            EnableAIFinder = False
            MaxCANBufferSize = 10000
            EnableAutoEnrichment = True
            EnableVINAutoProfile = True
        End Sub

        Public Sub Validate()
            If MaxCANBufferSize < 1000 OrElse MaxCANBufferSize > 100000 Then MaxCANBufferSize = 10000
        End Sub
    End Class

    ''' <summary>
    ''' Ana konfigürasyon modeli
    ''' </summary>
    Public Class AppConfiguration
        Public Property Serial As New SerialConfig()
        Public Property OBD As New OBDConfig()
        Public Property UI As New UIConfig()
        Public Property Paths As New PathsConfig()
        Public Property Advanced As New AdvancedConfig()
        Public Property Company As New CompanyConfig()
        Public Property Version As String = "1.0"
        Public Property LastModified As DateTime = DateTime.Now

        ''' <summary>
        ''' Tüm ayarları varsayılana sıfırla
        ''' </summary>
        Public Sub ResetToDefaults()
            Serial.ResetToDefaults()
            OBD.ResetToDefaults()
            UI.ResetToDefaults()
            Paths.ResetToDefaults()
            Advanced.ResetToDefaults()
            Company.ResetToDefaults()
            LastModified = DateTime.Now
        End Sub

        ''' <summary>
        ''' Tüm değerleri doğrula
        ''' </summary>
        Public Sub ValidateAll()
            Serial.Validate()
            OBD.Validate()
            UI.Validate()
            Paths.Validate()
            Advanced.Validate()
            Company.Validate()
        End Sub
    End Class

#End Region

#Region "ConfigManager Singleton"

    ''' <summary>
    ''' Merkezi konfigürasyon yönetim servisi (Singleton)
    ''' </summary>
    Public NotInheritable Class ConfigManager

#Region "Singleton"

        Private Shared _instance As ConfigManager
        Private Shared ReadOnly _instanceLock As New Object()

        ''' <summary>
        ''' Singleton instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As ConfigManager
            Get
                If _instance Is Nothing Then
                    SyncLock _instanceLock
                        If _instance Is Nothing Then
                            _instance = New ConfigManager()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        Public Shared Function CreateWithProvider(provider As IConfigProvider) As ConfigManager
            Return New ConfigManager(provider)
        End Function

        Private Sub New()
            Me.New(New FileConfigProvider(GetConfigFilePath()))
        End Sub

        Private Sub New(provider As IConfigProvider)
            If provider Is Nothing Then Throw New ArgumentNullException(NameOf(provider))
            _configProvider = provider
            _configFilePath = provider.GetLocation()
            _config = New AppConfiguration()
            Load()
        End Sub

#End Region

#Region "Events"

        ''' <summary>
        ''' Ayar değiştiğinde tetiklenir
        ''' </summary>
        Public Event OnConfigChanged(section As String)

        ''' <summary>
        ''' Ayarlar kaydedildiğinde tetiklenir
        ''' </summary>
        Public Event OnConfigSaved()

        ''' <summary>
        ''' Ayarlar yüklendiğinde tetiklenir
        ''' </summary>
        Public Event OnConfigLoaded()

        ''' <summary>
        ''' Varsayılana sıfırlandığında tetiklenir
        ''' </summary>
        Public Event OnConfigReset()

#End Region

#Region "Constants"

        Private Const CONFIG_FILE_NAME As String = "config.json"
        Private Const DATA_FOLDER As String = "data"

#End Region

#Region "Fields"

        Private ReadOnly _configFilePath As String
        Private _config As AppConfiguration
        Private ReadOnly _fileLock As New Object()
        Private _hasUnsavedChanges As Boolean = False
        Private ReadOnly _configProvider As IConfigProvider

#End Region

#Region "Properties - Direct Access"

        ''' <summary>
        ''' Seri port ayarları
        ''' </summary>
        Public ReadOnly Property Serial As SerialConfig
            Get
                Return _config.Serial
            End Get
        End Property

        ''' <summary>
        ''' OBD-II ayarları
        ''' </summary>
        Public ReadOnly Property OBD As OBDConfig
            Get
                Return _config.OBD
            End Get
        End Property

        ''' <summary>
        ''' UI ayarları
        ''' </summary>
        Public ReadOnly Property UI As UIConfig
            Get
                Return _config.UI
            End Get
        End Property

        ''' <summary>
        ''' Dosya yolu ayarları
        ''' </summary>
        Public ReadOnly Property Paths As PathsConfig
            Get
                Return _config.Paths
            End Get
        End Property

        ''' <summary>
        ''' Gelişmiş ayarlar
        ''' </summary>
        Public ReadOnly Property Advanced As AdvancedConfig
            Get
                Return _config.Advanced
            End Get
        End Property

        ''' <summary>
        ''' Firma bilgileri
        ''' </summary>
        Public ReadOnly Property Company As CompanyConfig
            Get
                Return _config.Company
            End Get
        End Property

        ''' <summary>
        ''' Config dosyası yolu
        ''' </summary>
        Public ReadOnly Property ConfigFilePath As String
            Get
                Return _configFilePath
            End Get
        End Property

        ''' <summary>
        ''' Kaydedilmemiş değişiklik var mı
        ''' </summary>
        Public ReadOnly Property HasUnsavedChanges As Boolean
            Get
                Return _hasUnsavedChanges
            End Get
        End Property

#End Region

#Region "Path Management"

        ''' <summary>
        ''' Config dosyası yolunu oluşturur
        ''' </summary>
        Private Shared Function GetConfigFilePath() As String
            Dim basePath As String = Application.StartupPath
            Dim dataPath As String = Path.Combine(basePath, DATA_FOLDER)
            Return Path.Combine(dataPath, CONFIG_FILE_NAME)
        End Function

        ''' <summary>
        ''' Data klasörünün var olmasını sağlar
        ''' </summary>
        Private Sub EnsureDataFolder()
            Try
                If _configProvider Is Nothing OrElse TypeOf _configProvider Is FileConfigProvider Then
                    Dim dir = Path.GetDirectoryName(_configFilePath)
                    If Not String.IsNullOrWhiteSpace(dir) AndAlso Not Directory.Exists(dir) Then
                        Directory.CreateDirectory(dir)
                    End If
                End If
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ConfigManager.EnsureDataFolder")
            End Try
        End Sub

#End Region

#Region "Load/Save Operations"

        ''' <summary>
        ''' Konfigürasyonu yükler
        ''' </summary>
        Public Sub Load()
            SyncLock _fileLock
                Try
                    EnsureDataFolder()

                    If _configProvider IsNot Nothing Then
                        Dim raw As JObject = _configProvider.LoadConfig()

                        If raw Is Nothing OrElse Not raw.HasValues Then
                            _config = New AppConfiguration()
                            Save()
                            RaiseEvent OnConfigLoaded()
                            Return
                        End If

                        Dim loadedFromProvider = raw.ToObject(Of AppConfiguration)()
                        If loadedFromProvider IsNot Nothing Then
                            _config = loadedFromProvider
                            _config.ValidateAll()
                        Else
                            _config = New AppConfiguration()
                        End If

                        _hasUnsavedChanges = False
                        RaiseEvent OnConfigLoaded()

                        Debug.WriteLine($"ConfigManager loaded via provider: {_configProvider.GetLocation()}")
                        Return
                    End If

                    If Not File.Exists(_configFilePath) Then
                        ' Dosya yoksa varsayılanlarla oluştur
                        _config = New AppConfiguration()
                        Save()
                        RaiseEvent OnConfigLoaded()
                        Return
                    End If

                    Dim jsonText = File.ReadAllText(_configFilePath)
                    If String.IsNullOrWhiteSpace(jsonText) Then
                        _config = New AppConfiguration()
                        Save()
                        RaiseEvent OnConfigLoaded()
                        Return
                    End If

                    ' Parse et
                    Dim loaded = JsonConvert.DeserializeObject(Of AppConfiguration)(jsonText)
                    If loaded IsNot Nothing Then
                        _config = loaded
                        _config.ValidateAll()
                    Else
                        _config = New AppConfiguration()
                    End If

                    _hasUnsavedChanges = False
                    RaiseEvent OnConfigLoaded()

                    Debug.WriteLine($"ConfigManager loaded: {_configFilePath}")

                Catch ex As JsonException
                    ErrorHandler.Instance.LogWarning($"Config JSON hatası, varsayılanlar yükleniyor: {ex.Message}", "ConfigManager")
                    _config = New AppConfiguration()
                    Save()
                    RaiseEvent OnConfigLoaded()

                Catch ex As Exception
                    ErrorHandler.Instance.LogError(ex, "ConfigManager.Load")
                    _config = New AppConfiguration()
                End Try
            End SyncLock
        End Sub

        ''' <summary>
        ''' Konfigürasyonu kaydeder (ATOMIC)
        ''' </summary>
        Public Function Save() As Boolean
            SyncLock _fileLock
                Try
                    EnsureDataFolder()

                    _config.LastModified = DateTime.Now
                    _config.ValidateAll()

                    Dim jsonText = JsonConvert.SerializeObject(_config, Formatting.Indented)

                    If _configProvider IsNot Nothing Then
                        _configProvider.SaveConfig(JObject.Parse(jsonText))
                        _hasUnsavedChanges = False
                        RaiseEvent OnConfigSaved()
                        Debug.WriteLine($"ConfigManager saved via provider: {_configProvider.GetLocation()}")
                        Return True
                    Else
                        ' Atomic write
                        If AtomicWrite(_configFilePath, jsonText) Then
                            _hasUnsavedChanges = False
                            RaiseEvent OnConfigSaved()
                            Debug.WriteLine($"ConfigManager saved: {_configFilePath}")
                            Return True
                        Else
                            ErrorHandler.Instance.LogError("Config atomic write başarısız", "ConfigManager")
                            Return False
                        End If
                    End If

                Catch ex As Exception
                    ErrorHandler.Instance.LogError(ex, "ConfigManager.Save")
                    Return False
                End Try
            End SyncLock
        End Function

        ''' <summary>
        ''' ATOMIC WRITE: Temp dosyaya yaz, validate et, replace et
        ''' </summary>
        Private Function AtomicWrite(targetPath As String, content As String) As Boolean
            Dim tempPath As String = targetPath & ".tmp"
            Dim backupPath As String = targetPath & ".bak"

            Try
                ' 1. Temp dosyaya yaz
                File.WriteAllText(tempPath, content)

                ' 2. Validate et
                Try
                    Dim validation = JToken.Parse(content)
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
        ''' Dosyayı güvenli siler
        ''' </summary>
        Private Shared Sub SafeDeleteFile(path As String)
            Try
                If File.Exists(path) Then
                    File.Delete(path)
                End If
            Catch
            End Try
        End Sub

        ''' <summary>
        ''' Yeniden yükle
        ''' </summary>
        Public Sub Reload()
            Load()
        End Sub

#End Region

#Region "Reset Operations"

        ''' <summary>
        ''' Tüm ayarları varsayılana sıfırla
        ''' </summary>
        Public Sub ResetAllToDefaults()
            _config.ResetToDefaults()
            _hasUnsavedChanges = True
            Save()
            RaiseEvent OnConfigReset()
        End Sub

        ''' <summary>
        ''' Belirli bir bölümü varsayılana sıfırla
        ''' </summary>
        Public Sub ResetSectionToDefaults(section As String)
            Select Case section.ToLower()
                Case "serial"
                    _config.Serial.ResetToDefaults()
                Case "obd"
                    _config.OBD.ResetToDefaults()
                Case "ui"
                    _config.UI.ResetToDefaults()
                Case "paths"
                    _config.Paths.ResetToDefaults()
                Case "advanced"
                    _config.Advanced.ResetToDefaults()
            End Select
            _hasUnsavedChanges = True
            RaiseEvent OnConfigChanged(section)
        End Sub

#End Region

#Region "Change Notification"

        ''' <summary>
        ''' Değişiklik bildir
        ''' </summary>
        Public Sub NotifyChange(section As String)
            _hasUnsavedChanges = True
            RaiseEvent OnConfigChanged(section)
        End Sub

        ''' <summary>
        ''' Değişiklik bildir ve kaydet
        ''' </summary>
        Public Sub NotifyChangeAndSave(section As String)
            _hasUnsavedChanges = True
            Save()
            RaiseEvent OnConfigChanged(section)
        End Sub

#End Region

#Region "Helper Methods"

        ''' <summary>
        ''' Gerekli klasörleri oluşturur
        ''' </summary>
        Public Sub EnsureAllFolders()
            Try
                Dim folders = {
                    _config.Paths.LogsFullPath,
                    _config.Paths.ExportsFullPath,
                    _config.Paths.SessionsFullPath,
                    _config.Paths.GetFullPath(_config.Paths.BackupsFolder)
                }

                For Each folder In folders
                    If Not Directory.Exists(folder) Then
                        Directory.CreateDirectory(folder)
                    End If
                Next

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "ConfigManager.EnsureAllFolders")
            End Try
        End Sub

        ''' <summary>
        ''' Config özeti
        ''' </summary>
        Public Function GetSummary() As String
            Dim sb As New System.Text.StringBuilder()
            sb.AppendLine("=== Konfigürasyon Özeti ===")
            sb.AppendLine($"Dosya: {_configFilePath}")
            sb.AppendLine($"Son Değişiklik: {_config.LastModified:yyyy-MM-dd HH:mm:ss}")
            sb.AppendLine()
            sb.AppendLine("[Serial]")
            sb.AppendLine($"  BaudRate: {_config.Serial.DefaultBaudRate}")
            sb.AppendLine($"  Timeout: {_config.Serial.ReadTimeout}ms")
            sb.AppendLine()
            sb.AppendLine("[OBD]")
            sb.AppendLine($"  RequestTimeout: {_config.OBD.RequestTimeout}ms")
            sb.AppendLine($"  PollingInterval: {_config.OBD.PollingInterval}ms")
            sb.AppendLine()
            sb.AppendLine("[UI]")
            sb.AppendLine($"  Theme: {_config.UI.Theme}")
            sb.AppendLine($"  Language: {_config.UI.Language}")
            Return sb.ToString()
        End Function

#End Region

    End Class

#End Region

End Namespace

