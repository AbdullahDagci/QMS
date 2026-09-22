# QMS bilgisayarlı sistem validasyon planı

Bu plan teknik kabul kapılarını tanımlar; kurumun GMP/GxP kapsam değerlendirmesi, URS’i, SOP’leri ve kalite onayı yerine geçmez.

## İzlenebilirlik

Her gereksinim; benzersiz URS kimliği, risk sınıfı, tasarım/uygulama referansı, test vakası, beklenen sonuç, gerçekleşen sonuç, kanıt ve onaylayan ile izlenmelidir. Başarısız testler sapma kaydı ve kontrollü tekrar test gerekçesi olmadan kapatılamaz.

## IQ — Kurulum yeterliliği

- Onaylı .NET, Node, PostgreSQL, Nginx ve ClamAV sürümleri ile imaj kimliklerini kaydedin.
- Production profilinde eksik DB, AllowedHosts, HMAC, malware scanner veya SMTP ayarıyla başlangıcın başarısız olduğunu doğrulayın.
- TLS, HSTS, security headers, non-root/read-only container ve yalnız iç ağa açık PostgreSQL’i doğrulayın.
- Migrasyonların sıralı uygulandığını; audit, e-imza ve managed file append-only trigger’larının mevcut olduğunu kanıtlayın.
- Yedek alıp temiz/izole ortama restore edin; RPO/RTO sonucunu kaydedin.

## OQ — Operasyon yeterliliği

- Giriş, başarısız deneme kilidi, oturum iptali, CSRF, anonim endpoint rate limit ve production’da quick-profile reddini test edin.
- Her rol için pozitif ve negatif yetki matrisi; bölüm, oluşturan ve açık görev grant’i üzerinden satır kapsamını test edin.
- Gizli dokümanların yetkisiz listede, detayda, dosyada ve dışa aktarım seçeneklerinde görünmediğini doğrulayın.
- Her elektronik imza adımında parola yeniden doğrulaması, açık anlam kabulü, görev ayrılığı, kayıt sürümü ve HMAC doğrulamasını test edin.
- Audit zincirinde ekleme sırası, eşzamanlı yazma ve DB düzeyinde update/delete reddini test edin; kontrollü bir kopyada tamper algılamayı gösterin.
- Dosyalarda boyut, uzantı/MIME/magic-byte, ClamAV, path traversal, yetkili yükleme, SHA-256/HMAC ve saklama tarihi kontrollerini test edin.
- Outbox claim/lease, tekrar deneme, dead-letter ve gecikmiş görev eskalasyonunu SMTP kesintisi dahil test edin.
- M.01–M.16 durum makinelerinin tüm geçerli ve geçersiz geçişlerini; bağlantılı DÖF/doküman/eğitim kapılarını çalıştırın.
- Elektronik form şemalarında alan türü, sınır, koşullu görünürlük/zorunluluk, döngü ve bilinmeyen alan reddini doğrulayın.
- Yayımlanmış form/çıktı sürümü ile kapatılmış form kaydının hem API hem doğrudan veritabanı seviyesinde değiştirilemediğini kanıtlayın.
- Form hazırlayan–yayımlayan ve kayıt oluşturan–onaylayan görev ayrılığını; kayıtların doğru form ve çıktı sürümüne sabitlendiğini test edin.
- Nihai form PDF'sinde alan değerleri, e-imzalar, denetim izi, snapshot hash ve dosya bütünlüğü doğrulamasını çalıştırın.

## PQ — Performans yeterliliği

- Gerçekçi veri hacminde sayfalama/filtreleme ve eşzamanlı kullanıcı senaryoları için kurumca onaylı eşikleri ölçün.
- Her modülde gerçek iş rolü kullanıcılarıyla uçtan uca kayıt oluşturma, inceleme, onay, kapanış ve rapor alma senaryosu yürütün.
- Bildirim teslim sürelerini, gecikme eskalasyonlarını, büyük dosya yüklemeyi ve rapor indirmeyi temsilî ağ koşullarında ölçün.
- Tarayıcı, erişilebilirlik, saat dilimi ve Türkçe karakter kabulünü kullanıcı ortamında doğrulayın.

## Sürüm kararı

Canlıya geçiş için kritik/yüksek açık kusur bulunmamalı; kalan riskler kalite sahibi tarafından gerekçeli kabul edilmeli; test kanıtları, kaynak commit’i, imaj digest’leri, migrasyon çıktısı ve geri dönüş planı aynı sürüm paketinde imzalanmalıdır.
