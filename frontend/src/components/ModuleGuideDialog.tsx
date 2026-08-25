import { useEffect, useState, type ReactNode } from "react";
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogContent,
  IconButton,
  LinearProgress,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import {
  AccountTreeRounded,
  ArrowBackRounded,
  ArrowForwardRounded,
  AssignmentRounded,
  CalendarMonthRounded,
  ChatBubbleOutlineRounded,
  CheckCircleRounded,
  DescriptionRounded,
  FactCheckRounded,
  GroupsRounded,
  HealthAndSafetyRounded,
  InfoOutlined,
  Inventory2Rounded,
  LinkRounded,
  LockRounded,
  ManageSearchRounded,
  MarkEmailReadRounded,
  PauseRounded,
  PlayArrowRounded,
  PlaylistAddCheckRounded,
  ReplayRounded,
  RuleRounded,
  ScienceRounded,
  TaskAltRounded,
  UploadFileRounded,
  VerifiedRounded,
  WarningAmberRounded,
  MenuBookRounded,
} from "@mui/icons-material";
import { ModalHeader } from "./ModalHeader";

type ModuleCode =
  | "M.01"
  | "M.02"
  | "M.03"
  | "M.04"
  | "M.05"
  | "M.06"
  | "M.07"
  | "M.08"
  | "M.09";
type GuideStage = { title: string; text: string; role: string; guard: string };

const guides: Record<
  ModuleCode,
  {
    title: string;
    summary: string;
    recordNumber: string;
    recordTitle: string;
    stages: GuideStage[];
    rules: string[];
    connections: string[];
  }
> = {
  "M.01": {
    title: "Sapma Yönetimi ekran simülasyonu",
    summary:
      "Örnek bir sapmanın bildirimden kontrollü kapanışa kadar hangi ekranlardan ve hangi rollerden geçtiğini adım adım izleyin.",
    recordNumber: "SP-2026-000128",
    recordTitle: "Dolum sıcaklığı limit sapması",
    stages: [
      {
        title: "Taslak",
        text: "Sapma tanımı, beklenen durum, olay zamanı ve acil aksiyon aynı formda kaydedilir.",
        role: "Sapma bildiren",
        guard: "Zorunlu alanlar ve olay–tespit zamanı doğrulanır.",
      },
      {
        title: "Gönderim",
        text: "Olasılık, şiddet ve tespit edilebilirlik değerlerinden RPN otomatik hesaplanır.",
        role: "Sapma bildiren",
        guard: "Risk sınıfı ve DÖF zorunluluğu sistem tarafından belirlenir.",
      },
      {
        title: "Ön inceleme",
        text: "KG kaydın kapsamını, ilk aksiyonları ve araştırma gerekliliğini değerlendirir.",
        role: "İşlem yetkilisi",
        guard: "Kaydı oluşturan kullanıcı kendi kalite kararını veremez.",
      },
      {
        title: "Araştırma",
        text: "Araştırmacı yöntem, kök neden kategorisi, bulgu ve sonucu kayda bağlar.",
        role: "Araştırmacı",
        guard: "Yalnız kayda atanmış araştırmacı bu ekranı tamamlayabilir.",
      },
      {
        title: "Etki",
        text: "Etkilenen batch ve seriler ayrı satırlarda değerlendirilerek dispozisyon kararı verilir.",
        role: "Araştırmacı / KG",
        guard: "Kararı bekleyen batch varken sonraki aşamaya geçilemez.",
      },
      {
        title: "KG kararı",
        text: "Kalite değerlendirmesi, DÖF bağlantısı ve etkinlik gerekliliği birlikte karara bağlanır.",
        role: "Değerlendiren",
        guard: "Majör ve kritik sapma ilişkili DÖF olmadan ilerleyemez.",
      },
      {
        title: "Aksiyon",
        text: "Bağlı DÖF kayıtlarının aksiyon ve kapanış ilerlemesi sapma ekranından izlenir.",
        role: "İşlem yetkilisi",
        guard: "Açık ilişkili DÖF varken sapma aksiyon aşaması tamamlanamaz.",
      },
      {
        title: "Etkinlik",
        text: "Tekrar oluşum, başarı kriterleri ve gözlem sonucu kanıtla değerlendirilir.",
        role: "Değerlendiren",
        guard: "Etkisiz sonuç kapanışa gönderilemez; ek aksiyon gerekir.",
      },
      {
        title: "Kapanış",
        text: "Kontrol listesi, gerekçe ve görev ayrılığı kontrolü ardından kayıt kapatılır.",
        role: "Onaylayan",
        guard: "Açık bağımlılık veya eksik onay varsa kapanış engellenir.",
      },
    ],
    rules: [
      "Majör ve kritik sapmalar ilişkili DÖF olmadan ilerleyemez.",
      "Kararı bekleyen batch/seri varken KG değerlendirmesi tamamlanamaz.",
      "Açık DÖF veya bağımlılık varken sapma kapatılamaz.",
    ],
    connections: ["M.02 DÖF Yönetimi", "Doküman", "Eğitim", "Risk", "Denetim"],
  },
  "M.02": {
    title: "DÖF Yönetimi ekran simülasyonu",
    summary:
      "Örnek bir DÖF kaydının kapsam onayından aksiyon kanıtı, KG doğrulaması, etkinlik ve kapanışa kadar ilerleyişini izleyin.",
    recordNumber: "DÖF-2026-000064",
    recordTitle: "Dolum sıcaklık kontrolünün iyileştirilmesi",
    stages: [
      {
        title: "Taslak",
        text: "Problem, doğrulanmış kök neden, acil düzeltme, sorumlu ve hedef tarih tanımlanır.",
        role: "Kalite Güvence",
        guard:
          "Etkinlik gerekiyorsa yöntem ve başarı kriterleri baştan tanımlanır.",
      },
      {
        title: "Kapsam onayı",
        text: "DÖF kapsamının kök nedeni ve etkilenen süreci yeterince kapsayıp kapsamadığı incelenir.",
        role: "Onaylayan",
        guard: "Kaydı oluşturan kullanıcı kapsam onayını veremez.",
      },
      {
        title: "Kök neden onayı",
        text: "Kök neden kanıtları kontrol edilir ve aksiyon planlamaya geçiş kararı verilir.",
        role: "Onaylayan",
        guard: "Doğrulanmamış kök neden üzerinden aksiyon planlanamaz.",
      },
      {
        title: "Aksiyon planı",
        text: "Düzeltici ve önleyici aksiyonlara ayrı sorumlu, hedef tarih ve açıklama atanır.",
        role: "İşlem yetkilisi",
        guard: "En az bir aksiyon olmadan plan onayına gönderilemez.",
      },
      {
        title: "Plan onayı",
        text: "Sorumlular, tarihler ve etkinlik planı onayla birlikte sabitlenir.",
        role: "Onaylayan",
        guard: "Onay sonrasında değişiklikler yeni revizyon olarak izlenir.",
      },
      {
        title: "Uygulama",
        text: "Aksiyon sorumlusu tamamlanma kanıtını yükleyerek KG doğrulaması ister.",
        role: "Aksiyon sorumlusu",
        guard: "Tamamlandı bildirimi, KG doğrulaması anlamına gelmez.",
      },
      {
        title: "KG doğrulaması",
        text: "Kanıt ile planlanan aksiyon karşılaştırılır; uygun bulunur veya revizyona gönderilir.",
        role: "Değerlendiren",
        guard: "Tüm aksiyonlar doğrulanmadan süreç ilerleyemez.",
      },
      {
        title: "Etkinlik bekleme",
        text: "Onaylı gözlem süresi sistem tarafından takip edilir ve hedef gün görünür.",
        role: "Sistem / KG",
        guard: "Gözlem süresi dolmadan etkinlik değerlendirmesi açılamaz.",
      },
      {
        title: "Etkinlik kararı",
        text: "Başarı kriterleri ölçüm sonuçlarıyla karşılaştırılarak etkili/etkisiz kararı verilir.",
        role: "Değerlendiren",
        guard: "Etkisiz sonuç DÖF’ü aksiyon planlamaya geri döndürür.",
      },
      {
        title: "Kapanış",
        text: "Aksiyon, kanıt, etkinlik ve açık bağımlılık kontrolleri tamamlanarak DÖF kapatılır.",
        role: "Onaylayan",
        guard:
          "Maker-checker ve eksiksiz kanıt zinciri API tarafından doğrulanır.",
      },
    ],
    rules: [
      "Aksiyon sahibinin “tamamlandı” bildirimi KG doğrulaması yerine geçmez.",
      "Tüm aksiyonlar kanıtla tamamlanmadan doğrulama aşamasına geçilemez.",
      "Başarısız etkinlik sonucu DÖF’ü aksiyon planlamaya geri döndürür.",
    ],
    connections: [
      "M.01 Sapma Yönetimi",
      "Şikayet",
      "Denetim",
      "Değişiklik",
      "Doküman ve Eğitim",
    ],
  },
  "M.03": {
    title: "Değişiklik Kontrol ekran simülasyonu",
    summary:
      "Örnek bir proses değişikliğinin paralel bölüm ve ruhsat değerlendirmelerinden kurul kararına, kanıtlı uygulamaya, devreye alma ve nihai kapanışa ilerleyişini izleyin.",
    recordNumber: "DK-2026-000042",
    recordTitle: "Dolum hattı alarm parametresi değişikliği",
    stages: [
      {
        title: "Taslak",
        text: "Mevcut ve önerilen durum, gerekçe, kapsam, risk, etkilenen bölümler ve geri dönüş planı birlikte kaydedilir.",
        role: "Değişiklik başlatan",
        guard:
          "Geri dönüş planı ve en az bir etkilenen bölüm olmadan kayıt oluşturulamaz.",
      },
      {
        title: "Ön değerlendirme",
        text: "KG değişikliğin sınıfını, riskini, geçici/kalıcı niteliğini ve değerlendirme kapsamını doğrular.",
        role: "KG değerlendiricisi",
        guard:
          "Kaydı oluşturan kullanıcı kendi ön değerlendirmesini onaylayamaz.",
      },
      {
        title: "Paralel değerlendirme",
        text: "Üretim, KG, validasyon ve gerekiyorsa Ruhsatlandırma etkilerini eş zamanlı değerlendirir.",
        role: "Bölüm / Ruhsat",
        guard:
          "Tüm bölüm görüşleri uygun olmadan değişiklik kuruluna geçilemez.",
      },
      {
        title: "Kurul kararı",
        text: "Değişiklik kurulu fayda, risk, kaynak ve düzenleyici etkileri tek karar ekranında değerlendirir.",
        role: "Değişiklik kurulu",
        guard: "Reddedilen etki değerlendirmesi varken kurul onayı verilemez.",
      },
      {
        title: "Plan onayı",
        text: "Doküman, eğitim, validasyon, risk ve teknik uygulama aksiyonları sahip ve tarihle sabitlenir.",
        role: "Plan onaylayanı",
        guard: "En az bir uygulama aksiyonu olmadan plan onaylanamaz.",
      },
      {
        title: "Uygulama",
        text: "Aksiyon sorumluları kanıt yükler; KG her kanıtı planlanan çıktı ile karşılaştırarak doğrular.",
        role: "Aksiyon sahibi / KG",
        guard: "Doğrulanmamış aksiyon devreye alma kapısını kapalı tutar.",
      },
      {
        title: "Devreye alma",
        text: "Tamamlanan plan, otorite belgesi ve bağımlılıklar kontrol edilerek ayrı elektronik imzayla devreye alınır.",
        role: "Devreye alma onaylayanı",
        guard:
          "Devreye alma onayı nihai kapanış değildir; ayrı zaman damgası tutulur.",
      },
      {
        title: "Sonrası doğrulama",
        text: "Yeni durumun hedeflenen sonucu verdiği ve beklenmeyen etki oluşturmadığı kanıtla doğrulanır.",
        role: "KG değerlendiricisi",
        guard: "Başarısız sonuç kontrollü geri dönüş planını çalıştırır.",
      },
      {
        title: "Nihai kapanış",
        text: "Açık görev, doküman, eğitim, validasyon, risk ve olay bağımlılıkları son kez kontrol edilerek kayıt kapatılır.",
        role: "Kapanış onaylayanı",
        guard: "Devreye alma ve kapanış aynı kullanıcı ve aynı imza olamaz.",
      },
    ],
    rules: [
      "Devreye alma onayı ile nihai kapanış iki ayrı elektronik imzadır.",
      "Otorite onayı gereken değişiklik belge referansı olmadan devreye alınamaz.",
      "Başarısız uygulama kontrollü geri dönüşe ve gerekirse sapma kaydına yönlenir.",
    ],
    connections: [
      "M.02 DÖF",
      "M.04 Doküman",
      "M.05 Eğitim",
      "M.11 Risk",
      "MBR",
      "Tedarikçi",
    ],
  },
  "M.04": {
    title: "Doküman Yönetimi ekran simülasyonu",
    summary:
      "Bir SOP’nin taslaktan bölüm incelemelerine, onaya, zorunlu eğitime, kontrollü dağıtıma, revizyona ve arşive kadar ilerleyişini ekran ekran izleyin.",
    recordNumber: "DOC-2026-000031",
    recordTitle: "SOP-URT-014 · Dolum hattı temizlik prosedürü",
    stages: [
      {
        title: "Taslak",
        text: "Kod, tür, sahip, gizlilik, içerik, gözden geçirme periyodu ve M.03 kaynağı birlikte tanımlanır.",
        role: "Doküman kontrol",
        guard:
          "Doküman kodu benzersizdir; ilk sürüm ve değişiklik özeti zorunludur.",
      },
      {
        title: "Yazım",
        text: "Doküman sahibi kontrollü içeriği tamamlar ve sürüm özetini günceller.",
        role: "Doküman yazarı",
        guard:
          "İncelemeye gönderildikten sonra aynı sürümün içeriği değiştirilemez.",
      },
      {
        title: "İnceleme",
        text: "İlgili bölümler ve Kalite Güvence aynı sürümü paralel olarak değerlendirir.",
        role: "Bölüm inceleyicileri",
        guard: "Bekleyen veya revizyon isteyen görüş varken onaya geçilemez.",
      },
      {
        title: "Onay",
        text: "İncelemeleri tamamlanan sürüm görev ayrılığı kontrolüyle elektronik olarak onaylanır.",
        role: "Doküman onaylayanı",
        guard: "Dokümanı hazırlayan kullanıcı kendi sürüm onayını veremez.",
      },
      {
        title: "Eğitim kapısı",
        text: "Etkilenen pozisyonların eğitimleri ve sınav kanıtları sürüme bağlanır.",
        role: "Eğitim koordinatörü",
        guard: "Zorunlu eğitimler tamamlanmadan doküman yürürlüğe alınamaz.",
      },
      {
        title: "Yürürlük",
        text: "Planlanan tarihte onaylı sürüm yürürlüğe alınır; önceki sürüm otomatik olarak geçersizleşir.",
        role: "Kalite Güvence",
        guard: "Yalnız onaylı ve eğitim kapısı kapanmış sürüm yayımlanabilir.",
      },
      {
        title: "Dağıtım ve okuma",
        text: "Elektronik okuma kanıtları ile numaralı basılı kontrollü kopyalar izlenir.",
        role: "Kullanıcı / Doküman kontrol",
        guard:
          "Her kullanıcı ve kopya için değiştirilemez zaman damgası tutulur.",
      },
      {
        title: "Revizyon",
        text: "Majör veya minör yeni sürüm açılır; önceki içerik karşılaştırma için korunur.",
        role: "Doküman kontrol",
        guard:
          "Yürürlükteki sürüm doğrudan değiştirilemez; yeni revizyon zorunludur.",
      },
      {
        title: "Arşiv",
        text: "Yürürlükten kaldırılan doküman, tüm basılı kopyalar iade veya imha edildikten sonra arşivlenir.",
        role: "Doküman kontrol",
        guard: "Dağıtımda kontrollü kopya varken arşiv işlemi engellenir.",
      },
    ],
    rules: [
      "Onaylı veya yürürlükteki sürüm değiştirilemez; yeni revizyon açılır.",
      "Zorunlu pozisyon eğitimi tamamlanmadan yürürlük kapısı açılmaz.",
      "İade edilmemiş kontrollü kopya varken doküman arşivlenemez.",
    ],
    connections: [
      "M.03 Değişiklik Kontrol",
      "M.05 Eğitim",
      "M.07 Denetim",
      "M.11 Risk",
      "M.12 MBR",
      "Form ve Şablonlar",
    ],
  },
  "M.05": {
    title: "Eğitim Yönetimi ekran simülasyonu",
    summary:
      "Pozisyon matrisinden doğan veya M.04 sürüm onayıyla otomatik açılan bir görevin atama, okuma imzası, sınav, eğitmen onayı ve yenileme akışını izleyin.",
    recordNumber: "EGT-2026-000087",
    recordTitle: "SOP-URT-014 · Dolum hattı temizlik eğitimi",
    stages: [
      {
        title: "Matris",
        text: "Pozisyon için zorunlu eğitim, yöntem, geçme puanı, geçerlilik ve kritik yeterlilik tanımlanır.",
        role: "Eğitim koordinatörü",
        guard:
          "Aynı pozisyon ve eğitim kodu yalnız bir etkin matris kuralına sahip olabilir.",
      },
      {
        title: "Otomatik ihtiyaç",
        text: "M.04 doküman sürümü onaylandığında etkilenen pozisyon için eğitim görevi otomatik oluşturulur.",
        role: "Sistem / Doküman kontrol",
        guard: "Görev tamamlanmadan bağlı doküman yürürlüğe alınamaz.",
      },
      {
        title: "Atama",
        text: "Çalışan, pozisyon, hedef tarih, yöntem ve eğitmen denetlenebilir görev kaydında sabitlenir.",
        role: "Eğitim koordinatörü",
        guard:
          "Yalnız etkin çalışan atanabilir ve görev kayıt bazında yetkilendirilir.",
      },
      {
        title: "Katılım ve okuma",
        text: "Çalışan içeriği tamamlar; elektronik eğitimde okuma ve anlama beyanını imzalar.",
        role: "Katılımcı",
        guard: "Atanan çalışan dışındaki kullanıcı katılım kanıtı oluşturamaz.",
      },
      {
        title: "Değerlendirme",
        text: "Sınav puanı ve gerekiyorsa pratik yeterlilik kanıtı eğitmen tarafından kaydedilir.",
        role: "Eğitmen",
        guard:
          "Geçme puanı ve maksimum deneme sayısı matris kuralından uygulanır.",
      },
      {
        title: "Eğitmen onayı",
        text: "Başarılı değerlendirme ayrı bir yeterlilik onayıyla tamamlanır.",
        role: "Eğitmen / bölüm yöneticisi",
        guard: "Çalışan kendi yeterliliğini onaylayamaz.",
      },
      {
        title: "Yeterlilik",
        text: "Tamamlanan eğitim için geçerlilik sonu hesaplanır ve kritik iş yetkisi aktif hale gelir.",
        role: "Sistem",
        guard: "Kritik iş yalnız süresi geçerli yeterlilikle yürütülmelidir.",
      },
      {
        title: "Yenileme",
        text: "Süre dolduğunda veya yeni doküman revizyonunda yeni eğitim ihtiyacı açılır.",
        role: "Eğitim koordinatörü",
        guard:
          "Eski kanıt korunur; yenileme yeni görev ve yeni zaman damgasıdır.",
      },
    ],
    rules: [
      "M.04’e bağlı eğitim tamamlanmadan doküman yürürlük kapısı açılmaz.",
      "Katılımcı kendi yeterlilik onayını veremez.",
      "Başarısız denemeler silinmez; maksimum denemede görev başarısız olur ve yeniden atanır.",
    ],
    connections: [
      "M.04 Doküman Yönetimi",
      "M.03 Değişiklik Kontrol",
      "M.02 DÖF",
      "M.01 Sapma",
      "İK / Organizasyon",
      "M.07 Denetim",
    ],
  },
  "M.06": {
    title: "Müşteri Şikâyetleri ekran simülasyonu",
    summary:
      "Bir müşteri bildiriminin triyajdan paralel bölüm araştırmalarına, ön ve nihai yanıt onaylarına, M.01/M.02 ve farmakovijilans bağlantılarına kadar nasıl ilerlediğini izleyin.",
    recordNumber: "ŞK-2026-000031",
    recordTitle: "QMS Tablet 10 mg · kırık tablet bildirimi",
    stages: [
      {
        title: "Kabul",
        text: "Kanal, müşteri, ülke, ürün, batch, olay tarihi, açıklama, numune ve yanıt hedefleri tek kayıt altında alınır.",
        role: "Şikâyet bildiren",
        guard:
          "Olay tarihi, alınma tarihi ve yanıt SLA sırası API tarafından doğrulanır.",
      },
      {
        title: "Triyaj",
        text: "Önem, ürün kalitesi, sağlık etkisi, advers olay ve tekrar sinyali değerlendirilir.",
        role: "Şikâyet koordinatörü / KG",
        guard:
          "Majör-kritik ürün kalitesi şikâyeti M.01 sapma kaydını otomatik açar.",
      },
      {
        title: "Ön yanıt",
        text: "Müşteriye ilk kontrol ve inceleme bilgisini veren ön yanıt ayrı bir sürüm olarak hazırlanır ve onaylanır.",
        role: "Koordinatör / Onaylayan",
        guard: "Onaylı ön yanıt olmadan araştırmalar başlatılamaz.",
      },
      {
        title: "Paralel araştırma",
        text: "Üretim, Kalite Kontrol ve seçilen diğer bölümler kendi bulgu ve kök neden katkılarını eş zamanlı tamamlar.",
        role: "Bölüm araştırmacıları",
        guard:
          "Tüm bölüm araştırmaları tamamlanmadan etki değerlendirmesine geçilemez.",
      },
      {
        title: "Etki ve kök neden",
        text: "Ürün, batch, pazar ve tekrar etkisi birleştirilir; doğrulanmış ortak kök neden kaydedilir.",
        role: "Kalite Güvence",
        guard: "Bölüm bulguları korunur ve nihai karardan ayrı izlenir.",
      },
      {
        title: "Farmakovijilans",
        text: "Advers olay şüphesinde M.15 yönlendirmesi açılır; ürün ve batch dışında müşteri/sağlık anlatısı aktarılmaz.",
        role: "Farmakovijilans değerlendiricisi",
        guard: "Gizlilik sınırı aktarım verisini teknik olarak sınırlar.",
      },
      {
        title: "DÖF kararı",
        text: "Doğrulanmış kök neden veya trend sinyali için M.02 DÖF kaydı otomatik oluşturulur ve şikâyete bağlanır.",
        role: "Kalite Güvence",
        guard: "DÖF gerekli kararında ilişkili kayıt olmadan ilerlenemez.",
      },
      {
        title: "Nihai yanıt",
        text: "Araştırma sonucu, alınan aksiyon ve müşteri mesajı yeni bir nihai yanıt sürümünde hazırlanıp onaylanır.",
        role: "Koordinatör / Onaylayan",
        guard: "Onaylı yanıt sürümleri değiştirilemez; düzeltme yeni sürümdür.",
      },
      {
        title: "Kapanış",
        text: "Nihai yanıt iletimi, farmakovijilans aktarımı ve kayıt bağlantıları kontrol edilerek şikâyet kapatılır.",
        role: "Onaylayan",
        guard:
          "Onaylı nihai yanıt veya gerekli FV aktarımı eksikse kapanış engellenir.",
      },
    ],
    rules: [
      "Ön ve nihai müşteri yanıtları ayrı, sürümlü ve onaylı kayıtlardır.",
      "Tüm paralel araştırmalar bitmeden ortak etki/kök neden kararı verilemez.",
      "Farmakovijilans aktarımı müşteri ve sağlık anlatısını şikâyet modülünden dışarı kopyalamaz.",
    ],
    connections: [
      "M.01 Sapma Yönetimi",
      "M.02 DÖF Yönetimi",
      "M.15 Farmakovijilans",
      "M.11 Risk",
      "Ürün / Batch",
      "Müşteri yanıt arşivi",
    ],
  },
  "M.07": {
    title: "İç Denetimler ekran simülasyonu",
    summary:
      "Yıllık veya gerekçeli plansız denetimin bağımsızlık kontrolünden kilitli soru listesine, risk sınıflı bulguya ve M.02 DÖF bağımlı kapanışa kadar ilerleyişini izleyin.",
    recordNumber: "İD-2026-000014",
    recordTitle: "Üretim kayıtları ve veri bütünlüğü denetimi",
    stages: [
      {
        title: "Yıllık plan",
        text: "Denetim türü, kapsam, amaç, kriter, denetlenen bölüm, baş denetçi ve takvim kontrollü plana alınır.",
        role: "Denetim planlayıcısı",
        guard: "Plansız denetimde gerekçe zorunludur.",
      },
      {
        title: "Hazırlık",
        text: "Denetçi bölüm bağımsızlığı, soru listesi sürümü ve denetim referansları hazırlanır.",
        role: "Baş denetçi",
        guard: "Denetçi kendi bölümünü denetleyemez.",
      },
      {
        title: "Plan onayı",
        text: "Yetkili onayıyla denetim planı ve soru listesi sürümü zaman damgasıyla kilitlenir.",
        role: "Kalite Güvence / Onaylayan",
        guard: "Kilit sonrası soru metni değiştirilemez.",
      },
      {
        title: "Uygulama",
        text: "Her soruya uygunluk sonucu, objektif kanıt ve denetçi notu kaydedilir.",
        role: "Baş denetçi",
        guard: "Tüm sorular yanıtlanmadan uygulama tamamlanamaz.",
      },
      {
        title: "Bulgular",
        text: "Uygunsuzluklar etki × olasılık puanıyla Kritik, Majör, Minör veya Gözlem olarak sınıflanır.",
        role: "Baş denetçi",
        guard: "Majör ve kritik bulguda M.02 DÖF otomatik açılır.",
      },
      {
        title: "Cevap / aksiyon",
        text: "Denetlenen bölüm bulgu yanıtını, nedeni ve düzeltici aksiyonu hedef tarihiyle sunar.",
        role: "Denetlenen bölüm yanıtlayanı",
        guard: "Her açık bulguya yanıt verilmeden doğrulamaya geçilemez.",
      },
      {
        title: "DÖF / doğrulama",
        text: "Denetçi kanıtı ve bağlı DÖF durumunu doğrular; uygun bulguyu kapatır.",
        role: "Kalite Güvence / Onaylayan",
        guard: "Bağlı DÖF kapanmadan bulgu kapatılamaz.",
      },
      {
        title: "Bulgu kapanışı",
        text: "Tüm bulguların doğrulama notu ve kapanış zamanı birlikte kontrol edilir.",
        role: "Kalite Güvence",
        guard: "Tek bir açık bulgu bile denetim kapanışını engeller.",
      },
      {
        title: "Denetim kapanışı",
        text: "Kapsam, kanıt, bulgu ve DÖF zinciri son kez gözden geçirilerek denetim kapatılır.",
        role: "Onaylayan",
        guard: "Kapanış gerekçesi ve değiştirilemez geçmiş zorunludur.",
      },
    ],
    rules: [
      "Soru listesi denetim başladığında sürümüyle birlikte kilitlenir.",
      "Baş denetçinin bölümü denetlenen bölümden farklı olmalıdır.",
      "Majör ve kritik bulgu M.02 DÖF açar; DÖF kapanmadan bulgu, bulgular kapanmadan denetim kapanmaz.",
    ],
    connections: [
      "M.02 DÖF Yönetimi",
      "M.04 Doküman Yönetimi",
      "M.05 Eğitim Yönetimi",
      "M.11 Risk Yönetimi",
      "Organizasyon ve roller",
      "Değiştirilemez denetim geçmişi",
    ],
  },
  "M.08": {
    title: "Dış Denetimler ekran simülasyonu",
    summary:
      "Bir otorite veya müşteri denetiminin resmi bildirimden kontrollü doküman talep paketine, bulgu taahhütlerine, M.02 DÖF’e ve yetkili kapanışına kadar nasıl ilerlediğini izleyin.",
    recordNumber: "DD-2026-000009",
    recordTitle: "TİTCK GMP ve veri bütünlüğü denetimi",
    stages: [
      {
        title: "Bildirildi",
        text: "Denetleyen kurum, resmi referans, ülke, kapsam, saha, takvim ve cevap hedefi tek kayda alınır.",
        role: "Dış denetim koordinatörü",
        guard:
          "Devlet kurumunda Mesul Müdür/atanmış kapanış yetkilisi zorunludur.",
      },
      {
        title: "Hazırlık",
        text: "Otoritenin istediği M.04 dokümanları gizlilik sınıfı ve güncel sürümüyle talep paketine alınır.",
        role: "Talep paketi kontrolörü",
        guard: "Doküman doğrudan paylaşılmaz; kontrollü dışa aktarım gerekir.",
      },
      {
        title: "Denetim",
        text: "Talep paketi alıcı, amaç, manifest ve erişim kanıtıyla aktarıldıktan sonra saha denetimi başlatılır.",
        role: "Dış denetim koordinatörü",
        guard: "Tek bir bekleyen doküman bile denetim başlangıcını engeller.",
      },
      {
        title: "Bulgular",
        text: "Otorite/müşteri referansı taşıyan bulgular Kritik, Majör, Minör veya Gözlem olarak kaydedilir.",
        role: "Denetim koordinatörü",
        guard: "Majör ve kritik bulguda M.02 DÖF otomatik açılır.",
      },
      {
        title: "Cevap planı",
        text: "Her bulgu için resmi cevap, kurumsal taahhüt, sorumlu ve hedef tarih sürümlü kayda bağlanır.",
        role: "Resmi bulgu yanıtlayanı",
        guard: "Tüm bulgular cevaplanmadan DÖF/aksiyon aşaması açılmaz.",
      },
      {
        title: "DÖF / aksiyon",
        text: "Taahhüt kanıtları ve bağlı M.02 DÖF kapanışı yetkili kullanıcı tarafından doğrulanır.",
        role: "Kalite Güvence / Onaylayan",
        guard: "Açık DÖF varken resmi bulgu kapatılamaz.",
      },
      {
        title: "Kapanış mektubu",
        text: "Otorite veya müşteri kapanış mektubu, dosya kanıtı ve kabul kararıyla kaydedilir.",
        role: "Dış denetim koordinatörü",
        guard: "Tüm bulgular kapanmadan bu aşamaya geçilemez.",
      },
      {
        title: "Yetkili kapanışı",
        text: "Kapanış mektubu ve kabul kanıtı nihai görev ayrılığı kontrolüne sunulur.",
        role: "Mesul Müdür / Atanmış yetkili",
        guard:
          "Devlet kurumu denetimini yalnız Mesul Müdür veya atanmış yetkili kapatabilir.",
      },
      {
        title: "Kapalı",
        text: "Resmi bildirim, paylaşımlar, bulgular, taahhütler, DÖF ve kapanış kanıtı tek denetim paketinde korunur.",
        role: "Kalite Güvence",
        guard: "Değiştirilemez audit trail ve erişim günlüğü korunur.",
      },
    ],
    rules: [
      "Talep paketi kontrollü dışa aktarım ve erişim kaydı olmadan paylaşılamaz.",
      "Majör-kritik bulgu otomatik M.02 DÖF açar; DÖF kapanmadan bulgu kapatılamaz.",
      "Devlet kurumu denetiminde nihai kapanış Mesul Müdür/atanmış yetkiliye aittir.",
    ],
    connections: [
      "M.02 DÖF Yönetimi",
      "M.04 Doküman Yönetimi",
      "M.05 Eğitim Yönetimi",
      "M.07 İç Denetimler",
      "Mesul Müdür / Organizasyon",
      "Kontrollü dışa aktarım günlüğü",
    ],
  },
  "M.09": {
    title: "Tedarikçi Denetimi ekran simülasyonu",
    summary:
      "Risk bazlı tedarikçi planının soru listesi, saha kanıtı, güvenli tedarikçi cevabı, M.02 DÖF ve kapsam bazlı nitelendirme kararına nasıl dönüştüğünü izleyin.",
    recordNumber: "TD-2026-000014",
    recordTitle: "Primer ambalaj tedarikçisi GMP denetimi",
    stages: [
      {
        title: "Risk planı",
        text: "Kritiklik, geçmiş performans ve açık bulgu sayısından risk skoru ile denetim frekansı hesaplanır.",
        role: "Tedarikçi denetimi planlayıcısı",
        guard:
          "Kritiklik ve geçmiş performans verisi olmadan plan oluşturulamaz.",
      },
      {
        title: "Kapsam",
        text: "Malzeme/hizmet kapsamı, saha, kriterler ve sürümlü soru listesi sabitlenir.",
        role: "Tedarikçi Kalite",
        guard: "En az bir soru listesi maddesi zorunludur.",
      },
      {
        title: "Denetçi",
        text: "Baş denetçi ve satınalma sorumlusu kayıt bazlı görevlerle atanır.",
        role: "Denetim planlayıcısı",
        guard: "Yetkili baş denetçi atanmadan uygulama başlayamaz.",
      },
      {
        title: "Uygulama",
        text: "Her soru uygunluk sonucu, objektif kanıt ve denetçi notuyla yanıtlanır.",
        role: "Tedarikçi baş denetçisi",
        guard: "Soru listesi denetim başlangıcında sürümüyle kilitlenir.",
      },
      {
        title: "Bulgular",
        text: "Bulgular Kritik, Majör, Minör veya Gözlem olarak sınıflanır.",
        role: "Baş denetçi",
        guard:
          "Kritik bulgu kapsamı askıya alır; majör/kritik bulgu M.02 DÖF açar.",
      },
      {
        title: "Tedarikçi cevabı",
        text: "Cevap ve taahhüt iç kullanıcı veya süreli tek kullanımlık güvenli davet üzerinden alınır.",
        role: "Tedarikçi yanıt sorumlusu",
        guard: "Davet tokenı yalnız bir kez gösterilir ve özeti saklanır.",
      },
      {
        title: "Kanıt",
        text: "Tedarikçi kanıt paketi ve taahhüt hedefi doğrulama için sabitlenir.",
        role: "Tedarikçi kanıt doğrulayıcısı",
        guard: "Kanıtı olmayan bulgu DÖF/CAPA aşamasına geçemez.",
      },
      {
        title: "DÖF / CAPA",
        text: "Bağlı M.02 kayıtları ve tedarikçi kanıtları yetkili kullanıcı tarafından doğrulanır.",
        role: "Kalite Güvence / Onaylayan",
        guard: "Açık DÖF varken bulgu kapatılamaz.",
      },
      {
        title: "Sonuç",
        text: "Onaylı, koşullu, askıda, reddedildi veya yeniden nitelendirme kararı gerekçeyle kaydedilir.",
        role: "Tedarikçi kalite onaylayanı",
        guard: "Kritik bulgulu tedarikçi doğrudan onaylanamaz.",
      },
      {
        title: "Kapalı",
        text: "Risk hesabı, soru listesi, kanıtlar, davetler, DÖF ve karar tek denetim paketinde korunur.",
        role: "Kalite Güvence",
        guard: "Tüm bulgular ve nitelendirme kararı tamamlanmalıdır.",
      },
    ],
    rules: [
      "Kritik bulgu tedarikçi kapsamını otomatik askıya alır ve yeniden nitelendirme gerektirir.",
      "Majör/kritik bulgu M.02 DÖF açar; DÖF kapanmadan bulgu ve denetim kapanmaz.",
      "Tedarikçi cevabı ikinci tenant açmadan, süreli ve tek kullanımlık güvenli davetle alınabilir.",
    ],
    connections: [
      "M.02 DÖF Yönetimi",
      "M.11 Risk Yönetimi",
      "M.16 Tedarikçi Değerlendirme",
      "Satınalma",
      "Güvenli tedarikçi cevap portalı",
      "Değiştirilemez denetim geçmişi",
    ],
  },
};

export function ModuleInfoButton({
  module,
  onClick,
}: {
  module: ModuleCode;
  onClick: () => void;
}) {
  return (
    <Button
      className="module-info-button"
      variant="outlined"
      size="large"
      startIcon={<InfoOutlined />}
      onClick={onClick}
    >
      {module} hakkında
    </Button>
  );
}

export function ModuleGuideDialog({
  module,
  open,
  onClose,
}: {
  module: ModuleCode;
  open: boolean;
  onClose: () => void;
}) {
  const guide = guides[module];
  const [activeStep, setActiveStep] = useState(0);
  const [playing, setPlaying] = useState(true);

  useEffect(() => {
    if (open) {
      setActiveStep(0);
      setPlaying(true);
    }
  }, [open, module]);
  useEffect(() => {
    if (!open || !playing) return;
    const timer = window.setInterval(
      () => setActiveStep((current) => (current + 1) % guide.stages.length),
      3600,
    );
    return () => window.clearInterval(timer);
  }, [open, playing, guide.stages.length]);

  const stage = guide.stages[activeStep];
  const selectStep = (index: number) => {
    setActiveStep(index);
    setPlaying(false);
  };
  const move = (direction: number) => {
    setActiveStep(
      (current) =>
        (current + direction + guide.stages.length) % guide.stages.length,
    );
    setPlaying(false);
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      className="module-guide-dialog"
      slotProps={{ paper: { className: "module-simulator-paper" } }}
    >
      <ModalHeader onClose={onClose}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          className="module-guide-header"
        >
          <Stack direction="row" spacing={1.4} sx={{ alignItems: "center" }}>
            <Box className="module-guide-header-icon">
              <InfoOutlined />
            </Box>
            <Box>
              <Typography variant="overline">
                {module} · İnteraktif modül simülasyonu
              </Typography>
              <Typography variant="h5">{guide.title}</Typography>
            </Box>
          </Stack>
          <Chip
            className="simulation-live-chip"
            icon={playing ? <PlayArrowRounded /> : <PauseRounded />}
            label={playing ? "Otomatik anlatım" : "Manuel inceleme"}
          />
        </Stack>
      </ModalHeader>
      <DialogContent className="module-guide-content">
        <Paper variant="outlined" className="module-guide-summary">
          <AccountTreeRounded />
          <Box>
            <Typography sx={{ fontWeight: 800 }}>
              Bu simülasyonda ne göreceksiniz?
            </Typography>
            <Typography color="text.secondary">{guide.summary}</Typography>
          </Box>
        </Paper>
        <Box className="module-simulator-shell">
          <Box
            className="module-simulator-rail"
            aria-label={`${module} simülasyon adımları`}
          >
            <Stack direction="row" className="simulator-rail-heading">
              <Box>
                <Typography variant="overline">SÜREÇ HARİTASI</Typography>
                <Typography sx={{ fontWeight: 800 }}>
                  {activeStep + 1} / {guide.stages.length}. adım
                </Typography>
              </Box>
              <Tooltip
                title={playing ? "Otomatik oynatmayı durdur" : "Otomatik oynat"}
              >
                <IconButton onClick={() => setPlaying((value) => !value)}>
                  {playing ? <PauseRounded /> : <PlayArrowRounded />}
                </IconButton>
              </Tooltip>
            </Stack>
            <LinearProgress
              variant="determinate"
              value={((activeStep + 1) / guide.stages.length) * 100}
              className="simulation-progress"
            />
            <Box className="simulation-step-list">
              {guide.stages.map((item, index) => (
                <button
                  type="button"
                  key={item.title}
                  className={`simulation-step-button ${index === activeStep ? "is-active" : ""} ${index < activeStep ? "is-complete" : ""}`}
                  onClick={() => selectStep(index)}
                >
                  <span className="simulation-step-index">
                    {index < activeStep ? <CheckCircleRounded /> : index + 1}
                  </span>
                  <span>
                    <strong>{item.title}</strong>
                    <small>{item.role}</small>
                  </span>
                </button>
              ))}
            </Box>
          </Box>
          <Box className="module-simulator-main">
            <Box key={`${module}-${activeStep}`} className="simulation-scene">
              <Paper variant="outlined" className="simulation-explanation">
                <Stack
                  direction={{ xs: "column", md: "row" }}
                  sx={{ justifyContent: "space-between", gap: 2 }}
                >
                  <Box>
                    <Typography variant="overline">
                      ŞU ANDA NE OLUYOR?
                    </Typography>
                    <Typography variant="h5">{stage.title}</Typography>
                    <Typography color="text.secondary">{stage.text}</Typography>
                  </Box>
                  <Chip icon={<GroupsRounded />} label={stage.role} />
                </Stack>
                <Stack direction="row" spacing={1} className="simulation-guard">
                  <RuleRounded />
                  <Box>
                    <Typography variant="caption">KRİTİK KONTROL</Typography>
                    <Typography variant="body2">{stage.guard}</Typography>
                  </Box>
                </Stack>
              </Paper>
              <SimulationScreen
                module={module}
                step={activeStep}
                guide={guide}
              />
            </Box>
            <Stack direction="row" className="simulation-controls">
              <Button startIcon={<ArrowBackRounded />} onClick={() => move(-1)}>
                Önceki ekran
              </Button>
              <Stack direction="row" spacing={1}>
                <Button
                  variant="outlined"
                  startIcon={playing ? <PauseRounded /> : <ReplayRounded />}
                  onClick={() => setPlaying((value) => !value)}
                >
                  {playing ? "Durdur" : "Otomatik oynat"}
                </Button>
                <Button
                  variant="contained"
                  endIcon={<ArrowForwardRounded />}
                  onClick={() => move(1)}
                >
                  Sonraki ekran
                </Button>
              </Stack>
            </Stack>
          </Box>
        </Box>
        <Box className="module-guide-bottom-grid">
          <Paper variant="outlined" className="module-guide-rules">
            <Stack
              direction="row"
              spacing={1}
              sx={{ alignItems: "center", mb: 1.5 }}
            >
              <RuleRounded color="primary" />
              <Typography sx={{ fontWeight: 800 }}>
                Modül genelindeki kritik kurallar
              </Typography>
            </Stack>
            {guide.rules.map((rule, index) => (
              <Stack direction="row" spacing={1.2} key={rule}>
                <Box className="module-guide-rule-number">{index + 1}</Box>
                <Typography variant="body2">{rule}</Typography>
              </Stack>
            ))}
          </Paper>
          <Paper variant="outlined" className="module-guide-connections">
            <Stack
              direction="row"
              spacing={1}
              sx={{ alignItems: "center", mb: 1.5 }}
            >
              <LinkRounded color="primary" />
              <Typography sx={{ fontWeight: 800 }}>
                Bağlandığı süreçler
              </Typography>
            </Stack>
            <Stack
              direction="row"
              spacing={1}
              useFlexGap
              sx={{ flexWrap: "wrap" }}
            >
              {guide.connections.map((item) => (
                <Chip key={item} label={item} />
              ))}
            </Stack>
          </Paper>
        </Box>
      </DialogContent>
    </Dialog>
  );
}

function SimulationScreen({
  module,
  step,
  guide,
}: {
  module: ModuleCode;
  step: number;
  guide: (typeof guides)[ModuleCode];
}) {
  return (
    <Paper variant="outlined" className="simulation-screen-frame">
      <Box className="simulation-browser-bar">
        <span />
        <span />
        <span />
        <Typography>{module} kalite kaydı · ekran simülasyonu</Typography>
        <Chip size="small" label="ÖRNEK EKRAN" />
      </Box>
      <Box className="simulation-record-header">
        <Box>
          <Typography variant="overline">{guide.recordNumber}</Typography>
          <Typography variant="h6">{guide.recordTitle}</Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Chip
            size="small"
            color="success"
            variant="outlined"
            label={`${step + 1}. adım`}
          />
          <Chip size="small" label={guide.stages[step].title} />
        </Stack>
      </Box>
      <Box className="simulation-screen-body">
        {module === "M.01" ? (
          <DeviationSimulation step={step} />
        ) : module === "M.02" ? (
          <CapaSimulation step={step} />
        ) : module === "M.03" ? (
          <ChangeSimulation step={step} />
        ) : module === "M.04" ? (
          <DocumentSimulation step={step} />
        ) : module === "M.06" ? (
          <ComplaintSimulation step={step} />
        ) : module === "M.07" ? (
          <InternalAuditSimulation step={step} />
        ) : module === "M.08" ? (
          <ExternalAuditSimulation step={step} />
        ) : module === "M.09" ? (
          <SupplierAuditSimulation step={step} />
        ) : (
          <TrainingSimulation step={step} />
        )}
      </Box>
    </Paper>
  );
}

function SupplierAuditSimulation({ step }: { step: number }) {
  const rows = [
    ["Kritiklik", "Yüksek", "40 puan"],
    ["Geçmiş performans", "78 / 100", "8 puan"],
    ["Açık bulgu", "2", "8 puan"],
  ];
  if (step === 0)
    return (
      <>
        <MockSection
          icon={<ScienceRounded />}
          title="Risk bazlı plan"
          badge="56 · Yüksek"
        >
          <MockTable
            headers={["Risk girdisi", "Değer", "Katkı"]}
            rows={rows}
            highlightRow={0}
          />
        </MockSection>
        <MockAction text="Kapsamı sabitle" />
      </>
    );
  if (step === 4)
    return (
      <MockSection
        icon={<WarningAmberRounded />}
        title="Kritik tedarikçi bulgusu"
        badge="Kapsam askıda"
      >
        <MockField
          label="Bulgu"
          value="Sterilizasyon validasyonu güncel değil"
          wide
          active
        />
        <MockField
          label="Otomatik bağ"
          value="DÖF-2026-000041 · Yeniden nitelendirme"
          wide
        />
      </MockSection>
    );
  if (step === 5)
    return (
      <MockSection
        icon={<MarkEmailReadRounded />}
        title="Tek kullanımlık güvenli davet"
        badge="7 gün"
      >
        <MockField label="Alıcı" value="quality@supplier.example" wide />
        <MockField
          label="Güvenlik"
          value="Token özeti saklanır · tek kullanım"
          wide
          active
        />
      </MockSection>
    );
  if (step === 8)
    return (
      <MockSection
        icon={<VerifiedRounded />}
        title="Kapsam bazlı nitelendirme"
        badge="Koşullu"
      >
        <MockField
          label="Karar"
          value="Koşullu · yeniden nitelendirme gerekli"
          wide
          active
        />
        <MockField
          label="M.16 tetikleyicisi"
          value="Ara değerlendirme oluştur"
          wide
        />
      </MockSection>
    );
  return (
    <MockSection
      icon={<FactCheckRounded />}
      title={guides["M.09"].stages[step].title}
      badge={`${step + 1}. adım`}
    >
      <MockField
        label="Kontrol"
        value={guides["M.09"].stages[step].guard}
        wide
        active
      />
      <MockField
        label="Sorumlu"
        value={guides["M.09"].stages[step].role}
        wide
      />
    </MockSection>
  );
}

function ExternalAuditSimulation({ step }: { step: number }) {
  if (step === 0)
    return (
      <>
        <MockSection
          icon={<FactCheckRounded />}
          title="Resmi dış denetim bildirimi"
          badge="TİTCK · Otorite"
        >
          <Box className="mock-field-grid">
            <MockField
              label="Resmi referans"
              value="TİTCK-GMP-2026-41"
              active
            />
            <MockField
              label="Saha / tarih"
              value="İstanbul Üretim · 2 Eyl 2026"
            />
            <MockField
              label="Kapsam"
              value="GMP kalite sistemi ve veri bütünlüğü"
              wide
            />
          </Box>
        </MockSection>
        <MockAction text="Hazırlığı başlat" />
      </>
    );
  if (step === 1)
    return (
      <MockSection
        icon={<DescriptionRounded />}
        title="Kontrollü talep paketi"
        badge="2 doküman"
      >
        <MockTable
          headers={["M.04 dokümanı", "Gizlilik", "Durum"]}
          rows={[
            ["SOP-QA-001 · v3.2", "Kurum İçi", "Aktarım bekliyor"],
            ["SOP-URT-014 · v5.0", "Gizli", "Aktarım bekliyor"],
          ]}
          highlightRow={1}
        />
      </MockSection>
    );
  if (step === 2)
    return (
      <MockSection
        icon={<UploadFileRounded />}
        title="Erişim kayıtlı dışa aktarım"
        badge="Manifest v1"
      >
        <Box className="mock-check-list">
          <span>
            <CheckCircleRounded />
            Yetkili alıcı: TİTCK denetim ekibi
          </span>
          <span>
            <CheckCircleRounded />
            Amaç ve gizlilik sınırı kaydedildi
          </span>
          <span>
            <LockRounded />
            Manifest ve SHA-256 kanıtı korundu
          </span>
        </Box>
      </MockSection>
    );
  if (step === 3)
    return (
      <MockSection
        icon={<WarningAmberRounded />}
        title="Resmi denetim bulgusu"
        badge="Majör"
      >
        <MockField label="Otorite referansı" value="OBS-2026-04" active />
        <MockField
          label="Bulgu"
          value="Yetki matrisi pozisyon değişikliklerini güncel yansıtmıyor."
          wide
        />
        <Paper variant="outlined" className="mock-linked-record">
          <LinkRounded />
          <Box>
            <Typography variant="caption">OTOMATİK M.02 BAĞLANTISI</Typography>
            <Typography sx={{ fontWeight: 800 }}>DÖF-2026-000027</Typography>
          </Box>
        </Paper>
      </MockSection>
    );
  if (step === 4)
    return (
      <MockSection
        icon={<AssignmentRounded />}
        title="Resmi cevap ve kurumsal taahhüt"
        badge="Cevap planı"
      >
        <MockField
          label="Resmi cevap"
          value="Bulgu kabul edilmiş, kapsam analizi tamamlanmıştır."
          wide
        />
        <MockField
          label="Taahhüt"
          value="Yetki matrisi revizyonu ve pozisyon eğitimleri 30 gün içinde tamamlanacaktır."
          active
          wide
        />
      </MockSection>
    );
  if (step === 5)
    return (
      <ApprovalScreen
        title="DÖF / aksiyon doğrulaması"
        items={[
          "DÖF aksiyonları doğrulandı",
          "Taahhüt kanıtları kabul edildi",
          "Etkinlik sonucu uygun",
        ]}
        action="Resmi bulguyu kapat"
      />
    );
  if (step === 6)
    return (
      <MockSection
        icon={<MarkEmailReadRounded />}
        title="Otorite kapanış mektubu"
        badge="Kabul edildi"
      >
        <MockField label="Kapanış referansı" value="TİTCK-KP-2026-117" active />
        <MockField
          label="Kanıt"
          value="İmzalı kapanış mektubu ve resmi kayıt numarası"
          wide
        />
      </MockSection>
    );
  if (step === 7)
    return (
      <ApprovalScreen
        title="Mesul Müdür kapanış kapısı"
        items={[
          "Tüm resmi bulgular kapalı",
          "Kapanış mektubu kabul edildi",
          "Talep paketi erişim günlüğü eksiksiz",
        ]}
        action="Yetkili kapanışını onayla"
      />
    );
  return (
    <MockSection
      icon={<VerifiedRounded />}
      title="Dış denetim paketi tamamlandı"
      badge="Kapalı"
    >
      <Box className="mock-check-list">
        <span>
          <CheckCircleRounded />
          Resmi bildirim ve kapsam
        </span>
        <span>
          <CheckCircleRounded />
          Doküman paylaşım manifestleri
        </span>
        <span>
          <CheckCircleRounded />
          Bulgu, DÖF ve kapanış mektubu zinciri
        </span>
      </Box>
    </MockSection>
  );
}

function InternalAuditSimulation({ step }: { step: number }) {
  if (step === 0)
    return (
      <>
        <MockSection
          icon={<FactCheckRounded />}
          title="2026 iç denetim programı"
          badge="Yıllık plan"
        >
          <Box className="mock-field-grid">
            <MockField label="Denetlenen bölüm" value="Üretim" active />
            <MockField
              label="Baş denetçi"
              value="Ayşe Demir · Kalite Güvence"
            />
            <MockField
              label="Kapsam"
              value="Batch kayıtları, veri bütünlüğü ve sapma bağlantıları"
              wide
            />
          </Box>
        </MockSection>
        <MockAction text="Denetim hazırlığını başlat" />
      </>
    );
  if (step === 1)
    return (
      <MockSection
        icon={<RuleRounded />}
        title="Bağımsızlık ve hazırlık"
        badge="Kontrol başarılı"
      >
        <Box className="mock-check-list">
          <span>
            <CheckCircleRounded />
            Denetçi bölümü: Kalite Güvence
          </span>
          <span>
            <CheckCircleRounded />
            Denetlenen bölüm: Üretim
          </span>
          <span>
            <CheckCircleRounded />
            Çıkar çatışması bulunmuyor
          </span>
        </Box>
      </MockSection>
    );
  if (step === 2)
    return (
      <MockSection
        icon={<LockRounded />}
        title="Soru listesi kilidi"
        badge="Sürüm 2026.1"
      >
        <MockField
          label="Kontrollü soru listesi"
          value="18 soru · ISO 9001 / şirket SOP referansları"
          active
          wide
        />
        <MockAction text="Planı onayla ve sürümü kilitle" />
      </MockSection>
    );
  if (step === 3)
    return (
      <MockSection
        icon={<PlaylistAddCheckRounded />}
        title="Kanıtlı saha uygulaması"
        badge="12 / 18"
      >
        <MockTable
          headers={["Soru", "Sonuç", "Objektif kanıt"]}
          rows={[
            ["Batch kaydı izlenebilir mi?", "Uygun", "BPR-260825"],
            ["Yetki matrisi güncel mi?", "Uygunsuz", "FRM-YET-04"],
            ["Sapmalar bağlı mı?", "Uygun", "SP-2026-000007"],
          ]}
          highlightRow={1}
        />
      </MockSection>
    );
  if (step === 4)
    return (
      <MockSection
        icon={<WarningAmberRounded />}
        title="Risk sınıflı bulgu"
        badge="Majör · 16"
      >
        <Box className="mock-metric-grid">
          <MiniMetric label="Etki" value="4" accent />
          <MiniMetric label="Olasılık" value="4" />
          <MiniMetric label="Risk" value="16" accent />
        </Box>
        <Paper variant="outlined" className="mock-linked-record">
          <LinkRounded />
          <Box>
            <Typography variant="caption">OTOMATİK M.02 BAĞLANTISI</Typography>
            <Typography sx={{ fontWeight: 800 }}>DÖF-2026-000021</Typography>
          </Box>
        </Paper>
      </MockSection>
    );
  if (step === 5)
    return (
      <MockSection
        icon={<GroupsRounded />}
        title="Bölüm yanıtı ve aksiyon"
        badge="Yanıtlandı"
      >
        <MockField
          label="Neden / yanıt"
          value="Pozisyon değişikliği sonrası yetki matrisi revizyonu gecikmiştir."
          wide
        />
        <MockField
          label="Düzeltici aksiyon"
          value="Matris revizyonu ve ilgili çalışan eğitimi"
          active
          wide
        />
      </MockSection>
    );
  if (step === 6)
    return (
      <ApprovalScreen
        title="DÖF ve bulgu doğrulaması"
        items={[
          "DÖF aksiyonları tamamlandı",
          "Etkinlik kanıtı kabul edildi",
          "Bulgu kapanış notu hazır",
        ]}
        action="Bulguyu kapat"
      />
    );
  if (step === 7)
    return (
      <MockSection
        icon={<CheckCircleRounded />}
        title="Bulgu kapanış kapısı"
        badge="0 açık bulgu"
      >
        <Box className="mock-check-list">
          <span>
            <CheckCircleRounded />
            Majör bulgu kapalı
          </span>
          <span>
            <CheckCircleRounded />
            M.02 DÖF kapalı
          </span>
          <span>
            <CheckCircleRounded />
            Doğrulama kanıtı kayıtlı
          </span>
        </Box>
      </MockSection>
    );
  return (
    <ApprovalScreen
      title="İç denetim kapanış kontrolü"
      items={[
        "Kilitli soru listesi eksiksiz yanıtlandı",
        "Tüm bulgu ve DÖF bağlantıları kapalı",
        "Kronolojik denetim geçmişi tamamlandı",
      ]}
      action="Denetimi kapat"
    />
  );
}

function ComplaintSimulation({ step }: { step: number }) {
  switch (step) {
    case 0:
      return (
        <>
          <MockSection
            icon={<ChatBubbleOutlineRounded />}
            title="Yeni müşteri bildirimi"
            badge="SLA başlatıldı"
          >
            <Box className="mock-field-grid">
              <MockField
                label="Müşteri / ülke"
                value="Anadolu Sağlık · Türkiye"
                active
              />
              <MockField
                label="Ürün / batch"
                value="QMS Tablet 10 mg · B260825-A"
              />
              <MockField
                label="Bildirim"
                value="Blister içinde kırık tablet ve eksik göz"
                wide
              />
              <MockField
                label="Nihai yanıt hedefi"
                value="24 Eyl 2026 · 17:00"
              />
            </Box>
          </MockSection>
          <MockAction text="Şikâyeti kontrollü kayda al" />
        </>
      );
    case 1:
      return (
        <MockSection
          icon={<WarningAmberRounded />}
          title="Risk ve güvenlik triyajı"
          badge="Majör"
        >
          <Box className="mock-metric-grid">
            <MiniMetric label="Ürün kalitesi" value="Evet" accent />
            <MiniMetric label="Sağlık etkisi" value="Bildirildi" />
            <MiniMetric label="Advers olay" value="Şüpheli" accent />
          </Box>
          <Paper variant="outlined" className="mock-linked-record">
            <LinkRounded />
            <Box>
              <Typography variant="caption">
                OTOMATİK M.01 BAĞLANTISI
              </Typography>
              <Typography sx={{ fontWeight: 800 }}>
                SP-2026-000007 oluşturulacak
              </Typography>
            </Box>
          </Paper>
        </MockSection>
      );
    case 2:
      return (
        <MockSection
          icon={<MarkEmailReadRounded />}
          title="Ön müşteri yanıtı"
          badge="Sürüm 1"
        >
          <MockField
            label="Onaylanacak mesaj"
            value="Bildiriminiz alınmış, batch kontrol altına alınmış ve bölüm araştırmaları başlatılmıştır."
            active
            wide
          />
          <MockAction text="Ön yanıt sürümünü onayla" />
        </MockSection>
      );
    case 3:
      return (
        <MockSection
          icon={<GroupsRounded />}
          title="Paralel bölüm araştırmaları"
          badge="3 bölüm"
        >
          <MockTable
            headers={["Bölüm", "Durum", "Kök neden katkısı"]}
            rows={[
              ["Üretim", "Tamamlandı", "Kapatma basıncı"],
              ["Kalite Kontrol", "Tamamlandı", "Numune doğrulandı"],
              ["Kalite Güvence", "Bekliyor", "—"],
            ]}
            highlightRow={2}
          />
        </MockSection>
      );
    case 4:
      return (
        <MockSection
          icon={<ManageSearchRounded />}
          title="Ortak etki ve kök neden"
          badge="Batch etkisi"
        >
          <MockField
            label="Etki"
            value="B260825-A ile sınırlı ambalaj bütünlüğü etkisi"
            wide
          />
          <MockField
            label="Doğrulanmış kök neden"
            value="Blister kapatma basıncı kontrol sıklığının yetersizliği"
            active
            wide
          />
        </MockSection>
      );
    case 5:
      return (
        <MockSection
          icon={<HealthAndSafetyRounded />}
          title="Gizlilik sınırlı farmakovijilans aktarımı"
          badge="FV-2026-000001"
        >
          <Box className="mock-check-list">
            <span>
              <CheckCircleRounded />
              Ürün ve batch aktarılır
            </span>
            <span>
              <CheckCircleRounded />
              Şikâyet teknik kimliği aktarılır
            </span>
            <span>
              <LockRounded />
              Müşteri adı ve sağlık anlatısı aktarılmaz
            </span>
          </Box>
        </MockSection>
      );
    case 6:
      return (
        <MockSection
          icon={<TaskAltRounded />}
          title="DÖF bağlantı kararı"
          badge="M.02"
        >
          <Box className="mock-decision-grid">
            <DecisionCard
              title="DÖF gerekli"
              text="Kök neden aksiyonu ve etkinlik izlemesi"
              selected
            />
            <DecisionCard
              title="DÖF gerekli değil"
              text="Gerekçeli kalite kararı"
            />
          </Box>
          <Paper variant="outlined" className="mock-linked-record">
            <LinkRounded />
            <Box>
              <Typography variant="caption">OLUŞTURULAN KAYIT</Typography>
              <Typography sx={{ fontWeight: 800 }}>DÖF-2026-000005</Typography>
            </Box>
          </Paper>
        </MockSection>
      );
    case 7:
      return (
        <MockSection
          icon={<MarkEmailReadRounded />}
          title="Nihai müşteri yanıtı"
          badge="Sürüm 1 · Onay bekliyor"
        >
          <MockField
            label="Araştırma sonucu ve aksiyon"
            value="Batch etkisi doğrulandı; kapatma basıncı kontrol sıklığı için DÖF başlatıldı."
            active
            wide
          />
          <MockAction text="Nihai yanıtı onayla" />
        </MockSection>
      );
    default:
      return (
        <ApprovalScreen
          title="Şikâyet kapanış kontrolü"
          items={[
            "Ön ve nihai yanıt sürümleri onaylı",
            "Tüm bölüm araştırmaları tamamlandı",
            "M.01, M.02 ve FV bağlantıları doğrulandı",
          ]}
          action="Şikâyeti kapat"
        />
      );
  }
}

function DocumentSimulation({ step }: { step: number }) {
  switch (step) {
    case 0:
      return (
        <>
          <MockSection
            icon={<DescriptionRounded />}
            title="Yeni kontrollü doküman"
            badge="Sürüm 0.1"
          >
            <Box className="mock-field-grid">
              <MockField label="Doküman kodu" value="SOP-URT-014" active />
              <MockField label="Tür / Gizlilik" value="SOP · Kurum İçi" />
              <MockField
                label="Kaynak değişiklik"
                value="DK-2026-000042"
                wide
              />
              <MockField label="Gözden geçirme" value="12 ay" />
            </Box>
          </MockSection>
          <MockAction text="Taslağı oluştur" />
        </>
      );
    case 1:
      return (
        <MockSection
          icon={<ManageSearchRounded />}
          title="Kontrollü içerik yazımı"
          badge="Doküman yazarı"
        >
          <MockField
            label="İçerik"
            value="Amaç · kapsam · sorumluluk · uygulama adımları · kayıtlar"
            active
            wide
          />
          <MockField
            label="Değişiklik özeti"
            value="Yeni alarm kontrol adımı eklendi."
            wide
          />
          <MockAction text="İncelemeye gönder" />
        </MockSection>
      );
    case 2:
      return (
        <MockSection
          icon={<GroupsRounded />}
          title="Paralel sürüm incelemeleri"
          badge="2 / 3 tamamlandı"
        >
          <MockTable
            headers={["Bölüm", "Durum", "Görüş"]}
            rows={[
              ["Üretim", "Uygun", "Uygulanabilir"],
              ["Kalite Güvence", "Uygun", "GMP uygun"],
              ["Ruhsatlandırma", "Bekliyor", "—"],
            ]}
            highlightRow={2}
          />
        </MockSection>
      );
    case 3:
      return (
        <ApprovalScreen
          title="Sürüm onayı"
          items={[
            "Tüm bölüm incelemeleri tamamlandı",
            "Değişiklik özeti içerikle uyumlu",
            "Hazırlayan ve onaylayan farklı kullanıcı",
          ]}
          action="Sürüm 0.1’i onayla"
        />
      );
    case 4:
      return (
        <MockSection
          icon={<ScienceRounded />}
          title="Yürürlük öncesi eğitim"
          badge="1 / 2 tamamlandı"
        >
          <MockTable
            headers={["Pozisyon", "Kanıt", "Durum"]}
            rows={[
              ["Hat Lideri", "Sınav %100", "Tamamlandı"],
              ["Üretim Operatörü", "—", "Bekliyor"],
            ]}
            highlightRow={1}
          />
          <MockAction text="Eğitim kanıtını kaydet" />
        </MockSection>
      );
    case 5:
      return (
        <MockSection
          icon={<VerifiedRounded />}
          title="Yürürlüğe alma"
          badge="Sürüm 0.1"
        >
          <Box className="mock-check-list">
            <MockCheck text="Onay elektronik imzası mevcut" />
            <MockCheck text="Zorunlu eğitimler tamamlandı" />
            <MockCheck text="Planlanan yürürlük tarihi geldi" />
          </Box>
          <MockAction text="Dokümanı yürürlüğe al" />
        </MockSection>
      );
    case 6:
      return (
        <MockSection
          icon={<Inventory2Rounded />}
          title="Dağıtım ve okuma kanıtı"
          badge="Kontrollü"
        >
          <MockTable
            headers={["Kayıt", "Kullanıcı / Birim", "Durum"]}
            rows={[
              ["Okuma", "Ayşe Yılmaz", "İmzalandı"],
              ["KK-001", "Dolum Hattı", "Dağıtımda"],
            ]}
            highlightRow={1}
          />
        </MockSection>
      );
    case 7:
      return (
        <MockSection
          icon={<ReplayRounded />}
          title="Minör revizyon"
          badge="Sürüm 0.2"
        >
          <Box className="mock-compare">
            <DecisionCard title="Önceki 0.1" text="Temizlik süresi 20 dakika" />
            <DecisionCard
              title="Yeni 0.2"
              text="Temizlik süresi 25 dakika"
              selected
            />
          </Box>
          <MockField
            label="Revizyon özeti"
            value="Validasyon sonucuna göre süre güncellendi."
            active
            wide
          />
        </MockSection>
      );
    default:
      return (
        <MockSection
          icon={<LockRounded />}
          title="Yürürlükten kaldırma ve arşiv"
          badge="Arşiv kapısı"
        >
          <Box className="mock-check-list">
            <MockCheck text="Yeni sürüm yürürlükte" />
            <MockCheck text="Eski elektronik sürüm salt okunur" />
            <MockCheck text="Tüm kontrollü kopyalar iade / imha edildi" />
          </Box>
          <MockAction text="Dokümanı arşivle" />
        </MockSection>
      );
  }
}

function TrainingSimulation({ step }: { step: number }) {
  switch (step) {
    case 0:
      return (
        <MockSection
          icon={<AccountTreeRounded />}
          title="Pozisyon–eğitim matrisi"
          badge="Etkin"
        >
          <MockTable
            headers={["Pozisyon", "Eğitim", "Yöntem", "Geçerlilik"]}
            rows={[
              ["Üretim Operatörü", "SOP-URT-014", "Oku + sınav", "12 ay"],
              ["Hat Lideri", "GMP-2026", "Sınıf + pratik", "24 ay"],
            ]}
            highlightRow={0}
          />
        </MockSection>
      );
    case 1:
      return (
        <MockSection
          icon={<LinkRounded />}
          title="M.04 sürümünden otomatik görev"
          badge="Doküman kapısı"
        >
          <Paper variant="outlined" className="mock-linked-record">
            <DescriptionRounded />
            <Box>
              <Typography variant="caption">KAYNAK DOKÜMAN</Typography>
              <Typography>SOP-URT-014 · Sürüm 0.2 · Eğitim bekliyor</Typography>
            </Box>
          </Paper>
          <Box className="mock-check-list" style={{ marginTop: 12 }}>
            <MockCheck text="Etkilenen pozisyon: Üretim Operatörü" />
            <MockCheck text="Hedef: planlanan yürürlük tarihi" />
          </Box>
        </MockSection>
      );
    case 2:
      return (
        <MockSection
          icon={<GroupsRounded />}
          title="Kontrollü eğitim ataması"
          badge="Atandı"
        >
          <Box className="mock-field-grid">
            <MockField
              label="Katılımcı"
              value="Mehmet Kaya · Üretim Operatörü"
              active
            />
            <MockField label="Hedef" value="02 Eyl 2026" />
            <MockField label="Değerlendirme" value="Oku ve anla · Geçme %80" />
            <MockField label="Kritik yeterlilik" value="Evet · 12 ay" />
          </Box>
        </MockSection>
      );
    case 3:
      return (
        <MockSection
          icon={<MenuBookRounded />}
          title="Okuma ve anlama imzası"
          badge="Katılımcı"
        >
          <MockField
            label="Elektronik imza anlamı"
            value="Dokümanı okudum, anladım ve görevimde uygulayacağım."
            active
            wide
          />
          <MockAction text="Okudum ve anladım olarak imzala" />
        </MockSection>
      );
    case 4:
      return (
        <MockSection
          icon={<ScienceRounded />}
          title="Sınav ve pratik değerlendirme"
          badge="1. deneme"
        >
          <Box className="mock-metric-grid">
            <MiniMetric label="Geçme" value="%80" />
            <MiniMetric label="Puan" value="%92" accent />
            <MiniMetric label="Pratik" value="Uygun" />
            <MiniMetric label="Kalan deneme" value="2" />
          </Box>
          <MockField
            label="Kanıt"
            value="EGT-F-01 sınav formu · iş başı gözlem kaydı"
            active
            wide
          />
        </MockSection>
      );
    case 5:
      return (
        <ApprovalScreen
          title="Yeterlilik onayı"
          items={[
            "Okuma imzası mevcut",
            "Sınav puanı ≥ %80",
            "Pratik uygulama uygun",
            "Katılımcı ve onaylayan farklı kullanıcı",
          ]}
          action="Yeterliliği onayla"
        />
      );
    case 6:
      return (
        <MockSection
          icon={<VerifiedRounded />}
          title="Aktif yeterlilik"
          badge="12 ay geçerli"
        >
          <Box className="mock-metric-grid">
            <MiniMetric label="Tamamlanma" value="25 Ağu" />
            <MiniMetric label="Geçerlilik" value="25 Ağu 27" accent />
            <MiniMetric label="Durum" value="Aktif" />
          </Box>
          <Paper variant="outlined" className="mock-lock-message">
            <LockRounded />
            <Typography>
              M.04 doküman eğitim kapısı kapandı; sürüm yürürlüğe alınabilir.
            </Typography>
          </Paper>
        </MockSection>
      );
    default:
      return (
        <MockSection
          icon={<ReplayRounded />}
          title="Yenileme ihtiyacı"
          badge="30 gün kaldı"
        >
          <Box className="mock-check-list">
            <MockCheck text="Çalışan ve pozisyon hâlâ etkin" />
            <MockCheck text="Yeni doküman revizyonu kontrol edildi" />
            <MockCheck text="Yeni görev önceki kanıtı değiştirmeden açıldı" />
          </Box>
          <MockAction text="Yenileme görevini ata" />
        </MockSection>
      );
  }
}

function DeviationSimulation({ step }: { step: number }) {
  switch (step) {
    case 0:
      return (
        <>
          <MockSection
            icon={<DescriptionRounded />}
            title="Yeni sapma bildirimi"
            badge="Taslak"
          >
            <Box className="mock-field-grid">
              <MockField
                label="Sapma başlığı"
                value="Dolum sıcaklığı limit sapması"
                active
              />
              <MockField label="Tespit edilen bölüm" value="Üretim" />
              <MockField
                label="Uygunsuzluk tanımı"
                value="Dolum sıcaklığı 25°C limitini aştı."
                wide
              />
              <MockField
                label="Acil aksiyon"
                value="Hat durduruldu, ürün karantinaya alındı."
                wide
              />
            </Box>
          </MockSection>
          <MockAction text="Taslağı kaydet" />
        </>
      );
    case 1:
      return (
        <>
          <Box className="mock-metric-grid">
            <MiniMetric label="Olasılık" value="2" />
            <MiniMetric label="Şiddet" value="4" />
            <MiniMetric label="Tespit" value="4" />
            <MiniMetric label="RPN" value="32" accent />
          </Box>
          <MockSection
            icon={<WarningAmberRounded />}
            title="Otomatik risk kararı"
            badge="MAJÖR"
          >
            <Typography className="mock-equation">2 × 4 × 4 = 32</Typography>
            <Typography variant="body2">
              DÖF bağlantısı zorunlu. Hedef kapanış: 7 gün.
            </Typography>
          </MockSection>
          <MockAction text="Kontrollü iş akışına gönder" />
        </>
      );
    case 2:
      return (
        <MockSection
          icon={<FactCheckRounded />}
          title="KG ön inceleme"
          badge="Atanan: KG Değerlendiricisi"
        >
          <Box className="mock-check-list">
            <MockCheck text="Kapsam ve sınıflandırma uygun" />
            <MockCheck text="Acil aksiyon yeterli" />
            <MockCheck text="Araştırmacı atanacak" />
          </Box>
          <MockField
            label="Ön inceleme notu"
            value="Proses araştırması ve batch değerlendirmesi gerekli."
            active
            wide
          />
        </MockSection>
      );
    case 3:
      return (
        <MockSection
          icon={<ManageSearchRounded />}
          title="Kök neden araştırması"
          badge="5 Neden"
        >
          <Box className="mock-why-chain">
            {[
              "Sıcaklık yükseldi",
              "Sensör geç uyardı",
              "Alarm eşiği hatalı",
              "Revizyon aktarılmadı",
              "Değişiklik kontrolü eksik",
            ].map((item, index) => (
              <Box key={item}>
                <span>{index + 1}</span>
                <Typography>{item}</Typography>
                {index < 4 && <ArrowForwardRounded />}
              </Box>
            ))}
          </Box>
          <MockField
            label="Doğrulanmış kök neden"
            value="Onaylı alarm eşiği PLC reçetesine aktarılmamış."
            active
            wide
          />
        </MockSection>
      );
    case 4:
      return (
        <MockSection
          icon={<Inventory2Rounded />}
          title="Batch / seri etkisi"
          badge="2 batch"
        >
          <MockTable
            headers={["Batch", "Etkilendi mi?", "Dispozisyon"]}
            rows={[
              ["B-260824", "Evet", "Beklet"],
              ["B-260825", "Hayır", "Serbest bırak"],
            ]}
            highlightRow={0}
          />
          <MockAction text="Etki değerlendirmesini tamamla" />
        </MockSection>
      );
    case 5:
      return (
        <MockSection
          icon={<VerifiedRounded />}
          title="Kalite değerlendirmesi"
          badge="Karar bekliyor"
        >
          <Box className="mock-decision-grid">
            <DecisionCard
              title="DÖF gerekli"
              text="RPN 32 · Majör sapma"
              selected
            />
            <DecisionCard
              title="Etkinlik izlemesi"
              text="30 günlük gözlem"
              selected
            />
          </Box>
          <Paper variant="outlined" className="mock-linked-record">
            <LinkRounded />
            <Box>
              <Typography variant="caption">İLİŞKİLİ KAYIT</Typography>
              <Typography>DÖF-2026-000064 · Taslak</Typography>
            </Box>
          </Paper>
        </MockSection>
      );
    case 6:
      return (
        <MockSection
          icon={<AssignmentRounded />}
          title="Bağlı DÖF ilerlemesi"
          badge="M.02 bağlantısı"
        >
          <Typography variant="body2" color="text.secondary">
            Sapma, bağlı DÖF tamamlanana kadar kontrollü olarak bekler.
          </Typography>
          <Box className="mock-progress-card">
            <Stack direction="row" sx={{ justifyContent: "space-between" }}>
              <Typography>DÖF aksiyonları</Typography>
              <strong>2 / 3</strong>
            </Stack>
            <LinearProgress variant="determinate" value={66} />
            <Stack direction="row" spacing={1}>
              <Chip size="small" color="success" label="Sensör kalibrasyonu" />
              <Chip size="small" color="warning" label="SOP revizyonu" />
            </Stack>
          </Box>
        </MockSection>
      );
    case 7:
      return (
        <MockSection
          icon={<ScienceRounded />}
          title="Etkinlik değerlendirmesi"
          badge="30 gün"
        >
          <Box className="mock-metric-grid">
            <MiniMetric label="Tekrar sayısı" value="0" accent />
            <MiniMetric label="Uygun batch" value="12/12" />
            <MiniMetric label="Alarm testi" value="%100" />
          </Box>
          <MockField
            label="Etkinlik sonucu"
            value="Başarı kriterleri karşılandı; sapma tekrar etmedi."
            active
            wide
          />
          <MockAction text="Etkili olarak onayla" />
        </MockSection>
      );
    default:
      return (
        <MockSection
          icon={<LockRounded />}
          title="Kapanış onayı"
          badge="Onaylayan"
        >
          <Box className="mock-check-list">
            <MockCheck text="Araştırma ve etki tamamlandı" />
            <MockCheck text="Bağlı DÖF kapalı" />
            <MockCheck text="Etkinlik başarılı" />
            <MockCheck text="Açık bağımlılık yok" />
          </Box>
          <MockField
            label="Kapanış gerekçesi"
            value="Tüm kalite kontrolleri ve kanıt zinciri tamamlandı."
            active
            wide
          />
          <MockAction text="Sapma kaydını kapat" />
        </MockSection>
      );
  }
}

function CapaSimulation({ step }: { step: number }) {
  switch (step) {
    case 0:
      return (
        <>
          <MockSection
            icon={<DescriptionRounded />}
            title="Yeni DÖF kaydı"
            badge="Taslak"
          >
            <Box className="mock-field-grid">
              <MockField
                label="Problem tanımı"
                value="Alarm eşiği reçeteye aktarılmamış."
                active
              />
              <MockField label="Sorumlu" value="Kalite Güvence" />
              <MockField
                label="Doğrulanmış kök neden"
                value="Değişiklik kontrolü kapanış kontrol listesi eksik."
                wide
              />
              <MockField label="Hedef tarih" value="30.09.2026" />
            </Box>
          </MockSection>
          <MockAction text="Kapsam onayına gönder" />
        </>
      );
    case 1:
      return (
        <ApprovalScreen
          title="Kapsam yeterliliği"
          items={[
            "Etkilenen prosesler tanımlı",
            "Sapma kaynağı bağlı",
            "Düzenleyici etki değerlendirildi",
          ]}
          action="Kapsamı onayla"
        />
      );
    case 2:
      return (
        <ApprovalScreen
          title="Kök neden kanıtları"
          items={[
            "5 Neden analizi tamamlandı",
            "Kök neden kanıtla doğrulandı",
            "Semptom ile kök neden ayrıldı",
          ]}
          action="Kök nedeni onayla"
        />
      );
    case 3:
      return (
        <MockSection
          icon={<AssignmentRounded />}
          title="Aksiyon planlama"
          badge="2 aksiyon"
        >
          <MockTable
            headers={["Tür", "Aksiyon", "Sorumlu", "Hedef"]}
            rows={[
              ["Düzeltici", "PLC reçetesini güncelle", "Otomasyon", "05 Eyl"],
              ["Önleyici", "SOP kontrol adımı ekle", "KG", "12 Eyl"],
            ]}
            highlightRow={1}
          />
          <MockAction text="Yeni aksiyon ekle" />
        </MockSection>
      );
    case 4:
      return (
        <MockSection
          icon={<LockRounded />}
          title="Plan onayı ve sabitleme"
          badge="Revizyon 1"
        >
          <Box className="mock-check-list">
            <MockCheck text="Sorumlular görevi kabul etti" />
            <MockCheck text="Hedef tarihler riskle uyumlu" />
            <MockCheck text="Etkinlik planı: 30 gün / 12 batch" />
          </Box>
          <Paper variant="outlined" className="mock-lock-message">
            <LockRounded />
            <Typography>
              Onayla birlikte plan alanları kilitlenir ve sonraki değişiklikler
              revizyon olarak izlenir.
            </Typography>
          </Paper>
          <MockAction text="Planı onayla" />
        </MockSection>
      );
    case 5:
      return (
        <MockSection
          icon={<UploadFileRounded />}
          title="Aksiyon tamamlama kanıtı"
          badge="Aksiyon sorumlusu"
        >
          <MockField
            label="Tamamlama açıklaması"
            value="PLC reçetesi onaylı eşiklerle güncellendi."
            active
            wide
          />
          <Paper variant="outlined" className="mock-upload">
            <UploadFileRounded />
            <Box>
              <Typography>PLC_Test_Raporu_v2.pdf</Typography>
              <Typography variant="caption">
                2.4 MB · elektronik imza doğrulandı
              </Typography>
            </Box>
            <Chip color="success" size="small" label="Yüklendi" />
          </Paper>
          <MockAction text="Tamamlandı bildir" />
        </MockSection>
      );
    case 6:
      return (
        <MockSection
          icon={<FactCheckRounded />}
          title="KG kanıt doğrulaması"
          badge="Karşılaştırmalı inceleme"
        >
          <Box className="mock-compare">
            <DecisionCard title="Planlanan" text="PLC alarm eşiğini 25°C yap" />
            <DecisionCard
              title="Kanıtlanan"
              text="Test sonucu: 25°C alarmı başarılı"
              selected
            />
          </Box>
          <MockField
            label="KG doğrulama notu"
            value="Kanıt planlanan aksiyonu tam olarak karşılıyor."
            active
            wide
          />
          <MockAction text="Kanıtı doğrula" />
        </MockSection>
      );
    case 7:
      return (
        <MockSection
          icon={<CalendarMonthRounded />}
          title="Etkinlik gözlem süresi"
          badge="18 gün kaldı"
        >
          <Box className="mock-calendar">
            <CalendarMonthRounded />
            <Box>
              <Typography variant="h5">12 Eki 2026</Typography>
              <Typography color="text.secondary">
                Etkinlik değerlendirmesinin açılacağı tarih
              </Typography>
            </Box>
          </Box>
          <LinearProgress variant="determinate" value={40} />
          <Typography variant="caption">
            12 / 30 günlük gözlem tamamlandı
          </Typography>
        </MockSection>
      );
    case 8:
      return (
        <MockSection
          icon={<ScienceRounded />}
          title="Etkinlik başarı kriterleri"
          badge="Değerlendiren"
        >
          <MockTable
            headers={["Kriter", "Hedef", "Sonuç"]}
            rows={[
              ["Sapma tekrarı", "0", "0"],
              ["Uygun batch", "≥ 10", "12"],
              ["Alarm testi", "%100", "%100"],
            ]}
            highlightRow={2}
          />
          <Box className="mock-decision-grid">
            <DecisionCard
              title="Etkili"
              text="Kapanış onayına gönder"
              selected
            />
            <DecisionCard title="Etkisiz" text="Aksiyon planına geri dön" />
          </Box>
        </MockSection>
      );
    default:
      return (
        <MockSection
          icon={<TaskAltRounded />}
          title="DÖF kapanış kontrolü"
          badge="Onaylayan"
        >
          <Box className="mock-check-list">
            <MockCheck text="Tüm aksiyonlar KG tarafından doğrulandı" />
            <MockCheck text="Etkinlik kriterleri karşılandı" />
            <MockCheck text="Açık görev ve bağımlılık yok" />
            <MockCheck text="Maker-checker kontrolü başarılı" />
          </Box>
          <MockField
            label="Kapanış notu"
            value="DÖF hedeflenen kalite sonucuna ulaşmıştır."
            active
            wide
          />
          <MockAction text="DÖF kaydını kapat" />
        </MockSection>
      );
  }
}

function ChangeSimulation({ step }: { step: number }) {
  switch (step) {
    case 0:
      return (
        <>
          <MockSection
            icon={<DescriptionRounded />}
            title="Yeni değişiklik kaydı"
            badge="Taslak"
          >
            <Box className="mock-field-grid">
              <MockField
                label="Mevcut durum"
                value="PLC alarm eşiği 28°C"
                active
              />
              <MockField label="Önerilen durum" value="Onaylı eşik 25°C" />
              <MockField
                label="Etkilenen bölümler"
                value="Üretim · KG · Validasyon"
                wide
              />
              <MockField
                label="Geri dönüş planı"
                value="Önceki PLC reçetesini kontrollü geri yükle"
                wide
              />
            </Box>
          </MockSection>
          <MockAction text="Değişiklik taslağını kaydet" />
        </>
      );
    case 1:
      return (
        <MockSection
          icon={<FactCheckRounded />}
          title="KG ön değerlendirme"
          badge="Yüksek risk"
        >
          <Box className="mock-metric-grid">
            <MiniMetric label="Tür" value="Süreç" />
            <MiniMetric label="Kalıcı" value="Evet" />
            <MiniMetric label="Validasyon" value="Gerekli" accent />
            <MiniMetric label="Ruhsat" value="Varyasyon" />
          </Box>
          <Box className="mock-check-list">
            <MockCheck text="Kapsam yeterli" />
            <MockCheck text="Geri dönüş planı uygulanabilir" />
            <MockCheck text="Paralel değerlendirme kapsamı doğru" />
          </Box>
        </MockSection>
      );
    case 2:
      return (
        <MockSection
          icon={<GroupsRounded />}
          title="Paralel bölüm değerlendirmeleri"
          badge="2 / 3 tamamlandı"
        >
          <MockTable
            headers={["Bölüm", "Durum", "Gerekli çıktı"]}
            rows={[
              ["Üretim", "Uygun", "Hat testi"],
              ["Kalite Güvence", "Uygun", "SOP revizyonu"],
              ["Ruhsatlandırma", "Bekliyor", "Varyasyon görüşü"],
            ]}
            highlightRow={2}
          />
          <MockAction text="Değişiklik kuruluna gönder" />
        </MockSection>
      );
    case 3:
      return (
        <MockSection
          icon={<RuleRounded />}
          title="Değişiklik kurulu kararı"
          badge="Kurul gündemi"
        >
          <Box className="mock-decision-grid">
            <DecisionCard
              title="Fayda / gerekçe"
              text="Tekrar eden sıcaklık sapmasını önler"
              selected
            />
            <DecisionCard
              title="Kalan risk"
              text="Validasyon ile kabul edilebilir"
              selected
            />
          </Box>
          <MockField
            label="Kurul karar gerekçesi"
            value="Koşullu onay: Validasyon ve varyasyon belgesi tamamlanmalı."
            active
            wide
          />
          <MockAction text="Kurul kararını onayla" />
        </MockSection>
      );
    case 4:
      return (
        <MockSection
          icon={<AssignmentRounded />}
          title="Onayla sabitlenecek uygulama planı"
          badge="4 aksiyon"
        >
          <MockTable
            headers={["Kategori", "Çıktı", "Sorumlu", "Hedef"]}
            rows={[
              ["Doküman", "SOP revizyonu", "Doküman Kontrol", "05 Eyl"],
              ["Eğitim", "Operatör eğitimi", "Eğitim", "08 Eyl"],
              ["Validasyon", "PQ raporu", "Validasyon", "12 Eyl"],
              ["Teknik", "PLC güncelleme", "Otomasyon", "12 Eyl"],
            ]}
            highlightRow={2}
          />
        </MockSection>
      );
    case 5:
      return (
        <MockSection
          icon={<UploadFileRounded />}
          title="Uygulama kanıtları"
          badge="3 / 4 doğrulandı"
        >
          <Box className="mock-progress-card">
            <Stack direction="row" sx={{ justifyContent: "space-between" }}>
              <Typography>Devreye alma kapıları</Typography>
              <strong>%75</strong>
            </Stack>
            <LinearProgress variant="determinate" value={75} />
          </Box>
          <Paper variant="outlined" className="mock-upload" sx={{ mt: 1.5 }}>
            <UploadFileRounded />
            <Box>
              <Typography>PQ_Raporu_v3.pdf</Typography>
              <Typography variant="caption">
                Elektronik imza ve içerik özeti doğrulandı
              </Typography>
            </Box>
            <Chip color="success" size="small" label="KG doğruladı" />
          </Paper>
        </MockSection>
      );
    case 6:
      return (
        <MockSection
          icon={<VerifiedRounded />}
          title="Devreye alma onayı"
          badge="Ayrı e-imza"
        >
          <Box className="mock-check-list">
            <MockCheck text="Tüm bloklayıcı aksiyonlar doğrulandı" />
            <MockCheck text="Otorite belgesi: TR-VAR-2026-184" />
            <MockCheck text="Doküman ve eğitim bağımlılıkları kapalı" />
          </Box>
          <Paper variant="outlined" className="mock-lock-message">
            <LockRounded />
            <Typography>
              Bu imza değişikliği devreye alır; kalite kaydını kapatmaz.
            </Typography>
          </Paper>
          <MockAction text="Devreye almayı onayla" />
        </MockSection>
      );
    case 7:
      return (
        <MockSection
          icon={<ScienceRounded />}
          title="Uygulama sonrası doğrulama"
          badge="30 günlük izleme"
        >
          <Box className="mock-metric-grid">
            <MiniMetric label="Alarm testi" value="%100" accent />
            <MiniMetric label="Uygun batch" value="12/12" />
            <MiniMetric label="Tekrar" value="0" />
          </Box>
          <Box className="mock-decision-grid">
            <DecisionCard
              title="Başarılı"
              text="Nihai kapanışa ilerle"
              selected
            />
            <DecisionCard
              title="Başarısız"
              text="Kontrollü geri dönüşü başlat"
            />
          </Box>
        </MockSection>
      );
    default:
      return (
        <MockSection
          icon={<TaskAltRounded />}
          title="Nihai kapanış kontrolü"
          badge="Onaylayan"
        >
          <Box className="mock-check-list">
            <MockCheck text="Devreye alma imzası mevcut" />
            <MockCheck text="Uygulama sonrası doğrulama başarılı" />
            <MockCheck text="Doküman, eğitim, validasyon ve risk kapalı" />
            <MockCheck text="Açık sapma veya DÖF bağımlılığı yok" />
          </Box>
          <MockField
            label="Kapanış gerekçesi"
            value="Değişiklik hedeflenen sonucu sağladı ve tüm kanıt zinciri tamamlandı."
            active
            wide
          />
          <MockAction text="Değişiklik kaydını kapat" />
        </MockSection>
      );
  }
}

function ApprovalScreen({
  title,
  items,
  action,
}: {
  title: string;
  items: string[];
  action: string;
}) {
  return (
    <MockSection icon={<VerifiedRounded />} title={title} badge="Onaylayan">
      <Box className="mock-check-list">
        {items.map((text) => (
          <MockCheck key={text} text={text} />
        ))}
      </Box>
      <MockField
        label="Onay notu"
        value="Kapsam ve sunulan kanıtlar yeterli bulundu."
        active
        wide
      />
      <MockAction text={action} />
    </MockSection>
  );
}
function MockSection({
  icon,
  title,
  badge,
  children,
}: {
  icon: ReactNode;
  title: string;
  badge: string;
  children: ReactNode;
}) {
  return (
    <Paper variant="outlined" className="mock-section">
      <Stack direction="row" className="mock-section-heading">
        <Stack direction="row" spacing={1}>
          <Box className="mock-section-icon">{icon}</Box>
          <Typography sx={{ fontWeight: 800 }}>{title}</Typography>
        </Stack>
        <Chip size="small" label={badge} />
      </Stack>
      {children}
    </Paper>
  );
}
function MockField({
  label,
  value,
  active = false,
  wide = false,
}: {
  label: string;
  value: string;
  active?: boolean;
  wide?: boolean;
}) {
  return (
    <Box
      className={`mock-field ${active ? "is-highlighted" : ""} ${wide ? "is-wide" : ""}`}
    >
      <Typography variant="caption">{label}</Typography>
      <Typography>{value}</Typography>
      {active && <span className="mock-focus-pulse" />}
    </Box>
  );
}
function MockAction({ text }: { text: string }) {
  return (
    <Stack direction="row" className="mock-action-row">
      <Button size="small">Vazgeç</Button>
      <Button size="small" variant="contained">
        {text}
      </Button>
    </Stack>
  );
}
function MockCheck({ text }: { text: string }) {
  return (
    <Stack direction="row" spacing={1}>
      <CheckCircleRounded />
      <Typography variant="body2">{text}</Typography>
    </Stack>
  );
}
function MiniMetric({
  label,
  value,
  accent = false,
}: {
  label: string;
  value: string;
  accent?: boolean;
}) {
  return (
    <Paper
      variant="outlined"
      className={`mock-metric ${accent ? "is-accent" : ""}`}
    >
      <Typography variant="caption">{label}</Typography>
      <Typography variant="h5">{value}</Typography>
    </Paper>
  );
}
function DecisionCard({
  title,
  text,
  selected = false,
}: {
  title: string;
  text: string;
  selected?: boolean;
}) {
  return (
    <Paper
      variant="outlined"
      className={`mock-decision ${selected ? "is-selected" : ""}`}
    >
      <Stack direction="row" spacing={1}>
        <span>{selected && <CheckCircleRounded />}</span>
        <Box>
          <Typography sx={{ fontWeight: 750 }}>{title}</Typography>
          <Typography variant="caption">{text}</Typography>
        </Box>
      </Stack>
    </Paper>
  );
}
function MockTable({
  headers,
  rows,
  highlightRow = -1,
}: {
  headers: string[];
  rows: string[][];
  highlightRow?: number;
}) {
  return (
    <Box className="mock-table">
      <Box className="mock-table-row is-head">
        {headers.map((item) => (
          <span key={item}>{item}</span>
        ))}
      </Box>
      {rows.map((row, index) => (
        <Box
          className={`mock-table-row ${index === highlightRow ? "is-highlighted" : ""}`}
          key={row.join("-")}
        >
          {row.map((item) => (
            <span key={item}>{item}</span>
          ))}
        </Box>
      ))}
    </Box>
  );
}
