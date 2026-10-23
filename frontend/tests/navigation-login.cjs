const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
(async()=>{
 const root=path.resolve(__dirname,'../dist/intranet-web/browser');
 const server=http.createServer((req,res)=>{const pathname=new URL(req.url,'http://localhost').pathname.replace(/^\/FrontEnd\//,'');const file=path.resolve(root,pathname||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(404).end();return;}const target=fs.existsSync(file)&&fs.statSync(file).isFile()?file:path.join(root,'index.html');res.setHeader('Content-Type',target.endsWith('.js')?'application/javascript':target.endsWith('.css')?'text/css':'text/html');fs.createReadStream(target).pipe(res);});
 await new Promise(resolve=>server.listen(4212,'127.0.0.1',resolve));const browser=await chromium.launch({channel:'chrome',headless:true});
 try{


 const page=await browser.newPage({viewport:{width:390,height:900}});let loggedIn=false;
 const session={displayName:'Prueba',gestionUser:'TEST',isGlobalUser:true,allowedOptions:[],profiles:[],canWriteDatabase:false,demo:false};
 await page.route('**/BackEnd/api/v1/**',route=>{const url=route.request().url();if(url.endsWith('/login')){loggedIn=true;return route.fulfill({json:session});}if(url.endsWith('/logout')){loggedIn=false;return route.fulfill({status:204});}if(url.endsWith('/sesion'))return loggedIn?route.fulfill({json:session}):route.fulfill({status:401,json:{title:'Inicia sesión'}});return route.fulfill({json:{rows:[],page:1,hasMore:false}});});
 await page.goto('http://127.0.0.1:4212/FrontEnd/login');
 for(let attempt=0;attempt<2;attempt++){
  await page.getByLabel('Usuario',{exact:true}).fill('TEST');await page.getByLabel('Contraseña',{exact:true}).fill('ficticia');await page.getByRole('button',{name:'Ingresar',exact:true}).click();
  const nav=page.locator('#main-navigation');await nav.waitFor({state:'visible'});await nav.getByRole('button',{name:'Ocultar navegación',exact:true}).click();await nav.waitFor({state:'hidden'});
  await page.getByRole('link',{name:'Nueva orden de compra',exact:true}).count();
  await page.getByRole('button',{name:'Cerrar sesion',exact:true}).click();await page.getByLabel('Usuario',{exact:true}).waitFor();
 }
 console.log('Inicio y nuevo inicio de sesión: menú abierto en celular; cierre manual conservado. API simulada.');
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exit(1)});
