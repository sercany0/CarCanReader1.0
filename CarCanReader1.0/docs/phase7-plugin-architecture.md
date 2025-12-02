# Phase 7 – Plugin Altyapısı Tasarımı

Bu mimari, mevcut UI davranışını değiştirmeden komutların modüler olarak genişletilebilmesini sağlar. Üç temel yapıtaşı vardır: plugin discovery, command pipeline ve event aggregator.

## 1) Plugin Discovery
- **Komponent:** `Services.Plugins.CommandPluginLoader`
- **Görev:** `Plugins` klasöründeki DLL’lerde `ICommandPlugin` uygulamalarını bulur, örnekler ve kayıtlarını yapar.
- **Akış:**
  1. Dizin taranır (`*.dll`).
  2. Her DLL’de `ICommandPlugin` implementasyonları bulunur.
  3. Plugin örneklenir, `Metadata` kontrol edilir.
  4. `InitializeAsync` ile komut hattı ve event aggregator referansları verilir.
  5. Başarı/başarısızlık olayları (`PluginLoaded`, `PluginFailed`) tetiklenir.
- **Geri dönüş:** Yalnızca eklentiler takıldığında etkili; yüklenmezse mevcut davranış aynen sürer.

## 2) Command Pipeline
- **Komponentler:**
  - Arayüzler: `ICommandPipeline`, `ICommandPipelineStep`, `ICommandPlugin`
  - Veri kabı: `CommandEnvelope`
  - Sonuç modeli: `CommandPipelineResult`
  - Uygulama: `Services.Plugins.CommandPipeline`
- **Görev:** Plugin adımlarını middleware benzeri sırada çalıştırır; default terminal adım başarılı sonuç döner ve mevcut akışa dokunmaz.
- **Akış:**
  1. `CommandEnvelope` (komut adı + payload + etiketler) hazırlanır.
  2. Adımlar FIFO eklenir, LIFO çağrılır (middleware deseni).
  3. Her adım `InvokeAsync` ile devam eden adımı çağırır veya sonucu döner.
  4. Varsayılan terminal adım yalnızca `Success=True` döner; böylece pipeline bağlanmadıysa davranış değişmez.

## 3) Event Aggregator
- **Komponent:** `Services.Plugins.InMemoryEventAggregator`
- **Görev:** Plugin’lerin ve servislerin UI kablolarını değiştirmeden mesajlaşmasını sağlamak.
- **Akış:**
  - `Subscribe` ile handler eklenir, `Publish` ile snapshot alınır ve handler’lar çağrılır.
  - Hatalar `Debug.WriteLine` ile izlenir, UI akışı bozulmaz.

## Kullanım Notları
- UI kontrol isimlerine ve mevcut event imzalarına dokunulmaz.
- Plugin klasörü yoksa loader sessizce çıkar; mevcut davranış sürer.
- Komut hattı ve event aggregator, DI üzerinden mevcut servislere bağlanabilir; entegrasyon ayrı bir fazda yapılır.
