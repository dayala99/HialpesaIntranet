import { ShellComponent } from './layout/shell.component';
import { provideAnimations } from '@angular/platform-browser/animations';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { LoginComponent } from './features/auth/pages/login/login.component';
import { sessionInterceptor } from './core/auth/session.interceptor';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './features/home/pages/home/home.component';
@NgModule({ declarations: [AppComponent, ShellComponent, HomeComponent, LoginComponent], imports: [FormsModule, MatIconModule, BrowserModule, AppRoutingModule], providers: [provideHttpClient(withInterceptors([sessionInterceptor])), provideAnimations()], bootstrap: [AppComponent] })
export class AppModule {
}
