import { Routes } from '@angular/router';

export const LESSON_PLANS_ROUTES: Routes = [
  {
    path: '',
    title: 'Lesson plans · Lesson Organizer',
    loadComponent: () => import('./lesson-plan-list-page').then((m) => m.LessonPlanListPage),
  },
];
