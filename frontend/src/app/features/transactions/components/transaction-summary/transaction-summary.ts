import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { Transaction } from '../../models/transaction.model';
import { TransactionStatusBadge } from '../transaction-status-badge/transaction-status-badge';

@Component({
  selector: 'app-transaction-summary',
  imports: [DatePipe, CurrencyPipe, TransactionStatusBadge],
  templateUrl: './transaction-summary.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionSummary {
  readonly transaction = input.required<Transaction>();
}
