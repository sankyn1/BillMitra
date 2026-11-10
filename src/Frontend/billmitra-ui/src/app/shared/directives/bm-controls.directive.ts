import { Directive, ElementRef, Input, OnInit, Renderer2 } from '@angular/core';

@Directive({
  selector: 'input[bmInput], select[bmSelect], textarea[bmTextarea]',
  standalone: true
})
export class BmInputDirective implements OnInit {
  @Input() bmSize: 'sm' | 'md' | 'lg' = 'md';
  @Input() bmInvalid = false;

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    const el = this.el.nativeElement;
    this.renderer.addClass(el, 'bm-input');
    this.renderer.addClass(el, `bm-input-${this.bmSize}`);
    if (this.bmInvalid) {
      this.renderer.addClass(el, 'bm-input-invalid');
    }
  }
}

export type BadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

@Directive({
  selector: '[bmBadge]',
  standalone: true
})
export class BmBadgeDirective implements OnInit {
  @Input() bmBadge: BadgeVariant = 'neutral';
  @Input() bmDot = false;

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    const el = this.el.nativeElement;
    this.renderer.addClass(el, 'bm-badge');
    this.renderer.addClass(el, `bm-badge-${this.bmBadge}`);
    if (this.bmDot) {
      this.renderer.addClass(el, 'bm-badge-dot');
    }
  }
}
