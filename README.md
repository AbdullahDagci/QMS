# QMS

Tek kurum için geliştirilen, modüler monolit mimarili elektronik kalite yönetim sistemi.

## Durum

Bu repository teknik iskelet ile M.01–M.09 çalışan kalite zincirini içerir:

- .NET 10 ASP.NET Core API.
- .NET background worker.
- Ayrı PostgreSQL migrator.
- EF Core + Npgsql ve ilk veritabanı migrasyonu.
- ASP.NET Core Identity veri modeli.
- Kalite kaydı, e-imza, audit trail ve outbox çekirdek tabloları.
- Sapma taslağı, atomik kayıt numarası, O × Ş × T risk hesabı ve sınıflandırma.
- Optimistic concurrency korumalı Sapma yaşam döngüsü ve kontrollü durum geçişleri.
- Kök neden araştırması, batch/seri etki ve serbest bırakma kararları.
- Açık araştırma, bekleyen batch kararı ve zorunlu DÖF için kapanış engelleri.
- Sapma oluşturma/gönderme audit olayları ve PostgreSQL migration'ı.
- M.02 DÖF aksiyon planlama, kanıt doğrulama, etkinlik ve kapanış yaşam döngüsü.
- M.03 Değişiklik Kontrol; paralel bölüm/ruhsat değerlendirmeleri, kurul, kanıtlı uygulama, ayrı devreye alma, uygulama sonrası doğrulama ve kontrollü geri dönüş.
- M.04 Doküman Yönetimi; majör/minör sürümleme, paralel inceleme, maker-checker onayı, zorunlu eğitim kapısı, elektronik okuma kanıtı ve numaralı kontrollü kopya izleme.
- M.03 → M.04 kaynak bağlantısı; değişiklik kaydından doğan doküman ve revizyonun uçtan uca izlenmesi.
- M.05 Eğitim Yönetimi; pozisyon matrisi, dokümana bağlı otomatik atama, okuma ve anlama imzası, sınav/pratik değerlendirme, eğitmen onayı, süreli kritik yeterlilik ve yenileme takibi.
- M.04 → M.05 yürürlük kapısı; onaylı doküman sürümünden ilgili pozisyondaki tüm etkin çalışanlara görev üretimi ve tüm yeterlilikler tamamlanana kadar yürürlüğü engelleme.
- M.06 Müşteri Şikâyetleri; kabul ve SLA, risk/FV triyajı, sürümlü ön ve nihai yanıt, paralel bölüm araştırmaları, ortak etki/kök neden ve kontrollü kapanış.
- M.06 → M.01/M.02/M.15 zinciri; majör-kritik ürün kalitesi şikâyetinden otomatik sapma, gerekli kök neden aksiyonundan DÖF ve sağlık verisini kopyalamayan farmakovijilans yönlendirmesi.
- M.07 İç Denetimler; yıllık ve gerekçeli plansız planlama, denetçi bağımsızlığı, sürümü kilitlenen soru listesi, kanıtlı uygulama, risk sınıflı bulgu, bölüm yanıtı ve kontrollü kapanış.
- M.07 → M.02 zinciri; majör-kritik denetim bulgusundan otomatik DÖF açılması, DÖF kapanmadan bulgunun ve tüm bulgular kapanmadan denetimin kapatılamaması.
- M.08 Dış Denetimler; otorite/müşteri bildirimi, M.04 bağlantılı kontrollü talep paketi, erişim günlüğü, resmi bulgu ve taahhüt, kapanış mektubu ve yetkili kapanışı.
- M.08 → M.02/M.04 zinciri; majör-kritik dış denetim bulgusundan otomatik DÖF, açık DÖF ile bulgu kapanış engeli ve yürürlükteki dokümanın alıcı/amaç/manifest kaydıyla dışa aktarılması.
- M.09 Tedarikçi Denetimi; kritik/performance/açık bulgu girdilerinden risk ve frekans hesabı, sürümü kilitlenen soru listesi, saha kanıtı, güvenli tek kullanımlık tedarikçi cevabı ve kapsam bazlı nitelendirme sonucu.
- M.09 → M.02/M.16 zinciri; majör-kritik bulgudan otomatik DÖF, kritik bulguda kapsamı otomatik askıya alma, açık DÖF ile kapanış engeli ve gelecekteki M.16 ara değerlendirme/yeniden nitelendirme tetikleyicisi.
- React sapma iş listesi, risk formu ve kontrollü gönderim işlemi.
- Global server-side pagination, whitelist sıralama, tip bazlı gelişmiş filtre ve aranabilir select standardı.
- React 19 + TypeScript + Vite 8 + MUI arayüz kabuğu.
- PostgreSQL 18, API, worker, migrator ve web için Docker dosyaları.
- Domain, application, architecture, integration ve frontend test başlangıçları.

Bu sürüm üretime hazır değildir. Gerçek e-imza yeniden doğrulaması, genel workflow yürütme, append-only veritabanı yetkileri ve validasyon paketi sonraki sertleştirme adımlarıdır.

## Belgeler

- [eQMS sistem analizi](./EQMS_SISTEM_ANALIZI.md)
- [Teknik mimari ve proje planı](./TEKNIK_MIMARI_VE_PROJE_PLANI.md)
- [Mimari karar kayıtları](./docs/adr/)

## Gereksinimler

- .NET SDK `10.0.302`.
- Node.js `24.x` ve npm `11.x`.
- PostgreSQL `18.x` veya Docker Engine + Docker Compose.

## Hızlı başlangıç — Docker

Ortam dosyasını oluşturun ve paylaşılan ortamlarda parolayı değiştirin:

```bash
cp .env.example .env
```

PostgreSQL'i başlatın:

```bash
docker compose -f deploy/docker-compose.yml up -d qms-postgres
```

Onaylı migrasyonu ayrı adım olarak çalıştırın:

```bash
docker compose -f deploy/docker-compose.yml --profile tools run --rm qms-migrator
```

Uygulamayı başlatın:

```bash
docker compose -f deploy/docker-compose.yml up --build -d qms-api qms-worker qms-web
```

Arayüz: `http://localhost:8080`  
Liveness: `http://localhost:8080/health/live`  
Readiness: `http://localhost:8080/health/ready`

## Yerel geliştirme

PostgreSQL bağlantısı varsayılan olarak şöyledir:

```text
Host=localhost;Port=5432;Database=qms;Username=qms_app;Password=qms_dev_password
```

Gerçek parola veya ortak ortam bağlantısı kaynak dosyaya yazılmamalı; environment variable kullanılmalıdır:

```bash
export ConnectionStrings__QmsDatabase='Host=localhost;Port=5432;Database=qms;Username=qms_app;Password=...'
```

Migrasyon ve API:

```bash
dotnet tool restore
dotnet run --project backend/src/Qms.Migrator/Qms.Migrator.csproj
dotnet run --project backend/src/Qms.Api/Qms.Api.csproj
```

Frontend:

```bash
cd frontend
npm ci
npm run dev
```

Vite, `/api` ve `/health` isteklerini `http://localhost:5008` adresindeki API'ye yönlendirir.

## Doğrulama komutları

Backend:

```bash
dotnet restore Qms.slnx
dotnet build Qms.slnx --no-restore
dotnet test Qms.slnx --no-build --no-restore
```

Frontend:

```bash
cd frontend
npm ci
npm run typecheck
npm run lint
npm run test
npm run build
```

## API başlangıç endpointleri

- `GET /api/v1/system/info`: tek kurum bilgisi ve 16 modül kataloğu.
- `GET /api/v1/deviations`: son 100 sapma kaydı.
- `POST /api/v1/deviations/search`: server-side sayfalama, sıralama ve gelişmiş kolon filtreleri.
- `POST /api/v1/deviations`: risk hesabıyla sapma taslağı oluşturur.
- `GET /api/v1/deviations/{id}`: sapma ayrıntısı.
- `GET /api/v1/deviations/{id}/details`: araştırma, batch etkisi, geçişler ve audit geçmişi.
- `POST /api/v1/deviations/{id}/submit`: sürüm kontrollü olarak taslağı iş akışına gönderir.
- `POST /api/v1/deviations/{id}/investigations`: tamamlanmış kök neden araştırması ekler.
- `POST /api/v1/deviations/{id}/batch-impacts`: batch/seri etki ve karar kaydı ekler.
- `POST /api/v1/deviations/{id}/transitions`: sunucu kurallarıyla kontrollü durum geçişi uygular.
- `POST /api/v1/documents/search`: M.04 sunucu sayfalaması, sıralaması ve tip bazlı kolon filtreleri.
- `POST /api/v1/documents`: M.03 bağlantılı veya bağımsız kontrollü doküman oluşturur.
- `GET /api/v1/documents/{id}/details`: sürüm, inceleme, eğitim, okuma, kopya ve audit zincirini döndürür.
- `POST /api/v1/documents/{id}/transitions`: inceleme, onay, eğitim, yürürlük, revizyon, geri çekme ve arşiv kapılarını uygular.
- `POST /api/v1/trainings/search`: M.05 sunucu sayfalaması, sıralaması ve veri türüne göre kolon filtreleri.
- `GET /api/v1/trainings/{id}/details`: katılım, imza, değerlendirme, yeterlilik, görev ve audit zincirini döndürür.
- `POST /api/v1/trainings/{id}/transitions`: atama, başlangıç, değerlendirme, eğitmen onayı, başarısızlık ve yenileme geçişlerini uygular.
- `GET|POST /api/v1/trainings/matrix`: pozisyon–eğitim matrisi kurallarını listeler veya oluşturur.
- `POST /api/v1/complaints/search`: M.06 sunucu sayfalaması, whitelist sıralaması ve veri türüne göre kolon filtreleri.
- `GET /api/v1/complaints/{id}/details`: araştırma, yanıt sürümleri, M.01/M.02/M.15 bağlantıları, görevler ve audit zincirini döndürür.
- `POST /api/v1/complaints/{id}/transitions`: triyaj, paralel araştırma, etki, nihai yanıt ve kapanış kapılarını uygular.
- `POST /api/v1/complaints/{id}/responses`: ön ve nihai müşteri yanıtlarını ayrı değiştirilemez sürümler halinde oluşturur.
- `POST /api/v1/internal-audits/search`: M.07 sunucu sayfalaması, whitelist sıralaması ve veri türüne göre kolon filtreleri.
- `GET /api/v1/internal-audits/{id}/details`: kilitli soru listesi, risk sınıflı bulgular, M.02 bağlantıları, görevler ve kronolojik denetim geçmişini döndürür.
- `POST /api/v1/internal-audits/{id}/transitions`: plan, bağımsızlık, uygulama, bulgu, DÖF doğrulama ve kapanış kapılarını uygular.
- `POST /api/v1/internal-audits/{id}/findings`: risk puanını sınıflandırır ve majör-kritik bulguda ilişkili M.02 DÖF kaydını atomik oluşturur.
- `POST /api/v1/external-audits/search`: M.08 sunucu sayfalaması, whitelist sıralaması ve tip bazlı gelişmiş kolon filtreleri.
- `GET /api/v1/external-audits/{id}/details`: kontrollü talep paketi, erişim günlüğü, resmi bulgular, M.02 bağlantıları, kapanış mektubu ve kronolojiyi döndürür.
- `POST /api/v1/external-audits/{id}/documents/{requestId}/export`: M.04 dokümanını alıcı, amaç, manifest kanıtı ve sürümle kontrollü dışa aktarır.
- `POST /api/v1/external-audits/{id}/findings`: dış denetim bulgusunu sınıflandırır ve majör-kritik bulguda ilişkili M.02 DÖF kaydını atomik oluşturur.
- `POST /api/v1/external-audits/{id}/closure-letter`: otorite/müşteri kapanış mektubu, kabul kararı ve dosya kanıtını kaydeder.
- `POST /api/v1/supplier-audits/search`: M.09 sunucu sayfalaması, whitelist sıralaması ve risk/durum/tarih bazlı gelişmiş kolon filtreleri.
- `GET /api/v1/supplier-audits/{id}/details`: risk hesabı, kilitli soru listesi, bulgular, M.02 bağlantıları, güvenli davetler, nitelendirme sonucu ve kronolojiyi döndürür.
- `POST /api/v1/supplier-audits/{id}/findings`: tedarikçi bulgusunu sınıflandırır; majör-kritik bulguda M.02 DÖF açar, kritik bulguda kapsamı askıya alır.
- `POST /api/v1/supplier-audits/{id}/invitations`: ikinci tenant oluşturmadan süreli, tek kullanımlık ve özeti saklanan tedarikçi cevap daveti üretir.
- `POST /api/v1/supplier-audit-invitations/respond`: geçerli tek kullanımlık token ile tedarikçi cevap ve kanıtını kabul eder.
- `POST /api/v1/supplier-audits/{id}/result`: kapsam bazlı onay/koşul/askı/ret ve yeniden nitelendirme kararını gerekçesiyle kaydeder.
- `GET /health/live`: uygulama process sağlığı.
- `GET /health/ready`: PostgreSQL dahil hazır olma kontrolü.
- `GET /openapi/v1.json`: yalnız development ortamında OpenAPI belgesi.

## Solution yapısı

```text
backend/src/Qms.Api             HTTP ve composition root
backend/src/Qms.Application     Use-case ve orkestrasyon
backend/src/Qms.Contracts       API sözleşmeleri
backend/src/Qms.Domain          İş kuralları ve aggregate'ler
backend/src/Qms.Infrastructure  EF Core, Identity ve kalıcılık
backend/src/Qms.Migrator        Kontrollü tek seferlik migrasyon
backend/src/Qms.Worker          Outbox, termin ve entegrasyon işleri
backend/tests                   Otomatik test projeleri
frontend                        React/Vite SPA
deploy                          Docker, Compose ve Nginx
docs/adr                        Mimari karar kayıtları
```

## Veritabanı

İlk migrasyon şu şemaları oluşturur:

- `identity`: kullanıcı, rol, claim, token.
- `organization`: tek kurum ayarları.
- `core`: kalite kaydı ve e-imza.
- `deviation`: sapma alan verileri, risk ve durum bilgisi.
- `capa`: DÖF kayıtları ve doğrulamalı aksiyonlar.
- `change_control`: değişiklik, paralel etki değerlendirmesi ve uygulama aksiyonları.
- `document`: kontrollü doküman, sürüm, inceleme, eğitim gerekliliği, okuma kanıtı ve kontrollü kopyalar.
- `training`: pozisyon matrisi, çalışan eğitim görevleri, değerlendirme denemeleri ve süreli yeterlilikler.
- `complaint`: müşteri şikâyeti, paralel araştırmalar ve sürümlü müşteri yanıtları.
- `internal_audit`: iç denetim planları, kilitli soru listesi ve risk sınıflı bulgular.
- `external_audit`: dış denetim, kontrollü doküman talepleri, erişim/dışa aktarım günlüğü ve resmi bulgular.
- `audit`: denetim olayları.
- `integration`: outbox mesajları.

Yeni migrasyon:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project backend/src/Qms.Infrastructure/Qms.Infrastructure.csproj \
  --startup-project backend/src/Qms.Migrator/Qms.Migrator.csproj \
  --output-dir Persistence/Migrations
```

## Güvenlik notları

- `.env` repository'ye eklenmez.
- Varsayılan geliştirme parolası yalnız yerel kullanım içindir.
- Üretim migrasyonu API başlangıcında otomatik çalışmaz.
- `latest` Docker etiketi kullanılmaz.
- Kullanıcı arayüzü yetki sınırı değildir; her yetki API'de doğrulanacaktır.
- Audit trail ile uygulama logları farklı amaçlara sahiptir.
