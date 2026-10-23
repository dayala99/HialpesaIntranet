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
 else if(url.includes('/logistica/avios/pos?'))data={rows:[{po:'PO-00055204',clientCode:'10627',client:'VINCE WOMENS',description:'',createdAt:'2026-09-17'}],page:1,totalPages:1,totalRows:1,hasMore:false};
 else if(url.includes('/detalle?'))data=[{lot:'001',style:'V018884418',description:'SCOOP NK TANK',colorCode:'BAYLEAF',color:'BAYLEAF',size:'L / G',required:350,purchased:350,shipped:0,upc:''}];
 else if(url.endsWith('/preparar-apertura-po')){calls.push(route.request().postDataJSON());data={token:'A'.repeat(64),expiresAt:new Date(Date.now()+120000).toISOString()};}
 else if(url.endsWith('/estado-apertura'))data={received:true};else throw Error('Unexpected endpoint: '+url);
 await route.fulfill({json:data});});

 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios');
 await page.getByRole('button',{name:'Consultar PO',exact:true}).click();
 await page.getByRole('cell',{name:'VINCE WOMENS',exact:true}).waitFor();
 assert.equal(await page.getByRole('button',{name:'Preparar matriz',exact:true}).count(),0);
 await page.getByRole('button',{name:'Ver detalle de PO-00055204',exact:true}).click();
 await page.getByRole('dialog').getByRole('cell',{name:'V018884418',exact:true}).waitFor();
 await page.getByRole('dialog').getByRole('columnheader',{name:'Cantidad comprada',exact:true}).waitFor();
 await page.getByRole('dialog').screenshot({path:'test-results/po-details.png'});
 await page.getByRole('button',{name:'Cerrar',exact:true}).click();
 await page.getByRole('button',{name:'Ejecutar agente VINCE para PO-00055204',exact:true}).click();
 await page.waitForFunction(()=>window.vinceLinks.length===1);
 assert.deepEqual(calls,[{po:'PO-00055204',clientCode:'10627'}]);
 console.log('Producción: tabla PO, detalle con estilo/color/cantidad y acción del mismo agente sin OC correctos.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
