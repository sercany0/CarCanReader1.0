# Phase 3 DI Taslak Akışı

Bu belge, yeni arayüzler eklenirken MainForm veya MainFormServiceBinder constructor akışına dokunmadan önce planlanan enjeksiyon yolunu özetler.

## Enjeksiyon Akışı Taslağı
- **MainFormServiceBinder**: Somut servisleri (`CANSender`, `SerialPortManager`, `CommandRepository`, `ConfigManager`) oluşturup arayüz tiplerine map edecek. MainForm tarafı bağlamı arayüzlerle alacak.
- **MainForm**: Constructor veya `InitializeServices` benzeri giriş noktalarında binder'dan gelen arayüzleri kullanacak; UI event kabloları ve kontrol isimleri korunacak.
- **ConnectionFlowManager**: `ISerialPortManager` ve `IConfigManager` üzerinden bağlantı durumunu yönetecek. Timer/timeout davranışı aynı kalacak.
- **DtcGridPresenter**: Komut verisini `ICommandRepository` üzerinden okuyacak; DataGridView referansı değişmeden kalacak.
- **ConfigChangeHandler**: `IConfigManager` event'lerine abone olacak; OBD interval güncelleme mantığı aynı kalacak.

Bu taslak, ileride yapılacak implementasyonlarda event imzalarını veya UI kontrol isimlerini değiştirmeden DI geçişini güvence altına almak için referans olarak kullanılacaktır.
