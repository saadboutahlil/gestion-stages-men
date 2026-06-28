import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  roles: string[];
  category?: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent {
  constructor(private authService: AuthService) {}

  userRole = computed(() => this.authService.currentUser()?.role || '');

  menuItems: MenuItem[] = [
    // ── GENERAL ──
    { label: 'Tableau de bord', icon: 'fa-solid fa-gauge-high', route: '/dashboard', roles: ['Admin', 'Student', 'MinistereRH', 'Encadrant', 'School'] },
    { label: 'Assistant IA', icon: 'fa-solid fa-robot', route: '/assistant-faq', roles: ['Admin', 'Student', 'MinistereRH', 'Encadrant', 'School'] },

    // ── STUDENT ──
    { label: 'Offres de stage', icon: 'fa-solid fa-briefcase', route: '/offers', roles: ['Student'] },
    { label: 'Mes candidatures', icon: 'fa-solid fa-paper-plane', route: '/student/applications', roles: ['Student'] },
    { label: 'Ma convention', icon: 'fa-solid fa-file-contract', route: '/agreements', roles: ['Student'] },
    { label: 'Mon stage', icon: 'fa-solid fa-graduation-cap', route: '/student/internship', roles: ['Student'] },

    // ── MINISTERE RH ──
    { label: 'Offres de stage', icon: 'fa-solid fa-briefcase', route: '/offers', roles: ['MinistereRH'], category: 'RECRUTEMENT' },
    { label: 'Candidatures reçues', icon: 'fa-solid fa-user-graduate', route: '/ministere/applications', roles: ['MinistereRH'], category: 'RECRUTEMENT' },
    { label: 'Conventions', icon: 'fa-solid fa-file-signature', route: '/agreements', roles: ['MinistereRH'], category: 'CONVENTIONS & SUIVI' },
    { label: 'Validation des rapports', icon: 'fa-solid fa-clipboard-check', route: '/ministere/reports-validation', roles: ['MinistereRH'], category: 'CONVENTIONS & SUIVI' },
    { label: 'Stagiaires actifs', icon: 'fa-solid fa-users-viewfinder', route: '/ministere/internships', roles: ['MinistereRH'], category: 'RESSOURCES & ARCHIVES' },
    { label: 'Liste des encadrants', icon: 'fa-solid fa-user-tie', route: '/ministere/supervisors', roles: ['MinistereRH'], category: 'RESSOURCES & ARCHIVES' },
    { label: 'Historique des stages', icon: 'fa-solid fa-box-archive', route: '/admin/archives', roles: ['MinistereRH'], category: 'RESSOURCES & ARCHIVES' },

    // ── ENCADRANT ──
    { label: 'Mes stagiaires', icon: 'fa-solid fa-users', route: '/encadrant/interns', roles: ['Encadrant'] },

    // ── SCHOOL ──
    { label: 'Gestion des conventions', icon: 'fa-solid fa-file-pen', route: '/school/agreements', roles: ['School'] },
    { label: "Stages de l'école", icon: 'fa-solid fa-eye', route: '/school/internships', roles: ['School'] },

    // ── ADMIN ──
    { label: 'Gestion des utilisateurs', icon: 'fa-solid fa-users-gear', route: '/admin/users', roles: ['Admin'], category: 'GESTION PLATEFORME' },
    { label: 'Historique global', icon: 'fa-solid fa-box-archive', route: '/admin/archives', roles: ['Admin'], category: 'GESTION PLATEFORME' },
    { label: 'Statistiques plateforme', icon: 'fa-solid fa-chart-column', route: '/admin/statistics', roles: ['Admin'], category: 'SUPERVISION TECHNIQUE' },
    { label: 'Logs système', icon: 'fa-solid fa-terminal', route: '/admin/logs', roles: ['Admin'], category: 'SUPERVISION TECHNIQUE' },

    // ── PROFILE ──
    { label: 'Mon profil', icon: 'fa-solid fa-user-circle', route: '/profile', roles: ['Admin', 'Student', 'MinistereRH', 'Encadrant', 'School'] }
  ];

  filteredMenu = computed(() => {
    const role = this.userRole();
    return this.menuItems.filter(item => item.roles.includes(role));
  });
}
