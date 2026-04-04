import {
  Component, Input, Output, EventEmitter, signal,
  HostListener, ElementRef, ViewChild, OnDestroy, AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SelectOption {
  value: string | number | null;
  label: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-select.component.html',
  styleUrl: './custom-select.component.scss'
})
export class CustomSelectComponent implements OnDestroy {
  @Input() options: SelectOption[] = [];
  @Input() selected: string | number | null = null;
  @Input() placeholder = 'Sélectionner';
  @Output() selectedChange = new EventEmitter<string | number | null>();

  @ViewChild('trigger') triggerRef!: ElementRef<HTMLElement>;

  isOpen = signal(false);

  // Élément DOM du dropdown injecté directement dans document.body
  private dropdownEl: HTMLElement | null = null;
  private boundClose = this.closeOnOutsideClick.bind(this);
  private boundReposition = this.reposition.bind(this);

  get selectedLabel(): string {
    return this.options.find(o => o.value === this.selected)?.label ?? this.placeholder;
  }

  toggle() {
    if (this.isOpen()) {
      this.closeDropdown();
    } else {
      this.openDropdown();
    }
  }

  private openDropdown() {
    this.isOpen.set(true);
    this.createDropdownPortal();
    setTimeout(() => {
      document.addEventListener('click', this.boundClose);
      window.addEventListener('scroll', this.boundReposition, true);
      window.addEventListener('resize', this.boundReposition);
    }, 0);
  }

  private closeDropdown() {
    this.isOpen.set(false);
    this.removeDropdownPortal();
    document.removeEventListener('click', this.boundClose);
    window.removeEventListener('scroll', this.boundReposition, true);
    window.removeEventListener('resize', this.boundReposition);
  }

  private createDropdownPortal() {
    this.removeDropdownPortal();
    if (!this.triggerRef) return;

    const rect = this.triggerRef.nativeElement.getBoundingClientRect();
    const isLight = document.body.classList.contains('light-mode');

    const el = document.createElement('div');
    el.className = 'cs-dropdown-portal';
    el.style.cssText = `
      position: fixed;
      top: ${rect.bottom + 6}px;
      left: ${rect.left}px;
      width: ${rect.width}px;
      z-index: 99999;
      border-radius: 12px;
      padding: 4px;
      overflow-y: auto;
      max-height: 280px;
      animation: csSlideIn .15s ease;
      ${isLight
        ? 'background:#fff;border:1px solid #e8eaf0;box-shadow:0 8px 32px rgba(99,102,120,.12);'
        : 'background:#1e1e35;border:1px solid rgba(255,255,255,.1);box-shadow:0 8px 32px rgba(0,0,0,.4);'
      }
    `;

    this.options.forEach(option => {
      const item = document.createElement('div');
      const isActive = this.selected === option.value;
      item.style.cssText = `
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 8px 12px;
        border-radius: 8px;
        font-size: 14px;
        cursor: pointer;
        font-family: inherit;
        transition: background .12s;
        ${isActive
          ? 'background:#7c3aed;color:#fff;font-weight:600;'
          : isLight ? 'color:#374151;' : 'color:rgba(255,255,255,.75);'
        }
      `;

      const label = document.createElement('span');
      label.textContent = option.label ?? '';
      item.appendChild(label);

      if (isActive) {
        const check = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        check.setAttribute('width', '16'); check.setAttribute('height', '12');
        check.setAttribute('viewBox', '0 0 16 12'); check.setAttribute('fill', 'none');
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', 'M1 6L5.5 10.5L15 1');
        path.setAttribute('stroke', 'white'); path.setAttribute('stroke-width', '2.2');
        path.setAttribute('stroke-linecap', 'round'); path.setAttribute('stroke-linejoin', 'round');
        check.appendChild(path);
        item.appendChild(check);
      }

      item.addEventListener('mouseenter', () => {
        if (!isActive) item.style.background = isLight ? '#f5f3ff' : 'rgba(255,255,255,.07)';
      });
      item.addEventListener('mouseleave', () => {
        if (!isActive) item.style.background = 'transparent';
      });
      item.addEventListener('click', (e) => {
        e.stopPropagation();
        this.selected = option.value;
        this.selectedChange.emit(option.value);
        this.closeDropdown();
      });

      el.appendChild(item);
    });

    document.body.appendChild(el);
    this.dropdownEl = el;
  }

  private removeDropdownPortal() {
    if (this.dropdownEl && this.dropdownEl.parentNode) {
      this.dropdownEl.parentNode.removeChild(this.dropdownEl);
    }
    this.dropdownEl = null;
  }

  private reposition() {
    if (!this.dropdownEl || !this.triggerRef) return;
    const rect = this.triggerRef.nativeElement.getBoundingClientRect();
    this.dropdownEl.style.top  = `${rect.bottom + 6}px`;
    this.dropdownEl.style.left = `${rect.left}px`;
    this.dropdownEl.style.width = `${rect.width}px`;
  }

  private closeOnOutsideClick(event: Event) {
    const target = event.target as HTMLElement;
    if (!this.triggerRef?.nativeElement.contains(target) &&
        !this.dropdownEl?.contains(target)) {
      this.closeDropdown();
    }
  }

  isSelected(option: SelectOption): boolean {
    return this.selected === option.value;
  }

  ngOnDestroy() {
    this.closeDropdown();
  }
}
