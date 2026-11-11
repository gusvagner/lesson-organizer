import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LessonPlansStore } from '../../features/lesson-plans/lesson-plans-store';
import { StudentsStore } from '../../features/students/students-store';
import { AuthStore } from '../auth/auth-store';
import { Viewport } from './viewport';

@Component({
  selector: 'lo-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    MatToolbarModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  protected readonly auth = inject(AuthStore);
  protected readonly viewport = inject(Viewport);
  private readonly students = inject(StudentsStore);
  private readonly lessonPlans = inject(LessonPlansStore);
  protected readonly loading = computed(
    () => this.students.loading() || this.lessonPlans.loading(),
  );

  protected logout(): Promise<void> {
    return this.auth.logout();
  }
}
