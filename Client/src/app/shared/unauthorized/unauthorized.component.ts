import { Component } from '@angular/core';

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-unauthorized',
  standalone: true,
  template: `<h2>Unauthorized</h2><p>You do not have access to this area.</p>`
})
export class UnauthorizedComponent {}
