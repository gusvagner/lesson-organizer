import { Routes } from '@angular/router';

export const STUDENTS_ROUTES: Routes = [
  {
    path: '',
    title: 'Students · Lesson Organizer',
    loadComponent: () => import('./student-list-page').then((m) => m.StudentListPage),
  },
  {
    path: 'new',
    title: 'New student · Lesson Organizer',
    loadComponent: () => import('./student-form-page').then((m) => m.StudentFormPage),
  },
  {
    path: ':id',
    title: 'Student · Lesson Organizer',
    loadComponent: () => import('./student-details-page').then((m) => m.StudentDetailsPage),
  },
  {
    path: ':id/edit',
    title: 'Edit student · Lesson Organizer',
    loadComponent: () => import('./student-form-page').then((m) => m.StudentFormPage),
  },
];
