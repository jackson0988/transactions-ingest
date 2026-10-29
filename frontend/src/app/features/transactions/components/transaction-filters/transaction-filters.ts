import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import {
  EMPTY_TRANSACTION_FILTER,
  SOURCE_SYSTEMS,
  TRANSACTION_STATUSES,
  TransactionFilter,
} from '../../models/transaction.model';

@Component({
  selector: 'app-transaction-filters',
  templateUrl: './transaction-filters.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionFilters {
  readonly value = input.required<TransactionFilter>();
  readonly valueChange = output<TransactionFilter>();

  protected readonly sourceSystems = SOURCE_SYSTEMS;
  protected readonly statuses = TRANSACTION_STATUSES;

  protected update(patch: Partial<TransactionFilter>) {
    this.valueChange.emit({ ...this.value(), ...patch });
  }

  protected clear() {
    this.valueChange.emit(EMPTY_TRANSACTION_FILTER);
  }
}
