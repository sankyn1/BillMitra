import { Directive, ElementRef, HostBinding, Input, OnInit, Renderer2 } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger' | 'success';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Directive({
  selector: 'button[bmButton], a[bmButton]',
  standalone: true
})
export class BmButtonDirective implements OnInit {
  @Input() bmVariant: ButtonVariant = 'primary';
  @Input() bmSize: ButtonSize = 'md';
  @Input() bmBlock = false;

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    this.applyClasses();
  }

  private applyClasses(): void {
    const el = this.el.nativeElement;
    this.renderer.addClass(el, 'bm-btn');
    this.renderer.addClass(el, `bm-btn-${this.bmVariant}`);
    this.renderer.addClass(el, `bm-btn-${this.bmSize}`);
    if (this.bmBlock) {
      this.renderer.addClass(el, 'bm-btn-block');
    }
  }
}

