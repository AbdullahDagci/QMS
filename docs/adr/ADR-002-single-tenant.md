# ADR-002: Tek kurum ve tenant kimliği kullanılmaması

**Durum:** Kabul edildi  
**Tarih:** 25 Ağustos 2026

## Bağlam

Ürün multi-tenant olmayacaktır. Tek kurum içinde birden çok tesis, bölüm ve kullanıcı kapsamı bulunabilir.

## Karar

İş tablolarına `tenant_id` eklenmeyecektir. Kurum bilgisi tekil `organization_settings` kaydında tutulur. Görünürlük ve yetki; tesis, bölüm, rol ve gizlilik kapsamıyla yönetilir.

## Sonuçlar

- Veri modeli ve sorgular sadeleşir.
- Tenant izolasyonu iddiası veya altyapısı oluşmaz.
- Dış tedarikçi kullanıcıları ayrı tenant değil, sınırlı yetkili kimliklerdir.
- Geliştirme, validasyon ve üretim ortamlarının ayrılması yine zorunludur.
