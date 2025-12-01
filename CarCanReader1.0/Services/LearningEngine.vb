' LearningEngine.vb
' Otomatik sinyal keşif ve byte değişim takip servisi
' MainForm ve eski Decoder/LearningEngine'den taşınan mantığı içerir
'
' Özellikler:
'   - Byte seviyesinde değişim tespiti
'   - Sinyal tipi sınıflandırma (On/Off, Artan, Azalan, Komut)
'   - Değişim istatistikleri
'   - JSON-ready komut çıktısı üretimi
'
' Kullanım:
'   Dim engine As New LearningEngine()
'   AddHandler engine.OnByteChange, Sub(id, idx, old, new) Console.WriteLine($"Change: {id}")
'   engine.Start()
'   engine.AnalyzeFrame(frameId, frameData)

Namespace Services

''' <summary>
''' Tespit edilen sinyal tiplerini tanımlar
''' </summary>
Public Enum SignalType
    ''' <summary>
    ''' Bilinmeyen veya sınıflandırılamayan sinyal
    ''' </summary>
    Unknown = 0

    ''' <summary>
    ''' Açma/Kapama toggle sinyali (0↔1)
    ''' </summary>
    OnOff = 1

    ''' <summary>
    ''' Artan değer sensörü (sürekli yükselen)
    ''' </summary>
    IncreasingSensor = 2

    ''' <summary>
    ''' Azalan değer sensörü (sürekli düşen)
    ''' </summary>
    DecreasingSensor = 3

    ''' <summary>
    ''' Komut patlaması (hızlı ardışık değişimler)
    ''' </summary>
    CommandBurst = 4

    ''' <summary>
    ''' Çoklu byte değişimi
    ''' </summary>
    MultiByte = 5

    ''' <summary>
    ''' Sabit sensör değeri (değişken ama yön yok)
    ''' </summary>
    Sensor = 6
End Enum

''' <summary>
''' Byte değişim bilgisini taşıyan yapı
''' </summary>
Public Class ByteChangeInfo
    Public Property Id As Integer
    Public Property ByteIndex As Integer
    Public Property OldValue As Byte
    Public Property NewValue As Byte
    Public Property SignalType As SignalType
    Public Property Timestamp As DateTime
    Public Property ChangeCount As Integer
    Public Property OldFrameHex As String
    Public Property NewFrameHex As String

    ''' <summary>
    ''' Değişimi okunabilir string olarak döndürür
    ''' </summary>
    Public Overrides Function ToString() As String
        Return $"ID 0x{Id:X3} Byte[{ByteIndex}] {OldValue:X2}→{NewValue:X2} ({GetSignalTypeName()})"
    End Function

    ''' <summary>
    ''' Sinyal tipinin Türkçe adını döndürür
    ''' </summary>
    Public Function GetSignalTypeName() As String
        Select Case SignalType
            Case SignalType.OnOff : Return "ON/OFF"
            Case SignalType.IncreasingSensor : Return "Artan Sensör"
            Case SignalType.DecreasingSensor : Return "Azalan Sensör"
            Case SignalType.CommandBurst : Return "Komut"
            Case SignalType.MultiByte : Return "Çoklu Byte"
            Case SignalType.Sensor : Return "Sensör"
            Case Else : Return "Bilinmeyen"
        End Select
    End Function

    ''' <summary>
    ''' Detaylı format string döndürür
    ''' </summary>
    Public Function ToDetailedString() As String
        Return $"ID 0x{Id:X3} | {GetSignalTypeName()} | Byte[{ByteIndex}] | {OldValue:X2}→{NewValue:X2} | FRAME {OldFrameHex} → {NewFrameHex}"
    End Function
End Class

''' <summary>
''' ID bazında değişim istatistiklerini tutar
''' </summary>
Public Class IdChangeStats
    Public Property Id As Integer
    Public Property TotalChangeCount As Integer = 0
    Public Property ByteChangeCounts As New Dictionary(Of Integer, Integer)()
    Public Property LastChangeTime As DateTime
    Public Property FirstSeenTime As DateTime
    Public Property FrameCount As Integer = 0
    Public Property DetectedSignalTypes As New Dictionary(Of Integer, SignalType)()
End Class

''' <summary>
''' AI-Finder: Sınıflandırılmış sinyal modeli
''' MainForm event handler'ları için kullanılır
''' </summary>
Public Class ClassifiedSignal
    Public Property CanId As Integer
    Public Property ByteIndex As Integer
    Public Property SignalType As SignalType
    Public Property OldValue As Byte
    Public Property NewValue As Byte
    Public Property Confidence As Double
    Public Property Category As String = "Unknown"
    Public Property DetectedAt As DateTime = DateTime.Now

    Public Overrides Function ToString() As String
        Return $"ID 0x{CanId:X3} Byte[{ByteIndex}] → {SignalType} ({Confidence:P0})"
    End Function
End Class

''' <summary>
''' AI-Finder: Komut tahmini modeli
''' MainForm event handler'ları için kullanılır
''' </summary>
Public Class CommandPrediction
    Public Property CanId As Integer
    Public Property ByteIndex As Integer
    Public Property PredictedName As String = ""
    Public Property Category As String = "Unknown"
    Public Property FramePattern As String = ""
    Public Property OnFrame As String = ""
    Public Property OffFrame As String = ""
    Public Property IsToggle As Boolean = False
    Public Property Confidence As Double
    Public Property Source As String = "ai_finder"
    Public Property DetectedAt As DateTime = DateTime.Now

    Public Overrides Function ToString() As String
        Return $"{PredictedName} @ ID 0x{CanId:X3} - {Category} ({Confidence:P0})"
    End Function
End Class

''' <summary>
''' AI-Finder: Tespit edilen pattern modeli
''' MainForm event handler'ları için kullanılır
''' </summary>
Public Class DetectedPattern
    Public Property CanId As Integer
    Public Property ByteIndex As Integer
    Public Property PatternType As String = ""  ' "Toggle", "Linear", "Cyclic", "Burst"
    Public Property Description As String = ""
    Public Property SampleCount As Integer
    Public Property Confidence As Double
    Public Property DetectedAt As DateTime = DateTime.Now

    Public Overrides Function ToString() As String
        Return $"Pattern: {PatternType} @ ID 0x{CanId:X3} ({Confidence:P0})"
    End Function
End Class

''' <summary>
''' Otomatik sinyal keşif ve byte değişim takip motoru
''' </summary>
Public Class LearningEngine

#Region "Olaylar (Events)"

    ''' <summary>
    ''' Byte değişimi tespit edildiğinde tetiklenir
    ''' </summary>
    Public Event OnByteChange(id As Integer, byteIndex As Integer, oldValue As Byte, newValue As Byte)

    ''' <summary>
    ''' Sinyal tipi sınıflandırıldığında tetiklenir
    ''' </summary>
    Public Event OnSignalClassified(id As Integer, byteIndex As Integer, signalType As SignalType)

    ''' <summary>
    ''' Detaylı değişim bilgisi ile tetiklenir
    ''' </summary>
    Public Event OnChangeDetected(changeInfo As ByteChangeInfo)

    ''' <summary>
    ''' Motor başlatıldığında tetiklenir
    ''' </summary>
    Public Event OnStarted()

    ''' <summary>
    ''' Motor durdurulduğunda tetiklenir
    ''' </summary>
    Public Event OnStopped()

    ''' <summary>
    ''' AI-Finder: Sinyal sınıflandırıldığında (model ile) tetiklenir
    ''' </summary>
    Public Event OnSignalClassifiedEx(signal As ClassifiedSignal)

    ''' <summary>
    ''' AI-Finder: Komut tahmini yapıldığında tetiklenir
    ''' </summary>
    Public Event OnCommandPredicted(prediction As CommandPrediction)

    ''' <summary>
    ''' AI-Finder: Pattern tespit edildiğinde tetiklenir
    ''' </summary>
    Public Event OnPatternDetected(pattern As DetectedPattern)

#End Region

#Region "Özel Alanlar"

    ' Motorun çalışma durumu
    Private _isRunning As Boolean = False

    ' Son alınan frame'lerin geçmişi (ID → byte dizisi)
    Private _lastFrames As New Dictionary(Of Integer, Byte())()

    ' ID bazında istatistikler
    Private _idStats As New Dictionary(Of Integer, IdChangeStats)()

    ' Değişim geçmişi (son N değişim)
    Private _changeHistory As New List(Of ByteChangeInfo)()

    ' Byte bazında değişim yönü takibi (artan/azalan tespiti için)
    ' Key: "ID_ByteIndex", Value: son değişim yönleri listesi
    Private _byteDirectionHistory As New Dictionary(Of String, List(Of Integer))()

#End Region

#Region "Sabitler"

    ' Maksimum değişim geçmişi boyutu
    Private Const MAX_CHANGE_HISTORY As Integer = 1000

    ' Sinyal tipi tespiti için minimum örnek sayısı
    Private Const MIN_SAMPLES_FOR_CLASSIFICATION As Integer = 5

    ' Yön geçmişi boyutu
    Private Const DIRECTION_HISTORY_SIZE As Integer = 10

#End Region

#Region "Özellikler (Properties)"

    ''' <summary>
    ''' Motorun çalışıp çalışmadığını döndürür
    ''' </summary>
    Public ReadOnly Property IsRunning As Boolean
        Get
            Return _isRunning
        End Get
    End Property

    ''' <summary>
    ''' Takip edilen benzersiz ID sayısı
    ''' </summary>
    Public ReadOnly Property TrackedIdCount As Integer
        Get
            Return _lastFrames.Count
        End Get
    End Property

    ''' <summary>
    ''' Toplam tespit edilen değişim sayısı
    ''' </summary>
    Public ReadOnly Property TotalChangeCount As Integer
        Get
            Return _changeHistory.Count
        End Get
    End Property

#End Region

#Region "Ana Kontrol Metodları"

    ''' <summary>
    ''' Learning Engine'i başlatır
    ''' </summary>
    Public Sub Start()
        _isRunning = True
        RaiseEvent OnStarted()
    End Sub

    ''' <summary>
    ''' Learning Engine'i durdurur
    ''' </summary>
    Public Sub [Stop]()
        _isRunning = False
        RaiseEvent OnStopped()
    End Sub

    ''' <summary>
    ''' Tüm verileri sıfırlar ve motoru durdurur
    ''' </summary>
    Public Sub Reset()
        [Stop]()
        _lastFrames.Clear()
        _idStats.Clear()
        _changeHistory.Clear()
        _byteDirectionHistory.Clear()
    End Sub

#End Region

#Region "Ana Analiz Metodu"

    ''' <summary>
    ''' Gelen CAN frame'i analiz eder ve değişimleri tespit eder
    ''' </summary>
    ''' <param name="id">CAN ID</param>
    ''' <param name="data">Frame data baytları</param>
    Public Sub AnalyzeFrame(id As Integer, data() As Byte)
        ' Motor çalışmıyorsa çık
        If Not _isRunning Then Return

        ' Null veya boş data kontrolü
        If data Is Nothing OrElse data.Length = 0 Then Return

        ' ID istatistiklerini güncelle
        EnsureIdStats(id)
        _idStats(id).FrameCount += 1

        ' İlk defa görülen ID ise sadece kaydet ve çık
        If Not _lastFrames.ContainsKey(id) Then
            _lastFrames(id) = CloneByteArray(data)
            _idStats(id).FirstSeenTime = DateTime.Now
            Return
        End If

        ' Önceki frame'i al
        Dim oldBytes As Byte() = _lastFrames(id)

        ' Byte karşılaştırması yap
        Dim changes As List(Of ByteChangeInfo) = CompareFrames(id, oldBytes, data)

        ' Değişim varsa işle
        For Each change In changes
            ' İstatistikleri güncelle
            UpdateStats(change)

            ' Sinyal tipini sınıflandır
            change.SignalType = ClassifySignal(change)

            ' Geçmişe ekle
            AddToChangeHistory(change)

            ' Olayları tetikle
            RaiseEvent OnByteChange(change.Id, change.ByteIndex, change.OldValue, change.NewValue)
            RaiseEvent OnSignalClassified(change.Id, change.ByteIndex, change.SignalType)
            RaiseEvent OnChangeDetected(change)

            ' AI-Finder: Enhanced events with model objects
            Dim classifiedSignal As New ClassifiedSignal()
            classifiedSignal.CanId = change.Id
            classifiedSignal.ByteIndex = change.ByteIndex
            classifiedSignal.SignalType = change.SignalType
            classifiedSignal.OldValue = change.OldValue
            classifiedSignal.NewValue = change.NewValue
            classifiedSignal.Confidence = CalculateSignalConfidence(change)
            classifiedSignal.Category = AutoClassifyByIdAndPattern(change.Id, change.ByteIndex, change.OldValue, change.NewValue)
            RaiseEvent OnSignalClassifiedEx(classifiedSignal)

            ' Check for pattern detection
            If change.SignalType = SignalType.OnOff Then
                Dim pattern As New DetectedPattern()
                pattern.CanId = change.Id
                pattern.ByteIndex = change.ByteIndex
                pattern.PatternType = "Toggle"
                pattern.Description = $"ON/OFF toggle detected at Byte[{change.ByteIndex}]"
                pattern.Confidence = 0.85
                RaiseEvent OnPatternDetected(pattern)
            End If
        Next

        ' Son frame'i güncelle
        _lastFrames(id) = CloneByteArray(data)
    End Sub

#End Region

#Region "Frame Karşılaştırma"

    ''' <summary>
    ''' İki frame'i karşılaştırır ve değişimleri listeler
    ''' </summary>
    Private Function CompareFrames(id As Integer, oldBytes() As Byte, newBytes() As Byte) As List(Of ByteChangeInfo)
        Dim changes As New List(Of ByteChangeInfo)()

        ' Minimum uzunluk
        Dim minLen As Integer = Math.Min(oldBytes.Length, newBytes.Length)

        ' Her byte'ı karşılaştır
        For i As Integer = 0 To minLen - 1
            If oldBytes(i) <> newBytes(i) Then
                Dim change As New ByteChangeInfo()
                change.Id = id
                change.ByteIndex = i
                change.OldValue = oldBytes(i)
                change.NewValue = newBytes(i)
                change.Timestamp = DateTime.Now
                change.OldFrameHex = BytesToHexString(oldBytes)
                change.NewFrameHex = BytesToHexString(newBytes)
                changes.Add(change)
            End If
        Next

        ' Toplam değişim sayısını kaydet
        For Each change In changes
            change.ChangeCount = changes.Count
        Next

        Return changes
    End Function

#End Region

#Region "Sinyal Sınıflandırma"

    ''' <summary>
    ''' Değişimi analiz ederek sinyal tipini belirler
    ''' </summary>
    Private Function ClassifySignal(change As ByteChangeInfo) As SignalType
        Dim oldVal As Byte = change.OldValue
        Dim newVal As Byte = change.NewValue

        ' ON/OFF Toggle tespiti (0↔1)
        If (oldVal = 0 AndAlso newVal = 1) OrElse (oldVal = 1 AndAlso newVal = 0) Then
            Return SignalType.OnOff
        End If

        ' Çoklu byte değişimi kontrolü
        If change.ChangeCount > 1 Then
            Return SignalType.MultiByte
        End If

        ' Yön geçmişini güncelle ve analiz et
        Dim directionKey As String = $"{change.Id}_{change.ByteIndex}"
        Dim direction As Integer = Math.Sign(CInt(newVal) - CInt(oldVal))

        UpdateDirectionHistory(directionKey, direction)

        ' Yeterli örnek varsa yön analizi yap
        If _byteDirectionHistory.ContainsKey(directionKey) Then
            Dim dirHistory = _byteDirectionHistory(directionKey)

            If dirHistory.Count >= MIN_SAMPLES_FOR_CLASSIFICATION Then
                ' Tüm yönler aynı mı kontrol et
                Dim allPositive As Boolean = dirHistory.All(Function(d) d > 0)
                Dim allNegative As Boolean = dirHistory.All(Function(d) d < 0)

                If allPositive Then
                    Return SignalType.IncreasingSensor
                ElseIf allNegative Then
                    Return SignalType.DecreasingSensor
                End If
            End If
        End If

        ' Büyük değişim = muhtemelen komut
        Dim diff As Integer = Math.Abs(CInt(newVal) - CInt(oldVal))
        If diff > 100 Then
            Return SignalType.CommandBurst
        End If

        ' Genel sensör değişimi
        If newVal > oldVal Then
            Return SignalType.IncreasingSensor
        ElseIf newVal < oldVal Then
            Return SignalType.DecreasingSensor
        End If

        Return SignalType.Sensor
    End Function

    ''' <summary>
    ''' Sinyal güvenilirlik skoru hesaplar (0.0 - 1.0)
    ''' </summary>
    Private Function CalculateSignalConfidence(change As ByteChangeInfo) As Double
        Dim confidence As Double = 0.5

        ' ON/OFF toggle yüksek güvenilirlik
        If change.SignalType = SignalType.OnOff Then
            confidence += 0.35
        End If

        ' Tek byte değişimi
        If change.ChangeCount = 1 Then
            confidence += 0.1
        End If

        ' Sensör sinyalleri
        If change.SignalType = SignalType.IncreasingSensor OrElse
           change.SignalType = SignalType.DecreasingSensor Then
            confidence += 0.2
        End If

        Return Math.Min(1.0, confidence)
    End Function

    ''' <summary>
    ''' Yön geçmişini günceller
    ''' </summary>
    Private Sub UpdateDirectionHistory(key As String, direction As Integer)
        If Not _byteDirectionHistory.ContainsKey(key) Then
            _byteDirectionHistory(key) = New List(Of Integer)()
        End If

        _byteDirectionHistory(key).Add(direction)

        ' Maksimum boyutu aşarsa eski değerleri sil
        While _byteDirectionHistory(key).Count > DIRECTION_HISTORY_SIZE
            _byteDirectionHistory(key).RemoveAt(0)
        End While
    End Sub

#End Region

#Region "İstatistik Yönetimi"

    ''' <summary>
    ''' ID için istatistik kaydının var olmasını sağlar
    ''' </summary>
    Private Sub EnsureIdStats(id As Integer)
        If Not _idStats.ContainsKey(id) Then
            Dim stats As New IdChangeStats()
            stats.Id = id
            stats.FirstSeenTime = DateTime.Now
            _idStats(id) = stats
        End If
    End Sub

    ''' <summary>
    ''' Değişim istatistiklerini günceller
    ''' </summary>
    Private Sub UpdateStats(change As ByteChangeInfo)
        Dim stats = _idStats(change.Id)

        ' Toplam değişim sayısı
        stats.TotalChangeCount += 1

        ' Byte bazında değişim sayısı
        If Not stats.ByteChangeCounts.ContainsKey(change.ByteIndex) Then
            stats.ByteChangeCounts(change.ByteIndex) = 0
        End If
        stats.ByteChangeCounts(change.ByteIndex) += 1

        ' Son değişim zamanı
        stats.LastChangeTime = DateTime.Now

        ' Tespit edilen sinyal tipi (en son tespit edilen)
        stats.DetectedSignalTypes(change.ByteIndex) = change.SignalType
    End Sub

    ''' <summary>
    ''' Değişim geçmişine ekler
    ''' </summary>
    Private Sub AddToChangeHistory(change As ByteChangeInfo)
        _changeHistory.Add(change)

        ' Maksimum boyutu aşarsa eski değerleri sil
        While _changeHistory.Count > MAX_CHANGE_HISTORY
            _changeHistory.RemoveAt(0)
        End While
    End Sub

#End Region

#Region "Veri Erişim Metodları"

    ''' <summary>
    ''' En çok değişen ID'leri döndürür
    ''' </summary>
    Public Function GetTopChangedIds(Optional limit As Integer = 20) As List(Of IdChangeStats)
        Return _idStats.Values.
            OrderByDescending(Function(x) x.TotalChangeCount).
            Take(limit).
            ToList()
    End Function

    ''' <summary>
    ''' Belirli ID'nin istatistiklerini döndürür
    ''' </summary>
    Public Function GetIdStats(id As Integer) As IdChangeStats
        If _idStats.ContainsKey(id) Then
            Return _idStats(id)
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Son N değişimi döndürür
    ''' </summary>
    Public Function GetRecentChanges(Optional count As Integer = 50) As List(Of ByteChangeInfo)
        Return _changeHistory.
            Skip(Math.Max(0, _changeHistory.Count - count)).
            ToList()
    End Function

    ''' <summary>
    ''' Tüm değişim geçmişini döndürür
    ''' </summary>
    Public Function GetAllChanges() As List(Of ByteChangeInfo)
        Return _changeHistory.ToList()
    End Function

    ''' <summary>
    ''' Belirli ID için tespit edilen sinyal tiplerini döndürür
    ''' </summary>
    Public Function GetDetectedSignalTypes(id As Integer) As Dictionary(Of Integer, SignalType)
        If _idStats.ContainsKey(id) Then
            Return _idStats(id).DetectedSignalTypes
        End If
        Return New Dictionary(Of Integer, SignalType)()
    End Function

#End Region

#Region "JSON Export"

    ''' <summary>
    ''' Tespit edilen sinyalleri JSON-ready komut formatında döndürür
    ''' </summary>
    Public Function GenerateCommandSuggestions() As List(Of Dictionary(Of String, Object))
        Dim suggestions As New List(Of Dictionary(Of String, Object))()

        For Each kvp In _idStats
            Dim id = kvp.Key
            Dim stats = kvp.Value

            For Each byteKvp In stats.DetectedSignalTypes
                Dim byteIndex = byteKvp.Key
                Dim signalType = byteKvp.Value

                If signalType = SignalType.OnOff Then
                    ' Toggle komutu önerisi
                    Dim cmd As New Dictionary(Of String, Object)()
                    cmd("type") = "toggle"
                    cmd("id") = id.ToString("X3")
                    cmd("byte") = byteIndex
                    cmd("description") = $"Auto-detected ON/OFF at ID 0x{id:X3} Byte[{byteIndex}]"
                    suggestions.Add(cmd)
                End If
            Next
        Next

        Return suggestions
    End Function

#End Region

#Region "Yardımcı Metodlar"

    ''' <summary>
    ''' Byte dizisini kopyalar
    ''' </summary>
    Private Function CloneByteArray(source() As Byte) As Byte()
        If source Is Nothing Then Return Nothing

        Dim copy(source.Length - 1) As Byte
        Array.Copy(source, copy, source.Length)
        Return copy
    End Function

    ''' <summary>
    ''' Byte dizisini hex string'e çevirir
    ''' </summary>
    Private Function BytesToHexString(data() As Byte) As String
        If data Is Nothing OrElse data.Length = 0 Then Return ""

        Dim sb As New System.Text.StringBuilder()
        For i As Integer = 0 To data.Length - 1
            If i > 0 Then sb.Append(" ")
            sb.Append(data(i).ToString("X2"))
        Next
        Return sb.ToString()
    End Function

#End Region

#Region "Bit Analizi"

    ''' <summary>
    ''' İki byte arasındaki bit farklarını analiz eder
    ''' </summary>
    Public Function AnalyzeBitChanges(oldVal As Byte, newVal As Byte) As String
        Dim xorResult As Byte = oldVal Xor newVal
        Dim changedBits As New List(Of Integer)()

        For i As Integer = 0 To 7
            If (xorResult And (1 << i)) <> 0 Then
                changedBits.Add(i)
            End If
        Next

        If changedBits.Count = 0 Then
            Return "Değişim yok"
        End If

        Dim bitsStr As String = String.Join(",", changedBits)
        Return $"Bit[{bitsStr}] değişti | {Convert.ToString(oldVal, 2).PadLeft(8, "0"c)} → {Convert.ToString(newVal, 2).PadLeft(8, "0"c)}"
    End Function

#End Region

#Region "Durum Raporu"

    ''' <summary>
    ''' Motor durumunu özetleyen rapor döndürür
    ''' </summary>
    Public Function GetStatusReport() As String
        Dim sb As New System.Text.StringBuilder()

        sb.AppendLine("=== Learning Engine Durum Raporu ===")
        sb.AppendLine($"Çalışıyor: {If(_isRunning, "Evet", "Hayır")}")
        sb.AppendLine($"Takip edilen ID sayısı: {_lastFrames.Count}")
        sb.AppendLine($"Toplam değişim: {_changeHistory.Count}")
        sb.AppendLine()

        If _idStats.Count > 0 Then
            sb.AppendLine("En aktif ID'ler:")
            For Each stats In GetTopChangedIds(5)
                sb.AppendLine($"  ID 0x{stats.Id:X3}: {stats.TotalChangeCount} değişim")
            Next
        End If

        Return sb.ToString()
    End Function

#End Region

#Region "AI-Finder: Snapshot Yönetimi"

    ' Aksiyon öncesi/sonrası snapshot'ları
    Private _snapshotBefore As New Dictionary(Of Integer, Byte())
    Private _snapshotAfter As New Dictionary(Of Integer, Byte())
    Private _snapshotBeforeTime As DateTime = DateTime.MinValue
    Private _snapshotAfterTime As DateTime = DateTime.MinValue

    ''' <summary>
    ''' Kullanıcı aksiyonu öncesi snapshot yakalar
    ''' </summary>
    Public Sub CaptureSnapshotBefore()
        SyncLock _snapshotBefore
            _snapshotBefore.Clear()
            For Each kvp In _lastFrames
                _snapshotBefore(kvp.Key) = CloneByteArray(kvp.Value)
            Next
            _snapshotBeforeTime = DateTime.Now
        End SyncLock
    End Sub

    ''' <summary>
    ''' Kullanıcı aksiyonu sonrası snapshot yakalar
    ''' </summary>
    Public Sub CaptureSnapshotAfter()
        SyncLock _snapshotAfter
            _snapshotAfter.Clear()
            For Each kvp In _lastFrames
                _snapshotAfter(kvp.Key) = CloneByteArray(kvp.Value)
            Next
            _snapshotAfterTime = DateTime.Now
        End SyncLock
    End Sub

    ''' <summary>
    ''' Snapshot verilerini temizler
    ''' </summary>
    Public Sub ClearSnapshots()
        SyncLock _snapshotBefore
            _snapshotBefore.Clear()
            _snapshotAfter.Clear()
            _snapshotBeforeTime = DateTime.MinValue
            _snapshotAfterTime = DateTime.MinValue
        End SyncLock
    End Sub

    ''' <summary>
    ''' Snapshot hazır mı kontrol eder
    ''' </summary>
    Public Function AreSnapshotsReady() As Boolean
        Return _snapshotBefore.Count > 0 AndAlso _snapshotAfter.Count > 0
    End Function

#End Region

#Region "AI-Finder: Auto-Diff"

    ''' <summary>
    ''' Tahmin edilen komut yapısı
    ''' </summary>
    Public Class PredictedCommand
        Public Property Id As Integer
        Public Property IdHex As String
        Public Property ByteIndex As Integer
        Public Property OldValue As Byte
        Public Property NewValue As Byte
        Public Property ByteMask As String
        Public Property Description As String
        Public Property Category As String
        Public Property CommandType As String ' "single", "toggle"
        Public Property OnFrame As String
        Public Property OffFrame As String
        Public Property Confidence As Integer
        Public Property DetectedAt As DateTime = DateTime.Now

        Public Overrides Function ToString() As String
            Return $"[{Category}] ID 0x{IdHex} Byte[{ByteIndex}] - {Description} ({Confidence}%)"
        End Function
    End Class

    ''' <summary>
    ''' Öncesi/sonrası snapshot'ları karşılaştırarak farklılıkları bulur
    ''' </summary>
    ''' <param name="noiseThreshold">Minimum değişim sayısı eşiği (gürültü filtreleme)</param>
    Public Function AutoDiff(Optional noiseThreshold As Integer = 1) As List(Of PredictedCommand)
        Dim predictions As New List(Of PredictedCommand)

        Try
            If Not AreSnapshotsReady() Then
                Return predictions
            End If

            SyncLock _snapshotBefore
                ' Her iki snapshot'ta da olan ID'leri karşılaştır
                For Each kvp In _snapshotBefore
                    Dim id As Integer = kvp.Key
                    Dim beforeData As Byte() = kvp.Value

                    If Not _snapshotAfter.ContainsKey(id) Then Continue For

                    Dim afterData As Byte() = _snapshotAfter(id)

                    ' Byte seviyesinde karşılaştır
                    Dim minLen As Integer = Math.Min(beforeData.Length, afterData.Length)
                    Dim changedBytes As New List(Of Integer)

                    For i As Integer = 0 To minLen - 1
                        If beforeData(i) <> afterData(i) Then
                            changedBytes.Add(i)
                        End If
                    Next

                    ' Gürültü filtreleme
                    If changedBytes.Count >= noiseThreshold AndAlso changedBytes.Count <= 4 Then
                        For Each byteIdx In changedBytes
                            Dim pred As New PredictedCommand()
                            pred.Id = id
                            pred.IdHex = id.ToString("X3")
                            pred.ByteIndex = byteIdx
                            pred.OldValue = beforeData(byteIdx)
                            pred.NewValue = afterData(byteIdx)
                            pred.CommandType = If(IsTogglePattern(beforeData(byteIdx), afterData(byteIdx)), "toggle", "single")
                            pred.Confidence = CalculateConfidence(changedBytes.Count, beforeData(byteIdx), afterData(byteIdx))
                            pred.Category = AutoClassifyByIdAndPattern(id, byteIdx, beforeData(byteIdx), afterData(byteIdx))
                            pred.Description = GenerateDescription(pred)

                            ' Frame string'leri oluştur
                            pred.OnFrame = BuildSlcanFrame(id, afterData)
                            pred.OffFrame = BuildSlcanFrame(id, beforeData)

                            predictions.Add(pred)

                            ' AI-Finder: Raise CommandPrediction event
                            Dim cmdPred As New CommandPrediction()
                            cmdPred.CanId = id
                            cmdPred.ByteIndex = byteIdx
                            cmdPred.PredictedName = $"AutoCmd_{id:X3}_{byteIdx}"
                            cmdPred.Category = pred.Category
                            cmdPred.FramePattern = pred.OnFrame
                            cmdPred.OnFrame = pred.OnFrame
                            cmdPred.OffFrame = pred.OffFrame
                            cmdPred.IsToggle = (pred.CommandType = "toggle")
                            cmdPred.Confidence = pred.Confidence / 100.0 ' Convert to 0-1 range
                            RaiseEvent OnCommandPredicted(cmdPred)
                        Next
                    End If
                Next
            End SyncLock

            ' Güvenilirliğe göre sırala
            predictions = predictions.OrderByDescending(Function(p) p.Confidence).ToList()

        Catch ex As Exception
            ' Hata durumunda boş liste döndür
            predictions.Clear()
        End Try

        Return predictions
    End Function

    ''' <summary>
    ''' SLCAN frame string oluşturur
    ''' </summary>
    Private Function BuildSlcanFrame(id As Integer, data As Byte()) As String
        If data Is Nothing OrElse data.Length = 0 Then Return ""

        Dim idHex As String = id.ToString("X3")
        Dim dlc As Integer = data.Length
        Dim dataHex As String = BytesToHexString(data).Replace(" ", "")

        Return "t" & idHex & dlc.ToString() & dataHex
    End Function

    ''' <summary>
    ''' Toggle pattern olup olmadığını kontrol eder
    ''' </summary>
    Private Function IsTogglePattern(oldVal As Byte, newVal As Byte) As Boolean
        ' Klasik 0↔1 toggle
        If (oldVal = 0 AndAlso newVal = 1) OrElse (oldVal = 1 AndAlso newVal = 0) Then
            Return True
        End If

        ' 0x00↔0xFF toggle
        If (oldVal = 0 AndAlso newVal = &HFF) OrElse (oldVal = &HFF AndAlso newVal = 0) Then
            Return True
        End If

        ' Bit-level toggle (tek bit değişimi)
        Dim xorVal As Integer = oldVal Xor newVal
        If IsPowerOfTwo(xorVal) Then
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' 2'nin kuvveti mi kontrol eder
    ''' </summary>
    Private Function IsPowerOfTwo(n As Integer) As Boolean
        Return n > 0 AndAlso (n And (n - 1)) = 0
    End Function

    ''' <summary>
    ''' Güvenilirlik skoru hesaplar
    ''' </summary>
    Private Function CalculateConfidence(changedByteCount As Integer, oldVal As Byte, newVal As Byte) As Integer
        Dim confidence As Integer = 50

        ' Tek byte değişimi yüksek güvenilirlik
        If changedByteCount = 1 Then
            confidence += 30
        ElseIf changedByteCount = 2 Then
            confidence += 15
        End If

        ' Toggle pattern bonus
        If IsTogglePattern(oldVal, newVal) Then
            confidence += 15
        End If

        ' Temiz değerler bonus (0x00, 0x01, 0xFF)
        If oldVal = 0 OrElse oldVal = 1 OrElse oldVal = &HFF Then
            confidence += 5
        End If
        If newVal = 0 OrElse newVal = 1 OrElse newVal = &HFF Then
            confidence += 5
        End If

        Return Math.Min(100, confidence)
    End Function

#End Region

#Region "AI-Finder: Pattern Detection"

    ''' <summary>
    ''' Toggle pattern tespit eder (belirli ID ve byte için)
    ''' </summary>
    Public Function DetectTogglePattern(id As Integer, byteIndex As Integer) As Boolean
        Try
            Dim key As String = $"{id}_{byteIndex}"

            If Not _byteDirectionHistory.ContainsKey(key) Then
                Return False
            End If

            Dim history = _byteDirectionHistory(key)

            ' En az 4 değişim olmalı
            If history.Count < 4 Then Return False

            ' Yön değişimleri sayısını kontrol et
            Dim directionChanges As Integer = 0
            For i As Integer = 1 To history.Count - 1
                If Math.Sign(history(i)) <> Math.Sign(history(i - 1)) Then
                    directionChanges += 1
                End If
            Next

            ' Sık yön değişimi = toggle pattern
            Return directionChanges >= history.Count * 0.4

        Catch ex As Exception
            Debug.WriteLine($"DetectTogglePattern error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Linear pattern tespit eder (sürekli artan/azalan)
    ''' </summary>
    Public Function DetectLinearPattern(id As Integer, byteIndex As Integer) As SignalType
        Try
            Dim key As String = $"{id}_{byteIndex}"

            If Not _byteDirectionHistory.ContainsKey(key) Then
                Return SignalType.Unknown
            End If

            Dim history = _byteDirectionHistory(key)

            If history.Count < MIN_SAMPLES_FOR_CLASSIFICATION Then
                Return SignalType.Unknown
            End If

            ' Tüm yönler aynı mı?
            Dim allPositive As Boolean = history.All(Function(d) d > 0)
            Dim allNegative As Boolean = history.All(Function(d) d < 0)

            If allPositive Then
                Return SignalType.IncreasingSensor
            ElseIf allNegative Then
                Return SignalType.DecreasingSensor
            End If

            Return SignalType.Sensor

        Catch ex As Exception
            Debug.WriteLine($"DetectLinearPattern error: {ex.Message}")
            Return SignalType.Unknown
        End Try
    End Function

    ''' <summary>
    ''' Multi-byte pattern tespit eder (RPM, Speed gibi 2-byte değerler)
    ''' </summary>
    Public Function DetectMultiBytePattern(id As Integer) As Boolean
        Try
            If Not _idStats.ContainsKey(id) Then Return False

            Dim stats = _idStats(id)
            Dim changingBytes = stats.ByteChangeCounts.Where(Function(kvp) kvp.Value > 10).ToList()

            ' Ardışık byte'lar mı?
            If changingBytes.Count >= 2 Then
                changingBytes = changingBytes.OrderBy(Function(kvp) kvp.Key).ToList()

                For i As Integer = 0 To changingBytes.Count - 2
                    If changingBytes(i + 1).Key - changingBytes(i).Key = 1 Then
                        Return True ' Ardışık byte'lar bulundu
                    End If
                Next
            End If

            Return False

        Catch ex As Exception
            Debug.WriteLine($"DetectMultiBytePattern error: {ex.Message}")
            Return False
        End Try
    End Function

#End Region

#Region "AI-Finder: Auto-Classification"

    ''' <summary>
    ''' Komut kategorileri
    ''' </summary>
    Public Enum CommandCategory
        Unknown = 0
        Lighting = 1
        Doors = 2
        Windows = 3
        Mirrors = 4
        Cluster = 5
        HVAC = 6
        Multimedia = 7
        BodyControl = 8
        Engine = 9
        Transmission = 10
        Suspension = 11
        Brakes = 12
        Steering = 13
        Safety = 14
        Seats = 15
    End Enum

    ''' <summary>
    ''' ID ve pattern'e göre otomatik kategori tahmin eder
    ''' </summary>
    Private Function AutoClassifyByIdAndPattern(id As Integer, byteIndex As Integer, oldVal As Byte, newVal As Byte) As String
        ' Yaygın CAN ID aralıklarına göre kategori tahmini
        ' Bu değerler araç tipine göre değişebilir

        ' Lighting genellikle 0x100-0x200 aralığında
        If id >= &H100 AndAlso id <= &H200 Then
            Return "Lighting"
        End If

        ' Doors genellikle 0x400-0x500 aralığında
        If id >= &H400 AndAlso id <= &H500 Then
            Return "Doors"
        End If

        ' Cluster genellikle 0x300-0x400 aralığında
        If id >= &H300 AndAlso id <= &H400 Then
            Return "Cluster"
        End If

        ' HVAC genellikle 0x500-0x600 aralığında
        If id >= &H500 AndAlso id <= &H600 Then
            Return "HVAC"
        End If

        ' Toggle pattern için ek kategori tahmini
        If IsTogglePattern(oldVal, newVal) Then
            ' Tek bit toggle genellikle lighting veya doors
            Dim xorVal As Integer = oldVal Xor newVal
            If IsPowerOfTwo(xorVal) Then
                Return "BodyControl"
            End If
        End If

        Return "Unknown"
    End Function

    ''' <summary>
    ''' Kategori adını Türkçe'ye çevirir
    ''' </summary>
    Public Function GetCategoryDisplayName(category As String) As String
        Select Case category.ToLower()
            Case "lighting" : Return "Aydınlatma"
            Case "doors" : Return "Kapılar"
            Case "windows" : Return "Camlar"
            Case "mirrors" : Return "Aynalar"
            Case "cluster" : Return "Gösterge Paneli"
            Case "hvac" : Return "Klima/HVAC"
            Case "multimedia" : Return "Multimedya"
            Case "bodycontrol" : Return "Gövde Kontrol"
            Case "engine" : Return "Motor"
            Case "transmission" : Return "Şanzıman"
            Case "suspension" : Return "Süspansiyon"
            Case "brakes" : Return "Frenler"
            Case "steering" : Return "Direksiyon"
            Case "safety" : Return "Güvenlik"
            Case "seats" : Return "Koltuklar"
            Case Else : Return "Bilinmeyen"
        End Select
    End Function

    ''' <summary>
    ''' Prediction için açıklama oluşturur
    ''' </summary>
    Private Function GenerateDescription(pred As PredictedCommand) As String
        Dim desc As String = ""

        If pred.CommandType = "toggle" Then
            desc = $"Toggle @ Byte[{pred.ByteIndex}]: 0x{pred.OldValue:X2} ↔ 0x{pred.NewValue:X2}"
        Else
            desc = $"Value change @ Byte[{pred.ByteIndex}]: 0x{pred.OldValue:X2} → 0x{pred.NewValue:X2}"
        End If

        Return desc
    End Function

#End Region

#Region "AI-Finder: Prediction Olay"

    ''' <summary>
    ''' Yeni tahmin oluşturulduğunda tetiklenir
    ''' </summary>
    Public Event OnPredictionGenerated(prediction As PredictedCommand)

    ''' <summary>
    ''' Tahmin veritabanına kaydedildiğinde tetiklenir
    ''' </summary>
    Public Event OnPredictionSaved(prediction As PredictedCommand, success As Boolean)

#End Region

#Region "AI-Finder: Save to Database"

    ''' <summary>
    ''' Tahmin edilen komutu veritabanına kaydetmek için hazırlar
    ''' </summary>
    Public Function PrepareForSave(prediction As PredictedCommand,
                                    brand As String,
                                    model As String,
                                    moduleName As String,
                                    commandName As String) As Dictionary(Of String, Object)
        Dim result As New Dictionary(Of String, Object)

        Try
            result("brand") = brand
            result("model") = model
            result("module") = moduleName
            result("command") = commandName
            result("id") = prediction.IdHex
            result("category") = prediction.Category
            result("confidence") = prediction.Confidence
            result("detected_at") = prediction.DetectedAt.ToString("yyyy-MM-dd HH:mm:ss")

            If prediction.CommandType = "toggle" Then
                result("type") = "toggle"
                result("on") = prediction.OnFrame
                result("off") = prediction.OffFrame
                result("byte") = prediction.ByteIndex
            Else
                result("type") = "single"
                result("frame") = prediction.OnFrame
            End If

            result("source") = "ai_finder"
            result("verified") = False

        Catch ex As Exception
            Debug.WriteLine($"PrepareForSave error: {ex.Message}")
            result.Clear()
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Tahmin verilerini JSON formatına çevirir
    ''' </summary>
    Public Function PredictionToJson(prediction As PredictedCommand) As String
        Try
            Dim json As New System.Text.StringBuilder()

            json.AppendLine("{")
            json.AppendLine($"  ""id"": ""0x{prediction.IdHex}"",")
            json.AppendLine($"  ""byteIndex"": {prediction.ByteIndex},")
            json.AppendLine($"  ""type"": ""{prediction.CommandType}"",")
            json.AppendLine($"  ""category"": ""{prediction.Category}"",")
            json.AppendLine($"  ""description"": ""{prediction.Description}"",")
            json.AppendLine($"  ""confidence"": {prediction.Confidence},")

            If prediction.CommandType = "toggle" Then
                json.AppendLine($"  ""on"": ""{prediction.OnFrame}"",")
                json.AppendLine($"  ""off"": ""{prediction.OffFrame}"",")
            Else
                json.AppendLine($"  ""frame"": ""{prediction.OnFrame}"",")
            End If

            json.AppendLine($"  ""source"": ""ai_finder"",")
            json.AppendLine($"  ""detectedAt"": ""{prediction.DetectedAt:yyyy-MM-dd HH:mm:ss}""")
            json.AppendLine("}")

            Return json.ToString()

        Catch ex As Exception
            Debug.WriteLine($"PredictionToJson error: {ex.Message}")
            Return "{}"
        End Try
    End Function

#End Region

#Region "AI-Finder: Batch Analysis"

    ''' <summary>
    ''' Tüm değişim geçmişini analiz ederek olası komutları çıkarır
    ''' </summary>
    Public Function AnalyzeAllChangesForPredictions() As List(Of PredictedCommand)
        Dim predictions As New List(Of PredictedCommand)

        Try
            For Each kvp In _idStats
                Dim id As Integer = kvp.Key
                Dim stats As IdChangeStats = kvp.Value

                For Each byteKvp In stats.DetectedSignalTypes
                    Dim byteIndex As Integer = byteKvp.Key
                    Dim signalType As SignalType = byteKvp.Value

                    ' Toggle veya OnOff sinyalleri için prediction oluştur
                    If signalType = SignalType.OnOff Then
                        Dim pred As New PredictedCommand()
                        pred.Id = id
                        pred.IdHex = id.ToString("X3")
                        pred.ByteIndex = byteIndex
                        pred.CommandType = "toggle"
                        pred.Category = AutoClassifyByIdAndPattern(id, byteIndex, 0, 1)
                        pred.Description = $"Auto-detected toggle at ID 0x{id:X3} Byte[{byteIndex}]"
                        pred.Confidence = 70

                        predictions.Add(pred)
                        RaiseEvent OnPredictionGenerated(pred)
                    End If
                Next
            Next

            ' Güvenilirliğe göre sırala
            predictions = predictions.OrderByDescending(Function(p) p.Confidence).ToList()

        Catch ex As Exception
            Debug.WriteLine($"AnalyzeAllChangesForPredictions error: {ex.Message}")
            predictions.Clear()
        End Try

        Return predictions
    End Function

#End Region

#Region "AI-Finder: Command Prediction Access"

    ' Son tahmin edilen komutlar
    Private _lastPredictions As New List(Of CommandPrediction)

    ''' <summary>
    ''' Son tahmin edilen komutları döndürür
    ''' </summary>
    Public Function GetCommandPredictions() As List(Of CommandPrediction)
        Return _lastPredictions.ToList()
    End Function

    ''' <summary>
    ''' Tüm değişim geçmişinden komut tahminleri oluşturur ve döndürür
    ''' </summary>
    Public Function GenerateCommandPredictions() As List(Of CommandPrediction)
        _lastPredictions.Clear()

        Try
            For Each kvp In _idStats
                Dim id As Integer = kvp.Key
                Dim stats As IdChangeStats = kvp.Value

                For Each byteKvp In stats.DetectedSignalTypes
                    Dim byteIndex As Integer = byteKvp.Key
                    Dim signalType As SignalType = byteKvp.Value

                    ' Toggle veya OnOff sinyalleri için prediction oluştur
                    If signalType = SignalType.OnOff Then
                        Dim pred As New CommandPrediction()
                        pred.CanId = id
                        pred.ByteIndex = byteIndex
                        pred.PredictedName = $"Toggle_{id:X3}_B{byteIndex}"
                        pred.Category = AutoClassifyByIdAndPattern(id, byteIndex, 0, 1)
                        pred.IsToggle = True
                        pred.Confidence = 0.7
                        pred.FramePattern = $"t{id:X3}8"

                        _lastPredictions.Add(pred)
                        RaiseEvent OnCommandPredicted(pred)
                    End If
                Next
            Next

            ' Güvenilirliğe göre sırala
            _lastPredictions = _lastPredictions.OrderByDescending(Function(p) p.Confidence).ToList()

        Catch ex As Exception
            Debug.WriteLine($"GenerateCommandPredictions error: {ex.Message}")
            _lastPredictions.Clear()
        End Try

        Return _lastPredictions
    End Function

#End Region

#Region "AI-Finder: Status"

    ''' <summary>
    ''' AI-Finder durumunu özetler
    ''' </summary>
    Public Function GetAIFinderStatus() As String
        Dim sb As New System.Text.StringBuilder()

        sb.AppendLine("=== AI-Finder Durumu ===")
        sb.AppendLine($"Snapshot Before: {If(_snapshotBefore.Count > 0, $"{_snapshotBefore.Count} ID yakalandı", "Boş")}")
        sb.AppendLine($"Snapshot After: {If(_snapshotAfter.Count > 0, $"{_snapshotAfter.Count} ID yakalandı", "Boş")}")
        sb.AppendLine($"Snapshot'lar hazır: {If(AreSnapshotsReady(), "Evet", "Hayır")}")

        If _snapshotBeforeTime <> DateTime.MinValue Then
            sb.AppendLine($"Before zamanı: {_snapshotBeforeTime:HH:mm:ss}")
        End If
        If _snapshotAfterTime <> DateTime.MinValue Then
            sb.AppendLine($"After zamanı: {_snapshotAfterTime:HH:mm:ss}")
        End If

        Return sb.ToString()
    End Function

#End Region

End Class

End Namespace

