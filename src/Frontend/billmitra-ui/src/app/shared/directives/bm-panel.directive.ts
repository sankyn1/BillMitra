import { Directive, ElementRef, Input, OnInit, Renderer2 } from '@angular/core';

export type PanelElevation = 'none' | 'sm' | 'md' | 'lg';

@Directive({
  selector: '[bmPanel]',
  standalone: true
})
export class BmPanelDirective implements OnInit {
  @Input() bmElevation: PanelElevation = 'sm';
  @Input() bmBordered = true;

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    const el = this.el.nativeElement;
    this.renderer.addClass(el, 'bm-panel');
    this.renderer.addClass(el, `bm-panel-elevation-${this.bmElevation}`);
    if (this.bmBordered) {
      this.renderer.addClass(el, 'bm-panel-bordered');
    }
  }
}

@Directive({
  selector: '[bmPanelHeader]',
  standalone: true
})
export class BmPanelHeaderDirective implements OnInit {
  constructor(private el: ElementRef, private renderer: Renderer2) {}
  ngOnInit(): void {
    this.renderer.addClass(this.el.nativeElement, 'bm-panel-header');
  }
}

@Directive({
  selector: '[bmPanelBody]',
  standalone: true
})
export class BmPanelBodyDirective implements OnInit {
  constructor(private el: ElementRef, private renderer: Renderer2) {}
  ngOnInit(): void {
    this.renderer.addClass(this.el.nativeElement, 'bm-panel-body');
  }
}

@Directive({
  selector: '[bmPanelFooter]',
  standalone: true
})
export class BmPanelFooterDirective implements OnInit {
  constructor(private el: ElementRef, private renderer: Renderer2) {}
  ngOnInit(): void {
    this.renderer.addClass(this.el.nativeElement, 'bm-panel-footer');
  }
}

