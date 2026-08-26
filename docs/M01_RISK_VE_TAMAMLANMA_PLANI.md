# M.01 Sapma Yönetimi — Risk ve Tamamlanma Planı

**Durum: TAMAMLANDI — 25 Ağustos 2026**

Bu belge M.01 tamamlanana kadar geliştirme kapsamını ve kabul kapılarını tanımlar. M.02 ve sonraki modüller bu kapılar kapanmadan geliştirme kapsamına alınmaz.

## Risk matrisi

| Öncelik | Risk | Mevcut önlem / durum | Tamamlanma ölçütü |
|---|---|---|---|
| Kritik | Organizasyon değişikliklerinin geçmiş aktör ve bölüm bilgisini değiştirmesi | Audit aktör adı snapshot; araştırma ve batch aktör snapshot’ları eklendi | Tüm M.01 karar ve görev kayıtları tarihsel kimlik/bölüm/rol bilgilerini değiştirilemez saklar |
| Kritik | Etkin görev sahibi olmadan genel role sahip kullanıcının işlem yapabilmesi | M.01 fallback erişimi kaldırıldı; açık atama zorunlu | Her mutasyon etkin görev veya geçerli delegasyon gerektirir |
| Kritik | Uygun görev sahibi bulunamadığında iş akışının sessiz ilerlemesi | Bölüm, sapma türü ve minimum RPN koşullu yönetilebilir görev matrisi uygulanıyor; eşleşme yoksa geçiş duruyor | Görev üretilemeyen hiçbir geçiş commit edilmez |
| Kritik | Maker-checker ihlali | Oluşturan kişi ana onay adımlarından engelleniyor | Tüm karar adımları ve delegasyon senaryoları test edilir |
| Yüksek | Batch/seri kararı ve kilit durumunun tutarsız olması | Pending karar kilit gerektiriyor; batch numarası tekil | Release/Hold/Reject kuralları ve yeniden değerlendirme yaşam döngüsü tamamlanır |
| Yüksek | Majör/kritik sapmanın DÖF olmadan kapanması | KG kapısı DÖF bağlantısını zorunlu kılıyor | Açık/eksik DÖF ile aksiyon ve kapanışın tüm yolları engellenir |
| Yüksek | Araştırma kanıtı yetersizken etki aşamasına geçilmesi | En az bir tamamlanmış araştırma zorunlu | Araştırmacı, yöntem, kök neden, sonuç ve kanıt bütünlüğü doğrulanır |
| Yüksek | Elektronik imza anlamının yetersiz kalması | İmza adımlarında parola ile yeniden kimlik doğrulama ve açık imza-anlamı kabulü zorunlu | Kimlik doğrulama, imza anlamı, zaman ve kayıt sürümü birlikte saklanır |
| Orta | Risk sınıflandırma eşiklerinin parametrik olmaması | Sabit O × Ş × T hesabı mevcut | Onaylı risk matrisi sürümü kayıt snapshot’ına alınır |
| Orta | Geciken kayıtların kontrolsüz kalması | Hedef tarih hesaplanıyor | Eskalasyon, bildirim ve gecikme gerekçesi izlenir |
| Orta | Arayüzün görev sahibini ve tarihsel aktörü yeterince ayırmaması | Görev kartları ve audit geçmişi mevcut | “Kayıt anı” snapshot’ları ve güncel görev sahibi açıkça gösterilir |

## M.01 tamamlanma kapıları

1. Domain kuralları ve geçiş matrisi eksiksiz test edilir.
2. Her yazma işlemi görev, delegasyon, rol ve maker-checker kontrolünden geçer.
3. Aktör, bölüm, görev ve karar snapshot’ları geçmişte değişmez.
4. Araştırma, batch/seri, DÖF, etkinlik ve kapanış bağımlılıkları sunucuda zorunludur.
5. Audit trail her mutasyonu sürüm, aktör, gerekçe ve korelasyon bilgisiyle içerir.
6. API hata kodları yetki, doğrulama, çakışma ve iş kuralı hatalarını doğru ayırır.
7. M.01 arayüzü yalnız atanmış kullanıcılara işlem kontrolü gösterir.
8. Domain, entegrasyon ve frontend testleri başarılıdır.
9. Docker ortamında uçtan uca örnek sapma başarıyla kapatılır ve negatif senaryolar engellenir.

## Tamamlanma kanıtı

- Aktif organizasyon bölümleri lookup olarak kullanılıyor; serbest metin bölüm kabul edilmiyor.
- Sapma türleri yönetim ekranından ekleniyor, sıralanıyor ve pasife alınıyor. Geçmiş sapmalardaki tür adı snapshot olarak korunuyor.
- Araştırmacı, batch değerlendiricisi ve görev sahibi kişi/bölüm snapshot'ları saklanıyor.
- Atama/delegasyon dışındaki kullanıcıların mutasyonları 403 ile engelleniyor; arayüz işlem kontrollerini yalnız görev sahibine gösteriyor.
- Maker-checker kuralı sistem yöneticisi dahil kayıt oluşturucusuna istisna tanımıyor.
- Görevler yönetilebilir M.01 matrisinden atanıyor; bölüm, sapma türü, minimum RPN ve öncelik kuralları destekleniyor. Alfabetik kullanıcı seçimi ve gizli fallback kaldırıldı.
- Onay adımları parola ile yeniden doğrulama ve açık anlam kabulü istiyor; imza anlamı, aktör snapshot'ı, kayıt sürümü, zaman ve SHA-256 içerik hash'i ile elektronik imzaya dönüşüyor.
- Batch/seri yeniden değerlendirmesi değiştirilemez karar geçmişi olarak ekleniyor ve geçişte en son karar esas alınıyor.
- Risk matrisi sürümü (`M01-RISK-1.0`) her sapmada snapshot olarak saklanıyor.
- Gerçek senaryo `SP-2026-000001` farklı görev sahipleriyle kapatıldı: 1 araştırma, 1 batch kararı, 10 audit olayı ve 3 elektronik imza.
- Negatif senaryoda atanmamış KG profili 403 aldı.
- Backend: 56/56 test; frontend: 10/10 test; lint: 0 hata; Docker sağlık kontrolü: 200.
- `20260825201026_AddM01AssignmentMatrix` migrasyonu canlı Docker veritabanına uygulandı; görev matrisi çalışan arayüzde dört varsayılan aktif kuralla doğrulandı.
