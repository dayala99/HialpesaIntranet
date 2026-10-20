const {chromium}=require('playwright');
const readline=require('node:readline');
let activePage;let stage="starting";
(async()=>{
 const username=process.env.AVERY_USERNAME,password=process.env.AVERY_PASSWORD;
 delete process.env.AVERY_USERNAME;delete process.env.AVERY_PASSWORD;
 if(!username||!password)throw Error('credentials');
 const commandInput=readline.createInterface({input:process.stdin});
 const initial=JSON.parse(await new Promise(resolve=>commandInput.once('line',resolve)));
 const {planQuantities,MatchError}=require('./provider-quantities.cjs');
 const browser=await chromium.launch({channel:'chrome',headless:false,args:['--start-maximized']});
 const context=await browser.newContext({viewport:null});
 let page=await context.newPage();activePage=page;
 let searchUrl=null;
 const isRetail=url=>url.protocol==='https:'&&['www.retailcompliance.averydennison.com','retailcompliance.averydennison.com'].includes(url.hostname);
 const loginUrl=process.env.AVERY_LOGIN_URL||'https://webservices.averydennison.com/specstar/login.aspx';delete process.env.AVERY_LOGIN_URL;
 if(loginUrl!=='https://webservices.averydennison.com/specstar/login.aspx')throw Error('destination');
 const {authenticate}=require('./provider-auth.cjs');
 async function signIn(){
  await authenticate(page,{username,password,loginUrl,setStage:value=>{stage=value;}});
  searchUrl=page.url();
 }
 async function fillPo(po,rows,colorConfirmations=[]){
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
   stage='quantity-matching';
   const snapshot=await page.evaluate(()=>{
    const pos=[...document.body.innerText.matchAll(/PO Number:\s*(PO-[0-9]+)/g)].map(x=>x[1]);
    const rows=Array.from(document.querySelectorAll('input._oiOrderedQty')).map(q=>{
     const tr=q.closest('tr');const val=prefix=>tr.querySelector('input[id^="'+prefix+'"]')?.value?.trim()||'';
     return {id:q.id,kind:tr.querySelector('input[id^="oid_MSRP_IN_"]')?'RFID':'UPC',style:val('oid_STYLE_IN_'),size:val('oid_SIZECODE_IN_'),colorCode:val('oid_COLORCODE_IN_'),color:val('oid_COLORDESC_IN_'),barcode:val('oid_BARCODE_IN_'),quantity:q.value};
    });return {pos,rows};
   });
   if(snapshot.pos.length!==1||snapshot.pos[0]!==po)throw new MatchError('PO seleccionado: '+po+'. PO encontrados en el proveedor: '+snapshot.pos.join(', ')+'. Revisa el pedido abierto; no se cambiaron cantidades.');
   const plans=planQuantities(rows,snapshot.rows,po,colorConfirmations);
   const msrpBefore=await page.locator('input[id^="oid_MSRP_IN_"]').evaluateAll(inputs=>inputs.map(x=>({id:x.id,value:x.value})));
   // Validate all matches and editable fields before changing the first value.
   for(const plan of plans){const input=page.locator('[id="'+plan.id+'"]');if(!await input.isEnabled()||await input.getAttribute('readonly')!==null)throw Error('Quantity not editable');}
   stage='quantity-fill';
   for(const kind of ['RFID','UPC']){
     const tab=page.locator('a[id^="categoryDiv_order_"][id$="_'+(kind==='RFID'?'RFID':'UPCStickers')+'"]');
     if(await tab.count()!==1)throw Error('Missing or ambiguous category tab');
     await tab.click();
     for(const plan of plans.filter(x=>x.kind===kind)){
       const input=page.locator('[id="'+plan.id+'"]');
       if(!await input.isVisible())throw Error('Quantity tab not visible');
       await input.fill(plan.value);
       if(await input.inputValue()!==plan.value)throw Error('Quantity verification failed');
     }
     await page.screenshot({path:'test-results/avery-quantities-'+kind+'.png',fullPage:false});
   }
   for(const plan of plans)if(await page.locator('[id="'+plan.id+'"]').inputValue()!==plan.value)throw Error('Quantity changed after tab switch');
   const msrpAfter=await page.locator('input[id^="oid_MSRP_IN_"]').evaluateAll(inputs=>inputs.map(x=>({id:x.id,value:x.value})));
   if(JSON.stringify(msrpBefore)!==JSON.stringify(msrpAfter))throw Error('MSRP changed unexpectedly');
   require('fs').writeFileSync('test-results/avery-quantities.json',JSON.stringify({po,filled:plans.length,saved:false,rows:plans},null,2));


 }
 try{
  await fillPo(initial.po,initial.rows,initial.colorConfirmations);
  await page.screenshot({path:'test-results/avery-login.png',fullPage:false});
  console.log(JSON.stringify({status:'signed-in'}));
 }catch(error){
  require('fs').writeFileSync('test-results/avery-failure.json',JSON.stringify({stage,reason:error.message,time:new Date().toISOString()}));
  await page.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});
  console.log(JSON.stringify({status:error.code==='COLOR_CONFIRMATION'?'confirmation-required':'failed',stage,confirmations:error.code==='COLOR_CONFIRMATION'?error.confirmations:undefined,detail:error.code==='MATCH_ERROR'?error.message:undefined}));
 }
 let queue=Promise.resolve();
 commandInput.on('line',line=>{queue=queue.then(async()=>{try{const command=JSON.parse(line);await fillPo(command.po,command.rows,command.colorConfirmations);console.log(JSON.stringify({status:'po-filled'}));}catch(error){require('fs').writeFileSync('test-results/avery-failure.json',JSON.stringify({stage,reason:error.message,closed:page.isClosed(),time:new Date().toISOString()}));await page.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:error.code==='COLOR_CONFIRMATION'?'confirmation-required':'failed',stage,confirmations:error.code==='COLOR_CONFIRMATION'?error.confirmations:undefined,detail:error.code==='MATCH_ERROR'?error.message:undefined}));}})});
 // Keep the authenticated window available to the user. Never click order actions.
 const keep=setInterval(()=>{},30000);browser.on('disconnected',()=>{clearInterval(keep);process.exit(0)});
})().catch(async(error)=>{require("fs").writeFileSync("test-results/avery-failure.json",JSON.stringify({stage,reason:error.message,time:new Date().toISOString()}));if(activePage)await activePage.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:error.code==='COLOR_CONFIRMATION'?'confirmation-required':'failed',stage,confirmations:error.code==='COLOR_CONFIRMATION'?error.confirmations:undefined,detail:error.code==='MATCH_ERROR'?error.message:undefined}));process.exit(1)});
