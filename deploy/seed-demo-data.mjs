#!/usr/bin/env node
// Demo ortamına modül iş akışlarından geçen örnek kayıtlar ekler. Kayıtlar API üzerinden oluşturulduğu için
// denetim izi, kayıt numaraları ve görev atamaları gerçek kullanımdaki gibi üretilir.
//
// Kullanım: node deploy/seed-demo-data.mjs <api-adresi> [--force] [--skip=deviations,trainings,...]
// Örnek:    node deploy/seed-demo-data.mjs https://qms-api-x6t0.onrender.com
//
// API'de DemoMode:Enabled açık olmalıdır (hızlı giriş profilleri). Veritabanında sapma kaydı varsa
// --force verilmedikçe çalışmaz; yeniden çalıştırmak benzersiz kodlu kayıtlarda hata üretebilir.
// Yarım kalan bir yüklemeyi tamamlarken yüklenmiş bölümler --skip ile atlanır. Bölümler: deviations, capas,
// changes, documents, trainings, complaints, internal-audits, external-audits, supplier-audits, work-items,
// risks, mbr, specialized, forms.

const [baseArgument, ...flags] = process.argv.slice(2);
const base = (baseArgument ?? "").replace(/\/+$/, "");
if (!base) {
  console.error("Kullanım: node deploy/seed-demo-data.mjs <api-adresi> [--force] [--skip=bölüm,...]");
  process.exit(1);
}
const force = flags.includes("--force");
const skipped = new Set(flags.find((flag) => flag.startsWith("--skip="))?.slice(7).split(",") ?? []);
const runs = (section) => !skipped.has(section);
const signaturePassword = process.env.QMS_DEMO_SIGNATURE_PASSWORD ?? "Qms.Dev!2026";

const userId = (suffix) => `01991f70-6f40-7000-8000-${String(suffix).padStart(12, "0")}`;
const users = {
  quality: userId(1),
  approver: userId(10),
  reporter: userId(11),
  investigator: userId(12),
  actionOwner: userId(13),
  manager: userId(16),
  qualifiedPerson: userId(17),
  qualityReviewer: userId(18),
  documentController: userId(20),
  learner: userId(25),
};

const dayMs = 86_400_000;
const at = (days, hourUtc = 7) => {
  const date = new Date(Date.now() + days * dayMs);
  date.setUTCHours(hourUtc, 0, 0, 0);
  return date.toISOString();
};
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

const sessions = new Map();

async function login(profile) {
  for (let attempt = 0; attempt < 3; attempt++) {
    const response = await fetch(`${base}/api/v1/auth/quick-login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ profileKey: profile }),
    });
    if (response.status === 429) {
      console.log("  … giriş hız sınırı, 65 sn bekleniyor");
      await sleep(65_000);
      continue;
    }
    if (!response.ok) throw new Error(`${profile} girişi başarısız (${response.status}). DemoMode açık mı?`);
    const cookie = response.headers.getSetCookie().find((value) => value.includes("qms-session"));
    if (!cookie) throw new Error(`${profile} girişi oturum çerezi döndürmedi`);
    sessions.set(profile, cookie.split(";")[0].split("=").slice(1).join("="));
    return;
  }
  throw new Error(`${profile} girişi hız sınırı nedeniyle yapılamadı`);
}

async function api(profile, method, path, body, retried = false) {
  if (!sessions.has(profile)) await login(profile);
  const response = await fetch(`${base}${path}`, {
    method,
    headers: {
      Authorization: `Bearer ${sessions.get(profile)}`,
      "Content-Type": "application/json",
      "X-QMS-CSRF": "1",
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (response.status === 401 && !retried) {
    // Aynı profille başka bir yerden hızlı giriş yapılınca önceki oturum iptal edilir.
    sessions.delete(profile);
    return api(profile, method, path, body, true);
  }
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!response.ok)
    throw new Error(`${method} ${path} → ${response.status} ${data?.detail ?? data?.title ?? text}`.trim());
  return data;
}

const outcomes = [];
async function step(label, action) {
  try {
    const result = await action();
    outcomes.push({ label, ok: true });
    console.log(`✓ ${label}`);
    return result;
  } catch (error) {
    outcomes.push({ label, ok: false, error: error.message });
    console.log(`✗ ${label}\n    ${error.message}`);
    return null;
  }
}

const recordOf = (response) => response?.record ?? response;
const transition = (profile, path, created, name) =>
  api(profile, "POST", `${path}/${recordOf(created).id}/transitions`,
    { expectedVersion: recordOf(created).version, transition: name });

console.log(`QMS örnek veri yükleme → ${base}\n`);

const existing = await api("quality", "GET", "/api/v1/deviations");
if (Array.isArray(existing) && existing.length > 0 && !force) {
  console.log(`Veritabanında zaten ${existing.length} sapma kaydı var; tekrar yüklemek için --force kullanın.`);
  process.exit(0);
}

const lookups = await api("quality", "GET", "/api/v1/deviations/lookups");
const department = (code) => {
  const match = lookups.departments.find((item) => item.code === code);
  if (!match) throw new Error(`${code} bölümü bulunamadı`);
  return match.id;
};
const trainingOptions = await api("training-coordinator", "GET", "/api/v1/trainings/options");
const employee = (id) => {
  const match = trainingOptions.employees.find((item) => item.id === id);
  if (!match) throw new Error(`${id} çalışanı eğitim seçeneklerinde yok`);
  return match;
};

// M.01 Sapmalar
if (runs("deviations")) {
  const deviations = [
    {
      title: "Granülasyon kurutma sıcaklığı sapması",
      description: "GR-02 akışkan yatak kurutucuda çıkış sıcaklığı 3 dakika boyunca 62 °C'ye yükseldi.",
      expectedState: "Çıkış sıcaklığı 45–55 °C aralığında olmalıdır.",
      immediateAction: "Proses durduruldu, batch PAR5-26-0412 karantinaya alındı.",
      deviationType: "Proses", processStage: "Granülasyon",
      likelihood: 3, severity: 5, detectability: 4,
    },
    {
      title: "Tablet baskı makinesinde zımba aşınması",
      description: "TAB-01 hattında sertlik değerlerinde kademeli düşüş gözlendi; zımba yüzeylerinde aşınma tespit edildi.",
      expectedState: "Tablet sertliği 80–120 N aralığında olmalıdır.",
      immediateAction: "Hat durduruldu, son 30 dakikalık üretim ayrıldı.",
      deviationType: "Ekipman", processStage: "Tablet baskı",
      likelihood: 2, severity: 4, detectability: 3,
    },
    {
      title: "Temizlik kaydında eksik imza",
      description: "Hat temizlik formunda ikinci kontrol imzasının atılmadığı fark edildi.",
      expectedState: "Temizlik formu uygulayıcı ve kontrol eden tarafından imzalanmalıdır.",
      immediateAction: "Sorumlu vardiya amiri bilgilendirildi, temizlik doğrulaması tekrarlandı.",
      deviationType: "Doküman", processStage: "Hat temizliği",
      likelihood: 2, severity: 2, detectability: 2,
    },
  ];
  for (const [index, deviation] of deviations.entries()) {
    const created = await step(`Sapma: ${deviation.title}`, () => api("reporter", "POST", "/api/v1/deviations", {
      ...deviation,
      detectedDepartment: "Üretim",
      occurredAtUtc: at(-(index + 2), 6),
      detectedAtUtc: at(-(index + 2), 7),
    }));
    if (created && index < 2)
      await step("  → iş akışına gönderildi", () => api("reporter", "POST", `/api/v1/deviations/${created.id}/submit`,
        { expectedVersion: created.version }));
  }
}

// M.02 DÖF
if (runs("capas")) {
  const capas = [
    {
      title: "Etiket karışıklığı riskine karşı DÖF",
      description: "Ambalaj hattında iki farklı dozaj etiketinin aynı istasyonda bulunduğu tespit edildi.",
      rootCause: "Hat açılış kontrol listesinde etiket mutabakatı adımı bulunmuyor.",
      immediateActions: "Hat temizliği yapıldı, etiket stoku ayrıldı.",
    },
    {
      title: "Kalibrasyon takip sisteminin iyileştirilmesi",
      description: "İki terazinin kalibrasyon süresinin dolduğu rutin kontrolde fark edildi.",
      rootCause: "Kalibrasyon hatırlatmaları kişisel takvimlerde tutuluyor.",
      immediateActions: "Teraziler kullanımdan çekildi ve kalibre edildi.",
    },
  ];
  for (const [index, capa] of capas.entries()) {
    const created = await step(`DÖF: ${capa.title}`, () => api("quality", "POST", "/api/v1/capas", {
      ...capa,
      sourceDeviationId: null, sourceType: "Manual",
      ownerUserId: users.actionOwner, targetDateUtc: at(30 + index * 15),
      effectivenessRequired: false, effectivenessMethod: "", effectivenessSample: "",
      observationPeriodDays: 0, successCriteria: "", effectivenessEvaluatorUserId: null,
    }));
    if (created && index === 0)
      await step("  → onaya gönderildi", () => transition("quality", "/api/v1/capas", created, "submit"));
  }
}

// M.03 Değişiklik Kontrol
if (runs("changes")) {
  const changes = [
    {
      changeType: "Ekipman", title: "Tablet baskı makinesi zımba setinin değiştirilmesi",
      currentState: "TAB-01 hattında 10 mm yuvarlak zımba seti kullanılıyor.",
      proposedState: "Aşınma nedeniyle yeni tedarikçiden eşdeğer zımba seti devreye alınacak.",
      justification: "Aşınma kaynaklı sertlik sapmalarının önlenmesi.",
      scope: "TAB-01 hattı, Parasetamol 500 mg Tablet",
      riskLevel: "Orta", riskSummary: "Ürün kalitesine etkisi proses validasyonu ile kontrol altına alınacak.",
      productImpact: true, validationRequired: true, regulatoryImpact: "None",
      rollbackPlan: "Eski zımba seti karantinada saklanır, gerekirse yeniden takılır.",
      impactedDepartmentIds: () => [department("URT")],
    },
    {
      changeType: "Bilgisayarlı Sistem", title: "LIMS yazılımının 8.2 sürümüne yükseltilmesi",
      currentState: "Kalite kontrol laboratuvarı LIMS 7.4 sürümünü kullanıyor.",
      proposedState: "Satıcı desteği sona erdiği için LIMS 8.2 sürümüne geçilecek.",
      justification: "Güvenlik yamaları ve satıcı desteğinin sürdürülmesi.",
      scope: "LIMS sunucusu ve KK laboratuvarı iş istasyonları",
      riskLevel: "Yüksek", riskSummary: "Veri bütünlüğü etkisi CSV kapsamında değerlendirilecek.",
      productImpact: false, validationRequired: true, regulatoryImpact: "Notification",
      rollbackPlan: "Yükseltme öncesi tam yedek alınır, başarısızlıkta 7.4 sürümüne dönülür.",
      impactedDepartmentIds: () => [department("BT")],
    },
  ];
  for (const [index, change] of changes.entries()) {
    const created = await step(`Değişiklik: ${change.title}`, () => api("quality", "POST", "/api/v1/change-controls", {
      ...change,
      sourceCapaId: null, isTemporary: false, temporaryUntilUtc: null, siteImpact: false,
      ownerUserId: users.quality, targetDateUtc: at(60 + index * 30),
      impactedDepartmentIds: change.impactedDepartmentIds(),
    }));
    if (created && index === 0)
      await step("  → değerlendirmeye gönderildi", () => transition("quality", "/api/v1/change-controls", created, "submit"));
  }
}

// M.04 Doküman Yönetimi
if (runs("documents")) {
  const learnerPosition = await step("Eğitim pozisyonları okundu", async () => employee(users.learner).positionId);
  const documents = [
    {
      documentCode: "SOP-URT-014", title: "Tablet Baskı Hattı Açılış ve Kapanış Prosedürü", documentType: "SOP",
      departmentId: () => department("URT"), reviewDepartmentIds: () => [department("URT")],
      content: "1. Amaç\nTablet baskı hattının GMP'ye uygun açılış ve kapanışını tanımlamak.\n\n2. Kapsam\nTAB-01 ve TAB-02 hatları.\n\n3. Sorumluluklar\nVardiya amiri, hat operatörü, kalite güvence.\n\n4. Uygulama\n4.1 Hat açılış kontrol listesi doldurulur.\n4.2 Etiket ve malzeme mutabakatı yapılır.",
    },
    {
      documentCode: "TLM-KG-003", title: "Sapma Bildirimi ve Sınıflandırma Talimatı", documentType: "Talimat",
      departmentId: () => department("KG"), reviewDepartmentIds: () => [],
      content: "1. Amaç\nSapmaların zamanında bildirilmesi ve risk temelli sınıflandırılması.\n\n2. Uygulama\n2.1 Sapma tespit edildiğinde 24 saat içinde QMS'e kaydedilir.\n2.2 Olasılık × Şiddet × Tespit edilebilirlik ile risk puanı hesaplanır.",
    },
  ];
  for (const [index, document] of documents.entries()) {
    const created = await step(`Doküman: ${document.documentCode}`, () => api("quality", "POST", "/api/v1/documents", {
      ...document,
      sourceChangeControlId: null, ownerUserId: users.documentController, confidentiality: "Internal",
      reviewPeriodMonths: 24, plannedEffectiveDateUtc: at(45 + index * 10), changeSummary: "İlk yayın",
      departmentId: document.departmentId(), reviewDepartmentIds: document.reviewDepartmentIds(),
      trainingPositionIds: learnerPosition ? [learnerPosition] : [],
    }));
    if (created && index === 0)
      await step("  → yazım başlatıldı", () => transition("document-controller", "/api/v1/documents", created, "start-writing"));
  }
}

// M.05 Eğitim Yönetimi
if (runs("trainings")) {
  const trainings = [
    {
      employeeUserId: users.learner, courseCode: "EGT-GMP-101", courseTitle: "Temel GMP ve Hijyen Eğitimi",
      assessmentMode: "Exam", deliveryMethod: "Classroom", passingScore: 80, isCriticalQualification: false,
    },
    {
      employeeUserId: users.reporter, courseCode: "EGT-URT-014", courseTitle: "Tablet Baskı Hattı Operatör Yeterliliği",
      assessmentMode: "ExamAndPractical", deliveryMethod: "OnTheJob", passingScore: 85, isCriticalQualification: true,
    },
  ];
  for (const [index, training] of trainings.entries()) {
    const created = await step(`Eğitim: ${training.courseTitle}`, () => api("training-coordinator", "POST", "/api/v1/trainings", {
      ...training,
      matrixRuleId: null, controlledDocumentId: null, documentRevisionId: null,
      positionId: employee(training.employeeUserId).positionId,
      validityMonths: 12, maxAttempts: 3, dueAtUtc: at(21 + index * 14, 14),
      sessionCode: `S-${new Date().getUTCFullYear()}-${index + 1}`, trainer: "Barış Şen", assignNow: true,
    }));
    if (created && index === 0)
      await step("  → çalışan eğitime başladı", () => transition("learner", "/api/v1/trainings", created, "start"));
  }
}

// M.06 Müşteri Şikâyetleri
if (runs("complaints")) {
  const complaints = [
    {
      channel: "Email", customerName: "Anadolu Eczanesi", country: "TR", product: "Parasetamol 500 mg Tablet",
      batchNumber: "PAR5-26-0412", complaintType: "Packaging", severity: "Minor",
      description: "Blister folyosunda iki tablet cebinin boş olduğu bildirildi.", sampleExpected: true,
      attachmentSummary: "Müşteri fotoğrafı e-posta ile iletildi.",
    },
    {
      channel: "Phone", customerName: "Ege Ecza Deposu", country: "TR", product: "İbuprofen 200 mg Tablet",
      batchNumber: "IBU2-26-0187", complaintType: "ProductQuality", severity: "Major",
      description: "Kutudaki tabletlerin bir kısmında renk farklılığı ve kırılma olduğu bildirildi.", sampleExpected: true,
      attachmentSummary: "Numune kargo ile gönderilecek.",
    },
  ];
  for (const [index, complaint] of complaints.entries()) {
    const created = await step(`Şikâyet: ${complaint.customerName}`, () => api("quality", "POST", "/api/v1/complaints", {
      ...complaint,
      eventAtUtc: at(-(index + 4)), receivedAtUtc: at(-(index + 3)),
      hasHealthImpact: false, suspectedAdverseEvent: false, returnExpected: false,
      ownerUserId: users.quality, preliminaryResponseDueAtUtc: at(3 + index), finalResponseDueAtUtc: at(27 + index),
      investigationDepartmentIds: [department("URT")],
    }));
    if (created && index === 0)
      await step("  → triyaj başlatıldı", () => transition("quality", "/api/v1/complaints", created, "start-triage"));
  }
}

// M.07 İç Denetimler
if (runs("internal-audits")) {
  const internalAudits = [
    {
      title: "Üretim bölümü GMP iç denetimi", auditType: "Routine", auditee: "URT",
      scope: "Tablet üretim alanı, hat temizliği ve üretim kayıtları",
      objectives: "EU GMP Bölüm 5 gerekliliklerine uyumun doğrulanması",
      criteria: "EU GMP Part I Bölüm 5, SOP-URT-014",
      questions: [
        { question: "Hat açılış kontrolleri kayıt altına alınıyor mu?", reference: "SOP-URT-014 §4.1" },
        { question: "Etiket mutabakatı her vardiyada yapılıyor mu?", reference: "SOP-URT-014 §4.2" },
        { question: "Temizlik kayıtları ikinci kişi tarafından kontrol ediliyor mu?", reference: "EU GMP 5.19" },
      ],
    },
    {
      title: "Bilgi Teknolojileri veri bütünlüğü denetimi", auditType: "Routine", auditee: "BT",
      scope: "Bilgisayarlı sistemlerde erişim yetkileri ve yedekleme",
      objectives: "ALCOA+ ilkelerine ve EU GMP Ek 11'e uyumun doğrulanması",
      criteria: "EU GMP Ek 11, 21 CFR Part 11",
      questions: [
        { question: "Kullanıcı yetkileri periyodik olarak gözden geçiriliyor mu?", reference: "Ek 11 §12" },
        { question: "Yedekten geri yükleme testleri yapılıyor mu?", reference: "Ek 11 §7.2" },
      ],
    },
  ];
  for (const [index, audit] of internalAudits.entries()) {
    const { auditee, ...body } = audit;
    const created = await step(`İç denetim: ${audit.title}`, () => api("quality", "POST", "/api/v1/internal-audits", {
      ...body,
      planYear: new Date(at(20 + index * 21)).getUTCFullYear(),
      auditeeDepartment: "", auditeeDepartmentId: department(auditee),
      leadAuditorUserId: users.investigator, leadAuditor: "", leadAuditorDepartment: "",
      plannedStartUtc: at(20 + index * 21), plannedEndUtc: at(21 + index * 21, 15),
      isUnplanned: false, unplannedReason: null, checklistVersion: "2026.1",
    }));
    if (created && index === 0)
      await step("  → hazırlık başlatıldı", () => transition("quality", "/api/v1/internal-audits", created, "prepare"));
  }
}

// M.08 Dış Denetimler
if (runs("external-audits")) {
  const externalAudits = [
    {
      title: "TİTCK GMP rutin denetimi", auditKind: "Otorite denetimi", auditorOrganization: "TİTCK",
      isGovernmentAuthority: true, authorityCountry: "Türkiye", officialReference: "TİTCK-GMP-2026-118",
      scope: "Katı oral dozaj üretimi ve kalite sistemleri",
      documentRequests: [
        { controlledDocumentId: null, documentCode: "SMF-001", title: "Tesis Ana Dosyası", confidentiality: "Gizli" },
        { controlledDocumentId: null, documentCode: "VMP-001", title: "Validasyon Ana Planı", confidentiality: "Kurum İçi" },
      ],
    },
    {
      title: "Müşteri tedarikçi denetimi — Nordpharma GmbH", auditKind: "Müşteri denetimi",
      auditorOrganization: "Nordpharma GmbH", isGovernmentAuthority: false, authorityCountry: "Almanya",
      officialReference: "NP-SQA-2026-044", scope: "Fason tablet üretimi ve serbest bırakma süreci",
      documentRequests: [
        { controlledDocumentId: null, documentCode: "QAG-NP-01", title: "Kalite Anlaşması", confidentiality: "Gizli" },
      ],
    },
  ];
  for (const [index, audit] of externalAudits.entries()) {
    const created = await step(`Dış denetim: ${audit.title}`, () => api("quality", "POST", "/api/v1/external-audits", {
      ...audit,
      site: "İstanbul Üretim Tesisi", ownerUserId: users.quality, owner: "Elif Yılmaz",
      notifiedAtUtc: at(-(index + 5)), plannedStartUtc: at(25 + index * 20), plannedEndUtc: at(27 + index * 20, 15),
      responseDueAtUtc: at(57 + index * 20, 14),
      authorizedCloserUserId: users.qualifiedPerson, authorizedCloser: "Dr. Aylin Kurt",
    }));
    if (created && index === 0)
      await step("  → hazırlık başlatıldı", () => transition("quality", "/api/v1/external-audits", created, "start-preparation"));
  }
}

// M.09 Tedarikçi Denetimleri
if (runs("supplier-audits")) {
  const supplierAudits = [
    {
      supplierCode: "TED-API-007", supplierName: "Marmara Kimya A.Ş.", supplierScope: "Parasetamol etken madde üretimi",
      materialOrService: "Parasetamol API", country: "Türkiye", criticality: "Kritik",
      pastPerformanceScore: 78, openFindingCount: 1,
      scope: "API üretim ve kalite kontrol sistemleri", site: "Kocaeli Dilovası tesisi",
      checklist: [
        { category: "Kalite Sistemi", question: "Sapma ve DÖF süreci tanımlı ve etkin mi?", reference: "ICH Q7 §2" },
        { category: "Üretim", question: "Kritik proses parametreleri izleniyor mu?", reference: "ICH Q7 §8" },
      ],
    },
    {
      supplierCode: "TED-AMB-012", supplierName: "Trakya Ambalaj San. Ltd. Şti.", supplierScope: "Baskılı karton ve prospektüs",
      materialOrService: "Birincil olmayan ambalaj malzemesi", country: "Türkiye", criticality: "Orta",
      pastPerformanceScore: 91, openFindingCount: 0,
      scope: "Baskı ve renk kontrolü, sürüm yönetimi", site: "Tekirdağ Çorlu tesisi",
      checklist: [
        { category: "Baskı Kontrolü", question: "Baskı provaları onaylı sürüme göre doğrulanıyor mu?", reference: "EU GMP 5.41" },
      ],
    },
  ];
  for (const [index, audit] of supplierAudits.entries()) {
    const created = await step(`Tedarikçi denetimi: ${audit.supplierName}`, () => api("quality", "POST", "/api/v1/supplier-audits", {
      ...audit,
      supplierEvaluationId: null,
      leadAuditorUserId: users.investigator, leadAuditor: "Selin Arslan", leadAuditorDepartment: "Kalite Güvence",
      purchasingOwnerUserId: users.manager, purchasingOwner: "Hakan Özkan",
      verifierUserId: users.qualityReviewer, qualityApproverUserId: users.approver,
      plannedStartUtc: at(35 + index * 14), plannedEndUtc: at(36 + index * 14, 15), checklistVersion: "TD-2026.1",
    }));
    if (created && index === 0)
      await step("  → kapsam tanımlandı", () => transition("quality", "/api/v1/supplier-audits", created, "define-scope"));
  }
}

// M.10 İş Takibi
if (runs("work-items")) {
  const workItems = [
    {
      category: "FollowUp", priority: "High", title: "Kalibrasyon sertifikalarının e-arşive aktarılması",
      description: "Üçüncü çeyrek terazi kalibrasyon sertifikaları taranıp e-arşive yüklenecek.",
    },
    {
      category: "Improvement", priority: "Normal", title: "Hat açılış kontrol listesinin sadeleştirilmesi",
      description: "Operatör geri bildirimlerine göre tekrar eden kontrol adımları birleştirilecek.",
    },
  ];
  for (const [index, item] of workItems.entries()) {
    const created = await step(`İş: ${item.title}`, () => api("quality", "POST", "/api/v1/work-items", {
      ...item,
      sourceModule: null, sourceRecordId: null, sourceRecordNumber: null,
      ownerUserId: users.actionOwner, verifierUserId: users.quality, dueAtUtc: at(14 + index * 10, 14),
    }));
    if (created && index === 0)
      await step("  → sorumluya atandı", () => transition("quality", "/api/v1/work-items", created, "assign"));
  }
}

// M.11 Risk Yönetimi
if (runs("risks")) {
  const risk = await step("Risk: Tablet baskı prosesi FMEA", () => api("quality", "POST", "/api/v1/risks", {
    process: "Tablet baskı prosesi", scope: "TAB-01 hattı Parasetamol 500 mg", category: "Process",
    methodology: "FMEA", matrixVersion: "M11-FMEA-1.0", actionThreshold: 40,
    ownerUserId: users.quality, approverUserId: users.approver,
  }));
  const riskItems = [
    {
      failureMode: "Zımba aşınması nedeniyle düşük tablet sertliği", effect: "Ambalajlamada tablet kırılması",
      cause: "Zımba değişim periyodunun tanımlı olmaması", existingControls: "Saatlik IPC sertlik ölçümü",
      severity: 4, occurrence: 3, detectability: 4,
      action: "Zımba değişimini baskı sayısına bağlayan bakım planı oluşturulması",
      actionOwnerUserId: users.actionOwner, actionDueAtUtc: at(30, 14),
    },
    {
      failureMode: "Toz besleme dalgalanması", effect: "Tablet ağırlık varyasyonu",
      cause: "Hazne seviye sensörü kalibrasyon kayması", existingControls: "Otomatik ağırlık kontrolü ve ret",
      severity: 3, occurrence: 2, detectability: 2, action: null, actionOwnerUserId: null, actionDueAtUtc: null,
    },
  ];
  let scoredRisk = risk;
  for (const item of riskItems)
    if (scoredRisk)
      scoredRisk = await step(`  → risk satırı: ${item.failureMode}`, () => api("quality", "POST",
        `/api/v1/risks/${recordOf(scoredRisk).id}/items`, { expectedVersion: recordOf(scoredRisk).version, ...item }));
  if (scoredRisk) await step("  → puanlama başlatıldı", () => transition("quality", "/api/v1/risks", scoredRisk, "start-scoring"));
}

// M.12 Ana Üretim Kaydı
if (runs("mbr")) {
  const mbr = await step("Ana üretim kaydı: PRD-PAR-500", () => api("quality", "POST", "/api/v1/mbrs", {
    previousVersionId: null, productCode: "PRD-PAR-500", dosageFormCode: "Tablet", strength: "500 mg",
    batchSize: 250, batchUnitCode: "KG", siteCode: "IST-01", lineCode: "TAB-01", documentVersion: "1.0",
    changeReason: "İlk ana üretim kaydı", authorUserId: users.quality, reviewerUserId: users.manager,
    approverUserId: users.qualifiedPerson,
  }));
  if (mbr)
    await step("  → kritik kurutma adımı eklendi", () => api("quality", "POST", `/api/v1/mbrs/${recordOf(mbr).id}/steps`, {
      expectedVersion: recordOf(mbr).version, order: 1, phaseCode: "Granulation",
      instruction: "Granülatı akışkan yatak kurutucuda 50 °C'de 30 dakika kurutun.",
      materialOrEquipmentReference: "GR-02", isCritical: true, parameter: "Kurutma çıkış sıcaklığı",
      lowerLimit: 45, upperLimit: 55, unitCode: "C",
    }));
}

// M.13–M.16 Özel Kayıtlar
if (runs("specialized")) {
  const specialized = [
    {
      module: "m13", label: "Ambalaj metni: Parasetamol karton", typeCode: "Carton", subjectCode: "PRD-PAR-500",
      scopeCode: "TR-TR", reference: "ART-PAR5-KRT-v4", description: "Karton kutu metninde yeni yardımcı madde uyarısı güncellemesi.",
      structuredData: {
        proofDocumentNumber: "PRF-2026-031", proofVersion: "4", barcode: "8699123456789",
        proofSha256: "3f5b8a1c9d2e4f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a",
        regulatoryText: "Laktoz içerir. Çocukların ulaşamayacağı yerlerde saklayınız.",
      },
    },
    {
      module: "m14", label: "OOS: Parasetamol çözünme testi", typeCode: "OOS", subjectCode: "DISSOLUTION",
      scopeCode: "QC-CHEM", reference: "PAR5-26-0398 / N-2026-771",
      description: "30. dakika çözünme sonucu Q+5 limitinin altında bulundu.",
      structuredData: {
        specification: "Q=80% (30 dk)", observedResult: "%74", unit: "%",
        laboratoryInvestigation: "Faz 1: analist ve cihaz hatası bulunmadı.", hypothesis: "Granül sertliği",
        rootCause: "Kurutma aşırı sıcaklığı", disposition: "Faz 2 araştırması sürüyor",
        retestPerformed: false, retestAuthorized: false,
      },
    },
    {
      module: "m15", label: "Farmakovijilans: İbuprofen yan etki bildirimi", typeCode: "Initial", subjectCode: "PRD-IBU-200",
      scopeCode: "Domestic", reference: "PV-2026-0042", description: "Eczacı tarafından ciltte döküntü bildirimi iletildi.",
      structuredData: {
        eventTerm: "Ürtiker", patientCode: "HST-0042", source: "Eczacı", seriousness: "NonSerious",
        initialReceiptAtUtc: at(-2), regulatoryDueAtUtc: at(80),
      },
    },
    {
      module: "m16", label: "Tedarikçi değerlendirmesi: Marmara Kimya", typeCode: "Periodic", subjectCode: "API",
      scopeCode: "Critical", reference: "TED-API-007", description: "Yıllık tedarikçi performans değerlendirmesi.",
      structuredData: {
        score: 82, evaluationPeriod: "2025-10 / 2026-09", qualityScore: 85, deliveryScore: 78,
        qualificationDecision: "Approved",
      },
    },
  ];
  for (const [index, record] of specialized.entries()) {
    const { module, label, ...body } = record;
    await step(label, () => api("quality", "POST", `/api/v1/specialized/${module}`, {
      ...body,
      title: label.slice(label.indexOf(":") + 2),
      dueAtUtc: at(20 + index * 7, 14), ownerUserId: users.quality,
      reviewerUserId: users.investigator, approverUserId: users.qualifiedPerson,
    }));
  }
}

// Elektronik formlar
if (runs("forms")) {
  const field = (key, label, type, order, extra = {}) => ({
    key, label, type, required: true, helpText: "", placeholder: "", width: 6, order, maxLength: 500,
    min: null, max: null, unit: "", options: [], visibilityCondition: null, requiredCondition: null, ...extra,
  });
  const form = await step("Elektronik form: Terazi günlük kontrol formu", () => api("quality", "POST", "/api/v1/electronic-forms/definitions", {
    code: "FRM-TRZ-01", name: "Terazi Günlük Kontrol Formu", description: "Tartım odası terazilerinin günlük kontrolü",
    category: "Genel", kind: "Checklist", changeSummary: "İlk form sürümü", workflowType: "ReviewApprove",
    schema: {
      engineVersion: 1,
      sections: [{
        key: "kontrol", title: "Günlük kontrol", description: "", order: 1, columns: 2,
        fields: [
          field("teraziKodu", "Terazi kodu", "shortText", 1),
          field("referansAgirlik", "Referans ağırlık sonucu", "number", 2, { unit: "g", min: 0, max: 1000 }),
          field("seviyeUygun", "Su terazisi seviyesi uygun", "checkbox", 3),
          field("aciklama", "Açıklama", "longText", 4, { required: false, width: 12 }),
        ],
      }],
    },
    outputTemplate: {
      title: "", footerText: "Kontrollü elektronik kayıt", primaryColor: "#0F7773",
      includeEmptyFields: false, includeAuditTrail: true, includeSignatures: true,
    },
  }));
  const latestVersion = (details) =>
    [...(details?.versions ?? [])].sort((a, b) => b.versionNumber - a.versionNumber)[0];
  const reviewed = form && await step("  → incelemeye gönderildi", () =>
    api("quality", "POST", `/api/v1/electronic-forms/definitions/${form.definition.id}/submit-review`,
      { expectedVersion: latestVersion(form).rowVersion, password: null, meaningAccepted: false, comment: null }));
  const published = reviewed && await step("  → e-imza ile yayımlandı", () =>
    api("quality-reviewer", "POST", `/api/v1/electronic-forms/definitions/${form.definition.id}/publish`,
      { expectedVersion: latestVersion(reviewed).rowVersion, password: signaturePassword, meaningAccepted: true,
        comment: "Form tasarımı gözden geçirildi." }));
  if (published) {
    const formRecord = await step("Form kaydı: TRZ-004 günlük kontrol", () => api("reporter", "POST", "/api/v1/electronic-forms/records", {
      formDefinitionId: form.definition.id,
      data: { teraziKodu: "TRZ-004", referansAgirlik: 200.0003, seviyeUygun: true, aciklama: "Kontrol sonuçları kabul limitleri içinde." },
    }));
    if (formRecord)
      await step("  → onaya gönderildi", () => api("reporter", "POST", `/api/v1/electronic-forms/records/${recordOf(formRecord).id}/submit`,
        { expectedVersion: recordOf(formRecord).version }));
  }
}

const failed = outcomes.filter((outcome) => !outcome.ok);
console.log(`\n${outcomes.length - failed.length}/${outcomes.length} adım başarılı.`);
if (failed.length > 0) {
  console.log("Başarısız adımlar:");
  for (const outcome of failed) console.log(`  - ${outcome.label.trim()}: ${outcome.error}`);
  process.exitCode = 1;
}
