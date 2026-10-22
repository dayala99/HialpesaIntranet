import { Directive, ElementRef, HostListener, inject } from '@angular/core';
@Directive({ selector: 'input[type=date]' })
export class DatePickerDirective {
    private element = inject(ElementRef<HTMLInputElement>);
    @HostListener('click', ['$event'])
    open(event: MouseEvent) { const input = this.element.nativeElement; if (input.disabled || input.readOnly || typeof input.showPicker !== 'function')
        return; try {
        input.showPicker();
        event.preventDefault();
    }
    catch { } }
}
