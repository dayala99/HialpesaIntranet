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
 else if(url.endsWith('/ordenes/002/350118/detalles/001')){if(route.request().method()==='GET')data={purchased:1095,unitPrice:2,discountCode:'01',taxPercent:18,deliveryStart:'2026-10-21T00:00:00',deliveryEnd:'2026-10-22T00:00:00',observations:''};else{calls.push({method:route.request().method(),body:route.request().method()==='PUT'?route.request().postDataJSON():null});data={};}}
 else if(url.endsWith('/ordenes/002/350118'))data={order:{series:'002',number:'350118',class:'01',status:'P',supplier:'Test'},items:[{sequence:'001',itemCode:'PT000500',description:'Ticket',size:'',required:1092,purchased:1095,received:0}]};
 else if(url.endsWith('/registro/catalogos'))data={conditions:[],discounts:[],currencies:[],deliveryLocations:[],costCenters:[]};
 else if(url.endsWith('/registro/fabricas'))data=[{code:'HIA',description:'HIALPESA'}];
 else if(url.includes('/registro/grupos-por-op?')){assert.equal(new URL(url).searchParams.get('factory'),'HIA');data=[new URL(url).searchParams.get('productionOrder')==='J2451'?{code:'VCW-684',description:'J2451/52/54'}:{code:'VCW-685',description:'J2455'}];}
 else if(url.includes('/registro/catalogos/grupos'))data=[];
 else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');
 await page.getByRole('button',{name:'Agregar detalle a OC 002-350118',exact:true}).click();
 await page.getByRole('button',{name:'Modificar secuencia 001',exact:true}).click();await page.getByRole('dialog').getByLabel('Cantidad comprada',{exact:true}).fill('1100');await page.getByRole('dialog').getByRole('button',{name:'Guardar cambios',exact:true}).click();await page.getByRole('dialog').waitFor({state:'hidden'});assert.equal(calls[0].method,'PUT');assert.equal(calls[0].body.purchased,1100);
 await page.getByRole('button',{name:'Eliminar secuencia 001',exact:true}).click();await page.getByRole('dialog').getByRole('button',{name:'Cancelar',exact:true}).click();assert.equal(calls.length,1);
 await page.getByRole('button',{name:'Eliminar secuencia 001',exact:true}).click();await page.getByRole('dialog').getByRole('button',{name:'Eliminar',exact:true}).click();await page.getByText('Secuencia eliminada correctamente.',{exact:true}).waitFor();assert.equal(calls[1].method,'DELETE');console.log('Detalle OC: modificación envía PUT, cancelar no elimina y confirmar envía DELETE. Prueba simulada sin cambios reales.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
