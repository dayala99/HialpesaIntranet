const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
(async()=>{
 const root=path.resolve(__dirname,'../dist/intranet-web/browser');
 const server=http.createServer((req,res)=>{const pathname=new URL(req.url,'http://localhost').pathname.replace(/^\/FrontEnd\//,'');const file=path.resolve(root,pathname||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(404).end();return;}const target=fs.existsSync(file)&&fs.statSync(file).isFile()?file:path.join(root,'index.html');res.setHeader('Content-Type',target.endsWith('.js')?'application/javascript':target.endsWith('.css')?'text/css':'text/html');fs.createReadStream(target).pipe(res);});
 await new Promise(resolve=>server.listen(4212,'127.0.0.1',resolve));const browser=await chromium.launch({channel:'chrome',headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:900}}),calls=[];let downloads=0,received=false;page.on('download',()=>downloads++);
 await page.addInitScript(()=>{window.vinceLinks=[];window.open=url=>{window.vinceLinks.push(url);return null;};});
 await page.route('**/BackEnd/api/v1/**',async route=>{
 const url=route.request().url();let data;
 if(url.endsWith('/sesion'))data={displayName:'Prueba',gestionUser:'test',canWriteDatabase:true,demo:false};
 else if(url.includes('/ordenes?'))data={rows:[],page:1,totalPages:1,totalRows:0,hasMore:false};
 else if(url.endsWith('/registro/catalogos'))data={conditions:[{code:'53',description:'CREDITO-WIRE TRANSFER 30 DAYS'},{code:'001',description:'CONTADO'}],discounts:[{code:'01',description:'0.00 - 0.00'}],currencies:[],deliveryLocations:[],costCenters:[]};
 else if(url.endsWith('/registro/fabricas'))data=[{code:'HIA',description:'HIALPESA'}];
 else if(url.includes('/registro/grupos-por-op?')){assert.equal(new URL(url).searchParams.get('factory'),'HIA');data=[new URL(url).searchParams.get('productionOrder')==='J2451'?{code:'VCW-684',description:'J2451/52/54'}:{code:'VCW-685',description:'J2455'}];}
 else if(url.includes('/registro/proveedores/')&&url.endsWith('/valores'))data=url.includes('/000000000281/')?{conditionCode:'53',discountCode:'01',origin:'E',taxPercent:0}:{conditionCode:'001',discountCode:'01',origin:'N',taxPercent:18};
 else if(url.includes('/registro/catalogos/proveedores')){calls.push(new URL(url).searchParams.get('search'));data=[{code:'000000000281',description:'AVERY DENNISON HONG KONG B.V.'},{code:'000000000001',description:'PROVEEDOR NACIONAL'}];}
 else if(url.includes('/registro/catalogos/grupos'))data=[];
 else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');
 await page.getByRole('button',{name:'Nueva orden de compra',exact:true}).click();
 const input=page.getByRole('combobox',{name:'Proveedor',exact:true});await input.fill('AVERY');await page.getByRole('option',{name:/AVERY DENNISON/}).click();
 await page.waitForFunction(()=>document.querySelector('input[name=tax]')?.value==='0');
 assert.equal(await page.getByRole('combobox',{name:'Condición de venta',exact:true}).inputValue(),'CREDITO-WIRE TRANSFER 30 DAYS');assert.equal(await page.getByRole('combobox',{name:'Descuento',exact:true}).inputValue(),'0.00 - 0.00');
 await input.fill('NACIONAL');await page.getByRole('option',{name:/PROVEEDOR NACIONAL/}).click();await page.waitForFunction(()=>document.querySelector('input[name=tax]')?.value==='18');assert.equal(await page.getByRole('combobox',{name:'Condición de venta',exact:true}).inputValue(),'CONTADO');
 await input.fill('CAMBIO');await page.waitForFunction(()=>document.querySelector('input[name=tax]')?.value==='');assert.equal(await page.getByRole('combobox',{name:'Condición de venta',exact:true}).inputValue(),'');console.log('Proveedor: autocompleta condición/descuento; extranjero IGV 0, nacional 18; al cambiar limpia valores anteriores. Sin guardar OC.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
