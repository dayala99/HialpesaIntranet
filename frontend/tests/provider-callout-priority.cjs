const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const {openSelectedPo}=require('../tools/provider-order-setup.cjs');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true});
 try{
  const page=await browser.newPage();
  const po='PO-00055204';
  async function fixture(savedCount,calloutCount){
   const saved=Array.from({length:savedCount},()=>`<tr><td>${po}</td><td><a href="#" onclick="window.savedClicked=true;return false">Saved</a></td></tr>`).join('');
   const callout=Array.from({length:calloutCount},(_,i)=>`<tr><td><input type="checkbox" id="available${i}"></td><td>${po}</td><td>Available For Callout</td></tr>`).join('');
   await page.setContent(`<table>${saved}${callout}<tr><td><input type="checkbox" id="other" checked></td><td>PO-999999</td><td>Available For Callout</td></tr></table><button id="create">Create Order</button><input class="_oiOrderedQty" value="153"><input id="oid_MSRP_IN_1" value="88.00">`);
   await page.evaluate(()=>{window.savedClicked=false;window.createClicked=false;window.selection=[];document.querySelector('#create').onclick=()=>{window.createClicked=true;window.selection=[...document.querySelectorAll('input:checked')].map(x=>x.id);document.body.innerHTML='<p>PO Number: PO-00055204</p><input class="_oiOrderedQty" value="153"><input id="oid_MSRP_IN_1" value="88.00">';};});
  }
  for(const savedCount of [1,2]){
   await fixture(savedCount,1);
   assert.equal(await openSelectedPo(page,po),'callout');
   assert.equal(await page.evaluate(()=>window.savedClicked),false);
   assert.equal(await page.evaluate(()=>window.createClicked),true);
   assert.deepEqual(await page.evaluate(()=>window.selection),['available0']);
   assert.equal(await page.locator('input._oiOrderedQty').inputValue(),'153');
   assert.equal(await page.locator('#oid_MSRP_IN_1').inputValue(),'88.00');
  }
  await fixture(1,0);assert.equal(await openSelectedPo(page,po),'saved');assert.equal(await page.evaluate(()=>window.savedClicked),true);assert.equal(await page.evaluate(()=>window.createClicked),false);
  for(const [savedCount,calloutCount] of [[1,2],[0,2],[2,0]]){
   await fixture(savedCount,calloutCount);await assert.rejects(openSelectedPo(page,po),/único/);assert.equal(await page.evaluate(()=>window.createClicked||window.savedClicked),false);
  }
  console.log('PASS: Callout tiene prioridad sobre uno o varios Saved; Saved único funciona sin Callout; duplicados de la opción elegida se bloquean. Solo se selecciona la PO solicitada. Cantidades y MSRP intactos.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
