import { Routes } from '@angular/router';

export const STUDENTS_ROUTES: Routes = [
  {
    path: '',
    title: 'Students · Lesson Organizer',
    loadComponent: () => import('./student-list-page').then((m) => m.StudentListPage),
  },
];
