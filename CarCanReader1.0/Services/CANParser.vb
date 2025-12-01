' CANParser.vb
' SLCAN protokolü frame çözümleme servisi
' MainForm'dan taşınan parsing mantığını içerir
'
' SLCAN Frame Formatı:
'   t = standart frame (11-bit ID)
'   T = extended frame (29-bit ID) - gelecekte desteklenecek
'   Format: tIDDLCDATA
'   Örnek: t1238AABBCCDDEEFF
'          t   = frame tipi
'          123 = CAN ID (3 hex karakter)
'          8   = DLC (1 karakter, 0-8)
'          AA..= DATA (DLC x 2 hex karakter)

Namespace Services

Public Class CANParser

#Region "Olaylar (Events)"

    ''' <summary>
    ''' Frame başarıyla çözümlendiğinde tetiklenir
    ''' </summary>
    ''' <param name="id">CAN ID (integer)</param>
    ''' <param name="data">Veri baytları</param>
    Public Event OnFrameDecoded(id As Integer, data() As Byte)

    ''' <summary>
    ''' Parsing hatası oluştuğunda tetiklenir
    ''' </summary>
    ''' <param name="rawFrame">Ham frame string</param>
    ''' <param name="errorMessage">Hata mesajı</param>
    Public Event OnParseError(rawFrame As String, errorMessage As String)

#End Region

#Region "Sabitler"

    ' Minimum frame uzunluğu: t + 3 ID + 1 DLC = 5 karakter
    Private Const MIN_FRAME_LENGTH As Integer = 5

    ' Standart frame tipi karakteri
    Private Const STANDARD_FRAME_TYPE As Char = "t"c

    ' Extended frame tipi karakteri (gelecek için)
    Private Const EXTENDED_FRAME_TYPE As Char = "T"c

    ' Standart ID uzunluğu (hex karakter)
    Private Const STANDARD_ID_LENGTH As Integer = 3

    ' Extended ID uzunluğu (hex karakter) - gelecek için
    Private Const EXTENDED_ID_LENGTH As Integer = 8

    ' Maksimum DLC değeri
    Private Const MAX_DLC As Integer = 8

#End Region

#Region "Ana Parse Metodu"

    ''' <summary>
    ''' SLCAN frame string'ini parse eder ve CANFrame nesnesi döndürür
    ''' </summary>
    ''' <param name="frame">Ham SLCAN frame string (örn: "t1238AABBCCDDEEFF")</param>
    ''' <returns>Parse edilmiş CANFrame nesnesi</returns>
    Public Function Parse(frame As String) As CANFrame
        Dim result As New CANFrame()
        result.RawFrame = frame
        result.Timestamp = DateTime.Now

        ' Boş veya null frame kontrolü
        If String.IsNullOrWhiteSpace(frame) Then
            result.IsValid = False
            result.ErrorMessage = "Frame boş veya null"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If

        ' Frame'i temizle (whitespace, CR, LF kaldır)
        frame = CleanFrame(frame)
        result.RawFrame = frame

        ' Minimum uzunluk kontrolü
        If frame.Length < MIN_FRAME_LENGTH Then
            result.IsValid = False
            result.ErrorMessage = $"Frame çok kısa (min {MIN_FRAME_LENGTH} karakter gerekli)"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If

        ' Frame tipini belirle
        Dim frameType As Char = frame(0)
        If Not IsValidFrameType(frameType) Then
            result.IsValid = False
            result.ErrorMessage = $"Geçersiz frame tipi: '{frameType}' (beklenen: 't' veya 'T')"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If
        result.FrameType = frameType

        ' ID uzunluğunu belirle (standart vs extended)
        Dim idLength As Integer = GetIdLength(frameType)

        ' Frame uzunluk kontrolü (tip + ID + DLC minimum)
        If frame.Length < 1 + idLength + 1 Then
            result.IsValid = False
            result.ErrorMessage = "Frame ID veya DLC için yeterli karakter içermiyor"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If

        ' ID'yi parse et
        Dim idHex As String = frame.Substring(1, idLength)
        Dim id As Integer
        If Not TryParseHexId(idHex, id) Then
            result.IsValid = False
            result.ErrorMessage = $"Geçersiz ID hex değeri: '{idHex}'"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If
        result.Id = id

        ' DLC'yi parse et
        Dim dlcIndex As Integer = 1 + idLength
        Dim dlcChar As String = frame.Substring(dlcIndex, 1)
        Dim dlc As Integer
        If Not Integer.TryParse(dlcChar, dlc) Then
            result.IsValid = False
            result.ErrorMessage = $"Geçersiz DLC değeri: '{dlcChar}'"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If

        ' DLC aralık kontrolü
        If dlc < 0 OrElse dlc > MAX_DLC Then
            result.IsValid = False
            result.ErrorMessage = $"DLC aralık dışı: {dlc} (0-{MAX_DLC} olmalı)"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If
        result.DLC = dlc

        ' DATA'yı parse et
        Dim dataStartIndex As Integer = dlcIndex + 1
        Dim expectedDataLength As Integer = dlc * 2 ' Her byte 2 hex karakter

        ' Data uzunluk kontrolü
        Dim availableDataLength As Integer = frame.Length - dataStartIndex
        If availableDataLength < expectedDataLength Then
            result.IsValid = False
            result.ErrorMessage = $"Yetersiz data: {availableDataLength} karakter var, {expectedDataLength} bekleniyor"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If

        ' Data baytlarını parse et
        Dim dataHex As String = frame.Substring(dataStartIndex, expectedDataLength)
        Dim dataBytes As Byte() = Nothing
        If Not TryParseHexData(dataHex, dataBytes) Then
            result.IsValid = False
            result.ErrorMessage = $"Geçersiz data hex değeri: '{dataHex}'"
            RaiseEvent OnParseError(frame, result.ErrorMessage)
            Return result
        End If
        result.Data = dataBytes

        ' Başarılı parse
        result.IsValid = True
        result.ErrorMessage = ""

        ' Olayı tetikle
        RaiseEvent OnFrameDecoded(result.Id, result.Data)

        Return result
    End Function

#End Region

#Region "Yardımcı Metodlar"

    ''' <summary>
    ''' Frame string'ini temizler (whitespace, CR, LF kaldırır)
    ''' </summary>
    Private Function CleanFrame(frame As String) As String
        If frame Is Nothing Then Return ""

        ' Baştan ve sondan boşlukları kaldır
        frame = frame.Trim()

        ' CR ve LF karakterlerini kaldır
        frame = frame.Replace(vbCr, "").Replace(vbLf, "")

        Return frame
    End Function

    ''' <summary>
    ''' Frame tipinin geçerli olup olmadığını kontrol eder
    ''' </summary>
    Private Function IsValidFrameType(frameType As Char) As Boolean
        Return frameType = STANDARD_FRAME_TYPE OrElse frameType = EXTENDED_FRAME_TYPE
    End Function

    ''' <summary>
    ''' Frame tipine göre ID uzunluğunu döndürür
    ''' </summary>
    Private Function GetIdLength(frameType As Char) As Integer
        Select Case frameType
            Case STANDARD_FRAME_TYPE
                Return STANDARD_ID_LENGTH ' 3 karakter (11-bit)
            Case EXTENDED_FRAME_TYPE
                Return EXTENDED_ID_LENGTH ' 8 karakter (29-bit)
            Case Else
                Return STANDARD_ID_LENGTH
        End Select
    End Function

    ''' <summary>
    ''' Hex ID string'ini integer'a dönüştürmeyi dener
    ''' </summary>
    Private Function TryParseHexId(idHex As String, ByRef id As Integer) As Boolean
        Try
            Return Integer.TryParse(idHex, Globalization.NumberStyles.HexNumber, Nothing, id)
        Catch
            id = 0
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Hex data string'ini byte dizisine dönüştürmeyi dener
    ''' </summary>
    Private Function TryParseHexData(dataHex As String, ByRef dataBytes As Byte()) As Boolean
        Try
            If String.IsNullOrEmpty(dataHex) Then
                dataBytes = New Byte() {}
                Return True
            End If

            ' Çift sayıda karakter olmalı
            If dataHex.Length Mod 2 <> 0 Then
                dataBytes = Nothing
                Return False
            End If

            Dim byteCount As Integer = dataHex.Length \ 2
            Dim result(byteCount - 1) As Byte

            For i As Integer = 0 To byteCount - 1
                Dim hexPair As String = dataHex.Substring(i * 2, 2)
                Dim byteValue As Integer

                If Not Integer.TryParse(hexPair, Globalization.NumberStyles.HexNumber, Nothing, byteValue) Then
                    dataBytes = Nothing
                    Return False
                End If

                result(i) = CByte(byteValue)
            Next

            dataBytes = result
            Return True

        Catch
            dataBytes = Nothing
            Return False
        End Try
    End Function

#End Region

#Region "Ek Yardımcı Metodlar"

    ''' <summary>
    ''' Verilen string'in geçerli bir SLCAN frame olup olmadığını kontrol eder
    ''' (tam parse yapmadan hızlı kontrol)
    ''' </summary>
    Public Function IsValidFrame(frame As String) As Boolean
        If String.IsNullOrWhiteSpace(frame) Then Return False

        frame = CleanFrame(frame)

        If frame.Length < MIN_FRAME_LENGTH Then Return False

        Dim frameType As Char = frame(0)
        If Not IsValidFrameType(frameType) Then Return False

        Return True
    End Function

    ''' <summary>
    ''' Frame'den sadece ID'yi çıkarır (hızlı erişim için)
    ''' Parse hatası durumunda -1 döner
    ''' </summary>
    Public Function ExtractId(frame As String) As Integer
        Try
            If String.IsNullOrWhiteSpace(frame) Then Return -1

            frame = CleanFrame(frame)

            If frame.Length < MIN_FRAME_LENGTH Then Return -1

            Dim frameType As Char = frame(0)
            If Not IsValidFrameType(frameType) Then Return -1

            Dim idLength As Integer = GetIdLength(frameType)
            Dim idHex As String = frame.Substring(1, idLength)

            Dim id As Integer
            If TryParseHexId(idHex, id) Then
                Return id
            End If

            Return -1

        Catch
            Return -1
        End Try
    End Function

    ''' <summary>
    ''' Birden fazla frame'i toplu olarak parse eder
    ''' </summary>
    Public Function ParseMultiple(frames As IEnumerable(Of String)) As List(Of CANFrame)
        Dim results As New List(Of CANFrame)

        For Each frame In frames
            results.Add(Parse(frame))
        Next

        Return results
    End Function

    ''' <summary>
    ''' Sadece geçerli frame'leri parse edip döndürür
    ''' </summary>
    Public Function ParseValidOnly(frames As IEnumerable(Of String)) As List(Of CANFrame)
        Dim results As New List(Of CANFrame)

        For Each frame In frames
            Dim parsed = Parse(frame)
            If parsed.IsValid Then
                results.Add(parsed)
            End If
        Next

        Return results
    End Function

#End Region

End Class

End Namespace

