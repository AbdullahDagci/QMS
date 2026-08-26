import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ElectronicSignaturePanel } from "./ElectronicSignaturePanel";

describe("ElectronicSignaturePanel", () => {
  it("shows the exact meaning and forwards authentication inputs", async () => {
    const user = userEvent.setup();
    const onPasswordChange = vi.fn();
    const onAcceptedChange = vi.fn();

    render(
      <ElectronicSignaturePanel
        meaning="DÖF kalite onayı"
        password=""
        accepted={false}
        onPasswordChange={onPasswordChange}
        onAcceptedChange={onAcceptedChange}
      />,
    );

    expect(screen.getByText("DÖF kalite onayı")).toBeInTheDocument();
    await user.type(screen.getByLabelText(/Parolanızı yeniden girin/), "Sifre1!");
    await user.click(screen.getByRole("checkbox"));

    expect(onPasswordChange).toHaveBeenCalled();
    expect(onAcceptedChange).toHaveBeenCalledWith(true);
  });
});
