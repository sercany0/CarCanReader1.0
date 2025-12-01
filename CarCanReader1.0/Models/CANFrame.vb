' CANFrame.vb
' Parse edilmiş CAN frame verilerini temsil eden model sınıfı
' Bu sınıf SLCAN protokolünden çözümlenen frame bilgilerini tutar

Namespace Services

Public Class CANFrame

    ''' <summary>
    ''' Frame tipi: 't' = standart (11-bit), 'T' = extended (29-bit)
    ''' </summary>
    Public Property FrameType As Char

    ''' <summary>
    ''' CAN ID değeri (integer olarak)
    ''' Standart frame için 0x000 - 0x7FF arası
    ''' </summary>
    Public Property Id As Integer

    ''' <summary>
    ''' Data Length Code - veri bayt sayısı (0-8)
    ''' </summary>
    Public Property DLC As Integer

    ''' <summary>
    ''' Frame içindeki veri baytları
    ''' </summary>
    Public Property Data As Byte()

    ''' <summary>
    ''' Orijinal ham frame string (debug için)
    ''' Örnek: "t1238AABBCCDDEEFF"
    ''' </summary>
    Public Property RawFrame As String

    ''' <summary>
    ''' Frame'in geçerli olup olmadığını belirtir
    ''' False ise diğer alanlar güvenilir değildir
    ''' </summary>
    Public Property IsValid As Boolean

    ''' <summary>
    ''' Hata durumunda hata mesajı
    ''' </summary>
    Public Property ErrorMessage As String

    ''' <summary>
    ''' Frame'in alındığı zaman damgası
    ''' </summary>
    Public Property Timestamp As DateTime

    ''' <summary>
    ''' Varsayılan constructor - boş ve geçersiz frame oluşturur
    ''' </summary>
    Public Sub New()
        FrameType = "t"c
        Id = 0
        DLC = 0
        Data = New Byte() {}
        RawFrame = ""
        IsValid = False
        ErrorMessage = ""
        Timestamp = DateTime.Now
    End Sub

    ''' <summary>
    ''' ID'yi hex string olarak döndürür (3 karakter, sıfır dolgulu)
    ''' Örnek: "123", "0FF", "7FF"
    ''' </summary>
    Public Function GetIdHex() As String
        Return Id.ToString("X3")
    End Function

    ''' <summary>
    ''' Data baytlarını hex string olarak döndürür
    ''' Örnek: "AABBCCDDEEFF"
    ''' </summary>
    Public Function GetDataHex() As String
        If Data Is Nothing OrElse Data.Length = 0 Then
            Return ""
        End If

        Dim result As New System.Text.StringBuilder()
        For Each b As Byte In Data
            result.Append(b.ToString("X2"))
        Next
        Return result.ToString()
    End Function

    ''' <summary>
    ''' Frame'i okunabilir formatta döndürür
    ''' Örnek: "[t] ID=0x123 DLC=8 DATA=AABBCCDDEEFF"
    ''' </summary>
    Public Overrides Function ToString() As String
        If Not IsValid Then
            Return $"[INVALID] {RawFrame} - {ErrorMessage}"
        End If

        Return $"[{FrameType}] ID=0x{GetIdHex()} DLC={DLC} DATA={GetDataHex()}"
    End Function

End Class

End Namespace

