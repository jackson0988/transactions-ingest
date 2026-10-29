import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, combineLatest, debounceTime, of, switchMap, tap } from 'rxjs';

import { NotificationService } from '@core/services/notification.service';
import { environment } from '@env/environment';
import { Pagination } from '@shared/components/pagination/pagination';
import { SortDirection } from '@shared/models/paged-result.model';
import { TransactionFilters } from '../../components/transaction-filters/transaction-filters';
import { TransactionTable } from '../../components/transaction-table/transaction-table';
import { ImportApiService } from '../../data-access/import-api.service';
import { TransactionsApiService } from '../../data-access/transactions-api.service';
import {
  EMPTY_TRANSACTION_FILTER,
  Transaction,
  TransactionFilter,
  TransactionQuery,
  TransactionSortField,
} from '../../models/transaction.model';

@Component({
  selector: 'app-transaction-list-page',
  imports: [TransactionFilters, TransactionTable, Pagination],
  templateUrl: './transaction-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionListPage {
  private readonly transactionsApi = inject(TransactionsApiService);
  private readonly notify = inject(NotificationService);
  protected readonly importApi = inject(ImportApiService);

  // TODO: sync filter/paging with query params so a refresh keeps them
  protected readonly filter = signal<TransactionFilter>(EMPTY_TRANSACTION_FILTER);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(environment.defaultPageSize);
  protected readonly sortBy = signal<TransactionSortField>('transactionTime');
  protected readonly sortDir = signal<SortDirection>('desc');

  protected readonly items = signal<Transaction[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly loadFailed = signal(false);

  private readonly query = computed<TransactionQuery>(() => ({
    ...this.filter(),
    search: this.filter().search.trim(),
    page: this.page(),
    pageSize: this.pageSize(),
    sortBy: this.sortBy(),
    sortDir: this.sortDir(),
  }));

  private readonly reload$ = new BehaviorSubject<void>(undefined);

  constructor() {
    combineLatest([toObservable(this.query), this.reload$])
      .pipe(
        debounceTime(250),
        tap(() => {
          this.loading.set(true);
          this.loadFailed.set(false);
        }),
        switchMap(([q]) =>
          this.transactionsApi.getTransactions(q).pipe(
            catchError(() => {
              this.loadFailed.set(true);
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((res) => {
        this.loading.set(false);
        if (!res) return;
        this.items.set(res.items);
        this.totalCount.set(res.totalCount);
      });
  }

  protected onFilterChange(filter: TransactionFilter) {
    this.filter.set(filter);
    this.page.set(1);
  }

  protected onSort(field: TransactionSortField) {
    if (this.sortBy() === field) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDir.set('asc');
    }
  }

  protected onPageSizeChange(size: number) {
    this.pageSize.set(size);
    this.page.set(1);
  }

  protected refresh() {
    this.reload$.next();
  }

  protected runImport() {
    this.importApi.runImport().subscribe({
      next: (r) => {
        this.notify.success(
          `Import finished: ${r.inserted} new, ${r.updated} updated, ${r.revoked} revoked, ${r.unchanged} unchanged.`,
        );
        const failed = r.sources.filter((s) => s.error);
        if (failed.length) {
          this.notify.error(`Some sources failed: ${failed.map((s) => s.sourceSystem).join(', ')}`);
        }
        this.refresh();
      },
      // interceptor already shows the toast
      error: () => {},
    });
  }
}
