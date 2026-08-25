import { Box, Chip, Paper, Stack, Typography } from "@mui/material";
import {
  AccessTimeRounded,
  ArrowForwardRounded,
  CheckCircleRounded,
  HistoryRounded,
  PersonRounded,
} from "@mui/icons-material";

export interface AuditTimelineEvent {
  id: string;
  version: number;
  eventType: string;
  actor: string;
  occurredAtUtc: string;
  reason: string | null;
  payload?: Record<string, unknown>;
}

export function AuditTimeline({
  events,
  labels,
}: {
  events: AuditTimelineEvent[];
  labels: Record<string, string>;
}) {
  const chronologicalEvents = [...events].sort(
    (left, right) =>
      new Date(right.occurredAtUtc).getTime() -
      new Date(left.occurredAtUtc).getTime(),
  );

  if (chronologicalEvents.length === 0) {
    return (
      <Paper variant="outlined" className="history-empty-state">
        <HistoryRounded />
        <Typography variant="body2" color="text.secondary">
          Henüz geçmiş kaydı bulunmuyor.
        </Typography>
      </Paper>
    );
  }

  return (
    <Box className="audit-timeline" aria-label="Kronolojik durum geçmişi">
      {chronologicalEvents.map((event, index) => {
        const isLatest = index === 0;
        const isLast = index === chronologicalEvents.length - 1;
        const presentation = auditPresentation(event, labels);
        return (
          <Box
            className={`audit-timeline-item${isLatest ? " latest" : ""}`}
            key={event.id}
          >
            <Box className="audit-timeline-rail" aria-hidden="true">
              <Box className="audit-timeline-marker">
                {isLatest ? <CheckCircleRounded /> : <span>{index + 1}</span>}
              </Box>
              {!isLast && <Box className="audit-timeline-line" />}
            </Box>

            <Paper variant="outlined" className="audit-event-card">
              <Stack
                direction={{ xs: "column", sm: "row" }}
                sx={{
                  justifyContent: "space-between",
                  alignItems: { xs: "flex-start", sm: "center" },
                  gap: 1,
                }}
              >
                <Box>
                  <Stack
                    direction="row"
                    spacing={1}
                    sx={{ alignItems: "center", flexWrap: "wrap" }}
                  >
                    <Typography className="audit-event-title">
                      {presentation.title}
                    </Typography>
                    {isLatest && (
                      <Chip size="small" color="primary" label="Son işlem" />
                    )}
                  </Stack>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 0.35 }}
                  >
                    {presentation.description}
                  </Typography>
                </Box>
                <Stack
                  direction="row"
                  spacing={0.7}
                  className="audit-event-time"
                >
                  <AccessTimeRounded />
                  <Typography variant="body2">
                    {formatDateTime(event.occurredAtUtc)}
                  </Typography>
                </Stack>
              </Stack>

              {presentation.from && presentation.to && (
                <Stack direction="row" className="audit-status-route">
                  <Chip
                    size="small"
                    variant="outlined"
                    label={statusLabel(presentation.from)}
                  />
                  <ArrowForwardRounded />
                  <Chip
                    size="small"
                    color="primary"
                    variant="outlined"
                    label={statusLabel(presentation.to)}
                  />
                </Stack>
              )}

              <Box className="audit-event-meta">
                <Stack
                  direction="row"
                  spacing={0.7}
                  sx={{ alignItems: "center" }}
                >
                  <PersonRounded />
                  <Typography variant="caption">{event.actor}</Typography>
                </Stack>
                <Chip
                  size="small"
                  variant="outlined"
                  label={`Kayıt v${event.version}`}
                />
              </Box>

              {event.reason && (
                <Box className="audit-event-reason">
                  <Typography variant="caption" color="text.secondary">
                    İşlem notu / gerekçe
                  </Typography>
                  <Typography variant="body2">{event.reason}</Typography>
                </Box>
              )}
            </Paper>
          </Box>
        );
      })}
    </Box>
  );
}

function auditPresentation(
  event: AuditTimelineEvent,
  labels: Record<string, string>,
) {
  const from = payloadString(event.payload, "from", "From");
  const to = payloadString(event.payload, "to", "To");
  const transition = payloadString(event.payload, "transition", "Transition");
  const actionType = payloadString(event.payload, "actionType", "ActionType");
  const owner = payloadString(event.payload, "owner", "Owner");
  const approved = payloadBoolean(event.payload, "approved", "Approved");

  if (event.eventType === "CapaStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      submit: [
        "Kapsam onayına gönderildi",
        "DÖF taslağı kapsam değerlendirmesi için onaya sunuldu.",
      ],
      "approve-scope": [
        "DÖF kapsamı onaylandı",
        "Uygunsuzluğun kapsamı kabul edilerek kök neden onayına geçildi.",
      ],
      "approve-root-cause": [
        "Kök neden onaylandı",
        "Doğrulanan kök neden için aksiyon planlama aşaması başlatıldı.",
      ],
      "submit-plan": [
        "Aksiyon planı onaya gönderildi",
        "Planlanan düzeltici ve önleyici faaliyetler onaya sunuldu.",
      ],
      "approve-plan": [
        "Aksiyon planı onaylandı",
        "Onaylanan faaliyetler uygulama aşamasına alındı.",
      ],
      "request-action-verification": [
        "Aksiyonlar KG doğrulamasına gönderildi",
        "Tamamlama kanıtları Kalite Güvence değerlendirmesine sunuldu.",
      ],
      "approve-actions": [
        "Aksiyon doğrulamaları tamamlandı",
        to === "EffectivenessWaiting"
          ? "Doğrulanan aksiyonlar için etkinlik gözlem dönemi başlatıldı."
          : "Doğrulanan aksiyonlar kapanış onayına gönderildi.",
      ],
      "start-effectiveness-review": [
        "Etkinlik değerlendirmesi başlatıldı",
        "Tanımlanan gözlem dönemi tamamlandı ve sonuç incelemesine geçildi.",
      ],
      "complete-effectiveness": [
        to === "ActionPlanning"
          ? "Etkinlik başarısız bulundu"
          : "Etkinlik başarılı bulundu",
        to === "ActionPlanning"
          ? "Hedef kriterler karşılanmadığı için aksiyon planı revizyona döndü."
          : "Başarı kriterleri karşılandı ve DÖF kapanış onayına gönderildi.",
      ],
      close: [
        "DÖF kaydı kapatıldı",
        "Tüm aksiyon, doğrulama ve kapanış koşulları tamamlandı.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "DÖF durumu değiştirildi",
      "DÖF kontrollü iş akışında yeni bir aşamaya geçirildi.",
    ];
    return { title, description, from, to };
  }

  if (event.eventType === "DeviationStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      "start-preliminary-review": [
        "Ön inceleme başlatıldı",
        "Gönderilen sapma ilk kalite incelemesine alındı.",
      ],
      "start-investigation": [
        "Kök neden araştırması başlatıldı",
        "Ön inceleme tamamlandı ve araştırma aşamasına geçildi.",
      ],
      "complete-investigation": [
        "Araştırma tamamlandı",
        "Kök neden bulguları tamamlanarak etki değerlendirmesine geçildi.",
      ],
      "complete-impact-assessment": [
        "Etki değerlendirmesi tamamlandı",
        "Batch ve seri kararları tamamlanarak KG değerlendirmesine geçildi.",
      ],
      "complete-quality-assessment": [
        "KG değerlendirmesi tamamlandı",
        to === "ActionImplementation"
          ? "İlişkili DÖF üzerinden aksiyon uygulama aşaması başlatıldı."
          : "Kalite kararı sonrası bir sonraki kontrollü aşamaya geçildi.",
      ],
      "complete-linked-actions": [
        "İlişkili DÖF aksiyonları tamamlandı",
        "Bağlı DÖF kapandığı için sapma kapanış sürecine devam etti.",
      ],
      "complete-effectiveness-review": [
        "Etkinlik doğrulandı",
        "Aksiyonların etkili olduğu doğrulanarak kapanış onayına geçildi.",
      ],
      close: [
        "Sapma kaydı kapatıldı",
        "Tüm araştırma, etki ve bağımlılık koşulları tamamlandı.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "Sapma durumu değiştirildi",
      "Sapma kontrollü iş akışında yeni bir aşamaya geçirildi.",
    ];
    return { title, description, from, to };
  }

  if (event.eventType === "TrainingStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      assign: [
        "Eğitim görevi çalışana atandı",
        "Planlanan eğitim görevi katılımcının aktif iş listesine alındı.",
      ],
      start: [
        "Katılımcı eğitimi başlattı",
        "Atanan çalışan eğitim içeriğini tamamlamak üzere kontrollü süreci başlattı.",
      ],
      "submit-assessment": [
        "Eğitim değerlendirmeye gönderildi",
        "Katılım ve gerekli okuma imzası tamamlanarak eğitmen değerlendirmesi açıldı.",
      ],
      approve: [
        "Yeterlilik onaylandı",
        "Başarılı değerlendirme eğitmen tarafından onaylandı ve süreli yeterlilik oluşturuldu.",
      ],
      reassign: [
        "Eğitim yeniden atandı",
        "Başarısız veya süresi dolan eğitim yeni deneme için katılımcıya tekrar atandı.",
      ],
      expire: [
        "Yeterliliğin süresi doldu",
        "Geçerlilik tarihi geçen yeterlilik yenileme sürecine alındı.",
      ],
      cancel: [
        "Eğitim görevi iptal edildi",
        "Eğitim görevi kayıtlı gerekçeyle kontrollü olarak iptal edildi.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "Eğitim durumu değiştirildi",
      "Eğitim görevi kontrollü yaşam döngüsünde yeni bir aşamaya geçirildi.",
    ];
    return { title, description, from, to };
  }

  if (event.eventType === "ComplaintStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      "start-triage": [
        "Şikâyet triyaja alındı",
        "Önem, ürün kalitesi, hasta güvenliği ve tekrar sinyali değerlendirmesi başlatıldı.",
      ],
      "complete-triage": [
        "Triyaj ve otomatik bağlantılar tamamlandı",
        "Gerekli M.01 sapma ve farmakovijilans yönlendirmeleri oluşturularak ön yanıt aşamasına geçildi.",
      ],
      "start-investigation": [
        "Paralel bölüm araştırmaları başlatıldı",
        "Onaylı ön yanıt sonrası seçilen bölümlerin araştırma görevleri eş zamanlı açıldı.",
      ],
      "finish-investigations": [
        "Tüm bölüm araştırmaları tamamlandı",
        "Paralel bulgular ortak etki ve kök neden değerlendirmesine aktarıldı.",
      ],
      close: [
        "Müşteri şikâyeti kapatıldı",
        "Onaylı nihai yanıt ve gerekli güvenlik aktarımı doğrulanarak kayıt kontrollü biçimde kapatıldı.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "Şikâyet aşaması değiştirildi",
      "Şikâyet kontrollü yaşam döngüsünde yeni bir aşamaya geçirildi.",
    ];
    return { title, description, from, to };
  }

  if (event.eventType === "InternalAuditStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      prepare: [
        "Denetim hazırlığı başlatıldı",
        "Kapsam, denetçi bağımsızlığı ve soru listesi sürümü hazırlık kontrolüne alındı.",
      ],
      "submit-plan": [
        "Plan onaya gönderildi",
        "Baş denetçinin denetlenen bölümden bağımsız olduğu doğrulanarak plan onayı istendi.",
      ],
      "approve-plan": [
        "Plan ve soru listesi kilitlendi",
        "Onaylanan soru listesi sürümü değiştirilemez zaman damgasıyla denetim uygulamasına açıldı.",
      ],
      "complete-execution": [
        "Saha uygulaması tamamlandı",
        "Tüm soruların sonuç, objektif kanıt ve denetçi notları kaydedilerek bulgular aşamasına geçildi.",
      ],
      "confirm-findings": [
        "Denetim bulguları kesinleştirildi",
        "Risk sınıfları ve gerekli M.02 DÖF bağlantıları doğrulanarak bölüm yanıtları açıldı.",
      ],
      "complete-responses": [
        "Bulgu yanıtları doğrulamaya gönderildi",
        "Denetlenen bölümün yanıt ve düzeltici aksiyonları denetçi doğrulamasına sunuldu.",
      ],
      "start-finding-closure": [
        "Bulgu kapanış kontrolü başlatıldı",
        "Doğrulanan bulgular ve bağlı DÖF durumları toplu kapanış kontrolüne alındı.",
      ],
      "request-audit-closure": [
        "Denetim kapanış onayına gönderildi",
        "Tüm bulguların kapalı olduğu doğrulanarak nihai denetim kapanışı istendi.",
      ],
      close: [
        "İç denetim kapatıldı",
        "Kilitli soru listesi, kanıtlar, bulgular ve DÖF bağımlılıkları tamamlanarak kayıt kapatıldı.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "İç denetim aşaması değiştirildi",
      "Denetim kontrollü yaşam döngüsünde yeni bir aşamaya geçirildi.",
    ];
    return { title, description, from, to };
  }

  if (event.eventType === "ExternalAuditStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      "start-preparation": [
        "Dış denetim hazırlığı başlatıldı",
        "Resmi bildirim kapsamı ve kontrollü doküman talep paketi hazırlığa alındı.",
      ],
      "start-audit": [
        "Saha denetimi başlatıldı",
        "Talep paketindeki tüm dokümanların erişim kayıtlı dışa aktarımı doğrulandı.",
      ],
      "complete-audit": [
        "Dış denetim uygulaması tamamlandı",
        "Saha görüşmeleri ve resmi bulgu girişi tamamlanarak bulgular aşamasına geçildi.",
      ],
      "open-response-plan": [
        "Resmi cevap planı açıldı",
        "Otorite veya müşteri bulguları için cevap, taahhüt ve hedef tarih çalışması başlatıldı.",
      ],
      "start-capa-actions": [
        "Cevaplar DÖF ve aksiyon doğrulamasına gönderildi",
        "Tüm resmi cevap ve taahhütler kaydedilerek bağlı M.02 kayıtları izlemeye alındı.",
      ],
      "complete-capa-actions": [
        "Bulgu ve DÖF kapanışları tamamlandı",
        "Tüm resmi bulgular kapatılarak kapanış mektubu aşamasına geçildi.",
      ],
      "submit-authority-closure": [
        "Kapanış kanıtı yetkili onayına gönderildi",
        "Kapanış mektubu ile otorite/müşteri kabul kanıtı nihai kapanış görevine sunuldu.",
      ],
      close: [
        "Dış denetim yetkili tarafından kapatıldı",
        "Bildirim, paylaşım, bulgu, taahhüt, DÖF ve kapanış kanıtı zinciri tamamlandı.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "Dış denetim aşaması değiştirildi",
      "Dış denetim kontrollü yaşam döngüsünde yeni bir aşamaya geçirildi.",
    ];
    return { title, description, from, to };
  }

  if (event.eventType === "SupplierAuditStatusChanged") {
    const transitions: Record<string, [string, string]> = {
      "define-scope": [
        "Tedarikçi denetim kapsamı sabitlendi",
        "Risk girdileri, kapsam ve sürümlü soru listesi denetim planına bağlandı.",
      ],
      "assign-auditor": [
        "Tedarikçi baş denetçisi atandı",
        "Denetim ve satınalma sorumlulukları kayıt bazlı görevlere dönüştürüldü.",
      ],
      "start-audit": [
        "Tedarikçi saha denetimi başlatıldı",
        "Soru listesi sürümü kilitlenerek objektif kanıt toplama aşaması açıldı.",
      ],
      "complete-audit": [
        "Tedarikçi denetimi uygulaması tamamlandı",
        "Tüm soru maddeleri cevaplanarak bulgular kesinleştirme aşamasına geçti.",
      ],
      "request-supplier-response": [
        "Tedarikçi cevap süreci açıldı",
        "Bulgular için cevap, taahhüt, hedef tarih ve güvenli davet süreci başlatıldı.",
      ],
      "start-verification": [
        "Tedarikçi cevapları kanıt doğrulamaya gönderildi",
        "Tüm bulgu cevapları alınarak kanıt paketlerinin değerlendirmesi başlatıldı.",
      ],
      "start-capa": [
        "Kanıtlar DÖF/CAPA doğrulamasına gönderildi",
        "Tedarikçi kanıtları ve bağlı M.02 kayıtları kapanış kontrolüne alındı.",
      ],
      "record-result": [
        "Tedarikçi denetim sonucu aşaması açıldı",
        "Tüm bulgular kapatılarak kapsam bazlı nitelendirme kararı istendi.",
      ],
      close: [
        "Tedarikçi denetimi kapatıldı",
        "Risk hesabı, kanıt, DÖF ve nitelendirme kararı tamamlanarak denetim paketi kapatıldı.",
      ],
    };
    const [title, description] = transitions[transition ?? ""] ?? [
      "Tedarikçi denetimi aşaması değiştirildi",
      "Tedarikçi denetimi kontrollü yaşam döngüsünde ilerletildi.",
    ];
    return { title, description, from, to };
  }

  const presentations: Record<string, [string, string]> = {
    CapaCreated: [
      "DÖF taslağı oluşturuldu",
      "Yeni düzeltici ve önleyici faaliyet kaydı kontrollü sistemde açıldı.",
    ],
    CapaActionAdded: [
      `${actionType ?? "DÖF"} aksiyonu plana eklendi`,
      owner
        ? `Aksiyon sorumlusu ${owner} olarak atandı.`
        : "Yeni faaliyet aksiyon planına bağlandı.",
    ],
    CapaActionCompletionRequested: [
      "Aksiyon tamamlandı olarak bildirildi",
      "Aksiyon sahibi tamamlama kanıtını KG doğrulamasına sundu.",
    ],
    CapaActionVerified: [
      approved === false
        ? "Aksiyon KG tarafından reddedildi"
        : "Aksiyon KG tarafından doğrulandı",
      approved === false
        ? "Sunulan kanıt yeterli bulunmadı ve aksiyon revizyona döndü."
        : "Sunulan tamamlama kanıtı uygun bulundu.",
    ],
    DeviationCreated: [
      "Sapma taslağı oluşturuldu",
      "Yeni sapma kaydı kontrollü sistemde açıldı.",
    ],
    DeviationSubmitted: [
      "Sapma değerlendirmeye gönderildi",
      "Taslak tamamlandı ve kalite iş akışı başlatıldı.",
    ],
    DeviationInvestigationCompleted: [
      "Kök neden araştırması eklendi",
      "Araştırma yöntemi, kök neden ve sonuç kayda bağlandı.",
    ],
    DeviationBatchImpactAssessed: [
      "Batch / seri etkisi değerlendirildi",
      "Etkilenen batch veya seri için kontrollü karar kaydedildi.",
    ],
    TrainingAssignmentCreated: [
      "Eğitim görevi oluşturuldu",
      "Eğitim koordinatörü çalışan, yöntem, hedef tarih ve yeterlilik koşullarını tanımladı.",
    ],
    TrainingAssignmentCreatedFromDocument: [
      "M.04 dokümanından eğitim görevi üretildi",
      "Onaylanan doküman sürümünün eğitim gereksinimi ilgili çalışana otomatik atandı.",
    ],
    TrainingReadAcknowledged: [
      "Okuma ve anlama imzalandı",
      "Katılımcı güncel içeriği okuduğunu, anladığını ve uygulayacağını elektronik olarak beyan etti.",
    ],
    TrainingAssessmentPassed: [
      "Değerlendirme başarılı",
      "Sınav ve/veya pratik yeterlilik sonucu tanımlanan başarı kriterini karşıladı.",
    ],
    TrainingAssessmentFailed: [
      "Değerlendirme başarısız",
      "Değerlendirme sonucu başarı kriterini karşılamadı; deneme geçmişi değiştirilemez olarak korundu.",
    ],
    ComplaintCreated: [
      "Müşteri şikâyeti kaydı oluşturuldu",
      "Bildirim, ürün, batch, SLA ve paralel araştırma kapsamı kontrollü kayda alındı.",
    ],
    ComplaintResponseDrafted: [
      "Müşteri yanıtının yeni sürümü hazırlandı",
      "Ön veya nihai yanıt değiştirilemez onay sürecine sunulmak üzere sürümlendi.",
    ],
    ComplaintResponseApproved: [
      "Müşteri yanıtı onaylandı",
      "Hazırlanan yanıt sürümü yetkili kullanıcı tarafından onaylanarak sabitlendi.",
    ],
    ComplaintInvestigationCompleted: [
      "Bölüm araştırması tamamlandı",
      "Araştırma bulguları ve kök neden katkısı denetlenebilir kayda bağlandı.",
    ],
    ComplaintImpactAssessed: [
      "Etki ve kök neden kararı kaydedildi",
      "Paralel bulgular ürün, batch ve tekrar etkisiyle birlikte değerlendirildi.",
    ],
    ComplaintCapaDecisionRecorded: [
      "DÖF kararı kaydedildi",
      "Şikâyetin kök neden ve tekrar riski için M.02 bağlantısı değerlendirildi.",
    ],
    InternalAuditCreated: [
      "İç denetim planı oluşturuldu",
      "Kapsam, kriter, denetçi, takvim ve sürümlü soru listesi kontrollü kayda alındı.",
    ],
    AuditQuestionAnswered: [
      "Denetim sorusu kanıtıyla yanıtlandı",
      "Uygunluk sonucu, objektif kanıt ve denetçi notu kilitli soru listesine kaydedildi.",
    ],
    AuditFindingCreated: [
      "Risk sınıflı denetim bulgusu açıldı",
      "Etki ve olasılık puanından bulgu sınıfı hesaplandı; gerekiyorsa M.02 DÖF otomatik bağlandı.",
    ],
    AuditFindingResponseSubmitted: [
      "Bulgu yanıtı ve aksiyonu sunuldu",
      "Denetlenen bölümün nedeni ve düzeltici aksiyonu doğrulama aşamasına kaydedildi.",
    ],
    AuditFindingClosed: [
      "Denetim bulgusu doğrulanarak kapatıldı",
      "Kapanış kanıtı ve varsa bağlı M.02 DÖF durumu yetkili kullanıcı tarafından doğrulandı.",
    ],
    ExternalAuditCreated: [
      "Dış denetim kaydı oluşturuldu",
      "Denetleyen kurum, resmi referans, kapsam, takvim ve talep paketi kontrollü kayda alındı.",
    ],
    ExternalAuditDocumentExported: [
      "Talep paketi dokümanı kontrollü dışa aktarıldı",
      "Yetkili alıcı, paylaşım amacı, manifest kanıtı ve dışa aktarım sürümü erişim günlüğüne yazıldı.",
    ],
    ExternalAuditFindingCreated: [
      "Resmi dış denetim bulgusu açıldı",
      "Otorite/müşteri referansı ve sınıfı kaydedildi; gerekiyorsa M.02 DÖF otomatik bağlandı.",
    ],
    ExternalAuditFindingResponseSubmitted: [
      "Resmi cevap ve kurumsal taahhüt kaydedildi",
      "Bulgu cevabı, taahhüt edilen aksiyon ve hedef tarih doğrulama için sabitlendi.",
    ],
    ExternalAuditFindingClosed: [
      "Dış denetim bulgusu kapatıldı",
      "Taahhüt kanıtı ile varsa bağlı M.02 DÖF kapanışı yetkili kullanıcı tarafından doğrulandı.",
    ],
    ExternalAuditClosureLetterRecorded: [
      "Kapanış mektubu ve kabul kanıtı kaydedildi",
      "Otorite/müşteri referansı, dosya kanıtı ve kabul kararı yetkili kapanışına hazırlandı.",
    ],
    SupplierAuditCreated: [
      "Risk bazlı tedarikçi denetimi oluşturuldu",
      "Kritiklik, geçmiş performans ve açık bulgulardan risk skoru ile denetim frekansı hesaplandı.",
    ],
    SupplierAuditChecklistAnswered: [
      "Tedarikçi denetim sorusu kanıtıyla yanıtlandı",
      "Uygunluk sonucu, objektif kanıt ve denetçi notu kilitli soru listesine kaydedildi.",
    ],
    SupplierAuditFindingCreated: [
      "Risk sınıflı tedarikçi bulgusu açıldı",
      "Bulgu sınıfı ve kapsam statüsü kaydedildi; majör/kritik bulguda M.02 DÖF otomatik bağlandı.",
    ],
    SupplierAuditFindingResponseSubmitted: [
      "Tedarikçi cevabı ve taahhüdü kaydedildi",
      "Tedarikçi yanıtı, planlanan faaliyet ve hedef tarih değiştirilemez kayda alındı.",
    ],
    SupplierAuditFindingEvidenceSubmitted: [
      "Tedarikçi kanıt paketi sunuldu",
      "Taahhüt kanıtı doğrulama ve bağlı DÖF kapanış kontrolüne hazırlandı.",
    ],
    SupplierAuditFindingClosed: [
      "Tedarikçi bulgusu doğrulanarak kapatıldı",
      "Kanıt ve varsa bağlı M.02 DÖF kapanışı yetkili kullanıcı tarafından doğrulandı.",
    ],
    SupplierAuditInvitationCreated: [
      "Tek kullanımlık tedarikçi daveti oluşturuldu",
      "Süreli güvenli davetin yalnız token özeti saklandı; token sadece oluşturma anında gösterildi.",
    ],
    SupplierAuditInvitationResponseAccepted: [
      "Güvenli tedarikçi cevabı kabul edildi",
      "Tek kullanımlık davet tüketilerek bulgu cevabı ve kanıtı denetim kaydına bağlandı.",
    ],
    SupplierAuditResultRecorded: [
      "Kapsam bazlı nitelendirme kararı kaydedildi",
      "Onay, koşul, askı veya yeniden nitelendirme kararı gerekçe ve geçerlilik tarihiyle sabitlendi.",
    ],
  };
  const [title, description] = presentations[event.eventType] ?? [
    labels[event.eventType] ?? readableEventType(event.eventType),
    "Kayıt üzerinde denetlenebilir bir işlem gerçekleştirildi.",
  ];
  return { title, description, from, to };
}

function payloadString(
  payload: Record<string, unknown> | undefined,
  ...keys: string[]
) {
  for (const key of keys)
    if (typeof payload?.[key] === "string") return payload[key] as string;
  return undefined;
}

function payloadBoolean(
  payload: Record<string, unknown> | undefined,
  ...keys: string[]
) {
  for (const key of keys)
    if (typeof payload?.[key] === "boolean") return payload[key] as boolean;
  return undefined;
}

function statusLabel(status: string) {
  const labels: Record<string, string> = {
    Draft: "Taslak",
    Submitted: "Gönderildi",
    PreliminaryReview: "Ön inceleme",
    Investigation: "Araştırma",
    ImpactAssessment: "Etki değerlendirmesi",
    QualityAssessment: "KG değerlendirmesi",
    ActionImplementation: "Aksiyon uygulama",
    ScopeApproval: "Kapsam onayı",
    RootCauseApproval: "Kök neden onayı",
    ActionPlanning: "Aksiyon planlama",
    PlanApproval: "Plan onayı",
    Implementation: "Uygulama",
    ActionVerification: "KG aksiyon doğrulaması",
    EffectivenessWaiting: "Etkinlik bekleme",
    EffectivenessReview: "Etkinlik değerlendirmesi",
    ClosureApproval: "Kapanış onayı",
    Planned: "Planlandı",
    Assigned: "Atandı",
    InProgress: "Devam ediyor",
    Assessment: "Değerlendirme",
    TrainerApproval: "Eğitmen onayı",
    Completed: "Tamamlandı",
    Failed: "Başarısız",
    Expired: "Süresi doldu",
    Cancelled: "İptal",
    Closed: "Kapalı",
    Voided: "İptal",
    Received: "Alındı",
    Triage: "Triyaj",
    PreliminaryResponse: "Ön yanıt",
    CapaDecision: "DÖF kararı",
    FinalResponseApproval: "Nihai yanıt onayı",
    AnnualPlan: "Yıllık plan",
    Preparation: "Denetim hazırlığı",
    Execution: "Uygulama",
    Findings: "Bulgular",
    ResponseAction: "Cevap / aksiyon",
    CapaVerification: "DÖF / doğrulama",
    FindingClosure: "Bulgu kapanışı",
    AuditClosure: "Denetim kapanışı",
    Notified: "Planlandı / bildirildi",
    AuditInProgress: "Dış denetim",
    ResponsePlan: "Cevap planı",
    CapaAction: "DÖF / aksiyon",
    ClosureLetter: "Kapanış mektubu",
    AuthorityClosure: "Yetkili kapanışı",
    RiskPlan: "Risk bazlı plan",
    ScopeChecklist: "Kapsam / soru listesi",
    AuditorAssignment: "Denetçi atama",
    SupplierResponse: "Tedarikçi cevabı",
    EvidenceVerification: "Kanıt doğrulama",
    Capa: "DÖF / CAPA",
    AuditResult: "Denetim sonucu",
  };
  return labels[status] ?? status;
}

function readableEventType(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
