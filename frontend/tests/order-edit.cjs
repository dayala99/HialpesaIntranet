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
 else if(url.includes('/ordenes?'))data={rows:[{series:'002',number:'350118',class:'01',status:'P',supplier:'Test'}],page:1,totalPages:1,totalRows:1,hasMore:false};
 else if(url.endsWith('/ordenes/002/350118/grupos')){if(route.request().method()==='GET')data=[{code:'VCW-685',description:''}];else{calls.push(route.request().postDataJSON());data={};}}
 else if(url.endsWith('/ordenes/002/350118/edicion'))data={header:{supplierCode:'000000000281',conditionCode:'001',discountCode:'01',currencyCode:'USD',deliveryLocationCode:'001',costCenterCode:'100',taxPercent:18,deliveryStart:'2026-10-21T00:00:00',deliveryEnd:'2026-10-22T00:00:00',observations:'Original'},supplierName:'Proveedor'};
 else if(url.endsWith('/ordenes/002/350118')&&route.request().method()==='PUT'){calls.push(route.request().postDataJSON());data={};}
 else if(url.endsWith('/registro/catalogos'))data={conditions:[],discounts:[],currencies:[],deliveryLocations:[],costCenters:[]};
 else if(url.endsWith('/registro/fabricas'))data=[{code:'HIA',description:'HIALPESA'}];
 else if(url.includes('/registro/grupos-por-op?')){assert.equal(new URL(url).searchParams.get('factory'),'HIA');data=[new URL(url).searchParams.get('productionOrder')==='J2451'?{code:'VCW-684',description:'J2451/52/54'}:{code:'VCW-685',description:'J2455'}];}
 else if(url.includes('/registro/catalogos/grupos'))data=[];
 else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');
 await page.getByRole('button',{name:'Editar OC 002-350118',exact:true}).click();
 await page.getByLabel('Observaciones',{exact:true}).fill('Observación modificada');
 await page.getByRole('button',{name:'Guardar cambios',exact:true}).click();
 await page.getByText('OC 002-350118 actualizada correctamente.',{exact:true}).waitFor();
 assert.equal(calls.length,1);assert.equal(calls[0].observations,'Observación modificada');assert.equal(calls[0].supplierCode,'000000000281');assert.equal(calls[0].deliveryStart,'2026-10-21');assert.equal(calls[0].currencyCode,'USD');
 console.log('Edición OC: carga cabecera, cambia observaciones y envía PUT a la misma OC; sin crear otra OC ni escrituras reales.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
