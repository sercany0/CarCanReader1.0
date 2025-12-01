# 🚀 CarCanReader Enterprise Yol Haritası
## Detaylı Teknik Spesifikasyon ve Prompt Rehberi

**Versiyon:** 1.0  
**Oluşturulma:** 29 Kasım 2024  
**Hedef:** MVP → Profesyonel → Enterprise

---

# 📋 İÇİNDEKİLER

1. [Kullanım Rehberi](#kullanım-rehberi)
2. [FAZA 0: Temel Düzeltmeler](#faza-0-temel-düzeltmeler)
3. [FAZA 1: MVP](#faza-1-mvp)
4. [FAZA 2: Profesyonel](#faza-2-profesyonel)
5. [FAZA 3: Enterprise](#faza-3-enterprise)
6. [Ek Kaynaklar](#ek-kaynaklar)

---

# 📖 KULLANIM REHBERİ

## Prompt Nasıl Kullanılır?

1. Her prompt `PROMPT:` başlığıyla işaretli
2. Prompt'u kopyala ve Cursor'a yapıştır
3. İşlem tamamlandığında "Tamam, test ettim, çalışıyor" veya hata bildir
4. Sonraki prompt'a geç

## Semboller

| Sembol | Anlam |
|--------|-------|
| 📁 | Yeni dosya oluşturulacak |
| 🔧 | Mevcut dosya düzenlenecek |
| ⚠️ | Dikkat edilmesi gereken |
| ✅ | Test kriterleri |
| 📦 | Gerekli NuGet paketi |

---

# FAZA 0: TEMEL DÜZELTMELER
**Süre:** 1 Hafta  
**Öncelik:** Kritik

## 0.1 Thread Safety Düzeltmeleri

### Teknik Spesifikasyon
- Tüm UI güncellemeleri `SafeInvoke` kullanmalı
- Serial port okuma ayrı thread'de olmalı
- ConcurrentQueue kullanılmalı
- Dispose pattern düzgün uygulanmalı

### PROMPT 0.1:
```
Thread safety ve kaynak yönetimi düzeltmelerini yap:

1. MainForm.vb'deki tüm UI güncellemelerini kontrol et:
   - Invoke/BeginInvoke eksik olanları bul
   - SafeInvoke helper kullanımını yaygınlaştır

2. Serial port yönetimini düzelt:
   - SerialPort.DataReceived handler'ı thread-safe olsun
   - Buffer okuma için ConcurrentQueue<string> kullan
   - Dispose pattern'i düzgün uygula (IDisposable)

3. Services klasöründeki tüm servislerde:
   - SyncLock eksik olanları ekle
   - Event raise ederken null check yap
   - Timeout mekanizmalarını ekle

4. Memory leak kontrolü:
   - Timer'lar düzgün dispose edilsin
   - Event handler'lar unsubscribe edilsin
   - Large object'ler için using pattern

Test kriterleri:
✅ Uygulama 1 saat kesintisiz çalışabilmeli
✅ Memory kullanımı sabit kalmalı (artmamalı)
✅ UI donmamalı, responsive kalmalı
```

---

## 0.2 Exception Handling Standardizasyonu

### Teknik Spesifikasyon
- Tüm public metodlar try-catch içermeli
- Boş catch blokları kaldırılmalı
- Merkezi hata loglama sistemi
- Kullanıcıya anlaşılır mesajlar

### PROMPT 0.2:
```
Exception handling sistemini standartlaştır:

1. Services/ErrorHandler.vb oluştur:
   - Singleton pattern
   - LogError(ex As Exception, context As String) metodu
   - LogWarning(message As String) metodu
   - Errors.log dosyasına yaz (data klasöründe)
   - Son 1000 hatayı bellekte tut
   - UI'da göstermek için event: OnErrorLogged

2. Tüm servislerde exception handling:
   - Boş catch bloklarını doldur
   - Her catch'te ErrorHandler.LogError çağır
   - Kritik hatalar için özel handling

3. Kullanıcı mesajları:
   - Teknik detayları gizle
   - Türkçe, anlaşılır mesajlar
   - "Teknik Detay" butonu ile log göster

4. MainForm'da hata gösterimi:
   - StatusBar'da son hata
   - Hata listesi penceresi (F12 ile aç)

Test kriterleri:
✅ Hiçbir exception kullanıcıya stack trace göstermemeli
✅ Tüm hatalar Errors.log'a yazılmalı
✅ Uygulama hata sonrası çökmemeli
```

---

## 0.3 Konfigürasyon Sistemi

### Teknik Spesifikasyon
- Tüm ayarlar merkezi config dosyasında
- Hardcoded değerler kaldırılmalı
- Runtime değiştirilebilir ayarlar
- Varsayılan değerler

### PROMPT 0.3:
```
Merkezi konfigürasyon sistemi oluştur:

1. Services/ConfigManager.vb oluştur:
   - data/config.json dosyası kullan
   - Singleton pattern
   - Atomic write (CommandRepository'deki gibi)
   
2. Konfigürasyon yapısı:
   {
     "serial": {
       "defaultBaudRate": 500000,
       "readTimeout": 1000,
       "writeTimeout": 1000,
       "bufferSize": 4096
     },
     "obd": {
       "requestTimeout": 2000,
       "retryCount": 3,
       "pollingInterval": 100
     },
     "ui": {
       "theme": "dark",
       "language": "tr",
       "logMaxLines": 5000,
       "dashboardRefreshRate": 100
     },
     "paths": {
       "logsFolder": "logs",
       "exportsFolder": "exports",
       "sessionsFolder": "sessions"
     }
   }

3. Koddaki tüm hardcoded değerleri ConfigManager'dan çek:
   - Timeout değerleri
   - Buffer boyutları
   - Dosya yolları
   - UI ayarları

4. Ayarlar penceresi (Forms/SettingsForm.vb):
   - Tab'lı arayüz (Bağlantı, OBD, Arayüz, Gelişmiş)
   - Varsayılana sıfırla butonu
   - Değişiklikler anında uygulanabilsin

Test kriterleri:
✅ config.json silinse varsayılanlarla oluşsun
✅ Ayar değişiklikleri restart gerektirmemeli
✅ Geçersiz değerler varsayılana dönmeli
```

---

# FAZA 1: MVP (Minimum Viable Product)
**Süre:** 6-8 Hafta  
**Hedef:** Hobi kullanıcıları için satılabilir ürün

---

## 1.1 DTC Veritabanı Sistemi

### Teknik Spesifikasyon

**Dosya Yapısı:**
```
data/
├── dtc/
│   ├── powertrain.json    (P0000-P3999)
│   ├── chassis.json       (C0000-C3999)
│   ├── body.json          (B0000-B3999)
│   └── network.json       (U0000-U3999)
```

**DTC Veri Modeli:**
```json
{
  "code": "P0301",
  "category": "Powertrain",
  "subcategory": "Misfire",
  "description_tr": "1. Silindir Ateşleme Hatası",
  "description_en": "Cylinder 1 Misfire Detected",
  "severity": "high",
  "symptoms": ["Titreşim", "Güç kaybı"],
  "causes": ["Buji arızası", "Enjektör arızası"],
  "solutions": ["Buji kontrolü", "Enjektör testi"]
}
```

### PROMPT 1.1.1:
```
DTC veritabanı altyapısını oluştur:

1. 📁 Models/DTCInfo.vb oluştur:
   Public Class DTCInfo
       Public Property Code As String
       Public Property Category As String        ' Powertrain, Chassis, Body, Network
       Public Property Subcategory As String
       Public Property DescriptionTR As String
       Public Property DescriptionEN As String
       Public Property Severity As String        ' low, medium, high, critical
       Public Property Symptoms As List(Of String)
       Public Property Causes As List(Of String)
       Public Property Solutions As List(Of String)
       Public Property IsPending As Boolean
       Public Property IsStored As Boolean
       
       ' Severity'ye göre renk
       Public ReadOnly Property SeverityColor As Color
       
       ' Tam açıklama string'i
       Public Function GetFullDescription(language As String) As String
   End Class

2. 📁 Services/DTCDatabase.vb oluştur:
   Public Class DTCDatabase
       ' Singleton pattern
       Private Shared _instance As DTCDatabase
       Public Shared ReadOnly Property Instance As DTCDatabase
       
       ' DTC dictionary: Code -> DTCInfo
       Private _dtcData As Dictionary(Of String, DTCInfo)
       
       ' Events
       Public Event OnDatabaseLoaded(count As Integer)
       Public Event OnDTCFound(dtc As DTCInfo)
       
       ' Methods
       Public Sub LoadDatabase()           ' Tüm JSON dosyalarını yükle
       Public Function GetDTC(code As String) As DTCInfo
       Public Function SearchDTC(keyword As String) As List(Of DTCInfo)
       Public Function GetByCategory(category As String) As List(Of DTCInfo)
       Public Function GetBySeverity(severity As String) As List(Of DTCInfo)
       
       ' İstatistikler
       Public ReadOnly Property TotalCount As Integer
       Public ReadOnly Property CategoryCounts As Dictionary(Of String, Integer)
   End Class

3. 📁 data/dtc/ klasörünü oluştur

4. OBDService.vb'yi güncelle:
   - DTC parse edildiğinde DTCDatabase'den açıklama al
   - DTCInfo objesi olarak döndür
   - OnDTCReceived event'ini güncelle

Test kriterleri:
✅ DTCDatabase.Instance.GetDTC("P0301") açıklama döndürmeli
✅ Arama çalışmalı: SearchDTC("ateşleme")
✅ Kategori filtreleme çalışmalı
```

### PROMPT 1.1.2:
```
DTC veritabanı içeriğini oluştur - Powertrain kodları:

1. 📁 data/dtc/powertrain.json oluştur

2. Aşağıdaki kategorilerde P kodlarını ekle:
   
   P0000-P0099: Yakıt ve Hava Ölçümü
   P0100-P0199: Yakıt ve Hava Ölçümü (devam)
   P0200-P0299: Enjeksiyon Sistemi
   P0300-P0399: Ateşleme Sistemi
   P0400-P0499: Egzoz Emisyon Kontrolü
   P0500-P0599: Hız ve Rölanti Kontrolü
   P0600-P0699: ECU ve Yardımcı Çıkışlar
   P0700-P0799: Şanzıman
   P0800-P0899: Şanzıman (devam)
   P0900-P0999: Şanzıman (devam)
   P1000-P1999: Üretici Özel Kodları (genel)
   P2000-P2999: Genel Powertrain
   P3000-P3999: Genel Powertrain (devam)

3. Her kod için Türkçe açıklama, belirti ve çözüm önerileri ekle

4. En az 500 P kodu ekle (en yaygın olanlar)

Format:
[
  {
    "code": "P0300",
    "category": "Powertrain",
    "subcategory": "Ateşleme",
    "description_tr": "Rastgele/Çoklu Silindir Ateşleme Hatası Algılandı",
    "description_en": "Random/Multiple Cylinder Misfire Detected",
    "severity": "high",
    "symptoms": ["Motor titremesi", "Güç kaybı", "Yakıt tüketimi artışı"],
    "causes": ["Buji arızası", "Bobin arızası", "Enjektör tıkanması", "Düşük yakıt basıncı"],
    "solutions": ["Bujileri kontrol et", "Bobinleri test et", "Enjektörleri temizle"]
  }
]

Test kriterleri:
✅ JSON dosyası parse edilebilmeli
✅ En az 500 P kodu olmalı
✅ Her kodda Türkçe açıklama olmalı
```

### PROMPT 1.1.3:
```
DTC veritabanı içeriğini genişlet - Diğer kategoriler:

1. 📁 data/dtc/chassis.json oluştur:
   C0000-C0999: Genel Şasi
   C1000-C1999: Üretici Özel
   C2000-C2999: ABS/Fren Sistemi
   C3000-C3999: Süspansiyon/Direksiyon
   - En az 200 kod

2. 📁 data/dtc/body.json oluştur:
   B0000-B0999: Genel Gövde
   B1000-B1999: Klima/Isıtma
   B2000-B2999: Aydınlatma/Göstergeler
   B3000-B3999: Güvenlik/Hırsız Alarm
   - En az 200 kod

3. 📁 data/dtc/network.json oluştur:
   U0000-U0999: CAN Haberleşme
   U1000-U1999: Üretici CAN
   U2000-U2999: Ağ İletişimi
   U3000-U3999: Yazılım Modülü
   - En az 100 kod

4. Tüm dosyalarda Türkçe açıklamalar olsun

Test kriterleri:
✅ Toplam 1000+ DTC kodu olmalı
✅ Her kategori ayrı dosyada
✅ DTCDatabase tümünü yükleyebilmeli
```

### PROMPT 1.1.4:
```
DTC veritabanını UI'a entegre et:

1. 🔧 Diagnostics tab'daki DTC listesini güncelle:
   - ListView yerine DataGridView kullan
   - Kolonlar: Kod, Açıklama, Kategori, Şiddet
   - Şiddete göre satır renklendirme
   - Çift tıklama ile detay penceresi

2. 📁 Forms/DTCDetailForm.vb oluştur:
   - DTC kodu ve açıklama (büyük font)
   - Belirtiler listesi
   - Olası nedenler listesi
   - Çözüm önerileri listesi
   - "Kopyala" butonu
   - "İnternette Ara" butonu (varsayılan tarayıcıda)

3. DTC arama özelliği:
   - Diagnostics tab'a arama kutusu ekle
   - Hem kod hem açıklama içinde arama
   - Anlık filtreleme (her tuşta)

4. Kategori filtresi:
   - ComboBox: Tümü, Powertrain, Chassis, Body, Network
   - Filtreleme hemen uygulanmalı

Test kriterleri:
✅ DTC'ler renkli gösterilmeli (kırmızı=critical, turuncu=high, vb.)
✅ Detay penceresi Türkçe içerik göstermeli
✅ Arama ve filtreleme çalışmalı
```

---

## 1.2 ISO-TP (Multi-Frame) Protokolü

### Teknik Spesifikasyon

**ISO 15765-2 Frame Types:**
```
Single Frame (SF):     0X = X byte veri (X: 1-7)
First Frame (FF):      1X XX = Toplam uzunluk, ilk 6 byte veri
Consecutive Frame (CF): 2X = Sequence number (0-F), 7 byte veri
Flow Control (FC):     3X YY ZZ = Flag, Block Size, STmin
```

**State Machine:**
```
IDLE -> SF received -> Complete
IDLE -> FF received -> Wait CF -> ... -> Complete
IDLE -> Sending FF -> Wait FC -> Sending CF -> ... -> Complete
```

### PROMPT 1.2.1:
```
ISO-TP protokol handler'ı oluştur:

1. 📁 Services/IsoTpHandler.vb oluştur:

   Public Class IsoTpHandler
       ' Frame tipleri
       Public Enum FrameType
           SingleFrame = 0
           FirstFrame = 1
           ConsecutiveFrame = 2
           FlowControl = 3
       End Enum
       
       ' Durum makinesi
       Private Enum State
           Idle
           ReceivingMultiFrame
           SendingMultiFrame
           WaitingFlowControl
       End Enum
       
       ' Yapılandırma
       Public Property BlockSize As Byte = 0        ' 0 = sınırsız
       Public Property STmin As Byte = 10           ' Frame arası bekleme (ms)
       Public Property Timeout As Integer = 1000    ' Genel timeout (ms)
       
       ' Events
       Public Event OnMessageReceived(sourceId As Integer, data As Byte())
       Public Event OnMessageSent(targetId As Integer, success As Boolean)
       Public Event OnError(message As String)
       Public Event OnProgress(current As Integer, total As Integer)
       
       ' Referanslar
       Private _canSender As CANSender
       Private _sourceId As Integer = &H7E8    ' ECU yanıt adresi
       Private _targetId As Integer = &H7DF    ' OBD broadcast
       
       ' Alım buffer'ı
       Private _rxBuffer As New List(Of Byte)
       Private _rxExpectedLength As Integer
       Private _rxSequence As Byte
       Private _currentState As State = State.Idle
       
       ' Metodlar
       Public Sub New(canSender As CANSender)
       Public Sub ProcessFrame(frameId As Integer, data As Byte())
       Public Async Function SendMessage(targetId As Integer, data As Byte()) As Task(Of Boolean)
       
       ' Internal
       Private Sub HandleSingleFrame(data As Byte())
       Private Sub HandleFirstFrame(data As Byte())
       Private Sub HandleConsecutiveFrame(data As Byte())
       Private Sub HandleFlowControl(data As Byte())
       Private Sub SendFlowControl()
       Private Function BuildSingleFrame(data As Byte()) As Byte()
       Private Function BuildFirstFrame(data As Byte(), totalLength As Integer) As Byte()
       Private Function BuildConsecutiveFrame(data As Byte(), sequence As Byte) As Byte()
   End Class

2. Frame parsing mantığı:
   - İlk nibble'a göre frame tipini belirle
   - SF: Veriyi hemen döndür
   - FF: Buffer'ı başlat, CF bekle
   - CF: Buffer'a ekle, sequence kontrol et
   - FC: Gönderim parametrelerini ayarla

3. Flow Control gönderimi:
   - FF alındığında otomatik FC gönder
   - CTS (Clear To Send) flag = 0
   - BlockSize ve STmin ayarlanabilir olsun

Test kriterleri:
✅ 8 byte'tan kısa mesajlar SF olarak işlenmeli
✅ 8+ byte mesajlar FF+CF olarak işlenmeli
✅ Sequence hataları loglanmalı
```

### PROMPT 1.2.2:
```
ISO-TP'yi OBD ve UDS servislerine entegre et:

1. 🔧 OBDService.vb güncelle:
   - IsoTpHandler instance'ı ekle
   - VIN okuma (Mode 09 PID 02) ISO-TP kullanmalı
   - Calibration ID okuma ISO-TP kullanmalı
   - Multi-frame DTC yanıtları işlenmeli

2. 🔧 AdvancedUdsEngine.vb güncelle:
   - Tüm UDS istekleri IsoTpHandler üzerinden gitsin
   - ReadDataByIdentifier (0x22) multi-frame desteği
   - WriteDataByIdentifier (0x2E) multi-frame desteği
   - RoutineControl (0x31) multi-frame desteği

3. VIN okuma testi:
   - Request: 09 02
   - Response: 49 02 01 XX XX XX ... (17+ byte VIN)
   - ISO-TP ile tam VIN alınmalı

4. Timeout ve retry mekanizması:
   - Her frame için timeout
   - 3 retry denemesi
   - Başarısızlıkta anlamlı hata mesajı

Test kriterleri:
✅ VIN tam 17 karakter olarak okunmalı
✅ Uzun DTC listeleri (10+ DTC) okunabilmeli
✅ Timeout durumunda hata döndürmeli
```

### PROMPT 1.2.3:
```
ISO-TP test aracı oluştur:

1. 📁 Forms/IsoTpTestForm.vb oluştur:
   - Hedef ECU ID seçimi (hex input)
   - Gönderilecek veri (hex string, uzun olabilir)
   - "Gönder" butonu
   - Gönderilen frame'ler listesi
   - Alınan frame'ler listesi
   - Birleştirilmiş yanıt (hex ve ASCII)
   - Progress bar (multi-frame için)

2. Debug özellikleri:
   - Her frame'i ayrı göster
   - Timing bilgisi (ms)
   - Frame tipi etiketi
   - Sequence numarası

3. Hazır test komutları:
   - VIN Oku (09 02)
   - Calibration ID (09 04)
   - Tüm DTC'leri Oku (03)
   - ECU Bilgisi (09 0A)

4. Ana menüye "Araçlar > ISO-TP Test" ekle

Test kriterleri:
✅ Manuel hex veri gönderilebilmeli
✅ Multi-frame yanıtlar birleştirilmeli
✅ Progress gösterilmeli
```

---

## 1.3 Bağlantı Wizard'ı

### Teknik Spesifikasyon

**Wizard Adımları:**
1. Hoşgeldin ekranı
2. Adaptör seçimi (COM port)
3. Araç bağlantısı (kontak açık mı?)
4. Protokol tespiti
5. Bağlantı testi
6. Tamamlandı

### PROMPT 1.3.1:
```
Bağlantı wizard formunu oluştur:

1. 📁 Forms/ConnectionWizard.vb oluştur:
   
   Public Class ConnectionWizard
       Inherits Form
       
       ' Wizard state
       Private _currentStep As Integer = 0
       Private _totalSteps As Integer = 5
       
       ' Toplanan bilgiler
       Public Property SelectedPort As String
       Public Property SelectedBaudRate As Integer
       Public Property DetectedProtocol As String
       Public Property ConnectionSuccessful As Boolean
       
       ' UI elemanları
       Private pnlWizardContent As Panel
       Private lblStepTitle As Label
       Private lblStepDescription As Label
       Private btnBack As Button
       Private btnNext As Button
       Private btnCancel As Button
       Private progressSteps As ProgressBar
       
       ' Her adım için ayrı panel/UserControl
       Private ucStep1_Welcome As WizardStep1
       Private ucStep2_PortSelect As WizardStep2
       Private ucStep3_VehicleCheck As WizardStep3
       Private ucStep4_ProtocolDetect As WizardStep4
       Private ucStep5_Complete As WizardStep5
   End Class

2. Modern, temiz tasarım:
   - Sol tarafta adım listesi (hangi adımda olduğu vurgulu)
   - Sağ tarafta içerik alanı
   - Alt tarafta Back/Next/Cancel butonları
   - Progress bar
   - Koyu tema (mevcut uygulamayla uyumlu)

3. Adım 1 - Hoşgeldin:
   - "CarCanReader'a Hoşgeldiniz"
   - Adaptör resmi/ikonu
   - "Bu wizard size bağlantı kurmanızda yardımcı olacak"
   - Gereksinimler listesi

4. Adım 2 - Port Seçimi:
   - Mevcut COM portları listesi
   - Her port için: İsim, açıklama, PID/VID (varsa)
   - "Yenile" butonu
   - "Otomatik Algıla" butonu (CANable imzasını ara)
   - Baud rate seçimi (varsayılan: 500000)

Test kriterleri:
✅ Wizard modal olarak açılmalı
✅ Portlar otomatik listelenmeli
✅ CANable varsa işaretlenmeli
```

### PROMPT 1.3.2:
```
Bağlantı wizard'ının devamını oluştur:

1. Adım 3 - Araç Kontrolü:
   - "Lütfen aracın kontağını açın (motor çalışmasa da olur)"
   - Animasyonlu bekleme göstergesi
   - "Hazırım, kontağı açtım" butonu
   - Timeout: 30 saniye bekleme

2. Adım 4 - Protokol Tespiti:
   - Otomatik protokol algılama başlat
   - Denenen protokoller listesi:
     □ CAN 500K (High Speed)
     □ CAN 250K (Low Speed)  
     □ CAN 125K
   - Her protokol için ✓ veya ✗ işareti
   - Başarılı olan yeşil vurgulu
   - Progress: "Protokol deneniyor: CAN 500K..."

3. Adım 5 - Tamamlandı:
   - Başarılı: Yeşil checkmark, "Bağlantı başarılı!"
   - Başarısız: Kırmızı X, "Bağlantı kurulamadı", sorun giderme önerileri
   - Bağlantı özeti:
     - Port: COM3
     - Baud Rate: 500000
     - Protokol: CAN HS
   - "Bağlantıyı Kaydet" checkbox'ı
   - "Bitir" butonu

4. Protokol algılama mantığı (Services/ProtocolDetector.vb):
   - Her protokol için SLCAN init sekansı dene
   - OBD-II 0100 PID'i gönder
   - Yanıt alınırsa protokol bulundu
   - 3 saniye timeout per protokol

Test kriterleri:
✅ Protokol otomatik algılanmalı
✅ Başarılı bağlantı kaydedilmeli
✅ Başarısız durumda yardımcı mesajlar gösterilmeli
```

### PROMPT 1.3.3:
```
Bağlantı wizard'ını MainForm'a entegre et:

1. 🔧 MainForm.vb güncelle:
   - İlk açılışta (kayıtlı bağlantı yoksa) wizard otomatik göster
   - Menü: "Bağlantı > Bağlantı Wizard'ı"
   - Toolbar'a wizard butonu ekle
   - "Bağlan" butonu başarısız olursa wizard öner

2. Son kullanılan bağlantı:
   - ConfigManager'da sakla: lastPort, lastBaudRate, lastProtocol
   - Sonraki açılışta otomatik bağlanmayı dene
   - Başarısız olursa wizard göster

3. Quick Connect özelliği:
   - Son başarılı bağlantıyı hatırla
   - "Hızlı Bağlan" butonu (wizard'sız)
   - 5 saniye timeout

4. Bağlantı durumu göstergesi:
   - StatusBar'da: "🟢 Bağlı (COM3, 500K)" veya "🔴 Bağlı Değil"
   - Bağlantı koptuğunda otomatik algıla
   - "Yeniden Bağlan" seçeneği sun

Test kriterleri:
✅ İlk açılışta wizard otomatik gelsin
✅ Son bağlantı hatırlansın
✅ Bağlantı kopmasını algılasın
```

---

## 1.4 Canlı Veri Grafikleri

### Teknik Spesifikasyon

**Gerekli NuGet Paketi:**
```
📦 Uygulamada zaten var: System.Windows.Forms.DataVisualization
```

**Grafik Türleri:**
- Line chart: RPM, Speed, Coolant Temp
- Real-time: Son 60 saniye veri
- Auto-scale Y axis
- Smooth scrolling

### PROMPT 1.4.1:
```
Dashboard'a canlı grafik sistemi ekle:

1. 📁 Controls/LiveChart.vb oluştur:
   Public Class LiveChart
       Inherits UserControl
       
       Private WithEvents chart As Chart
       Private _dataPoints As Queue(Of Tuple(Of DateTime, Double))
       Private _maxPoints As Integer = 600   ' 60 saniye x 10Hz
       Private _isPaused As Boolean = False
       
       ' Özellikler
       Public Property Title As String
       Public Property Unit As String
       Public Property MinValue As Double
       Public Property MaxValue As Double
       Public Property AutoScale As Boolean = True
       Public Property LineColor As Color = Color.Lime
       Public Property GridColor As Color = Color.FromArgb(50, 50, 50)
       
       ' Metodlar
       Public Sub AddDataPoint(value As Double)
       Public Sub Clear()
       Public Sub Pause()
       Public Sub Resume()
       Public Sub ExportToCsv(filePath As String)
       
       ' Chart styling
       Private Sub InitializeChart()
           ' Koyu tema
           ' Şeffaf arka plan
           ' Anti-aliased çizgiler
           ' Animasyonsuz (performans için)
       End Sub
   End Class

2. Grafik özellikleri:
   - X ekseni: Saat:Dakika:Saniye formatında
   - Y ekseni: Otomatik ölçekleme (min-max değerlere göre)
   - Arka plan: Koyu (mevcut tema ile uyumlu)
   - Çizgi: 2px kalınlık, anti-aliased
   - Grid: Hafif görünür, 10 bölüm

3. Performans optimizasyonu:
   - DoubleBuffered = True
   - Sadece görünür alanı çiz
   - Veri noktası limitli (son 600)
   - UI thread dışında veri işle

Test kriterleri:
✅ 10Hz yenileme hızında akıcı olmalı
✅ Memory leak olmamalı
✅ CPU kullanımı düşük olmalı
```

### PROMPT 1.4.2:
```
Dashboard tab'ına grafikleri entegre et:

1. 🔧 MainForm.Designer.vb - Dashboard tab'ı yeniden düzenle:
   
   Üst kısım (mevcut):
   - grpCanAnalysis (sol) - Araç verileri
   - grpOBD (sağ) - OBD sensörleri
   
   Alt kısım (yeni):
   - Grafik paneli (SplitContainer ile bölünmüş)
     - Sol: RPM grafiği
     - Orta: Hız grafiği
     - Sağ: Sıcaklık grafiği

2. 🔧 DashboardManager.vb güncelle:
   - LiveChart referansları ekle
   - Her veri güncellemesinde chart'a da gönder
   - chartRPM.AddDataPoint(rpm)
   - chartSpeed.AddDataPoint(speed)
   - chartTemp.AddDataPoint(coolantTemp)

3. Grafik kontrolleri:
   - "Duraklat/Devam" butonu
   - "Temizle" butonu
   - "Dışa Aktar (CSV)" butonu
   - Zaman aralığı seçici: 30s, 60s, 120s, 300s

4. Grafik efsanesi (legend):
   - Mevcut değer gösterimi
   - Min/Max değerler
   - Ortalama değer

Test kriterleri:
✅ Grafikler gerçek zamanlı güncellensin
✅ Duraklatma çalışsın
✅ CSV export çalışsın
```

---

## 1.5 Yardım ve Dokümantasyon Sistemi

### PROMPT 1.5.1:
```
In-app yardım sistemi oluştur:

1. 📁 Forms/HelpViewer.vb oluştur:
   - Split panel: Sol tarafta konu ağacı, sağ tarafta içerik
   - TreeView: Kategoriler ve alt konular
   - WebBrowser veya RichTextBox: HTML/RTF içerik
   - Arama kutusu (üstte)
   - Yazdır butonu

2. Yardım içeriği yapısı (data/help/ klasörü):
   help/
   ├── index.json          (konu yapısı)
   ├── getting-started.html
   ├── connection.html
   ├── obd-reading.html
   ├── dtc-clearing.html
   ├── coding.html
   ├── learning-engine.html
   ├── troubleshooting.html
   └── images/
       ├── connection-wizard.png
       └── ...

3. Konu ağacı:
   📘 Başlarken
      ├── Gereksinimler
      ├── Kurulum
      └── İlk Bağlantı
   📘 Temel Kullanım
      ├── Araç Bağlantısı
      ├── OBD Veri Okuma
      ├── Hata Kodu Okuma
      └── Hata Kodu Silme
   📘 Gelişmiş Özellikler
      ├── Coding Sistemi
      ├── Learning Engine
      └── CAN Analizi
   📘 Sorun Giderme
      ├── Bağlantı Sorunları
      ├── Veri Okuma Sorunları
      └── Sık Sorulan Sorular

4. Context-sensitive help:
   - F1 tuşuyla o an açık tab'a göre yardım aç
   - Her panelde "?" butonu, ilgili konuyu açar

Test kriterleri:
✅ F1 ile yardım açılmalı
✅ Arama çalışmalı
✅ Resimler görünmeli
```

### PROMPT 1.5.2:
```
Yardım içeriğini Türkçe olarak oluştur:

1. 📁 data/help/getting-started.html:
   - CarCanReader nedir?
   - Desteklenen adaptörler (CANable, vb.)
   - Sistem gereksinimleri
   - Kurulum adımları (ekran görüntüleriyle)

2. 📁 data/help/connection.html:
   - Adaptörü bağlama
   - Sürücü kurulumu
   - COM port bulma
   - Bağlantı wizard'ını kullanma
   - Sorun giderme

3. 📁 data/help/obd-reading.html:
   - OBD-II nedir?
   - Desteklenen PID'ler listesi
   - Canlı veri okuma
   - Veri kaydetme
   - Grafik kullanımı

4. 📁 data/help/dtc-clearing.html:
   - DTC nedir?
   - Hata kodlarını okuma
   - Kod anlamlarını öğrenme
   - Kodları silme (uyarılar)
   - Freeze frame verisi

5. 📁 data/help/troubleshooting.html:
   - "Bağlantı kurulamıyor" çözümleri
   - "Veri gelmiyor" çözümleri
   - "Hata kodu okunamıyor" çözümleri
   - Bilinen sorunlar ve çözümleri

Her HTML dosyasında:
- Adım adım talimatlar
- Ekran görüntüleri (placeholder olarak tanımla)
- İpuçları ve uyarılar
- İlgili konulara linkler

Test kriterleri:
✅ Tüm konular dolu olmalı
✅ Türkçe, anlaşılır dil
✅ Pratik örnekler içermeli
```

---

# FAZA 2: PROFESİYONEL
**Süre:** 8-10 Hafta  
**Hedef:** Oto elektrikçiler ve ileri kullanıcılar

---

## 2.1 ECU Tam Tarama Sistemi

### PROMPT 2.1.1:
```
Tam ECU tarama sistemi oluştur:

1. 📁 Services/EcuScanner.vb oluştur:
   Public Class EcuScanner
       ' ECU adres aralıkları
       Private Const ECU_START As Integer = &H700
       Private Const ECU_END As Integer = &H7FF
       
       ' Bilinen ECU tipleri
       Public Enum EcuType
           Engine
           Transmission
           ABS
           Airbag
           BodyControl
           Instrument
           Climate
           Steering
           Unknown
       End Enum
       
       ' ECU bilgi modeli
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
       End Class
       
       ' Events
       Public Event OnEcuFound(ecu As EcuInfo)
       Public Event OnScanProgress(current As Integer, total As Integer)
       Public Event OnScanComplete(ecus As List(Of EcuInfo))
       
       ' Methods
       Public Async Function ScanAllEcus() As Task(Of List(Of EcuInfo))
       Public Async Function GetEcuDetails(address As Integer) As Task(Of EcuInfo)
       Public Async Function ReadEcuDTCs(address As Integer) As Task(Of List(Of DTCInfo))
       
       ' ECU tanıma
       Private Function IdentifyEcuType(address As Integer, response As Byte()) As EcuType
   End Class

2. Tarama algoritması:
   - 0x700-0x7FF arasında TesterPresent (3E 00) gönder
   - Yanıt veren adresleri kaydet
   - Her ECU için ReadDID (22 F1 90) ile part number oku
   - ECU tipini response pattern'den tahmin et

3. Bilinen ECU adresleri veritabanı:
   - data/ecu-addresses.json
   - Marka/model bazlı bilinen adresler
   - ECU isimleri ve tipleri

Test kriterleri:
✅ Tüm online ECU'lar bulunmalı
✅ Her ECU için temel bilgi alınmalı
✅ Tarama 30 saniye içinde bitmeli
```

### PROMPT 2.1.2:
```
ECU tarama UI'ını oluştur:

1. 📁 Forms/EcuScannerForm.vb oluştur:
   - Sol panel: ECU listesi (TreeView)
     - Her ECU bir node
     - Alt node'lar: DTC, Info, Live Data
   - Sağ panel: Seçili ECU detayları
     - ECU adı ve adresi
     - Yazılım/Donanım versiyonu
     - Part number
     - DTC sayısı
     - "DTC Oku", "DTC Sil" butonları

2. Toolbar:
   - "Tara" butonu (ana tarama)
   - "Hızlı Tarama" (sadece bilinen adresler)
   - "Durdur" butonu
   - Progress bar
   - ECU sayacı: "5/12 ECU bulundu"

3. ECU ikonu sistemi:
   - Motor: 🔧
   - Şanzıman: ⚙️
   - ABS: 🛞
   - Airbag: 🎈
   - Gövde: 🚗
   - Diğer: 📦
   - Hatalı (DTC var): 🔴
   - Normal: 🟢

4. Ana menüye "Araçlar > ECU Tarama" ekle

Test kriterleri:
✅ TreeView'da ECU'lar görünmeli
✅ DTC olan ECU'lar işaretli olmalı
✅ Detay paneli bilgi göstermeli
```

---

## 2.2 PDF Rapor Sistemi

### PROMPT 2.2.1:
```
PDF rapor altyapısını oluştur:

📦 NuGet Paketi Ekle: iTextSharp veya PDFsharp

1. 📁 Services/ReportGenerator.vb oluştur:
   Public Class ReportGenerator
       ' Rapor bölümleri
       Public Class ReportData
           Public Property VehicleInfo As VehicleInfoModel
           Public Property ScanDate As DateTime
           Public Property ScannedEcus As List(Of EcuInfo)
           Public Property FoundDTCs As List(Of DTCInfo)
           Public Property LiveDataSnapshot As Dictionary(Of String, Double)
           Public Property TechnicianName As String
           Public Property CustomerName As String
           Public Property Notes As String
       End Class
       
       ' Şablon ayarları
       Public Property CompanyName As String
       Public Property CompanyLogo As Image
       Public Property CompanyAddress As String
       Public Property CompanyPhone As String
       
       ' Methods
       Public Function GenerateReport(data As ReportData, outputPath As String) As Boolean
       Public Function GenerateReportPreview(data As ReportData) As Image()
       
       ' Rapor bölümleri
       Private Sub AddHeader(doc As Document)
       Private Sub AddVehicleInfo(doc As Document, info As VehicleInfoModel)
       Private Sub AddDTCTable(doc As Document, dtcs As List(Of DTCInfo))
       Private Sub AddEcuSummary(doc As Document, ecus As List(Of EcuInfo))
       Private Sub AddLiveData(doc As Document, data As Dictionary)
       Private Sub AddFooter(doc As Document)
   End Class

2. Rapor şablonu:
   ┌────────────────────────────────────────┐
   │ [LOGO]  ARAÇ TANI RAPORU               │
   │         Firma Adı - Tel - Adres        │
   ├────────────────────────────────────────┤
   │ ARAÇ BİLGİLERİ                         │
   │ VIN: WVWZZZ3CZWE123456                 │
   │ Marka: Volkswagen  Model: Golf         │
   │ Yıl: 2018                              │
   ├────────────────────────────────────────┤
   │ TARANAN ECU'LAR                        │
   │ ✓ Motor ECU (0x7E0) - 2 DTC            │
   │ ✓ ABS (0x7E1) - Hata yok               │
   │ ...                                    │
   ├────────────────────────────────────────┤
   │ BULUNAN HATA KODLARI                   │
   │ P0301 - Silindir 1 Ateşleme Hatası     │
   │   Şiddet: Yüksek                       │
   │   Önerilen: Buji kontrolü              │
   │ ...                                    │
   ├────────────────────────────────────────┤
   │ CANLI VERİ (TARAMA ANI)                │
   │ Motor Devri: 850 RPM                   │
   │ Soğutucu Sıcaklık: 90°C                │
   │ ...                                    │
   ├────────────────────────────────────────┤
   │ Tarih: 29.11.2024   Teknisyen: ...     │
   │ Notlar: ...                            │
   └────────────────────────────────────────┘

Test kriterleri:
✅ PDF dosyası oluşabilmeli
✅ Türkçe karakterler doğru görünmeli
✅ Logo eklenebilmeli
```

### PROMPT 2.2.2:
```
PDF rapor UI'ını oluştur:

1. 📁 Forms/ReportForm.vb oluştur:
   - Tab 1: Araç Bilgileri
     - VIN (otomatik doldur veya manuel)
     - Marka/Model/Yıl
     - Plaka
     - Kilometre
   
   - Tab 2: Rapor İçeriği
     - Checkbox: ECU özeti ekle
     - Checkbox: DTC listesi ekle
     - Checkbox: Canlı veri ekle
     - Checkbox: Grafikler ekle
   
   - Tab 3: Firma Bilgileri
     - Logo yükle
     - Firma adı
     - Adres
     - Telefon
     - E-posta
   
   - Tab 4: Önizleme
     - PDF sayfa önizleme
     - Sayfa navigasyonu
   
   - Alt butonlar:
     - "Önizle"
     - "PDF Kaydet"
     - "Yazdır"
     - "E-posta Gönder" (varsayılan mail client)

2. Firma bilgilerini ConfigManager'a kaydet
   - Bir kez girilsin, hep hatırlansın

3. Rapor şablonu seçimi (gelecekte):
   - Standart
   - Detaylı
   - Müşteri özeti

Test kriterleri:
✅ Rapor önizleme çalışmalı
✅ PDF kaydedilmeli
✅ Türkçe karakterler doğru olmalı
✅ Yazdırma çalışmalı
```

---

## 2.3 Oturum Kayıt Sistemi

### PROMPT 2.3.1:
```
Oturum kayıt sistemi oluştur:

1. 📁 Services/SessionRecorder.vb oluştur:
   Public Class SessionRecorder
       ' Kayıt formatı
       Public Enum RecordFormat
           Binary      ' Küçük boyut, hızlı
           CSV         ' Okunabilir
       End Enum
       
       ' Kayıt durumu
       Public Property IsRecording As Boolean
       Public Property IsPaused As Boolean
       Public Property RecordedFrameCount As Long
       Public Property RecordingDuration As TimeSpan
       Public Property FilePath As String
       
       ' Events
       Public Event OnRecordingStarted(filePath As String)
       Public Event OnRecordingStopped(frameCount As Long)
       Public Event OnFrameRecorded(frameNumber As Long)
       Public Event OnPlaybackProgress(current As Long, total As Long)
       Public Event OnPlaybackFrame(timestamp As Long, frameId As Integer, data As Byte())
       
       ' Kayıt metodları
       Public Sub StartRecording(filePath As String, format As RecordFormat)
       Public Sub StopRecording()
       Public Sub PauseRecording()
       Public Sub ResumeRecording()
       Public Sub RecordFrame(frameId As Integer, data As Byte())
       
       ' Oynatma metodları
       Public Sub LoadSession(filePath As String)
       Public Sub StartPlayback(speed As Double)  ' 0.5x, 1x, 2x
       Public Sub PausePlayback()
       Public Sub StopPlayback()
       Public Sub SeekTo(frameNumber As Long)
       
       ' Dışa aktarma
       Public Sub ExportToCSV(outputPath As String)
       Public Sub ExportToASC(outputPath As String)  ' Vector CANalyzer formatı
   End Class

2. Binary kayıt formatı:
   Header:
   - Magic: "CCREC" (5 byte)
   - Version: 1 (1 byte)
   - StartTime: Unix timestamp (8 byte)
   - FrameCount: (8 byte)
   
   Her frame:
   - RelativeTime: ms (4 byte)
   - FrameId: (2 byte)
   - DLC: (1 byte)
   - Data: (8 byte)
   = 15 byte per frame

3. Performans hedefi:
   - 10.000 frame/saniye kayıt kapasitesi
   - Async yazma (buffer + flush)
   - Memory-mapped file opsiyonu

Test kriterleri:
✅ 1 saatlik kayıt 100MB'dan az olmalı
✅ Kayıt sırasında UI donmamalı
✅ Playback orijinal zamanlamayla çalışmalı
```

### PROMPT 2.3.2:
```
Oturum kayıt UI'ını oluştur:

1. 📁 Forms/SessionPlayerForm.vb oluştur:
   - Video player benzeri arayüz:
     - ▶ Play / ⏸ Pause / ⏹ Stop
     - ⏪ Geri sar / ⏩ İleri sar
     - Seek bar (timeline)
     - Hız seçici: 0.25x, 0.5x, 1x, 2x, 4x
     - Geçen süre / Toplam süre
   
   - Frame listesi:
     - DataGridView: Zaman, ID, DLC, Data
     - Çift tıklama ile o frame'e git
     - Filtreleme: ID bazlı
   
   - Bilgi paneli:
     - Kayıt tarihi/saati
     - Toplam süre
     - Toplam frame sayısı
     - Dosya boyutu

2. 🔧 MainForm'a kayıt kontrolleri ekle:
   - Toolbar'a: 🔴 Kaydet / ⏹ Durdur butonları
   - StatusBar'da: "Kaydediliyor: 00:05:23 (15,234 frame)"
   - Kayıt sırasında buton kırmızı yanıp sönsün

3. Kayıt başlatma dialog'u:
   - Dosya adı önerisi: "session_2024-11-29_14-30.ccrec"
   - Format seçimi: Binary / CSV
   - Kayıt klasörü seçimi

4. "Son Kayıtlar" menüsü:
   - Son 10 kayıt listesi
   - Hızlı erişim

Test kriterleri:
✅ Kayıt/playback akıcı çalışmalı
✅ Seek çalışmalı
✅ Hız değiştirme çalışmalı
```

---

## 2.4 Çoklu Dil Desteği

### PROMPT 2.4.1:
```
Çoklu dil altyapısını oluştur:

1. 📁 Localization/LocalizationManager.vb oluştur:
   Public Class LocalizationManager
       Private Shared _instance As LocalizationManager
       Public Shared ReadOnly Property Instance As LocalizationManager
       
       Private _currentLanguage As String = "tr"
       Private _strings As Dictionary(Of String, String)
       
       ' Desteklenen diller
       Public Shared ReadOnly Languages As New Dictionary(Of String, String) From {
           {"tr", "Türkçe"},
           {"en", "English"},
           {"de", "Deutsch"}
       }
       
       ' Events
       Public Event OnLanguageChanged(newLanguage As String)
       
       ' Methods
       Public Sub SetLanguage(langCode As String)
       Public Function GetString(key As String) As String
       Public Function GetString(key As String, ParamArray args() As Object) As String
       
       ' Kısayol
       Public Shared Function T(key As String) As String
       Public Shared Function T(key As String, ParamArray args() As Object) As String
   End Class

2. Dil dosyaları (JSON formatında):
   📁 data/lang/tr.json
   📁 data/lang/en.json
   📁 data/lang/de.json

3. String key yapısı:
   {
     "app": {
       "title": "CarCanReader",
       "version": "Versiyon {0}"
     },
     "menu": {
       "file": "Dosya",
       "connection": "Bağlantı",
       "tools": "Araçlar",
       "help": "Yardım"
     },
     "connection": {
       "connect": "Bağlan",
       "disconnect": "Bağlantıyı Kes",
       "status_connected": "Bağlı ({0}, {1})",
       "status_disconnected": "Bağlı Değil"
     },
     "dashboard": {
       "rpm": "Motor Devri",
       "speed": "Hız",
       "coolant_temp": "Soğutucu Sıcaklığı"
     },
     "errors": {
       "connection_failed": "Bağlantı kurulamadı: {0}",
       "timeout": "Zaman aşımı"
     }
   }

4. Tüm UI string'lerini key'e çevir (büyük iş!)
   - Aşamalı geçiş: Önce ana menü ve toolbar
   - Sonra form başlıkları
   - Sonra tüm label'lar

Test kriterleri:
✅ Dil değişimi anında yansımalı
✅ Eksik string için key göstermeli
✅ Format parametreleri çalışmalı
```

### PROMPT 2.4.2:
```
İngilizce dil dosyasını oluştur:

1. 📁 data/lang/en.json oluştur:
   - Tüm tr.json key'lerinin İngilizce karşılıkları
   - Profesyonel, teknik doğru terimler
   - OBD-II standart terminoloji

2. Önemli bölümler:
   - Menüler
   - DTC açıklamaları (en azından kategori başlıkları)
   - Hata mesajları
   - Yardım başlıkları

3. Dil seçim UI'ı:
   - Ayarlar > Dil sekmesi
   - Bayrak ikonlu dil listesi
   - "Yeniden başlatma gerekebilir" uyarısı

Test kriterleri:
✅ İngilizce UI tam çalışmalı
✅ DTC açıklamaları İngilizce görünmeli
✅ Menüler doğru çevrilmiş olmalı
```

---

## 2.5 Lisans ve Aktivasyon Sistemi

### PROMPT 2.5.1:
```
Offline lisans sistemi oluştur:

1. 📁 Services/LicenseManager.vb oluştur:
   Public Class LicenseManager
       ' Lisans seviyeleri
       Public Enum LicenseLevel
           Trial       ' 7 gün, sınırlı
           Basic       ' OBD okuma, DTC
           Professional ' + Coding, ECU tarama
           Enterprise  ' + Cloud, müşteri yönetimi
       End Enum
       
       ' Lisans bilgisi
       Public Class LicenseInfo
           Public Property Level As LicenseLevel
           Public Property LicenseKey As String
           Public Property HardwareId As String
           Public Property ExpiryDate As DateTime
           Public Property CustomerName As String
           Public Property CustomerEmail As String
           Public Property MaxVehicles As Integer  ' Enterprise için
           Public Property Features As List(Of String)
       End Class
       
       ' Donanım ID (benzersiz makine tanımlayıcı)
       Public Shared Function GetHardwareId() As String
           ' CPU ID + MAC Address + Disk Serial kombinasyonu
           ' SHA256 hash
       End Function
       
       ' Lisans doğrulama
       Public Function ValidateLicense(licenseKey As String) As LicenseInfo
       Public Function IsFeatureEnabled(featureName As String) As Boolean
       
       ' Lisans oluşturma (offline tool için)
       Public Shared Function GenerateLicense(hwId As String, level As LicenseLevel, expiry As DateTime) As String
   End Class

2. Lisans key formatı:
   XXXX-XXXX-XXXX-XXXX-XXXX
   - Base32 encoded
   - Hardware ID bağımlı
   - Seviye ve süre kodlanmış
   - Checksum içerir

3. Kısıtlama matrisi:
   Feature          | Trial | Basic | Pro | Enterprise
   ------------------|-------|-------|-----|------------
   OBD Okuma        | ✓     | ✓     | ✓   | ✓
   DTC Okuma        | ✓     | ✓     | ✓   | ✓
   DTC Silme        | ✗     | ✓     | ✓   | ✓
   Coding           | ✗     | ✗     | ✓   | ✓
   ECU Tarama       | ✗     | ✗     | ✓   | ✓
   PDF Rapor        | ✗     | ✗     | ✓   | ✓
   Oturum Kayıt     | ✗     | ✓     | ✓   | ✓
   Cloud Sync       | ✗     | ✗     | ✗   | ✓
   Müşteri Yönetimi | ✗     | ✗     | ✗   | ✓
   Araç Limiti      | 1     | 10    | ∞   | ∞

4. Trial özellikleri:
   - İlk çalıştırmadan 7 gün
   - Günlük kalan süre gösterimi
   - "Satın Al" butonu

Test kriterleri:
✅ Hardware ID tekrarlanabilir olmalı
✅ Lisans doğrulama çalışmalı
✅ Özellik kısıtlamaları çalışmalı
```

### PROMPT 2.5.2:
```
Lisans aktivasyon UI'ını oluştur:

1. 📁 Forms/ActivationForm.vb oluştur:
   - Mevcut lisans durumu gösterimi
   - Lisans seviyesi ve özellikler
   - Bitiş tarihi
   - Lisans key girişi (5x4 TextBox)
   - "Aktive Et" butonu
   - "Satın Al" butonu (web sitesine yönlendir)
   - Hardware ID gösterimi (kopyalanabilir)

2. Başlangıç kontrolü:
   - Uygulama açılışında lisans kontrol et
   - Trial bittiyse ActivationForm göster
   - Geçerli lisans varsa devam et

3. Trial uyarısı:
   - StatusBar'da: "Trial: 5 gün kaldı"
   - Son 3 gün için günlük popup
   - "Hatırlat" ve "Satın Al" butonları

4. Lisans bilgi penceresi:
   - Menü: Yardım > Lisans Bilgisi
   - Mevcut lisans detayları
   - Yükseltme seçenekleri

Test kriterleri:
✅ Aktivasyon çalışmalı
✅ Trial sayacı doğru çalışmalı
✅ Özellik kısıtlamaları uygulanmalı
```

---

# FAZA 3: ENTERPRISE
**Süre:** 10-12 Hafta  
**Hedef:** Profesyonel servisler

---

## 3.1 Cloud Sync Sistemi

### PROMPT 3.1.1:
```
Cloud sync altyapısını oluştur:

⚠️ NOT: Bu prompt backend API gerektirir. Önce basit mock/offline versiyon.

1. 📁 Services/CloudSyncService.vb oluştur:
   Public Class CloudSyncService
       ' API endpoint
       Private Const API_BASE As String = "https://api.carcanreader.com/v1"
       
       ' Sync durumu
       Public Property IsAuthenticated As Boolean
       Public Property LastSyncTime As DateTime
       Public Property SyncStatus As String
       
       ' Events
       Public Event OnSyncStarted()
       Public Event OnSyncProgress(current As Integer, total As Integer)
       Public Event OnSyncCompleted(success As Boolean)
       Public Event OnConflict(localItem As Object, remoteItem As Object)
       
       ' Auth
       Public Async Function Login(email As String, password As String) As Task(Of Boolean)
       Public Sub Logout()
       Public Async Function Register(email As String, password As String) As Task(Of Boolean)
       
       ' Sync metodları
       Public Async Function SyncVehicleProfiles() As Task
       Public Async Function SyncCommands() As Task
       Public Async Function SyncSessions() As Task
       Public Async Function FullSync() As Task
       
       ' Conflict resolution
       Public Enum ConflictResolution
           KeepLocal
           KeepRemote
           KeepBoth
           AskUser
       End Enum
       Public Property DefaultConflictResolution As ConflictResolution
   End Class

2. Offline-first yaklaşım:
   - Tüm değişiklikler önce lokalde
   - Bağlantı varsa arka planda sync
   - Çakışma varsa kullanıcıya sor

3. Sync edilecek veriler:
   - Araç profilleri
   - Özel komutlar
   - Oturum kayıtları (opsiyonel, büyük)
   - Ayarlar
   - Lisans bilgisi

4. Mock mod (API olmadan test için):
   - Lokalde "cloud" klasörüne kaydet
   - Sync simülasyonu
   - Geliştirme için yeterli

Test kriterleri:
✅ Offline çalışmalı
✅ Mock sync çalışmalı
✅ Conflict detection çalışmalı
```

---

## 3.2 Müşteri/Araç Yönetimi

### PROMPT 3.2.1:
```
Müşteri yönetim veritabanını oluştur:

📦 NuGet: System.Data.SQLite

1. 📁 Services/CustomerDatabase.vb oluştur:
   - SQLite veritabanı: data/customers.db
   
   Tablolar:
   
   Customers:
   - Id (INTEGER PRIMARY KEY)
   - Name (TEXT)
   - Phone (TEXT)
   - Email (TEXT)
   - Address (TEXT)
   - Notes (TEXT)
   - CreatedAt (DATETIME)
   - UpdatedAt (DATETIME)
   
   Vehicles:
   - Id (INTEGER PRIMARY KEY)
   - CustomerId (INTEGER FK)
   - VIN (TEXT UNIQUE)
   - Plate (TEXT)
   - Brand (TEXT)
   - Model (TEXT)
   - Year (INTEGER)
   - Mileage (INTEGER)
   - Notes (TEXT)
   - CreatedAt (DATETIME)
   
   ServiceHistory:
   - Id (INTEGER PRIMARY KEY)
   - VehicleId (INTEGER FK)
   - Date (DATETIME)
   - Mileage (INTEGER)
   - Description (TEXT)
   - DTCsFound (TEXT)      -- JSON array
   - DTCsCleared (TEXT)    -- JSON array
   - TechnicianName (TEXT)
   - Cost (DECIMAL)
   - Notes (TEXT)

2. Repository pattern:
   Public Class CustomerRepository
       Function GetAll() As List(Of Customer)
       Function GetById(id As Integer) As Customer
       Function Search(keyword As String) As List(Of Customer)
       Sub Add(customer As Customer)
       Sub Update(customer As Customer)
       Sub Delete(id As Integer)
   End Class
   
   Public Class VehicleRepository
       ' Benzer metodlar
       Function GetByCustomer(customerId As Integer) As List(Of Vehicle)
       Function GetByVIN(vin As String) As Vehicle
   End Class

3. Servis kaydı otomasyonu:
   - Tanı yapıldığında otomatik kayıt öner
   - VIN ile araç eşleştir
   - DTC'leri otomatik kaydet

Test kriterleri:
✅ CRUD işlemleri çalışmalı
✅ Arama çalışmalı
✅ İlişkiler doğru çalışmalı
```

### PROMPT 3.2.2:
```
Müşteri yönetim UI'ını oluştur:

1. 📁 Forms/CustomerManagerForm.vb oluştur:
   
   Sol panel (Master):
   - Arama kutusu
   - Müşteri listesi (DataGridView)
   - Yeni Ekle / Düzenle / Sil butonları
   
   Sağ panel (Detail):
   - Tab 1: Müşteri Bilgileri
     - Ad, Telefon, E-posta, Adres, Notlar
   - Tab 2: Araçları
     - Bu müşteriye ait araçlar listesi
     - Araç ekleme formu
   - Tab 3: Servis Geçmişi
     - Tüm ziyaretler listesi
     - Tarih, açıklama, bulunan hatalar
   
   Alt panel:
   - İstatistikler: Toplam ziyaret, son ziyaret, toplam tutar

2. Araç detay penceresi (popup):
   - Araç bilgileri
   - Servis geçmişi timeline
   - "Tanı Başlat" butonu (bu araçla)

3. Ana menüye "Müşteriler" ekle
   - Son müşteriler
   - Yeni müşteri
   - Tüm müşteriler

4. MainForm entegrasyonu:
   - Tanı tamamlandığında: "Servis kaydı oluştur?"
   - VIN okunduğunda: "Bu araç kayıtlı: [Müşteri adı]"

Test kriterleri:
✅ Müşteri CRUD çalışmalı
✅ Araç-müşteri ilişkisi çalışmalı
✅ Servis geçmişi görünmeli
```

---

## 3.3 Actuator Test Sistemi

### PROMPT 3.3.1:
```
Actuator test sistemi oluştur:

⚠️ UYARI: Bu özellik dikkatli kullanılmalı, motor çalışırken tehlikeli olabilir!

1. 📁 Services/ActuatorTester.vb oluştur:
   Public Class ActuatorTester
       ' Test tipleri
       Public Enum ActuatorType
           Injector
           IgnitionCoil
           FuelPump
           CoolingFan
           ACCompressor
           EGRValve
           PurgeValve
           IdleValve
           RelayTest
       End Enum
       
       ' Test durumu
       Public Enum TestState
           Idle
           Running
           Completed
           Failed
           Cancelled
       End Enum
       
       ' Events
       Public Event OnTestStarted(actuator As ActuatorType)
       Public Event OnTestProgress(step As Integer, total As Integer)
       Public Event OnTestCompleted(result As TestResult)
       Public Event OnSafetyWarning(message As String)
       
       ' Methods
       Public Function GetAvailableTests(brand As String, model As String) As List(Of ActuatorType)
       Public Async Function RunTest(actuator As ActuatorType, params As TestParams) As Task(Of TestResult)
       Public Sub CancelTest()
       
       ' Güvenlik kontrolleri
       Private Function CheckSafetyConditions() As Boolean
           ' Motor durmuş mu?
           ' Vites P/N'de mi?
           ' El freni çekili mi?
       End Function
   End Class

2. Test komutları veritabanı:
   📁 data/actuator-tests.json
   {
     "VW": {
       "Golf": {
         "Injector": {
           "mode": "UDS",
           "service": "31",
           "subfunction": "01",
           "routine": "FF00",
           "parameters": { "cylinder": 1-4 },
           "duration_ms": 1000
         }
       }
     }
   }

3. Güvenlik önlemleri:
   - Her test öncesi uyarı popup'ı
   - Motor devir kontrolü (0 olmalı)
   - Acil durdurma butonu
   - Test süresi limiti
   - Ardışık test arası bekleme

Test kriterleri:
✅ Güvenlik uyarısı göstermeli
✅ Test çalıştırılabilmeli (simülasyon)
✅ Acil durdurma çalışmalı
```

---

## 3.4 Servis Reset Sistemi

### PROMPT 3.4.1:
```
Servis bakım reset sistemi oluştur:

1. 📁 Services/ServiceResetManager.vb oluştur:
   Public Class ServiceResetManager
       ' Reset tipleri
       Public Enum ServiceType
           OilService
           BrakePads
           AirFilter
           CabinFilter
           SparkPlugs
           BatteryChange
           InspectionDue
       End Enum
       
       ' Reset prosedürü
       Public Class ResetProcedure
           Public Property ServiceType As ServiceType
           Public Property Brand As String
           Public Property Model As String
           Public Property YearFrom As Integer
           Public Property YearTo As Integer
           Public Property Steps As List(Of ResetStep)
           Public Property Notes As String
       End Class
       
       ' Reset adımı
       Public Class ResetStep
           Public Property StepNumber As Integer
           Public Property Description As String
           Public Property Action As String         ' "send", "wait", "confirm"
           Public Property Data As String           ' UDS komutu
           Public Property ExpectedResponse As String
           Public Property TimeoutMs As Integer
       End Class
       
       ' Methods
       Public Function GetAvailableResets(brand As String, model As String, year As Integer) As List(Of ServiceType)
       Public Function GetProcedure(serviceType As ServiceType, brand As String, model As String) As ResetProcedure
       Public Async Function ExecuteReset(procedure As ResetProcedure) As Task(Of Boolean)
   End Class

2. Reset prosedürleri veritabanı:
   📁 data/service-resets.json
   Marka/model bazlı prosedürler

3. Wizard tarzı UI:
   - Adım 1: Servis tipi seç
   - Adım 2: Araç bilgisi onayla
   - Adım 3: Prosedür adımlarını göster
   - Adım 4: Onay ve başlat
   - Adım 5: Sonuç

4. Popüler araç markaları için prosedürler:
   - VW/Audi (VAG)
   - BMW
   - Mercedes
   - Ford
   - Renault
   - Fiat

Test kriterleri:
✅ Prosedür wizard çalışmalı
✅ Reset komutu gönderilebilmeli
✅ Sonuç raporlanmalı
```

---

# EK KAYNAKLAR

## DTC Veritabanı Kaynakları
- OBD-II PIDs: https://en.wikipedia.org/wiki/OBD-II_PIDs
- DTC Listesi: https://www.obd-codes.com/
- SAE J2012: DTC Definitions

## ISO-TP Referansları
- ISO 15765-2 standardı
- CAN FD desteği (gelecek)

## UDS Referansları
- ISO 14229-1 (UDS)
- ISO 27145 (WWH-OBD)

## Geliştirme Araçları
- CANable: https://canable.io/
- SLCAN Protokolü: https://github.com/linux-can/can-utils
- OpenDBC: https://github.com/commaai/opendbc

---

# NOTLAR

## Her Oturumda Yapılacaklar
1. Önceki değişiklikleri test et
2. Yeni prompt'u ver
3. Sonucu test et
4. Sorunları bildir
5. Sonraki prompt'a geç

## Yedekleme
- Her major özellik sonrası Git commit
- `git commit -m "FAZA X.Y: Özellik adı tamamlandı"`

## Test Araçları
- OBD-II simülatör (ECUsim, FreeMatics)
- CAN bus analyzer
- Gerçek araç (dikkatli!)

---

**Son Güncelleme:** 29 Kasım 2024  
**Toplam Prompt Sayısı:** 25+  
**Tahmini Süre:** 6-8 Ay

