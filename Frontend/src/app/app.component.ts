import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';
import { SignalRService } from './core/services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class AppComponent implements OnInit {
  constructor(
    private theme: ThemeService,
    private signalR: SignalRService
  ) {}
  
  ngOnInit() {
    this.theme.init();
    this.signalR.startConnection();
  }
}
