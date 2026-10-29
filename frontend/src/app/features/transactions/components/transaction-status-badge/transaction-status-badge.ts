import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { TransactionStatus } from '../../models/transaction.model';

const STATUS_CLASS: Record<TransactionStatus, string> = {
  Active: 'text-bg-success',
  Revoked: 'text-bg-danger',
  Finalized: 'text-bg-secondary',
};

@Component({
  selector: 'app-transaction-status-badge',
  template: `<span class="badge rounded-pill" [class]="cssClass()">{{ status() }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionStatusBadge {
  readonly status = input.required<TransactionStatus>();
  protected readonly cssClass = computed(() => STATUS_CLASS[this.status()] ?? 'text-bg-light');
}
