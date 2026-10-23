export interface DyeingFilter {
  mode: number; client: string; series: string; orderNumber: string; color: string;
  batch: string; status: string; factory: string; productionOrder: string; fiber: string;
  from: string; to: string; page: number;
}
export interface DyeingCatalogOption { kind: string; code: string; description: string; }
export type DyeingRow = Record<string, string | number | boolean | null>;
export interface DyeingPage { rows: DyeingRow[]; totalRows: number; page: number; pageSize: number; }
