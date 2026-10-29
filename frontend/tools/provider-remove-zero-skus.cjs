const {MatchError}=require('./provider-quantities.cjs');
const {quantityInput}=require('./provider-fields.cjs');
const zero=value=>/^0+(?:\.0+)?$/.test(String(value).trim());
async function visibleRows(page){return page.locator('input._oiOrderedQty:not([id^="oid_MSRP_IN_"])').filter({visible:true}).evaluateAll(inputs=>inputs.map(input=>{const row=input.closest('tr');const val=prefix=>row.querySelector('input[id^="'+prefix+'"]')?.value??'';return {id:input.id,quantity:input.value,kind:row.querySelector('input[id^="oid_MSRP_IN_"]')?'RFID':'UPC',style:val('oid_STYLE_IN_'),size:val('oid_SIZECODE_IN_'),colorCode:val('oid_COLORCODE_IN_'),color:val('oid_COLORDESC_IN_'),barcode:val('oid_BARCODE_IN_'),msrp:val('oid_MSRP_IN_')};}));}
const values=rows=>rows.map(({id,...row})=>JSON.stringify(row)).sort();
async function removeZeroSkus(page,setStage=()=>{}){
 const removed=[];
 for(const kind of ['RFID','UPC']){
  setStage('remove-zero-'+kind.toLowerCase());
  const tab=page.locator('a[id^="categoryDiv_order_"][id$="_'+(kind==='RFID'?'RFID':'UPCStickers')+'"]');
  if(await tab.count()!==1)throw new MatchError('No se encontró una pestaña única '+kind+' para revisar SKU en cero.');
  await tab.click();
  await page.waitForFunction(kind=>{const inputs=[...document.querySelectorAll('input._oiOrderedQty:not([id^="oid_MSRP_IN_"])')].filter(e=>e.getClientRects().length);return inputs.length>0&&inputs.every(e=>(e.closest('tr').querySelector('input[id^="oid_MSRP_IN_"]')?'RFID':'UPC')===kind);},kind,{timeout:15000});
  const before=await visibleRows(page);
  if(before.some(row=>row.kind!==kind))throw new MatchError('No se pudo aislar la tabla '+kind+' para eliminar SKU en cero.');
  const zeros=before.filter(row=>zero(row.quantity));if(!zeros.length)continue;
  const positive=before.filter(row=>!zero(row.quantity));
  for(const row of before){const checkbox=quantityInput(page,row.id).locator('xpath=ancestor::tr[1]').locator('input[type=checkbox]');if(await checkbox.count()!==1||!await checkbox.isEnabled())throw new MatchError('No se encontró el checkbox de una fila de '+kind+'.');}
  // Clear prior row selections, including positive quantities, before selecting only zero rows.
  for(const row of before){const checkbox=quantityInput(page,row.id).locator('xpath=ancestor::tr[1]').locator('input[type=checkbox]');if(await checkbox.isChecked())await checkbox.uncheck();}
  for(const row of zeros)await quantityInput(page,row.id).locator('xpath=ancestor::tr[1]').locator('input[type=checkbox]').check();
  for(const row of before){const checked=await quantityInput(page,row.id).locator('xpath=ancestor::tr[1]').locator('input[type=checkbox]').isChecked();if(checked!==zero(row.quantity))throw new MatchError('La selección de SKU en '+kind+' no coincide con las cantidades cero.');}
  const button=page.getByRole('button',{name:/^Remove SKU$/i}).filter({visible:true}).first();
  if(!await button.count()||!await button.isEnabled())throw new MatchError('No se encontró Remove SKU para '+kind+'.');
  let unexpected=false;const dialog=async d=>{if(d.type()==='confirm'&&/remove|sku|delete/i.test(d.message()))await d.accept();else{unexpected=true;await d.dismiss();}};
  page.on('dialog',dialog);
  try{
   await button.scrollIntoViewIfNeeded();
   // Scrolling/focus and portal handlers must not clear selections before Remove SKU.
   const selection=await page.locator('input._oiOrderedQty:not([id^="oid_MSRP_IN_"])').filter({visible:true}).evaluateAll(inputs=>inputs.map(input=>({id:input.id,value:input.value,checked:input.closest('tr').querySelector('input[type=checkbox]')?.checked===true})));
   if(selection.length!==before.length||selection.some(row=>!before.some(original=>original.id===row.id&&original.quantity===row.value)||row.checked!==zero(row.value)))throw new MatchError('Los checkboxes de '+kind+' cambiaron antes de Remove SKU. No se pulsó el botón.');
   await button.click();
   const expected=JSON.stringify(values(positive));const deadline=Date.now()+20000;let actual;
   do{actual=await visibleRows(page);if(JSON.stringify(values(actual))===expected)break;await page.waitForTimeout(100);}while(Date.now()<deadline);
   if(unexpected||JSON.stringify(values(actual))!==expected)throw new MatchError('Remove SKU no conservó las filas esperadas de '+kind+'. Revisa la tabla antes de continuar.');
  }finally{page.off('dialog',dialog);}
  removed.push(...zeros);
 }
 return removed;
}
module.exports={removeZeroSkus,zero};
