import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { Feedback } from '../../core/feedback/feedback';
import { Viewport } from '../../core/layout/viewport';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { StudentCards } from './components/student-cards';
import { StudentTable } from './components/student-table';
import { StudentActions } from './student-actions';
import { StudentResponse } from './student.model';
import { StudentsStore } from './students-store';

const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'lo-student-list-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    EmptyState,
    StudentCards,
    StudentTable,
  ],
  templateUrl: './student-list-page.html',
  styleUrl: '../../shared/layout/page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentListPage {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly feedback = inject(Feedback);
  private readonly actions = inject(StudentActions);
  protected readonly store = inject(StudentsStore);
  protected readonly viewport = inject(Viewport);

  readonly search = input<string | undefined>();

  protected readonly searchTerm = computed(() => (this.search() ?? '').trim());
  protected readonly loaded = signal(false);
  protected readonly searchBox = new FormControl('', { nonNullable: true });

  constructor() {
    effect(() => {
      const term = this.searchTerm();
      untracked(() => {
        if (term !== this.searchBox.value.trim()) {
          this.searchBox.setValue(term, { emitEvent: false });
        }
        void this.load();
      });
    });
    this.searchBox.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe((value) => this.applySearch(value));
  }

  protected clearSearch(): void {
    this.searchBox.setValue('', { emitEvent: false });
    this.applySearch('');
  }

  protected addStudent(): void {
    void this.router.navigate(['/students/new']);
  }

  protected async remove(student: StudentResponse): Promise<void> {
    if (await this.actions.remove(student)) {
      await this.load();
    }
  }

  private applySearch(value: string): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { search: value.trim() || null },
      replaceUrl: true,
    });
  }

  private async load(): Promise<void> {
    const outcome = await this.store.load(this.searchTerm());
    if (!outcome.ok) {
      this.feedback.failure(outcome.problem);
    }
    this.loaded.set(true);
  }
}
