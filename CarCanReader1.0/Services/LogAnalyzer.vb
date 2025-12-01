' LogAnalyzer.vb
' CAN log analiz servisi
' Log satırlarını işler ve istatistiksel analiz sonuçları üretir
'
' ÖNEMLİ: Bu sınıf UI kodu İÇERMEZ.
' MainForm tarafından kullanılır ve sonuçlar şu kontrollerde gösterilir:
'   - lstIdStats
'   - lstFinderTopIDs
'   - lstFinderByteStats
'   - lblFinderInfo
'
' Kullanım:
'   Dim analyzer As New LogAnalyzer()
'   Dim frames = analyzer.ExtractFrames(lstLog.Items)
'   Dim idStats = analyzer.CountIds(frames)
'   Dim byteStats = analyzer.ComputeByteStats(frames)
'   Dim insights = analyzer.BuildInsights(idStats, byteStats)

Namespace Services

    ''' <summary>
    ''' CAN log analiz motoru
    ''' UI ile doğrudan etkileşimi yoktur
    ''' </summary>
    Public Class LogAnalyzer

#Region "Sabitler"

        ' RX satırı tanımlayıcısı
        Private Const RX_MARKER As String = "RX:"

        ' Minimum SLCAN frame uzunluğu (t + 3 ID + 1 DLC = 5)
        Private Const MIN_FRAME_LENGTH As Integer = 5

        ' Yoğun ID eşiği (insight için)
        Private Const BUSY_ID_THRESHOLD As Integer = 100

        ' Sık değişen byte eşiği (insight için)
        Private Const FREQUENT_CHANGE_THRESHOLD As Integer = 50

#End Region

#Region "ExtractFrames"

        ''' <summary>
        ''' Log satırlarından SLCAN frame'lerini çıkarır
        ''' "RX:" içeren satırlardan frame kısmını alır
        ''' </summary>
        ''' <param name="logLines">Log satırları (lstLog.Items)</param>
        ''' <returns>SLCAN frame listesi</returns>
        Public Function ExtractFrames(logLines As IEnumerable(Of String)) As List(Of String)
            Dim frames As New List(Of String)()

            Try
                If logLines Is Nothing Then Return frames

                For Each line As String In logLines
                    Try
                        If String.IsNullOrWhiteSpace(line) Then Continue For

                        ' RX: içeren satırları bul
                        Dim rxIndex As Integer = line.IndexOf(RX_MARKER, StringComparison.OrdinalIgnoreCase)
                        If rxIndex = -1 Then Continue For

                        ' RX: sonrasındaki kısmı al
                        Dim afterRx As String = line.Substring(rxIndex + RX_MARKER.Length).Trim()

                        ' Boşluk varsa ilk kelimeyi al
                        Dim spaceIndex As Integer = afterRx.IndexOf(" "c)
                        Dim frame As String
                        If spaceIndex > 0 Then
                            frame = afterRx.Substring(0, spaceIndex)
                        Else
                            frame = afterRx
                        End If

                        ' Geçerli frame mi kontrol et
                        If IsValidFrame(frame) Then
                            frames.Add(frame)
                        End If

                    Catch
                        ' Tek satır hatası, devam et
                        Continue For
                    End Try
                Next

            Catch
                ' Genel hata, boş liste döndür
                frames.Clear()
            End Try

            Return frames
        End Function

        ''' <summary>
        ''' Object koleksiyonundan frame'leri çıkarır (lstLog.Items için)
        ''' </summary>
        Public Function ExtractFrames(logItems As System.Collections.IEnumerable) As List(Of String)
            Dim stringList As New List(Of String)()

            Try
                If logItems Is Nothing Then Return ExtractFrames(stringList)

                For Each item In logItems
                    If item IsNot Nothing Then
                        stringList.Add(item.ToString())
                    End If
                Next

            Catch
                ' Hata durumunda boş liste
            End Try

            Return ExtractFrames(stringList)
        End Function

        ''' <summary>
        ''' Frame'in geçerli SLCAN formatında olup olmadığını kontrol eder
        ''' </summary>
        Private Function IsValidFrame(frame As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(frame) Then Return False
                If frame.Length < MIN_FRAME_LENGTH Then Return False

                ' t veya T ile başlamalı
                Dim firstChar As Char = frame(0)
                If firstChar <> "t"c AndAlso firstChar <> "T"c Then Return False

                Return True

            Catch
                Return False
            End Try
        End Function

#End Region

#Region "CountIds"

        ''' <summary>
        ''' Her CAN ID'nin kaç kez göründüğünü sayar
        ''' </summary>
        ''' <param name="frames">SLCAN frame listesi</param>
        ''' <returns>ID → sayım sözlüğü</returns>
        Public Function CountIds(frames As IEnumerable(Of String)) As Dictionary(Of Integer, Integer)
            Dim idCounts As New Dictionary(Of Integer, Integer)()

            Try
                If frames Is Nothing Then Return idCounts

                For Each frame As String In frames
                    Try
                        Dim parsed = ParseFrame(frame)
                        Dim id As Integer = parsed.Id

                        ' ID 0 ise geçersiz parse, atla
                        If id = 0 AndAlso (parsed.Data Is Nothing OrElse parsed.Data.Length = 0) Then
                            ' Geçersiz frame, ancak ID gerçekten 0 olabilir
                            ' Frame uzunluğu kontrol et
                            If frame.Length < MIN_FRAME_LENGTH Then Continue For
                        End If

                        If Not idCounts.ContainsKey(id) Then
                            idCounts(id) = 0
                        End If
                        idCounts(id) += 1

                    Catch
                        Continue For
                    End Try
                Next

            Catch
                idCounts.Clear()
            End Try

            Return idCounts
        End Function

        ''' <summary>
        ''' ID sayımlarını azalan sırada döndürür
        ''' </summary>
        Public Function GetSortedIdCounts(idCounts As Dictionary(Of Integer, Integer)) As List(Of KeyValuePair(Of Integer, Integer))
            Try
                If idCounts Is Nothing Then Return New List(Of KeyValuePair(Of Integer, Integer))()

                Return idCounts.OrderByDescending(Function(x) x.Value).ToList()

            Catch
                Return New List(Of KeyValuePair(Of Integer, Integer))()
            End Try
        End Function

#End Region

#Region "ParseFrame"

        ''' <summary>
        ''' SLCAN frame'i ID ve Data olarak parse eder
        ''' </summary>
        ''' <param name="frame">SLCAN frame (örn: "t5A881112233445566")</param>
        ''' <returns>Tuple (Id, Data). Hata durumunda (0, boş dizi)</returns>
        Public Function ParseFrame(frame As String) As (Id As Integer, Data As Byte())
            Try
                If String.IsNullOrWhiteSpace(frame) Then
                    Return (0, New Byte() {})
                End If

                frame = frame.Trim()

                If frame.Length < MIN_FRAME_LENGTH Then
                    Return (0, New Byte() {})
                End If

                ' Frame tipi kontrolü
                Dim frameType As Char = frame(0)
                If frameType <> "t"c AndAlso frameType <> "T"c Then
                    Return (0, New Byte() {})
                End If

                ' ID uzunluğunu belirle
                Dim idLength As Integer = If(frameType = "t"c, 3, 8)

                ' Frame yeterince uzun mu
                If frame.Length < 1 + idLength + 1 Then
                    Return (0, New Byte() {})
                End If

                ' ID'yi parse et
                Dim idHex As String = frame.Substring(1, idLength)
                Dim id As Integer
                If Not Integer.TryParse(idHex, Globalization.NumberStyles.HexNumber, Nothing, id) Then
                    Return (0, New Byte() {})
                End If

                ' DLC'yi parse et
                Dim dlcChar As String = frame.Substring(1 + idLength, 1)
                Dim dlc As Integer
                If Not Integer.TryParse(dlcChar, dlc) Then
                    Return (id, New Byte() {})
                End If

                ' Data'yı parse et
                Dim dataStartIndex As Integer = 1 + idLength + 1
                Dim expectedDataLength As Integer = dlc * 2

                If frame.Length < dataStartIndex + expectedDataLength Then
                    ' Yeterli data yok, mevcut olanı al
                    expectedDataLength = frame.Length - dataStartIndex
                End If

                Dim dataHex As String = frame.Substring(dataStartIndex, expectedDataLength)
                Dim dataBytes As Byte() = ParseHexToBytes(dataHex)

                Return (id, dataBytes)

            Catch
                Return (0, New Byte() {})
            End Try
        End Function

        ''' <summary>
        ''' Hex string'i byte dizisine çevirir
        ''' </summary>
        Private Function ParseHexToBytes(hexStr As String) As Byte()
            Try
                If String.IsNullOrWhiteSpace(hexStr) Then Return New Byte() {}

                ' Tek sayıda karakter varsa son karakteri at
                If hexStr.Length Mod 2 <> 0 Then
                    hexStr = hexStr.Substring(0, hexStr.Length - 1)
                End If

                Dim byteCount As Integer = hexStr.Length \ 2
                If byteCount = 0 Then Return New Byte() {}

                Dim bytes(byteCount - 1) As Byte

                For i As Integer = 0 To byteCount - 1
                    Dim hexPair As String = hexStr.Substring(i * 2, 2)
                    Dim byteValue As Integer

                    If Integer.TryParse(hexPair, Globalization.NumberStyles.HexNumber, Nothing, byteValue) Then
                        bytes(i) = CByte(byteValue)
                    Else
                        bytes(i) = 0
                    End If
                Next

                Return bytes

            Catch
                Return New Byte() {}
            End Try
        End Function

#End Region

#Region "ComputeByteStats"

        ''' <summary>
        ''' Her ID için byte seviyesinde değişim istatistiklerini hesaplar
        ''' </summary>
        ''' <param name="frames">SLCAN frame listesi</param>
        ''' <returns>ID → (ByteIndex → DeğişimSayısı) sözlüğü</returns>
        Public Function ComputeByteStats(frames As IEnumerable(Of String)) As Dictionary(Of Integer, Dictionary(Of Integer, Integer))
            Dim stats As New Dictionary(Of Integer, Dictionary(Of Integer, Integer))()

            Try
                If frames Is Nothing Then Return stats

                ' Son görülen frame'leri takip et
                Dim lastFrames As New Dictionary(Of Integer, Byte())()

                For Each frame As String In frames
                    Try
                        Dim parsed = ParseFrame(frame)
                        Dim id As Integer = parsed.Id
                        Dim data As Byte() = parsed.Data

                        ' Geçersiz frame atla
                        If data Is Nothing OrElse data.Length = 0 Then Continue For

                        ' Bu ID için stats yoksa oluştur
                        If Not stats.ContainsKey(id) Then
                            stats(id) = New Dictionary(Of Integer, Integer)()
                        End If

                        ' Önceki frame varsa karşılaştır
                        If lastFrames.ContainsKey(id) Then
                            Dim oldData As Byte() = lastFrames(id)
                            Dim minLen As Integer = Math.Min(oldData.Length, data.Length)

                            For i As Integer = 0 To minLen - 1
                                If oldData(i) <> data(i) Then
                                    ' Bu byte değişti
                                    If Not stats(id).ContainsKey(i) Then
                                        stats(id)(i) = 0
                                    End If
                                    stats(id)(i) += 1
                                End If
                            Next
                        End If

                        ' Son frame'i güncelle
                        lastFrames(id) = CloneByteArray(data)

                    Catch
                        Continue For
                    End Try
                Next

            Catch
                stats.Clear()
            End Try

            Return stats
        End Function

        ''' <summary>
        ''' Byte dizisini kopyalar
        ''' </summary>
        Private Function CloneByteArray(source As Byte()) As Byte()
            If source Is Nothing Then Return New Byte() {}

            Dim copy(source.Length - 1) As Byte
            Array.Copy(source, copy, source.Length)
            Return copy
        End Function

#End Region

#Region "BuildInsights"

        ''' <summary>
        ''' İstatistiklerden gelişmiş okunabilir içgörüler oluşturur
        ''' Auto-detect toggle bytes ve continuous sensor bytes
        ''' </summary>
        Public Function BuildInsights(idStats As Dictionary(Of Integer, Integer),
                                        byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer))) As List(Of String)
            Dim insights As New List(Of String)()

            Try
                If idStats Is Nothing Then idStats = New Dictionary(Of Integer, Integer)()
                If byteStats Is Nothing Then byteStats = New Dictionary(Of Integer, Dictionary(Of Integer, Integer))()

                insights.Add("╔═══════════════════════════════════════════════════════════╗")
                insights.Add("║                   ANALYSIS INSIGHTS                       ║")
                insights.Add("╠═══════════════════════════════════════════════════════════╣")

                ' Auto-detect toggles
                Dim toggles = DetectPotentialToggles(byteStats)
                If toggles.Count > 0 Then
                    insights.Add("║ [TOGGLE] Detected potential ON/OFF signals:              ║")
                    For Each t In toggles.Take(5)
                        insights.Add($"║   • ID 0x{t.Id:X3} Byte[{t.ByteIndex}] - {t.ChangeCount} toggles         ║")
                    Next
                    insights.Add("║                                                           ║")
                End If

                ' Auto-detect sensors
                Dim sensors = DetectPotentialSensors(byteStats)
                If sensors.Count > 0 Then
                    insights.Add("║ [SENSOR] Detected continuous data streams:               ║")
                    For Each s In sensors.Take(5)
                        insights.Add($"║   • ID 0x{s.Id:X3} - {s.ChangingByteCount} active bytes              ║")
                    Next
                    insights.Add("║                                                           ║")
                End If

                ' En yoğun ID'leri bul
                Dim sortedIds = idStats.OrderByDescending(Function(x) x.Value).Take(10).ToList()

                insights.Add("║ [TRAFFIC] Top CAN IDs by frequency:                       ║")
                For Each kvp In sortedIds.Take(5)
                    Dim id = kvp.Key
                    Dim count = kvp.Value
                    Dim indicator As String = If(count > BUSY_ID_THRESHOLD, "[HIGH]", "[    ]")
                    insights.Add($"║   {indicator} 0x{id:X3}: {count,6} frames                       ║")
                Next

                ' Byte değişim detayları
                insights.Add("║                                                           ║")
                insights.Add("║ [BYTES] Notable byte-level changes:                       ║")

                For Each kvp In sortedIds.Take(3)
                    Dim id = kvp.Key
                    If byteStats.ContainsKey(id) Then
                        Dim byteChanges = byteStats(id)
                        Dim sortedBytes = byteChanges.OrderByDescending(Function(x) x.Value).Take(3).ToList()

                        For Each byteKvp In sortedBytes
                            If byteKvp.Value >= 10 Then
                                Dim signalHint As String = ""
                                If byteKvp.Value >= FREQUENT_CHANGE_THRESHOLD AndAlso sortedBytes.Count = 1 Then
                                    signalHint = " [TOGGLE?]"
                                ElseIf sortedBytes.Count >= 2 Then
                                    signalHint = " [SENSOR?]"
                                End If
                                insights.Add($"║   0x{id:X3} B[{byteKvp.Key}]: {byteKvp.Value,4}x{signalHint,-10}                ║")
                            End If
                        Next
                    End If
                Next

                ' Özet
                insights.Add("╠═══════════════════════════════════════════════════════════╣")
                insights.Add("║                       SUMMARY                             ║")
                insights.Add("╠═══════════════════════════════════════════════════════════╣")
                insights.Add($"║  Unique CAN IDs:    {idStats.Count,6}                              ║")
                insights.Add($"║  Total Frames:      {idStats.Values.Sum(),6}                              ║")
                insights.Add($"║  IDs with changes:  {byteStats.Count,6}                              ║")
                insights.Add($"║  Potential toggles: {toggles.Count,6}                              ║")
                insights.Add($"║  Potential sensors: {sensors.Count,6}                              ║")
                insights.Add("╚═══════════════════════════════════════════════════════════╝")

            Catch ex As Exception
                insights.Add($"[!] Analiz hatası: {ex.Message}")
            End Try

            Return insights
        End Function

        ''' <summary>
        ''' Basit içgörü formatı (eski uyumluluk)
        ''' </summary>
        Public Function BuildSimpleInsights(idStats As Dictionary(Of Integer, Integer),
                                             byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer))) As List(Of String)
            Dim insights As New List(Of String)()

            Try
                Dim sortedIds = idStats.OrderByDescending(Function(x) x.Value).Take(10).ToList()

                For Each kvp In sortedIds
                    insights.Add($"ID 0x{kvp.Key:X3}: {kvp.Value} frame")
                Next

                insights.Add("")
                insights.Add($"Toplam: {idStats.Count} ID, {idStats.Values.Sum()} frame")

            Catch
            End Try

            Return insights
        End Function

#End Region

#Region "Ek Analiz Metodları"

        ''' <summary>
        ''' Belirli bir ID için detaylı byte istatistiklerini döndürür
        ''' </summary>
        Public Function GetByteStatsForId(byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer)),
                                           id As Integer) As Dictionary(Of Integer, Integer)
            Try
                If byteStats Is Nothing Then Return New Dictionary(Of Integer, Integer)()
                If Not byteStats.ContainsKey(id) Then Return New Dictionary(Of Integer, Integer)()

                Return byteStats(id)

            Catch
                Return New Dictionary(Of Integer, Integer)()
            End Try
        End Function

        ''' <summary>
        ''' En çok değişen byte'ları olan ID'leri döndürür
        ''' </summary>
        Public Function GetMostActiveIds(byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer)),
                                          Optional limit As Integer = 10) As List(Of (Id As Integer, TotalChanges As Integer))
            Dim result As New List(Of (Id As Integer, TotalChanges As Integer))()

            Try
                If byteStats Is Nothing Then Return result

                For Each kvp In byteStats
                    Dim totalChanges As Integer = kvp.Value.Values.Sum()
                    result.Add((kvp.Key, totalChanges))
                Next

                result = result.OrderByDescending(Function(x) x.TotalChanges).Take(limit).ToList()

            Catch
                result.Clear()
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Toggle sinyali olma olasılığı yüksek ID'leri tespit eder
        ''' </summary>
        Public Function DetectPotentialToggles(byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer))) As List(Of (Id As Integer, ByteIndex As Integer, ChangeCount As Integer))
            Dim toggles As New List(Of (Id As Integer, ByteIndex As Integer, ChangeCount As Integer))()

            Try
                If byteStats Is Nothing Then Return toggles

                For Each idKvp In byteStats
                    Dim id As Integer = idKvp.Key
                    Dim byteChanges = idKvp.Value

                    ' Sadece tek byte değişen ID'leri bul
                    Dim significantBytes = byteChanges.Where(Function(x) x.Value >= FREQUENT_CHANGE_THRESHOLD).ToList()

                    If significantBytes.Count = 1 Then
                        Dim byteInfo = significantBytes(0)
                        toggles.Add((id, byteInfo.Key, byteInfo.Value))
                    End If
                Next

                toggles = toggles.OrderByDescending(Function(x) x.ChangeCount).ToList()

            Catch
                toggles.Clear()
            End Try

            Return toggles
        End Function

        ''' <summary>
        ''' Sensör verisi olma olasılığı yüksek ID'leri tespit eder
        ''' </summary>
        Public Function DetectPotentialSensors(byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer))) As List(Of (Id As Integer, ChangingByteCount As Integer))
            Dim sensors As New List(Of (Id As Integer, ChangingByteCount As Integer))()

            Try
                If byteStats Is Nothing Then Return sensors

                For Each idKvp In byteStats
                    Dim id As Integer = idKvp.Key
                    Dim byteChanges = idKvp.Value

                    ' Birden fazla byte değişen ID'leri bul
                    Dim changingBytes = byteChanges.Where(Function(x) x.Value >= 10).Count()

                    If changingBytes >= 2 Then
                        sensors.Add((id, changingBytes))
                    End If
                Next

                sensors = sensors.OrderByDescending(Function(x) x.ChangingByteCount).ToList()

            Catch
                sensors.Clear()
            End Try

            Return sensors
        End Function

#End Region

#Region "Formatlama Yardımcıları"

        ''' <summary>
        ''' ID istatistiklerini okunabilir satırlara çevirir (gelişmiş format)
        ''' </summary>
        Public Function FormatIdStats(idStats As Dictionary(Of Integer, Integer)) As List(Of String)
            Dim lines As New List(Of String)()

            Try
                If idStats Is Nothing Then Return lines

                Dim sorted = idStats.OrderByDescending(Function(x) x.Value).ToList()
                Dim maxCount = If(sorted.Count > 0, sorted(0).Value, 1)

                lines.Add("╔═══════════════════════════════════════════════╗")
                lines.Add("║            CAN ID FREQUENCY TABLE             ║")
                lines.Add("╠═══════════════════════════════════════════════╣")

                For Each kvp In sorted
                    Dim id = kvp.Key
                    Dim count = kvp.Value
                    Dim barLength = CInt(Math.Round(count / maxCount * 20))
                    Dim bar = New String("█"c, barLength) & New String("░"c, 20 - barLength)

                    ' Activity indicator
                    Dim activity As String
                    If count > BUSY_ID_THRESHOLD * 5 Then
                        activity = "[VERY HIGH]"
                    ElseIf count > BUSY_ID_THRESHOLD Then
                        activity = "[HIGH]     "
                    ElseIf count > 50 Then
                        activity = "[MEDIUM]   "
                    Else
                        activity = "[LOW]      "
                    End If

                    lines.Add($"║ 0x{id:X3} │ {bar} │ {count,5} │ {activity} ║")
                Next

                lines.Add("╚═══════════════════════════════════════════════╝")

            Catch
                lines.Clear()
            End Try

            Return lines
        End Function

        ''' <summary>
        ''' Basit ID listesi formatı
        ''' </summary>
        Public Function FormatIdStatsSimple(idStats As Dictionary(Of Integer, Integer)) As List(Of String)
            Dim lines As New List(Of String)()

            Try
                If idStats Is Nothing Then Return lines

                Dim sorted = idStats.OrderByDescending(Function(x) x.Value).ToList()

                For Each kvp In sorted
                    lines.Add($"ID 0x{kvp.Key:X3}  →  {kvp.Value} frame")
                Next

            Catch
                lines.Clear()
            End Try

            Return lines
        End Function

        ''' <summary>
        ''' Byte istatistiklerini okunabilir satırlara çevirir
        ''' </summary>
        Public Function FormatByteStats(byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer)),
                                          id As Integer) As List(Of String)
            Dim lines As New List(Of String)()

            Try
                If byteStats Is Nothing Then Return lines
                If Not byteStats.ContainsKey(id) Then Return lines

                Dim stats = byteStats(id)
                Dim sorted = stats.OrderByDescending(Function(x) x.Value).ToList()

                For Each kvp In sorted
                    lines.Add($"Byte[{kvp.Key}]: {kvp.Value} değişim")
                Next

            Catch
                lines.Clear()
            End Try

            Return lines
        End Function

        ''' <summary>
        ''' Durum özeti döndürür
        ''' </summary>
        Public Function GetSummary(idStats As Dictionary(Of Integer, Integer),
                                    byteStats As Dictionary(Of Integer, Dictionary(Of Integer, Integer))) As String
            Try
                Dim uniqueIds As Integer = If(idStats IsNot Nothing, idStats.Count, 0)
                Dim totalFrames As Integer = If(idStats IsNot Nothing, idStats.Values.Sum(), 0)
                Dim idsWithChanges As Integer = If(byteStats IsNot Nothing, byteStats.Count, 0)

                Return $"Analiz: {uniqueIds} benzersiz ID, {totalFrames} frame, {idsWithChanges} ID'de değişim tespit edildi"

            Catch
                Return "Analiz tamamlanamadı"
            End Try
        End Function

#End Region

    End Class

End Namespace

