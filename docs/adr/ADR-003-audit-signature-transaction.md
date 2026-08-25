# ADR-003: Kayıt, e-imza ve audit olayının tek transaction olması

**Durum:** Kabul edildi  
**Tarih:** 25 Ağustos 2026

## Bağlam

Bir kalite kaydının durumunun değişip imzasının veya audit olayının yazılmaması kabul edilemez. Aynı şekilde imza oluşup iş kaydının ilerlememesi de veri bütünlüğü hatasıdır.

## Karar

Kritik workflow geçişi; kayıt sürümü, görevler, e-imza, audit event ve outbox mesajını tek PostgreSQL transaction'ında yazar. İşlem optimistic concurrency ile beklenen kayıt sürümüne bağlanır.

## Sonuçlar

- Kısmi geçişler engellenir.
- E-posta veya dış entegrasyon transaction içinde gönderilmez; outbox worker tarafından idempotent işlenir.
- İmza, imzalanan kayıt sürümünün içerik özetiyle ilişkilendirilir.
- Audit tablosu için üretimde eklemeli veritabanı yetkileri uygulanmalıdır.
