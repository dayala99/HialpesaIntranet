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
 if(url.endsWith('/sesion'))data={displayName:'Prueba',gestionUser:'test',canWriteDatabase:false,demo:false};
 else if(url.endsWith('/pos'))data=['PO-00054154'];
 else if(url.endsWith('/estado-apertura'))data={received};
 else if(url.endsWith('/preparar-apertura')){calls.push(route.request().postDataJSON());assert.equal(route.request().headers()['x-intranet-provider'],'1');data={token:'A'.repeat(64),expiresAt:new Date(Date.now()+120000).toISOString()};}
 else if(url.includes('/ordenes?'))data={rows:[{series:'002',number:'350000',supplier:'Proveedor de prueba',class:'01',status:'L'}],page:1,hasMore:false};else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});
 await page.clock.install();await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios/nueva');const action=page.getByRole('button',{name:'Abrir Avery Dennison para OC 002-350000',exact:true});await action.click();await page.getByRole('dialog').getByRole('button',{name:'Cancelar',exact:true}).click();assert.equal(calls.length,0);
 await action.click();await page.getByRole('dialog').getByRole('button',{name:'PO-00054154',exact:true}).click();const popup=page.getByRole('dialog');const counter=popup.getByRole('timer');await counter.waitFor();await page.clock.runFor(400);
 const links=await page.evaluate(()=>window.vinceLinks);assert.equal(links.length,1);const url=new URL(links[0]);assert.equal(url.protocol,'hialpesa-vince:');assert.equal(url.searchParams.get('server'),'http://127.0.0.1:4212/BackEnd/api/v1');assert.equal(url.searchParams.get('token'),'A'.repeat(64));assert.deepEqual(calls,[{series:'002',number:'350000',po:'PO-00054154'}]);
 assert.equal(await popup.getByRole('button').count(),0);assert.equal(await popup.getByRole('link').count(),0);assert.match((await popup.innerText()).trim(),/^TIEMPO RESTANTE\s+\d\d:\d\d$/);
 const counterBox=await counter.boundingBox(),popupBox=await popup.boundingBox();assert(Math.abs(counterBox.x+counterBox.width/2-popupBox.x-popupBox.width/2)<4);
 await page.clock.fastForward(5000);assert.match(await counter.innerText(),/01:5[0-9]/);await popup.screenshot({path:'test-results/vince-agent-launch.png'});
 await page.clock.fastForward(120000);await popup.waitFor({state:'hidden'});await page.getByText(/La autorización venció/).waitFor();assert.equal(downloads,0);
 await page.clock.setSystemTime(new Date());received=true;
 await action.click();await page.getByRole('dialog').getByRole('button',{name:'PO-00054154',exact:true}).click();await page.clock.runFor(1200);
 await page.getByRole('dialog').waitFor({state:'hidden'});await page.getByText(/El agente VINCE recibió la operación/).waitFor();assert.equal(downloads,0);assert.equal((await page.evaluate(()=>window.vinceLinks)).length,2);
 console.log('UI producción: popup cierra al recibir el agente; sin recepción espera hasta vencer; cero descargas.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(e=>{console.error(e);process.exit(1)});




