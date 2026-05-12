import { Component, OnInit, OnDestroy, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { CustomSelectComponent } from '../../../shared/components/custom-select/custom-select.component';
import { BesoinsService } from '../../../core/services/besoins.service';
import { WorkflowService } from '../../../core/services/workflow.service';
import { SignaturesService, SignatureApercu } from '../../../core/services/signatures.service';
import { MaSignatureService } from '../../../core/services/ma-signature.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { BesoinDTO, HistoriqueDTO, DocumentDTO } from '../../../core/models/besoin.models';
import { SignatureUtilisateurDTO } from '../../../core/models/signature-utilisateur.models';

interface VerificationResult {
  signatureId: number;
  authentique: boolean;
  message: string;
  horodatage: string;
  signataireNom: string;
}

interface SignatureResult {
  id: number;
  signataireNom: string;
  horodatage: string;
  empreinte: string;
  valide: boolean;
}

@Component({
  selector: 'app-besoin-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, PdfViewerModule, CustomSelectComponent, TranslateModule],
  templateUrl: './besoin-detail.component.html',
  styleUrl: './besoin-detail.component.scss'
})
export class BesoinDetailComponent implements OnInit, OnDestroy {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('signCanvas') signCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('docZone') docZone!: ElementRef; 

  besoin: BesoinDTO | null = null;
  historique: HistoriqueDTO[] = [];
  documents: DocumentDTO[] = [];
  loading = true;
  uploading = false;

  // Source PDF pour ng2-pdf-viewer (URL blob ou Uint8Array)
  pdfSrc: string | Uint8Array | null = null;
  pdfLoading = false;
  pdfError = false;
  pdfPage = 1;
  pdfTotalPages = 0;
  get apercuPdfViewerSrc(): string | Uint8Array | undefined {
    return this.apercuPdfSrc ?? this.pdfSrc ?? undefined;
  }

  get isImage(): boolean {
    return this.documents.length > 0 && this.documents[0].type?.startsWith('image/');
  }

  get peutValider(): boolean {
  if (!this.besoin) return false;

  return !this.besoin.dejaValideParMoi &&
         (this.besoin.statut ?? '').startsWith('EN_ATTENTE_') &&
         !this.validerLoading;  // ← utiliser validerLoading qui existe déjà
 }

  // URL image pour les non-PDF
  imageUrl: string | null = null;
  pdfObjectUrl: string | null = null;

  // Modale vérification
  showVerifModal = false;
  verifResult: VerificationResult | null = null;
  verifLoading = false;
  signatureId: number | null = null;

  // Carte confirmation signature
  signatureResult: SignatureResult | null = null;

  // Modale aperçu
  showApercuModal = false;
  apercuData: SignatureApercu | null = null;
  apercuLoading = false;
  apercuDocLoading = false;
  apercuPdfSrc: string | null = null;
  apercuPdfObjectUrl: string | null = null;
  docZoom = 1;

  // Modale signature manuscrite
  showSignModal = false;
  signEtape: 1 | 2 = 1;
  signErreur = '';
  signLoading = false;
  signPos: { x: number; y: number } | null = null;
  signPosPreview: { x: number; y: number } | null = null;

  // Dimensions du canvas PDF capturées à l'étape 1 (avant switch vers étape 2)
  private pdfCanvasSnapshot: { width: number; height: number } | null = null;

  // Canvas
  private ctx: CanvasRenderingContext2D | null = null;
  private drawing = false;
  private lastX = 0;
  private lastY = 0;

  private async extractHttpErrorBody(err: any): Promise<any> {
    const payload = err?.error;
    if (!payload) return null;

    if (payload instanceof ArrayBuffer) {
      try {
        const text = new TextDecoder('utf-8').decode(payload);
        try {
          return JSON.parse(text);
        } catch {
          return text;
        }
      } catch {
        return '[ArrayBuffer non lisible]';
      }
    }

    if (payload instanceof Blob) {
      try {
        const text = await payload.text();
        try {
          return JSON.parse(text);
        } catch {
          return text;
        }
      } catch {
        return '[Blob non lisible]';
      }
    }

    return payload;
  }

  validerForm = this.fb.group({
    decision:    ['APPROUVE', Validators.required],
    motif:       [''],
    commentaire: ['']
  });

  validerLoading = false;
  validerDone = false;

  signaturePerso: SignatureUtilisateurDTO | null = null;
  private signalRSubscription?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    public besoinsService: BesoinsService,
    private workflowService: WorkflowService,
    private signaturesService: SignaturesService,
    private maSignatureService: MaSignatureService,
    public auth: AuthService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private confirm: ConfirmService,
    private signalR: SignalRService
  ) {}

  ngOnInit() {
    const id = +this.route.snapshot.params['id'];
    console.log('[FINASSIST][Init] Chargement besoin detail, id =', id);
    
    // Initialiser SignalR
    this.signalR.startConnection().then(() => {
      this.signalR.joinBesoinGroup(id);
    });

    // S'abonner aux mises à jour de l'historique via SignalR
    this.signalRSubscription = this.signalR.historiqueUpdates.subscribe(update => {
      if (update.besoinId === id) {
        console.log('[SignalR] Mise à jour historique reçue:', update);
        // Recharger l'historique complet pour avoir les vrais IDs et éviter les doublons
        this.besoinsService.getHistorique(id).subscribe({
          next: h => {
            this.historique = h;
            this.cdr.detectChanges();
          },
          error: () => {
            // Fallback : insérer l'entrée en tête si le rechargement échoue
            this.historique = [
              { id: 0, action: update.action, description: update.description, dateAction: update.dateAction },
              ...this.historique
            ];
            this.cdr.detectChanges();
          }
        });
        // Aussi rafraîchir le statut du besoin (ex: après validation)
        this.besoinsService.getById(id).subscribe({
          next: b => { this.besoin = b; this.cdr.detectChanges(); },
          error: () => {}
        });
      }
    });

    this.besoinsService.getById(id).subscribe({
      next: b => {
        console.log('[FINASSIST][Init] getById OK =>', b);
        this.besoin = b;
        this.loading = false;
      },
      error: err => {
        console.error('[FINASSIST][Init] getById ERROR', err);
        this.loading = false;
      }
    });
    this.besoinsService.getHistorique(id).subscribe({
      next: h => {
        console.log('[FINASSIST][Init] getHistorique OK, count =', h?.length ?? 0);
        this.historique = h;
      },
      error: err => console.error('[FINASSIST][Init] getHistorique ERROR', err)
    });
    this.besoinsService.getDocuments(id).subscribe({
      next: docs => {
        console.log('[FINASSIST][Init] getDocuments OK =>', docs);
        this.documents = docs;
      },
      error: err => console.error('[FINASSIST][Init] getDocuments ERROR', err)
    });

    // Charger la signature personnelle de l'utilisateur
    this.maSignatureService.get().subscribe({
      next: s => this.signaturePerso = s,
      error: () => this.signaturePerso = null
    });
  }

  ngOnDestroy() {
    if (this.imageUrl) URL.revokeObjectURL(this.imageUrl);
    if (this.pdfObjectUrl) URL.revokeObjectURL(this.pdfObjectUrl);
    if (this.apercuPdfObjectUrl) URL.revokeObjectURL(this.apercuPdfObjectUrl);
    
    // Nettoyer SignalR
    const id = +this.route.snapshot.params['id'];
    this.signalR.leaveBesoinGroup(id);
    this.signalRSubscription?.unsubscribe();
  }

  // Charge le document via HttpClient Angular (token injecté par l'intercepteur)
  private chargerDocument(besoinId: number, documentId: number, onDone?: () => void) {
    console.groupCollapsed('[FINASSIST][Signature] chargerDocument()');
    console.log('besoinId:', besoinId, 'documentId:', documentId);
    console.log('documents[0]:', this.documents?.[0]);
    console.log('isImage:', this.isImage, 'pdfSrc?', !!this.pdfSrc, 'imageUrl?', !!this.imageUrl);
    console.groupEnd();

    // Si le document est déjà en mémoire (PDF ou image), ne pas rester bloqué sur le loader.
    if (this.pdfSrc || this.imageUrl) {
      console.log('[FINASSIST][Signature] Document déjà chargé, on réutilise le cache.');
      this.pdfLoading = false;
      this.pdfError = false;
      this.cdr.detectChanges();
      onDone?.();
      return;
    }

    this.pdfLoading = true;
    this.pdfError = false;
    const url = this.besoinsService.getDocumentUrl(besoinId, documentId);
    console.log('[FINASSIST][Signature] GET document (arraybuffer) url =', url);
    const startedAt = Date.now();
    const timeoutId = window.setTimeout(() => {
      console.warn('[FINASSIST][Signature] Requête document toujours en attente (>10s)', { url, besoinId, documentId });
    }, 10000);

    this.http.get(url, { responseType: 'arraybuffer', observe: 'response' }).subscribe({
      next: response => {
        const buffer = response.body ?? new ArrayBuffer(0);
        console.log('[FINASSIST][Signature] Réponse document reçue', {
          status: response.status,
          contentType: response.headers.get('content-type'),
          contentLength: response.headers.get('content-length'),
          elapsedMs: Date.now() - startedAt,
          bytes: buffer?.byteLength
        });
        this.pdfLoading = false;
        if (this.isImage) {
          const blob = new Blob([buffer], { type: this.documents[0].type });
          if (this.imageUrl) URL.revokeObjectURL(this.imageUrl);
          this.imageUrl = URL.createObjectURL(blob);
          console.log('[FINASSIST][Signature] imageUrl créée =', this.imageUrl);
        } else {
          const pdfBlob = new Blob([buffer], { type: 'application/pdf' });
          if (this.pdfObjectUrl) URL.revokeObjectURL(this.pdfObjectUrl);
          this.pdfObjectUrl = URL.createObjectURL(pdfBlob);
          this.pdfSrc = this.pdfObjectUrl;
          console.log('[FINASSIST][Signature] pdfSrc Blob URL =', this.pdfObjectUrl);
        }
        this.cdr.detectChanges(); // forcer la mise à jour de la vue
        onDone?.();
      },
      error: (err) => {
        window.clearTimeout(timeoutId);
        this.pdfLoading = false;
        this.pdfError = true;
        console.error('[FINASSIST][Signature] Erreur chargement document', {
          url,
          status: err?.status,
          statusText: err?.statusText,
          error: err?.error,
          message: err?.message,
          elapsedMs: Date.now() - startedAt
        });
        // Si le document n'existe plus côté backend (404), resynchroniser la liste locale.
        if (err?.status === 404 && this.besoin?.id) {
          console.warn('[FINASSIST][Signature] Document introuvable côté API, resynchronisation de /pieces-jointes...');
          this.besoinsService.getDocuments(this.besoin.id).subscribe({
            next: docs => {
              console.log('[FINASSIST][Signature] Liste documents après resync =>', docs);
              this.documents = docs;
            },
            error: syncErr => console.error('[FINASSIST][Signature] Echec resync /pieces-jointes', syncErr)
          });
        }
        this.cdr.detectChanges();
        onDone?.();
      },
      complete: () => {
        window.clearTimeout(timeoutId);
        console.log('[FINASSIST][Signature] Requête document terminée.');
      }
    });
  }

 onPdfLoaded(pdf: any) {
  this.pdfTotalPages = pdf?.numPages ?? 0;
  console.log('[FINASSIST] PDF chargé, pages =', this.pdfTotalPages);

  setTimeout(() => {
    // ✅ Chercher dans les DEUX conteneurs possibles
    const zone = (document.querySelector('.doc-zone')
               ?? document.querySelector('.apercu-doc-zone')) as HTMLElement | null;

    const viewer   = zone?.querySelector('pdf-viewer') as HTMLElement | null;
    const container = zone?.querySelector('.ng2-pdf-viewer-container') as HTMLElement | null;

    console.group('[FINASSIST][CSS Debug] État après rendu PDF');
    console.log('zone trouvée :', zone?.className ?? 'NON TROUVÉE');
    console.log('zone dimensions :', zone ? {
      offsetHeight : zone.offsetHeight,
      offsetWidth  : zone.offsetWidth,
      position     : getComputedStyle(zone).position,  // doit être "relative"
    } : 'N/A');
    console.log('pdf-viewer :', viewer ? {
      offsetHeight : viewer.offsetHeight,              // doit être > 0
      display      : getComputedStyle(viewer).display  // doit être "block"
    } : 'NON TROUVÉ');
    console.log('ng2-pdf-viewer-container :', container ? {
      offsetHeight : container.offsetHeight,
      position     : getComputedStyle(container).position  // doit être "absolute"
    } : 'NON TROUVÉ');
    console.groupEnd();
  }, 300); // délai augmenté à 300ms pour laisser le DOM se stabiliser
}

  onPdfError(error: any) { this.pdfError = true; console.error('PDF error:', error); }

  get estSigne(): boolean { return !!this.besoin?.statut?.startsWith('SIGNE_PAR_'); }

  badgeClass(statut: string | undefined | null): string {
    if (!statut) return 'badge statut-brouillon';
    if (statut === 'BROUILLON')             return 'badge statut-brouillon';
    if (statut === 'ENREGISTRE')            return 'badge statut-enregistre';
    if (statut === 'TRANSMIS')              return 'badge statut-transmis';
    if (statut === 'TERMINE')               return 'badge statut-termine';
    if (statut.startsWith('EN_ATTENTE_'))   return 'badge statut-attente';
    if (statut.startsWith('APPROUVE_PAR_')) return 'badge statut-approuve';
    if (statut.startsWith('REJETE_PAR_'))   return 'badge statut-rejete';
    if (statut.startsWith('SIGNE_PAR_'))    return 'badge statut-signe';
    return 'badge';
  }

  labelStatut(statut: string | undefined | null): string {
    if (!statut) return '';
    if (statut === 'BROUILLON')  return 'Brouillon';
    if (statut === 'ENREGISTRE') return 'Enregistré';
    if (statut === 'SOUMISE')    return 'Soumis';
    if (statut === 'TRANSMIS')   return 'Transmis';
    if (statut === 'TERMINE')    return 'Terminé';
    if (statut.startsWith('EN_ATTENTE_'))   return `En attente — ${this.formatRole(statut.replace('EN_ATTENTE_', ''))}`;
    if (statut.startsWith('APPROUVE_PAR_')) return `Approuvé par ${this.formatRole(statut.replace('APPROUVE_PAR_', ''))}`;
    if (statut.startsWith('REJETE_PAR_'))   return `Rejeté par ${this.formatRole(statut.replace('REJETE_PAR_', ''))}`;
    if (statut.startsWith('SIGNE_PAR_'))    return `Signé par ${this.formatRole(statut.replace('SIGNE_PAR_', ''))}`;
    return statut;
  }

  private formatRole(roleCode: string): string {
    const map: Record<string, string> = {
      'RESPONSABLE':       'le Responsable',
      'DIRECTION':         'la Direction',
      'DIRECTIONGENERALE': 'la Direction Générale',
      'ADMINISTRATEUR':    "l'Administrateur",
      'AGENT':             "l'Agent",
    };
    return map[roleCode] ?? roleCode.charAt(0) + roleCode.slice(1).toLowerCase();
  }

  // ── Pièces jointes ────────────────────────────────────────────────────────
  onFichierChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const fichier = input.files?.[0];
    if (!fichier) return;
    this.uploading = true;
    this.besoinsService.ajouterPieceJointe(this.besoin!.id, fichier).subscribe({
      next: doc => {
        this.documents.push(doc);
        this.pdfSrc = null; // reset pour forcer rechargement
        if (this.pdfObjectUrl) {
          URL.revokeObjectURL(this.pdfObjectUrl);
          this.pdfObjectUrl = null;
        }
        this.toastr.success(`"${doc.nom}" ajouté.`);
        this.uploading = false;
        if (this.fileInput) this.fileInput.nativeElement.value = '';
        this.besoinsService.getHistorique(this.besoin!.id).subscribe(h => this.historique = h);
      },
      error: e => { this.toastr.error(e.error?.message ?? 'Erreur upload.'); this.uploading = false; }
    });
  }

  async supprimerDocument(docId: number) {
    const ok = await this.confirm.confirm({ titre: 'Supprimer le document', message: 'Êtes-vous sûr de vouloir supprimer ce document ?', labelConfirm: 'Supprimer', danger: true });
    if (!ok) return;
    this.besoinsService.supprimerDocument(this.besoin!.id, docId).subscribe({
      next: () => {
        this.toastr.success('Document supprimé.');
        this.documents = this.documents.filter(d => d.id !== docId);
        this.pdfSrc = null;
        if (this.pdfObjectUrl) {
          URL.revokeObjectURL(this.pdfObjectUrl);
          this.pdfObjectUrl = null;
        }
        this.besoinsService.getHistorique(this.besoin!.id).subscribe(h => this.historique = h);
      },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }

  // ── Actions workflow ──────────────────────────────────────────────────────
  enregistrer() {
    // Rafraîchir le statut avant d'agir pour éviter les actions sur un état obsolète
    this.besoinsService.getById(this.besoin!.id).subscribe({
      next: besoinFrais => {
        this.besoin = besoinFrais;
        if (besoinFrais.statut !== 'BROUILLON') {
          this.toastr.warning(
            `Le besoin est déjà en statut "${besoinFrais.statut}". La page a été mise à jour.`,
            'Statut mis à jour'
          );
          this.cdr.detectChanges();
          return;
        }
        this.besoinsService.enregistrer(besoinFrais.id).subscribe({
          next: b => { this.besoin = b; this.toastr.success('Besoin enregistré.'); this.cdr.detectChanges(); },
          error: e => {
            if (e.status === 409) {
              // Conflit : le statut a changé entre-temps, on resynchronise
              this.besoinsService.getById(this.besoin!.id).subscribe(b => {
                this.besoin = b;
                this.cdr.detectChanges();
              });
              this.toastr.warning('Le statut du besoin a changé. La page a été mise à jour.', 'Statut mis à jour');
            } else {
              this.toastr.error(e.error?.message ?? 'Erreur.');
            }
          }
        });
      },
      error: () => this.toastr.error('Impossible de vérifier le statut du besoin.')
    });
  }

  soumettre() {
    // Rafraîchir le statut avant d'agir pour éviter les actions sur un état obsolète
    this.besoinsService.getById(this.besoin!.id).subscribe({
      next: besoinFrais => {
        this.besoin = besoinFrais;
        if (besoinFrais.statut !== 'ENREGISTRE') {
          this.toastr.warning(
            `Le besoin est déjà en statut "${besoinFrais.statut}". La page a été mise à jour.`,
            'Statut mis à jour'
          );
          this.cdr.detectChanges();
          return;
        }
        this.besoinsService.soumettre(besoinFrais.id).subscribe({
          next: b => { this.besoin = b; this.toastr.success('Besoin soumis avec succès.'); this.cdr.detectChanges(); },
          error: e => {
            if (e.status === 409) {
              // Conflit : le statut a changé entre-temps, on resynchronise
              this.besoinsService.getById(this.besoin!.id).subscribe(b => {
                this.besoin = b;
                this.cdr.detectChanges();
              });
              this.toastr.warning('Le statut du besoin a changé. La page a été mise à jour.', 'Statut mis à jour');
            } else {
              this.toastr.error(e.error?.message ?? 'Erreur.');
            }
          }
        });
      },
      error: () => this.toastr.error('Impossible de vérifier le statut du besoin.')
    });
  }

  valider() {
    if (this.validerForm.invalid || this.validerLoading || this.validerDone) return;

    const decision = this.validerForm.value.decision;
    const motif = this.validerForm.value.motif?.trim();

    if (decision === 'REJETE' && !motif) {
      this.toastr.error('Le motif est obligatoire en cas de rejet.', 'Champ manquant');
      return;
    }

    this.validerLoading = true;
    this.workflowService.valider(this.besoin!.id, this.validerForm.value as any).subscribe({
      next: b => {
        this.besoinsService.getById(this.besoin!.id).subscribe(besoinMisAJour => {
          this.besoin = besoinMisAJour;
          this.cdr.detectChanges();
        });
        this.validerDone = true;
        this.validerLoading = false;
        if (decision === 'APPROUVE') {
          this.toastr.success('Besoin approuvé avec succès.');
        } else {
          this.toastr.warning(`Besoin rejeté. Motif : ${motif}`, 'Rejet confirmé', { timeOut: 6000 });
        }
        this.validerForm.reset({ decision: 'APPROUVE', motif: '', commentaire: '' });
        this.besoinsService.getHistorique(this.besoin!.id).subscribe(h => this.historique = h);
      },
      error: e => {
        this.validerLoading = false;
        this.toastr.error(e.error?.message ?? 'Erreur lors de la validation.');
      }
    });
  }

  transmettre() {
    this.workflowService.transmettre(this.besoin!.id).subscribe({
      next: () => {
        this.toastr.success('Besoin transmis.');
        // Recharger le besoin pour mettre à jour le statut
        this.besoinsService.getById(this.besoin!.id).subscribe({
          next: b => this.besoin = b,
          error: () => {}
        });
      },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la transmission.')
    });
  }

  // ── Modale signature manuscrite ───────────────────────────────────────────
  ouvrirSignature() {
    // Vérifier si l'utilisateur a une signature personnelle enregistrée
    if (!this.signaturePerso) {
      this.toastr.warning(
        'Vous n\'avez pas encore créé votre signature. Rendez-vous dans "Ma Signature" pour en créer une.',
        'Signature requise',
        { timeOut: 6000 }
      );
      this.router.navigate(['/ma-signature']);
      return;
    }

    this.signEtape = 1;
    this.signPos = null;
    this.signErreur = '';
    this.signLoading = false;
    this.pdfError = false;
    this.pdfLoading = false;
    // Reset le pdfSrc pour forcer le rechargement du bon document
    this.pdfSrc = null;
    if (this.pdfObjectUrl) { URL.revokeObjectURL(this.pdfObjectUrl); this.pdfObjectUrl = null; }
    this.showSignModal = true;
    this.cdr.detectChanges();

    if (!this.besoin) { this.signErreur = 'Besoin non chargé.'; return; }

    // Vérifier s'il existe un PDF signé par une étape précédente
    // Si oui, N2 doit signer sur le document déjà signé par N1
    this.pdfLoading = true;
    this.signaturesService.getApercuBesoin(this.besoin.id).subscribe({
      next: apercu => {
        // Un PDF signé existe → le télécharger et l'utiliser comme base
        this.signaturesService.telechargerDocumentSigne(apercu.id).subscribe({
          next: blob => {
            if (this.pdfObjectUrl) URL.revokeObjectURL(this.pdfObjectUrl);
            this.pdfObjectUrl = URL.createObjectURL(blob as Blob);
            this.pdfSrc = this.pdfObjectUrl;
            this.pdfLoading = false;
            this.cdr.detectChanges();
          },
          error: () => {
            // Fallback : charger le document original
            this._chargerDocumentPourSignature();
          }
        });
      },
      error: () => {
        // Pas de signature précédente → charger le document original
        this._chargerDocumentPourSignature();
      }
    });
  }

  private _chargerDocumentPourSignature() {
    if (!this.besoin) return;
    if (this.documents.length === 0) {
      this.besoinsService.getDocuments(this.besoin.id).subscribe({
        next: docs => {
          this.documents = docs;
          if (docs.length === 0) {
            this.pdfLoading = false;
            this.signErreur = 'Aucune pièce jointe à afficher.';
            this.cdr.detectChanges();
            return;
          }
          this.chargerDocument(this.besoin!.id, docs[0].id);
        },
        error: () => {
          this.pdfLoading = false;
          this.signErreur = 'Impossible de récupérer les pièces jointes.';
          this.cdr.detectChanges();
        }
      });
    } else {
      this.chargerDocument(this.besoin.id, this.documents[0].id);
    }
  }

  fermerSignModal() { this.showSignModal = false; }


onDocClick(event: MouseEvent) {
  // ng2-pdf-viewer génère un canvas par page dans .ng2-pdf-viewer-container .page
  const allPages = Array.from(
    document.querySelectorAll('.pdf-content-wrapper .ng2-pdf-viewer-container .page')
  ) as HTMLElement[];

  if (allPages.length === 0) {
    console.warn('[FINASSIST][Signature] Aucune page PDF trouvée dans le DOM');
    return;
  }

  // Trouver la page sur laquelle l'utilisateur a cliqué
  let clickedPageIndex = -1;
  let clickedPageEl: HTMLElement | null = null;

  for (let i = 0; i < allPages.length; i++) {
    const rect = allPages[i].getBoundingClientRect();
    if (event.clientY >= rect.top && event.clientY <= rect.bottom &&
        event.clientX >= rect.left && event.clientX <= rect.right) {
      clickedPageIndex = i;
      clickedPageEl = allPages[i];
      break;
    }
  }

  if (clickedPageIndex === -1 || !clickedPageEl) {
    console.warn('[FINASSIST][Signature] Clic hors des pages PDF');
    return;
  }

  const pageRect = clickedPageEl.getBoundingClientRect();
  const clickX = event.clientX - pageRect.left;
  const clickY = event.clientY - pageRect.top;

  // % relatif à la page cliquée (0-100)
  const pctXOnPage = Math.min(95, Math.max(0, (clickX / pageRect.width) * 100));
  const pctYOnPage = Math.min(95, Math.max(0, (clickY / pageRect.height) * 100));

  // Encoder : pageIndex * 1000 + pctY pour transmettre les deux infos en un seul champ
  // Backend décodera : pageIndex = floor(positionY / 1000), pctY = positionY % 1000
  const encodedY = clickedPageIndex * 1000 + Math.round(pctYOnPage * 10) / 10;

  this.signPos = {
    x: Math.round(pctXOnPage * 10) / 10,
    y: encodedY
  };

  // Pour l'affichage du rectangle dans la modale (position visuelle)
  const wrapper = document.querySelector('.pdf-content-wrapper') as HTMLElement | null;
  if (wrapper) {
    const wrapperRect = wrapper.getBoundingClientRect();
    const wX = Math.min(95, Math.max(0, ((event.clientX - wrapperRect.left) / wrapperRect.width) * 100));
    const wY = Math.min(95, Math.max(0, ((event.clientY - wrapperRect.top + wrapper.scrollTop) / wrapper.scrollHeight) * 100));
    this.signPosPreview = { x: Math.round(wX * 10) / 10, y: Math.round(wY * 10) / 10 };
  } else {
    this.signPosPreview = { x: this.signPos.x, y: pctYOnPage };
  }

  console.log('[FINASSIST][Signature] Position calculée:', {
    totalPages: allPages.length,
    clickedPageIndex,
    pageSize: { w: Math.round(pageRect.width), h: Math.round(pageRect.height) },
    pctXOnPage: Math.round(pctXOnPage * 10) / 10,
    pctYOnPage: Math.round(pctYOnPage * 10) / 10,
    signPos: this.signPos,
    signPosPreview: this.signPosPreview
  });
}


  confirmerPlacement() {
    if (!this.signPos) return;

    // Capturer les dimensions du canvas PDF AVANT de switcher vers l'étape 2
    const pdfCanvas = document.querySelector(
      '.pdf-content-wrapper .ng2-pdf-viewer-container .page canvas'
    ) as HTMLCanvasElement | null;

    if (pdfCanvas) {
      const r = pdfCanvas.getBoundingClientRect();
      this.pdfCanvasSnapshot = { width: r.width, height: r.height };

      // Log détaillé pour diagnostiquer le positionnement
      console.group('[FINASSIST][Snapshot] Dimensions capturées à confirmerPlacement');
      console.log('Canvas affiché (CSS px):', { w: Math.round(r.width), h: Math.round(r.height) });
      console.log('Canvas natif (attributs HTML):', { w: pdfCanvas.width, h: pdfCanvas.height });
      console.log('devicePixelRatio:', window.devicePixelRatio);
      console.log('signPos (% du canvas affiché):', this.signPos);
      console.log('→ Position en px sur canvas affiché:', {
        x: Math.round((this.signPos.x / 100) * r.width),
        y: Math.round((this.signPos.y / 100) * r.height)
      });
      // Vérifier si le canvas natif est différent du canvas affiché (DPR scaling)
      if (Math.abs(pdfCanvas.width - r.width) > 5) {
        console.warn('⚠ Canvas natif ≠ canvas affiché — DPR ou zoom actif. scaleX =',
          (pdfCanvas.width / r.width).toFixed(3));
      }
      console.groupEnd();
    } else {
      console.warn('[FINASSIST][Signature] Canvas PDF non trouvé à confirmerPlacement, snapshot null');
      this.pdfCanvasSnapshot = null;
    }

    this.signEtape = 2;
    setTimeout(() => this.initCanvas(), 50);
  }

  retourEtape1() { this.signEtape = 1; }

  private initCanvas() {
    if (!this.signCanvas) return;
    const canvas = this.signCanvas.nativeElement;
    this.ctx = canvas.getContext('2d');
    if (!this.ctx) return;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);
    this.ctx.strokeStyle = '#000000';
    this.ctx.lineWidth = 2;
    this.ctx.lineCap = 'round';
    this.ctx.lineJoin = 'round';
  }

  onCanvasMouseDown(e: MouseEvent) {
    this.drawing = true;
    const r = this.signCanvas.nativeElement.getBoundingClientRect();
    this.lastX = e.clientX - r.left; this.lastY = e.clientY - r.top;
  }

  onCanvasMouseMove(e: MouseEvent) {
    if (!this.drawing || !this.ctx) return;
    const r = this.signCanvas.nativeElement.getBoundingClientRect();
    const x = e.clientX - r.left; const y = e.clientY - r.top;
    this.ctx.beginPath(); this.ctx.moveTo(this.lastX, this.lastY);
    this.ctx.lineTo(x, y); this.ctx.stroke();
    this.lastX = x; this.lastY = y;
  }

  onCanvasMouseUp() { this.drawing = false; }

  onCanvasTouchStart(e: TouchEvent) {
    e.preventDefault();
    const t = e.touches[0]; const r = this.signCanvas.nativeElement.getBoundingClientRect();
    this.drawing = true; this.lastX = t.clientX - r.left; this.lastY = t.clientY - r.top;
  }

  onCanvasTouchMove(e: TouchEvent) {
    e.preventDefault();
    if (!this.drawing || !this.ctx) return;
    const t = e.touches[0]; const r = this.signCanvas.nativeElement.getBoundingClientRect();
    const x = t.clientX - r.left; const y = t.clientY - r.top;
    this.ctx.beginPath(); this.ctx.moveTo(this.lastX, this.lastY);
    this.ctx.lineTo(x, y); this.ctx.stroke();
    this.lastX = x; this.lastY = y;
  }

  effacerCanvas() {
    if (!this.ctx || !this.signCanvas) return;
    const c = this.signCanvas.nativeElement;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, c.width, c.height);
  }

  async validerSignature() {
    if (!this.signPos || !this.signaturePerso) return;
    // Utiliser la signature personnelle sauvegardée
    const signatureBase64 = this.signaturePerso.imageBase64;
    const docId = this.documents[0]?.id ?? 0;
    this.signLoading = true;
    this.signErreur = '';

    try {
      // ── 1. Charger pdf-lib dynamiquement ──────────────────────────────────
      const { PDFDocument } = await import('pdf-lib');

      // ── 2. Récupérer les bytes du PDF original depuis le blob URL en cache ─
      if (!this.pdfObjectUrl) throw new Error('PDF original non chargé en mémoire.');
      const pdfResponse = await fetch(this.pdfObjectUrl);
      const pdfArrayBuffer = await pdfResponse.arrayBuffer();
      const pdfDoc = await PDFDocument.load(pdfArrayBuffer);

      // ── 3. Utiliser le snapshot des dimensions du canvas PDF (capturé à l'étape 1) ──
      if (!this.pdfCanvasSnapshot) throw new Error('Dimensions du canvas PDF non capturées.');
      const canvasW = this.pdfCanvasSnapshot.width;
      const canvasH = this.pdfCanvasSnapshot.height;

      // Décoder signPos.y : pageIndex * 1000 + pctYOnPage
      const pageIndex = Math.floor(this.signPos.y / 1000);
      const pctYOnPage = this.signPos.y % 1000;
      const pctXOnPage = this.signPos.x;

      const targetPageIndex = Math.max(0, Math.min(pageIndex, pdfDoc.getPageCount() - 1));
      const page = pdfDoc.getPage(targetPageIndex);
      const pageWidth  = page.getWidth();
      const pageHeight = page.getHeight();

      // Ratio canvas affiché → points PDF (une page = canvasH px = pageHeight pt)
      const scaleX = pageWidth  / canvasW;
      const scaleY = pageHeight / canvasH;

      // % sur la page → px canvas → points PDF
      const clickXPx = (pctXOnPage / 100) * canvasW;
      const clickYPx = (pctYOnPage / 100) * canvasH;

      const xPt = clickXPx * scaleX;
      // PDF origin = bas gauche, Y inversé
      const yPt = pageHeight - (clickYPx * scaleY) - 60;

      const xPtClamped = Math.max(0, Math.min(xPt, pageWidth  - 150));
      const yPtClamped = Math.max(0, Math.min(yPt, pageHeight - 60));

      console.group('[FINASSIST][pdf-lib] Calcul de position');
      console.log('1. Canvas snapshot (px CSS):', { w: Math.round(canvasW), h: Math.round(canvasH) });
      console.log('2. Page PDF ciblée:', targetPageIndex, '— dimensions (points):', { w: Math.round(pageWidth), h: Math.round(pageHeight) });
      console.log('3. Scale canvas→PDF:', { scaleX: scaleX.toFixed(3), scaleY: scaleY.toFixed(3) });
      console.log('4. signPos décodé:', { pageIndex, pctXOnPage, pctYOnPage });
      console.log('5. Clic en px canvas:', { x: Math.round(clickXPx), y: Math.round(clickYPx) });
      console.log('6. Position finale PDF (origine bas-gauche):', { x: Math.round(xPtClamped), y: Math.round(yPtClamped) });
      console.log('7. Vérification visuelle — signature à:',
        `${Math.round((xPtClamped / pageWidth) * 100)}% depuis gauche,`,
        `${Math.round(((pageHeight - yPtClamped - 60) / pageHeight) * 100)}% depuis le haut de la page ${targetPageIndex + 1}`
      );
      console.groupEnd();

      // ── 4. Incrustation de la signature sur la page cible ────────────────
      // Retirer le préfixe data URL quel que soit le type (png, jpeg, etc.)
      const dataUrlMatch = signatureBase64.match(/^data:image\/(\w+);base64,(.+)$/);
      if (!dataUrlMatch) throw new Error('Format de signature invalide.');
      const imageType = dataUrlMatch[1].toLowerCase(); // png, jpeg, jpg, webp...
      const imageData = dataUrlMatch[2];
      const signatureBytes = Uint8Array.from(atob(imageData), c => c.charCodeAt(0));

      // pdf-lib supporte PNG et JPEG nativement
      const signatureImage = imageType === 'png'
        ? await pdfDoc.embedPng(signatureBytes)
        : await pdfDoc.embedJpg(signatureBytes);

      page.drawImage(signatureImage, {
        x: xPtClamped,
        y: yPtClamped,
        width:  150,
        height: 60,
        opacity: 1
      });

      // ── 4b. Texte sous la signature (nom + date) ──────────────────────────
      const { StandardFonts, rgb } = await import('pdf-lib');
      const font = await pdfDoc.embedFont(StandardFonts.Helvetica);
      const fontSize = 7;
      const user = this.auth.currentUser();
      const nomSignataire = user ? `${user.prenom} ${user.nom}` : 'Signataire';
      const dateStr = new Date().toLocaleDateString('fr-FR', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
      });

      // Ligne de séparation fine
      page.drawLine({
        start: { x: xPtClamped, y: yPtClamped - 2 },
        end:   { x: xPtClamped + 150, y: yPtClamped - 2 },
        thickness: 0.5,
        color: rgb(0.5, 0.5, 0.5)
      });

      page.drawText(`Signé par : ${nomSignataire}`, {
        x: xPtClamped,
        y: yPtClamped - 11,
        size: fontSize,
        font,
        color: rgb(0.2, 0.2, 0.2)
      });

      page.drawText(`Date : ${dateStr}`, {
        x: xPtClamped,
        y: yPtClamped - 20,
        size: fontSize,
        font,
        color: rgb(0.2, 0.2, 0.2)
      });

      // ── 5. Sérialiser le PDF signé en base64 ──────────────────────────────
      const pdfSigneBytes = await pdfDoc.save();
      const pdfSigneBase64 = btoa(
        Array.from(pdfSigneBytes).map(b => String.fromCharCode(b)).join('')
      );

      console.log('[FINASSIST][pdf-lib] PDF signé généré, bytes =', pdfSigneBytes.length);

      // ── 6. Envoyer au backend ─────────────────────────────────────────────
      this.signaturesService.signerParBesoin(this.besoin!.id, {
        documentId: docId,
        signatureBase64: imageData,  // base64 pur sans préfixe data URL
        positionX: xPtClamped,
        positionY: yPtClamped,
        largeur: 150,
        hauteur: 60,
        pdfSigneBase64
      }).subscribe({
        next: (res: any) => {
          this.signatureId = res?.id ?? null;
          this.signatureResult = {
            id: res?.id, signataireNom: res?.signataireNom ?? '',
            horodatage: res?.horodatage ?? new Date().toISOString(),
            empreinte: res?.empreinte ?? '', valide: res?.valide ?? true
          };
          this.fermerSignModal();
          this.toastr.success('Signature apposée avec succès.');
          this.besoinsService.getById(this.besoin!.id).subscribe(b => this.besoin = b);
          this.besoinsService.getHistorique(this.besoin!.id).subscribe(h => this.historique = h);
          this.signLoading = false;
        },
        error: e => { this.signErreur = e.error?.message ?? 'Erreur lors de la signature.'; this.signLoading = false; }
      });

    } catch (err: any) {
      console.error('[FINASSIST][pdf-lib] Erreur incrustation:', err);
      this.signErreur = err?.message ?? 'Erreur lors de la génération du PDF signé.';
      this.signLoading = false;
    }
  }

  // ── Aperçu document ───────────────────────────────────────────────────────
  ouvrirApercu() {
    if (!this.besoin) return;
    this.showApercuModal = true;
    this.apercuData = null;
    this.apercuLoading = true;
    this.apercuPdfSrc = null;
    this.apercuDocLoading = false;
    this.pdfError = false;

    // Toujours vérifier s'il existe une signature pour ce besoin
    // (même si le statut est EN_ATTENTE_ROLE{N} après transmission)
    this.signaturesService.getApercuBesoin(this.besoin.id).subscribe({
      next: d => {
        // Une signature existe → afficher le PDF signé
        this.apercuData = d;
        this.apercuDocLoading = true;
        this.signaturesService.telechargerDocumentSigne(d.id).subscribe({
          next: blob => {
            if (this.apercuPdfObjectUrl) URL.revokeObjectURL(this.apercuPdfObjectUrl);
            this.apercuPdfObjectUrl = URL.createObjectURL(blob as Blob);
            this.apercuPdfSrc = this.apercuPdfObjectUrl;
            this.apercuDocLoading = false;
            this.apercuLoading = false;
          },
          error: err => {
            this.extractHttpErrorBody(err).then(body => {
              console.error('[FINASSIST][Apercu] telechargerDocumentSigne ERROR', { status: err?.status, body });
              this.apercuLoading = false;
              this.apercuDocLoading = false;
              // Fallback : charger le document original
              this._chargerDocumentOriginal();
            });
          }
        });
      },
      error: () => {
        // Pas de signature → charger le document original
        this.apercuLoading = false;
        this._chargerDocumentOriginal();
      }
    });
  }

  private _chargerDocumentOriginal() {
    if (!this.besoin) return;
    const charger = (docs: DocumentDTO[]) => {
      if (docs.length > 0 && !this.pdfSrc) {
        this.apercuDocLoading = true;
        this.chargerDocument(this.besoin!.id, docs[0].id, () => { this.apercuDocLoading = false; });
      }
    };
    if (this.documents.length > 0) {
      charger(this.documents);
    } else {
      this.apercuDocLoading = true;
      this.besoinsService.getDocuments(this.besoin.id).subscribe(docs => {
        this.documents = docs;
        charger(docs);
        if (docs.length === 0) this.apercuDocLoading = false;
      });
    }
  }

  fermerApercu() {
    this.showApercuModal = false;
    this.apercuData = null;
    if (this.apercuPdfObjectUrl) {
      URL.revokeObjectURL(this.apercuPdfObjectUrl);
      this.apercuPdfObjectUrl = null;
    }
    this.apercuPdfSrc = null;
  }

  telecharger() {
    if (!this.apercuData) return;
    console.log('[FINASSIST][Apercu] Téléchargement manuel PDF signé signatureId =', this.apercuData.id);
    this.signaturesService.telechargerDocumentSigne(this.apercuData.id).subscribe({
      next: blob => {
        console.log('[FINASSIST][Apercu] Téléchargement manuel OK', {
          size: (blob as Blob)?.size,
          type: (blob as Blob)?.type
        });
        const url = URL.createObjectURL(blob as Blob);
        const a = document.createElement('a');
        a.href = url; a.download = 'document_signe.pdf'; a.click();
        URL.revokeObjectURL(url);
      },
      error: err => {
        this.extractHttpErrorBody(err).then(body => {
          console.error('[FINASSIST][Apercu] Téléchargement manuel ERROR', {
            signatureId: this.apercuData?.id,
            status: err?.status,
            statusText: err?.statusText,
            message: err?.message,
            body
          });
          const msg = body?.message ?? "Le téléchargement du document signé a échoué.";
          this.toastr.error(msg);
        });
      }
    });
  }

  zoomIn()  { this.docZoom = Math.min(this.docZoom + 0.25, 3); }
  zoomOut() { this.docZoom = Math.max(this.docZoom - 0.25, 0.5); }

  // ── Vérification signature ────────────────────────────────────────────────
  verifierSignature() {
    if (!this.signatureId) return;
    this.verifLoading = true;
    this.showVerifModal = true;
    this.verifResult = null;
    this.signaturesService.verifier(this.signatureId).subscribe({
      next: (res: any) => { this.verifResult = res; this.verifLoading = false; },
      error: () => { this.verifLoading = false; this.showVerifModal = false; this.toastr.error('Erreur vérification.'); }
    });
  }

  fermerModal() { this.showVerifModal = false; this.verifResult = null; }
}
