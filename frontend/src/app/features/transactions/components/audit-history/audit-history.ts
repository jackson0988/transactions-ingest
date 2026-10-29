import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { AuditChangeType, AuditEntry } from '../../models/audit-entry.model';

const CHANGE_CLASS: Record<AuditChangeType, string> = {
  Created: 'text-bg-primary',
  Updated: 'text-bg-warning',
  Revoked: 'text-bg-danger',
  Restored: 'text-bg-info',
  Finalized: 'text-bg-secondary',
};

@Component({
  selector: 'app-audit-history',
  imports: [DatePipe],
  templateUrl: './audit-history.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditHistory {
  /** expected newest first */
  readonly entries = input.required<AuditEntry[]>();

  protected changeClass(type: AuditChangeType) {
    return CHANGE_CLASS[type] ?? 'text-bg-light';
  }
}
