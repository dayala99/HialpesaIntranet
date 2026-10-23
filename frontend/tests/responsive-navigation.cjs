const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
(async()=>{
 const root=path.resolve(__dirname,'../dist/intranet-web/browser');
 const server=http.createServer((req,res)=>{const pathname=new URL(req.url,'http://localhost').pathname.replace(/^\/FrontEnd\//,'');const file=path.resolve(root,pathname||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(404).end();return;}const target=fs.existsSync(file)&&fs.statSync(file).isFile()?file:path.join(root,'index.html');res.setHeader('Content-Type',target.endsWith('.js')?'application/javascript':target.endsWith('.css')?'text/css':'text/html');fs.createReadStream(target).pipe(res);});
 await new Promise(resolve=>server.listen(4212,'127.0.0.1',resolve));const browser=await chromium.launch({channel:'chrome',headless:true});
 try{

 const page=await browser.newPage();
 await page.route('**/BackEnd/api/v1/**',route=>route.fulfill({json:route.request().url().endsWith('/sesion')?{displayName:'Prueba',gestionUser:'test',isGlobalUser:true,allowedOptions:[],profiles:[],canWriteDatabase:false,demo:false}:{rows:[],page:1,hasMore:false}}));
 for(const width of [390,768,1024]){
 await page.setViewportSize({width,height:900});
 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios');
 const nav=page.locator('#main-navigation');
 await nav.waitFor({state:'visible'});
 await nav.getByRole('button',{name:'Ocultar navegación',exact:true}).click();await nav.waitFor({state:'hidden'});
 await page.getByRole('button',{name:'Abrir navegación',exact:true}).click();await nav.waitFor({state:'visible'});await page.keyboard.press('Escape');assert.equal(await nav.isVisible(),true);await nav.getByRole('button',{name:'Ocultar navegación',exact:true}).click();await nav.waitFor({state:'hidden'});
 await page.getByRole('button',{name:'Abrir navegación',exact:true}).click();await nav.waitFor({state:'visible'});await page.locator('.menu-backdrop').click({position:{x:width-10,y:300}});await nav.waitFor({state:'hidden'});await page.getByRole('button',{name:'Abrir navegación',exact:true}).click();await nav.waitFor({state:'visible'});await nav.getByRole('link',{name:'Inicio',exact:true}).click();await page.waitForURL('**/FrontEnd/');assert.equal(await nav.isVisible(),true);await nav.getByRole('button',{name:'Ocultar navegación',exact:true}).click();await nav.waitFor({state:'hidden'});
 }
 await page.setViewportSize({width:1440,height:900});
 await page.goto('http://127.0.0.1:4212/FrontEnd/logistica/avios');
 const desktopNav=page.locator('#main-navigation');await desktopNav.waitFor({state:'visible'});
 const selected=desktopNav.locator('a.active');
 const group=desktopNav.getByRole('button',{name:'Logística',exact:true});
 if(await group.getAttribute('aria-expanded')!=='true')await group.click();
 for(const item of [selected,group]){
 await item.hover();const hover=await item.evaluate(e=>getComputedStyle(e).backgroundColor);
 await page.mouse.move(1000,500);const idle=await item.evaluate(e=>getComputedStyle(e).backgroundColor);
 assert.equal(idle,'rgb(36, 73, 75)');assert.equal(hover,idle);
 }
 const before=await page.locator('.workspace').boundingBox();
 await page.getByRole('button',{name:'Ocultar navegación',exact:true}).click();await desktopNav.waitFor({state:'hidden'});
 const after=await page.locator('.workspace').boundingBox();assert.ok(after.width>before.width);
 await page.getByRole('button',{name:'Abrir navegación',exact:true}).click();await desktopNav.waitFor({state:'visible'});
 console.log('Menú verificado: tocar fuera cierra en celular/tablet; seleccionar opciones conserva abierto. Control manual en escritorio. APIs simuladas.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
