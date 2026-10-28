import {Component,signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
export interface RequestItem {id:string;title:string;description:string;location:string;acceptedEstimateId:string|null;}
interface Estimate {id:string;revision:number;amount:number;notes:string;providerName:string;history:{number:number;amount:number;notes:string}[];}
@Component({selector:'app-root',standalone:true,imports:[CommonModule,FormsModule],templateUrl:'./app.html'})
export class AppComponent {
 session=signal<{token:string;name:string;role:string}|null>(null); requests=signal<RequestItem[]>([]); selected=signal<RequestItem|null>(null); estimates=signal<Estimate[]>([]); error=signal(''); busy=signal(false);
 registering=false; email=''; password=''; name=''; role='Customer'; title=''; description=''; location=''; amount:number|null=null; notes='';
 async api(path:string,method='GET',body?:unknown):Promise<any>{ const r=await fetch('/api'+path,{method,headers:{'Content-Type':'application/json',...(this.session()?{'Authorization':'Bearer '+this.session()!.token}:{})},...(body===undefined?{}:{body:JSON.stringify(body)})}); if(!r.ok){let msg='Request failed ('+r.status+').';try{const b=await r.json();msg=b.message||msg;}catch{} if(r.status===401&&this.session())this.logout();throw new Error(msg);}return r.status===204?null:r.json(); }
 async run(action:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');try{await action();}catch(e){this.error.set(e instanceof Error?e.message:'Unable to connect.');}finally{this.busy.set(false);}}
 async auth(){await this.run(async()=>{this.session.set(await this.api('/auth/'+(this.registering?'register':'login'),'POST',{email:this.email,password:this.password,name:this.name,role:this.role}));this.password='';await this.load();});}
 logout(){this.session.set(null);this.requests.set([]);this.selected.set(null);this.estimates.set([]);this.password='';}
 async load(){this.requests.set(await this.api('/requests'));const s=this.selected();if(s){this.selected.set(this.requests().find(x=>x.id===s.id)||null);if(this.selected())await this.details(this.selected()!);}}
 async details(r:RequestItem){const es=await this.api('/requests/'+r.id+'/estimates') as Estimate[];this.selected.set(r);this.estimates.set(es);this.amount=this.session()?.role==='Provider'?(es[0]?.amount??null):null;this.notes=this.session()?.role==='Provider'?(es[0]?.notes??''):'';}
 async create(){await this.run(async()=>{await this.api('/requests','POST',{title:this.title,description:this.description,location:this.location});this.title=this.description=this.location='';await this.load();});}
 async quote(){await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/estimate','PUT',{amount:this.amount,notes:this.notes,expectedRevision:this.estimates()[0]?.revision??0});await this.load();});}
 async accept(e:Estimate){if(!confirm('Accept this estimate for $'+e.amount+'? This closes the request to further revisions.'))return;await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/accept','POST',{estimateId:e.id,expectedRevision:e.revision});await this.load();});}
}
