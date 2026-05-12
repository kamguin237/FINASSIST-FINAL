import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService, Language } from '../../../core/services/language.service';

@Component({
  selector: 'app-language-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="lang-selector">
      <div class="lang-trigger" (click)="toggleDropdown()">
        <span class="lang-value">
          {{ currentLanguage === 'fr' ? '🇫🇷 Français' : '🇬🇧 English' }}
        </span>
        <span class="lang-chevron" [class.rotated]="isOpen">
          <svg width="12" height="8" viewBox="0 0 12 8" fill="none">
            <path d="M1 1L6 6L11 1" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
        </span>
      </div>

      @if (isOpen) {
        <div class="lang-dropdown">
          <div class="lang-option" 
               [class.lang-option--active]="currentLanguage === 'fr'"
               (click)="selectLanguage('fr')">
            <span>🇫🇷 Français</span>
            @if (currentLanguage === 'fr') {
              <svg class="lang-check" width="16" height="12" viewBox="0 0 16 12" fill="none">
                <path d="M1 6L5.5 10.5L15 1" stroke="white" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            }
          </div>
          <div class="lang-option" 
               [class.lang-option--active]="currentLanguage === 'en'"
               (click)="selectLanguage('en')">
            <span>🇬🇧 English</span>
            @if (currentLanguage === 'en') {
              <svg class="lang-check" width="16" height="12" viewBox="0 0 16 12" fill="none">
                <path d="M1 6L5.5 10.5L15 1" stroke="white" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .lang-selector {
      position: relative;
      display: block;
      width: 100%;
      user-select: none;
    }

    .lang-trigger {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: .5rem;
      padding: .55rem .9rem;
      border-radius: 10px;
      cursor: pointer;
      transition: border-color .15s, box-shadow .15s;
      background: rgba(255,255,255,.06);
      border: 1px solid rgba(255,255,255,.1);
      color: #ffffff;
      min-width: 160px;

      &:hover {
        border-color: rgba(124,58,237,.5);
      }
    }

    .lang-value {
      font-size: .88rem;
      font-weight: 500;
      flex: 1;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .lang-chevron {
      flex-shrink: 0;
      color: rgba(255,255,255,.5);
      transition: transform .2s ease;
      display: flex;
      align-items: center;

      &.rotated {
        transform: rotate(180deg);
      }
    }

    .lang-dropdown {
      position: absolute;
      top: calc(100% + 6px);
      left: 0;
      right: 0;
      z-index: 9999;
      border-radius: 12px;
      padding: .4rem;
      overflow-y: auto;
      max-height: 280px;
      animation: langSlideIn .15s ease;
      background: #1e1e35;
      border: 1px solid rgba(255,255,255,.1);
      box-shadow: 0 8px 32px rgba(0,0,0,.4);
    }

    @keyframes langSlideIn {
      from {
        opacity: 0;
        transform: translateY(-6px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .lang-option {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: .6rem .85rem;
      border-radius: 8px;
      font-size: .88rem;
      cursor: pointer;
      transition: background .12s;
      color: rgba(255,255,255,.75);

      &:hover:not(.lang-option--active) {
        background: rgba(255,255,255,.07);
        color: #ffffff;
      }

      &--active {
        background: #7c3aed;
        color: #ffffff;
        font-weight: 600;
      }
    }

    .lang-check {
      flex-shrink: 0;
    }

    /* Light mode */
    body.light-mode .lang-trigger {
      background: #ffffff;
      border-color: #e8eaed;
      color: #1a1d23;

      &:hover {
        border-color: rgba(13, 148, 136, 0.5);
      }
    }

    body.light-mode .lang-chevron {
      color: rgba(26, 29, 35, 0.5);
    }

    body.light-mode .lang-dropdown {
      background: #fff;
      border: 1px solid #e8eaf0;
      box-shadow: 0 8px 32px rgba(99,102,120,.12);
    }

    body.light-mode .lang-option {
      color: #374151;

      &:hover:not(.lang-option--active) {
        background: #f5f3ff;
        color: #1a1d23;
      }

      &--active {
        background: #0d9488;
        color: #ffffff;
      }
    }
  `],
  host: {
    '(document:click)': 'onDocumentClick($event)'
  }
})
export class LanguageSelectorComponent {
  isOpen = false;

  constructor(public langService: LanguageService) {}

  get currentLanguage(): Language {
    return this.langService.currentLanguage();
  }

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }

  selectLanguage(lang: Language) {
    this.langService.setLanguage(lang);
    this.isOpen = false;
  }

  onDocumentClick(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.lang-selector')) {
      this.isOpen = false;
    }
  }
}
