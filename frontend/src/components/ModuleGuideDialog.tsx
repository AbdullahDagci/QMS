import type { ReactNode } from "react";
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogContent,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import {
  AccountTreeRounded,
  ArrowForwardRounded,
  AssignmentTurnedInRounded,
  GroupsRounded,
  InfoOutlined,
  LinkRounded,
  LockRounded,
  MenuBookRounded,
  RuleRounded,
  SecurityRounded,
  VerifiedRounded,
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

type GuideStep = {
  title: string;
  actor: string;
  permission: string;
  action: string;
  result: string;
  control: string;
};

type GuideRole = {
  role: string;
  taskRole: string;
  permission: string;
  responsibility: string;
};

type GuideConnection = {
  process: string;
  direction: string;
  trigger: string;
  communication: string;
  data: string;
  dependency: string;
};

type ModuleGuide = {
  name: string;
  purpose: string;
  entry: string;
  steps: GuideStep[];
  roles: GuideRole[];
  rules: string[];
  connections: GuideConnection[];
};

const step = (
  title: string,
  actor: string,
  permission: string,
  action: string,
  result: string,
  control: string,
): GuideStep => ({ title, actor, permission, action, result, control });

const guides: Record<ModuleCode, ModuleGuide> = {
  "M.01": {
    name: "Sapma Yönetimi",
    purpose:
      "Bir uygunsuzluğu ilk bildirimden araştırma, etki ve kalite değerlendirmesine; gerekiyorsa DÖF ve etkinlik kontrolü üzerinden kontrollü kapanışa taşır.",
    entry:
      "Sapma Yönetimi listesinden “Yeni sapma” seçilir. Kayıt açmak için deviation.create izni ve organizasyonda etkin bir bölüm gerekir.",
    steps: [
      step(
        "Sapma kaydını oluştur",
        "Sapma Bildiren · Initiator",
        "deviation.create",
        "Başlık, gerçekleşen ve beklenen durum, acil aksiyon, tür, bölüm, proses aşaması ve olay/tespit zamanlarını girer. Olasılık × şiddet × tespit edilebilirlik değerlerini seçer.",
        "Sistem RPN, sınıf, hedef kapanış tarihi ve DÖF gerekliliğini hesaplayarak taslak kaydı oluşturur.",
        "Zorunlu alanlar, tarih sırası ve kullanıcının etkin bölüm kaydı API tarafından doğrulanır.",
      ),
      step(
        "Kaydı gönder",
        "Sapma Bildiren · Initiator",
        "deviation.create",
        "Taslağı kontrol eder ve gönderir.",
        "Initiator görevi kapanır; atama matrisi RPN, sapma türü ve bölüme göre ProcessAuthority görevini uygun KG kullanıcısına verir.",
        "Eşleşen etkin M.01 atama kuralı ve atanabilir KG kullanıcısı bulunmalıdır.",
      ),
      step(
        "Ön incelemeyi tamamla",
        "İşlem Yetkilisi · ProcessAuthority",
        "deviation.manage + kayıt görevi",
        "Kapsamı ve ilk aksiyonları inceler, karar notunu girer ve araştırmaya gönderir.",
        "Elektronik imza alınır; kayıt Investigation durumuna geçer ve Investigator görevi açılır.",
        "Kaydı oluşturan kişi kendi kalite kararını veremez; parola ve imza anlamı yeniden doğrulanır.",
      ),
      step(
        "Araştırmayı kaydet",
        "Araştırmacı · Investigator",
        "deviation.investigate + kayıt görevi",
        "Yöntem, kök neden kategorisi, bulgular ve araştırma sonucunu kaydeder; ardından araştırmayı tamamlar.",
        "En az bir tamamlanmış araştırma ile kayıt ImpactAssessment aşamasına geçer.",
        "Yalnız atanmış araştırmacı veya kapsamı M.01 olan etkin delege işlem yapabilir.",
      ),
      step(
        "Batch/seri etkisini değerlendir",
        "Araştırmacı · Investigator",
        "deviation.investigate + kayıt görevi",
        "Etkilenen batch veya serileri, etki açıklamasını ve dispozisyon kararını ayrı ayrı kaydeder.",
        "Tüm güncel batch kararları kesinleşince kayıt QualityAssessment aşamasına ve Evaluator görevine geçer.",
        "Pending dispozisyonlu batch/seri varsa geçiş engellenir.",
      ),
      step(
        "KG değerlendirmesini onayla",
        "Değerlendiren · Evaluator",
        "deviation.manage + kayıt görevi",
        "Kalite değerlendirme notunu, etkinlik gerekliliğini ve ilişkili DÖF durumunu karara bağlar.",
        "DÖF gerekiyorsa ActionImplementation; yalnız etkinlik gerekiyorsa EffectivenessReview; aksi halde ClosureApproval açılır.",
        "Majör/kritik veya DÖF gerekli kayıtta ilişkili M.02 bulunmalıdır. Karar elektronik imzalıdır.",
      ),
      step(
        "Bağlı DÖF aksiyonlarını izle",
        "İşlem Yetkilisi · ProcessAuthority",
        "deviation.manage + kayıt görevi",
        "Sapma içinden ilişkili M.02 kayıtlarını izler ve tümünün kapandığını doğrular.",
        "Bağlı DÖF'ler kapalıysa süreç etkinlik incelemesine veya kapanış onayına ilerler.",
        "En az bir bağlı DÖF olmalı ve açık hiçbir ilişkili DÖF kalmamalıdır.",
      ),
      step(
        "Etkinliği doğrula",
        "Değerlendiren · Evaluator",
        "deviation.manage + kayıt görevi",
        "Tekrar oluşumu, başarı kriterini ve gözlem kanıtını değerlendirerek etkili/etkisiz kararını imzalar.",
        "Etkili sonuç ClosureApproval açar; etkisiz sonuç kayıt üzerinde yeni aksiyon gerektirir.",
        "Karar notu, parola ve elektronik imza anlamı zorunludur.",
      ),
      step(
        "Sapmayı kapat",
        "Onaylayan · Approver",
        "deviation.manage + kayıt görevi",
        "Kapanış gerekçesini girer, açık görev ve bağımlılıkları kontrol ederek nihai onayı verir.",
        "Kayıt Closed olur, kalite kaydı kapanır ve nihai rapor üretilir.",
        "Oluşturan kişi kapanış onayı veremez; açık DÖF/bağımlılık ve eksik elektronik imza kapanışı engeller.",
      ),
    ],
    roles: [
      { role: "DeviationReporter", taskRole: "Initiator", permission: "deviation.create", responsibility: "Sapmayı oluşturur, risk girdilerini tamamlar ve gönderir." },
      { role: "QualityAssurance", taskRole: "ProcessAuthority", permission: "deviation.manage", responsibility: "Ön incelemeyi ve bağlı DÖF aksiyon kontrolünü yürütür." },
      { role: "Investigator", taskRole: "Investigator", permission: "deviation.investigate", responsibility: "Araştırmayı ve batch/seri etki değerlendirmesini tamamlar." },
      { role: "QualityAssurance", taskRole: "Evaluator", permission: "deviation.manage", responsibility: "KG ve etkinlik kararlarını elektronik imzayla verir." },
      { role: "Approver", taskRole: "Approver", permission: "deviation.manage", responsibility: "Bağımsız nihai kapanış onayını verir." },
    ],
    rules: [
      "Sistem rolü tek başına yeterli değildir; kullanıcı ilgili kaydın etkin görevine de atanmış olmalıdır.",
      "M.01 atama matrisi ProcessAuthority, Investigator, Evaluator ve Approver görevlerini sapma türü, bölüm ve minimum RPN'e göre dağıtır.",
      "Ön inceleme, KG kararı, etkinlik ve kapanış geçişleri yeniden parola girilen elektronik imzadır.",
      "ALL veya M.01 kapsamlı, tarih aralığı geçerli delegasyon atanan kullanıcı adına işlem yapabilir.",
    ],
    connections: [
      {
        process: "M.02 · DÖF Yönetimi",
        direction: "M.01 → M.02 → M.01",
        trigger: "M.01 detayından DÖF oluşturulurken CreateCapaRequest.SourceDeviationId alanına sapma kimliği gönderilir.",
        communication: "M.02 kaydı SourceDeviationId ile M.01'e bağlanır. M.01 detay servisi aynı kimlikle bağlı DÖF listesini ve her DÖF'ün durumunu sorgular. Aynı sapma için ikinci DÖF oluşturma isteği mevcut kaydı döndürür.",
        data: "Sapma kimliği, kaynak türü, başlık/problem, kök neden, acil aksiyon, DÖF sahibi, hedef tarih ve etkinlik planı.",
        dependency: "DÖF gerekli M.01 kaydı bağlantı olmadan KG değerlendirmesini geçemez. ActionImplementation ve nihai kapanışta SourceDeviationId ile bağlı açık DÖF aranır; tümü Closed olmadan M.01 ilerlemez.",
      },
      {
        process: "M.06 · Müşteri Şikâyetleri",
        direction: "M.06 → M.01",
        trigger: "M.06 complete-triage geçişinde ComplaintType=ProductQuality ve önem derecesi Minor dışında ise M.01 kaydı otomatik oluşturulur.",
        communication: "M.06, oluşturduğu sapmanın kimliğini Complaint.LinkedDeviationId alanında saklar. M.01 audit event'i kaynak complaintId, product ve batchNumber değerlerini taşır.",
        data: "Ürün ve batch, şikâyet açıklaması, olay/alınma zamanı, önemden türetilen şiddet, trendden türetilen olasılık; sapma türü “Müşteri Şikâyeti” olarak atanır.",
        dependency: "Bağlantı triyaj işlemiyle aynı veritabanı transaction'ında oluşturulur. M.01 kapanışı M.06 durumunu sorgulamaz; bu bağ kodda izlenebilirlik yönündedir.",
      },
      {
        process: "M.03 · Değişiklik Kontrol",
        direction: "M.01 → M.02 → M.03 (dolaylı)",
        trigger: "M.01'e bağlı DÖF üzerinden yeni M.03 kaydı açılırken DÖF kimliği SourceCapaId olarak seçilebilir.",
        communication: "M.03 doğrudan sapma kimliği tutmaz; kaynak numarası M.02 SourceCapaId ilişkisi üzerinden gösterilir.",
        data: "M.01 kimliği M.02 SourceDeviationId'de, M.02 kimliği M.03 SourceCapaId'de tutulur.",
        dependency: "M.01 servisi M.03 durumunu doğrudan sorgulamaz. M.01'in teknik kapanış kapısı bağlı M.02 durumudur.",
      },
    ],
  },
  "M.02": {
    name: "DÖF Yönetimi",
    purpose:
      "Doğrulanmış bir problem ve kök nedeni, sahipli aksiyonlar, kanıt, KG doğrulaması ve etkinlik ölçümüyle kalıcı olarak ortadan kaldırır.",
    entry:
      "DÖF Yönetimi listesinden bağımsız DÖF açılabilir; ayrıca M.01 sapma, şikâyet veya denetim bulgusu kaynaklı DÖF sistem tarafından bağlanabilir. Oluşturma/planlama için capa.plan gerekir.",
    steps: [
      step("DÖF kaydını oluştur", "DÖF Başlatan · Initiator", "capa.plan", "Başlık, problem, doğrulanmış kök neden, acil düzeltme, sorumlu, hedef tarih ve gerekiyorsa etkinlik planını girer.", "Taslak DÖF ve Initiator görevi oluşur; kaynak M.01 varsa iki kayıt ilişkilendirilir.", "Etkinlik seçildiyse yöntem, numune, gözlem süresi ve başarı kriteri zorunludur."),
      step("Kapsam onayına gönder", "DÖF Başlatan · Initiator", "capa.plan + kayıt görevi", "Taslağı gönderir.", "Kayıt ScopeApproval olur ve oluşturandan farklı Approver atanır.", "Maker-checker gereği oluşturan kullanıcı onay adımlarını veremez."),
      step("Kapsamı onayla", "Onaylayan · Approver", "capa.manage + kayıt görevi", "DÖF kapsamının problemi, kaynağı ve etkilenen süreci yeterince kapsadığını değerlendirip onaylar.", "Kayıt RootCauseApproval durumuna geçer; aynı kayıt bazlı Approver görevi kök neden kararı için devam eder.", "Kaydı oluşturan kullanıcı kapsam onayını veremez; karar elektronik imzalıdır."),
      step("Kök nedeni onayla", "Onaylayan · Approver", "capa.manage + kayıt görevi", "Doğrulanmış kök neden açıklamasını ve kanıtını kontrol ederek onaylar.", "ActionPlanning açılır; Approver görevi kapanır ve oluşturandan farklı QualityAssurance kullanıcısına ProcessAuthority görevi atanır.", "Kök neden doğrulanmadan aksiyon planı hazırlanamaz; karar elektronik imzalıdır."),
      step("Aksiyon planını hazırla", "İşlem Yetkilisi · ProcessAuthority", "capa.plan + kayıt görevi", "Her düzeltici/önleyici aksiyona açıklama, etkin kullanıcı sahibi ve hedef tarih ekler; planı onaya gönderir.", "PlanApproval aşaması ve bağımsız Approver görevi açılır.", "En az bir aksiyon olmalı; aksiyon sahibi ActionOwner veya QualityAssurance rolündeki etkin kullanıcı olmalıdır."),
      step("Planı onayla", "Onaylayan · Approver", "capa.manage + kayıt görevi", "Aksiyon kapsamı, sahipleri, tarihler ve etkinlik planını elektronik imzayla onaylar.", "Her aksiyon sahibine kayıt bazlı ActionOwner görevi atanır ve Implementation başlar.", "Onaydan sonra tamamlanma kanıtı aksiyon bazında izlenir."),
      step("Aksiyonu tamamlanmak üzere gönder", "Aksiyon Sahibi · ActionOwner", "capa.complete-action + aksiyon görevi", "Kendi aksiyonuna uygulama kanıtını girer ve KG doğrulamasına gönderir.", "Tüm aksiyonlar kanıtla bildirildiğinde ActionVerification aşaması açılır.", "“Tamamlandı” bildirimi KG onayı değildir; kullanıcı yalnız kendisine atanmış aksiyonda işlem yapar."),
      step("Aksiyon kanıtlarını doğrula", "Değerlendiren · Evaluator", "capa.verify + kayıt görevi", "Her kanıtı planlanan çıktı ile karşılaştırır; onaylar veya gerekçeyle sahibine geri gönderir.", "Tüm aksiyonlar onaylanınca etkinlik gerekiyorsa EffectivenessWaiting, gerekmiyorsa ClosureApproval açılır.", "Tek reddedilen aksiyon sahibine yeni görev açar ve sürecin ilerlemesini durdurur."),
      step("Etkinlik bekleme süresini tamamla", "Değerlendiren · Evaluator", "capa.verify + kayıt görevi", "Onaylı gözlem süresi ve etkinlik hedef tarihi dolduğunda incelemeyi başlatır.", "Kayıt EffectivenessWaiting durumundan EffectivenessReview durumuna geçer.", "Gözlem süresi dolmadan etkinlik incelemesi başlatılamaz."),
      step("Etkinlik sonucunu kararla", "Değerlendiren · Evaluator", "capa.verify + kayıt görevi", "Ölçüm sonuçlarını yöntem, numune ve başarı kriteriyle karşılaştırıp etkili/etkisiz kararını verir.", "Etkili sonuç ClosureApproval açar; etkisiz sonuç DÖF'ü ActionPlanning'e geri döndürür.", "Sonuç notu, yeniden parola ve elektronik imza anlamı zorunludur."),
      step("DÖF kaydını kapat", "Onaylayan · Approver", "capa.manage + kayıt görevi", "Tüm aksiyon, kanıt ve etkinlik zincirini kontrol edip kapanış notunu imzalar.", "DÖF Closed olur; kaynak M.01 veya denetim kapanış kapısı bu durumu okuyabilir.", "Oluşturan kullanıcı kapatamaz; açık görev, eksik kanıt veya başarısız etkinlik kapanışı engeller."),
    ],
    roles: [
      { role: "QualityAssurance", taskRole: "Initiator / ProcessAuthority", permission: "capa.plan", responsibility: "DÖF'ü açar, kapsamı tanımlar ve aksiyon planını hazırlar." },
      { role: "Approver", taskRole: "Approver", permission: "capa.manage", responsibility: "Kapsam, kök neden, plan ve kapanış onaylarını verir." },
      { role: "ActionOwner", taskRole: "Action:{id}", permission: "capa.complete-action", responsibility: "Kendisine atanan aksiyonu kanıtıyla tamamlanmaya gönderir." },
      { role: "QualityAssurance", taskRole: "Evaluator", permission: "capa.verify", responsibility: "Aksiyon kanıtlarını ve etkinliği bağımsız olarak doğrular." },
    ],
    rules: [
      "Yetki, sistem rolü/permission ile kayıt veya aksiyon bazlı etkin görevin birlikte sağlanmasıyla oluşur.",
      "Kapsam, kök neden, plan, etkinlik ve kapanış onaylarında görev ayrılığı uygulanır.",
      "ALL veya M.02 kapsamlı geçerli delegasyon, atanmış görev sahibinin yetkisini süreli olarak devredebilir.",
      "Kaynak M.01, şikâyet veya denetim kaydı DÖF kapanmadan kendi kapanış kapısını geçemez.",
    ],
    connections: [
      {
        process: "M.01 · Sapma Yönetimi",
        direction: "M.01 → M.02 → M.01",
        trigger: "DÖF, SourceDeviationId verilerek oluşturulur; kaynak sapmanın varlığı doğrulanır ve aynı sapma için tek kayıt korunur.",
        communication: "M.02 SourceDeviationId ve kaynak kayıt numarasını döndürür. M.01 bağlı DÖF'leri SourceDeviationId ile sorgular.",
        data: "Kaynak sapma kimliği ve numarası; DÖF başlığı, problem, kök neden, aksiyonlar, hedef tarih, durum ve etkinlik sonucu.",
        dependency: "M.01, bağlı M.02 kaydı Closed değilse ilişkili aksiyon ve kapanış geçişlerini reddeder.",
      },
      {
        process: "M.06 · Müşteri Şikâyetleri",
        direction: "M.06 → M.02",
        trigger: "M.06 CapaDecision aşamasında CapaRequired=true kararı verildiğinde M.02 kaydı aynı transaction içinde otomatik oluşturulur.",
        communication: "M.06 oluşturulan DÖF kimliğini Complaint.LinkedCapaId alanında saklar; M.02 audit event'i complaintId ve complaintNumber taşır. M.02 üzerinde SourceType=Complaint kullanılır.",
        data: "Ürün bazlı DÖF başlığı, şikâyet açıklaması, doğrulanmış kök neden, müşteri etkisi acil aksiyonu, sahip, hedef tarih ve trend etkinlik planı.",
        dependency: "CapaRequired=true iken LinkedCapaId olmadan M.06 FinalResponseApproval'a geçemez. M.06 Close metodu M.02 durumunu sorgulamaz; kapanış kapısı onaylı nihai yanıt ve gerekiyorsa FV aktarımıdır.",
      },
      {
        process: "M.07 / M.08 / M.09 · Denetim Bulguları",
        direction: "Denetim bulgusu → M.02 → bulgu",
        trigger: "M.07'de risk skoru 12 ve üzeri; M.08/M.09'da Major veya Critical sınıfı ya da kullanıcının CapaRequired seçimi M.02'yi bulguyla birlikte oluşturur.",
        communication: "Bulgu LinkedCapaId tutar; M.02 SourceType sırasıyla InternalAudit, ExternalAudit veya SupplierAudit olur. Her iki kaydın audit event'i karşı kaydın kimliğini taşır.",
        data: "Bulgu başlığı/açıklaması/referansı, bulgu sahibi, cevap hedefi, kaynak denetim kimliği ve kaynağa özel etkinlik yöntemi.",
        dependency: "Denetim bulgusu kapanırken LinkedCapaId ile M.02 Status=Closed kontrol edilir. DÖF açıkken bulgu; bulgular açıkken denetim kapanamaz.",
      },
      {
        process: "M.03 · Değişiklik Kontrol",
        direction: "M.02 → M.03",
        trigger: "M.03 oluşturma isteğinde isteğe bağlı SourceCapaId seçilir ve kaynak DÖF'ün varlığı doğrulanır.",
        communication: "M.03 SourceCapaId alanını saklar ve listede/detayda DÖF kayıt numarasını join ile gösterir.",
        data: "Yalnız DÖF kimliği doğrudan taşınır; değişiklik kapsamı, risk ve plan verileri M.03 formunda ayrıca girilir.",
        dependency: "Kodda M.02 kapanışını M.03 durumuna bağlayan karşılıklı bir kapanış kontrolü yoktur; ilişki kaynak izlenebilirliği sağlar.",
      },
    ],
  },
  "M.03": {
    name: "Değişiklik Kontrol",
    purpose: "Bir proses, ekipman, doküman veya ürün değişikliğini etki değerlendirmeleri ve bağımsız onaylarla kontrollü biçimde devreye alır.",
    entry: "Değişiklik Kontrol listesinden yeni kayıt açılır. change.create izni, geri dönüş planı ve en az bir etkilenen bölüm gerekir.",
    steps: [
      step("Talebi oluştur ve gönder", "Başlatan · Initiator", "change.create", "Mevcut/önerilen durum, gerekçe, kapsam, risk, bölümler ve geri dönüş planını girer.", "PreliminaryReview ve KG ProcessAuthority görevi açılır.", "Oluşturan kendi onayını veremez."),
      step("Ön değerlendirmeyi tamamla", "KG · ProcessAuthority", "change.review + kayıt görevi", "Sınıf, risk, süre ve bölüm kapsamını doğrular.", "DepartmentReview başlar; bölüm/Ruhsat değerlendirme görevleri açılır.", "Tüm değerlendirme sahipleri etkin kullanıcı olmalıdır."),
      step("Paralel etkileri değerlendir", "Bölüm İnceleyicileri + Evaluator", "change.review + değerlendirme görevi", "Her bölüm etki, gereksinim ve kararını girer; KG kurula gönderir.", "BoardReview ve ChangeBoard görevi açılır.", "Tüm bölüm görüşleri tamamlanmadan kurul geçişi açılmaz."),
      step("Kurul ve plan onayı", "ChangeBoard → Approver", "change.approve", "Kurul kararı imzalanır; doküman, eğitim, validasyon ve uygulama aksiyonları planlanıp onaylanır.", "Implementation ve aksiyon sahibi görevleri açılır.", "Reddedilmiş etki ve aksiyonsuz plan ilerleyemez."),
      step("Uygula ve doğrula", "Aksiyon Sahibi + Evaluator", "change.execute / change.review", "Sahipler kanıt yükler, KG kanıtları doğrular ve devreye alma onayına gönderir.", "CommissioningApproval açılır.", "Doğrulanmamış aksiyon devreye almayı engeller."),
      step("Devreye al", "Approver", "change.approve + kayıt görevi", "Otorite belgesi ve bağımlılıkları kontrol ederek elektronik imza verir.", "PostImplementationVerification başlar.", "Devreye alma nihai kapanış değildir; gerektiğinde kontrollü rollback kullanılabilir."),
      step("Sonucu doğrula ve kapat", "Evaluator → Approver", "change.review / change.approve", "Uygulama sonucunu doğrular; başarılıysa kapanış onayına, başarısızsa rollback'e yönlendirir.", "Başarılı bağımsız onayla kayıt kapanır.", "Devreye alma ve kapanış ayrı imza ve görevlerdir."),
    ],
    roles: [
      { role: "QualityAssurance / DepartmentManager", taskRole: "Initiator", permission: "change.create", responsibility: "Değişikliği başlatır." },
      { role: "QualityAssurance", taskRole: "ProcessAuthority / Evaluator", permission: "change.review", responsibility: "Ön inceleme, paralel etki ve uygulama doğrulamasını yürütür." },
      { role: "RegulatoryAffairs / DepartmentManager", taskRole: "Assessment:{id}", permission: "change.review", responsibility: "Kendi bölüm veya ruhsat etkisini değerlendirir." },
      { role: "Approver", taskRole: "ChangeBoard / Approver", permission: "change.approve", responsibility: "Kurul, plan, devreye alma ve kapanış kararlarını verir." },
      { role: "ActionOwner", taskRole: "Action:{id}", permission: "change.execute", responsibility: "Atanmış uygulama aksiyonunun kanıtını tamamlar." },
    ],
    rules: ["Tüm etki değerlendirmeleri tamamlanmadan kurul kararı verilemez.", "Otorite onayı gereken değişiklik belge referansı olmadan devreye alınamaz.", "Başarısız uygulama kontrollü geri dönüşe yönlendirilir."],
    connections: [
      {
        process: "M.02 · DÖF Yönetimi",
        direction: "M.02 → M.03",
        trigger: "Yeni değişiklik kaydında SourceCapaId seçilirse backend kaynak DÖF'ün varlığını doğrular.",
        communication: "M.03 SourceCapaId saklar; M.02 kayıt numarası liste ve detay sorgularında join ile okunur.",
        data: "Kaynak DÖF kimliği ve kayıt numarası. Değişikliğin mevcut/önerilen durumu ve uygulama planı otomatik kopyalanmaz.",
        dependency: "M.03 geçişleri M.02 Status değerini sorgulamaz; bağlantı kodda kaynak kaydı gösteren izlenebilirlik bağıdır.",
      },
      {
        process: "M.04 · Doküman Yönetimi",
        direction: "M.03 → M.04",
        trigger: "Yeni doküman oluşturulurken isteğe bağlı SourceChangeControlId seçilir ve M.03 kaydının varlığı doğrulanır.",
        communication: "M.04 SourceChangeControlId saklar ve M.03 kayıt numarasını listede/detayda join ile gösterir.",
        data: "Kaynak değişiklik kimliği ve numarası. Doküman kodu, içerik, incelemeler ve eğitim gereksinimleri M.04 formunda ayrıca tanımlanır.",
        dependency: "M.03 servisi bağlı M.04 dokümanlarını veya durumlarını sorgulamaz; M.03 kapanışı doküman durumuyla teknik olarak bloke edilmez.",
      },
      {
        process: "M.05 · Eğitim Yönetimi",
        direction: "M.03 → M.04 → M.05 (dolaylı)",
        trigger: "M.03 kaynaklı M.04 sürümü onaylandığında ve eğitim gereksinimi varsa M.05 görevleri oluşturulur.",
        communication: "M.05 doğrudan SourceChangeControlId tutmaz; zincir ControlledDocument.SourceChangeControlId ve TrainingAssignment.ControlledDocumentId üzerinden kurulur.",
        data: "M.03 kimliği M.04'te; doküman, sürüm, gereksinim ve çalışan bilgileri M.05'te tutulur.",
        dependency: "M.03 kapanışı M.05'i sorgulamaz. Eğitim kapısı M.04 release işlemini bloke eder.",
      },
    ],
  },
  "M.04": {
    name: "Doküman Yönetimi",
    purpose: "Kontrollü dokümanları yazım, paralel inceleme, onay, eğitim kapısı, yürürlük, revizyon ve arşiv boyunca sürüm bazında yönetir.",
    entry: "Yeni doküman için document.create gerekir. Yürürlükteki doküman doğrudan düzenlenmez; DocumentCoordinator “Revizyon talep et” işlemini başlatır.",
    steps: [
      step("Dokümanı oluştur ve yaz", "Doküman Kontrol / Yazar · DocumentAuthor", "document.create + document.write", "Kod, tür, sahip, gizlilik, gözden geçirme dönemi ve ilk sürüm içeriğini tanımlar; yazımı başlatıp tamamlar.", "Review aşaması açılır.", "Kod benzersizdir; incelemeye gönderilen sürümün içeriği kilitlenir."),
      step("Paralel incelemeleri tamamla", "Bölüm İnceleyicileri · DocumentReview:{id}", "document.review + kayıt görevi", "Atanmış bölümler aynı sürüme görüş verir; koordinatör onaya gönderir.", "Approval ve DocumentApprover görevi açılır.", "Bekleyen veya revizyon isteyen inceleme varken geçiş olmaz."),
      step("Sürümü onayla", "Doküman Onaylayanı · DocumentApprover", "document.approve + kayıt görevi", "İnceleme sonuçlarını kontrol edip elektronik imza verir.", "Eğitim gerekiyorsa TrainingWaiting ve M.05 görevleri; gerekmiyorsa Approved durumu oluşur.", "Hazırlayan kendi sürümünü onaylayamaz."),
      step("Eğitim kapısını kapat", "Eğitim Koordinatörü + Çalışanlar", "training.manage / training.complete", "Etkilenen pozisyonlara atanan eğitim ve sınav görevlerini tamamlar.", "Tüm zorunlu görevler tamamlanınca doküman yürürlüğe alınabilir.", "Eksik veya başarısız zorunlu eğitim release işlemini engeller."),
      step("Yürürlüğe al ve dağıt", "DocumentCoordinator", "document.distribute + kayıt görevi", "Onaylı sürümü planlanan tarihte imzayla yürürlüğe alır; okuma ve kontrollü kopya dağıtımlarını izler.", "Önceki sürüm geçersizleşir; yeni sürüm Effective olur.", "Her okuma ve kontrollü kopya değiştirilemez zaman damgasıyla tutulur."),
      step("Revizyon veya periyodik inceleme başlat", "DocumentCoordinator", "document.distribute + kayıt görevi", "Effective kayıtta revizyon talebi, periyodik gözden geçirme veya yürürlükten kaldırma işlemini seçer.", "Revizyonda yeni sürüm/yazar görevi; gözden geçirmede karar görevi oluşur.", "Yürürlükteki içerik yerinde değiştirilemez."),
      step("Yürürlükten kaldır ve arşivle", "DocumentCoordinator", "document.distribute + kayıt görevi", "Gerekçeli imzayla yürürlükten kaldırır; kontrollü kopyalar iade/imha edilince arşivler.", "Kayıt Archived olur ve geçmiş sürümler korunur.", "Dağıtımda kontrollü kopya varken arşivlenemez."),
    ],
    roles: [
      { role: "DocumentController", taskRole: "DocumentAuthor / DocumentCoordinator", permission: "document.create, write, distribute", responsibility: "Yaşam döngüsünü, revizyonu, yürürlüğü ve dağıtımı yönetir." },
      { role: "DepartmentManager / QualityAssurance / RegulatoryAffairs", taskRole: "DocumentReview:{id}", permission: "document.review", responsibility: "Kendi bölüm etkisi için sürümü inceler." },
      { role: "Approver", taskRole: "DocumentApprover", permission: "document.approve", responsibility: "Sürümü bağımsız elektronik imzayla onaylar." },
      { role: "TrainingCoordinator / Learner", taskRole: "TrainingCoordinator / Learner", permission: "training.manage / complete", responsibility: "Yürürlük öncesi zorunlu eğitim kapısını tamamlar." },
    ],
    rules: ["Sistem rolüne ek olarak etkin kayıt görevi zorunludur.", "Onaylı/yürürlükteki sürüm değiştirilmez; yeni revizyon açılır.", "Onay, yürürlük, periyodik inceleme, geri çekme ve arşiv kararları elektronik imzalıdır."],
    connections: [
      {
        process: "M.03 · Değişiklik Kontrol",
        direction: "M.03 → M.04",
        trigger: "M.04 oluşturma isteğinde SourceChangeControlId verilirse kaynak M.03 kaydının varlığı doğrulanır.",
        communication: "ControlledDocument.SourceChangeControlId kalıcı bağı tutar; kaynak kayıt numarası M.04 liste ve detay sorgularında join ile okunur.",
        data: "M.03 kimliği ve kayıt numarası. Doküman içeriği veya aksiyonları otomatik kopyalanmaz.",
        dependency: "M.04 iş akışı M.03 durumunu sorgulamaz; kaynak bağlantısı izlenebilirlik içindir.",
      },
      {
        process: "M.05 · Eğitim Yönetimi",
        direction: "M.04 → M.05 → M.04",
        trigger: "M.04 approve geçişi TrainingWaiting sonucunu üretirse güncel sürümün her bekleyen pozisyon gereksinimi için etkin çalışanlara M.05 ataması oluşturulur.",
        communication: "TrainingAssignment; ControlledDocumentId, DocumentRevisionId ve DocumentTrainingRequirementId tutar. Son bağlı çalışan görevi Completed olduğunda M.05 SyncDocumentRequirement metodu M.04 gereksinimini tamamlar ve Document audit event'i yazar.",
        data: "Doküman kimliği/kodu, sürüm, gereksinim, pozisyon, çalışan, hedef tarih, ReadAndAcknowledge yöntemi, geçme puanı 100 ve deneme sınırı.",
        dependency: "M.04 release, güncel sürümde Pending eğitim gereksinimi varsa reddedilir. M.05 bağlantısı olan gereksinim M.04 ekranından elle kapatılamaz.",
      },
      {
        process: "M.08 · Dış Denetimler",
        direction: "M.04 → M.08",
        trigger: "M.08 oluşturulurken seçilen kontrollü dokümanlar denetim talep paketine alınır.",
        communication: "ExternalAuditDocumentRequest ControlledDocumentId ile M.04'e bağlanır; doküman kodu, başlık ve gizlilik bilgisi talep kaydında tutulur. Export işleminde alıcı, amaç, kanıt ve export sürümü erişim kaydına yazılır.",
        data: "Doküman kimliği, kodu, başlığı, gizlilik sınıfı, dışa aktarım sürümü, alıcı, amaç, kanıt ve erişim zamanı.",
        dependency: "M.08 start-audit geçişi tüm doküman talepleri Exported olmadan açılmaz. M.04 yaşam döngüsü M.08 durumunu sorgulamaz.",
      },
    ],
  },
  "M.05": {
    name: "Eğitim Yönetimi",
    purpose: "Pozisyon veya doküman sürümünden doğan eğitim ihtiyacını atama, katılım, değerlendirme, eğitmen onayı ve yenilemeyle yönetir.",
    entry: "TrainingCoordinator veya QualityAssurance yeni eğitim görevi planlar; M.04 onayı da etkilenen pozisyonlar için görevleri otomatik oluşturabilir.",
    steps: [
      step("Eğitimi planla ve ata", "Eğitim Koordinatörü", "training.manage", "Çalışan, yöntem, hedef tarih, geçme puanı, deneme sayısı ve gerekiyorsa doküman sürümünü belirler.", "Learner görevi Assigned durumunda çalışana atanır.", "Yalnız etkin çalışan ve geçerli matris/doküman ilişkisi kullanılabilir."),
      step("Eğitimi tamamla", "Katılımcı · Learner", "training.complete + kayıt görevi", "İçeriği okur, katılım beyanını verir ve değerlendirmeye gönderir.", "Sınav sonucuna göre TrainerApproval veya Failed oluşur.", "Yalnız atanmış çalışan kendi görevini tamamlayabilir; denemeler silinmez."),
      step("Yeterliliği onayla", "Eğitmen · Trainer", "training.approve + kayıt görevi", "Sınav/pratik kanıtı kontrol ederek elektronik yeterlilik onayı verir.", "Completed olur ve geçerlilik tarihi hesaplanır.", "Katılımcı kendi yeterliliğini onaylayamaz."),
      step("Yenile veya süresini yönet", "Eğitim Koordinatörü", "training.manage + kayıt görevi", "Başarısız/süresi dolmuş görevi yeniden atar; tamamlanan eğitimin süresini doldu olarak işaretler.", "Yeni görev ve yeni zaman damgası oluşur; eski kanıt korunur.", "M.04'e bağlı zorunlu görev tamamlanmadan doküman release olamaz."),
    ],
    roles: [
      { role: "TrainingCoordinator / QualityAssurance", taskRole: "TrainingCoordinator", permission: "training.manage", responsibility: "Planlama, atama, yenileme ve iptali yönetir." },
      { role: "Learner", taskRole: "Learner", permission: "training.complete", responsibility: "Kendi eğitim ve değerlendirme görevini tamamlar." },
      { role: "Trainer / DepartmentManager", taskRole: "Trainer", permission: "training.approve", responsibility: "Yeterliliği bağımsız olarak onaylar." },
    ],
    rules: ["Katılımcı kendi yeterliliğini onaylayamaz.", "Başarısız denemeler audit trail içinde korunur.", "Doküman kaynaklı zorunlu eğitim yürürlük için bloklayıcıdır."],
    connections: [
      {
        process: "M.04 · Doküman Yönetimi",
        direction: "M.04 → M.05 → M.04",
        trigger: "Doküman onayı TrainingWaiting oluşturduğunda M.04, etkilenen pozisyondaki her etkin çalışan için M.05 kaydı ve Learner/TrainingCoordinator görevleri üretir.",
        communication: "M.05 kaydı doküman, sürüm ve eğitim gereksinimi kimliklerini saklar. Tüm aynı gereksinim atamaları Completed olduğunda M.05, M.04 DocumentTrainingRequirement kaydını tamamlar.",
        data: "Doküman kodu/başlığı/sürümü, pozisyon, çalışan, gereksinim, hedef tarih, yöntem, geçme puanı ve yeterlilik kanıtı.",
        dependency: "Bağlı gereksinim tamamlanmadan M.04 yürürlüğe alma işlemi reddedilir. M.04, M.05 bağlantılı gereksinimi manuel tamamlama isteğini de reddeder.",
      },
      {
        process: "Eğitim Matrisi ↔ M.04",
        direction: "M.04 → Eğitim Matrisi → M.05",
        trigger: "TrainingMatrixRule oluşturulurken isteğe bağlı ControlledDocumentId seçilebilir; backend dokümanın varlığını doğrular.",
        communication: "Matris kuralı ControlledDocumentId tutar; matris sorgusu M.04 DocumentCode değerini join ile döndürür.",
        data: "Doküman kimliği/kodu, pozisyon, eğitim kodu/başlığı, değerlendirme yöntemi, geçme puanı, geçerlilik ve kritik yeterlilik bilgisi.",
        dependency: "Matris bağlantısı tek başına M.04 gereksinimini kapatmaz; yürürlük kapısı yalnız DocumentTrainingRequirement bağlantılı tamamlanmış atamalarla açılır.",
      },
    ],
  },
  "M.06": {
    name: "Müşteri Şikâyetleri",
    purpose: "Müşteri bildirimini triyaj, ön yanıt, paralel araştırma, etki/kök neden, DÖF/FV bağlantıları ve nihai yanıtla yönetir.",
    entry: "complaint.create izni olan kullanıcı müşteri, ürün, batch, olay ve SLA bilgileriyle kaydı açar; bir ComplaintCoordinator atanır.",
    steps: [
      step("Kaydı al ve triyaj yap", "Şikâyet Koordinatörü", "complaint.create / complaint.manage", "Bildirimi kaydeder; önem, sağlık etkisi, kalite ve tekrar sinyalini değerlendirir.", "Gerekirse M.01/FV bağlantıları açılır ve PreliminaryResponse başlar.", "Majör/kritik kalite şikâyeti M.01'i zorunlu tetikler."),
      step("Ön yanıtı hazırla ve onaylat", "Coordinator → ResponseApprover", "complaint.manage / complaint.approve", "Ön yanıt sürümünü hazırlar; farklı onaylayan elektronik imza verir.", "Paralel araştırmalar başlatılabilir.", "Yanıtı hazırlayan kendi sürümünü onaylayamaz."),
      step("Paralel araştırmaları tamamla", "ComplaintInvestigator", "complaint.investigate + bölüm görevi", "Her bölüm bulgu ve kök neden katkısını kaydeder.", "Tüm görevler tamamlanınca etki değerlendirmesi açılır.", "Atanmamış kullanıcı başka bölümün araştırmasını tamamlayamaz."),
      step("Etki, DÖF ve FV kararını ver", "ComplaintCoordinator / KG", "complaint.manage", "Ortak kök nedeni ve etkiyi doğrular; M.02 ve gerekiyorsa farmakovijilans bağlantısını oluşturur.", "Nihai yanıt hazırlığına geçilir.", "DÖF gerekli kararda M.02 bağlantısı olmadan ilerlenemez."),
      step("Nihai yanıtı onayla ve kapat", "Coordinator → ResponseApprover", "complaint.manage / complaint.approve", "Nihai yanıt sürümünü hazırlar, iletim ve bağlantıları kontrol ederek ayrı kullanıcıyla kapatır.", "Kayıt Closed olur; tüm sürümler korunur.", "Onaylı nihai yanıt ve gerekli FV aktarımı olmadan kapanmaz."),
    ],
    roles: [
      { role: "QualityAssurance / DepartmentManager", taskRole: "ComplaintCoordinator", permission: "complaint.manage", responsibility: "Triyaj, bağlantılar, ortak karar ve yanıt hazırlığını yönetir." },
      { role: "Investigator / DepartmentManager", taskRole: "ComplaintInvestigator", permission: "complaint.investigate", responsibility: "Atandığı bölüm araştırmasını tamamlar." },
      { role: "Approver / QualifiedPerson", taskRole: "ResponseApprover", permission: "complaint.approve", responsibility: "Müşteri yanıtlarını ve kapanışı bağımsız onaylar." },
      { role: "PharmacovigilanceReviewer", taskRole: "PharmacovigilanceReviewer", permission: "specialized.manage", responsibility: "Advers olay şüphesini gizlilik sınırları içinde değerlendirir." },
    ],
    rules: ["Ön ve nihai yanıtlar ayrı, sürümlü ve onaylı kayıtlardır.", "Tüm bölüm araştırmaları bitmeden ortak kök neden kararı verilemez.", "Oluşturan kullanıcı nihai kapanışı onaylayamaz."],
    connections: [
      {
        process: "M.01 · Sapma Yönetimi",
        direction: "M.06 → M.01",
        trigger: "complete-triage sırasında ComplaintType=ProductQuality ve Severity Minor dışında olduğunda M.01 otomatik oluşturulur.",
        communication: "Complaint.LinkedDeviationId oluşan sapmayı tutar; M.01 audit event'i complaintId, product ve batchNumber içerir.",
        data: "Ürün, batch, açıklama, olay/alınma zamanı; severity şiddete, trend sinyali olasılığa çevrilir. Müşteri adı M.01'e aktarılmaz.",
        dependency: "İki kayıt aynı transaction'da oluşur. M.06 kapanış metodu M.01 durumunu sorgulamaz.",
      },
      {
        process: "M.15 · Farmakovijilans",
        direction: "M.06 → M.15",
        trigger: "complete-triage sırasında SuspectedAdverseEvent=true ise FV kalite kaydı otomatik oluşturulur.",
        communication: "Complaint.PharmacovigilanceRecordId ve PharmacovigilanceStatus=Transferred saklanır. FV kayıt metadata'sında sourceComplaintId ve privacyBoundary bulunur.",
        data: "Şikâyet kimliği, ürün, batch ve severity aktarılır. privacyBoundary=NoCustomerOrHealthNarrativeTransferred nedeniyle müşteri veya sağlık anlatısı aktarılmaz.",
        dependency: "Advers olay şüphesi varsa PharmacovigilanceStatus Transferred olmadan M.06 kapatılamaz.",
      },
      {
        process: "M.02 · DÖF Yönetimi",
        direction: "M.06 → M.02",
        trigger: "CapaDecision aşamasında CapaRequired=true seçildiğinde M.02 otomatik oluşturulur.",
        communication: "Complaint.LinkedCapaId M.02 kimliğini saklar; M.02 audit event'i complaintId ve complaintNumber içerir.",
        data: "Ürün tabanlı başlık, şikâyet açıklaması, doğrulanmış kök neden, müşteri etkisi acil aksiyonu, sahip, hedef ve isteğe bağlı trend etkinlik planı.",
        dependency: "CapaRequired=true iken M.02 oluşturulmadan FinalResponseApproval'a geçilemez. Mevcut Close kodu M.02 Status değerini kontrol etmez.",
      },
    ],
  },
  "M.07": {
    name: "İç Denetimler",
    purpose: "İç denetimi bağımsız plan, kilitli soru listesi, kanıt, risk sınıflı bulgu, bölüm yanıtı ve DÖF bağımlı kapanışla yürütür.",
    entry: "QualityAssurance internal-audit.plan izniyle yıllık veya gerekçeli plansız denetim açar; baş denetçi ve bölüm yanıtlayanı atanır.",
    steps: [
      step("Planla ve hazırla", "AuditPlanner", "internal-audit.plan", "Kapsam, kriter, denetlenen bölüm, baş denetçi, takvim ve soru listesini tanımlar.", "Preparation ve ardından PlanApproval oluşur.", "Baş denetçi kendi bölümünü denetleyemez; plansız kayıtta gerekçe zorunludur."),
      step("Planı ve soruları kilitle", "Approver", "internal-audit.approve + kayıt görevi", "Bağımsızlık ve kapsamı onaylar.", "Checklist sürümü kilitlenir ve Execution açılır.", "Planlayıcı ve baş denetçiden farklı onaylayan atanır."),
      step("Denetimi uygula", "LeadAuditor", "internal-audit.execute + kayıt görevi", "Her soruya sonuç, objektif kanıt ve not girer; bulguları risk puanıyla oluşturur.", "Findings ve ResponseAction aşamaları açılır.", "Tüm sorular yanıtlanmadan uygulama tamamlanmaz; majör/kritik bulgu M.02 açar."),
      step("Bölüm yanıtlarını tamamla", "AuditeeResponder", "internal-audit.respond + kayıt görevi", "Her bulguya neden, düzeltici aksiyon ve hedef tarih girer.", "CapaVerification başlar.", "Her açık bulgu yanıtlanmadan geçiş olmaz."),
      step("Bulguları doğrula ve denetimi kapat", "Approver / KG", "internal-audit.approve + kayıt görevi", "Kanıtı ve bağlı DÖF durumunu doğrular; bulguları tek tek, sonra denetimi kapatır.", "Tüm bulgular kapalıysa audit Closed olur.", "Bağlı M.02 kapanmadan bulgu; tek açık bulgu varken denetim kapanmaz."),
    ],
    roles: [
      { role: "QualityAssurance", taskRole: "AuditPlanner", permission: "internal-audit.plan", responsibility: "Denetim planını ve kapsamı oluşturur." },
      { role: "Investigator", taskRole: "LeadAuditor", permission: "internal-audit.execute", responsibility: "Soru listesini, kanıtları ve bulguları yürütür." },
      { role: "DepartmentManager / ActionOwner", taskRole: "AuditeeResponder", permission: "internal-audit.respond", responsibility: "Denetlenen bölümün bulgu yanıtlarını verir." },
      { role: "Approver / QualityAssurance", taskRole: "Approver", permission: "internal-audit.approve", responsibility: "Plan, bulgu ve nihai kapanışı doğrular." },
    ],
    rules: ["Baş denetçi denetlenen bölümden farklı olmalıdır.", "Soru listesi uygulama öncesi sürümüyle kilitlenir.", "Majör/kritik bulgu M.02 kapanmadan kapatılamaz."],
    connections: [
      {
        process: "M.02 · DÖF Yönetimi",
        direction: "M.07 bulgusu → M.02 → M.07 bulgusu",
        trigger: "Bulgu eklenirken impact × likelihood skoru 12 ve üzerindeyse veya CapaRequired=true gönderildiyse M.02 aynı transaction'da oluşturulur.",
        communication: "AuditFinding.LinkedCapaId M.02 kimliğini tutar. M.02 SourceType=InternalAudit olur; M.02 audit event'i auditId, M.07 event'i capaId taşır.",
        data: "Bulgu başlığı/açıklaması/referansı, bulgu sahibi, hedef tarih; kök neden metni “İç denetim bulgusu: {referans}”, etkinlik yöntemi iç denetim tekrar doğrulamasıdır.",
        dependency: "M.07 bulgu kapanışında LinkedCapaId ile M.02 Status=Closed aranır. DÖF açıkken bulgu, açık bulgu varken denetim kapanamaz.",
      },
    ],
  },
  "M.08": {
    name: "Dış Denetimler",
    purpose: "Otorite veya müşteri denetimini kontrollü doküman paketi, resmi bulgular, taahhütler, DÖF ve yetkili kapanışıyla yönetir.",
    entry: "QualityAssurance veya DepartmentManager external-audit.create izniyle resmi referans, kurum, kapsam ve takvim bilgilerini kaydeder.",
    steps: [
      step("Bildirimi kaydet ve hazırlığı başlat", "ExternalAuditCoordinator", "external-audit.create", "Kurum, resmi referans, ülke, kapsam, saha ve SLA bilgilerini girer.", "Preparation açılır.", "Devlet kurumu denetiminde yetkili kapanış kullanıcısı zorunludur."),
      step("Kontrollü talep paketi hazırla", "DocumentPackageController", "external-audit.prepare", "Güncel M.04 sürümlerini manifest, amaç, alıcı ve erişim kanıtıyla pakete alır.", "Eksiksiz paketle AuditInProgress başlar.", "Bekleyen doküman varken denetim başlatılamaz; doğrudan kontrolsüz paylaşım yapılamaz."),
      step("Bulguları ve cevap planını kaydet", "ExternalAuditCoordinator / Responder", "external-audit.prepare / respond", "Resmi bulguları sınıflar; cevap, taahhüt, sorumlu ve hedef tarihi kaydeder.", "CapaAction aşaması açılır.", "Majör/kritik bulgu M.02 oluşturur; tüm bulgular cevaplanmalıdır."),
      step("DÖF ve kanıtları doğrula", "Approver / KG", "external-audit.approve", "Taahhüt kanıtlarını ve bağlı M.02 durumunu doğrular.", "Tüm bulgular kapanınca ClosureLetter açılır.", "Açık DÖF varken resmi bulgu kapatılamaz."),
      step("Yetkili kapanışı ver", "QualifiedPerson / AuthorizedCloser", "external-audit.approve + kayıt görevi", "Kapanış mektubu ve kabul kanıtını imzalar.", "Kayıt Closed olur.", "Devlet kurumu denetimini yalnız Mesul Müdür/atanmış yetkili kapatır; oluşturan kapatamaz."),
    ],
    roles: [
      { role: "QualityAssurance / DepartmentManager", taskRole: "ExternalAuditCoordinator", permission: "external-audit.create", responsibility: "Denetim kaydını ve resmi akışı koordine eder." },
      { role: "DocumentController", taskRole: "DocumentPackageController", permission: "external-audit.prepare", responsibility: "Kontrollü talep paketini oluşturur ve aktarır." },
      { role: "DepartmentManager / ActionOwner", taskRole: "ExternalAuditeeResponder", permission: "external-audit.respond", responsibility: "Resmi bulgu cevap ve taahhütlerini hazırlar." },
      { role: "Approver / QualifiedPerson", taskRole: "ExternalAuditAuthorizedCloser", permission: "external-audit.approve", responsibility: "Kanıtları ve yetkili kapanışı doğrular." },
    ],
    rules: ["Dokümanlar kontrollü dışa aktarım olmadan paylaşılmaz.", "Majör/kritik bulgu bağlı M.02 kapanmadan kapanmaz.", "Devlet kurumu kapanışı özel yetki ve elektronik imza gerektirir."],
    connections: [
      {
        process: "M.04 · Doküman Yönetimi",
        direction: "M.04 → M.08 talep paketi",
        trigger: "M.08 oluşturulurken DocumentRequest girdilerindeki ControlledDocumentId değerleriyle güncel kontrollü dokümanlar seçilir.",
        communication: "Talep satırı M.04 kimliğini, doküman kodunu, başlığı ve gizlilik sınıfını saklar. ExportDocument işlemi alıcı, amaç, kanıt, export sürümü ve erişim zamanını ayrı access kaydına yazar.",
        data: "ControlledDocumentId, kod, başlık, gizlilik, exportVersion, recipient, purpose, evidence ve accessedAtUtc.",
        dependency: "Tüm doküman talepleri Exported olmadan M.08 start-audit geçişi reddedilir.",
      },
      {
        process: "M.02 · DÖF Yönetimi",
        direction: "M.08 bulgusu → M.02 → M.08 bulgusu",
        trigger: "Bulgu Major/Critical ise veya CapaRequired=true seçilirse M.02 aynı transaction'da otomatik oluşturulur.",
        communication: "ExternalAuditFinding.LinkedCapaId tutulur. M.02 SourceType=ExternalAudit ve audit event payload'ında externalAuditId bulunur.",
        data: "Bulgu başlığı/açıklaması/resmi referansı, sorumlu, cevap hedefi; etkinlik yöntemi taahhüt ve otorite kabul kanıtıdır.",
        dependency: "M.08 bulgu kapanırken M.02 Status=Closed kontrol edilir. Açık DÖF'lü bulgu kapanamaz; tüm bulgular kapanmadan kapanış mektubu aşaması açılamaz.",
      },
    ],
  },
  "M.09": {
    name: "Tedarikçi Denetimi",
    purpose: "Tedarikçiyi risk planından soru listesi, saha kanıtı, güvenli cevap, DÖF ve kapsam bazlı nitelendirme kararına taşır.",
    entry: "QualityAssurance veya DepartmentManager supplier-audit.plan izniyle tedarikçi, kapsam, kritiklik ve geçmiş performans verileriyle risk planı oluşturur.",
    steps: [
      step("Risk planını ve kapsamı oluştur", "SupplierAuditPlanner", "supplier-audit.plan", "Kritiklik, geçmiş performans ve açık bulgulardan risk/frekans hesaplar; kapsam ve soru listesini sabitler.", "AuditorAssignment açılır.", "Kritiklik, performans ve en az bir soru listesi maddesi zorunludur."),
      step("Denetçiyi ata ve uygula", "SupplierAuditLeadAuditor", "supplier-audit.execute", "Baş denetçi atanır; her soruya sonuç, kanıt ve not girer.", "Findings aşaması açılır.", "Soru listesi başlangıçta sürümüyle kilitlenir."),
      step("Bulguları sınıflandır", "SupplierAuditLeadAuditor", "supplier-audit.execute", "Bulguları kritik, majör, minör veya gözlem olarak kaydeder.", "SupplierResponse açılır; gerekli M.02 bağlantıları kurulur.", "Kritik bulgu kapsamı askıya alır; majör/kritik bulgu M.02 açar."),
      step("Tedarikçi cevabı ve kanıtı al", "SupplierResponder", "supplier-audit.respond + kayıt görevi", "İç kullanıcı veya süreli tek kullanımlık güvenli davetle cevap, taahhüt ve kanıt toplar.", "EvidenceVerification açılır.", "Token yalnız bir kez gösterilir; yalnız özeti saklanır."),
      step("Kanıt ve DÖF'ü doğrula", "SupplierAuditVerifier", "supplier-audit.approve", "Kanıt paketini ve bağlı M.02 kapanışlarını doğrular.", "AuditResult aşaması açılır.", "Açık DÖF veya kanıtsız bulgu ilerlemeyi engeller."),
      step("Nitelendirme kararı ver ve kapat", "SupplierQualityApprover", "supplier-audit.approve + kayıt görevi", "Onaylı, koşullu, askıda, reddedildi veya yeniden nitelendirme kararını gerekçeyle verir.", "Kayıt Closed olur ve karar M.16'ya girdi sağlar.", "Kritik bulgulu tedarikçi doğrudan onaylanamaz; tüm bulgular kapanmış olmalıdır."),
    ],
    roles: [
      { role: "QualityAssurance / DepartmentManager", taskRole: "SupplierAuditPlanner", permission: "supplier-audit.plan", responsibility: "Risk planı, kapsam ve denetçi atamasını yönetir." },
      { role: "Investigator / Approver", taskRole: "SupplierAuditLeadAuditor", permission: "supplier-audit.execute", responsibility: "Saha uygulamasını ve bulguları yürütür." },
      { role: "DepartmentManager / ActionOwner", taskRole: "SupplierResponder", permission: "supplier-audit.respond", responsibility: "Tedarikçi cevap ve kanıtlarını toplar." },
      { role: "QualityAssurance / Approver", taskRole: "SupplierAuditVerifier / SupplierQualityApprover", permission: "supplier-audit.approve", responsibility: "Kanıt, DÖF ve nitelendirme sonucunu doğrular." },
    ],
    rules: ["Kritik bulgu kapsamı askıya alır ve yeniden nitelendirme gerektirir.", "Majör/kritik bulgu M.02 kapanmadan denetim kapanmaz.", "Tedarikçi erişimi ikinci tenant yerine süreli tek kullanımlık davetle verilir."],
    connections: [
      {
        process: "M.16 · Tedarikçi Değerlendirme",
        direction: "M.16 → M.09",
        trigger: "M.09 oluşturma isteğinde isteğe bağlı SupplierEvaluationId alınır.",
        communication: "SupplierAudit.SupplierEvaluationId alanı değeri saklar ve detay cevabında döndürür.",
        data: "Yalnız M.16 kayıt kimliği doğrudan taşınır; tedarikçi kodu/adı, performans ve açık bulgu sayısı M.09 isteğinde ayrıca gönderilir.",
        dependency: "CreateAsync mevcut kodunda SupplierEvaluationId için varlık veya durum doğrulaması yapmaz; bağlantı teknik olarak izlenebilirlik alanıdır.",
      },
      {
        process: "M.02 · DÖF Yönetimi",
        direction: "M.09 bulgusu → M.02 → M.09 bulgusu",
        trigger: "Bulgu Major/Critical ise veya CapaRequired=true seçilirse M.02 aynı transaction'da otomatik oluşturulur.",
        communication: "SupplierAuditFinding.LinkedCapaId tutulur. M.02 SourceType=SupplierAudit ve audit event payload'ında supplierAuditId bulunur.",
        data: "Bulgu başlığı/açıklaması/referansı, sahip ve cevap hedefi; etkinlik yöntemi tedarikçi kanıtı ve kalite doğrulamasıdır.",
        dependency: "M.09 bulgusu M.02 Closed olmadan kapatılamaz. Tüm bulgular ve bağlı DÖF'ler kapanmadan denetim sonucu oluşturulamaz.",
      },
      {
        process: "Güvenli Tedarikçi Cevap Ekranı",
        direction: "M.09 → tek kullanımlık bağlantı → M.09",
        trigger: "SupplierResponder, SupplierResponse aşamasında alıcı e-posta ve son kullanma tarihiyle davet oluşturur.",
        communication: "Sistem rastgele token üretir, yalnız SHA-256 özetini saklar ve /supplier-response?token=... yolunu bir kez döndürür. Gönderim tokenı kullanılmış işaretler ve cevap, taahhüt, hedef tarih ile kanıtı bulguya yazar.",
        data: "FindingId, supplierResponse, commitment, commitmentDueAtUtc ve evidence. Audit aktörü “Tedarikçi güvenli yanıt ekranı” olarak kaydedilir.",
        dependency: "Token süresi dolmuş veya kullanılmışsa tekrar kullanılamaz. Cevap ve kanıt olmadan M.09 doğrulama/CAPA aşamalarına geçemez.",
      },
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
      startIcon={<MenuBookRounded />}
      onClick={onClick}
    >
      {module} kullanım kılavuzu
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

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      className="module-guide-dialog"
      slotProps={{ paper: { className: "module-guide-manual-paper" } }}
    >
      <ModalHeader onClose={onClose}>
        <Stack direction="row" className="module-guide-header">
          <Stack direction="row" spacing={1.4} sx={{ alignItems: "center" }}>
            <Box className="module-guide-header-icon">
              <MenuBookRounded />
            </Box>
            <Box>
              <Typography variant="overline">{module} · KULLANIM KILAVUZU</Typography>
              <Typography variant="h5">{guide.name}</Typography>
            </Box>
          </Stack>
          <Chip icon={<VerifiedRounded />} label="Kod tabanlı iş akışı" />
        </Stack>
      </ModalHeader>

      <DialogContent className="module-guide-content module-guide-manual-content">
        <Paper variant="outlined" className="module-guide-summary module-guide-manual-summary">
          <InfoOutlined />
          <Box>
            <Typography variant="overline">MODÜLÜN AMACI</Typography>
            <Typography className="module-guide-purpose">{guide.purpose}</Typography>
            <Typography color="text.secondary">{guide.entry}</Typography>
          </Box>
          <Stack direction="row" spacing={1} className="module-guide-summary-stats">
            <Chip label={`${guide.steps.length} aşama`} />
            <Chip label={`${guide.roles.length} görev grubu`} />
          </Stack>
        </Paper>

        <Box className="module-guide-manual-layout">
          <Paper component="nav" variant="outlined" className="module-guide-manual-nav" aria-label={`${module} kılavuz içeriği`}>
            <Typography variant="overline">KILAVUZ İÇERİĞİ</Typography>
            <GuideNavItem icon={<AccountTreeRounded />} label="İşlem sırası" count={guide.steps.length} href={`#${module}-flow`} />
            <GuideNavItem icon={<GroupsRounded />} label="Rol ve görevler" count={guide.roles.length} href={`#${module}-roles`} />
            <GuideNavItem icon={<LinkRounded />} label="Süreç bağlantıları" count={guide.connections.length} href={`#${module}-connections`} />
            <GuideNavItem icon={<RuleRounded />} label="Kontrol kuralları" count={guide.rules.length} href={`#${module}-rules`} />
            <Box className="module-guide-access-note">
              <SecurityRounded />
              <Typography variant="body2">
                Bir işlemin görünmesi için sistem izni ve aktif kayıt görevi birlikte gerekir.
              </Typography>
            </Box>
          </Paper>

          <Box className="module-guide-manual-main">
            <GuideSectionHeader
              id={`${module}-flow`}
              eyebrow="İŞLEM SIRASI"
              title="Modül aşama aşama nasıl kullanılır?"
              description="Adımlar gerçek backend durum geçişleri ve kayıt bazlı görev kontrolleri sırasındadır."
              icon={<AccountTreeRounded />}
            />

            <Box className="module-guide-step-list">
              {guide.steps.map((item, index) => (
                <Paper key={item.title} variant="outlined" className="module-guide-step-card">
                  <Box className="module-guide-step-marker">
                    <span>{index + 1}</span>
                    {index < guide.steps.length - 1 && <i />}
                  </Box>
                  <Box className="module-guide-step-content">
                    <Stack direction={{ xs: "column", sm: "row" }} className="module-guide-step-heading">
                      <Box>
                        <Typography variant="overline">{index + 1}. AŞAMA</Typography>
                        <Typography variant="h6">{item.title}</Typography>
                      </Box>
                      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: "wrap" }}>
                        <Chip icon={<GroupsRounded />} label={item.actor} />
                        <Chip variant="outlined" icon={<SecurityRounded />} label={item.permission} />
                      </Stack>
                    </Stack>
                    <Box className="module-guide-step-details">
                      <GuideDetail icon={<AssignmentTurnedInRounded />} label="Kullanıcının işlemi" text={item.action} />
                      <GuideDetail icon={<ArrowForwardRounded />} label="İşlemden sonra" text={item.result} />
                      <GuideDetail icon={<LockRounded />} label="Geçiş koşulu" text={item.control} tone="warning" />
                    </Box>
                  </Box>
                </Paper>
              ))}
            </Box>

            <GuideSectionHeader
              id={`${module}-roles`}
              eyebrow="YETKİ MODELİ"
              title="Hangi rol hangi işlevi yapar?"
              description="Sistem rolü genel izni, görev rolü ise belirli kayıt üzerindeki sorumluluğu ifade eder."
              icon={<GroupsRounded />}
            />
            <Box className="module-guide-role-grid">
              {guide.roles.map((item) => (
                <Paper key={`${item.role}-${item.taskRole}`} variant="outlined" className="module-guide-role-card">
                  <Box className="module-guide-role-icon"><GroupsRounded /></Box>
                  <Box>
                    <Typography variant="overline">SİSTEM ROLÜ</Typography>
                    <Typography variant="h6">{item.role}</Typography>
                    <Typography color="text.secondary">{item.responsibility}</Typography>
                    <Stack direction="row" spacing={1} useFlexGap sx={{ mt: 1.5, flexWrap: "wrap" }}>
                      <Chip size="small" label={`Görev: ${item.taskRole}`} />
                      <Chip size="small" variant="outlined" label={`İzin: ${item.permission}`} />
                    </Stack>
                  </Box>
                </Paper>
              ))}
            </Box>

            <GuideSectionHeader
              id={`${module}-connections`}
              eyebrow="SÜREÇ İLETİŞİMİ"
              title="Bağlandığı süreçlerle veri ve durum iletişimi"
              description="Yalnız kodda uygulanan kimlik bağı, otomatik kayıt üretimi ve durum bağımlılıkları gösterilir."
              icon={<LinkRounded />}
            />
            <Box className="module-guide-connection-list">
              {guide.connections.map((item, index) => (
                <Paper key={`${item.process}-${item.direction}`} variant="outlined" className="module-guide-connection-card">
                  <Stack direction={{ xs: "column", sm: "row" }} className="module-guide-connection-heading">
                    <Stack direction="row" spacing={1.2} sx={{ alignItems: "center" }}>
                      <Box className="module-guide-connection-index">{index + 1}</Box>
                      <Box>
                        <Typography variant="overline">BAĞLI SÜREÇ</Typography>
                        <Typography variant="h6">{item.process}</Typography>
                      </Box>
                    </Stack>
                    <Chip icon={<AccountTreeRounded />} label={item.direction} />
                  </Stack>
                  <Box className="module-guide-connection-details">
                    <ConnectionDetail label="Bağlantıyı ne tetikler?" text={item.trigger} />
                    <ConnectionDetail label="Sistemler nasıl iletişim kurar?" text={item.communication} />
                    <ConnectionDetail label="Hangi veri aktarılır?" text={item.data} />
                    <ConnectionDetail label="Hangi işlem neyi bekler?" text={item.dependency} tone="dependency" />
                  </Box>
                </Paper>
              ))}
            </Box>

            <GuideSectionHeader
              id={`${module}-rules`}
              eyebrow="API KONTROLLERİ"
              title="Modül genelindeki zorunlu kurallar"
              description="Bu kurallar kullanıcı arayüzünden bağımsız olarak backend tarafından uygulanır."
              icon={<RuleRounded />}
            />
            <Paper variant="outlined" className="module-guide-rules module-guide-rule-panel">
              {guide.rules.map((rule, index) => (
                <Stack direction="row" spacing={1.2} key={rule}>
                  <Box className="module-guide-rule-number">{index + 1}</Box>
                  <Typography variant="body2">{rule}</Typography>
                </Stack>
              ))}
            </Paper>
          </Box>
        </Box>
      </DialogContent>
    </Dialog>
  );
}

function GuideNavItem({ icon, label, count, href }: { icon: ReactNode; label: string; count: number; href: string }) {
  return (
    <Box component="a" href={href} className="module-guide-nav-item">
      {icon}
      <span>{label}</span>
      <b>{count}</b>
    </Box>
  );
}

function GuideSectionHeader({ id, eyebrow, title, description, icon }: { id: string; eyebrow: string; title: string; description: string; icon: ReactNode }) {
  return (
    <Stack id={id} direction="row" spacing={1.5} className="module-guide-section-header">
      <Box className="module-guide-section-icon">{icon}</Box>
      <Box>
        <Typography variant="overline">{eyebrow}</Typography>
        <Typography variant="h5">{title}</Typography>
        <Typography color="text.secondary">{description}</Typography>
      </Box>
    </Stack>
  );
}

function GuideDetail({ icon, label, text, tone = "default" }: { icon: ReactNode; label: string; text: string; tone?: "default" | "warning" }) {
  return (
    <Box className={`module-guide-detail is-${tone}`}>
      <Box className="module-guide-detail-icon">{icon}</Box>
      <Box>
        <Typography variant="overline">{label}</Typography>
        <Typography variant="body2">{text}</Typography>
      </Box>
    </Box>
  );
}

function ConnectionDetail({ label, text, tone = "default" }: { label: string; text: string; tone?: "default" | "dependency" }) {
  return (
    <Box className={`module-guide-connection-detail is-${tone}`}>
      <Typography variant="overline">{label}</Typography>
      <Typography variant="body2">{text}</Typography>
    </Box>
  );
}
