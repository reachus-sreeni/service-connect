import {defineConfig} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
const data=resolve('.e2e-data');mkdirSync(data,{recursive:true});
export default defineConfig({
 testDir:'e2e',fullyParallel:false,workers:1,timeout:45000,
 reporter:[['list'],['html',{open:'never'}]],
 use:{baseURL:'http://127.0.0.1:4201',viewport:{width:1365,height:900},trace:'retain-on-failure',screenshot:'only-on-failure'},
 webServer:[
  {command:'dotnet run --no-launch-profile --project ../backend/ServiceConnect.Api -c Release',url:'http://127.0.0.1:5001/health',reuseExistingServer:false,timeout:120000,env:{
   ASPNETCORE_ENVIRONMENT:'Development',ASPNETCORE_URLS:'http://127.0.0.1:5001',DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE:'false',
   Database__Provider:'Sqlite',Database__AutoInitialize:'true',Database__SeedDemo:'true',
   ConnectionStrings__Sqlite:'Data Source='+resolve(data,`browser-${Date.now()}.db`),
   Jwt__Key:'isolated-local-browser-tests-signing-key-not-for-production-2026',
   FrontendOrigin:'http://127.0.0.1:4201',AuthRateLimit__PermitLimit:'1000'
  }},
  {command:'npx ng serve --host 127.0.0.1 --port 4201 --proxy-config proxy.e2e.json',url:'http://127.0.0.1:4201',reuseExistingServer:false,timeout:120000}
 ]
});
