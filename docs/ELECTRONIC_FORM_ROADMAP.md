# Elektronik form ve parametrik çıktı motoru yol haritası

## Hedef

Teknik kullanıcıya ihtiyaç duymadan form tasarlamak; her doldurulan kaydı yayımlanmış form ve çıktı sürümüne sabitlemek; inceleme, elektronik imza, denetim izi ve doğrulanabilir PDF çıktısını aynı kalite kaydı zincirinde yönetmek.

## Faz 1 — Çalışan temel dilim (tamamlandı)

- Boş formdan veya hazır ekipman kontrol şablonundan form oluşturma.
- Bölüm ve alan ekleme, silme, sıralama; tek/çift sütun ve tam/yarım genişlik seçimi.
- Kısa/uzun metin, sayı, tarih, tarih-saat, tekli/çoklu seçim ve onay kutusu alanları.
- Zorunlu alan, koşullu görünürlük, koşullu zorunluluk, metin uzunluğu ve sayısal alt/üst sınırları.
- Sunucu tarafında şema/veri doğrulama; bilinmeyen ve gizli alan verisinin reddi veya ayıklanması.
- Taslak → inceleme → e-imzalı yayımlama; hazırlayan ile yayımlayan arasında görev ayrılığı.
- Yayımlanmış form ve çıktı sürümünün değiştirilememesi; yeni sürümü yayımdaki sürümden başlatma.
- Form doldurma, taslak kaydetme, onaya gönderme ve farklı kullanıcıyla e-imzalı kapatma.
- Kayıtların form sürümü, çıktı sürümü, form kodu ve form adı snapshot'ına sabitlenmesi.
- Ek/kanıt dosyaları, denetim izi, elektronik imza ve SHA-256/HMAC korumalı nihai PDF.
- Rol politikaları: görüntüleme, kullanma, tasarlama ve onaylama.

## Faz 2 — Gelişmiş alan kitaplığı

### 7 Eylül 2026 — Belge stüdyosu dilimi

- Tam ekran, sekmeli araç şeridi; kapatılabilir alan/özellik panelleri ve genişliğe sığan A4 yüzeyi.
- Sayfa üzerinde zengin metin düzenleme; seçili metne font, punto, kalın/italik/altı çizili ve renk uygulama.
- Boyut seçerek sabit tablo ekleme, hücrelerde yazma, hücre birleştirme/bölme, sütun genişliğini fare/klavye ile değiştirme.
- Alanı tabloya taşıma ve tablodan belgeye çıkarma; blok kimliği/biçimi korunarak belge akışında sıralama.
- Mevcut alanlardan etiket/değer form tablosu oluşturma.
- Geri/ileri alma; aynı sekmede sayfa yenilemeye karşı taslak kurtarma. Bu, sunucuya otomatik kayıt değildir.
- Metin biçimleri, birleşik hücreler ve sütun oranlarının şema doğrulaması, doldurma görünümü ve PDF üretimiyle bağlantısı.

Bu dilim tam Word eşdeğeri değildir. DOCX içe/dışa aktarma, görsel/logo araçları, tekrarlanan veri satırları ve tasarım yüzeyinde gerçek sayfalama henüz yoktur. Tasarım yüzeyi içerikle uzar; nihai PDF sayfalamasını PDF motoru yapar. Doldurma ön izlemesi birebir PDF provası değildir. Metin içi vurgu arka planı PDF'de henüz desteklenmez; hücre/blok arka planı desteklenir.

### Serbest yerleşim düzeltmesi

- Tutamak varsayılan olarak X/Y koordinatlarına taşır; sıralama ayrı moddur. Hareket zoom'dan bağımsız mm olarak kaydedilir ve tek geri alma adımıdır.
- İlk serbest taşımada geniş akış alanı taşınabilir kutuya dönüşür; sağ alt köşe veya X/Y/genişlik/yükseklik paneliyle düzenlenir.
- Konumlar A4'ün yazdırılabilir form gövdesi içinde sınırlıdır. Başlangıç: sol kenar boşluğu, üst kenar boşluğu + 34 mm başlık alanı. İlk sayfadaki metin/alan/tablo blokları desteklenir; sayfalar arası serbest çizim değildir.
- Doldurma görünümü ve PDF bu koordinatları korur. PDF'de kayıt bilgileri, imzalar ve denetim izi ayrı rapor ekindedir. İçerik kayıtlı kutuya sığmazsa kutuda devam sayfası notu gösterilir; tam içerik devam sayfalarında kayıpsız verilir. Sabit sürüme bağlı uzun cevaplar çıktı alınmasını engellemez.
- Escape, pencere odağı kaybı ve sayfa dışında bırakma taşımayı iptal eder; kenarda otomatik kaydırma vardır.

### Devam eden kapsam

- Tekrarlanan satır/tablo, dosya alanı, kullanıcı/bölüm seçimi, imza kutusu ve zengin metin.
- Formül alanları, toplam/ortalama, tolerans kontrolü ve uygun/uygun değil değerlendirmesi.
- Alan grupları, hazır alan blokları, kopyala-yapıştır ve sürükle-bırak sıralama.
- Taslak otomatik kaydetme, kayıp değişiklik uyarısı ve mobil doldurma ergonomisi.
- Şema değişiklik farkı ve geriye uyumluluk kontrolü.

Kabul kapısı: En az beş gerçek kurum formu kod yazmadan kurulmalı; masaüstü ve mobilde kullanıcı kabul testi tamamlanmalıdır.

## Faz 3 — Gelişmiş çıktı şablonu tasarımcısı

- Logo, üstbilgi, altbilgi, sayfa numarası, tablo ve imza blokları için görsel düzenleyici.
- Alanların çıktıdaki sırası/görünürlüğü, koşullu bloklar ve sayfa sonu kuralları.
- QR/barkod ve PDF üzerindeki kayıt doğrulama bilgisi.
- Mevcut kurumsal PDF formu üzerine koordinat bazlı alan yerleştirme seçeneği.
- Tasarım anında örnek verili PDF ön izleme ve kontrollü çıktı sürüm karşılaştırması.

Kabul kapısı: Aynı kayıt eski ve yeni çıktı sürümlerinde yeniden üretilememeli; daima kayda sabitlenmiş şablon sürümü kullanılmalıdır.

## Faz 4 — Parametrik iş akışı ve görevler

- Form bazında hazırlayan, inceleyen ve onaylayan rol/kullanıcı/bölüm kuralları.
- Çok adımlı veya paralel onay, iade–düzeltme ve iptal akışları.
- Son tarih, hatırlatma, eskalasyon, vekâlet ve görev kutusu entegrasyonu.
- Alan değerine göre yönlendirme; örneğin “uygun değil” sonucunda DÖF veya iş aksiyonu açma.
- Form sürümü yayımlanırken iş akışı tanımının aynı sürüme kilitlenmesi.

Kabul kapısı: Görev ayrılığı, vekâlet, gecikme ve iade senaryolarının tamamı denetim iziyle doğrulanmalıdır.

## Faz 5 — Yönetişim, arama ve validasyon

- Form devre dışı bırakma, arşiv, saklama süresi ve yasal hold.
- Form/kayıt içe–dışa aktarma, kontrollü şablon kütüphanesi ve kategori yönetimi.
- Alan bazlı arama, raporlama, trendler, zaman serileri ve CSV/XLSX veri dışa aktarımı.
- URS–risk–tasarım–test izlenebilirlik matrisi; IQ/OQ/PQ senaryoları ve kanıt paketi.
- Yük, erişilebilirlik, güvenlik, yedek/geri yükleme ve felaket kurtarma doğrulaması.

Kabul kapısı: Kurum kalite biriminin onayladığı validasyon paketi ve kontrollü canlıya geçiş kararı bulunmalıdır.

## Bilinçli ilk sürüm sınırları

- İlk sürümde form adı, kodu ve kategorisi ilk yayımdan sonra sabittir; sonraki sürümlerde alan şeması ile PDF ayarları değiştirilebilir.
- İş akışı şimdilik tek inceleme ve tek onay modelidir.
- Tekrarlanan tablo, hesaplama alanı ve mevcut PDF üzerine koordinat yerleştirme Faz 2–3 kapsamındadır.
