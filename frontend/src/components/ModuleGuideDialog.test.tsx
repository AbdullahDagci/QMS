import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ModuleGuideDialog } from "./ModuleGuideDialog";

describe("ModuleGuideDialog", () => {
  it("M.01 için simülasyon yerine sıralı iş akışı ve rol kılavuzunu gösterir", () => {
    render(<ModuleGuideDialog module="M.01" open onClose={vi.fn()} />);

    expect(screen.getByRole("heading", { name: "Sapma Yönetimi" })).toBeInTheDocument();
    expect(screen.getByText("9 aşama")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Sapma kaydını oluştur" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Hangi rol hangi işlevi yapar?" })).toBeInTheDocument();
    expect(screen.getByText("DeviationReporter")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Bağlandığı süreçlerle veri ve durum iletişimi" })).toBeInTheDocument();
    expect(screen.getByText("M.01 → M.02 → M.01")).toBeInTheDocument();
    expect(screen.getByText(/SourceDeviationId ile M\.01'e bağlanır/)).toBeInTheDocument();
    expect(screen.queryByText(/simülasyon/i)).not.toBeInTheDocument();
  });

  it("M.02 görev ve permission ayrımını açıklar", () => {
    render(<ModuleGuideDialog module="M.02" open onClose={vi.fn()} />);

    expect(screen.getByRole("heading", { name: "DÖF Yönetimi" })).toBeInTheDocument();
    expect(screen.getByText("11 aşama")).toBeInTheDocument();
    expect(screen.getByText("capa.plan", { selector: ".MuiChip-label" })).toBeInTheDocument();
    expect(screen.getByText("Görev: Action:{id}")).toBeInTheDocument();
    expect(screen.getByText("İzin: capa.complete-action")).toBeInTheDocument();
    expect(screen.getByText("M.07 / M.08 / M.09 · Denetim Bulguları")).toBeInTheDocument();
    expect(screen.getByText(/DÖF açıkken bulgu; bulgular açıkken denetim kapanamaz/)).toBeInTheDocument();
  });
});
