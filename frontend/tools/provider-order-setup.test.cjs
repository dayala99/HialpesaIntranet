const assert=require('node:assert/strict');const {chromium}=require('playwright');const {openSelectedPo,ediDay}=require('./provider-order-setup.cjs');
(async()=>{assert.equal(ediDay('31 Feb 2026'),null);assert.equal(ediDay('14 Sep 2026'),Date.UTC(2026,8,14));assert.equal(ediDay('2026-10-28'),ediDay('28 Oct 2026'));const browser=await chromium.launch({channel:'chrome',headless:true});try{const page=await browser.newPage();
async function run(name,rows,expected,error=false){await page.setContent(`<table><tr><th>Select</th><th>PO Number</th><th>Status</th><th>EDI_LOAD_DATE</th></tr>${rows.map(([id,status,date,po='PO-00054150'])=>`<tr><td><input type="checkbox" data-id="${id}"></td><td>${po}</td><td>${status==='Saved'?`<a href="#${id}" onclick="window.chosen='${id}';document.body.insertAdjacentHTML('beforeend','<input class=_oiOrderedQty>');return false">Saved</a>`:status}</td><td>${date}</td></tr>`).join('')}</table><button onclick="window.chosen=document.querySelector('input:checked').dataset.id;document.body.insertAdjacentHTML('beforeend','<input class=_oiOrderedQty>')">Create Order</button>`);if(error){await assert.rejects(openSelectedPo(page,'PO-00054150'),e=>e.code==='MATCH_ERROR');assert.equal(await page.evaluate(()=>window.chosen),undefined);}else{await openSelectedPo(page,'PO-00054150');assert.equal(await page.evaluate(()=>window.chosen),expected);}await page.evaluate(()=>delete window.chosen);console.log('PASS '+name);}
await run('ejemplo cancelados + Saved septiembre/octubre',[['a','Cancelled','11 Sep 2026'],['b','Saved','14 Sep 2026'],['c','Cancelled','28 Oct 2026'],['d','Saved','28 Oct 2026']],'d');
await run('fecha mayor aunque no sea última',[['a','Saved','28 Oct 2026'],['b','Saved','14 Sep 2026']],'a');
await run('misma fecha el último Saved',[['a','Saved','28 Oct 2026'],['b','Saved','28 Oct 2026'],['c','Cancelled','29 Oct 2026']],'b');
await run('mismo día ignora hora y toma última fila',[['a','Saved','28 Oct 2026 18:00'],['b','Saved','28 Oct 2026 08:00']],'b');
await run('Callout sigue primero',[['a','Saved','28 Oct 2026'],['b','Saved','29 Oct 2026'],['c','Available For Callout','14 Sep 2026']],'c');
await run('Saved de otra PO no participa',[['a','Saved','28 Oct 2026'],['b','Saved','29 Oct 2026','PO-999']],'a');
await run('fecha inválida detiene sin clic',[['a','Saved','31 Feb 2026'],['b','Saved','28 Oct 2026']],null,true);
await run('varios Callout detienen',[['a','Available For Callout','28 Oct 2026'],['b','Available For Callout','28 Oct 2026'],['c','Saved','28 Oct 2026']],null,true);
await run('único Saved conserva compatibilidad',[['a','Saved','-']],'a');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1)});

