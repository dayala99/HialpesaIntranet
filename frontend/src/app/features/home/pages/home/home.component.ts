import {SessionService} from '../../../../core/auth/session.service';
import { Component,inject } from '@angular/core';
@Component({ selector: 'app-home', templateUrl: './home.component.html' })
export class HomeComponent {
 readonly session=inject(SessionService);
}
