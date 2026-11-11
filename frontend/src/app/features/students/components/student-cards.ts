import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { EnglishLevelLabel } from '../../../shared/labels/english-level-label';
import { StudentResponse } from '../student.model';

@Component({
  selector: 'lo-student-cards',
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink, EnglishLevelLabel],
  template: `
    @for (student of students(); track student.id) {
      <mat-card appearance="outlined">
        <mat-card-header>
          <mat-card-title>
            <a [routerLink]="['/students', student.id]">{{ student.name }}</a>
          </mat-card-title>
          <mat-card-subtitle>
            {{ student.englishLevel | englishLevelLabel }}
            @if (student.email) {
              · {{ student.email }}
            }
          </mat-card-subtitle>
        </mat-card-header>
        <mat-card-actions align="end">
          <a mat-button [routerLink]="['/students', student.id, 'edit']">Edit</a>
          <button
            mat-button
            class="delete-student"
            type="button"
            [disabled]="saving()"
            (click)="remove.emit(student)"
          >
            Delete
          </button>
        </mat-card-actions>
      </mat-card>
    }
  `,
  styleUrl: '../../../shared/layout/table-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentCards {
  readonly students = input.required<StudentResponse[]>();
  readonly saving = input(false);
  readonly remove = output<StudentResponse>();
}
