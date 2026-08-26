import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "./LoginPage";

describe("LoginPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        Response.json([
          {
            key: "learner",
            displayName: "Aslı Çetin",
            departmentName: "Üretim",
            roles: ["Learner"],
          },
          {
            key: "trainer",
            displayName: "Barış Şen",
            departmentName: "Kalite Güvence",
            roles: ["Trainer"],
          },
        ]),
      ),
    );
  });

  it("shows role-aware quick profiles and filters them", async () => {
    render(<LoginPage />);

    expect(
      await screen.findByRole("button", { name: /Aslı Çetin/ }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /Barış Şen/ }),
    ).toBeInTheDocument();
    expect(screen.getByText("Eğitim Katılımcısı")).toBeInTheDocument();
    expect(screen.getByText("Eğitmen")).toBeInTheDocument();

    await userEvent.type(
      screen.getByRole("textbox", { name: "Hızlı giriş profillerinde ara" }),
      "eğitmen",
    );

    expect(
      screen.queryByRole("button", { name: /Aslı Çetin/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /Barış Şen/ }),
    ).toBeInTheDocument();
    expect(screen.getByText("1 test profili")).toBeInTheDocument();
  });
});
