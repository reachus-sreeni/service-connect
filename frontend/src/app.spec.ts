import {describe,it,expect,vi,beforeEach,afterEach} from 'vitest';
import {TestBed} from '@angular/core/testing';
import {AppComponent} from './app';
import {ServiceApiClient} from './api-client';
import {filterCatalog,validateDraft} from './catalog';
import {RequestWizardComponent} from './ui/request-wizard';
import {ServiceCategory} from './models';
const categories:ServiceCategory[]=[{id:'plumbing',name:'Plumbing',description:'Leaks and drainage',isFeatured:true,options:[{id:'leak',name:'Leak repair',description:'Repair leaking taps'}]},{id:'garden',name:'Gardening',description:'Outdoor care',isFeatured:true,options:[{id:'lawn',name:'Lawn care',description:'Mowing and edging'}]}];
describe('Service Connect UI',()=>{
 beforeEach(()=>{vi.spyOn(ServiceApiClient.prototype,'request').mockResolvedValue(categories);});
 afterEach(()=>vi.restoreAllMocks());
 it('shows new request action to customers and work discovery to providers',async()=>{
  await TestBed.configureTestingModule({imports:[AppComponent]}).compileComponents();
  const fixture=TestBed.createComponent(AppComponent);fixture.componentInstance.session.set({token:'t',name:'Customer',role:'Customer'});fixture.componentInstance.view.set('requests');fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('New request');
  fixture.componentInstance.session.set({token:'t',name:'Provider',role:'Provider'});fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Explore service requests');
  expect(fixture.nativeElement.textContent).not.toContain('New request');
 });
 it('clears private state on sign out',()=>{const app=new AppComponent();app.session.set({token:'secret',name:'Test',role:'Customer'});app.requests.set([{id:'1',title:'Repair',description:'Door',location:'Austin',acceptedEstimateId:null}]);app.logout();expect(app.session()).toBeNull();expect(app.requests()).toEqual([]);expect(app.selected()).toBeNull();});
 it('keeps provider revision number in update payload',async()=>{const app=new AppComponent();app.selected.set({id:'r',title:'Repair',description:'Door',location:'Austin',acceptedEstimateId:null});app.estimates.set([{id:'e',revision:3,amount:100,notes:'Scope',providerName:'Pro',history:[]}]);app.amount=125;app.notes='Updated scope';const spy=vi.spyOn(app,'api').mockResolvedValue({});vi.spyOn(app,'load').mockResolvedValue(undefined);await app.quote();expect(spy).toHaveBeenCalledWith('/requests/r/estimate','PUT',{amount:125,notes:'Updated scope',expectedRevision:3});});
 it('requires phone and provider qualifications at registration',async()=>{
  await TestBed.configureTestingModule({imports:[AppComponent]}).compileComponents();
  const fixture=TestBed.createComponent(AppComponent);fixture.componentInstance.view.set('account');fixture.componentInstance.registering=true;fixture.componentInstance.role='Provider';fixture.detectChanges();
  const phone=fixture.nativeElement.querySelector('input[name="phone"]') as HTMLInputElement;
  expect(phone.required).toBe(true);expect(phone.type).toBe('tel');expect(fixture.nativeElement.querySelector('textarea[name="qualifications"]')).not.toBeNull();
 });
 it('requires verified contacts and administrator approval for quoting',()=>{
  const app=new AppComponent();app.me.set({id:'p',name:'Provider',email:'p@example.test',phone:'+12025550101',role:'Provider',emailVerified:true,phoneVerified:false,provider:{accountId:'p',businessName:'Business',qualifications:'Experience',status:'Approved',reviewNotes:''}});
  expect(app.canQuote()).toBe(false);app.me.update(me=>({...me!,phoneVerified:true,provider:{...me!.provider!,status:'Pending'}}));expect(app.canQuote()).toBe(false);
  app.me.update(me=>({...me!,provider:{...me!.provider!,status:'Approved'}}));expect(app.canQuote()).toBe(true);
 });
 it('shows required customer feedback only when work awaits confirmation',async()=>{
  await TestBed.configureTestingModule({imports:[AppComponent]}).compileComponents();const fixture=TestBed.createComponent(AppComponent);const app=fixture.componentInstance;
  app.session.set({token:'t',name:'Customer',role:'Customer'});app.view.set('requests');app.selected.set({id:'r',title:'Repair',description:'Door',location:'Demo',acceptedEstimateId:'e',status:'AwaitingCustomerReview'});fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Confirm completion and rate the provider');expect(fixture.nativeElement.querySelector('textarea[name="feedback"]').required).toBe(true);
  app.selected.update(request=>({...request!,status:'Completed'}));fixture.detectChanges();expect(fixture.nativeElement.textContent).not.toContain('Confirm completion and rate the provider');
 });
 it('finds a category by its service option and rejects cross-category selections',()=>{
  expect(filterCatalog(categories,'mowing').map(c=>c.name)).toEqual(['Gardening']);
  const draft={serviceId:'plumbing',serviceOptionId:'lawn',title:'Repair',description:'A leak',location:'Demo area'};
  expect(validateDraft(draft,categories,3).serviceOptionId).toBeDefined();
 });
 it('validates each wizard step and emits the reviewed service option',async()=>{
  await TestBed.configureTestingModule({imports:[RequestWizardComponent]}).compileComponents();const fixture=TestBed.createComponent(RequestWizardComponent);
  fixture.componentRef.setInput('categories',categories);fixture.componentRef.setInput('initialCategoryId','plumbing');fixture.detectChanges();const wizard=fixture.componentInstance;
  wizard.next();expect(wizard.step()).toBe(1);expect(wizard.errors().serviceOptionId).toBeDefined();
  wizard.draft.serviceOptionId='leak';wizard.prefillTitle();wizard.next();expect(wizard.step()).toBe(2);
  wizard.next();expect(wizard.errors().location).toBeDefined();expect(wizard.step()).toBe(2);
  wizard.draft.location=' Demo area ';wizard.draft.description=' Slow leak ';wizard.next();expect(wizard.step()).toBe(3);
  const submitted=vi.fn();wizard.submitted.subscribe(submitted);wizard.next();expect(submitted).toHaveBeenCalledWith({serviceId:'plumbing',serviceOptionId:'leak',title:'Leak repair',description:'Slow leak',location:'Demo area'});
 });
 it('keeps non-popular catalog entries discoverable through families and search',()=>{
  const extended=[...categories,{id:'appliance',name:'Laptop Repair',description:'Computer repair',isFeatured:false,groupName:'Appliance care',options:[{id:'diagnostic',name:'Laptop diagnostic assessment',description:'Describe the laptop issue'}]}];
  expect(filterCatalog(extended,'')).toHaveLength(2);expect(filterCatalog(extended,'','All services')).toHaveLength(3);
  expect(filterCatalog(extended,'','Appliance care').map(c=>c.name)).toEqual(['Laptop Repair']);
  expect(filterCatalog(extended,'laptop').map(c=>c.name)).toEqual(['Laptop Repair']);
 });
});
