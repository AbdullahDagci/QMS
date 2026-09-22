# Production kurulum ve işletim kılavuzu

Bu dağıtım tek kurumlu QMS kurulumu içindir. Canlıya geçiş, kurumun onaylı değişiklik yönetimi ve validasyon prosedürleri altında yürütülmelidir.

## İlk kurulum

1. `.env.example` dosyasını `.env` olarak kopyalayın ve alan adı, SMTP ve mutlak secret/TLS dosya yollarını doldurun.
2. Veritabanı sahibi ve uygulama runtime parolalarını ayrı dosyalara; en az 32 rastgele byte içeren Base64 HMAC anahtarını üçüncü bir erişimi kısıtlı dosyaya yazın. HMAC anahtarı kayıt ve rapor bütünlüğünün parçasıdır; kaybolması halinde geçmiş mühürler doğrulanamaz.
3. İlk production yöneticisinin e-posta/görünen adını ve güçlü başlangıç parolasının dosya yolunu tanımlayın. Bootstrap hesabı yalnız kullanıcı tablosu tamamen boşken oluşturulur ve olay audit izine yazılır. İlk girişten sonra profil menüsündeki “Parolayı değiştir” işlemiyle bu parolayı yenileyin; bootstrap secret dosyasını erişimi kısıtlı kurtarma kasasında tutun.
4. SMTP parola dosyası kullanılmayan bir relay için de boş olmayan, erişimi kısıtlı bir placeholder içermelidir; kullanıcı adı boşsa uygulama bu değeri kullanmaz.
5. `docker compose -f deploy/docker-compose.yml config --quiet` ile yapılandırmayı doğrulayın.
6. `docker compose -f deploy/docker-compose.yml up -d qms-postgres qms-clamav` komutundan sonra kontrollü migrasyonu çalıştırın:

   ```bash
   docker compose -f deploy/docker-compose.yml --profile tools run --rm qms-migrator
   ```

7. API, worker ve web servislerini başlatın. `https://HOST:8443/health/ready` başarılı olmadan trafiği açmayın.

Migrator; şema değişikliklerine ek olarak eski audit/e-imza kayıtlarını ve nihai PDF’leri HMAC ile mühürler, eski görevlerden kayıt erişim grant’lerini üretir ve runtime rolünün tablo yetkilerini uygular. Runtime rolünün audit, e-imza ve managed-file kayıtlarında update/delete yetkisi yoktur. Bütünlük uyuşmazlığında migrasyon durur; bu durum atlanmamalıdır.

## Secret ve sertifika yönetimi

- Secret dosyalarını repository’ye veya container imajına koymayın. Dosya izinlerini yalnız dağıtım hesabı okuyacak şekilde sınırlandırın.
- Veritabanı ve SMTP parolaları kurum politikasına göre döndürülebilir. PostgreSQL role parolası da owner hesabıyla değiştirilip secret dosyasıyla eşitlenmeli; ardından ilgili servisler kontrollü yeniden başlatılmalıdır.
- HMAC anahtarını rutin parola gibi doğrudan değiştirmeyin. Anahtar rotasyonu, eski anahtarın doğrulamada tutulduğu ayrı bir veri migrasyonu ve validasyon gerektirir.
- TLS sertifikası yenilendiğinde yalnız web servisini yeniden oluşturun; TLS 1.2/1.3 ve HSTS ayarlarını dış taramayla doğrulayın.

## İzleme ve alarm eşikleri

- `/health/live`: process ayakta mı; `/health/ready`: PostgreSQL ve zorunlu ClamAV tarayıcısı erişilebilir mi.
- Her HTTP yanıtındaki `X-Correlation-ID`, uygulama logu ve audit olayı arasında iz sürmek için kullanılır.
- `integration.outbox_message` içinde `dead-letter` kayıt sayısı sıfırdan büyükse alarm üretin.
- 24 saatte bir yeniden eskale edilen gecikmiş görevleri ve e-posta gönderim hatalarını worker loglarından izleyin.
- Disk kullanımını PostgreSQL, `qms-files` ve ClamAV imza volume’ları için ayrı alarm eşikleriyle takip edin.
- `audit-integrity` yönetim endpointini periyodik ve olay bazlı kontrollere dahil edin.

## Yedekleme ve geri dönüş

`deploy/backup.sh`, kısa bir bakım penceresinde yazan servisleri durdurur ve PostgreSQL ile dosya volume’unun tutarlı kopyasını alır:

```bash
BACKUP_DIRECTORY=/srv/qms-backups deploy/backup.sh qms-2026-09-04
```

Yedekleri çevrimdışı/immutable hedefe kopyalayın; HMAC anahtarı ve TLS private key’i aynı arşive koymayın. Geri dönüş yalnız yetkili bakım penceresinde gerçekleştirilmelidir:

```bash
QMS_RESTORE_CONFIRM=RESTORE_QMS deploy/restore.sh /srv/qms-backups/qms-2026-09-04
```

Geri dönüşten sonra readiness, örnek audit zinciri, e-imza doğrulaması, PDF HMAC kontrolü ve dosya indirme kontrolü yapılmadan kullanıcı trafiği açılmamalıdır. En az üç ayda bir izole ortamda restore tatbikatı ve ölçülen RTO/RPO kaydı tutulmalıdır.

Restore betiği uygulama servislerini açmadan önce migratorü çalıştırarak şema, bütünlük backfill’leri ve runtime rol yetkilerini yeniden uygular; bu adım başarısızsa servisler kapalı kalır.

## Olay müdahalesi

Bütünlük doğrulama hatasında kaydı yeniden üretmeyin veya yan dosyayı silmeyin. Sistemi salt-okunur operasyon moduna alın, correlation ID ve container loglarını koruyun, etkilenen volume snapshot’ını alın ve kalite/bilgi güvenliği sapma sürecini başlatın.
