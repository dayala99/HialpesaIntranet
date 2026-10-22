const {quantityInput}=require('./provider-fields.cjs');
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
 require('./provider-lifecycle.cjs').bindBrowserLifetime(browser,context,()=>{commandInput.close();process.exit(0);});
 let page=await context.newPage();activePage=page;
 let searchUrl=null;let selectedPoPage=null;
 const isRetail=url=>url.protocol==='https:'&&['www.retailcompliance.averydennison.com','retailcompliance.averydennison.com'].includes(url.hostname);
 const loginUrl=process.env.AVERY_LOGIN_URL||'https://webservices.averydennison.com/specstar/login.aspx';delete process.env.AVERY_LOGIN_URL;
 if(loginUrl!=='https://webservices.averydennison.com/specstar/login.aspx')throw Error('destination');
 const {authenticate}=require('./provider-auth.cjs');
 async function signIn(){
  await authenticate(page,{username,password,loginUrl,setStage:value=>{stage=value;}});
  searchUrl=page.url();
 }
 async function fillPo(po,rows,colorConfirmations=[],resumeCurrent=false,mode){
   if(!/^PO-[0-9]{1,20}$/.test(po))throw Error('po');
   if(resumeCurrent){
    if(page.isClosed()||!selectedPoPage||selectedPoPage.po!==po||selectedPoPage.url!==page.url()||!isRetail(new URL(page.url())))throw new MatchError('La ventana del PO cambió durante la confirmación. Vuelve a consultar el PO; no se cambiaron cantidades.');
   }else{
   stage='session';
   // A draft's Home link can lead to the login page. Reuse the search URL
   // observed after authentication, and recover an expired session once.
   if(page.isClosed()){page=await context.newPage();activePage=page;}
   if(!searchUrl)await signIn();
   let current=new URL(page.url());
   if(!isRetail(current)||!current.pathname.endsWith('/OrderSetup.html')){
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
   await require('./provider-order-setup.cjs').openSelectedPo(page,po,value=>{stage=value;});
   await page.locator('input._oiOrderedQty:not([id^="oid_MSRP_IN_"])').first().waitFor({state:'attached',timeout:60000});
   selectedPoPage={po,url:page.url()};
   }
   stage='quantity-matching';
   const snapshot=await page.evaluate(()=>{
    const pos=[...document.body.innerText.matchAll(/PO Number:\s*(PO-[0-9]+)/g)].map(x=>x[1]);
    const rows=Array.from(document.querySelectorAll('input._oiOrderedQty:not([id^="oid_MSRP_IN_"] )')).map(q=>{
     const tr=q.closest('tr');const poText=q.closest('td')?.previousElementSibling?.textContent?.trim().replace(/,/g,'')||'';const poQuantity=/^\d+(\.\d+)?$/.test(poText)?Number(poText):null;const val=prefix=>tr.querySelector('input[id^="'+prefix+'"]')?.value?.trim()||'';
     return {id:q.id,poQuantity,kind:tr.querySelector('input[id^="oid_MSRP_IN_"]')?'RFID':'UPC',style:val('oid_STYLE_IN_'),size:val('oid_SIZECODE_IN_'),colorCode:val('oid_COLORCODE_IN_'),color:val('oid_COLORDESC_IN_'),barcode:val('oid_BARCODE_IN_'),quantity:q.value};
    });return {pos,rows};
   });
   if(snapshot.pos.length!==1||snapshot.pos[0]!==po)throw new MatchError('PO seleccionado: '+po+'. PO encontrados en el proveedor: '+snapshot.pos.join(', ')+'. Revisa el pedido abierto; no se cambiaron cantidades.');
   if(mode==='preserve-provider'){
    if(!snapshot.rows.length||!snapshot.rows.some(r=>r.kind==='RFID')||!snapshot.rows.some(r=>r.kind==='UPC'))throw new MatchError('No se encontraron las tablas RFID y UPC Stickers para '+po+'.');
    require('fs').writeFileSync('test-results/avery-quantities.json',JSON.stringify({po,mode,filled:0,saved:false}));
    return;
   }
   const plans=planQuantities(rows,snapshot.rows,po,colorConfirmations);
   const msrpBefore=await page.locator('input[id^="oid_MSRP_IN_"]').evaluateAll(inputs=>inputs.map(x=>({id:x.id,value:x.value})));
   // Validate all matches and editable fields before changing the first value.
   for(const plan of plans){const input=quantityInput(page,plan.id);if(await input.count()!==1||!await input.isEnabled()||await input.getAttribute('readonly')!==null)throw Error('Quantity not editable');}
   stage='quantity-fill';
   for(const kind of ['RFID','UPC']){
     const tab=page.locator('a[id^="categoryDiv_order_"][id$="_'+(kind==='RFID'?'RFID':'UPCStickers')+'"]');
     if(await tab.count()!==1)throw Error('Missing or ambiguous category tab');
     await tab.click();
     for(const plan of plans.filter(x=>x.kind===kind)){
       const input=quantityInput(page,plan.id);
       if(!await input.isVisible())throw Error('Quantity tab not visible');
       await input.fill(plan.value);
       if(await input.inputValue()!==plan.value)throw Error('Quantity verification failed');
     }
     await page.screenshot({path:'test-results/avery-quantities-'+kind+'.png',fullPage:false});
   }
   for(const plan of plans)if(await quantityInput(page,plan.id).inputValue()!==plan.value)throw Error('Quantity changed after tab switch');
   const msrpAfter=await page.locator('input[id^="oid_MSRP_IN_"]').evaluateAll(inputs=>inputs.map(x=>({id:x.id,value:x.value})));
   if(JSON.stringify(msrpBefore)!==JSON.stringify(msrpAfter))throw Error('MSRP changed unexpectedly');
   require('fs').writeFileSync('test-results/avery-quantities.json',JSON.stringify({po,filled:plans.length,saved:false,rows:plans},null,2));


 }
 async function runFill(command){
  const approved=[...(command.colorConfirmations||[])];let resumeCurrent=false;
  for(let attempt=0;attempt<7;attempt++){
   try{await fillPo(command.po,command.rows,approved,resumeCurrent,command.mode);if(['po-required','preserve-provider'].includes(command.mode)||!command.series||!command.number){stage='po-without-oc-complete';console.log(JSON.stringify({status:'awaiting-completion-acknowledgment'}));await require('./provider-color-dialog.cjs').showCompleted(page);return true;}const shipping=await require('./provider-shipping.cjs').fillShipping(page,command,value=>{stage=value;});require('fs').writeFileSync('test-results/avery-shipping.json',JSON.stringify(shipping,null,2));await require('./provider-preview.cjs').openPreview(page,command,value=>{stage=value;});return true;}catch(error){
    if(error.code==='MATCH_ERROR'&&process.env.AVERY_COLOR_CONFIRMATION_UI==='browser'){
     console.log(JSON.stringify({status:'awaiting-mismatch-acknowledgment'}));
     await require('./provider-color-dialog.cjs').showMismatch(page,command.po,error.message);error.displayedInBrowser=true;throw error;
    }
    if(!['COLOR_CONFIRMATION','COLOR_SELECTION'].includes(error.code)||process.env.AVERY_COLOR_CONFIRMATION_UI!=='browser')throw error;
    console.log(JSON.stringify({status:'awaiting-color-confirmation',count:error.confirmations.length}));
    const accepted=await require('./provider-color-dialog.cjs').confirmColors(page,command.po,error.confirmations);
    if(!accepted){console.log(JSON.stringify({status:'cancelled',stage:'color-confirmation'}));return false;}
    console.log(JSON.stringify({status:'color-confirmed'}));approved.push(...(Array.isArray(accepted)?accepted:error.confirmations));resumeCurrent=true;
   }
  }
  throw new Error('Las equivalencias cambiaron repetidamente. Vuelve a consultar el PO.');
 }
 try{
  if(await runFill(initial)){
  await page.screenshot({path:'test-results/avery-login.png',fullPage:false});
  console.log(JSON.stringify({status:'signed-in'}));}
 }catch(error){
  require('fs').writeFileSync('test-results/avery-failure.json',JSON.stringify({stage,reason:error.message,time:new Date().toISOString()}));
  await page.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});
  console.log(JSON.stringify({status:error.code==='COLOR_CONFIRMATION'?'confirmation-required':'failed',stage,confirmations:error.code==='COLOR_CONFIRMATION'?error.confirmations:undefined,detail:error.code==='MATCH_ERROR'?error.message:undefined,displayedInBrowser:error.displayedInBrowser===true}));
 }
 let queue=Promise.resolve();
 commandInput.on('line',line=>{queue=queue.then(async()=>{try{const command=JSON.parse(line);if(await runFill(command))console.log(JSON.stringify({status:'po-filled'}));}catch(error){require('fs').writeFileSync('test-results/avery-failure.json',JSON.stringify({stage,reason:error.message,closed:page.isClosed(),time:new Date().toISOString()}));await page.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:error.code==='COLOR_CONFIRMATION'?'confirmation-required':'failed',stage,confirmations:error.code==='COLOR_CONFIRMATION'?error.confirmations:undefined,detail:error.code==='MATCH_ERROR'?error.message:undefined,displayedInBrowser:error.displayedInBrowser===true}));}})});
 // Keep the authenticated window available to the user. Never click Save Order or submit the order.

})().catch(async(error)=>{require("fs").writeFileSync("test-results/avery-failure.json",JSON.stringify({stage,reason:error.message,time:new Date().toISOString()}));if(activePage)await activePage.screenshot({path:'test-results/avery-login-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:error.code==='COLOR_CONFIRMATION'?'confirmation-required':'failed',stage,confirmations:error.code==='COLOR_CONFIRMATION'?error.confirmations:undefined,detail:error.code==='MATCH_ERROR'?error.message:undefined,displayedInBrowser:error.displayedInBrowser===true}));process.exit(1)});






