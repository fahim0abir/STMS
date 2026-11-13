import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ButtonComponent } from '../ui/button.component';
import { BadgeComponent } from '../ui/badge.component';
import { AuthService } from '../../core/auth/auth.service';

import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ButtonComponent, BadgeComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  private readonly authService = inject(AuthService);

  readonly sidebarOpen = signal(false);
  readonly currentUser = toSignal(this.authService.currentUser$, { initialValue: this.authService.currentUserValue });

  readonly navLinks = [
    { path: '/dashboard', label: 'Dashboard', roles: ['Admin', 'ProjectManager', 'TeamMember'] },
    { path: '/projects', label: 'Projects', roles: ['Admin', 'ProjectManager'] },
    { path: '/tasks', label: 'Tasks', roles: ['Admin', 'ProjectManager', 'TeamMember'] },
  ];

  toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }

  logout(): void {
    this.sidebarOpen.set(false);
    this.authService.logout();
  }

  isLinkVisible(roles: string[]): boolean {
    const user = this.currentUser();
    return !!user && roles.includes(user.role);
  }
}

