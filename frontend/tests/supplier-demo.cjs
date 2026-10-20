const {chromium}=require('@playwright/test');
const {existsSync}=require('node:fs');
const fs=require('node:fs/promises');
const path=require('node:path');
const assert=require('node:assert/strict');

(async()=>{
 const executablePath=[process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,'C:/Program Files/Google/Chrome/Application/chrome.exe','C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p=>p&&existsSync(p));
 const browser=await chromium.launch({headless:true,executablePath});
 const order={series:'002',number:'999002',supplier:'PROVEEDOR FICTICIO · PRUEBA LOCAL',status:'P',class:'01',client:'Cliente ficticio',createdAt:'2026-10-19T09:00:00'};
 const items=Array.from({length:298},(_,i)=>({sequence:String(i+1).padStart(3,'0'),itemCode:i%2?'ST-PRUEBA':'PT-PRUEBA',description:'Artículo de prueba <sin HTML>',size:['S','M','L'][i%3],style:'EST-'+(i%4),required:i===0?11.44:i===1?0:i===2?20:i===3?0.00001:Number(((i%30)+0.24).toFixed(2)),purchased:1000+i,received:0}));
 const apiCalls=[];
 try{
  const context=await browser.newContext({viewport:{width:1440,height:1000},reducedMotion:'reduce'});
  await context.route('**/api/v1/**',async route=>{
   const request=route.request();apiCalls.push(request.method()+' '+new URL(request.url()).pathname);
   const respond=body=>route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(body),headers:{'Access-Control-Allow-Origin':'http://localhost:4200','Access-Control-Allow-Credentials':'true'}});
   if(request.method()!=='GET')return route.abort();
   if(request.url().endsWith('/sesion'))return respond({displayName:'Usuario ficticio',gestionUser:'test',demo:false,canWriteDatabase:false});
   if(request.url().endsWith('/ordenes/002/999002'))return respond({order,items});
   return route.fulfill({status:404,contentType:'application/json',body:'{}'});
  });
  const page=await context.newPage();page.setDefaultTimeout(15000);
  const errors=[];context.on('page',p=>p.on('pageerror',e=>errors.push(e.message)));page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://localhost:4200/logistica/avios');
  await page.getByLabel('Serie',{exact:true}).fill('002');await page.getByLabel('Número de OC',{exact:true}).fill('999002');
  await page.locator('.search-form button[type=submit]').click();await page.getByText('298 secuencias').waitFor();
  const popupPromise=page.waitForEvent('popup');
  await page.getByRole('button',{name:'Probar llenado en página nueva',exact:true}).click();
  const popup=await popupPromise;popup.setDefaultTimeout(15000);
  await popup.waitForLoadState('domcontentloaded');
  assert.match(popup.url(),/\/prueba-proveedor\.html#/);
  await popup.getByRole('status').filter({hasText:'Llenado completo: 298 campos'}).waitFor();
  const values=await popup.locator('input[type=number]').evaluateAll(inputs=>inputs.map(input=>({sequence:input.dataset.sequence,value:input.valueAsNumber})));
  assert.deepEqual(values,items.map(item=>({sequence:item.sequence,value:item.required})));
  assert.notEqual(values[0].value,items[0].purchased);
  assert.equal(values[1].value,0);assert.equal(values[3].value,0.00001);
  assert.equal(await popup.locator('button[type=submit]').count(),0);
  await page.getByRole('status').filter({hasText:'298 cantidades requeridas completadas'}).waitFor();
  const output=path.join(__dirname,'..','test-results');await fs.mkdir(output,{recursive:true});
  await popup.screenshot({path:path.join(output,'proveedor-prueba-desktop.png'),fullPage:false});
  await popup.getByRole('button',{name:'Vaciar campos',exact:true}).click();
  assert.equal(await popup.locator('input[type=number]').evaluateAll(inputs=>inputs.every(input=>input.value==='')),true);
  await popup.getByRole('button',{name:'Repetir llenado automático',exact:true}).click();
  await popup.getByRole('status').filter({hasText:'Llenado completo: 298 campos'}).waitFor();
  assert.deepEqual(await popup.locator('input[type=number]').evaluateAll(inputs=>inputs.map(input=>input.valueAsNumber)),items.map(item=>item.required));
  await popup.setViewportSize({width:390,height:950});assert.equal(await popup.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await popup.screenshot({path:path.join(output,'proveedor-prueba-mobile.png'),fullPage:false});
  const empty=await context.newPage();await empty.goto('http://localhost:4200/prueba-proveedor.html');
  await empty.getByRole('status').filter({hasText:'Página en blanco de prueba'}).waitFor();
  assert.equal(await empty.locator('input[type=number]').count(),0);
  assert.ok(apiCalls.every(call=>call.startsWith('GET ')));assert.deepEqual(errors,[]);
  console.log('OK: nueva pestaña, 298 cantidades requeridas exactas por secuencia, decimales y cero, vaciar/repetir, móvil y página sin datos. Solo consultas simuladas; sin pedidos ni SQL.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
