import { Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LookupOption } from '../../models/lookup.models';
let nextId = 0;
@Component({ selector: 'app-catalog-combobox', template: `
<label [for]="id">{{label}}</label><div class="control"><input [id]="id" [value]="display" (input)="filter($any($event.target).value)" (focus)="show()" (keydown)="key($event)" [disabled]="disabled" [attr.aria-label]="label" role="combobox" aria-autocomplete="list" [attr.aria-controls]="id+'-list'" [attr.aria-expanded]="open" [attr.aria-activedescendant]="active>=0?id+'-'+active:null" [placeholder]="placeholder" autocomplete="off"><button type="button" [disabled]="disabled" [attr.aria-label]="'Mostrar '+label" (click)="open?open=false:show()">▾</button></div>
<div class="options" *ngIf="open"><div role="listbox" [id]="id+'-list'" [attr.aria-label]="label"><button type="button" role="option" *ngFor="let option of filtered;let i=index" [id]="id+'-'+i" [attr.aria-selected]="value===option.code" [class.active]="i===active" (click)="choose(option)"><strong *ngIf="showCode">{{option.code}}</strong><span>{{option.description}}</span></button></div><p *ngIf="!filtered.length">No se encontraron coincidencias.</p></div>`,
    styles: [`:host{display:block;position:relative;min-width:0}label{display:block;margin-bottom:8px;font-size:11px;color:var(--muted)}.control{display:flex}.control input{width:100%;min-width:0;border-radius:6px 0 0 6px}.control>button{border-radius:0 6px 6px 0;min-width:38px}.options{position:absolute;top:100%;left:0;right:0;z-index:20;background:white;border:1px solid #cfdbd7;border-radius:6px;box-shadow:0 8px 20px #102d3220;max-height:260px;overflow:auto}.options button{display:flex;gap:10px;width:100%;justify-content:flex-start;text-align:left;border:0;border-radius:0;padding:12px;white-space:normal;overflow-wrap:anywhere}.options button:hover,.options button.active{background:#e8f3ef}.options strong{flex-shrink:0}.options p{padding:12px;margin:0}`],
    host: { '(focusout)': 'blur($event)' }, providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => CatalogComboboxComponent), multi: true }] })
export class CatalogComboboxComponent implements ControlValueAccessor {
    @Input()
    showCode = true;
    @Input() placeholder = 'Escribe código o descripción';
    @Input()
    label = '';
    @Input()
    options: LookupOption[] = [];
    id = 'catalog-combo-' + nextId++;
    value = '';
    query = '';
    editing = false;
    open = false;
    disabled = false;
    active = -1;
    private changed: (value: string) => void = () => { };
    private touched: () => void = () => { };
    get display() { const option = this.options.find(o => o.code === this.value); return this.editing ? this.query : option ? (this.showCode ? option.code + ' · ' : '') + option.description : this.query; }
    get filtered() { const term = this.normalize(this.query); return this.options.filter(o => !term || this.normalize(o.code + ' ' + o.description).includes(term)); }
    private normalize(value: string) { return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }
    show() { this.query = ''; this.editing = false; this.open = true; this.active = -1; }
    filter(value: string) { this.query = value; this.editing = true; this.value = ''; this.changed(''); this.open = true; this.active = -1; }
    choose(option: LookupOption) { this.value = option.code; this.query = ''; this.editing = false; this.open = false; this.changed(option.code); this.touched(); }
    key(event: KeyboardEvent) { if (event.key === 'Escape') {
        this.open = false;
        event.preventDefault();
    }
    else if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        if (!this.open)
            this.show();
        const n = this.filtered.length;
        if (n)
            this.active = (this.active + (event.key === 'ArrowDown' ? 1 : -1) + n) % n;
    }
    else if (event.key === 'Enter' && this.open) {
        event.preventDefault();
        if (this.active >= 0 && this.filtered[this.active])
            this.choose(this.filtered[this.active]);
    } }
    blur(event: FocusEvent) { if (!(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node)) {
        this.open = false;
        this.touched();
    } }
    writeValue(value: string) { this.value = value ?? ''; this.query = ''; this.editing = false; }
    registerOnChange(fn: (value: string) => void) { this.changed = fn; }
    registerOnTouched(fn: () => void) { this.touched = fn; }
    setDisabledState(value: boolean) { this.disabled = value; }
}

