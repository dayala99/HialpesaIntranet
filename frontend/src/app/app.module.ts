import { provideAnimations } from '@angular/platform-browser/animations';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { LoginComponent } from './login/login.component';
import { sessionInterceptor } from './core/session.interceptor';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';

@NgModule({ declarations: [AppComponent, HomeComponent, LoginComponent], imports: [FormsModule, MatIconModule, BrowserModule, AppRoutingModule], providers: [provideHttpClient(withInterceptors([sessionInterceptor])), provideAnimations()], bootstrap: [AppComponent] })
export class AppModule {}
