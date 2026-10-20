const {chromium}=require('playwright');
const assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const context=await browser.newContext();let writes=0;let existing='';
await context.route('**/api/v1/**',async route=>{const req=route.request(),url=req.url();let data={};
if(req.method()==='POST'){assert.match(url,/importaciones\/ordenes\/002\/999001/);writes++;data={number:'20269999'};existing='20269999';}
else if(url.endsWith('/sesion'))data={displayName:'Prueba',gestionUser:'test',demo:false,canWriteDatabase:true};
else if(url.endsWith('/catalogos'))data=[{Kind:'itemTypes',Code:'371',Name:'Avíos'},{Kind:'origins',Code:'HK',Name:'Hong Kong'},{Kind:'areas',Code:'05',Name:'Logística'}];
else if(url.includes('/importaciones/ordenes/'))data={automaticNumbering:true,validAnnex:true,source:{supplierCode:'TEST',supplier:'Proveedor ficticio',annexType:'P',annex:'4815',currency:'USD',existingImport:existing,supplierOrigin:'E'}};
else if(url.includes('/importaciones?'))data={rows:existing?[{number:existing,supplier:'Proveedor ficticio',status:'R',currency:'USD'}]:[],hasMore:false,page:1};
else throw Error('Petición inesperada '+url);
await route.fulfill({json:data,headers:{'Access-Control-Allow-Origin':'http://localhost:4200','Access-Control-Allow-Credentials':'true'}});});
const page=await context.newPage();let errors=[];page.on('pageerror',e=>errors.push(e.message));
await page.goto('http://localhost:4200/logistica/importaciones?series=002&number=999001');
await page.getByRole('button',{name:'Consultar OC',exact:true}).click();
await page.locator('select[name=itemType]').selectOption('371');await page.locator('select[name=origin]').selectOption('HK');
await page.getByRole('button',{name:'Revisar creación'}).click();assert.equal(writes,0);
await page.getByRole('button',{name:'Crear y asociar importación'}).click();await page.getByRole('status').filter({hasText:'20269999'}).waitFor();assert.equal(writes,1);
await page.getByRole('heading',{name:'Listado de importaciones'}).waitFor();await page.getByRole('button',{name:'Nueva importación'}).click();await page.locator('input[name=number]').fill('999001');await page.getByRole('button',{name:'Consultar OC',exact:true}).click();await page.getByRole('dialog').waitFor();assert.equal(await page.getByRole('heading',{name:'Datos de la OC 002-999001'}).count(),0);await page.getByRole('button',{name:'Entendido',exact:true}).click();await page.getByRole('heading',{name:'Listado de importaciones'}).waitFor();
assert.equal(await page.getByRole('button',{name:'Crear y asociar importación'}).count(),0);
await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
assert.deepEqual(errors,[]);console.log('PASS: precarga, revisión, POST interceptado, bloqueo duplicado y móvil. Sin conexión SQL de escritura.');await browser.close();})().catch(e=>{console.error(e);process.exit(1)});

