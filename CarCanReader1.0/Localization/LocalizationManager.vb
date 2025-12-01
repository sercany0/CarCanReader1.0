Imports System.IO
Imports Newtonsoft.Json.Linq
Imports CarCanReader1._0.Services

Namespace Localization
    ''' <summary>
    ''' Çoklu dil desteği yönetimi
    ''' JSON tabanlı dil dosyaları kullanır
    ''' </summary>
    Public Class LocalizationManager
        Private Shared _instance As LocalizationManager
        Private Shared ReadOnly _lock As New Object()

        ''' <summary>
        ''' Singleton instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As LocalizationManager
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New LocalizationManager()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ''' <summary>
        ''' Desteklenen diller
        ''' </summary>
        Public Shared ReadOnly Languages As New Dictionary(Of String, String) From {
            {"tr", "Türkçe"},
            {"en", "English"},
            {"de", "Deutsch"}
        }

        ''' <summary>
        ''' Mevcut dil kodu
        ''' </summary>
        Private _currentLanguage As String = "tr"

        ''' <summary>
        ''' Yüklenmiş string'ler (key -> value)
        ''' </summary>
        Private _strings As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' Dil dosyası yolu
        ''' </summary>
        Private ReadOnly _langFolder As String

        ''' <summary>
        ''' Events
        ''' </summary>
        Public Event OnLanguageChanged(newLanguage As String)

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Private Sub New()
            _langFolder = Path.Combine(Application.StartupPath, "data", "lang")
            
            ' Klasör yoksa oluştur
            If Not Directory.Exists(_langFolder) Then
                Directory.CreateDirectory(_langFolder)
            End If

            ' Varsayılan dili yükle
            LoadLanguage("tr")
        End Sub

        ''' <summary>
        ''' Dil değiştir
        ''' </summary>
        Public Sub SetLanguage(langCode As String)
            If Not Languages.ContainsKey(langCode) Then
                ErrorHandler.Instance.LogWarning($"Desteklenmeyen dil kodu: {langCode}, varsayılan (tr) kullanılıyor")
                langCode = "tr"
            End If

            If _currentLanguage = langCode Then
                Return ' Zaten bu dil yüklü
            End If

            LoadLanguage(langCode)
            _currentLanguage = langCode

            RaiseEvent OnLanguageChanged(langCode)
            ErrorHandler.Instance.LogInfo($"Dil değiştirildi: {langCode}")
        End Sub

        ''' <summary>
        ''' Dil dosyasını yükle
        ''' </summary>
        Private Sub LoadLanguage(langCode As String)
            Try
                Dim langFile = Path.Combine(_langFolder, $"{langCode}.json")
                
                If Not File.Exists(langFile) Then
                    ErrorHandler.Instance.LogWarning($"Dil dosyası bulunamadı: {langFile}, varsayılan dil dosyası oluşturuluyor")
                    CreateDefaultLanguageFile(langFile, langCode)
                End If

                Dim jsonContent = File.ReadAllText(langFile, System.Text.Encoding.UTF8)
                Dim json = JObject.Parse(jsonContent)

                ' Dictionary'yi temizle
                _strings.Clear()

                ' Tüm key'leri düzleştirilmiş formatta yükle
                FlattenJson(json, "", _strings)

                ErrorHandler.Instance.LogInfo($"Dil yüklendi: {langCode} ({_strings.Count} string)")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, $"LocalizationManager.LoadLanguage({langCode})")
                
                ' Hata durumunda varsayılan string'leri yükle
                LoadFallbackStrings()
            End Try
        End Sub

        ''' <summary>
        ''' JSON'u düzleştirilmiş key-value formatına çevir
        ''' Örnek: {"app": {"title": "CarCanReader"}} -> "app.title" = "CarCanReader"
        ''' </summary>
        Private Sub FlattenJson(token As JToken, prefix As String, result As Dictionary(Of String, String))
            If token Is Nothing Then Return

            Select Case token.Type
                Case JTokenType.Object
                    For Each prop As JProperty In token.Children(Of JProperty)()
                        Dim newPrefix = If(String.IsNullOrEmpty(prefix), prop.Name, $"{prefix}.{prop.Name}")
                        FlattenJson(prop.Value, newPrefix, result)
                    Next

                Case JTokenType.Array
                    Dim index = 0
                    For Each item In token.Children()
                        Dim newPrefix = If(String.IsNullOrEmpty(prefix), $"[{index}]", $"{prefix}[{index}]")
                        FlattenJson(item, newPrefix, result)
                        index += 1
                    Next

                Case JTokenType.String, JTokenType.Integer, JTokenType.Float, JTokenType.Boolean
                    ' String, sayı veya boolean değer
                    Dim value = token.ToString()
                    If Not String.IsNullOrEmpty(prefix) AndAlso Not String.IsNullOrEmpty(value) Then
                        result(prefix) = value
                    End If

                Case Else
                    ' Diğer tipler için değeri string'e çevir
                    Dim value = token.ToString()
                    If Not String.IsNullOrEmpty(prefix) AndAlso Not String.IsNullOrEmpty(value) Then
                        result(prefix) = value
                    End If
            End Select
        End Sub

        ''' <summary>
        ''' String al (key ile)
        ''' </summary>
        Public Function GetString(key As String) As String
            If String.IsNullOrEmpty(key) Then
                Return String.Empty
            End If

            If _strings.ContainsKey(key) Then
                Return _strings(key)
            End If

            ' Eksik key için uyarı ve key'i döndür
            ErrorHandler.Instance.LogWarning($"Eksik çeviri anahtarı: {key}")
            Return $"[{key}]"
        End Function

        ''' <summary>
        ''' String al (format parametreleri ile)
        ''' </summary>
        Public Function GetString(key As String, ParamArray args() As Object) As String
            Try
                Dim template = GetString(key)
                Return String.Format(template, args)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, $"LocalizationManager.GetString({key}) format hatası")
                Return $"[{key}]"
            End Try
        End Function

        ''' <summary>
        ''' Mevcut dil kodu
        ''' </summary>
        Public ReadOnly Property CurrentLanguage As String
            Get
                Return _currentLanguage
            End Get
        End Property

        ''' <summary>
        ''' Kısayol metod (static)
        ''' </summary>
        Public Shared Function T(key As String) As String
            Return Instance.GetString(key)
        End Function

        ''' <summary>
        ''' Kısayol metod (format ile)
        ''' </summary>
        Public Shared Function T(key As String, ParamArray args() As Object) As String
            Return Instance.GetString(key, args)
        End Function

        ''' <summary>
        ''' Varsayılan dil dosyası oluştur
        ''' </summary>
        Private Sub CreateDefaultLanguageFile(filePath As String, langCode As String)
            Try
                Dim defaultContent As String

                Select Case langCode
                    Case "tr"
                        defaultContent = GetDefaultTurkishContent()
                    Case "en"
                        defaultContent = GetDefaultEnglishContent()
                    Case "de"
                        defaultContent = GetDefaultGermanContent()
                    Case Else
                        defaultContent = GetDefaultTurkishContent()
                End Select

                File.WriteAllText(filePath, defaultContent, System.Text.Encoding.UTF8)
                ErrorHandler.Instance.LogInfo($"Varsayılan dil dosyası oluşturuldu: {filePath}")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, $"LocalizationManager.CreateDefaultLanguageFile({filePath})")
            End Try
        End Sub

        ''' <summary>
        ''' Varsayılan Türkçe içerik
        ''' </summary>
        Private Function GetDefaultTurkishContent() As String
            Return "{""app"":{""title"":""CarCanReader"",""version"":""Versiyon {0}""},""menu"":{""file"":""Dosya"",""connection"":""Bağlantı"",""tools"":""Araçlar"",""help"":""Yardım""},""connection"":{""connect"":""Bağlan"",""disconnect"":""Bağlantıyı Kes"",""status_connected"":""Bağlı ({0}, {1})"",""status_disconnected"":""Bağlı Değil""},""dashboard"":{""rpm"":""Motor Devri"",""speed"":""Hız"",""coolant_temp"":""Soğutucu Sıcaklığı""},""errors"":{""connection_failed"":""Bağlantı kurulamadı: {0}"",""timeout"":""Zaman aşımı""}}"
        End Function

        ''' <summary>
        ''' Varsayılan İngilizce içerik
        ''' </summary>
        Private Function GetDefaultEnglishContent() As String
            Return "{""app"":{""title"":""CarCanReader"",""version"":""Version {0}""},""menu"":{""file"":""File"",""connection"":""Connection"",""tools"":""Tools"",""help"":""Help""},""connection"":{""connect"":""Connect"",""disconnect"":""Disconnect"",""status_connected"":""Connected ({0}, {1})"",""status_disconnected"":""Not Connected""},""dashboard"":{""rpm"":""Engine RPM"",""speed"":""Speed"",""coolant_temp"":""Coolant Temperature""},""errors"":{""connection_failed"":""Connection failed: {0}"",""timeout"":""Timeout""}}"
        End Function

        ''' <summary>
        ''' Varsayılan Almanca içerik
        ''' </summary>
        Private Function GetDefaultGermanContent() As String
            Return "{""app"":{""title"":""CarCanReader"",""version"":""Version {0}""},""menu"":{""file"":""Datei"",""connection"":""Verbindung"",""tools"":""Werkzeuge"",""help"":""Hilfe""},""connection"":{""connect"":""Verbinden"",""disconnect"":""Trennen"",""status_connected"":""Verbunden ({0}, {1})"",""status_disconnected"":""Nicht verbunden""},""dashboard"":{""rpm"":""Motordrehzahl"",""speed"":""Geschwindigkeit"",""coolant_temp"":""Kühlmitteltemperatur""},""errors"":{""connection_failed"":""Verbindung fehlgeschlagen: {0}"",""timeout"":""Zeitüberschreitung""}}"
        End Function

        ''' <summary>
        ''' Hata durumunda varsayılan string'ler
        ''' </summary>
        Private Sub LoadFallbackStrings()
            _strings.Clear()
            _strings("app.title") = "CarCanReader"
            _strings("app.version") = "Version {0}"
            _strings("menu.file") = "Dosya"
            _strings("menu.connection") = "Bağlantı"
            _strings("menu.tools") = "Araçlar"
            _strings("menu.help") = "Yardım"
            _strings("connection.connect") = "Bağlan"
            _strings("connection.disconnect") = "Bağlantıyı Kes"
            _strings("connection.status_connected") = "Bağlı ({0}, {1})"
            _strings("connection.status_disconnected") = "Bağlı Değil"
        End Sub
    End Class
End Namespace

