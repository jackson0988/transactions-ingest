export type AuditChangeType = 'Created' | 'Updated' | 'Revoked' | 'Restored' | 'Finalized';

export interface AuditEntry {
  id: number;
  transactionRecordId: number;
  importBatchId: string | null;
  changeType: AuditChangeType;
  // null for Created, those are whole-row events
  fieldName: string | null;
  oldValue: string | null;
  newValue: string | null;
  changedAtUtc: string;
}
