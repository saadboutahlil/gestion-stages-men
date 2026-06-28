import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const token = authService.getToken();
  
  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  // Check roles if required by route
  const requiredRoles = route.data['roles'] as Array<string>;
  if (requiredRoles && requiredRoles.length > 0) {
    // Wait for user to load if not already loaded
    if(authService.isLoading()) {
      // In a real app we might return a promise or observable here
      // For simplicity, if we have a token but no user yet, let's allow navigation and catch unauthorized APIs
      return true; 
    }
    
    const user = authService.currentUser();
    if (user && !requiredRoles.includes(user.role)) {
      router.navigate(['/unauthorized']); // Or redirect to their specific dashboard
      return false;
    }
  }

  return true;
};
