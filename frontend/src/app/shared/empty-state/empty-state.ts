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
      gap: var(--lo-space-3);
      padding: var(--lo-space-12) var(--lo-space-6);
      border: 1px dashed var(--lo-border);
      border-radius: var(--lo-radius-lg);
      background-color: var(--lo-surface);
      color: var(--lo-text-muted);
      text-align: center;
    }
    .icon {
      width: 56px;
      height: 56px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background-color: var(--lo-primary-soft);
      color: var(--lo-primary);
      font-size: 28px;
    }
    p {
      margin: 0;
      max-width: 42ch;
      color: var(--lo-text);
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
