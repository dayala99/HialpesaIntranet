import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { AviosApi, apiError } from '../../core/avios-api.service';
import { OrderReference, PrintOrder } from '../../core/models';
@Component({selector:'app-order-print',templateUrl:'./order-print.component.html',styleUrl:'./order-print.component.scss'})
export class OrderPrintComponent {
  private api=inject(AviosApi);
  @Output() closed=new EventEmitter<void>();
  data:PrintOrder|null=null;error='';loading=false;
  @Input() set reference(value:OrderReference|null){if(value){this.loading=true;this.error='';this.api.printOrder(value).subscribe({next:data=>{this.data=data;this.loading=false;},error:e=>{this.error=apiError(e);this.loading=false;}});}}
  get purchasedTotal(){return this.data?.lines.reduce((sum,row)=>sum+row.quantity,0)??0;}
  get hasCalculated(){return this.data?.breakdown?.some(row=>row.calculated)??false;}
  get lineTotal(){return this.data?.lines.reduce((sum,row)=>sum+row.amount,0)??0;}
  print(){window.print();}
}
