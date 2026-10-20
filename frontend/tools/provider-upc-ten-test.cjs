const {chromium}=require('playwright');
const readline=require('node:readline');
let activePage;let stage="starting";
(async()=>{
 const username=process.env.AVERY_USERNAME,password=process.env.AVERY_PASSWORD;
 delete process.env.AVERY_USERNAME;delete process.env.AVERY_PASSWORD;
 if(!username||!password)throw Error('credentials');
 const commandInput=readline.createInterface({input:process.stdin});
 const initial=JSON.parse(await new Promise(resolve=>commandInput.once('line',resolve)));
 const {planQuantities}=require('./provider-quantities.cjs');
 const browser=await chromium.launch({channel:'chrome',headless:false,args:['--start-maximized']});
 const context=await browser.newContext({viewport:null});
 let page=await context.newPage();activePage=page;
 let searchUrl=null;
 const isRetail=url=>url.protocol==='https:'&&['www.retailcompliance.averydennison.com','retailcompliance.averydennison.com'].includes(url.hostname);
 const diagnostics=[];
 page.on('request',request=>{if(request.method()==='POST'&&request.url().includes('/specstar/')){const values=new URLSearchParams(request.postData()||'');diagnostics.push({userSent:[...values.values()].includes(username),passwordSent:[...values.values()].includes(password)});require('fs').writeFileSync('test-results/avery-auth-check.json',JSON.stringify(diagnostics));}});
 const loginUrl=process.env.AVERY_LOGIN_URL||'https://webservices.averydennison.com/specstar/login.aspx';delete process.env.AVERY_LOGIN_URL;
 if(loginUrl!=='https://webservices.averydennison.com/specstar/login.aspx')throw Error('destination');
 const {authenticate}=require('./provider-auth.cjs');
 async function signIn(){await authenticate(page,{username,password,loginUrl,setStage:value=>{stage=value;}});searchUrl=page.url();}
 async function fillPo(po,rows){
   if(!/^PO-[0-9]{1,20}$/.test(po))throw Error('po');
   stage='session';
   // A draft's Home link can lead to the login page. Reuse the search URL
   // observed after authentication, and recover an expired session once.
   if(page.isClosed()){page=await context.newPage();activePage=page;}
   if(!searchUrl)await signIn();
   let current=new URL(page.url());
   if(!isRetail(current)||!current.pathname.endsWith('/OrderSetup.html')){
     page=await context.newPage();activePage=page;
     if(searchUrl){
       const target=new URL(searchUrl);
       if(!isRetail(target)||!target.pathname.endsWith('/OrderSetup.html'))throw Error('search-destination');
       await page.goto(target.href,{waitUntil:'domcontentloaded',timeout:45000});
     }
   }
   const ready=await page.locator('#orderSearchButton').waitFor({state:'visible',timeout:8000}).then(()=>true,()=>false);
   if(!ready){await signIn();}
   stage='session';
   current=new URL(page.url());
   if(!isRetail(current)||!current.pathname.endsWith('/OrderSetup.html'))throw Error('session-destination');
   searchUrl=page.url();
   stage='po-field';
   const field=page.locator('input[type="text"]').nth(1);
   const label=await field.evaluate(e=>e.parentElement.previousElementSibling?.textContent||'');
   if(!label.replace(/\s+/g,' ').includes('PO Number'))throw Error('field');
   await field.fill(po);
   if(await field.inputValue()!==po)throw Error('value');
   stage='search';
   await page.locator('#orderSearchButton').click();
   stage='saved-result';
   const saved=page.getByRole('link',{name:'Saved',exact:true});
   await saved.first().waitFor({state:'visible',timeout:20000});
   let selected=null;
   for(const link of await saved.all()){
     const correctPo=await link.evaluate((e,po)=>Array.from(e.closest('tr')?.querySelectorAll('td')||[]).some(cell=>cell.textContent.trim()===po),po);
     if(correctPo){selected=link;break;}
   }
   if(!selected)throw Error('No Saved result for selected PO');
   await page.screenshot({path:'test-results/avery-search-results.png',fullPage:false});
   stage='opening-saved';
   const before=page.url();
   await selected.click();
   await page.waitForURL(url=>url.href!==before,{waitUntil:'domcontentloaded',timeout:20000});
   await page.waitForFunction(()=>document.body && document.body.innerText.trim().length>100,{},{timeout:20000});
   stage='loading-saved';
   await page.getByText('Processing',{exact:true}).waitFor({state:'hidden',timeout:60000});
   await page.screenshot({path:'test-results/avery-saved.png',fullPage:false});

   stage='upc-test';
   const pos=await page.evaluate(()=>[...document.body.innerText.matchAll(/PO Number:\s*(PO-[0-9]+)/g)].map(x=>x[1]));
   if(pos.length!==1||pos[0]!==po)throw Error('Unexpected PO');
   const baseline=await page.locator('input[id^="oid_MSRP_IN_"],table[id*="_RFID_"] input._oiOrderedQty').evaluateAll(inputs=>inputs.map(x=>({id:x.id,value:x.value})));
   const tab=page.locator('a[id^="categoryDiv_order_"][id$="_UPCStickers"]');
   if(await tab.count()!==1)throw Error('Ambiguous UPC tab');
   await tab.click();
   const quantities=page.locator('table[id*="_UPCStickers_"] input._oiOrderedQty');
   if(await quantities.count()!==12)throw Error('Expected 12 sticker rows');
   for(const field of await quantities.all()){if(!await field.isVisible()||!await field.isEnabled())throw Error('Sticker field not editable');}
   for(const field of await quantities.all()){await field.fill('10');}
   await quantities.last().press('Tab');
   for(const field of await quantities.all())if(await field.inputValue()!=='10')throw Error('Quantity verification failed');
   const after=await page.locator('input[id^="oid_MSRP_IN_"],table[id*="_RFID_"] input._oiOrderedQty').evaluateAll(inputs=>inputs.map(x=>({id:x.id,value:x.value})));
   if(JSON.stringify(baseline)!==JSON.stringify(after))throw Error('RFID or MSRP changed');
   await page.screenshot({path:'test-results/avery-upc-ten.png',fullPage:false});
   require('fs').writeFileSync('test-results/avery-upc-ten.json',JSON.stringify({po,filled:12,quantity:10,rfidAndMsrpUnchanged:true,saveOrderClicked:false}));

 }
 await fillPo(initial.po,initial.rows);
 await page.screenshot({path:'test-results/avery-login.png',fullPage:false});
 console.log(JSON.stringify({status:'signed-in'}));
 let queue=Promise.resolve();
 commandInput.on('line',line=>{queue=queue.then(async()=>{try{const command=JSON.parse(line);await fillPo(command.po,command.rows);console.log(JSON.stringify({status:'po-filled'}));}catch(error){require('fs').writeFileSync('test-results/avery-failure.json',JSON.stringify({stage,reason:error.message,closed:page.isClosed(),time:new Date().toISOString()}));await page.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:'failed',stage}));}})});
 // Keep the authenticated window available to the user. Never click order actions.
 const keep=setInterval(()=>{},30000);browser.on('disconnected',()=>{clearInterval(keep);process.exit(0)});
})().catch(async(error)=>{require("fs").writeFileSync("test-results/avery-failure.json",JSON.stringify({stage,reason:error.message,time:new Date().toISOString()}));if(activePage)await activePage.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:'failed',stage}));process.exit(1)});
