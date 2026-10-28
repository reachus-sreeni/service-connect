import {Injectable} from '@angular/core';
export class ApiError extends Error {constructor(message:string,public status:number){super(message);}}
@Injectable({providedIn:'root'})
export class ServiceApiClient {
 async request<T>(path:string,method='GET',body?:unknown,token?:string):Promise<T> {
  const response=await fetch('/api'+path,{method,headers:{'Content-Type':'application/json',...(token?{'Authorization':'Bearer '+token}:{})},...(body===undefined?{}:{body:JSON.stringify(body)})});
  if(!response.ok){let message='We couldn’t complete that request. Please try again.';try{const result=await response.json();message=result.message||message;}catch{}throw new ApiError(message,response.status);}
  return response.status===204?null as T:response.json();
 }
}
