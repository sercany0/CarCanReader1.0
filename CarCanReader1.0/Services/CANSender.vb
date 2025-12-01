' CANSender.vb
' CAN frame gönderme servisi
' MainForm'dan taşınan gönderme mantığını içerir
'
' SLCAN Gönderme Formatı:
'   t = standart frame (11-bit ID)
'   T = extended frame (29-bit ID) - gelecekte desteklenecek
'   Format: tIDDLCDATA\r
'   Örnek: t1238AABBCCDDEEFF
'          t   = frame tipi
'          123 = CAN ID (3 hex karakter)
'          8   = DLC (1 karakter, 0-8)
'          AA..= DATA (DLC x 2 hex karakter)

Imports System.IO.Ports

Namespace Services

    ' ErrorHandler referansı için
    ' Hatalar ErrorHandler.Instance üzerinden loglanır

Public Class CANSender

#Region "Olaylar (Events)"

    ''' <summary>
    ''' Frame başarıyla gönderildiğinde tetiklenir
    ''' </summary>
    ''' <param name="frame">Gönderilen SLCAN frame string</param>
    Public Event OnFrameSent(frame As String)

    ''' <summary>
    ''' Gönderme hatası oluştuğunda tetiklenir
    ''' </summary>
    ''' <param name="message">Hata mesajı</param>
    Public Event OnSendError(message As String)

#End Region

#Region "Özel Alanlar"

    ' Bağlı seri port referansı
    Private _serialPort As SerialPort

#End Region

#Region "Sabitler"

    ' Standart ID uzunluğu (hex karakter)
    Private Const STANDARD_ID_LENGTH As Integer = 3

    ' Extended ID uzunluğu (hex karakter) - gelecek için
    Private Const EXTENDED_ID_LENGTH As Integer = 8

    ' Maksimum DLC değeri
    Private Const MAX_DLC As Integer = 8

    ' Maksimum data hex karakter uzunluğu (8 byte x 2)
    Private Const MAX_DATA_HEX_LENGTH As Integer = 16

    ' Standart frame tipi karakteri
    Private Const STANDARD_FRAME_PREFIX As String = "t"

    ' Extended frame tipi karakteri
    Private Const EXTENDED_FRAME_PREFIX As String = "T"

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Yeni CANSender örneği oluşturur
    ''' </summary>
    Public Sub New()
        _serialPort = Nothing
    End Sub

    ''' <summary>
    ''' SerialPort referansı ile CANSender örneği oluşturur
    ''' </summary>
    ''' <param name="port">Kullanılacak SerialPort nesnesi</param>
    Public Sub New(port As SerialPort)
        _serialPort = port
    End Sub

#End Region

#Region "SerialPort Yönetimi"

    ''' <summary>
    ''' Kullanılacak SerialPort'u ayarlar
    ''' </summary>
    Public Sub SetSerialPort(port As SerialPort)
        _serialPort = port
    End Sub

    ''' <summary>
    ''' SerialPort'un bağlı ve açık olup olmadığını kontrol eder
    ''' </summary>
    Public Function IsConnected() As Boolean
        Return _serialPort IsNot Nothing AndAlso _serialPort.IsOpen
    End Function

#End Region

#Region "Ana Gönderme Metodları"

    ''' <summary>
    ''' ID ve data ile CAN frame oluşturup gönderir
    ''' </summary>
    ''' <param name="id">CAN ID (3 hex karakter, örn: "123")</param>
    ''' <param name="data">Data baytları (hex string, örn: "AABBCCDD")</param>
    Public Sub Send(id As String, data As String)
        Try
            ' Bağlantı kontrolü
            If Not IsConnected() Then
                RaiseEvent OnSendError("Seri port bağlı değil")
                Return
            End If

            ' Parametreleri temizle
            id = CleanHexString(id)
            data = CleanHexString(data)

            ' Doğrulama
            Dim validationError As String = ""
            If Not ValidateFrameWithError(id, data, validationError) Then
                RaiseEvent OnSendError(validationError)
                Return
            End If

            ' Frame oluştur
            Dim frame As String = BuildFrame(id, data)

            ' Gönder
            SendToPort(frame)

            ' Başarı olayını tetikle
            RaiseEvent OnFrameSent(frame)

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "CANSender.Send")
            RaiseEvent OnSendError("Gönderme hatası: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Hazır SLCAN frame string'i doğrudan gönderir
    ''' </summary>
    ''' <param name="frame">Tam SLCAN frame string (örn: "t1238AABBCCDDEEFF")</param>
    Public Sub SendRaw(frame As String)
        Try
            ' Bağlantı kontrolü
            If Not IsConnected() Then
                RaiseEvent OnSendError("Seri port bağlı değil")
                Return
            End If

            ' Boş kontrolü
            If String.IsNullOrWhiteSpace(frame) Then
                RaiseEvent OnSendError("Frame boş olamaz")
                Return
            End If

            ' Frame'i temizle
            frame = frame.Trim()

            ' Temel format kontrolü
            If Not IsValidRawFrame(frame) Then
                RaiseEvent OnSendError("Geçersiz frame formatı")
                Return
            End If

            ' Gönder
            SendToPort(frame)

            ' Başarı olayını tetikle
            RaiseEvent OnFrameSent(frame)

        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "CANSender.SendRaw")
            RaiseEvent OnSendError("Gönderme hatası: " & ex.Message)
        End Try
    End Sub

#End Region

#Region "Doğrulama Metodları"

    ''' <summary>
    ''' ID ve data değerlerinin geçerli olup olmadığını kontrol eder
    ''' </summary>
    ''' <param name="id">CAN ID (hex string)</param>
    ''' <param name="data">Data baytları (hex string)</param>
    ''' <returns>Geçerli ise True, değilse False</returns>
    Public Function ValidateFrame(id As String, data As String) As Boolean
        Dim errorMessage As String = ""
        Return ValidateFrameWithError(id, data, errorMessage)
    End Function

    ''' <summary>
    ''' ID ve data değerlerini doğrular, hata varsa mesajı döndürür
    ''' </summary>
    Private Function ValidateFrameWithError(id As String, data As String, ByRef errorMessage As String) As Boolean
        errorMessage = ""

        ' ID kontrolü
        If String.IsNullOrWhiteSpace(id) Then
            errorMessage = "ID boş olamaz"
            Return False
        End If

        ' ID uzunluk kontrolü (standart: 3 karakter)
        If id.Length <> STANDARD_ID_LENGTH Then
            errorMessage = $"ID uzunluğu {STANDARD_ID_LENGTH} karakter olmalı (şu an: {id.Length})"
            Return False
        End If

        ' ID hex format kontrolü
        If Not IsValidHex(id) Then
            errorMessage = "ID geçersiz hex değeri içeriyor"
            Return False
        End If

        ' ID aralık kontrolü (0x000 - 0x7FF standart CAN)
        Dim idValue As Integer
        If Integer.TryParse(id, Globalization.NumberStyles.HexNumber, Nothing, idValue) Then
            If idValue < 0 OrElse idValue > &H7FF Then
                errorMessage = $"ID aralık dışı: 0x{id} (0x000-0x7FF olmalı)"
                Return False
            End If
        End If

        ' Data null olabilir (DLC=0 durumu)
        If data Is Nothing Then
            data = ""
        End If

        ' Data uzunluk kontrolü (çift sayı olmalı)
        If data.Length Mod 2 <> 0 Then
            errorMessage = "Data uzunluğu çift sayı olmalı (her byte 2 hex karakter)"
            Return False
        End If

        ' Data maksimum uzunluk kontrolü
        If data.Length > MAX_DATA_HEX_LENGTH Then
            errorMessage = $"Data çok uzun: {data.Length \ 2} byte (maksimum {MAX_DLC} byte)"
            Return False
        End If

        ' Data hex format kontrolü
        If data.Length > 0 AndAlso Not IsValidHex(data) Then
            errorMessage = "Data geçersiz hex değeri içeriyor"
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Ham SLCAN frame'in temel format kontrolünü yapar
    ''' </summary>
    Private Function IsValidRawFrame(frame As String) As Boolean
        If String.IsNullOrWhiteSpace(frame) Then Return False

        ' Minimum uzunluk: t + 3 ID + 1 DLC = 5
        If frame.Length < 5 Then Return False

        ' Frame tipi kontrolü
        Dim frameType As Char = frame(0)
        If frameType <> "t"c AndAlso frameType <> "T"c Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' String'in geçerli hex karakterler içerip içermediğini kontrol eder
    ''' </summary>
    Private Function IsValidHex(value As String) As Boolean
        If String.IsNullOrEmpty(value) Then Return True

        For Each c As Char In value.ToUpper()
            If Not ((c >= "0"c AndAlso c <= "9"c) OrElse (c >= "A"c AndAlso c <= "F"c)) Then
                Return False
            End If
        Next

        Return True
    End Function

#End Region

#Region "Frame Oluşturma"

    ''' <summary>
    ''' ID ve data'dan SLCAN frame string oluşturur
    ''' </summary>
    ''' <param name="id">CAN ID (3 hex karakter)</param>
    ''' <param name="data">Data baytları (hex string)</param>
    ''' <returns>SLCAN frame string (örn: "t1238AABBCCDDEEFF")</returns>
    Public Function BuildFrame(id As String, data As String) As String
        ' ID'yi büyük harfe çevir ve 3 karaktere tamamla
        id = id.ToUpper().PadLeft(STANDARD_ID_LENGTH, "0"c)

        ' Data'yı büyük harfe çevir
        If data Is Nothing Then data = ""
        data = data.ToUpper()

        ' DLC hesapla (byte sayısı)
        Dim dlc As Integer = data.Length \ 2

        ' SLCAN formatında birleştir: t + ID + DLC + DATA
        Return STANDARD_FRAME_PREFIX & id & dlc.ToString() & data
    End Function

    ''' <summary>
    ''' Extended CAN frame oluşturur (29-bit ID)
    ''' </summary>
    ''' <param name="id">CAN ID (8 hex karakter)</param>
    ''' <param name="data">Data baytları (hex string)</param>
    ''' <returns>Extended SLCAN frame string</returns>
    Public Function BuildExtendedFrame(id As String, data As String) As String
        ' ID'yi büyük harfe çevir ve 8 karaktere tamamla
        id = id.ToUpper().PadLeft(EXTENDED_ID_LENGTH, "0"c)

        ' Data'yı büyük harfe çevir
        If data Is Nothing Then data = ""
        data = data.ToUpper()

        ' DLC hesapla
        Dim dlc As Integer = data.Length \ 2

        ' Extended format: T + ID (8) + DLC + DATA
        Return EXTENDED_FRAME_PREFIX & id & dlc.ToString() & data
    End Function

#End Region

#Region "Yardımcı Metodlar"

    ''' <summary>
    ''' Hex string'i temizler (boşlukları kaldırır, büyük harfe çevirir)
    ''' </summary>
    Private Function CleanHexString(value As String) As String
        If value Is Nothing Then Return ""

        ' Boşlukları ve ayırıcıları kaldır
        value = value.Replace(" ", "").Replace("-", "").Replace(":", "")

        ' Trim
        value = value.Trim()

        Return value
    End Function

    ''' <summary>
    ''' Frame'i seri porta yazar
    ''' </summary>
    Private Sub SendToPort(frame As String)
        If _serialPort Is Nothing Then
            Throw New InvalidOperationException("SerialPort ayarlanmamış")
        End If

        If Not _serialPort.IsOpen Then
            Throw New InvalidOperationException("SerialPort açık değil")
        End If

        ' SLCAN frame'i gönder (CR ile sonlandır)
        _serialPort.WriteLine(frame)
    End Sub

#End Region

#Region "Ek Gönderme Metodları"

    ''' <summary>
    ''' Byte dizisi olarak data gönderir
    ''' </summary>
    ''' <param name="id">CAN ID (3 hex karakter)</param>
    ''' <param name="data">Data baytları</param>
    Public Sub SendBytes(id As String, data() As Byte)
        ' Byte dizisini hex string'e çevir
        Dim hexData As String = ""
        If data IsNot Nothing AndAlso data.Length > 0 Then
            Dim sb As New System.Text.StringBuilder()
            For Each b As Byte In data
                sb.Append(b.ToString("X2"))
            Next
            hexData = sb.ToString()
        End If

        ' Normal Send metodunu çağır
        Send(id, hexData)
    End Sub

    ''' <summary>
    ''' Integer ID ile frame gönderir
    ''' </summary>
    ''' <param name="id">CAN ID (integer, 0-2047)</param>
    ''' <param name="data">Data baytları (hex string)</param>
    Public Sub SendWithIntId(id As Integer, data As String)
        ' ID'yi 3 karakterli hex string'e çevir
        Dim hexId As String = id.ToString("X3")
        Send(hexId, data)
    End Sub

    ''' <summary>
    ''' Tek byte değiştirerek frame gönderir
    ''' </summary>
    ''' <param name="id">CAN ID</param>
    ''' <param name="baseData">Temel data (hex string)</param>
    ''' <param name="byteIndex">Değiştirilecek byte indeksi (0-7)</param>
    ''' <param name="newValue">Yeni byte değeri</param>
    Public Sub SendWithModifiedByte(id As String, baseData As String, byteIndex As Integer, newValue As Byte)
        ' Base data'yı byte dizisine çevir
        Dim bytes As New List(Of Byte)

        baseData = CleanHexString(baseData)
        For i As Integer = 0 To baseData.Length - 1 Step 2
            If i + 1 < baseData.Length Then
                Dim hexPair As String = baseData.Substring(i, 2)
                Dim byteValue As Integer
                If Integer.TryParse(hexPair, Globalization.NumberStyles.HexNumber, Nothing, byteValue) Then
                    bytes.Add(CByte(byteValue))
                End If
            End If
        Next

        ' Gerekirse diziyi genişlet
        While bytes.Count <= byteIndex
            bytes.Add(0)
        End While

        ' Byte'ı değiştir
        bytes(byteIndex) = newValue

        ' Gönder
        SendBytes(id, bytes.ToArray())
    End Sub

#End Region

#Region "Kuyruk Sistemi (Gelecek için)"

    ' TODO: Yoğun gönderim durumları için kuyruk sistemi eklenebilir
    ' Private sendQueue As New Queue(Of String)
    ' Private sendTimer As Timer

#End Region

End Class

End Namespace

