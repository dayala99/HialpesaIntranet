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
 if(url.endsWith('/sesion'))data={displayName:'Prueba',gestionUser:'test',isGlobalUser:true,allowedOptions:[],profiles:[],canWriteDatabase:true,demo:false};
 else if(url.includes('/ordenes?'))data={rows:[],page:1,totalPages:1,totalRows:0,hasMore:false};
 else if(url.endsWith('/registro/catalogos'))data={conditions:[{code:'001',description:'CONTADO'},{code:'002',description:'CRÉDITO 30 DÍAS'}],discounts:[{code:'000',description:'SIN DESCUENTO'}],currencies:[{code:'USD',description:'DÓLARES'}],deliveryLocations:[{code:'001',description:'ALMACÉN PRINCIPAL'}],costCenters:[{code:'100',description:'LOGÍSTICA'}]};
 else if(url.endsWith('/registro/fabricas'))data=[{code:'HIA',description:'HIALPESA'}];
 else if(url.includes('/registro/grupos-por-op?')){assert.equal(new URL(url).searchParams.get('factory'),'HIA');data=[new URL(url).searchParams.get('productionOrder')==='J2451'?{code:'VCW-684',description:'J2451/52/54'}:{code:'VCW-685',description:'J2455'}];}
 else if(url.includes('/registro/catalogos/proveedores')){calls.push(new URL(url).searchParams.get('search'));data=[{code:'000000000281',description:'AVERY DENNISON HONG KONG B.V.'}];}
 else if(url.includes('/registro/catalogos/grupos'))data=[];
 else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');
 await page.getByRole('button',{name:'Nueva orden de compra',exact:true}).click();
 const cases=[['Condición de pago','credito','002'],['Descuento','sin','000'],['Moneda','dolares','USD'],['Lugar de entrega','almacen','001'],['Centro de costo','logistica','100']];
 for(const [label,query,code] of cases){const combo=page.getByRole('combobox',{name:label,exact:true});await combo.fill(query);const list=page.getByRole('listbox',{name:label,exact:true});await list.getByRole('option').waitFor();assert.equal(await list.getByRole('option').count(),1);await combo.press('ArrowDown');await combo.press('Enter');await page.waitForFunction(({label,code})=>document.querySelector('input[aria-label="'+label+'"]')?.value.startsWith(code+' · '),{label,code});}
 const combo=page.getByRole('combobox',{name:'Moneda',exact:true});await combo.fill('NO EXISTE');await page.getByText('No se encontraron coincidencias.',{exact:true}).waitFor();await combo.press('Escape');assert.equal(await combo.getAttribute('aria-expanded'),'false');
 console.log('Cinco catálogos: filtro por descripción sin tildes, selección de código por teclado y estado sin resultados correctos. Sin consultas adicionales ni guardar OC.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
