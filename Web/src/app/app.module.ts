import { APP_INITIALIZER, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { environment } from '../environments/environment';

export function loadAppConfig() {
  return () =>
    fetch('assets/variable.json')
      .then((res) => res.json())
      .then((config) => {
        const host = window.location?.hostname || 'localhost';
        environment.apiBaseUrl = config.apiBaseUrl || `http://${host}:5000`;
        environment.signalrHubUrl = config.signalrHubUrl || `http://${host}:5000/hubs/chat`;
      })
      .catch(() => {
        const host = window.location?.hostname || 'localhost';
        environment.apiBaseUrl = `http://${host}:5000`;
        environment.signalrHubUrl = `http://${host}:5000/hubs/chat`;
      });
}

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule
  ],
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: loadAppConfig,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
