const {chromium}=require('playwright'),assert=require('node:assert/strict');const {quantityInput}=require('../tools/provider-fields.cjs');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true});try{const page=await browser.newPage();await page.setContent('<input class="_oiOrderedQty" id="order_1_cat_2_3" value="5"><input class="_oiOrderedQty" id="oid_MSRP_IN_1" value=""><input class="_oiOrderedQty" id="oid_MSRP_IN_2" value="99"><input id="other" value="7">');
await quantityInput(page,'order_1_cat_2_3').fill('135');assert.equal(await quantityInput(page,'order_1_cat_2_3').inputValue(),'135');
for(const id of ['oid_MSRP_IN_1','oid_MSRP_IN_2'])assert.throws(()=>quantityInput(page,id),/MSRP/);
assert.equal(await quantityInput(page,'other').count(),0);assert.equal(await page.locator('#oid_MSRP_IN_1').inputValue(),'');assert.equal(await page.locator('#oid_MSRP_IN_2').inputValue(),'99');
console.log('Quantity To Order actualizado; MSRP vacío y MSRP existente intactos incluso si comparten clase CSS.');}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
