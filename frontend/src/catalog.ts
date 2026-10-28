import {RequestDraft,ServiceCategory} from './models';
export const categoryIcon=(name:string)=>({'Plumbing':'plumbing','Gardening':'garden','Cleaning':'sparkles','Carpentry':'hammer','Automotive Service':'car','Electrical':'bolt'} as Record<string,string>)[name]||'tools';
export function filterCatalog(categories:ServiceCategory[],query:string):ServiceCategory[]{
 const search=query.trim().toLocaleLowerCase();if(!search)return categories.filter(c=>c.isFeatured!==false);
 return categories.filter(c=>[c.name,c.description,...c.options.flatMap(o=>[o.name,o.description])].some(text=>text.toLocaleLowerCase().includes(search)));
}
export function validateDraft(draft:RequestDraft,categories:ServiceCategory[],step:number):Partial<Record<keyof RequestDraft,string>>{
 const errors:Partial<Record<keyof RequestDraft,string>>={};
 const category=categories.find(c=>c.id===draft.serviceId);
 if(!category)errors.serviceId='Choose a service category.';
 if(!category?.options.some(o=>o.id===draft.serviceOptionId))errors.serviceOptionId='Choose a service option from this category.';
 if(step>=2){
  if(!draft.location.trim()||draft.location.length>200)errors.location='Enter a city or area, up to 200 characters.';
  if(!draft.title.trim()||draft.title.length>120)errors.title='Add a short title, up to 120 characters.';
  if(!draft.description.trim()||draft.description.length>4000)errors.description='Tell the provider what you need, up to 4,000 characters.';
 }
 return errors;
}
