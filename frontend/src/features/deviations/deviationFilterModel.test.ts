import { describe, expect, it } from "vitest";
import { emptyDeviationFilters, toColumnFilters } from "./deviationFilterModel";

describe("deviation filter model", () => {
  it("maps typed UI filters to the server whitelist contract", () => {
    const filters = toColumnFilters({
      ...emptyDeviationFilters,
      title: "sıcaklık",
      riskMin: "21",
      riskMax: "50",
      classifications: ["Major", "Critical"],
      capaRequired: "true",
      targetFrom: "2026-08-01",
      targetTo: "2026-08-31",
    });

    expect(filters).toEqual([
      { field: "title", operator: "contains", value: "sıcaklık" },
      {
        field: "classification",
        operator: "in",
        values: ["Major", "Critical"],
      },
      { field: "capaRequired", operator: "equals", value: "true" },
      { field: "riskScore", operator: "between", value: "21", valueTo: "50" },
      {
        field: "targetDateUtc",
        operator: "between",
        value: "2026-08-01",
        valueTo: "2026-08-31",
      },
    ]);
  });
});
