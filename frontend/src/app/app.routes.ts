import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';
import { guestGuard } from './core/auth/guest-guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Log in · Lesson Organizer',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create an account · Lesson Organizer',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'lesson-plans' },
      {
        path: 'lesson-plans',
        loadChildren: () =>
          import('./features/lesson-plans/lesson-plans.routes').then((m) => m.LESSON_PLANS_ROUTES),
      },
      {
        path: 'students',
        loadChildren: () =>
          import('./features/students/students.routes').then((m) => m.STUDENTS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: 'lesson-plans' },
];
