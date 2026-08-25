# ADR-004: Server-side liste sorgu standardı

## Durum

Kabul edildi.

## Karar

Tüm modül liste ekranları ortak bir sunucu sorgu sözleşmesi kullanır:

```json
{
  "page": 1,
  "pageSize": 25,
  "sortBy": "createdAtUtc",
  "sortDirection": "desc",
  "filters": [
    { "field": "status", "operator": "in", "values": ["Submitted", "Investigation"] }
  ]
}
```

Yanıt `items`, `page`, `pageSize`, `totalCount` ve `totalPages` alanlarını içerir. Sayfa numarası sunucuda 1 tabanlıdır. İzin verilen sayfa boyutları `10, 25, 50, 100` olarak sabittir.

Her modül filtrelenebilir ve sıralanabilir kolonlarını sunucuda whitelist eder. İstemciden gelen serbest kolon adı, SQL ifadesi veya dinamik sıralama ifadesi doğrudan sorguya aktarılmaz.

Veri tipine göre izin verilen temel operatörler:

- Metin: `contains`, `startsWith`, `equals`
- Sayı: `equals`, `greaterThanOrEqual`, `lessThanOrEqual`, `between`
- Tarih: `on`, `before`, `after`, `between`
- Enum: `equals`, `in`
- Boolean: `equals`

Liste endpointleri gelişmiş filtre gövdeleri için `POST /api/v1/{resource}/search` biçimini kullanır. Normal kaynak oluşturma endpointleriyle karışmaması için `/search` sabit alt yolu zorunludur.

Frontend tüm liste ekranlarında server-side pagination, server-side sorting, önceki sayfayı sorgu sırasında koruma ve ortak `SearchableSelect` bileşenlerini kullanır. Sabit enum listeleri istemcide aranır; büyüyen ana veri listeleri daha sonra ayrı server-side arama endpointlerine bağlanır.

## Sonuçlar

- Büyük veri setleri tarayıcı belleğine taşınmaz.
- Filtre davranışı modüller arasında tutarlı olur.
- Kolon ve operatör whitelist'i sorgu güvenliğini ve indeks planlamasını kontrol altında tutar.
- Yeni modül geliştirilirken liste sorgu sözleşmesi ve kolon metadata'sı tanımlanması kabul kriteridir.
