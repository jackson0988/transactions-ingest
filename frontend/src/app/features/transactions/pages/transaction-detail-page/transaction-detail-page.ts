import { ChangeDetectionStrategy, Component, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of, switchMap, tap } from 'rxjs';

import { AuditHistory } from '../../components/audit-history/audit-history';
import { TransactionSummary } from '../../components/transaction-summary/transaction-summary';
import { TransactionsApiService } from '../../data-access/transactions-api.service';
import { AuditEntry } from '../../models/audit-entry.model';
import { Transaction } from '../../models/transaction.model';

@Component({
  selector: 'app-transaction-detail-page',
  imports: [RouterLink, TransactionSummary, AuditHistory],
  templateUrl: './transaction-detail-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionDetailPage {
  private readonly transactionsApi = inject(TransactionsApiService);

  // bound from the :id route param (withComponentInputBinding)
  readonly id = input.required({ transform: numberAttribute });

  protected readonly transaction = signal<Transaction | null>(null);
  protected readonly audit = signal<AuditEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  constructor() {
    toObservable(this.id)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.notFound.set(false);
        }),
        switchMap((id) =>
          forkJoin({
            tx: this.transactionsApi.getById(id),
            audit: this.transactionsApi.getAuditHistory(id),
          }).pipe(catchError(() => of(null))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((res) => {
        this.loading.set(false);
        if (!res) {
          this.notFound.set(true);
          return;
        }
        this.transaction.set(res.tx);
        this.audit.set(res.audit);
      });
  }
}
