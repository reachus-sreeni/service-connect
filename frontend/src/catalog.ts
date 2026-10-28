import {RequestDraft,ServiceCategory} from './models';
const knownIcon=(name:string)=>({'Plumbing':'plumbing','Gardening':'garden','Cleaning':'sparkles','Carpentry':'hammer','Automotive Service':'car','Electrical':'bolt'} as Record<string,string>)[name]||'tools';
export function categoryIcon(name:string):string {
 const known=knownIcon(name);if(known!=='tools')return known;
 if(/cleaning|disinfection/i.test(name))return 'sparkles';
 if(/skin|laser/i.test(name))return 'star';
 if(/salon|grooming|hair|makeup/i.test(name))return 'scissors';
 if(/spa|massage/i.test(name))return 'spa';
 if(/pest/i.test(name))return 'bug';
 if(/painting|wall decor/i.test(name))return 'paint';
 if(/smart lock/i.test(name))return 'shield';
 if(/furniture assembly/i.test(name))return 'hammer';
 if(/television|laptop/i.test(name))return 'monitor';
 if(/refrigerator|washing|microwave|AC |geyser|stove|chimney|purifier|cooler|inverter/i.test(name))return 'appliance';
 return 'tools';
}
export function filterCatalog(categories:ServiceCategory[],query:string,group='Popular'):ServiceCategory[]{
 const search=query.trim().toLocaleLowerCase();if(!search)return group==='Popular'?categories.filter(c=>c.isFeatured!==false):group==='All services'?categories:categories.filter(c=>c.groupName===group);
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
