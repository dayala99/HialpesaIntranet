const {chromium}=require('@playwright/test');
const {existsSync}=require('node:fs');
const fs=require('node:fs/promises');
const path=require('node:path');
const assert=require('node:assert/strict');

(async()=>{
 const executablePath=[process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,'C:/Program Files/Google/Chrome/Application/chrome.exe','C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p=>p&&existsSync(p));
 const browser=await chromium.launch({headless:true,executablePath});
 const output=path.join(__dirname,'..','test-results');await fs.mkdir(output,{recursive:true});
 const reference={series:'002',number:'999001'};
 const order={...reference,supplier:'PROVEEDOR FICTICIO DE PRUEBA',status:'P',class:'01',client:'CLIENTE FICTICIO',createdAt:'2026-10-16T09:00:00'};
 let created=0,saved=0;let items=[];const unexpected=[];
 const option={code:'001',description:'Opción ficticia'};
 const candidates=[11,16,28].map((required,i)=>({key:'fake-key-'+i,factory:'001',productionOrder:'J9999',presentationCode:1,presentation:'Negro',component:'PT',itemCode:'PT000574',description:'Artículo ficticio',combination:'',color:'',size:['S','M','L'][i],destination:'',style:'EST-TEST',providerReference:'',required,purchase:[15,20,30][i],unitPrice:2.5}));
 try{
  const context=await browser.newContext({viewport:{width:1440,height:1100},reducedMotion:'reduce'});
  // Every API request is intercepted. No registration request can reach a database.
  await context.route('**/api/v1/**',async route=>{
   const request=route.request(),url=new URL(request.url()),p=url.pathname,method=request.method();
   const respond=(body,status=200)=>route.fulfill({status,contentType:'application/json',body:JSON.stringify(body),headers:{'Access-Control-Allow-Origin':'http://localhost:4200','Access-Control-Allow-Credentials':'true'}});
   if(method==='OPTIONS')return route.fulfill({status:204,headers:{'Access-Control-Allow-Origin':'http://localhost:4200','Access-Control-Allow-Credentials':'true','Access-Control-Allow-Headers':'content-type,x-intranet-write','Access-Control-Allow-Methods':'GET,POST,OPTIONS'}});
   if(p.endsWith('/sesion'))return respond({displayName:'USUARIO FICTICIO',gestionUser:'test',demo:false,canWriteDatabase:true});
   if(p.endsWith('/registro/catalogos'))return respond({conditions:[option],discounts:[option],currencies:[{code:'USD',description:'Dólar'}],deliveryLocations:[option],costCenters:[option]});
   if(method==='POST'&&p.endsWith('/avios/ordenes')){
    const data=request.postDataJSON();assert.equal(data.manufacturingGroup,'GRP-TEST');assert.equal(data.supplierCode,'000000000001');assert.equal(request.headers()['x-intranet-write'],'1');created++;return respond(reference,201);
   }
   if(p.endsWith('/pendientes'))return respond(candidates);
   if(method==='POST'&&p.endsWith('/detalles')){
    assert.equal(created,1);const data=request.postDataJSON();assert.equal(data.productionOrder,'J9999');assert.equal(data.itemCode,'PT000574');assert.equal(data.unitPrice,3.25);assert.deepEqual(data.requirementKeys,['fake-key-0','fake-key-1','fake-key-2']);
    assert.equal(request.headers()['x-intranet-write'],'1');saved++;
    items=candidates.map((r,i)=>({sequence:String(i+1).padStart(3,'0'),itemCode:r.itemCode,description:r.description,size:r.size,style:r.style,required:r.required,purchased:r.purchase,received:0}));
    return respond({...reference,requirementsAdded:3,itemsAdded:3,requiredTotal:55,purchaseTotal:65});
   }
   if(p.endsWith('/impresion'))return respond({order,items,currency:'USD',conditions:'Pago ficticio',deliveryLocation:'Lugar ficticio',manufacturingGroup:'GRP-TEST',observations:'DOCUMENTO DE PRUEBA AISLADA. NO REGISTRADO EN HIALPESA.',taxPercent:18,deliveryStart:'2026-10-20',deliveryEnd:'2026-10-25',lines:items.map(r=>({sequence:r.sequence,code:r.itemCode,description:r.description,size:r.size,quantity:r.purchased,unitPrice:3.25,amount:r.purchased*3.25}))});
   if(p.endsWith('/ordenes/002/999001'))return respond({order,items});
   unexpected.push(method+' '+p);return respond({title:'API no simulada; solicitud bloqueada.'},500);
  });
  const page=await context.newPage();page.setDefaultTimeout(15000);
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://localhost:4200/logistica/avios');
  await page.getByRole('button',{name:'+ Nueva OC',exact:true}).click();
  await page.locator('app-order-registration').waitFor();

  await page.getByLabel('Condición de pago',{exact:true}).selectOption('001');
  await page.getByLabel('Código de proveedor',{exact:true}).fill('000000000001');
  await page.getByLabel('Descuento',{exact:true}).selectOption('001');
  await page.getByLabel('Moneda',{exact:true}).selectOption('USD');
  await page.getByLabel('Lugar de entrega',{exact:true}).selectOption('001');
  await page.getByLabel('Centro de costo',{exact:true}).selectOption('001');
  await page.getByLabel('Grupo de manufactura',{exact:true}).fill('GRP-TEST');
  await page.getByLabel('Observaciones',{exact:true}).fill('PRUEBA AISLADA');
  assert.equal(created,0);assert.equal(saved,0);
  await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:path.join(output,'registro-cabecera.png'),fullPage:true});
  await page.getByRole('button',{name:'Guardar OC y grupo',exact:true}).click();
  await page.getByLabel('OP',{exact:true}).waitFor();assert.equal(created,1);
  await page.getByLabel('OP',{exact:true}).fill('J9999');await page.getByLabel('Ítem',{exact:true}).fill('PT000574');
  await page.getByRole('button',{name:'Consultar requerimientos',exact:true}).click();
  await page.getByLabel('Precio unitario',{exact:true}).fill('3.25');
  await page.getByText('3 requerimientos seleccionados',{exact:true}).waitFor();
  assert.equal(saved,0);
  for(const width of [320,390,768,1440]){await page.setViewportSize({width,height:1000});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,'Desbordamiento a '+width);}
  await page.setViewportSize({width:1440,height:1100});await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:path.join(output,'registro-detalle.png'),fullPage:true});
  await page.getByRole('button',{name:'Guardar detalle en OC',exact:true}).click();
  await page.getByText('Detalle registrado:',{exact:false}).waitFor();assert.equal(saved,1);
  await page.getByRole('button',{name:'Cerrar registro',exact:true}).click();
  await page.getByRole('button',{name:'Imprimir OC',exact:true}).click();
  await page.getByRole('article',{name:'Orden de compra para impresión'}).waitFor();
  await page.getByText('211.25',{exact:false}).waitFor();
  await page.emulateMedia({media:'print'});await page.pdf({path:path.join(output,'oc-ficticia-impresion.pdf'),format:'A4',printBackground:true});
  assert.equal(await page.locator('.avios-workspace').isVisible(),false);
  assert.deepEqual(unexpected,[]);assert.deepEqual(errors,[]);
  console.log('OK AISLADO: nueva OC + grupo, detalle por OP/ítem, selección, redondeo, precio y PDF. Todas las APIs simuladas; cero conexiones de escritura a SQL.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
