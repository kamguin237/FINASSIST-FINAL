import {
  Component, Input, Output, EventEmitter,
  OnChanges, SimpleChanges, HostListener, ElementRef,
  OnDestroy, ChangeDetectorRef, Renderer2
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-time-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './time-picker.component.html',
  styleUrl: './time-picker.component.scss'
})
export class TimePickerComponent implements OnChanges, OnDestroy {
  @Input() value = '';
  @Output() valueChange = new EventEmitter<string>();

  open = false;
  selectedH = 0;
  selectedM = 0;

  hours   = Array.from({ length: 24 }, (_, i) => i);
  minutes = Array.from({ length: 60 }, (_, i) => i);

  private dropdownEl: HTMLElement | null = null;
  private unlistenClick?: () => void;

  constructor(
    private el: ElementRef,
    private renderer: Renderer2,
    private cdr: ChangeDetectorRef
  ) {}

  get isLightMode(): boolean {
    return document.body.classList.contains('light-mode');
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['value'] && this.value) {
      const [h, m] = this.value.split(':').map(Number);
      this.selectedH = isNaN(h) ? 0 : h;
      this.selectedM = isNaN(m) ? 0 : m;
    }
  }

  ngOnDestroy() {
    this.destroyDropdown();
  }

  get display(): string {
    if (!this.value) return '--:--';
    return `${String(this.selectedH).padStart(2, '0')}:${String(this.selectedM).padStart(2, '0')}`;
  }

  toggle(e: MouseEvent) {
    e.stopPropagation();
    if (this.open) {
      this.destroyDropdown();
    } else {
      this.createDropdown();
    }
  }

  private createDropdown() {
    this.open = true;
    const light = this.isLightMode;

    // Calculer la position du trigger
    const triggerEl = this.el.nativeElement.querySelector('.tp-trigger') as HTMLElement;
    if (!triggerEl) {
      console.error('[TimePicker] .tp-trigger not found');
      return;
    }
    const rect = triggerEl.getBoundingClientRect();
    console.log('[TimePicker] trigger rect:', rect.top, rect.bottom, rect.left);

    // position:fixed avec coordonnées viewport
    const top  = rect.bottom + 6;
    const left = rect.left;
    console.log('[TimePicker] dropdown position:', top, left);

    // Créer le dropdown et l'attacher au body
    const div = this.renderer.createElement('div') as HTMLElement;
    div.className = 'tp-dropdown-portal' + (light ? ' light' : '');
    div.style.cssText = `
      position: fixed;
      top: ${top}px;
      left: ${left}px;
      z-index: 999999;
      background: ${light ? '#ffffff' : '#1e1e30'};
      border: 1px solid rgba(124,58,237,0.3);
      border-radius: 14px;
      box-shadow: ${light ? '0 16px 48px rgba(0,0,0,0.15)' : '0 16px 48px rgba(0,0,0,0.5)'};
      overflow: hidden;
      min-width: 170px;
      font-family: inherit;
    `;

    div.innerHTML = this.buildDropdownHTML(light);
    document.body.appendChild(div);
    this.dropdownEl = div;
    console.log('[TimePicker] dropdown appended to body, in body:', document.body.contains(div));

    // Ajouter les event listeners
    div.querySelectorAll('.tp-h-item').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const h = parseInt((btn as HTMLElement).dataset['h'] ?? '0');
        this.selectedH = h;
        this.emit();
        this.refreshDropdown();
      });
    });

    div.querySelectorAll('.tp-m-item').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const m = parseInt((btn as HTMLElement).dataset['m'] ?? '0');
        this.selectedM = m;
        this.emit();
        this.refreshDropdown();
      });
    });

    div.querySelector('.tp-ok-btn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.destroyDropdown();
    });

    // Scroll vers l'item actif
    setTimeout(() => {
      const hActive = div.querySelector('.tp-h-item.active') as HTMLElement;
      const mActive = div.querySelector('.tp-m-item.active') as HTMLElement;
      hActive?.scrollIntoView({ block: 'center', behavior: 'smooth' });
      mActive?.scrollIntoView({ block: 'center', behavior: 'smooth' });
    }, 50);

    // Fermer au clic extérieur
    this.unlistenClick = this.renderer.listen('document', 'click', (e: MouseEvent) => {
      if (!div.contains(e.target as Node) && !this.el.nativeElement.contains(e.target as Node)) {
        this.destroyDropdown();
      }
    });

    this.cdr.detectChanges();
  }

  private buildDropdownHTML(light: boolean): string {
    const textColor   = light ? '#374151' : '#a0a0b0';
    const activeColor = '#ffffff';
    const activeBg    = 'linear-gradient(135deg,#7c3aed 0%,#6d28d9 100%)';
    const hoverBg     = light ? 'rgba(124,58,237,0.08)' : 'rgba(124,58,237,0.12)';
    const headerBorder = light ? '#e8eaed' : 'rgba(255,255,255,0.08)';
    const footerBg    = light ? '#f9fafb' : 'rgba(255,255,255,0.03)';
    const footerBorder = light ? '#e8eaed' : 'rgba(255,255,255,0.08)';
    const previewColor = '#a78bfa';
    const scrollbarColor = 'rgba(124,58,237,0.3)';

    const style = `<style>
      .tp-dropdown-portal { font-size: 14px; }
      .tp-dp-header { display:flex; align-items:center; justify-content:space-around; padding:8px 12px 6px; border-bottom:1px solid ${headerBorder}; }
      .tp-dp-col-label { font-size:11px; font-weight:700; color:#7c3aed; text-transform:uppercase; letter-spacing:0.08em; width:64px; text-align:center; }
      .tp-dp-sep { font-size:14px; color:rgba(124,58,237,0.5); font-weight:700; width:16px; text-align:center; }
      .tp-dp-cols { display:flex; padding:8px; gap:0; }
      .tp-dp-col-wrap { width:64px; overflow:hidden; }
      .tp-dp-col { width:70px; height:200px; overflow-y:auto; display:flex; flex-direction:column; gap:2px; scroll-behavior:smooth; padding-right:6px; scrollbar-width:none; }
      .tp-dp-col::-webkit-scrollbar { display:none !important; width:0 !important; }
      .tp-dp-divider { width:16px; display:flex; align-items:center; justify-content:center; font-size:18px; font-weight:700; color:rgba(124,58,237,0.5); padding-top:80px; flex-shrink:0; }
      .tp-h-item, .tp-m-item { width:100%; height:36px; border:none; background:transparent; color:${textColor}; font-size:14px; font-variant-numeric:tabular-nums; font-weight:500; border-radius:8px; cursor:pointer; transition:background 0.12s,color 0.12s,transform 0.1s; flex-shrink:0; text-align:center; font-family:inherit; }
      .tp-h-item:hover:not(.active), .tp-m-item:hover:not(.active) { background:${hoverBg}; color:${light ? '#1a1d23' : '#ffffff'}; }
      .tp-h-item.active, .tp-m-item.active { background:${activeBg}; color:${activeColor}; font-weight:700; box-shadow:0 3px 10px rgba(124,58,237,0.35); transform:scale(1.04); }
      .tp-dp-footer { display:flex; align-items:center; justify-content:space-between; padding:8px 12px; border-top:1px solid ${footerBorder}; background:${footerBg}; }
      .tp-dp-preview { font-size:13px; font-weight:700; color:${previewColor}; font-variant-numeric:tabular-nums; letter-spacing:0.04em; }
      .tp-ok-btn { padding:4px 14px; background:#7c3aed; color:#fff; border:none; border-radius:7px; font-size:13px; font-weight:700; cursor:pointer; font-family:inherit; }
      .tp-ok-btn:hover { opacity:0.85; }
      @keyframes tpSlide { from { opacity:0; transform:translateY(-8px) scale(0.97); } to { opacity:1; transform:translateY(0) scale(1); } }
    </style>`;

    const hItems = this.hours.map(h =>
      `<button class="tp-h-item${h === this.selectedH ? ' active' : ''}" data-h="${h}">${String(h).padStart(2,'0')}</button>`
    ).join('');

    const mItems = this.minutes.map(m =>
      `<button class="tp-m-item${m === this.selectedM ? ' active' : ''}" data-m="${m}">${String(m).padStart(2,'0')}</button>`
    ).join('');

    return `${style}
      <div class="tp-dp-header">
        <span class="tp-dp-col-label">H</span>
        <div class="tp-dp-sep">:</div>
        <span class="tp-dp-col-label">min</span>
      </div>
      <div class="tp-dp-cols">
        <div class="tp-dp-col-wrap"><div class="tp-dp-col">${hItems}</div></div>
        <div class="tp-dp-divider">:</div>
        <div class="tp-dp-col-wrap"><div class="tp-dp-col">${mItems}</div></div>
      </div>
      <div class="tp-dp-footer">
        <span class="tp-dp-preview">${String(this.selectedH).padStart(2,'0')} h ${String(this.selectedM).padStart(2,'0')} min</span>
        <button class="tp-ok-btn">OK</button>
      </div>`;
  }

  private refreshDropdown() {
    if (!this.dropdownEl) return;
    const light = this.isLightMode;
    // Mettre à jour les classes active
    this.dropdownEl.querySelectorAll('.tp-h-item').forEach(btn => {
      const h = parseInt((btn as HTMLElement).dataset['h'] ?? '0');
      btn.classList.toggle('active', h === this.selectedH);
    });
    this.dropdownEl.querySelectorAll('.tp-m-item').forEach(btn => {
      const m = parseInt((btn as HTMLElement).dataset['m'] ?? '0');
      btn.classList.toggle('active', m === this.selectedM);
    });
    // Mettre à jour le preview
    const preview = this.dropdownEl.querySelector('.tp-dp-preview');
    if (preview) {
      preview.textContent = `${String(this.selectedH).padStart(2,'0')} h ${String(this.selectedM).padStart(2,'0')} min`;
    }
    this.cdr.detectChanges();
  }

  private destroyDropdown() {
    if (this.dropdownEl) {
      document.body.removeChild(this.dropdownEl);
      this.dropdownEl = null;
    }
    if (this.unlistenClick) {
      this.unlistenClick();
      this.unlistenClick = undefined;
    }
    this.open = false;
    this.cdr.detectChanges();
  }

  private emit() {
    const val = `${String(this.selectedH).padStart(2, '0')}:${String(this.selectedM).padStart(2, '0')}`;
    this.valueChange.emit(val);
  }

  pad(n: number): string {
    return String(n).padStart(2, '0');
  }

  @HostListener('window:scroll')
  @HostListener('window:resize')
  onScrollOrResize() {
    if (this.open && this.dropdownEl) {
      const rect = this.el.nativeElement.querySelector('.tp-trigger')?.getBoundingClientRect();
      if (rect) {
        this.dropdownEl.style.top  = `${rect.bottom + 6}px`;
        this.dropdownEl.style.left = `${rect.left}px`;
      }
    }
  }
}
