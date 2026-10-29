export type ImportBatchStatus = 'Running' | 'Succeeded' | 'CompletedWithErrors' | 'Failed';

export interface SourceImportSummary {
  sourceSystem: string;
  received: number;
  inserted: number;
  updated: number;
  unchanged: number;
  revoked: number;
  restored: number;
  rejected: number;
  error: string | null;
}

export interface ImportResult {
  batchId: string;
  status: ImportBatchStatus;
  startedAtUtc: string;
  completedAtUtc: string;
  inserted: number;
  updated: number;
  unchanged: number;
  revoked: number;
  restored: number;
  rejected: number;
  finalized: number;
  sources: SourceImportSummary[];
}
