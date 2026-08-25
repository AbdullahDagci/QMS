import { describe, expect, it } from "vitest";
import {
  emptyTrainingFilters,
  toTrainingColumnFilters,
} from "./trainingFilterModel";
describe("training filters", () => {
  it("omits empty values", () =>
    expect(toTrainingColumnFilters(emptyTrainingFilters)).toEqual([]));
  it("maps typed filters", () =>
    expect(
      toTrainingColumnFilters({
        ...emptyTrainingFilters,
        status: "Assigned",
        critical: "true",
        dueFrom: "2026-08-01",
        dueTo: "2026-08-31",
      }),
    ).toEqual([
      { field: "status", operator: "equals", value: "Assigned" },
      { field: "isCriticalQualification", operator: "equals", value: "true" },
      {
        field: "dueAtUtc",
        operator: "between",
        value: "2026-08-01",
        valueTo: "2026-08-31",
      },
    ]));
});
