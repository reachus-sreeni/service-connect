import {test,expect,Page,TestInfo} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
async function accessible(page:Page,info:TestInfo){
 const result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa']).analyze();
 await info.attach('axe-results',{body:JSON.stringify({url:page.url(),violations:result.violations},null,2),contentType:'application/json'});
 expect(result.violations).toEqual([]);
}
async function login(page:Page,email='alex@example.test'){
 await page.getByRole('button',{name:'Sign in',exact:true}).click();
 await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill('Local-Demo-Only-2026!');
 await page.getByRole('button',{name:'Sign in to Service Connect',exact:true}).click();
 await expect(page.getByRole('button',{name:'Sign out',exact:true})).toBeEnabled();
}
test('public discovery, keyboard skip link and category details are accessible',async({page},info)=>{
 await page.goto('/');await expect(page.getByRole('button',{name:/Plumbing.*service options/})).toBeVisible();
 await page.keyboard.press('Tab');await expect(page.getByRole('link',{name:'Skip to main content'})).toBeFocused();
 await accessible(page,info);
 await page.getByRole('button',{name:/Plumbing.*service options/}).click();await expect(page.getByRole('heading',{name:'Choose the help you need'})).toBeFocused();
 await expect(page.getByRole('heading',{name:'Leak repair',exact:true})).toBeVisible();await accessible(page,info);
});
test('service search preserves selection through sign-in and submits a reviewed request',async({page},info)=>{
 await page.goto('/');await page.getByLabel('Search services',{exact:true}).fill('oil');await page.getByLabel('Service area',{exact:true}).fill('Des Moines');
 await page.getByRole('button',{name:/Automotive Service.*5 service options/}).click();
 await page.getByRole('button',{name:'Request estimates for Oil & filter service',exact:true}).click();
 await page.getByLabel('Email',{exact:true}).fill('alex@example.test');await page.getByLabel('Password',{exact:true}).fill('Local-Demo-Only-2026!');await page.getByRole('button',{name:'Sign in to Service Connect',exact:true}).click();
 await expect(page.getByRole('radio',{name:/Oil & filter service/})).toBeChecked();await page.getByRole('button',{name:'Continue',exact:true}).click();
 await expect(page.getByLabel('City or area',{exact:false})).toHaveValue('Des Moines');await page.getByLabel('What needs to be done?',{exact:false}).fill('Oil and filter maintenance for a compact car.');
 await page.getByRole('button',{name:'Continue',exact:true}).click();await expect(page.getByRole('heading',{name:'Everything look right?'})).toBeVisible();await accessible(page,info);
 await page.getByRole('button',{name:'Send request',exact:true}).click();await expect(page.getByRole('heading',{name:'Oil & filter service',exact:true})).toBeVisible();await accessible(page,info);
});
test('wizard errors are announced, linked to fields and retain entered data',async({page},info)=>{
 await page.goto('/');await login(page);await page.getByRole('button',{name:/Gardening.*5 service options/}).click();await page.getByRole('button',{name:'Request estimates for Lawn care',exact:true}).click();
 await page.getByRole('button',{name:'Continue',exact:true}).click();await page.getByRole('button',{name:'Continue',exact:true}).click();
 await expect(page.locator('#request-errors')).toBeFocused();await expect(page.getByRole('link',{name:/Enter a city or area/})).toBeVisible();await accessible(page,info);
 await page.getByRole('link',{name:/Enter a city or area/}).click();await expect(page.getByLabel('City or area',{exact:false})).toBeFocused();
 await page.getByLabel('City or area',{exact:false}).fill('Ames');await page.getByRole('button',{name:'Back',exact:true}).click();await expect(page.getByRole('radio',{name:/Lawn care/})).toBeChecked();
});
test('provider registration explains requirements with labelled fields',async({page},info)=>{
 await page.goto('/');await page.getByRole('button',{name:'Sign in',exact:true}).click();await page.getByRole('button',{name:'Create an account',exact:true}).click();await page.getByLabel('I’m here as a').selectOption('Provider');
 await expect(page.getByLabel('Phone with country code')).toHaveAttribute('required','');await expect(page.getByLabel('Qualifications and experience')).toBeVisible();await accessible(page,info);
});
test('provider workspace and estimate form are accessible',async({page},info)=>{
 await page.goto('/');await login(page,'morgan@example.test');await expect(page.getByRole('heading',{name:'Explore service requests',exact:true})).toBeVisible();
 await page.getByRole('button').filter({hasText:'Repair loose cabinet hinge'}).click();await expect(page.getByLabel('Scope and terms')).toBeVisible();await accessible(page,info);
});
test('completed service feedback and administrator review are accessible',async({page},info)=>{
 await page.goto('/');await login(page);await page.getByRole('button',{name:'My requests',exact:true}).click();await page.getByRole('button').filter({hasText:'Repair a squeaky door'}).click();await page.getByRole('button',{name:'Activity & feedback',exact:true}).click();
 await expect(page.getByRole('heading',{name:'Customer review of provider',exact:true})).toBeVisible();await accessible(page,info);
 await page.getByRole('button',{name:'Sign out',exact:true}).click();await login(page,'admin@example.test');await expect(page.getByRole('heading',{name:'Provider review',exact:true})).toBeVisible();await expect(page.getByText('Casey Pending',{exact:true})).toBeVisible();await accessible(page,info);
});
test('mobile discovery reflows without horizontal scrolling',async({page},info)=>{
 await page.setViewportSize({width:390,height:844});await page.goto('/');await expect(page.getByRole('button',{name:/Cleaning.*service options/})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1)).toBe(true);await accessible(page,info);
});

test('expanded service families remain searchable and accessible',async({page},info)=>{
 await page.goto('/');await page.getByRole('button',{name:'All services (34)',exact:true}).click();
 await expect(page.getByRole('button',{name:/Women’s Salon.*service options/})).toBeVisible();
 await expect(page.getByRole('button',{name:/Pest Control.*service options/})).toBeVisible();
 await page.getByLabel('Search services',{exact:true}).fill('laser');await expect(page.getByRole('button',{name:/Skin & Laser Services.*service options/})).toBeVisible();
 await page.getByRole('button',{name:'Cleaning & hygiene',exact:true}).click();await expect(page.getByRole('button',{name:/Bathroom & Kitchen Cleaning.*service options/})).toBeVisible();await accessible(page,info);
});
