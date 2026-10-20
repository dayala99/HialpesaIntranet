const assert=require('node:assert/strict');const {authenticate}=require('../tools/provider-auth.cjs');
class Portal {
 constructor(mode){this.mode=mode;this.posts=0;this.bounced=false;this.location='about:blank';this.entered='';}
 url(){return this.location;}isClosed(){return false;}
 async goto(url){this.location=url;this.entered='';}
 async waitForNavigation(){}async waitForURL(predicate){assert.ok(predicate(new URL(this.location)));}
 locator(selector){return {isVisible:async()=>selector==='#Header1_haLogoff'&&!this.location.includes('login.aspx'),fill:async v=>{if(selector.includes('Username'))this.entered=v;},inputValue:async()=>this.entered,waitFor:async()=>{},click:async()=>{this.posts++;if(this.mode==='rejected')return;if(this.mode==='always-reset'||this.posts===1){this.entered='';return;}this.location='https://webservices.averydennison.com/specstar/default.aspx';}};}
 getByRole(role,{name}){return {click:async()=>{if(name==='PRODUCT ORDERING'&&!this.bounced){this.bounced=true;this.location='https://webservices.averydennison.com/specstar/login.aspx?ReturnUrl=test';this.entered='';}else if(name==='VINCE'){this.location='https://www.retailcompliance.averydennison.com/prd1/acs/sf/ui/test/OrderSetup.html';}}};}
}
(async()=>{
 const options={username:'test@example.invalid',password:'fake-test-only'};
 const page=new Portal('reset-then-bounce');await authenticate(page,options);assert.equal(page.posts,3);assert.ok(page.url().endsWith('OrderSetup.html'));
 await page.goto('https://www.retailcompliance.averydennison.com/prd1/acs/sf/ReLogonForm.html');await authenticate(page,options);assert.equal(page.posts,4);assert.ok(page.url().endsWith('OrderSetup.html'));console.log('PASS: cleared login, PRODUCT ORDERING bounce and expired session recover');
 const rejected=new Portal('rejected');await assert.rejects(()=>authenticate(rejected,options),/auth-rejected/);assert.equal(rejected.posts,1);console.log('PASS: rejected populated form does not repeat credentials');
 const resets=new Portal('always-reset');await assert.rejects(()=>authenticate(resets,options),/auth-session-reset-limit/);assert.equal(resets.posts,3);console.log('PASS: session retries bounded');
})().catch(error=>{console.error(error);process.exit(1)});