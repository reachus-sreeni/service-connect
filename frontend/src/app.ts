import {Component,signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
export interface RequestItem {id:string;title:string;description:string;location:string;acceptedEstimateId:string|null;status?:string;serviceId?:string;}
interface Estimate {id:string;revision:number;amount:number;notes:string;providerName:string;history:{number:number;amount:number;notes:string}[];}
interface Profile {accountId:string;businessName:string;qualifications:string;status:string;reviewNotes:string;account?:{name:string;email:string;phone:string;emailVerified:boolean;phoneVerified:boolean};}
interface Me {id:string;name:string;email:string;phone:string;role:string;emailVerified:boolean;phoneVerified:boolean;provider:Profile|null;}
interface Review {id:string;direction:string;overall:number;quality:number|null;punctuality:number|null;communication:number;cooperation:number|null;readiness:number|null;feedback:string;}
interface History {activities:{id:string;kind:string;details:string;createdAt:string}[];reviews:Review[];}
@Component({selector:'app-root',standalone:true,imports:[CommonModule,FormsModule],templateUrl:'./app.html'})
export class AppComponent {
 session=signal<{token:string;name:string;role:string}|null>(null);
 me=signal<Me|null>(null);services=signal<{id:string;name:string}[]>([]);providers=signal<Profile[]>([]);
 requests=signal<RequestItem[]>([]);selected=signal<RequestItem|null>(null);estimates=signal<Estimate[]>([]);
 history=signal<History|null>(null);error=signal('');notice=signal('');busy=signal(false);
 registering=false;email='';phone='';password='';name='';role='Customer';businessName='';qualifications='';
 title='';description='';location='';serviceId='';amount:number|null=null;notes='';adminNotes:Record<string,string>={};
 channels=['email','phone'];challengeIds:Record<string,string>={};codes:Record<string,string>={};
 ratings={overall:5,quality:5,communication:5,punctuality:5,cooperation:5,readiness:5,feedback:''};
 async api(path:string,method='GET',body?:unknown):Promise<any>{
  const response=await fetch('/api'+path,{method,headers:{'Content-Type':'application/json',...(this.session()?{'Authorization':'Bearer '+this.session()!.token}:{})},...(body===undefined?{}:{body:JSON.stringify(body)})});
  if(!response.ok){let message='Request failed ('+response.status+').';try{const result=await response.json();message=result.message||message;}catch{}if(response.status===401&&this.session())this.logout();throw new Error(message);}
  return response.status===204?null:response.json();
 }
 async run(action:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');this.notice.set('');try{await action();}catch(error){this.error.set(error instanceof Error?error.message:'Unable to connect.');}finally{this.busy.set(false);}}
 async auth(){await this.run(async()=>{this.session.set(await this.api('/auth/'+(this.registering?'register':'login'),'POST',{email:this.email,phone:this.phone,password:this.password,name:this.name,role:this.role,businessName:this.businessName,qualifications:this.qualifications}));this.password='';await this.load();});}
 logout(){this.session.set(null);this.me.set(null);this.requests.set([]);this.selected.set(null);this.estimates.set([]);this.history.set(null);this.providers.set([]);this.services.set([]);this.challengeIds={};this.codes={};this.password='';this.ratings.feedback='';this.adminNotes={};}
 async load(){
  this.me.set(await this.api('/me'));this.services.set(await this.api('/services'));if(!this.serviceId)this.serviceId=this.services()[0]?.id??'';
  if(this.session()?.role==='Admin')this.providers.set(await this.api('/admin/providers'));
  this.requests.set(await this.api('/requests'));
  const selected=this.selected();if(selected){this.selected.set(this.requests().find(x=>x.id===selected.id)||null);if(this.selected())await this.details(this.selected()!);else this.history.set(null);}
 }
 async details(request:RequestItem){
  const estimates=await this.api('/requests/'+request.id+'/estimates') as Estimate[];
  this.selected.set(request);this.estimates.set(estimates);this.history.set(null);this.ratings.feedback='';
  this.amount=this.session()?.role==='Provider'?(estimates[0]?.amount??null):null;this.notes=this.session()?.role==='Provider'?(estimates[0]?.notes??''):'';
  if(this.session()?.role!=='Provider'||this.isAcceptedProvider())this.history.set(await this.api('/requests/'+request.id+'/history'));
 }
 isAcceptedProvider(){return this.session()?.role==='Provider'&&this.estimates().some(e=>e.id===this.selected()?.acceptedEstimateId);}
 canQuote(){const me=this.me();return me?.role==='Provider'&&me.emailVerified&&me.phoneVerified&&me.provider?.status==='Approved';}
 canPost(){const me=this.me();return !!me?.emailVerified&&!!me.phoneVerified;}
 async create(){await this.run(async()=>{await this.api('/requests','POST',{title:this.title,description:this.description,location:this.location,serviceId:this.serviceId});this.title=this.description=this.location='';await this.load();});}
 async quote(){await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/estimate','PUT',{amount:this.amount,notes:this.notes,expectedRevision:this.estimates()[0]?.revision??0});await this.load();});}
 async accept(estimate:Estimate){if(!confirm('Accept this estimate for $'+estimate.amount+'? This closes quoting.'))return;await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/accept','POST',{estimateId:estimate.id,expectedRevision:estimate.revision});await this.load();});}
 async sendCode(channel:string){await this.run(async()=>{const result=await this.api('/me/verification/'+channel+'/send','POST');this.challengeIds[channel]=result.challengeId;this.notice.set('Verification code sent. It expires in 10 minutes.');});}
 async confirmCode(channel:string){await this.run(async()=>{await this.api('/me/verification/'+channel+'/confirm','POST',{challengeId:this.challengeIds[channel],code:this.codes[channel]});this.codes[channel]='';delete this.challengeIds[channel];await this.load();this.notice.set(channel+' verified.');});}
 async decide(provider:Profile,approve:boolean){await this.run(async()=>{await this.api('/admin/providers/'+provider.accountId+'/decision','POST',{approve,notes:this.adminNotes[provider.accountId]||''});await this.load();});}
 async workDone(){await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/work-done','POST');await this.load();});}
 async complete(){await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/complete','POST',{overall:this.ratings.overall,quality:this.ratings.quality,communication:this.ratings.communication,punctuality:this.ratings.punctuality,feedback:this.ratings.feedback});await this.load();});}
 async reviewCustomer(){await this.run(async()=>{await this.api('/requests/'+this.selected()!.id+'/customer-review','POST',{overall:this.ratings.overall,communication:this.ratings.communication,cooperation:this.ratings.cooperation,readiness:this.ratings.readiness,feedback:this.ratings.feedback});await this.load();});}
 statusLabel(status?:string){return ({Open:'Open',Accepted:'Accepted',AwaitingCustomerReview:'Awaiting customer review',Completed:'Completed'} as Record<string,string>)[status||'Open']||status;}
 activityLabel(kind:string){return ({RequestCreated:'Request posted',EstimateSubmitted:'Estimate submitted',EstimateRevised:'Estimate revised',EstimateAccepted:'Estimate accepted',WorkMarkedDone:'Work marked done',Completed:'Completion confirmed',CustomerRated:'Customer feedback submitted'} as Record<string,string>)[kind]||kind;}
 hasProviderReview(){return this.history()?.reviews.some(r=>r.direction==='ProviderToCustomer')??false;}
}
