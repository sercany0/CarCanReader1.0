' EcuScanner.vb
' ECU (Electronic Control Unit) tarama ve bilgi toplama servisi
' 
' Özellikler:
'   - 0x700-0x7FF arası ECU tarama (TesterPresent)
'   - ECU bilgilerini okuma (ReadDID)
'   - ECU tipini otomatik tanıma
'   - Bilinen ECU adresleri veritabanı desteği
'
' Kullanım:
'   Dim scanner As New EcuScanner(canSender, udsEngine)
'   AddHandler scanner.OnEcuFound, AddressOf HandleEcuFound
'   Dim ecus = Await scanner.ScanAllEcus()

Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports CarCanReader1._0.Models
Imports CarCanReader1._0.Services

Namespace Services

    ''' <summary>
    ''' ECU tipi enum
    ''' </summary>
    Public Enum EcuType
        Engine = 0           ' Motor kontrol modülü
        Transmission = 1    ' Şanzıman kontrol modülü
        ABS = 2             ' ABS/Fren kontrol modülü
        Airbag = 3          ' Hava yastığı kontrol modülü
        BodyControl = 4      ' Gövde kontrol modülü
        Instrument = 5       ' Kombi/instrument panel
        Climate = 6          ' Klima kontrol modülü
        Steering = 7         ' Direksiyon kontrol modülü
        Unknown = 99         ' Bilinmeyen tip
    End Enum

    ''' <summary>
    ''' ECU bilgi modeli
    ''' </summary>
    Public Class EcuInfo
        Public Property Address As Integer
        Public Property ResponseAddress As Integer
        Public Property Name As String
        Public Property Type As EcuType
        Public Property IsOnline As Boolean
        Public Property DTCCount As Integer
        Public Property SoftwareVersion As String
        Public Property HardwareVersion As String
        Public Property PartNumber As String
        Public Property SerialNumber As String
        Public Property Manufacturer As String
        Public Property LastResponse As Byte()

        Public Sub New()
            Address = 0
            ResponseAddress = 0
            Name = "Unknown ECU"
            Type = EcuType.Unknown
            IsOnline = False
            DTCCount = 0
            SoftwareVersion = ""
            HardwareVersion = ""
            PartNumber = ""
            SerialNumber = ""
            Manufacturer = ""
            LastResponse = New Byte() {}
        End Sub
    End Class

    ''' <summary>
    ''' ECU tarama servisi
    ''' </summary>
    Public Class EcuScanner

#Region "Sabitler"

        ' ECU adres aralıkları (ISO-TP functional addressing)
        Private Const ECU_START As Integer = &H700
        Private Const ECU_END As Integer = &H7FF

        ' TesterPresent servis kodu
        Private Const SERVICE_TESTER_PRESENT As Byte = &H3E

        ' ReadDID servis kodu
        Private Const SERVICE_READ_DID As Byte = &H22

        ' Bilinen DID'ler (Data Identifier)
        Private Const DID_PART_NUMBER As Integer = &HF190
        Private Const DID_SOFTWARE_VERSION As Integer = &HF191
        Private Const DID_HARDWARE_VERSION As Integer = &HF192
        Private Const DID_SERIAL_NUMBER As Integer = &HF194

        ' Timeout değerleri (ms)
        Private Const SCAN_TIMEOUT As Integer = 200
        Private Const READ_DID_TIMEOUT As Integer = 500

        ' Veritabanı dosya yolu
        Private ReadOnly _databasePath As String

#End Region

#Region "Özel Alanlar"

        Private _canSender As CANSender
        Private _udsEngine As AdvancedUdsEngine
        Private _isotpHandler As IsoTpHandler
        Private _foundEcus As List(Of EcuInfo)
        Private _knownAddresses As Dictionary(Of Integer, EcuInfo)

#End Region

#Region "Olaylar (Events)"

        ''' <summary>
        ''' ECU bulunduğunda tetiklenir
        ''' </summary>
        Public Event OnEcuFound(ecu As EcuInfo)

        ''' <summary>
        ''' Tarama ilerlemesi güncellendiğinde tetiklenir
        ''' </summary>
        Public Event OnScanProgress(current As Integer, total As Integer)

        ''' <summary>
        ''' Tarama tamamlandığında tetiklenir
        ''' </summary>
        Public Event OnScanComplete(ecus As List(Of EcuInfo))

        ''' <summary>
        ''' Hata oluştuğunda tetiklenir
        ''' </summary>
        Public Event OnError(message As String)

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Yeni EcuScanner örneği oluşturur
        ''' </summary>
        ''' <param name="canSender">CAN gönderme servisi</param>
        ''' <param name="udsEngine">UDS motor servisi</param>
        Public Sub New(canSender As CANSender, udsEngine As AdvancedUdsEngine)
            _canSender = canSender
            _udsEngine = udsEngine
            _foundEcus = New List(Of EcuInfo)()
            _knownAddresses = New Dictionary(Of Integer, EcuInfo)()

            ' Veritabanı yolu
            Dim dataFolder = Path.Combine(Application.StartupPath, "data")
            If Not Directory.Exists(dataFolder) Then
                Directory.CreateDirectory(dataFolder)
            End If
            _databasePath = Path.Combine(dataFolder, "ecu-addresses.json")

            ' Bilinen adresleri yükle
            LoadKnownAddresses()
        End Sub

#End Region

#Region "Ana Tarama Metodları"

        ''' <summary>
        ''' Tüm ECU'ları tarar
        ''' </summary>
        ''' <returns>Bulunan ECU listesi</returns>
        Public Async Function ScanAllEcus() As Task(Of List(Of EcuInfo))
            Try
                _foundEcus.Clear()

                Dim totalAddresses = ECU_END - ECU_START + 1
                Dim currentAddress = 0

                ' 0x700-0x7FF arası tara
                For address As Integer = ECU_START To ECU_END
                    currentAddress += 1

                    ' İlerleme güncelle
                    RaiseEvent OnScanProgress(currentAddress, totalAddresses)

                    ' TesterPresent gönder
                    Dim ecu = Await TestEcuAddress(address)

                    If ecu IsNot Nothing AndAlso ecu.IsOnline Then
                        ' ECU bulundu, detayları oku
                        Await GetEcuDetails(ecu)
                        _foundEcus.Add(ecu)
                        RaiseEvent OnEcuFound(ecu)
                    End If

                    ' Kısa bekleme (CAN bus yükünü azalt)
                    Await Task.Delay(10)
                Next

                ' Tarama tamamlandı
                RaiseEvent OnScanComplete(_foundEcus)

                Return _foundEcus

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScanner.ScanAllEcus")
                RaiseEvent OnError("Tarama hatası: " & ex.Message)
                Return _foundEcus
            End Try
        End Function

        ''' <summary>
        ''' Belirli bir ECU adresini test eder (TesterPresent)
        ''' </summary>
        ''' <param name="address">Test edilecek ECU adresi</param>
        ''' <returns>ECU bilgisi (yanıt varsa), Nothing (yanıt yoksa)</returns>
        Public Async Function TestEcuAddress(address As Integer) As Task(Of EcuInfo)
            Try
                ' TesterPresent gönder (3E 00) - async olarak çalıştır
                Dim response = Await Task.Run(Function() _udsEngine.SendTesterPresent(address, False))

                If response.Status = UdsResponseStatus.Success Then
                    ' ECU yanıt verdi
                    Dim ecu As New EcuInfo()
                    ecu.Address = address
                    ecu.ResponseAddress = address + 8 ' Genellikle response = request + 8
                    ecu.IsOnline = True
                    ecu.Name = GetEcuNameFromAddress(address)
                    ecu.Type = IdentifyEcuType(address, response.Data)

                    ' Bilinen adreslerden kontrol et
                    If _knownAddresses.ContainsKey(address) Then
                        Dim knownEcu = _knownAddresses(address)
                        ecu.Name = knownEcu.Name
                        ecu.Type = knownEcu.Type
                        ecu.Manufacturer = knownEcu.Manufacturer
                    End If

                    Return ecu
                End If

                Return Nothing

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, $"EcuScanner.TestEcuAddress({address:X3})")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' ECU detaylarını okur (part number, version, vb.)
        ''' </summary>
        ''' <param name="ecu">Detayları okunacak ECU</param>
        Public Async Function GetEcuDetails(ecu As EcuInfo) As Task
            Try
                If ecu Is Nothing OrElse Not ecu.IsOnline Then
                    Return
                End If

                ' Part Number oku (F1 90) - async olarak çalıştır
                Await Task.Run(Sub()
                                   Try
                                       Dim partNumberResponse = _udsEngine.ReadDataByIdentifier(ecu.Address, DID_PART_NUMBER)
                                       If partNumberResponse.Status = UdsResponseStatus.Success AndAlso partNumberResponse.Data IsNot Nothing AndAlso partNumberResponse.Data.Length > 0 Then
                                           ecu.PartNumber = System.Text.Encoding.ASCII.GetString(partNumberResponse.Data).TrimEnd(Chr(0))
                                       End If
                                   Catch
                                       ' Part number okunamadı, devam et
                                   End Try
                               End Sub)

                ' Software Version oku (F1 91)
                Await Task.Run(Sub()
                                   Try
                                       Dim swVersionResponse = _udsEngine.ReadDataByIdentifier(ecu.Address, DID_SOFTWARE_VERSION)
                                       If swVersionResponse.Status = UdsResponseStatus.Success AndAlso swVersionResponse.Data IsNot Nothing AndAlso swVersionResponse.Data.Length > 0 Then
                                           ecu.SoftwareVersion = System.Text.Encoding.ASCII.GetString(swVersionResponse.Data).TrimEnd(Chr(0))
                                       End If
                                   Catch
                                       ' Software version okunamadı, devam et
                                   End Try
                               End Sub)

                ' Hardware Version oku (F1 92)
                Await Task.Run(Sub()
                                   Try
                                       Dim hwVersionResponse = _udsEngine.ReadDataByIdentifier(ecu.Address, DID_HARDWARE_VERSION)
                                       If hwVersionResponse.Status = UdsResponseStatus.Success AndAlso hwVersionResponse.Data IsNot Nothing AndAlso hwVersionResponse.Data.Length > 0 Then
                                           ecu.HardwareVersion = System.Text.Encoding.ASCII.GetString(hwVersionResponse.Data).TrimEnd(Chr(0))
                                       End If
                                   Catch
                                       ' Hardware version okunamadı, devam et
                                   End Try
                               End Sub)

                ' Serial Number oku (F1 94)
                Await Task.Run(Sub()
                                   Try
                                       Dim serialResponse = _udsEngine.ReadDataByIdentifier(ecu.Address, DID_SERIAL_NUMBER)
                                       If serialResponse.Status = UdsResponseStatus.Success AndAlso serialResponse.Data IsNot Nothing AndAlso serialResponse.Data.Length > 0 Then
                                           ecu.SerialNumber = System.Text.Encoding.ASCII.GetString(serialResponse.Data).TrimEnd(Chr(0))
                                       End If
                                   Catch
                                       ' Serial number okunamadı, devam et
                                   End Try
                               End Sub)

                ' DTC sayısını oku
                Try
                    Dim dtcs = Await ReadEcuDTCs(ecu.Address)
                    ecu.DTCCount = dtcs.Count
                Catch
                    ' DTC okunamadı, devam et
                End Try

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, $"EcuScanner.GetEcuDetails({ecu.Address:X3})")
            End Try
        End Function

        ''' <summary>
        ''' Belirli bir ECU'nun DTC'lerini okur
        ''' </summary>
        ''' <param name="address">ECU adresi</param>
        ''' <returns>DTC listesi</returns>
        Public Async Function ReadEcuDTCs(address As Integer) As Task(Of List(Of DTCInfo))
            Try
                Dim dtcs As New List(Of DTCInfo)()

                ' UDS Mode 03 (ReadDTCInformation - Stored DTCs)
                ' Bu özellik AdvancedUdsEngine'e eklenebilir veya OBDService kullanılabilir
                ' Şimdilik boş liste döndür
                ' TODO: UDS Mode 03 implementasyonu
                Await Task.Delay(1) ' Async uyarısını gidermek için

                Return dtcs

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, $"EcuScanner.ReadEcuDTCs({address:X3})")
                Return New List(Of DTCInfo)()
            End Try
        End Function

#End Region

#Region "ECU Tanıma"

        ''' <summary>
        ''' ECU tipini response pattern'den tahmin eder
        ''' </summary>
        ''' <param name="address">ECU adresi</param>
        ''' <param name="response">Yanıt verisi</param>
        ''' <returns>ECU tipi</returns>
        Private Function IdentifyEcuType(address As Integer, response As Byte()) As EcuType
            Try
                ' Bilinen adreslerden kontrol et
                If _knownAddresses.ContainsKey(address) Then
                    Return _knownAddresses(address).Type
                End If

                ' Adres aralığına göre tahmin et
                Select Case address
                    Case &H7E0 To &H7E7
                        ' Genellikle Engine/Transmission
                        Return EcuType.Engine
                    Case &H7E8 To &H7EF
                        ' Genellikle Engine response
                        Return EcuType.Engine
                    Case &H7A0 To &H7A7
                        ' Genellikle ABS
                        Return EcuType.ABS
                    Case &H7B0 To &H7B7
                        ' Genellikle Airbag
                        Return EcuType.Airbag
                    Case &H7C0 To &H7C7
                        ' Genellikle Body Control
                        Return EcuType.BodyControl
                    Case &H7D0 To &H7D7
                        ' Genellikle Instrument
                        Return EcuType.Instrument
                    Case &H7F0 To &H7F7
                        ' Genellikle Climate
                        Return EcuType.Climate
                    Case Else
                        Return EcuType.Unknown
                End Select

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScanner.IdentifyEcuType")
                Return EcuType.Unknown
            End Try
        End Function

        ''' <summary>
        ''' ECU adresinden isim oluşturur
        ''' </summary>
        ''' <param name="address">ECU adresi</param>
        ''' <returns>ECU ismi</returns>
        Private Function GetEcuNameFromAddress(address As Integer) As String
            Try
                ' Bilinen adreslerden kontrol et
                If _knownAddresses.ContainsKey(address) Then
                    Return _knownAddresses(address).Name
                End If

                ' Varsayılan isim
                Return $"ECU_{address:X3}"

            Catch ex As Exception
                Return $"ECU_{address:X3}"
            End Try
        End Function

#End Region

#Region "Veritabanı İşlemleri"

        ''' <summary>
        ''' Bilinen ECU adreslerini yükler
        ''' </summary>
        Private Sub LoadKnownAddresses()
            Try
                If Not File.Exists(_databasePath) Then
                    ' Veritabanı yoksa varsayılan oluştur
                    CreateDefaultDatabase()
                    Return
                End If

                Dim jsonContent = File.ReadAllText(_databasePath, System.Text.Encoding.UTF8)
                Dim json = JObject.Parse(jsonContent)

                If json("ecus") IsNot Nothing Then
                    For Each ecuJson As JObject In json("ecus")
                        Try
                            Dim address = Convert.ToInt32(ecuJson("address").ToString(), 16)
                            Dim ecu As New EcuInfo()
                            ecu.Address = address
                            ecu.Name = If(ecuJson("name")?.ToString(), "Unknown ECU")
                            ecu.Type = CType([Enum].Parse(GetType(EcuType), If(ecuJson("type")?.ToString(), "Unknown")), EcuType)
                            ecu.Manufacturer = If(ecuJson("manufacturer")?.ToString(), "")
                            ecu.PartNumber = If(ecuJson("partNumber")?.ToString(), "")

                            _knownAddresses(address) = ecu
                        Catch
                            ' Hatalı kayıt, atla
                        End Try
                    Next
                End If

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScanner.LoadKnownAddresses")
                ' Hata durumunda varsayılan veritabanı oluştur
                CreateDefaultDatabase()
            End Try
        End Sub

        ''' <summary>
        ''' Varsayılan ECU adresleri veritabanı oluşturur
        ''' </summary>
        Private Sub CreateDefaultDatabase()
            Try
                Dim defaultData As New JObject()
                Dim ecusArray As New JArray()

                ' Bilinen ECU adresleri (örnek)
                Dim knownEcus = New List(Of JObject) From {
                    New JObject(New JProperty("address", "7E0"), New JProperty("name", "Engine Control Module"), New JProperty("type", "Engine"), New JProperty("manufacturer", "Generic")),
                    New JObject(New JProperty("address", "7E1"), New JProperty("name", "Transmission Control Module"), New JProperty("type", "Transmission"), New JProperty("manufacturer", "Generic")),
                    New JObject(New JProperty("address", "7A0"), New JProperty("name", "ABS Control Module"), New JProperty("type", "ABS"), New JProperty("manufacturer", "Generic")),
                    New JObject(New JProperty("address", "7B0"), New JProperty("name", "Airbag Control Module"), New JProperty("type", "Airbag"), New JProperty("manufacturer", "Generic")),
                    New JObject(New JProperty("address", "7C0"), New JProperty("name", "Body Control Module"), New JProperty("type", "BodyControl"), New JProperty("manufacturer", "Generic")),
                    New JObject(New JProperty("address", "7D0"), New JProperty("name", "Instrument Cluster"), New JProperty("type", "Instrument"), New JProperty("manufacturer", "Generic")),
                    New JObject(New JProperty("address", "7F0"), New JProperty("name", "Climate Control Module"), New JProperty("type", "Climate"), New JProperty("manufacturer", "Generic"))
                }

                For Each ecu In knownEcus
                    ecusArray.Add(ecu)
                Next

                defaultData("ecus") = ecusArray

                ' Dosyaya yaz
                File.WriteAllText(_databasePath, defaultData.ToString(Formatting.Indented), System.Text.Encoding.UTF8)

            Catch ex As Exception
                ErrorHandler.Instance.LogError(ex, "EcuScanner.CreateDefaultDatabase")
            End Try
        End Sub

#End Region

#Region "Yardımcı Metodlar"

        ''' <summary>
        ''' Bulunan ECU sayısını döndürür
        ''' </summary>
        Public ReadOnly Property FoundEcuCount As Integer
            Get
                Return _foundEcus.Count
            End Get
        End Property

        ''' <summary>
        ''' Bulunan ECU listesini döndürür
        ''' </summary>
        Public ReadOnly Property FoundEcus As List(Of EcuInfo)
            Get
                Return _foundEcus
            End Get
        End Property

#End Region

    End Class

End Namespace

