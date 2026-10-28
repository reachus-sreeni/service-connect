import {Component,input} from '@angular/core';
export function statusLabel(status?:string){return ({Open:'Open for estimates',Accepted:'Provider selected',AwaitingCustomerReview:'Ready for your review',Completed:'Completed',Pending:'Pending review',Approved:'Approved',Rejected:'Not approved'} as Record<string,string>)[status||'Open']||status;}
@Component({selector:'sc-status',standalone:true,template:'<span class="status-pill" [attr.data-status]="value()">{{label()}}</span>'})
export class StatusPillComponent{value=input<string|undefined>('Open');label(){return statusLabel(this.value());}}
