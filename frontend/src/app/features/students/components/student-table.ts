import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { EnglishLevelLabel } from '../../../shared/labels/english-level-label';
import { StudentResponse } from '../student.model';

@Component({
  selector: 'lo-student-table',
  imports: [MatButtonModule, MatIconModule, MatTableModule, RouterLink, EnglishLevelLabel],
  template: `
    <div class="table-card">
      <table mat-table [dataSource]="students()">
        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let student">
            <a [routerLink]="['/students', student.id]">{{ student.name }}</a>
          </td>
        </ng-container>
        <ng-container matColumnDef="email">
          <th mat-header-cell *matHeaderCellDef>Email</th>
          <td mat-cell *matCellDef="let student">{{ student.email ?? '—' }}</td>
        </ng-container>
        <ng-container matColumnDef="level">
          <th mat-header-cell *matHeaderCellDef>English level</th>
          <td mat-cell *matCellDef="let student">{{ student.englishLevel | englishLevelLabel }}</td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef><span class="visually-hidden">Actions</span></th>
          <td mat-cell *matCellDef="let student" class="actions">
            <a
              mat-icon-button
              [routerLink]="['/students', student.id, 'edit']"
              [attr.aria-label]="'Edit ' + student.name"
            >
              <mat-icon>edit</mat-icon>
            </a>
            <button
              mat-icon-button
              class="delete-student"
              type="button"
              [disabled]="saving()"
              [attr.aria-label]="'Delete ' + student.name"
              (click)="remove.emit(student)"
            >
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>
    </div>
  `,
  styleUrl: '../../../shared/layout/table-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentTable {
  readonly students = input.required<StudentResponse[]>();
  readonly saving = input(false);
  readonly remove = output<StudentResponse>();
  protected readonly columns = ['name', 'email', 'level', 'actions'];
}
