' ICanSender.vb
' CAN frame gönderme servisinin arayüz taslağı

Imports System.IO.Ports

Namespace Services
    ''' <summary>
    ''' CAN frame gönderme işlemlerini soyutlar.
    ''' Somut CANSender sınıfının mevcut public API'sini yansıtır.
    ''' </summary>
    Public Interface ICanSender
        ''' <summary>
        ''' Frame başarıyla gönderildiğinde bildirim üretir.
        ''' </summary>
        Event OnFrameSent(frame As String)

        ''' <summary>
        ''' Gönderme hatası oluştuğunda bildirim üretir.
        ''' </summary>
        Event OnSendError(message As String)

        ''' <summary>
        ''' Kullanılacak SerialPort referansını ayarlar.
        ''' </summary>
        Sub SetSerialPort(port As SerialPort)

        ''' <summary>
        ''' Bağlı seri portun erişilebilir olup olmadığını döndürür.
        ''' </summary>
        Function IsConnected() As Boolean

        ''' <summary>
        ''' ID ve data ile CAN frame oluşturup gönderir.
        ''' </summary>
        Sub Send(id As String, data As String)

        ''' <summary>
        ''' Hazır SLCAN frame string'ini doğrudan gönderir.
        ''' </summary>
        Sub SendRaw(frame As String)

        ''' <summary>
        ''' Sadece doğrulama yapmak için frame parametrelerini denetler.
        ''' </summary>
        Function ValidateFrame(id As String, data As String) As Boolean

        ''' <summary>
        ''' Standart frame formatında SLCAN string üretir.
        ''' </summary>
        Function BuildFrame(id As String, data As String) As String

        ''' <summary>
        ''' Extended frame formatında SLCAN string üretir.
        ''' </summary>
        Function BuildExtendedFrame(id As String, data As String) As String

        ''' <summary>
        ''' Byte dizisi kullanarak frame gönderir.
        ''' </summary>
        Sub SendBytes(id As String, data() As Byte)

        ''' <summary>
        ''' Integer ID ile frame gönderir.
        ''' </summary>
        Sub SendWithIntId(id As Integer, data As String)

        ''' <summary>
        ''' Belirli bir byte değerini değiştirerek frame gönderir.
        ''' </summary>
        Sub SendWithModifiedByte(id As String, baseData As String, byteIndex As Integer, newValue As Byte)
    End Interface
End Namespace
