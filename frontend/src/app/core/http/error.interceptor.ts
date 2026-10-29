import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { NotificationService } from '../services/notification.service';

// backend returns RFC 7807 ProblemDetails, try to show the title/detail if we got one
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      notify.error(toMessage(err));
      return throwError(() => err);
    }),
  );
};

function toMessage(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return 'Cannot reach the server. Is the API running?';
  }
  const problem = err.error;
  if (problem && typeof problem === 'object') {
    return problem.detail ?? problem.title ?? `Request failed (${err.status})`;
  }
  if (err.status === 404) return 'Not found.';
  return `Request failed (${err.status} ${err.statusText})`;
}
