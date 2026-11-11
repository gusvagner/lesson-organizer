import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LessonPlansStore } from '../../features/lesson-plans/lesson-plans-store';
import { StudentsStore } from '../../features/students/students-store';
import { AuthStore } from '../auth/auth-store';
import { AppLogo } from './app-logo';
import { Theme } from './theme';
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
    AppLogo,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  protected readonly auth = inject(AuthStore);
  protected readonly viewport = inject(Viewport);
  protected readonly theme = inject(Theme);
  private readonly students = inject(StudentsStore);
  private readonly lessonPlans = inject(LessonPlansStore);
  protected readonly loading = computed(
    () => this.students.loading() || this.lessonPlans.loading(),
  );
  protected readonly initial = computed(
    () => this.auth.currentUser()?.name.trim().charAt(0).toUpperCase() ?? '',
  );

  protected logout(): Promise<void> {
    return this.auth.logout();
  }
}
