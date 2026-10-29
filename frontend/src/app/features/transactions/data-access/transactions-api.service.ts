import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@env/environment';
import { PagedResult } from '@shared/models/paged-result.model';
import { toHttpParams } from '@shared/utils/http-params';
import { AuditEntry } from '../models/audit-entry.model';
import { Transaction, TransactionQuery } from '../models/transaction.model';

@Injectable({ providedIn: 'root' })
export class TransactionsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/transactions`;

  getTransactions(query: TransactionQuery): Observable<PagedResult<Transaction>> {
    return this.http.get<PagedResult<Transaction>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getById(id: number): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.baseUrl}/${id}`);
  }

  getAuditHistory(id: number): Observable<AuditEntry[]> {
    return this.http.get<AuditEntry[]>(`${this.baseUrl}/${id}/audit`);
  }
}
