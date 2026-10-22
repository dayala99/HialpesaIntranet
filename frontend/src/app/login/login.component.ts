import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { SessionService } from '../core/session.service';
import { apiError } from '../core/avios-api.service';
@Component({selector:'app-login',templateUrl:'./login.component.html',styleUrl:'./login.component.scss'})
export class LoginComponent {
 private session=inject(SessionService);private router=inject(Router);private route=inject(ActivatedRoute);
 username='';password='';busy=false;error='';
 submit(){
  if(this.busy||!this.username.trim()||!this.password)return;this.busy=true;this.error='';
  this.session.login(this.username.trim(),this.password).pipe(finalize(()=>{this.busy=false;this.password='';})).subscribe({next:()=>{
   const target=this.route.snapshot.queryParamMap.get('returnUrl')||'/';
   void this.router.navigateByUrl(target.startsWith('/')&&!target.startsWith('//')&&!target.includes('\\')&&!target.startsWith('/login')?target:'/');
  },error:e=>this.error=apiError(e)});
 }
}

