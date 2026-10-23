const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
(async()=>{
 const root=path.resolve(__dirname,'../dist/intranet-web/browser');
 const server=http.createServer((req,res)=>{const pathname=new URL(req.url,'http://localhost').pathname.replace(/^\/FrontEnd\//,'');const file=path.resolve(root,pathname||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(404).end();return;}const target=fs.existsSync(file)&&fs.statSync(file).isFile()?file:path.join(root,'index.html');res.setHeader('Content-Type',target.endsWith('.js')?'application/javascript':target.endsWith('.css')?'text/css':'text/html');fs.createReadStream(target).pipe(res);});
 await new Promise(resolve=>server.listen(4212,'127.0.0.1',resolve));const browser=await chromium.launch({channel:'chrome',headless:true});
 try{

 const page=await browser.newPage({viewport:{width:1440,height:1000}});const calls=[],errors=[];let pending=false;
 page.on('pageerror',e=>errors.push(e.message));
 await page.route('**/BackEnd/api/v1/**',async route=>{
  assert.equal(route.request().method(),'GET');const u=new URL(route.request().url());
  if(u.pathname.endsWith('/sesion'))return route.fulfill({json:{displayName:'Prueba',gestionUser:'test',demo:false,isGlobalUser:true,allowedOptions:[],profiles:[],canWriteDatabase:false}});
  if(u.pathname.endsWith('/catalogos'))return route.fulfill({json:[{kind:'clients',code:'00001',description:'CLIENTE PRUEBA'},{kind:'factories',code:'HIA',description:'HIALPESA'},{kind:'statuses',code:'P',description:'Pendiente'},{kind:'fibers',code:'A',description:'Algodón'}]});
  assert.ok(u.pathname.endsWith('/tintoreria/partidas'));calls.push(Object.fromEntries(u.searchParams));
  if(pending)return route.fulfill({status:409,json:{title:'El filtro está pendiente de habilitación: falta crear PA_TI_ORDTRA_TINTORERIA_ITEMS_S0001 en HIALPESA.'}});
  const mode=u.searchParams.get('mode'),p=Number(u.searchParams.get('page'));
  const row={cod_ordtra:'99537',num_secuencia:1,np:'J2111',tela:'TE0001',nombre:'Tela prueba',color:'BLANCO',kgs_progr:120.5,status:'P-Pendiente',clase_oc:'01-Producción'};
  if(mode==='4'){row.grupo='GRUPO PRUEBA';row.cliente_manuf='CLIENTE MANUFACTURA';}
  else row.fec_creacion='2026-10-22T09:30:00';
  return route.fulfill({json:{rows:[row],page:p,pageSize:50,totalRows:51}});
 });
 await page.goto('http://127.0.0.1:4212/FrontEnd/tintoreria/generacion-partidas');
 await page.getByLabel('Número de OT',{exact:true}).fill('99537');await page.getByRole('button',{name:'Buscar',exact:true}).click();
 await page.getByRole('button',{name:'Seleccionar partida 99537 secuencia 1'}).click();
 await page.getByText('01-Producción',{exact:true}).first().waitFor();assert.equal(calls[0].mode,'4');
 await page.getByRole('columnheader',{name:'grupo',exact:true}).waitFor();
 await page.getByRole('button',{name:'Siguiente',exact:true}).click();await page.getByText('Página 2 de 2',{exact:true}).waitFor();assert.equal(calls[1].page,'2');assert.equal(calls[1].batch,'99537');
 await page.getByLabel('Por cliente',{exact:true}).check();await page.locator('select[name=client]').selectOption('00001');
 await page.getByRole('button',{name:'Buscar',exact:true}).click();await page.getByRole('columnheader',{name:'Fecha',exact:true}).waitFor();assert.equal(calls.at(-1).mode,'2');assert.equal(await page.getByRole('columnheader',{name:'grupo',exact:true}).count(),0);
 await page.locator('select[name=clientMode]').selectOption({label:'Orden de compra'});await page.getByLabel('Serie OC',{exact:true}).fill('002');await page.getByLabel('Número OC',{exact:true}).fill('350136');await page.getByRole('button',{name:'Buscar',exact:true}).click();await page.getByRole('columnheader',{name:'Fecha',exact:true}).waitFor();assert.equal(calls.at(-1).mode,'1');
 await page.locator('select[name=clientMode]').selectOption({label:'Fechas de creación'});await page.locator('input[name=from]').fill('2026-10-22');await page.locator('input[name=to]').fill('2026-10-21');let count=calls.length;await page.getByRole('button',{name:'Buscar',exact:true}).click();await page.getByText('Revisa el rango de fechas.',{exact:true}).waitFor();assert.equal(calls.length,count);
 await page.locator('input[name=to]').fill('2026-10-22');await page.getByRole('button',{name:'Buscar',exact:true}).click();await page.getByRole('columnheader',{name:'Fecha',exact:true}).waitFor();assert.equal(calls.at(-1).mode,'3');
 for(const [label,input,value,mode] of [['Por estado','Estado','P','5'],['Por N/P','N/P','J2111','6'],['Por fibra','Fibra','A','7']]){
  await page.getByLabel(label,{exact:true}).check();if(mode==='6')await page.getByLabel(input,{exact:true}).fill(value);else await page.locator('select[name='+ (mode==='5'?'status':'fiber') +']').selectOption(value);
  await page.getByRole('button',{name:'Buscar',exact:true}).click();await page.getByRole('columnheader',{name:'Fecha',exact:true}).waitFor();assert.equal(calls.at(-1).mode,mode);
 }
 await page.screenshot({path:path.join(__dirname,'../test-results/partidas-filter-desktop.png'),fullPage:true});
 await page.setViewportSize({width:390,height:900});await page.locator('#main-navigation').getByRole('button',{name:'Ocultar navegación',exact:true}).click();await page.locator('#main-navigation').waitFor({state:'hidden'});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);await page.screenshot({path:path.join(__dirname,'../test-results/partidas-filter-mobile.png'),fullPage:true});
 pending=true;await page.getByRole('button',{name:'Buscar',exact:true}).click();await page.getByRole('alert').filter({hasText:'pendiente de habilitación'}).waitFor();assert.equal(await page.locator('tbody tr').count(),0);
 assert.deepEqual(errors,[]);console.log('Partidas: 7 filtros, columnas por respuesta, fechas, paginación, selección y móvil verificados. Solo API simulada.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
