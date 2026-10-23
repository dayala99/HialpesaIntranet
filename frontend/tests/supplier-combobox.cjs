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
 else if(url.endsWith('/registro/catalogos'))data={conditions:[],discounts:[],currencies:[],deliveryLocations:[],costCenters:[]};
 else if(url.endsWith('/registro/fabricas'))data=[{code:'HIA',description:'HIALPESA'}];
 else if(url.includes('/registro/grupos-por-op?')){assert.equal(new URL(url).searchParams.get('factory'),'HIA');data=[new URL(url).searchParams.get('productionOrder')==='J2451'?{code:'VCW-684',description:'J2451/52/54'}:{code:'VCW-685',description:'J2455'}];}
 else if(url.includes('/registro/catalogos/proveedores')){calls.push(new URL(url).searchParams.get('search'));data=[{code:'000000000281',description:'AVERY DENNISON HONG KONG B.V.'}];}
 else if(url.includes('/registro/catalogos/grupos'))data=[];
 else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');
 await page.getByRole('button',{name:'Nueva orden de compra',exact:true}).click();
 const input=page.getByRole('combobox',{name:'Proveedor',exact:true});
 await input.fill('D');await page.waitForTimeout(400);assert.equal(calls.length,0);
 await input.fill('DENNISON');const option=page.getByRole('option',{name:/AVERY DENNISON/});await option.waitFor();assert.deepEqual(calls,['DENNISON']);await option.click();
 await page.waitForFunction(()=>document.querySelector('input[name=supplierCode]')?.value==='000000000281');
 await input.fill('AVERY');await page.waitForFunction(()=>document.querySelector('input[name=supplierCode]')?.value==='');
 await option.waitFor();await input.press('ArrowDown');await input.press('Enter');await page.waitForFunction(()=>document.querySelector('input[name=supplierCode]')?.value==='000000000281');
 console.log('Proveedor filtrable: mínimo dos caracteres, búsqueda diferida, selección por clic/teclado y código invalidado al editar.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
