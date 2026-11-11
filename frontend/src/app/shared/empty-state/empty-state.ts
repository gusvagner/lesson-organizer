import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'lo-empty-state',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <mat-icon class="icon" aria-hidden="true">{{ icon() }}</mat-icon>
    <p>{{ message() }}</p>
    @if (actionLabel(); as label) {
      <button mat-stroked-button type="button" (click)="action.emit()">{{ label }}</button>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding: 48px 16px;
      text-align: center;
      color: var(--mat-sys-on-surface-variant);
    }
    .icon {
      width: 48px;
      height: 48px;
      font-size: 48px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyState {
  readonly icon = input.required<string>();
  readonly message = input.required<string>();
  readonly actionLabel = input<string | null>(null);
  readonly action = output<void>();
}
