import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { QrSignatureService, QrSessionInfo } from '../../core/services/qr-signature.service';

@Component({
  selector: 'app-sign-mobile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sign-mobile.component.html',
  styleUrl: './sign-mobile.component.scss'
})
export class SignMobileComponent implements OnInit {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  token = '';
  sessionInfo: QrSessionInfo | null = null;
  loading = true;
  erreur = '';
  submitted = false;
  submitting = false;

  private ctx: CanvasRenderingContext2D | null = null;
  private drawing = false;
  private lastX = 0;
  private lastY = 0;

  constructor(
    private route: ActivatedRoute,
    private qrService: QrSignatureService
  ) {}

  ngOnInit() {
    this.token = this.route.snapshot.params['token'];
    this.qrService.getSessionInfo(this.token).subscribe({
      next: info => { this.sessionInfo = info; this.loading = false; setTimeout(() => this.initCanvas(), 100); },
      error: err => { this.erreur = err.error?.message ?? 'Session invalide ou expirée.'; this.loading = false; }
    });
  }

  initCanvas() {
    if (!this.canvasRef) return;
    const c = this.canvasRef.nativeElement;
    c.width  = c.offsetWidth;
    c.height = c.offsetHeight;
    this.ctx = c.getContext('2d');
    if (!this.ctx) return;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, c.width, c.height);
    this.ctx.strokeStyle = '#1a1a3e';
    this.ctx.lineWidth = 3;
    this.ctx.lineCap = 'round';
    this.ctx.lineJoin = 'round';
  }

  onTouchStart(e: TouchEvent) {
    e.preventDefault();
    const t = e.touches[0];
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.drawing = true;
    this.lastX = t.clientX - r.left;
    this.lastY = t.clientY - r.top;
  }

  onTouchMove(e: TouchEvent) {
    e.preventDefault();
    if (!this.drawing || !this.ctx) return;
    const t = e.touches[0];
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    const x = t.clientX - r.left, y = t.clientY - r.top;
    this.ctx.beginPath();
    this.ctx.moveTo(this.lastX, this.lastY);
    this.ctx.lineTo(x, y);
    this.ctx.stroke();
    this.lastX = x; this.lastY = y;
  }

  onTouchEnd() { this.drawing = false; }

  onMouseDown(e: MouseEvent) {
    this.drawing = true;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.lastX = e.clientX - r.left; this.lastY = e.clientY - r.top;
  }

  onMouseMove(e: MouseEvent) {
    if (!this.drawing || !this.ctx) return;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    const x = e.clientX - r.left, y = e.clientY - r.top;
    this.ctx.beginPath(); this.ctx.moveTo(this.lastX, this.lastY);
    this.ctx.lineTo(x, y); this.ctx.stroke();
    this.lastX = x; this.lastY = y;
  }

  onMouseUp() { this.drawing = false; }

  effacer() {
    if (!this.ctx || !this.canvasRef) return;
    const c = this.canvasRef.nativeElement;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, c.width, c.height);
  }

  envoyer() {
    if (!this.canvasRef) return;
    const imageBase64 = this.canvasRef.nativeElement.toDataURL('image/png');
    this.submitting = true;
    this.qrService.submitSignature(this.token, imageBase64).subscribe({
      next: () => { this.submitted = true; this.submitting = false; },
      error: err => { this.erreur = err.error?.message ?? 'Erreur lors de l\'envoi.'; this.submitting = false; }
    });
  }
}
