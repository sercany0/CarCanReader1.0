' ISerialPortManager.vb
' Seri port yönetim servisinin arayüz taslağı

Imports System.IO.Ports

Namespace Services
    ''' <summary>
    ''' Seri port açma/kapama ve veri gönderim/alım akışını soyutlar.
    ''' Somut SerialPortManager sınıfının temel public API'sini taşır.
    ''' </summary>
    Public Interface ISerialPortManager
        ''' <summary>
        ''' Gelen satırlar işlendiğinde bildirim üretir.
        ''' </summary>
        Event OnDataReceived(data As String)

        ''' <summary>
        ''' Bağlantı durumu değiştiğinde bildirim üretir.
        ''' </summary>
        Event OnConnectionChanged(isConnected As Boolean)

        ''' <summary>
        ''' İşlem hatası oluştuğunda bildirim üretir.
        ''' </summary>
        Event OnError(message As String)

        ''' <summary>
        ''' Ham veri okunduğunda bildirim üretir.
        ''' </summary>
        Event OnRawDataReceived(data As String)

        ''' <summary>
        ''' Kullanılacak port adını belirler.
        ''' </summary>
        Property PortName As String

        ''' <summary>
        ''' Kullanılacak baud rate değerini belirler.
        ''' </summary>
        Property BaudRate As Integer

        ''' <summary>
        ''' Bağlantı durumunu bildirir.
        ''' </summary>
        ReadOnly Property IsConnected As Boolean

        ''' <summary>
        ''' Son hata mesajını döndürür.
        ''' </summary>
        ReadOnly Property LastError As String

        ''' <summary>
        ''' Bekleyen satır sayısını döndürür.
        ''' </summary>
        ReadOnly Property PendingLines As Integer

        ''' <summary>
        ''' Varsayılan konfigürasyon değerlerini yükler.
        ''' </summary>
        Sub LoadConfigDefaults()

        ''' <summary>
        ''' Seri port bağlantısını açar.
        ''' </summary>
        Function Connect() As Boolean

        ''' <summary>
        ''' Seri port bağlantısını kapatır.
        ''' </summary>
        Sub Disconnect()

        ''' <summary>
        ''' Satır bazlı veri gönderir (satır sonu ekler).
        ''' </summary>
        Function WriteLine(data As String) As Boolean

        ''' <summary>
        ''' Ham string veri gönderir.
        ''' </summary>
        Function Write(data As String) As Boolean

        ''' <summary>
        ''' Byte dizisi gönderir.
        ''' </summary>
        Function WriteBytes(data() As Byte) As Boolean
    End Interface
End Namespace
