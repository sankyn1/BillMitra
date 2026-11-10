import { Directive, ElementRef, Input, OnInit, Renderer2 } from '@angular/core';

@Directive({
  selector: 'table[bmTable]',
  standalone: true
})
export class BmTableDirective implements OnInit {
  @Input() bmCompact = false;
  @Input() bmStriped = true;
  @Input() bmHover = true;

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    const el = this.el.nativeElement;
    this.renderer.addClass(el, 'bm-table');
    if (this.bmCompact) this.renderer.addClass(el, 'bm-table-compact');
    if (this.bmStriped) this.renderer.addClass(el, 'bm-table-striped');
    if (this.bmHover) this.renderer.addClass(el, 'bm-table-hover');
  }
}

@Directive({
  selector: 'tr[bmTableRow]',
  standalone: true
})
export class BmTableRowDirective implements OnInit {
  constructor(private el: ElementRef, private renderer: Renderer2) {}
  ngOnInit(): void {
    this.renderer.addClass(this.el.nativeElement, 'bm-table-row');
  }
}

@Directive({
  selector: 'th[bmTableHeaderCell], td[bmTableCell]',
  standalone: true
})
export class BmTableCellDirective implements OnInit {
  @Input() bmAlign: 'left' | 'center' | 'right' = 'left';

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    const el = this.el.nativeElement;
    this.renderer.addClass(el, 'bm-table-cell');
    if (this.bmAlign !== 'left') {
      this.renderer.addClass(el, `bm-align-${this.bmAlign}`);
    }
  }
}

