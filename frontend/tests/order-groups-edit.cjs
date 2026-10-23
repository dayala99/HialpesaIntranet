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
 else if(url.includes('/ordenes?'))data={rows:[{series:'002',number:'350118',class:'01',status:'P',supplier:'Test'}],page:1,totalPages:1,totalRows:1,hasMore:false};
 else if(url.endsWith('/ordenes/002/350118/grupos')){if(route.request().method()==='GET')data=[{code:'VCW-685',description:''}];else{calls.push(route.request().postDataJSON());data={};}}
 else if(url.endsWith('/registro/catalogos'))data={conditions:[],discounts:[],currencies:[],deliveryLocations:[],costCenters:[]};
 else if(url.endsWith('/registro/fabricas'))data=[{code:'HIA',description:'HIALPESA'}];
 else if(url.includes('/registro/grupos-por-op?')){assert.equal(new URL(url).searchParams.get('factory'),'HIA');data=[new URL(url).searchParams.get('productionOrder')==='J2451'?{code:'VCW-684',description:'J2451/52/54'}:{code:'VCW-685',description:'J2455'}];}
 else if(url.includes('/registro/catalogos/grupos'))data=[];
 else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');
 const action=page.getByRole('button',{name:'Añadir grupo manufactura a OC 002-350118',exact:true});
 await action.click();let dialog=page.getByRole('dialog');await dialog.getByRole('button',{name:'Quitar grupo VCW-685',exact:true}).waitFor();
 assert.equal(await dialog.getByLabel('OP',{exact:true}).inputValue(),'');await dialog.screenshot({path:path.join(__dirname,'../test-results/refactor-manufacturing-groups.png')});await dialog.getByRole('button',{name:'Quitar grupo VCW-685',exact:true}).click();await dialog.getByRole('button',{name:'Cerrar',exact:true}).click();await dialog.waitFor({state:'hidden'});assert.equal(calls.length,0);
 await action.click();dialog=page.getByRole('dialog');await dialog.getByRole('button',{name:'Quitar grupo VCW-685',exact:true}).waitFor();await dialog.getByRole('button',{name:'Quitar grupo VCW-685',exact:true}).click();
 await dialog.getByLabel('OP',{exact:true}).fill('J2451');await dialog.getByRole('button',{name:'Buscar',exact:true}).click();await dialog.getByRole('cell',{name:'VCW-684',exact:true}).waitFor();await dialog.getByRole('button',{name:'Seleccionar',exact:true}).click();await dialog.getByRole('button',{name:'Guardar grupos',exact:true}).click();
 await page.waitForFunction(()=>document.querySelector('mat-dialog-actions .primary')?.disabled===true);assert.deepEqual(calls,[{groups:['VCW-684'],removedGroups:['VCW-685']}]);await dialog.getByRole('button',{name:'Cerrar',exact:true}).click();
 console.log('Grupos OC: precarga asignados; cerrar descarta; guardar envía solo altas/bajas y mantiene el popup. Sin escrituras reales.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
