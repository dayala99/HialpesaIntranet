import { provideAnimations } from '@angular/platform-browser/animations';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';

@NgModule({ declarations: [AppComponent, HomeComponent], imports: [MatIconModule, BrowserModule, AppRoutingModule], providers: [provideHttpClient(), provideAnimations()], bootstrap: [AppComponent] })
export class AppModule {}
