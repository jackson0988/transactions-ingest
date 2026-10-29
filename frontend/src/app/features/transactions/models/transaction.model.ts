import { SortDirection } from '@shared/models/paged-result.model';

export type TransactionStatus = 'Active' | 'Revoked' | 'Finalized';

export const TRANSACTION_STATUSES: readonly TransactionStatus[] = ['Active', 'Revoked', 'Finalized'];

export const SOURCE_SYSTEMS = ['Store Uptown', 'Store Downtown', 'Online'] as const;
export type SourceSystem = (typeof SOURCE_SYSTEMS)[number];

export interface Transaction {
  id: number;
  sourceSystem: SourceSystem;
  transactionId: number;
  cardNumber: string | null;
  locationCode: string;
  productName: string;
  amount: number;
  transactionTime: string; // ISO, UTC
  status: TransactionStatus;
  revisionNumber: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export type TransactionSortField =
  | 'sourceSystem'
  | 'transactionId'
  | 'locationCode'
  | 'productName'
  | 'amount'
  | 'transactionTime'
  | 'status';

export interface TransactionFilter {
  search: string;
  sourceSystem: SourceSystem | '';
  status: TransactionStatus | '';
}

export const EMPTY_TRANSACTION_FILTER: TransactionFilter = { search: '', sourceSystem: '', status: '' };

export interface TransactionQuery extends Partial<TransactionFilter> {
  page: number;
  pageSize: number;
  sortBy?: TransactionSortField;
  sortDir?: SortDirection;
}
