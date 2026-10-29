import { HttpParams } from '@angular/common/http';

/** Builds HttpParams, skipping empty values so the api doesn't get ?status=&search= */
export function toHttpParams(source: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(source)) {
    if (value === undefined || value === null || value === '') continue;
    params = params.set(key, String(value));
  }
  return params;
}
