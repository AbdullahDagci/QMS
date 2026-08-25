# ADR-001: Modüler monolit

**Durum:** Kabul edildi  
**Tarih:** 25 Ağustos 2026

## Bağlam

Sistem tek kuruma hizmet eder. Sapma, DÖF, aksiyon, e-imza ve audit olayları güçlü transaction tutarlılığı gerektirir. İlk ekip ve operasyon sınırı mikroservislerin dağıtık işlem ve işletim maliyetini haklı çıkarmamaktadır.

## Karar

Backend tek deploy edilebilir ASP.NET Core uygulaması olarak başlayacaktır. Domain modülleri namespace, katman, test ve PostgreSQL şemalarıyla ayrılır. Background işler aynı kod tabanındaki ayrı worker process'i tarafından yürütülür.

## Sonuçlar

- Kritik işlemler tek PostgreSQL transaction'ında tamamlanabilir.
- Validasyon, dağıtım ve hata analizi daha yalındır.
- Modül sınırlarının doğrudan tablo erişimiyle aşılmaması architecture testleriyle korunmalıdır.
- Gerçek bir bağımsız ölçek/yaşam döngüsü ihtiyacı oluşursa ilgili modül ileride ayrıştırılabilir.
