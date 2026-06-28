import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { LayoutComponent } from './layout/layout.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { 
    path: 'register', 
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) 
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { 
        path: 'dashboard', 
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'search',
        loadComponent: () => import('./features/search/search.component').then(m => m.SearchComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent)
      },
      {
        path: 'assistant-faq',
        loadComponent: () => import('./features/chatbot/chatbot.component').then(m => m.ChatbotComponent)
      },

      // ── OFFRES ──
      {
        path: 'offers',
        loadComponent: () => import('./features/offers/offers-list/offers-list.component').then(m => m.OffersListComponent)
      },
      {
        path: 'offres/nouvelle',
        loadComponent: () => import('./features/offers/offer-form/offer-form.component').then(m => m.OfferFormComponent),
        data: { roles: ['MinistereRH'] }
      },

      // ── STUDENT ──
      {
        path: 'student/applications',
        loadComponent: () => import('./features/applications/applications-list/applications-list.component').then(m => m.ApplicationsListComponent),
        data: { roles: ['Student'] }
      },
      {
        path: 'student/internship',
        loadComponent: () => import('./features/student/internship/student-internship.component').then(m => m.StudentInternshipComponent),
        data: { roles: ['Student'] }
      },

      // ── MINISTERE RH ──
      {
        path: 'ministere/applications',
        loadComponent: () => import('./features/applications/applications-list/applications-list.component').then(m => m.ApplicationsListComponent),
        data: { roles: ['MinistereRH', 'Admin'] }
      },
      {
        path: 'agreements',
        loadComponent: () => import('./features/agreements/agreements-list/agreements-list.component').then(m => m.AgreementsListComponent),
        data: { roles: ['MinistereRH', 'Admin', 'Student', 'School'] }
      },
      {
        path: 'ministere/internships',
        loadComponent: () => import('./features/internships/internships-list/internships-list.component').then(m => m.InternshipsListComponent),
        data: { roles: ['MinistereRH', 'Admin'] }
      },
      {
        path: 'ministere/supervisors',
        loadComponent: () => import('./features/supervisors/supervisors-list.component').then(m => m.SupervisorsListComponent),
        data: { roles: ['MinistereRH', 'Admin'] }
      },
      {
        path: 'ministere/reports-validation',
        loadComponent: () => import('./features/reports/reports-validation/reports-validation.component').then(m => m.ReportsValidationComponent),
        data: { roles: ['MinistereRH', 'Admin'] }
      },

      // ── ENCADRANT ──
      {
        path: 'encadrant/interns',
        loadComponent: () => import('./features/internships/internships-list/internships-list.component').then(m => m.InternshipsListComponent),
        data: { roles: ['Encadrant'] }
      },

      // ── SCHOOL ──
      {
        path: 'school/agreements',
        loadComponent: () => import('./features/agreements/agreements-list/agreements-list.component').then(m => m.AgreementsListComponent),
        data: { roles: ['School'] }
      },
      {
        path: 'school/internships',
        loadComponent: () => import('./features/internships/internships-list/internships-list.component').then(m => m.InternshipsListComponent),
        data: { roles: ['School'] }
      },

      // ── ADMIN ──
      {
        path: 'admin/archives',
        loadComponent: () => import('./features/admin/archives/admin-archives.component').then(m => m.AdminArchivesComponent),
        data: { roles: ['Admin', 'MinistereRH'] }
      },
      {
        path: 'admin/users',
        loadComponent: () => import('./features/admin/users/admin-users.component').then(m => m.AdminUsersComponent),
        data: { roles: ['Admin'] }
      },
      {
        path: 'admin/statistics',
        loadComponent: () => import('./features/admin/statistics/admin-statistics.component').then(m => m.AdminStatisticsComponent),
        data: { roles: ['Admin'] }
      },
      {
        path: 'admin/logs',
        loadComponent: () => import('./features/admin/logs/admin-logs.component').then(m => m.AdminLogsComponent),
        data: { roles: ['Admin'] }
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
