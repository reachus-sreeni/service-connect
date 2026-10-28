import {Component,input,output} from '@angular/core';
import {ServiceCategory} from '../models';
import {categoryIcon} from '../catalog';
import {IconComponent} from './icon';
@Component({selector:'sc-category-card',standalone:true,imports:[IconComponent],template:`
<button type="button" class="category-card" [class.is-selected]="selected()" [attr.aria-pressed]="selected()" (click)="chosen.emit(category())">
 <span class="category-art" [attr.data-category]="icon()"><sc-icon [name]="icon()"/><span class="art-orbit"></span></span>
 <span class="category-name">{{category().name}}</span><span class="category-count">{{category().options.length}} service options</span>
</button>`})
export class CategoryCardComponent {category=input.required<ServiceCategory>();selected=input(false);chosen=output<ServiceCategory>();icon(){return categoryIcon(this.category().name);}}
