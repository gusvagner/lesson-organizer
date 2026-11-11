import { Routes } from '@angular/router';

export const LESSON_PLANS_ROUTES: Routes = [
  {
    path: '',
    title: 'Lesson plans · Lesson Organizer',
    loadComponent: () => import('./lesson-plan-list-page').then((m) => m.LessonPlanListPage),
  },
  {
    path: 'new',
    title: 'New lesson plan · Lesson Organizer',
    loadComponent: () => import('./lesson-plan-form-page').then((m) => m.LessonPlanFormPage),
  },
  {
    path: ':id/edit',
    title: 'Edit lesson plan · Lesson Organizer',
    loadComponent: () => import('./lesson-plan-form-page').then((m) => m.LessonPlanFormPage),
  },
];
