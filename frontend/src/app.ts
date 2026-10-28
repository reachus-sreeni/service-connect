import {Component,computed,signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ApiError,ServiceApiClient} from './api-client';
import {filterCatalog,categoryIcon} from './catalog';
import {Estimate,History,Me,Profile,RequestDraft,RequestItem,ServiceCategory} from './models';
import {IconComponent} from './ui/icon';
import {CategoryCardComponent} from './ui/category-card';
import {StatusPillComponent} from './ui/status-pill';
import {RequestWizardComponent} from './ui/request-wizard';
export type {RequestItem} from './models';
@Component({selector:'app-root',standalone:true,imports:[CommonModule,FormsModule,IconComponent,CategoryCardComponent,StatusPillComponent,RequestWizardComponent],templateUrl:'./app.html'})
export class AppComponent {
 constructor(private client:ServiceApiClient=new ServiceApiClient()){}
 view=signal<'discover'|'requests'|'account'|'admin'|'new-request'>('discover');
 searchTerm=signal('');areaSearch='';activeCategoryId=signal('');chosenOptionId='';pendingRequest=false;catalogGroup=signal('Popular');
 requestFilter='all';requestSearch='';detailTab=signal<'estimates'|'activity'>('estimates');
 catalogLoading=signal(false);catalogError=signal('');
 visibleCategories=computed(()=>filterCatalog(this.services(),this.searchTerm(),this.catalogGroup()));
 familyNames=computed(()=>[...new Set(this.services().map(c=>c.groupName||'Other services'))].sort());
 optionCount(){return this.services().reduce((total,c)=>total+c.options.length,0);}
 chooseFamily(family:string){this.catalogGroup.set(family);this.searchTerm.set('');this.activeCategoryId.set('');}
 activeCategory=computed(()=>this.services().find(c=>c.id===this.activeCategoryId())||null);
 ngOnInit(){void this.loadCatalog();}
 async loadCatalog(){this.catalogLoading.set(true);try{this.services.set(await this.api('/services'));this.catalogError.set('');}catch{this.catalogError.set('We couldn’t load the services. Please try again.');}finally{this.catalogLoading.set(false);}}
 navigate(view:'discover'|'requests'|'account'|'admin'|'new-request'){this.view.set(view);this.error.set('');setTimeout(()=>document.getElementById('page-title')?.focus());}
 chooseCategory(category:ServiceCategory){this.activeCategoryId.set(category.id);this.chosenOptionId='';setTimeout(()=>document.getElementById('category-options-title')?.focus());}
 startRequest(optionId:string){this.chosenOptionId=optionId;this.pendingRequest=true;if(!this.session()){this.navigate('account');return;}if(this.session()?.role!=='Customer'){this.notice.set('Sign in as a customer to request a service.');return;}if(!this.canPost()){this.navigate('account');this.notice.set('Verify your email and phone before posting a request.');return;}this.navigate('new-request');}
 async submitRequest(draft:RequestDraft){await this.run(async()=>{const request=await this.api('/requests','POST',draft) as RequestItem;this.pendingRequest=false;await this.load();this.view.set('requests');await this.details(this.requests().find(r=>r.id===request.id)||request);this.notice.set('Your request is ready for estimates.');setTimeout(()=>document.getElementById('request-detail-title')?.focus());});}
 filteredRequests(){const search=this.requestSearch.trim().toLocaleLowerCase();return this.requests().filter(r=>(this.requestFilter==='all'||r.status===this.requestFilter)&&(!search||[r.title,r.description,r.location].some(t=>t.toLocaleLowerCase().includes(search))));}
 countStatus(status:string){return this.requests().filter(r=>r.status===status).length;}
 serviceName(request:RequestItem){return this.services().find(c=>c.id===request.serviceId)?.name||'Service request';}
 optionName(request:RequestItem){return this.services().flatMap(c=>c.options).find(o=>o.id===request.serviceOptionId)?.name||'Custom request';}
 pageTitle(){return this.view()==='requests'?(this.session()?.role==='Provider'?'Explore service requests':'Your service requests'):this.view()==='admin'?'Provider review':this.view()==='new-request'?'Let’s get your project started':'Your account';}
 errorAction(){return this.session()?'Please review the details and try again.':'Check your details and try again.';}

 session=signal<{token:string;name:string;role:string}|null>(null);
 me=signal<Me|null>(null);services=signal<ServiceCategory[]>([]);providers=signal<Profile[]>([]);
 requests=signal<RequestItem[]>([]);selected=signal<RequestItem|null>(null);estimates=signal<Estimate[]>([]);
 history=signal<History|null>(null);error=signal('');notice=signal('');busy=signal(false);
 registering=false;email='';phone='';password='';name='';role='Customer';businessName='';qualifications='';
 title='';description='';location='';serviceId='';amount:number|null=null;notes='';adminNotes:Record<string,string>={};
 channels=['email','phone'];challengeIds:Record<string,string>={};codes:Record<string,string>={};
 ratings={overall:5,quality:5,communication:5,punctuality:5,cooperation:5,readiness:5,feedback:''};
 async api(path:string,method='GET',body?:unknown):Promise<any>{try{return await this.client.request(path,method,body,this.session()?.token);}catch(error){if(error instanceof ApiError&&error.status===401&&this.session())this.logout();throw error;}}
 async run(action:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');this.notice.set('');try{await action();}catch(error){this.error.set(error instanceof Error?error.message:'Unable to connect.');}finally{this.busy.set(false);}}
 async auth(){await this.run(async()=>{this.session.set(await this.api('/auth/'+(this.registering?'register':'login'),'POST',{email:this.email,phone:this.phone,password:this.password,name:this.name,role:this.role,businessName:this.businessName,qualifications:this.qualifications}));this.password='';await this.load();this.navigate(this.pendingRequest&&this.session()?.role==='Customer'&&this.canPost()?'new-request':this.session()?.role==='Admin'?'admin':this.session()?.role==='Provider'?'requests':this.pendingRequest?'account':'discover');});}
 logout(){this.session.set(null);this.me.set(null);this.requests.set([]);this.selected.set(null);this.estimates.set([]);this.history.set(null);this.providers.set([]);this.challengeIds={};this.codes={};this.password='';this.ratings.feedback='';this.adminNotes={};this.view.set('discover');this.pendingRequest=false;}
 async load(){
  this.me.set(await this.api('/me'));this.services.set(await this.api('/services'));
  if(this.session()?.role==='Admin')this.providers.set(await this.api('/admin/providers'));
  this.requests.set(await this.api('/requests'));
  const selected=this.selected();if(selected){this.selected.set(this.requests().find(x=>x.id===selected.id)||null);if(this.selected())await this.details(this.selected()!);else this.history.set(null);}
 }
 async details(request:RequestItem){
  const estimates=await this.api('/requests/'+request.id+'/estimates') as Estimate[];
  if(this.selected()?.id!==request.id)this.detailTab.set('estimates');this.selected.set(request);this.estimates.set(estimates);this.history.set(null);this.ratings.feedback='';
  this.amount=this.session()?.role==='Provider'?(estimates[0]?.amount??null):null;this.notes=this.session()?.role==='Provider'?(estimates[0]?.notes??''):'';
  if(this.session()?.role!=='Provider'||this.isAcceptedProvider())this.history.set(await this.api('/requests/'+request.id+'/history'));
 }
 isAcceptedProvider(){return this.session()?.role==='Provider'&&this.estimates().some(e=>e.id===this.selected()?.acceptedEstimateId);}
 canQuote(){const me=this.me();return me?.role==='Provider'&&me.emailVerified&&me.phoneVerified&&me.provider?.status==='Approved';}
 canPost(){const me=this.me();return !!me?.emailVerified&&!!me.phoneVerified;}
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
