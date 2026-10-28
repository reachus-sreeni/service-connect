import {Component,input,output,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RequestDraft,ServiceCategory} from '../models';
import {validateDraft} from '../catalog';
import {IconComponent} from './icon';
@Component({selector:'sc-request-wizard',standalone:true,imports:[FormsModule,IconComponent],templateUrl:'./request-wizard.html'})
export class RequestWizardComponent {
 categories=input.required<ServiceCategory[]>();initialCategoryId=input('');initialOptionId=input('');area=input('');busy=input(false);
 submitted=output<RequestDraft>();cancelled=output<void>();step=signal(1);errors=signal<Partial<Record<keyof RequestDraft,string>>>({});
 draft:RequestDraft={serviceId:'',serviceOptionId:'',title:'',description:'',location:''};
 ngOnInit(){this.draft.serviceId=this.initialCategoryId();this.draft.serviceOptionId=this.initialOptionId();this.draft.location=this.area();this.prefillTitle();}
 category(){return this.categories().find(c=>c.id===this.draft.serviceId);}
 option(){return this.category()?.options.find(o=>o.id===this.draft.serviceOptionId);}
 changeCategory(){this.draft.serviceOptionId='';this.errors.set({});}
 prefillTitle(){if(!this.draft.title)this.draft.title=this.option()?.name||'';}
 errorKeys(){return Object.keys(this.errors()) as (keyof RequestDraft)[];}
 next(){const errors=validateDraft(this.draft,this.categories(),this.step());this.errors.set(errors);if(Object.keys(errors).length){setTimeout(()=>document.getElementById('request-errors')?.focus());return;}
  if(this.step()===3)this.submitted.emit({...this.draft,title:this.draft.title.trim(),location:this.draft.location.trim(),description:this.draft.description.trim()});
  else {this.step.update(step=>step+1);this.focusStep();}
 }
 back(){this.errors.set({});this.step.update(step=>Math.max(1,step-1));this.focusStep();}
 focusStep(){setTimeout(()=>document.getElementById('request-step-title')?.focus());}
}
