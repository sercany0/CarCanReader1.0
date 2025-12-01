Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports CarCanReader1._0.Models

Namespace Services
    ''' <summary>
    ''' CAN oturumlarını kaydetme, oynatma ve dışa aktarma servisi
    ''' Binary ve CSV formatlarını destekler
    ''' </summary>
    Public Class SessionRecorder
        Implements IDisposable

#Region "Enums"
        ''' <summary>
        ''' Kayıt dosya formatı
        ''' </summary>
        Public Enum RecordFormat
            Binary      ' Küçük boyut, hızlı
            CSV         ' Okunabilir, debug için
        End Enum

        ''' <summary>
        ''' Oynatma durumu
        ''' </summary>
        Private Enum PlaybackState
            Stopped
            Playing
            Paused
        End Enum
#End Region

#Region "Constants"
        ' Binary format magic header
        Private Const MAGIC_HEADER As String = "CCREC"
        Private Const FORMAT_VERSION As Byte = 1

        ' Frame boyutları
        Private Const FRAME_SIZE As Integer = 15 ' RelativeTime(4) + FrameId(2) + DLC(1) + Data(8)
        Private Const HEADER_SIZE As Integer = 22 ' Magic(5) + Version(1) + StartTime(8) + FrameCount(8)

        ' Buffer ayarları
        Private Const WRITE_BUFFER_SIZE As Integer = 64 * 1024 ' 64KB buffer
        Private Const FLUSH_INTERVAL_MS As Integer = 1000 ' Her 1 saniyede flush
#End Region

#Region "Properties"
        ''' <summary>
        ''' Kayıt durumu
        ''' </summary>
        Public Property IsRecording As Boolean
            Get
                Return _isRecording
            End Get
            Private Set(value As Boolean)
                _isRecording = value
            End Set
        End Property
        Private _isRecording As Boolean = False

        ''' <summary>
        ''' Duraklatma durumu
        ''' </summary>
        Public Property IsPaused As Boolean
            Get
                Return _isPaused
            End Get
            Private Set(value As Boolean)
                _isPaused = value
            End Set
        End Property
        Private _isPaused As Boolean = False

        ''' <summary>
        ''' Kaydedilen frame sayısı
        ''' </summary>
        Public Property RecordedFrameCount As Long
            Get
                SyncLock _recordLock
                    Return _recordedFrameCount
                End SyncLock
            End Get
            Private Set(value As Long)
                _recordedFrameCount = value
            End Set
        End Property
        Private _recordedFrameCount As Long = 0

        ''' <summary>
        ''' Kayıt süresi
        ''' </summary>
        Public ReadOnly Property RecordingDuration As TimeSpan
            Get
                If Not IsRecording Then
                    Return _totalRecordingDuration
                End If
                Return _totalRecordingDuration.Add(DateTime.Now - _currentRecordingStart)
            End Get
        End Property

        ''' <summary>
        ''' Kayıt dosya yolu
        ''' </summary>
        Public Property FilePath As String

        ''' <summary>
        ''' Oynatma hızı (0.5x, 1x, 2x, vb.)
        ''' </summary>
        Public Property PlaybackSpeed As Double = 1.0

        ''' <summary>
        ''' Toplam frame sayısı (yüklü oturum için)
        ''' </summary>
        Public ReadOnly Property TotalFrameCount As Long
            Get
                Return _loadedFrames.Count
            End Get
        End Property

        ''' <summary>
        ''' Mevcut oynatma pozisyonu
        ''' </summary>
        Public ReadOnly Property CurrentPlaybackFrame As Long
            Get
                Return _currentPlaybackFrame
            End Get
        End Property
#End Region

#Region "Events"
        Public Event OnRecordingStarted(filePath As String)
        Public Event OnRecordingStopped(frameCount As Long)
        Public Event OnFrameRecorded(frameNumber As Long)
        Public Event OnPlaybackProgress(current As Long, total As Long)
        Public Event OnPlaybackFrame(timestamp As Long, frameId As Integer, data As Byte())
        Public Event OnPlaybackStarted()
        Public Event OnPlaybackStopped()
        Public Event OnError(message As String)
#End Region

#Region "Private Fields"
        ' Kayıt için
        Private _recordFormat As RecordFormat
        Private _recordStream As FileStream
        Private _recordWriter As BinaryWriter
        Private _csvWriter As StreamWriter
        Private _recordStartTime As DateTime
        Private _currentRecordingStart As DateTime
        Private _totalRecordingDuration As TimeSpan
        Private _recordLock As New Object()
        Private _writeBuffer As New List(Of RecordedFrame)()
        Private _flushTimer As Timer
        Private _recordCancellationToken As CancellationTokenSource

        ' Oynatma için
        Private _loadedFrames As New List(Of RecordedFrame)()
        Private _playbackState As PlaybackState = PlaybackState.Stopped
        Private _currentPlaybackFrame As Long = 0
        Private _playbackTask As Task
        Private _playbackCancellationToken As CancellationTokenSource
        Private _sessionStartTime As DateTime

        ' Dispose
        Private _disposed As Boolean = False
#End Region

#Region "Internal Classes"
        ''' <summary>
        ''' Kaydedilmiş frame yapısı
        ''' </summary>
        Private Class RecordedFrame
            Public Property RelativeTimeMs As UInteger ' Kayıt başlangıcından itibaren ms
            Public Property FrameId As UShort
            Public Property DLC As Byte
            Public Property Data As Byte()

            Public Sub New()
                Data = New Byte(7) {}
            End Sub
        End Class
#End Region

#Region "Recording Methods"
        ''' <summary>
        ''' Kayıt başlat
        ''' </summary>
        Public Sub StartRecording(filePath As String, format As RecordFormat)
            If IsRecording Then
                Throw New InvalidOperationException("Kayıt zaten devam ediyor")
            End If

            Try
                Me.FilePath = filePath
                _recordFormat = format
                _recordedFrameCount = 0
                _totalRecordingDuration = TimeSpan.Zero
                _writeBuffer.Clear()

                ' Dosya oluştur
                _recordStream = New FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, WRITE_BUFFER_SIZE, True)

                If format = RecordFormat.Binary Then
                    _recordWriter = New BinaryWriter(_recordStream, Encoding.UTF8)
                    WriteBinaryHeader()
                Else
                    _csvWriter = New StreamWriter(_recordStream, Encoding.UTF8)
                    WriteCSVHeader()
                End If

                _recordStartTime = DateTime.Now
                _currentRecordingStart = _recordStartTime
                _sessionStartTime = _recordStartTime
                IsRecording = True
                IsPaused = False

                ' Periyodik flush timer başlat
                _recordCancellationToken = New CancellationTokenSource()
                _flushTimer = New Timer(AddressOf FlushTimerCallback, Nothing, FLUSH_INTERVAL_MS, FLUSH_INTERVAL_MS)

                RaiseEvent OnRecordingStarted(filePath)
                ErrorHandler.Instance.LogInfo($"Kayıt başlatıldı: {filePath} ({format})")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.StartRecording")
                Cleanup()
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Kayıt durdur
        ''' </summary>
        Public Sub StopRecording()
            If Not IsRecording Then
                Return
            End If

            Try
                IsRecording = False
                IsPaused = False

                ' Timer'ı durdur
                If _flushTimer IsNot Nothing Then
                    _flushTimer.Dispose()
                    _flushTimer = Nothing
                End If

                ' Son buffer'ı flush et
                FlushWriteBuffer()

                ' Binary format için header'ı güncelle (frame count)
                If _recordFormat = RecordFormat.Binary AndAlso _recordWriter IsNot Nothing Then
                    UpdateBinaryHeader()
                End If

                ' Kaynakları temizle
                Cleanup()

                Dim finalCount = _recordedFrameCount
                RaiseEvent OnRecordingStopped(finalCount)
                ErrorHandler.Instance.LogInfo($"Kayıt durduruldu: {finalCount} frame kaydedildi")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.StopRecording")
            End Try
        End Sub

        ''' <summary>
        ''' Kaydı duraklat
        ''' </summary>
        Public Sub PauseRecording()
            If Not IsRecording OrElse IsPaused Then
                Return
            End If

            IsPaused = True
            _totalRecordingDuration = _totalRecordingDuration.Add(DateTime.Now - _currentRecordingStart)
            FlushWriteBuffer()
        End Sub

        ''' <summary>
        ''' Kaydı devam ettir
        ''' </summary>
        Public Sub ResumeRecording()
            If Not IsRecording OrElse Not IsPaused Then
                Return
            End If

            IsPaused = False
            _currentRecordingStart = DateTime.Now
        End Sub

        ''' <summary>
        ''' Frame kaydet
        ''' </summary>
        Public Sub RecordFrame(frameId As Integer, data As Byte())
            If Not IsRecording OrElse IsPaused Then
                Return
            End If

            Try
                Dim frame As New RecordedFrame()
                frame.FrameId = CUShort(frameId And &HFFFF)
                frame.DLC = CByte(Math.Min(data.Length, 8))
                Array.Copy(data, frame.Data, frame.DLC)

                ' Relative time hesapla
                Dim elapsed = DateTime.Now - _recordStartTime
                frame.RelativeTimeMs = CUInt(elapsed.TotalMilliseconds)

                ' Buffer'a ekle
                SyncLock _recordLock
                    _writeBuffer.Add(frame)
                    _recordedFrameCount += 1

                    ' Buffer doluysa flush et
                    If _writeBuffer.Count >= 1000 Then
                        FlushWriteBuffer()
                    End If
                End SyncLock

                ' Her 100 frame'de bir event fırlat (performans için)
                If _recordedFrameCount Mod 100 = 0 Then
                    RaiseEvent OnFrameRecorded(_recordedFrameCount)
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.RecordFrame")
            End Try
        End Sub
#End Region

#Region "Playback Methods"
        ''' <summary>
        ''' Oturum dosyasını yükle
        ''' </summary>
        Public Sub LoadSession(filePath As String)
            If IsRecording Then
                Throw New InvalidOperationException("Kayıt devam ederken oturum yüklenemez")
            End If

            If _playbackState <> PlaybackState.Stopped Then
                StopPlayback()
            End If

            Try
                Me.FilePath = filePath
                _loadedFrames.Clear()

                ' Dosya uzantısına göre format belirle
                Dim extension = Path.GetExtension(filePath).ToLower()

                If extension = ".csv" Then
                    LoadCSVSession(filePath)
                Else
                    LoadBinarySession(filePath)
                End If

                _currentPlaybackFrame = 0
                ErrorHandler.Instance.LogInfo($"Oturum yüklendi: {filePath}, {_loadedFrames.Count} frame")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.LoadSession")
                RaiseEvent OnError($"Oturum yüklenemedi: {ex.Message}")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Oynatmayı başlat
        ''' </summary>
        Public Sub StartPlayback(speed As Double)
            If _loadedFrames.Count = 0 Then
                Throw New InvalidOperationException("Yüklenmiş oturum yok")
            End If

            If _playbackState = PlaybackState.Playing Then
                Return
            End If

            PlaybackSpeed = Math.Max(0.1, Math.Min(10.0, speed)) ' 0.1x - 10x arası sınırla

            If _playbackState = PlaybackState.Paused Then
                ' Devam ettir
                _playbackState = PlaybackState.Playing
                RaiseEvent OnPlaybackStarted()
            Else
                ' Yeni oynatma başlat
                _currentPlaybackFrame = 0
                _playbackState = PlaybackState.Playing
                _playbackCancellationToken = New CancellationTokenSource()

                _playbackTask = Task.Run(AddressOf PlaybackLoop, _playbackCancellationToken.Token)
                RaiseEvent OnPlaybackStarted()
            End If
        End Sub

        ''' <summary>
        ''' Oynatmayı duraklat
        ''' </summary>
        Public Sub PausePlayback()
            If _playbackState = PlaybackState.Playing Then
                _playbackState = PlaybackState.Paused
            End If
        End Sub

        ''' <summary>
        ''' Oynatmayı durdur
        ''' </summary>
        Public Sub StopPlayback()
            If _playbackState = PlaybackState.Stopped Then
                Return
            End If

            _playbackState = PlaybackState.Stopped

            If _playbackCancellationToken IsNot Nothing Then
                _playbackCancellationToken.Cancel()
            End If

            If _playbackTask IsNot Nothing Then
                Try
                    _playbackTask.Wait(1000) ' 1 saniye bekle
                Catch ex As AggregateException
                    ' Task cancelled, normal
                End Try
            End If

            _currentPlaybackFrame = 0
            RaiseEvent OnPlaybackStopped()
        End Sub

        ''' <summary>
        ''' Belirli frame'e atla
        ''' </summary>
        Public Sub SeekTo(frameNumber As Long)
            If frameNumber < 0 OrElse frameNumber >= _loadedFrames.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(frameNumber))
            End If

            Dim wasPlaying = (_playbackState = PlaybackState.Playing)

            If wasPlaying Then
                PausePlayback()
            End If

            _currentPlaybackFrame = frameNumber

            If wasPlaying Then
                StartPlayback(PlaybackSpeed)
            End If
        End Sub
#End Region

#Region "Export Methods"
        ''' <summary>
        ''' CSV formatına dışa aktar
        ''' </summary>
        Public Sub ExportToCSV(outputPath As String)
            If _loadedFrames.Count = 0 Then
                Throw New InvalidOperationException("Yüklenmiş oturum yok")
            End If

            Try
                Using writer As New StreamWriter(outputPath, False, Encoding.UTF8)
                    ' Header
                    writer.WriteLine("Time(ms),FrameID,DLC,Data")

                    ' Frames
                    For Each frame In _loadedFrames
                        Dim dataHex = BitConverter.ToString(frame.Data, 0, frame.DLC).Replace("-", " ")
                        writer.WriteLine($"{frame.RelativeTimeMs},{frame.FrameId:X3},{frame.DLC},{dataHex}")
                    Next
                End Using

                ErrorHandler.Instance.LogInfo($"CSV dışa aktarıldı: {outputPath}")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.ExportToCSV")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Vector CANalyzer ASC formatına dışa aktar
        ''' </summary>
        Public Sub ExportToASC(outputPath As String)
            If _loadedFrames.Count = 0 Then
                Throw New InvalidOperationException("Yüklenmiş oturum yok")
            End If

            Try
                Using writer As New StreamWriter(outputPath, False, Encoding.ASCII)
                    ' ASC Header
                    writer.WriteLine("date " & _sessionStartTime.ToString("ddd MMM dd hh:mm:ss tt yyyy"))
                    writer.WriteLine("base hex  timestamps absolute")
                    writer.WriteLine("internal events logged")
                    writer.WriteLine("Begin Triggerblock " & _sessionStartTime.ToString("ddd MMM dd hh:mm:ss.fff tt yyyy"))
                    writer.WriteLine("   0.000000 Start of measurement")

                    ' Frames
                    For Each frame In _loadedFrames
                        Dim timeSeconds = frame.RelativeTimeMs / 1000.0
                        Dim dataHex = BitConverter.ToString(frame.Data, 0, frame.DLC).Replace("-", " ")
                        writer.WriteLine($"  {timeSeconds:F6} 1  {frame.FrameId:X3}             Rx   d {frame.DLC} {dataHex}")
                    Next

                    writer.WriteLine("End TriggerBlock")
                End Using

                ErrorHandler.Instance.LogInfo($"ASC dışa aktarıldı: {outputPath}")

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.ExportToASC")
                Throw
            End Try
        End Sub
#End Region

#Region "Private Methods - Binary Format"
        ''' <summary>
        ''' Binary header yaz
        ''' </summary>
        Private Sub WriteBinaryHeader()
            ' Magic
            _recordWriter.Write(Encoding.ASCII.GetBytes(MAGIC_HEADER))

            ' Version
            _recordWriter.Write(FORMAT_VERSION)

            ' Start time (Unix timestamp)
            Dim unixTime = CLng((_recordStartTime.ToUniversalTime() - New DateTime(1970, 1, 1)).TotalSeconds)
            _recordWriter.Write(unixTime)

            ' Frame count (placeholder, güncellenecek)
            _recordWriter.Write(CLng(0))
        End Sub

        ''' <summary>
        ''' Binary header'ı güncelle (frame count)
        ''' </summary>
        Private Sub UpdateBinaryHeader()
            Try
                Dim currentPos = _recordStream.Position
                _recordStream.Seek(14, SeekOrigin.Begin) ' Magic(5) + Version(1) + StartTime(8) = 14
                _recordWriter.Write(_recordedFrameCount)
                _recordStream.Seek(currentPos, SeekOrigin.Begin)
            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.UpdateBinaryHeader")
            End Try
        End Sub

        ''' <summary>
        ''' Binary oturum yükle
        ''' </summary>
        Private Sub LoadBinarySession(filePath As String)
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using reader As New BinaryReader(fs, Encoding.UTF8)
                    ' Header oku
                    Dim magic = Encoding.ASCII.GetString(reader.ReadBytes(5))
                    If magic <> MAGIC_HEADER Then
                        Throw New InvalidDataException("Geçersiz dosya formatı")
                    End If

                    Dim version = reader.ReadByte()
                    If version <> FORMAT_VERSION Then
                        Throw New InvalidDataException($"Desteklenmeyen format versiyonu: {version}")
                    End If

                    Dim startTimeUnix = reader.ReadInt64()
                    _sessionStartTime = New DateTime(1970, 1, 1).AddSeconds(startTimeUnix).ToLocalTime()

                    Dim frameCount = reader.ReadInt64()

                    ' Frame'leri oku
                    For i As Long = 0 To frameCount - 1
                        Dim frame As New RecordedFrame()
                        frame.RelativeTimeMs = reader.ReadUInt32()
                        frame.FrameId = reader.ReadUInt16()
                        frame.DLC = reader.ReadByte()
                        frame.Data = reader.ReadBytes(8)

                        _loadedFrames.Add(frame)
                    Next
                End Using
            End Using
        End Sub
#End Region

#Region "Private Methods - CSV Format"
        ''' <summary>
        ''' CSV header yaz
        ''' </summary>
        Private Sub WriteCSVHeader()
            _csvWriter.WriteLine("Time(ms),FrameID,DLC,Data")
        End Sub

        ''' <summary>
        ''' CSV oturum yükle
        ''' </summary>
        Private Sub LoadCSVSession(filePath As String)
            Using reader As New StreamReader(filePath, Encoding.UTF8)
                ' Header'ı atla
                reader.ReadLine()

                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While

                    Dim parts = line.Split(","c)
                    If parts.Length < 4 Then Continue While

                    Try
                        Dim frame As New RecordedFrame()
                        frame.RelativeTimeMs = UInteger.Parse(parts(0).Trim())
                        frame.FrameId = UShort.Parse(parts(1).Trim(), Globalization.NumberStyles.HexNumber)
                        frame.DLC = Byte.Parse(parts(2).Trim())

                        ' Data parse et
                        Dim dataStr = parts(3).Trim()
                        Dim dataBytes = dataStr.Split(" "c)
                        For j = 0 To Math.Min(dataBytes.Length - 1, 7)
                            frame.Data(j) = Byte.Parse(dataBytes(j), Globalization.NumberStyles.HexNumber)
                        Next

                        _loadedFrames.Add(frame)

                    Catch ex As Exception
                        ' Geçersiz satırı atla
                        Continue While
                    End Try
                End While
            End Using
        End Sub
#End Region

#Region "Private Methods - Buffer Management"
        ''' <summary>
        ''' Write buffer'ını flush et
        ''' </summary>
        Private Sub FlushWriteBuffer()
            SyncLock _recordLock
                If _writeBuffer.Count = 0 Then
                    Return
                End If

                Try
                    If _recordFormat = RecordFormat.Binary Then
                        For Each frame In _writeBuffer
                            _recordWriter.Write(frame.RelativeTimeMs)
                            _recordWriter.Write(frame.FrameId)
                            _recordWriter.Write(frame.DLC)
                            _recordWriter.Write(frame.Data, 0, 8)
                        Next
                    Else ' CSV
                        For Each frame In _writeBuffer
                            Dim dataHex = BitConverter.ToString(frame.Data, 0, frame.DLC).Replace("-", " ")
                            _csvWriter.WriteLine($"{frame.RelativeTimeMs},{frame.FrameId:X3},{frame.DLC},{dataHex}")
                        Next
                    End If

                    _recordStream.Flush()
                    _writeBuffer.Clear()

                Catch ex As Exception
                    ErrorHandler.Instance.LogError(ex, "SessionRecorder.FlushWriteBuffer")
                End Try
            End SyncLock
        End Sub

        ''' <summary>
        ''' Periyodik flush timer callback
        ''' </summary>
        Private Sub FlushTimerCallback(state As Object)
            If IsRecording AndAlso Not IsPaused Then
                FlushWriteBuffer()
            End If
        End Sub
#End Region

#Region "Private Methods - Playback"
        ''' <summary>
        ''' Oynatma döngüsü
        ''' </summary>
        Private Sub PlaybackLoop()
            Try
                Dim lastFrameTime As UInteger = 0

                While _currentPlaybackFrame < _loadedFrames.Count AndAlso Not _playbackCancellationToken.Token.IsCancellationRequested
                    ' Duraklatma kontrolü
                    While _playbackState = PlaybackState.Paused AndAlso Not _playbackCancellationToken.Token.IsCancellationRequested
                        Thread.Sleep(50)
                    End While

                    If _playbackState <> PlaybackState.Playing Then
                        Exit While
                    End If

                    Dim frame = _loadedFrames(CInt(_currentPlaybackFrame))

                    ' Timing kontrolü
                    If _currentPlaybackFrame > 0 Then
                        Dim deltaMs = CInt((frame.RelativeTimeMs - lastFrameTime) / PlaybackSpeed)
                        If deltaMs > 0 Then
                            Thread.Sleep(deltaMs)
                        End If
                    End If

                    ' Frame event'i fırlat
                    RaiseEvent OnPlaybackFrame(frame.RelativeTimeMs, frame.FrameId, frame.Data)

                    lastFrameTime = frame.RelativeTimeMs
                    _currentPlaybackFrame += 1

                    ' Progress event (her 100 frame'de bir)
                    If _currentPlaybackFrame Mod 100 = 0 Then
                        RaiseEvent OnPlaybackProgress(_currentPlaybackFrame, _loadedFrames.Count)
                    End If
                End While

                ' Son progress event
                RaiseEvent OnPlaybackProgress(_currentPlaybackFrame, _loadedFrames.Count)

                ' Oynatma bitti
                _playbackState = PlaybackState.Stopped
                RaiseEvent OnPlaybackStopped()

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.PlaybackLoop")
                _playbackState = PlaybackState.Stopped
                RaiseEvent OnError($"Oynatma hatası: {ex.Message}")
            End Try
        End Sub
#End Region

#Region "Private Methods - Cleanup"
        ''' <summary>
        ''' Kaynakları temizle
        ''' </summary>
        Private Sub Cleanup()
            Try
                If _csvWriter IsNot Nothing Then
                    _csvWriter.Flush()
                    _csvWriter.Close()
                    _csvWriter = Nothing
                End If

                If _recordWriter IsNot Nothing Then
                    _recordWriter.Flush()
                    _recordWriter.Close()
                    _recordWriter = Nothing
                End If

                If _recordStream IsNot Nothing Then
                    _recordStream.Close()
                    _recordStream = Nothing
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "SessionRecorder.Cleanup")
            End Try
        End Sub
#End Region

#Region "IDisposable"
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    ' Managed kaynakları temizle
                    If IsRecording Then
                        StopRecording()
                    End If

                    If _playbackState <> PlaybackState.Stopped Then
                        StopPlayback()
                    End If

                    If _flushTimer IsNot Nothing Then
                        _flushTimer.Dispose()
                    End If

                    If _recordCancellationToken IsNot Nothing Then
                        _recordCancellationToken.Dispose()
                    End If

                    If _playbackCancellationToken IsNot Nothing Then
                        _playbackCancellationToken.Dispose()
                    End If

                    Cleanup()
                End If

                _disposed = True
            End If
        End Sub
#End Region
    End Class
End Namespace

