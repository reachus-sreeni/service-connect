import {Component,input} from '@angular/core';
const paths:Record<string,string>={
 tools:'M14 6a4 4 0 0 0-5 5l-6 6a2 2 0 0 0 3 3l6-6a4 4 0 0 0 5-5l-3 3-3-3 3-3Z',
 plumbing:'M7 3h10M12 3v4M5 12V9h12a3 3 0 0 1 3 3v2h-5v-2H5Zm-2 4h4v5H3Zm14 1s-2 2-2 3a2 2 0 0 0 4 0c0-1-2-3-2-3Z',
 garden:'M12 21V11M12 15C4 15 3 10 3 5c6 0 9 3 9 8m0-2c0-5 3-8 9-8 0 6-3 10-9 10',
 sparkles:'m12 2 2.5 7.5L22 12l-7.5 2.5L12 22l-2.5-7.5L2 12l7.5-2.5L12 2Zm7 0v4m-2-2h4',
 hammer:'m14 3 7 7-3 3-3-3-9 11-3-3L14 8l-3-3 3-2Z',
 car:'m5 6-2 7v6h3v-3h12v3h3v-6l-2-7H5Zm-1 6h16M6 14h2m8 0h2',
 bolt:'m13 2-9 12h7l-1 8 10-13h-8l1-7Z',
 search:'M21 21l-5-5m2-6a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z',
 pin:'M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0Zm-5 0a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z',
 arrow:'M4 12h15m-6-6 6 6-6 6',check:'m5 12 4 4L19 6',
 shield:'m12 2 9 4v6c0 6-9 10-9 10S3 18 3 12V6l9-4Zm-5 10 3 3 6-6',
 user:'M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM3 22v-3a9 9 0 0 1 18 0v3',
 bag:'M5 7h14v14H5V7Zm4 0V5a3 3 0 0 1 6 0v2',
 clock:'M12 6v6l4 2m6-2a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z',
 star:'m12 2 3 6 7 1-5 5 1 7-6-3-6 3 1-7-5-5 7-1 3-6Z',
 close:'m6 6 12 12M6 18 18 6',plus:'M12 5v14M5 12h14',chevron:'m9 5 7 7-7 7',
};
@Component({selector:'sc-icon',standalone:true,template:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path [attr.d]="path()"/></svg>'})
export class IconComponent {name=input('tools');path(){return paths[this.name()]||paths.tools;}}
