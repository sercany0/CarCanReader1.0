' ErrorHandler.vb
' Merkezi hata yönetim servisi
'
' Özellikler:
'   - Singleton pattern
'   - Errors.log dosyasına atomic write
'   - Son 1000 hatayı bellekte tutma
'   - Kullanıcı dostu Türkçe mesajlar
'   - UI event'leri ile entegrasyon
'
' Kullanım:
'   ErrorHandler.Instance.LogError(ex, "OBDService.RequestPID")
'   ErrorHandler.Instance.LogWarning("Bağlantı zayıf")

Imports System.IO
Imports System.Collections.Concurrent
Imports System.Text
Imports System.Windows.Forms

Namespace Services

#Region "Error Models"

    ''' <summary>
    ''' Hata seviyesi
    ''' </summary>
    Public Enum ErrorSeverity
        Debug = 0
        Info = 1
        Warning = 2
        [Error] = 3
        Critical = 4
    End Enum

    ''' <summary>
    ''' Log edilmiş hata bilgisi
    ''' </summary>
    Public Class ErrorLogEntry
        Public Property Timestamp As DateTime
        Public Property Severity As ErrorSeverity
        Public Property Context As String
        Public Property Message As String
        Public Property TechnicalDetails As String
        Public Property StackTrace As String
        Public Property ExceptionType As String

        ''' <summary>
        ''' Kullanıcı dostu özet
        ''' </summary>
        Public ReadOnly Property UserFriendlyMessage As String
            Get
                Return GetUserFriendlyMessage(Message, Context)
            End Get
        End Property

        ''' <summary>
        ''' Tam log satırı
        ''' </summary>
        Public ReadOnly Property FullLogLine As String
            Get
                Dim sb As New StringBuilder()
                sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ")
                sb.Append($"[{Severity.ToString().ToUpper()}] ")
                sb.Append($"[{Context}] ")
                sb.Append(Message)
                If Not String.IsNullOrEmpty(TechnicalDetails) Then
                    sb.Append($" | Details: {TechnicalDetails}")
                End If
                Return sb.ToString()
            End Get
        End Property

        ''' <summary>
        ''' Teknik detayları içeren tam bilgi
        ''' </summary>
        Public ReadOnly Property FullDetails As String
            Get
                Dim sb As New StringBuilder()
                sb.AppendLine($"Zaman: {Timestamp:yyyy-MM-dd HH:mm:ss.fff}")
                sb.AppendLine($"Seviye: {Severity}")
                sb.AppendLine($"Bağlam: {Context}")
                sb.AppendLine($"Mesaj: {Message}")
                If Not String.IsNullOrEmpty(ExceptionType) Then
                    sb.AppendLine($"Exception Tipi: {ExceptionType}")
                End If
                If Not String.IsNullOrEmpty(TechnicalDetails) Then
                    sb.AppendLine($"Teknik Detay: {TechnicalDetails}")
                End If
                If Not String.IsNullOrEmpty(StackTrace) Then
                    sb.AppendLine("Stack Trace:")
                    sb.AppendLine(StackTrace)
                End If
                Return sb.ToString()
            End Get
        End Property

        ''' <summary>
        ''' Kullanıcı dostu mesaj oluşturur
        ''' </summary>
        Private Shared Function GetUserFriendlyMessage(message As String, context As String) As String
            ' Yaygın hata mesajlarını Türkçeleştir
            Dim lowerMsg = message.ToLower()

            If lowerMsg.Contains("timeout") OrElse lowerMsg.Contains("zaman aşımı") Then
                Return "İstek zaman aşımına uğradı. Bağlantıyı kontrol edin."
            ElseIf lowerMsg.Contains("connection") OrElse lowerMsg.Contains("bağlantı") Then
                Return "Bağlantı sorunu oluştu. Adaptörü kontrol edin."
            ElseIf lowerMsg.Contains("port") AndAlso lowerMsg.Contains("access") Then
                Return "Seri port erişim hatası. Port başka uygulama tarafından kullanılıyor olabilir."
            ElseIf lowerMsg.Contains("null") OrElse lowerMsg.Contains("nothing") Then
                Return "Beklenmeyen bir hata oluştu. Uygulama yeniden başlatılmalı."
            ElseIf lowerMsg.Contains("json") OrElse lowerMsg.Contains("parse") Then
                Return "Veri formatı hatası. Veritabanı dosyası bozulmuş olabilir."
            ElseIf lowerMsg.Contains("file") OrElse lowerMsg.Contains("dosya") Then
                Return "Dosya işlem hatası. Dosya erişim izinlerini kontrol edin."
            ElseIf lowerMsg.Contains("unauthorized") OrElse lowerMsg.Contains("yetki") Then
                Return "Erişim izni hatası. Uygulamayı yönetici olarak çalıştırın."
            Else
                ' Context'e göre özelleştir
                Select Case context.ToLower()
                    Case "obdservice", "obd"
                        Return "OBD iletişim hatası. Araç bağlantısını kontrol edin."
                    Case "cansender", "can"
                        Return "CAN gönderim hatası. Adaptör bağlantısını kontrol edin."
                    Case "serialport", "serial"
                        Return "Seri port hatası. Bağlantı ayarlarını kontrol edin."
                    Case "commandrepository", "json"
                        Return "Veritabanı hatası. Dosya bütünlüğünü kontrol edin."
                    Case Else
                        Return "Bir hata oluştu. Detaylar için log dosyasına bakın."
                End Select
            End If
        End Function
    End Class

#End Region

#Region "ErrorHandler Singleton"

    ''' <summary>
    ''' Merkezi hata yönetim servisi (Singleton)
    ''' </summary>
    Public NotInheritable Class ErrorHandler

#Region "Singleton"

        Private Shared _instance As ErrorHandler
        Private Shared ReadOnly _instanceLock As New Object()

        ''' <summary>
        ''' Singleton instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As ErrorHandler
            Get
                If _instance Is Nothing Then
                    SyncLock _instanceLock
                        If _instance Is Nothing Then
                            _instance = New ErrorHandler()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        Private Sub New()
            _errorLog = New ConcurrentQueue(Of ErrorLogEntry)()
            _logFilePath = GetLogFilePath()
            EnsureLogDirectory()
        End Sub

#End Region

#Region "Events"

        ''' <summary>
        ''' Hata loglandığında tetiklenir
        ''' </summary>
        Public Event OnErrorLogged(entry As ErrorLogEntry)

        ''' <summary>
        ''' Kritik hata oluştuğunda tetiklenir
        ''' </summary>
        Public Event OnCriticalError(entry As ErrorLogEntry)

        ''' <summary>
        ''' Uyarı loglandığında tetiklenir
        ''' </summary>
        Public Event OnWarningLogged(entry As ErrorLogEntry)

#End Region

#Region "Constants"

        Private Const MAX_LOG_ENTRIES As Integer = 1000
        Private Const LOG_FILE_NAME As String = "errors.log"
        Private Const MAX_LOG_FILE_SIZE As Long = 10 * 1024 * 1024 ' 10 MB
        Private Const DATA_FOLDER As String = "data"

#End Region

#Region "Fields"

        Private ReadOnly _errorLog As ConcurrentQueue(Of ErrorLogEntry)
        Private ReadOnly _logFilePath As String
        Private ReadOnly _fileLock As New Object()
        Private _totalErrorCount As Long = 0
        Private _totalWarningCount As Long = 0

#End Region

#Region "Properties"

        ''' <summary>
        ''' Toplam hata sayısı
        ''' </summary>
        Public ReadOnly Property TotalErrorCount As Long
            Get
                Return _totalErrorCount
            End Get
        End Property

        ''' <summary>
        ''' Toplam uyarı sayısı
        ''' </summary>
        Public ReadOnly Property TotalWarningCount As Long
            Get
                Return _totalWarningCount
            End Get
        End Property

        ''' <summary>
        ''' Bellekteki log sayısı
        ''' </summary>
        Public ReadOnly Property LogCount As Integer
            Get
                Return _errorLog.Count
            End Get
        End Property

        ''' <summary>
        ''' Log dosyası yolu
        ''' </summary>
        Public ReadOnly Property LogFilePath As String
            Get
                Return _logFilePath
            End Get
        End Property

        ''' <summary>
        ''' Son hata
        ''' </summary>
        Public ReadOnly Property LastError As ErrorLogEntry
            Get
                Return _errorLog.LastOrDefault()
            End Get
        End Property

#End Region

#Region "Path Management"

        ''' <summary>
        ''' Log dosyası yolunu oluşturur
        ''' </summary>
        Private Shared Function GetLogFilePath() As String
            Dim basePath As String = Application.StartupPath
            Dim dataPath As String = Path.Combine(basePath, DATA_FOLDER)
            Return Path.Combine(dataPath, LOG_FILE_NAME)
        End Function

        ''' <summary>
        ''' Log dizininin var olmasını sağlar
        ''' </summary>
        Private Sub EnsureLogDirectory()
            Try
                Dim dir = Path.GetDirectoryName(_logFilePath)
                If Not Directory.Exists(dir) Then
                    Directory.CreateDirectory(dir)
                End If
            Catch ex As Exception
                Debug.WriteLine($"EnsureLogDirectory error: {ex.Message}")
            End Try
        End Sub

#End Region

#Region "Logging Methods"

        ''' <summary>
        ''' Exception loglar
        ''' </summary>
        Public Sub LogError(ex As Exception, context As String)
            If ex Is Nothing Then Return

            Dim entry As New ErrorLogEntry() With {
                .Timestamp = DateTime.Now,
                .Severity = ErrorSeverity.Error,
                .Context = If(context, "Unknown"),
                .Message = ex.Message,
                .TechnicalDetails = GetExceptionDetails(ex),
                .StackTrace = ex.StackTrace,
                .ExceptionType = ex.GetType().Name
            }

            LogEntry(entry)
            Threading.Interlocked.Increment(_totalErrorCount)
        End Sub

        ''' <summary>
        ''' Kritik hata loglar
        ''' </summary>
        Public Sub LogCritical(ex As Exception, context As String)
            If ex Is Nothing Then Return

            Dim entry As New ErrorLogEntry() With {
                .Timestamp = DateTime.Now,
                .Severity = ErrorSeverity.Critical,
                .Context = If(context, "Unknown"),
                .Message = ex.Message,
                .TechnicalDetails = GetExceptionDetails(ex),
                .StackTrace = ex.StackTrace,
                .ExceptionType = ex.GetType().Name
            }

            LogEntry(entry)
            Threading.Interlocked.Increment(_totalErrorCount)

            ' Kritik hata event'i
            RaiseEvent OnCriticalError(entry)
        End Sub

        ''' <summary>
        ''' Uyarı loglar
        ''' </summary>
        Public Sub LogWarning(message As String, Optional context As String = "")
            Dim entry As New ErrorLogEntry() With {
                .Timestamp = DateTime.Now,
                .Severity = ErrorSeverity.Warning,
                .Context = If(String.IsNullOrEmpty(context), "General", context),
                .Message = message,
                .TechnicalDetails = "",
                .StackTrace = "",
                .ExceptionType = ""
            }

            LogEntry(entry)
            Threading.Interlocked.Increment(_totalWarningCount)

            RaiseEvent OnWarningLogged(entry)
        End Sub

        ''' <summary>
        ''' Info loglar
        ''' </summary>
        Public Sub LogInfo(message As String, Optional context As String = "")
            Dim entry As New ErrorLogEntry() With {
                .Timestamp = DateTime.Now,
                .Severity = ErrorSeverity.Info,
                .Context = If(String.IsNullOrEmpty(context), "General", context),
                .Message = message,
                .TechnicalDetails = "",
                .StackTrace = "",
                .ExceptionType = ""
            }

            LogEntry(entry)
        End Sub

        ''' <summary>
        ''' Debug loglar
        ''' </summary>
        Public Sub LogDebug(message As String, Optional context As String = "")
#If DEBUG Then
            Dim entry As New ErrorLogEntry() With {
                .Timestamp = DateTime.Now,
                .Severity = ErrorSeverity.Debug,
                .Context = If(String.IsNullOrEmpty(context), "Debug", context),
                .Message = message,
                .TechnicalDetails = "",
                .StackTrace = "",
                .ExceptionType = ""
            }

            LogEntry(entry)
#End If
        End Sub

        ''' <summary>
        ''' Mesaj ile hata loglar
        ''' </summary>
        Public Sub LogError(message As String, context As String)
            Dim entry As New ErrorLogEntry() With {
                .Timestamp = DateTime.Now,
                .Severity = ErrorSeverity.Error,
                .Context = If(context, "Unknown"),
                .Message = message,
                .TechnicalDetails = "",
                .StackTrace = Environment.StackTrace,
                .ExceptionType = ""
            }

            LogEntry(entry)
            Threading.Interlocked.Increment(_totalErrorCount)
        End Sub

#End Region

#Region "Internal Logging"

        ''' <summary>
        ''' Entry'i loglar
        ''' </summary>
        Private Sub LogEntry(entry As ErrorLogEntry)
            ' Belleğe ekle
            _errorLog.Enqueue(entry)

            ' Limit aşımı kontrolü
            While _errorLog.Count > MAX_LOG_ENTRIES
                Dim discarded As ErrorLogEntry = Nothing
                _errorLog.TryDequeue(discarded)
            End While

            ' Dosyaya yaz (async)
            Task.Run(Sub() WriteToFile(entry))

            ' Debug output
            Debug.WriteLine(entry.FullLogLine)

            ' Event tetikle
            If entry.Severity >= ErrorSeverity.Error Then
                RaiseEvent OnErrorLogged(entry)
            End If
        End Sub

        ''' <summary>
        ''' Dosyaya yazar
        ''' </summary>
        Private Sub WriteToFile(entry As ErrorLogEntry)
            SyncLock _fileLock
                Try
                    ' Dosya boyutu kontrolü
                    CheckLogFileSize()

                    ' Dosyaya ekle
                    File.AppendAllText(_logFilePath, entry.FullLogLine & Environment.NewLine)

                Catch ex As Exception
                    Debug.WriteLine($"WriteToFile error: {ex.Message}")
                End Try
            End SyncLock
        End Sub

        ''' <summary>
        ''' Log dosyası boyutunu kontrol eder ve gerekirse rotate eder
        ''' </summary>
        Private Sub CheckLogFileSize()
            Try
                If File.Exists(_logFilePath) Then
                    Dim fileInfo As New FileInfo(_logFilePath)
                    If fileInfo.Length > MAX_LOG_FILE_SIZE Then
                        ' Eski dosyayı yedekle
                        Dim backupPath = _logFilePath & ".old"
                        If File.Exists(backupPath) Then
                            File.Delete(backupPath)
                        End If
                        File.Move(_logFilePath, backupPath)
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"CheckLogFileSize error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Exception detaylarını çıkarır
        ''' </summary>
        Private Shared Function GetExceptionDetails(ex As Exception) As String
            Dim sb As New StringBuilder()

            ' Inner exception varsa ekle
            If ex.InnerException IsNot Nothing Then
                sb.Append($"Inner: {ex.InnerException.Message}")
            End If

            ' Data dictionary
            If ex.Data IsNot Nothing AndAlso ex.Data.Count > 0 Then
                For Each key As Object In ex.Data.Keys
                    sb.Append($" | {key}={ex.Data(key)}")
                Next
            End If

            ' HResult
            If ex.HResult <> 0 Then
                sb.Append($" | HResult: 0x{ex.HResult:X8}")
            End If

            Return sb.ToString()
        End Function

#End Region

#Region "Query Methods"

        ''' <summary>
        ''' Tüm log'ları döndürür
        ''' </summary>
        Public Function GetAllLogs() As List(Of ErrorLogEntry)
            Return _errorLog.ToList()
        End Function

        ''' <summary>
        ''' Son N log'u döndürür
        ''' </summary>
        Public Function GetRecentLogs(count As Integer) As List(Of ErrorLogEntry)
            Dim allLogs = _errorLog.ToList()
            Dim skipCount = Math.Max(0, allLogs.Count - count)
            Return allLogs.Skip(skipCount).ToList()
        End Function

        ''' <summary>
        ''' Seviyeye göre filtreler
        ''' </summary>
        Public Function GetLogsBySeverity(severity As ErrorSeverity) As List(Of ErrorLogEntry)
            Return _errorLog.Where(Function(e) e.Severity = severity).ToList()
        End Function

        ''' <summary>
        ''' Hataları döndürür (Error ve Critical)
        ''' </summary>
        Public Function GetErrors() As List(Of ErrorLogEntry)
            Return _errorLog.Where(Function(e) e.Severity >= ErrorSeverity.Error).ToList()
        End Function

        ''' <summary>
        ''' Context'e göre filtreler
        ''' </summary>
        Public Function GetLogsByContext(context As String) As List(Of ErrorLogEntry)
            Return _errorLog.Where(Function(e) e.Context.ToLower().Contains(context.ToLower())).ToList()
        End Function

        ''' <summary>
        ''' Log'ları temizler
        ''' </summary>
        Public Sub ClearMemoryLogs()
            Dim entry As ErrorLogEntry = Nothing
            While _errorLog.TryDequeue(entry)
            End While
        End Sub

#End Region

#Region "Report Methods"

        ''' <summary>
        ''' Özet rapor oluşturur
        ''' </summary>
        Public Function GetSummaryReport() As String
            Dim sb As New StringBuilder()
            sb.AppendLine("=== Hata Raporu ===")
            sb.AppendLine($"Toplam Hata: {_totalErrorCount}")
            sb.AppendLine($"Toplam Uyarı: {_totalWarningCount}")
            sb.AppendLine($"Bellekteki Log: {_errorLog.Count}")
            sb.AppendLine($"Log Dosyası: {_logFilePath}")

            ' Son 5 hata
            Dim errorList = GetErrors()
            Dim skipCount = Math.Max(0, errorList.Count - 5)
            Dim recentErrors = errorList.Skip(skipCount).ToList()
            
            If recentErrors.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine("Son Hatalar:")
                For Each errorEntry In recentErrors
                    sb.AppendLine($"  [{errorEntry.Timestamp:HH:mm:ss}] {errorEntry.Context}: {errorEntry.Message}")
                Next
            End If

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Log dosyasını açar
        ''' </summary>
        Public Sub OpenLogFile()
            Try
                If File.Exists(_logFilePath) Then
                    Process.Start("notepad.exe", _logFilePath)
                Else
                    MessageBox.Show("Log dosyası henüz oluşturulmamış.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Catch ex As Exception
                Debug.WriteLine($"OpenLogFile error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Log klasörünü açar
        ''' </summary>
        Public Sub OpenLogFolder()
            Try
                Dim folder = Path.GetDirectoryName(_logFilePath)
                If Directory.Exists(folder) Then
                    Process.Start("explorer.exe", folder)
                End If
            Catch ex As Exception
                Debug.WriteLine($"OpenLogFolder error: {ex.Message}")
            End Try
        End Sub

#End Region

    End Class

#End Region

#Region "Extension Methods"

    ''' <summary>
    ''' Exception için extension metodlar
    ''' </summary>
    Public Module ExceptionExtensions

        ''' <summary>
        ''' Exception'ı loglar ve kullanıcı dostu mesaj döndürür
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Function LogAndGetMessage(ex As Exception, context As String) As String
            ErrorHandler.Instance.LogError(ex, context)
            Return ErrorHandler.Instance.LastError?.UserFriendlyMessage
        End Function

        ''' <summary>
        ''' Exception'ı kritik olarak loglar
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Sub LogCritical(ex As Exception, context As String)
            ErrorHandler.Instance.LogCritical(ex, context)
        End Sub

    End Module

#End Region

End Namespace

