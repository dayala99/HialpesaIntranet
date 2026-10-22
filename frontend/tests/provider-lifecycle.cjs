const {chromium}=require('playwright'),assert=require('node:assert/strict');const {bindBrowserLifetime}=require('../tools/provider-lifecycle.cjs');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true});const context=await browser.newContext();let ended=0;bindBrowserLifetime(browser,context,()=>ended++);
 const a=await context.newPage(),b=await context.newPage();await a.goto('data:text/html,prueba local');await a.close();assert(browser.isConnected());assert.equal(ended,0);
 const disconnected=new Promise(resolve=>browser.once('disconnected',resolve));await b.close();await Promise.race([disconnected,new Promise((_,reject)=>setTimeout(()=>reject(Error('El navegador no terminó')),5000).unref())]);assert.equal(browser.isConnected(),false);assert.equal(ended,1);
 console.log('Cierre verificado: cerrar una pestaña conserva la sesión; cerrar la última cierra Chrome y finaliza una sola vez. Sin acceso al proveedor.');
})().catch(e=>{console.error(e);process.exit(1)});
