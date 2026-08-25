# ADR-005: Görsel Arayüz Standardı

## Durum

Kabul edildi — 25 Ağustos 2026

## Karar

Tüm QMS modülleri aynı güçlü fakat regülasyon odaklı görsel dili kullanır:

- Kayıt detaylarında gradient vurgulu başlık; kayıt türü, güncel aşama ve risk seviyesi birlikte gösterilir.
- Ana marka paleti kırık beyaz, sis mavisi, arduvaz ve adaçayı yeşili üzerinden kurulur; doygun renkler
  yalnızca risk, hata ve başarı gibi semantik durumlarda ölçülü kullanılır.
- Süreç yaşam döngüsü ayrı bir iş akışı yüzeyinde, tamamlanan/güncel/bekleyen adımlar ayrıştırılarak sunulur.
- Uzun kayıt detayları `Genel Bakış`, `Araştırma ve Etki`, `Karar ve Aksiyon`, `Geçmiş` sekmelerine ayrılır;
  modal başlığı ve sekme çubuğu kaydırma sırasında sabit kalır.
- Bilgi kartlarında ikon ve semantik renk kullanılır; aynı ağırlıkta düz kutu diziliminden kaçınılır.
- Bölüm başlıkları ikon, açıklama ve kayıt sayacı içerir.
- Boş durumlar yalnızca metin yerine ikonlu açıklayıcı yüzeylerle gösterilir.
- Renk tek başına anlam taşımaz; metin, ikon veya durum etiketiyle desteklenir.
- Hareketler kısa ve işlevsel kalır; denetlenebilir veriyi okumayı zorlaştıran dekorasyon kullanılmaz.
- Tüm yüzeyler tablet ve mobilde yatay taşma üretmeden yeniden düzenlenir.

## Ortak bileşenler

- `ModalHeader`: sabit başlık ve sağ üst kapatma aksiyonu.
- `AuditTimeline`: eskiden yeniye kronolojik denetim izi.
- İkonlu semantik bilgi kartları ve görsel boş durum kalıbı.
- Global ağ loader'ı ve tablo skeleton satırları.

## Gerekçe

Kalite sistemi yoğun ve ilişkili veri içerir. Güçlü görsel hiyerarşi; kullanıcının kayıt kimliği, risk,
güncel aşama ve bekleyen aksiyonu daha hızlı ayırt etmesini sağlar. Ortak standart, M.02 ve sonraki
modüllerde aynı kullanım alışkanlığının korunmasını güvence altına alır.
