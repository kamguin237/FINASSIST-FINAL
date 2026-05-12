import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MaSignatureService } from '../../core/services/ma-signature.service';
import { QrSignatureService } from '../../core/services/qr-signature.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { SignatureUtilisateurDTO, SaveSignatureUtilisateurDTO } from '../../core/models/signature-utilisateur.models';

@Component({
  selector: 'app-ma-signature',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './ma-signature.component.html',
  styleUrl: './ma-signature.component.scss'
})
export class MaSignatureComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('qrCanvas') qrCanvasRef!: ElementRef<HTMLCanvasElement>;

  onglet: 'manuscrite' | 'typographique' | 'upload' | 'qrcode' = 'manuscrite';
  signatureExistante: SignatureUtilisateurDTO | null = null;
  saving = false;

  // QR Code
  qrSession: { token: string; urlMobile: string; expiration: string } | null = null;
  qrLoading = false;
  qrStatus: 'idle' | 'waiting' | 'done' | 'expired' = 'idle';
  private pollingInterval: any = null;

  // Manuscrite
  private ctx: CanvasRenderingContext2D | null = null;
  private drawing = false;
  private lastX = 0;
  private lastY = 0;
  private canvasInitialized = false;

  // Typographique
  texteSignature = '';
  policeChoisie = 'Dancing Script';
  polices = [
    { value: 'Dancing Script',   label: 'Dancing Script' },
    { value: 'Great Vibes',      label: 'Great Vibes' },
    { value: 'Pacifico',         label: 'Pacifico' },
    { value: 'Satisfy',          label: 'Satisfy' },
    { value: 'Caveat',           label: 'Caveat' },
    { value: 'Sacramento',       label: 'Sacramento' },
    { value: 'Pinyon Script',    label: 'Pinyon Script' },
    { value: 'Alex Brush',       label: 'Alex Brush' },
    { value: 'Allura',           label: 'Allura' },
    { value: 'Kaushan Script',   label: 'Kaushan Script' },
    { value: 'Courgette',        label: 'Courgette' },
    { value: 'Lobster',          label: 'Lobster' },
    { value: 'Yellowtail',       label: 'Yellowtail' },
    { value: 'Italianno',        label: 'Italianno' },
    { value: 'Clicker Script',   label: 'Clicker Script' },
    { value: 'Euphoria Script',  label: 'Euphoria Script' },
    { value: 'Marck Script',     label: 'Marck Script' },
    { value: 'Niconne',          label: 'Niconne' },
    { value: 'Qwigley',          label: 'Qwigley' },
    { value: 'Ruthie',           label: 'Ruthie' },
  ];

  // Upload
  uploadPreview: string | null = null;

  constructor(
    private service: MaSignatureService,
    private qrService: QrSignatureService,
    private toastr: ToastrService,
    private confirm: ConfirmService,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.service.get().subscribe({
      next: s => this.signatureExistante = s,
      error: () => this.signatureExistante = null
    });
  }

  // Initialise le canvas dès qu'il est disponible dans le DOM
  private pendingQrRender = false;

  ngAfterViewChecked() {
    if (this.onglet === 'manuscrite' && this.canvasRef && !this.canvasInitialized) {
      this.initCanvas();
    }
    // Rendre le QR dès que le canvas #qrCanvas est disponible dans le DOM
    if (this.pendingQrRender && this.qrCanvasRef) {
      this.pendingQrRender = false;
      this.renderQr();
    }
  }

  // ── Canvas manuscrite ──────────────────────────────────────────────────────
  initCanvas() {
    if (!this.canvasRef) return;
    const canvas = this.canvasRef.nativeElement;
    this.ctx = canvas.getContext('2d');
    if (!this.ctx) return;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);
    this.ctx.strokeStyle = '#1a1a3e';
    this.ctx.lineWidth = 2.5;
    this.ctx.lineCap = 'round';
    this.ctx.lineJoin = 'round';
    this.canvasInitialized = true;
  }

  onMouseDown(e: MouseEvent) {
    this.drawing = true;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.lastX = e.clientX - r.left;
    this.lastY = e.clientY - r.top;
  }

  onMouseMove(e: MouseEvent) {
    if (!this.drawing || !this.ctx) return;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    const x = e.clientX - r.left, y = e.clientY - r.top;
    this.ctx.beginPath();
    this.ctx.moveTo(this.lastX, this.lastY);
    this.ctx.lineTo(x, y);
    this.ctx.stroke();
    this.lastX = x; this.lastY = y;
  }

  onMouseUp() { this.drawing = false; }

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

  effacer() {
    if (!this.ctx || !this.canvasRef) return;
    const c = this.canvasRef.nativeElement;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, c.width, c.height);
  }

  // ── Typographique ──────────────────────────────────────────────────────────
  get apercuTypo(): string {
    if (!this.texteSignature.trim()) return '';
    const canvas = document.createElement('canvas');
    canvas.width = 500; canvas.height = 120;
    const ctx = canvas.getContext('2d')!;
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = '#1a1a3e';
    ctx.font = `52px "${this.policeChoisie}"`;
    ctx.textBaseline = 'middle';
    ctx.fillText(this.texteSignature, 20, 60);
    return canvas.toDataURL('image/png');
  }

  // ── Upload ─────────────────────────────────────────────────────────────────
  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => { this.uploadPreview = reader.result as string; };
    reader.readAsDataURL(file);
  }

  // ── Sauvegarde ─────────────────────────────────────────────────────────────
  sauvegarder() {
    let dto: SaveSignatureUtilisateurDTO | null = null;

    if (this.onglet === 'manuscrite') {
      if (!this.canvasRef) return;
      const image = this.canvasRef.nativeElement.toDataURL('image/png');
      dto = { type: 'manuscrite', imageBase64: image };
    } else if (this.onglet === 'typographique') {
      if (!this.texteSignature.trim()) { this.toastr.warning(this.translate.instant('signature.enterName')); return; }
      dto = { type: 'typographique', imageBase64: this.apercuTypo, police: this.policeChoisie };
    } else {
      if (!this.uploadPreview) { this.toastr.warning(this.translate.instant('signature.selectFile')); return; }
      dto = { type: 'upload', imageBase64: this.uploadPreview };
    }

    this.saving = true;
    this.service.save(dto).subscribe({
      next: s => {
        this.signatureExistante = s;
        this.toastr.success('Signature sauvegardée.');
        this.saving = false;
      },
      error: e => {
        this.toastr.error(e.error?.message ?? 'Erreur lors de la sauvegarde.');
        this.saving = false;
      }
    });
  }

  async supprimer() {
    const ok = await this.confirm.confirm({ titre: this.translate.instant('signature.deleteTitle'), message: this.translate.instant('signature.deleteConfirm'), labelConfirm: this.translate.instant('common.delete'), danger: true });
    if (!ok) return;
    this.service.delete().subscribe({
      next: () => { this.signatureExistante = null; this.toastr.success('Signature supprimée.'); },
      error: () => this.toastr.error('Erreur lors de la suppression.')
    });
  }

  setOnglet(o: 'manuscrite' | 'typographique' | 'upload' | 'qrcode') {
    this.onglet = o;
    if (o === 'manuscrite') {
      this.canvasInitialized = false; // forcer réinitialisation
      this.ctx = null;
    }
    if (o === 'qrcode') this.chargerQr();
  }

  // ── QR Code ────────────────────────────────────────────────────────────────
  chargerQr() {
    this.genererQr();
  }

  genererQr() {
    this.qrLoading = true;
    this.qrStatus = 'idle';
    this.stopPolling();
    this.qrService.createSession().subscribe({
      next: s => {
        this.qrSession = s;
        this.qrLoading = false;
        this.qrStatus = 'waiting';
        // Le canvas #qrCanvas n'est pas encore dans le DOM ici
        // ngAfterViewChecked va détecter quand il sera disponible et appeler renderQr
        this.pendingQrRender = true;
        this.startPolling();
      },
      error: () => { this.qrLoading = false; this.toastr.error('Erreur lors de la génération.'); }
    });
  }

  async renderQr() {
    if (!this.qrSession || !this.qrCanvasRef) return;
    try {
      const qrcodeModule = await import('qrcode');
      // Le dynamic import peut exposer les méthodes sur .default ou directement
      const toCanvas = (qrcodeModule as any).default?.toCanvas ?? (qrcodeModule as any).toCanvas;
      await toCanvas(this.qrCanvasRef.nativeElement, this.qrSession.urlMobile, {
        width: 200, margin: 2,
        color: { dark: '#000000', light: '#ffffff' }
      });
    } catch (e) { console.error('QR render error', e); }
  }

  startPolling() {
    this.pollingInterval = setInterval(() => {
      if (!this.qrSession) return;
      this.qrService.getStatus(this.qrSession.token).subscribe({
        next: s => {
          if (s.completed) {
            this.qrStatus = 'done';
            this.stopPolling();
            this.toastr.success('✅ Signature reçue depuis votre téléphone !');
            this.service.get().subscribe({ next: sig => this.signatureExistante = sig, error: () => {} });
          } else if (s.expired) {
            this.qrStatus = 'expired';
            this.stopPolling();
          }
        }
      });
    }, 3000);
  }

  stopPolling() {
    if (this.pollingInterval) { clearInterval(this.pollingInterval); this.pollingInterval = null; }
  }

  telechargerQr() {
    if (!this.qrCanvasRef) return;
    const url = this.qrCanvasRef.nativeElement.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = url; a.download = `qr-signature-finassist.png`;
    a.click();
  }

  copierLien() {
    if (!this.qrSession) return;
    navigator.clipboard.writeText(this.qrSession.urlMobile).then(() => {
      this.toastr.success('Lien copié dans le presse-papier.');
    });
  }

  // Nettoyage
  ngOnDestroy() { this.stopPolling(); }
}
