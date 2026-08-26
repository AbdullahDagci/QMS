using Qms.Contracts.System;

namespace Qms.Application.SystemCatalog;

public static class QmsModuleCatalog
{
    public static readonly IReadOnlyCollection<ModuleSummary> Modules =
    [
        new("M.01", "Sapma Yönetimi", "active"),
        new("M.02", "DÖF Yönetimi", "active"),
        new("M.03", "Değişiklik Kontrol", "active"),
        new("M.04", "Doküman Yönetimi", "active"),
        new("M.05", "Eğitim Yönetimi", "active"),
        new("M.06", "Müşteri Şikayetleri", "active"),
        new("M.07", "İç Denetimler", "active"),
        new("M.08", "Dış Denetimler", "active"),
        new("M.09", "Tedarikçi Denetimi", "active"),
        new("M.10", "İş Takip ve Aksiyon", "active"),
        new("M.11", "Risk Yönetimi (FMEA)", "active"),
        new("M.12", "MBR Yönetimi", "active"),
        new("M.13", "Artwork Yönetimi", "active"),
        new("M.14", "Limit Dışı Durum", "active"),
        new("M.15", "Farmakovijilans", "active"),
        new("M.16", "Tedarikçi Değerlendirme", "active"),
    ];
}
