import {describe,it,expect,vi} from 'vitest';
import {TestBed} from '@angular/core/testing';
import {AppComponent} from './app';
describe('Service Connect UI',()=>{
 it('shows customer request form and hides it for providers',async()=>{
  await TestBed.configureTestingModule({imports:[AppComponent]}).compileComponents();
  const fixture=TestBed.createComponent(AppComponent);fixture.componentInstance.session.set({token:'t',name:'Customer',role:'Customer'});fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('What do you need help with?');
  fixture.componentInstance.session.set({token:'t',name:'Provider',role:'Provider'});fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Explore service requests');
  expect(fixture.nativeElement.textContent).not.toContain('Post request');
 });
 it('clears private state on sign out',()=>{const app=new AppComponent();app.session.set({token:'secret',name:'Test',role:'Customer'});app.requests.set([{id:'1',title:'Repair',description:'Door',location:'Austin',acceptedEstimateId:null}]);app.logout();expect(app.session()).toBeNull();expect(app.requests()).toEqual([]);expect(app.selected()).toBeNull();});
 it('keeps provider revision number in update payload',async()=>{const app=new AppComponent();app.selected.set({id:'r',title:'Repair',description:'Door',location:'Austin',acceptedEstimateId:null});app.estimates.set([{id:'e',revision:3,amount:100,notes:'Scope',providerName:'Pro',history:[]}]);app.amount=125;app.notes='Updated scope';const spy=vi.spyOn(app,'api').mockResolvedValue({});vi.spyOn(app,'load').mockResolvedValue(undefined);await app.quote();expect(spy).toHaveBeenCalledWith('/requests/r/estimate','PUT',{amount:125,notes:'Updated scope',expectedRevision:3});});
 it('requires phone and provider qualifications at registration',async()=>{
  await TestBed.configureTestingModule({imports:[AppComponent]}).compileComponents();
  const fixture=TestBed.createComponent(AppComponent);fixture.componentInstance.registering=true;fixture.componentInstance.role='Provider';fixture.detectChanges();
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
  app.session.set({token:'t',name:'Customer',role:'Customer'});app.selected.set({id:'r',title:'Repair',description:'Door',location:'Demo',acceptedEstimateId:'e',status:'AwaitingCustomerReview'});fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Confirm completion and rate the provider');expect(fixture.nativeElement.querySelector('textarea[name="feedback"]').required).toBe(true);
  app.selected.update(request=>({...request!,status:'Completed'}));fixture.detectChanges();expect(fixture.nativeElement.textContent).not.toContain('Confirm completion and rate the provider');
 });
});
