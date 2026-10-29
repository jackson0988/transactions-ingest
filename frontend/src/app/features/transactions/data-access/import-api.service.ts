import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, finalize } from 'rxjs';

import { environment } from '@env/environment';
import { ImportResult } from '../models/import-result.model';

@Injectable({ providedIn: 'root' })
export class ImportApiService {
  private readonly http = inject(HttpClient);

  private readonly _running = signal(false);
  readonly running = this._running.asReadonly();

  runImport(): Observable<ImportResult> {
    this._running.set(true);
    return this.http
      .post<ImportResult>(`${environment.apiBaseUrl}/import`, {})
      .pipe(finalize(() => this._running.set(false)));
  }
}
