import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { SortDirection } from '@shared/models/paged-result.model';
import { Transaction, TransactionSortField } from '../../models/transaction.model';
import { TransactionStatusBadge } from '../transaction-status-badge/transaction-status-badge';

interface Column {
  field: TransactionSortField;
  label: string;
  css?: string;
}

@Component({
  selector: 'app-transaction-table',
  imports: [RouterLink, DatePipe, CurrencyPipe, TransactionStatusBadge],
  templateUrl: './transaction-table.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionTable {
  readonly items = input.required<Transaction[]>();
  readonly loading = input(false);
  readonly loadFailed = input(false);
  readonly sortBy = input.required<TransactionSortField>();
  readonly sortDir = input.required<SortDirection>();

  readonly sortChange = output<TransactionSortField>();
  readonly retry = output<void>();

  protected readonly columns: Column[] = [
    { field: 'sourceSystem', label: 'Source System' },
    { field: 'transactionId', label: 'Txn ID' },
    { field: 'locationCode', label: 'Location' },
    { field: 'productName', label: 'Product' },
    { field: 'amount', label: 'Amount', css: 'text-end' },
    { field: 'transactionTime', label: 'Transaction Time (UTC)' },
    { field: 'status', label: 'Status' },
  ];

  protected sortIcon(field: TransactionSortField): string {
    if (this.sortBy() !== field) return 'bi-arrow-down-up text-body-tertiary';
    return this.sortDir() === 'asc' ? 'bi-sort-up' : 'bi-sort-down';
  }
}
