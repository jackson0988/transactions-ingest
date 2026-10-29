import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toast-container position-fixed bottom-0 end-0 p-3">
      @for (t of notify.toasts(); track t.id) {
        <div class="toast show align-items-center border-0" [class]="'text-bg-' + t.kind" role="alert" aria-live="assertive">
          <div class="d-flex">
            <div class="toast-body">{{ t.message }}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" aria-label="Close" (click)="notify.dismiss(t.id)"></button>
          </div>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainer {
  protected readonly notify = inject(NotificationService);
}
