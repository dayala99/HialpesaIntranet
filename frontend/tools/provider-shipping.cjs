const {MatchError}=require('./provider-quantities.cjs');
async function fillShipping(page,command,setStage=()=>{}){
 setStage('shipping-next');
 require('fs').mkdirSync('test-results',{recursive:true});
 const next=page.getByRole('button',{name:/^Next\s*>?$/i}).filter({visible:true}).last();
 await next.click({timeout:15000});
 let scope;const deadline=Date.now()+45000;
 while(!scope&&Date.now()<deadline){for(const frame of page.frames()){if(await frame.evaluate(()=>/Customer\s+Reference\s*#/i.test(document.body?.innerText||'')).catch(()=>false)){scope=frame;break;}}if(!scope)await page.waitForTimeout(200);}
 if(!scope)throw new MatchError('No se encontró el formulario Shipping and Billing en la página ni en sus marcos.');
 setStage('shipping-fields');
 const pos=await scope.evaluate(()=>[...document.body.innerText.matchAll(/PO-\d+/g)].map(m=>m[0]));
 if(!pos.length||pos.some(po=>po!==command.po))throw new MatchError('El PO de la pantalla de envío no coincide con '+command.po+'.');
 // Labels are read only: locate controls in the value cell to their right.
 async function valueCell(label,key){
  setStage('shipping-'+key);
  await scope.waitForFunction(({label,key})=>{
   const norm=s=>s.replace(/\s+/g,' ').replace(/[\s:*#]+$/g,'').trim().toLowerCase();
   const cells=[...document.querySelectorAll('td,th')].filter(e=>e.getClientRects().length&&!e.querySelector('table')&&norm(e.textContent)===norm(label));
   const right=[...new Set(cells.map(e=>e.nextElementSibling).filter(e=>e&&/^(TD|TH)$/.test(e.tagName)&&e.getClientRects().length))];
   if(right.length!==1)return false;
   right[0].setAttribute('data-vince-value-cell',key);return true;
  },{label,key},{timeout:20000});
  return scope.locator('[data-vince-value-cell="'+key+'"]');
 }
 async function control(label,tag,key){
  const cell=await valueCell(label,key);const input=cell.locator(tag).filter({visible:true});
  if(await input.count()!==1)throw new MatchError('No se encontró un campo único a la derecha de '+label+'.');
  await input.waitFor({state:'visible'});return input;
 }
 async function select(label,text,key){
  const cell=await valueCell(label==='Shipping Carrier'?'Consolidator Information':label,key);
  const norm=s=>s.replace(/\s+/g,' ').trim().toLowerCase();
  await cell.locator('select').first().waitFor({state:'visible'});
  await scope.waitForFunction(({key,text})=>[...document.querySelectorAll('[data-vince-value-cell="'+key+'"] select')].some(s=>!s.disabled&&[...s.options].some(o=>o.textContent.replace(/\s+/g,' ').trim().toLowerCase()===text)),{key,text:norm(text)},{timeout:20000});
  const selects=cell.locator('select').filter({visible:true});let field,matches;
  for(let i=0;i<await selects.count();i++){const candidate=selects.nth(i);if(!await candidate.isEnabled())continue;const options=await candidate.locator('option').evaluateAll(os=>os.map(o=>({label:o.textContent,value:o.value})));const exact=options.filter(o=>norm(o.label)===norm(text));if(exact.length){if(field||exact.length!==1)throw new MatchError('Más de una opción coincide con '+text+'.');field=candidate;matches=exact;}}
  if(!field)throw new MatchError('No se encontró la descripción '+text+' a la derecha de '+label+'.');
  await field.selectOption(matches[0].value);
  if(await field.inputValue()!==matches[0].value)throw Error('No se conservó '+label+'.');
 }
 const reference=/^\d{3}$/.test(command.series||'')&&/^\d{6}$/.test(command.number||'')?command.series+'-'+command.number:null;
 if(reference){const input=await control('Customer Reference','input:not([type=hidden])','reference');await input.fill(reference);if(await input.inputValue()!==reference)throw Error('Customer Reference no se conservó.');}
 await select('Shipping Method','Courier','method');
 await select('Shipping Carrier','DHL (COURIER)','carrier');
 const instructions=await control('Shipping Instructions','textarea','instructions');
 await instructions.fill('20418108151');
 if(await instructions.inputValue()!=='20418108151')throw Error('Shipping Instructions no se conservó.');
 setStage('shipping-complete');
 return {po:command.po,customerReference:reference,shippingMethod:'Courier',shippingCarrier:'DHL (COURIER)',shippingInstructions:'20418108151',saved:false};
}
module.exports={fillShipping};
