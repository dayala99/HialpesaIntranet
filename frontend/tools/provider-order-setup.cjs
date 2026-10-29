'use strict';
const {MatchError}=require('./provider-quantities.cjs');
const normalize=value=>String(value||'').replace(/\s+/g,' ').trim().toUpperCase();
// EDI dates are compared by calendar day; equal dates use the last row in grid order.
function ediDay(value){
 const text=normalize(value);let year,month,day;let m=text.match(/^(\d{1,2})\s+([A-Z]{3,9})\s+(\d{4})(?:\s.*)?$/);
 if(m){day=Number(m[1]);month=['JAN','FEB','MAR','APR','MAY','JUN','JUL','AUG','SEP','OCT','NOV','DEC'].indexOf(m[2].slice(0,3));year=Number(m[3]);}
 else {m=text.match(/^(\d{4})-(\d{2})-(\d{2})(?:[T\s].*)?$/);if(!m)return null;year=Number(m[1]);month=Number(m[2])-1;day=Number(m[3]);}
 if(year<1900||month<0||month>11||day<1||day>31)return null;
 const date=new Date(Date.UTC(year,month,day));return date.getUTCFullYear()===year&&date.getUTCMonth()===month&&date.getUTCDate()===day?date.getTime():null;
}
async function latestSaved(candidates,po){
 if(candidates.length===1)return candidates[0];
 let selected=null,latest=-Infinity;
 for(const candidate of candidates){
  const value=await candidate.row.evaluate(row=>{
   const table=row.closest('table');if(!table)return null;
   const headers=Array.from(table.rows).find(tr=>Array.from(tr.cells).some(cell=>cell.textContent.toUpperCase().replace(/[^A-Z]/g,'')==='EDILOADDATE'));
   if(!headers)return null;
   const index=Array.from(headers.cells).findIndex(cell=>cell.textContent.toUpperCase().replace(/[^A-Z]/g,'')==='EDILOADDATE');
   return row.cells[index]?.textContent?.trim()??null;
  });
  const day=ediDay(value);
  if(day===null)throw new MatchError('No se pudo leer EDI_LOAD_DATE de todos los Saved de '+po+'. Revisa las fechas de la grilla.');
  if(day>=latest){latest=day;selected=candidate;}
 }
 return selected;
}
async function openSelectedPo(page,po,setStage=()=>{}){
 setStage('po-result');
 await page.waitForFunction(po=>Array.from(document.querySelectorAll('tr')).some(tr=>Array.from(tr.querySelectorAll('td')).some(td=>td.textContent.trim()===po)&&/Saved|Available\s+For\s+Callout/i.test(tr.textContent)),po,{timeout:20000});
 const candidates=[];
 for(const row of await page.locator('tr').all()){
  const cells=await row.locator('td').allTextContents();
  if(!cells.some(text=>text.trim()===po))continue;
  const saved=row.getByRole('link',{name:'Saved',exact:true});
  if(await saved.count()===1)candidates.push({row,saved});
  if(cells.some(text=>normalize(text)==='AVAILABLE FOR CALLOUT'))candidates.push({row,callout:true});
 }
 const saved=candidates.filter(x=>x.saved);
 const callouts=candidates.filter(x=>x.callout);
 // A new callout takes precedence over previously saved drafts for this PO.
 if(callouts.length>1||(!callouts.length&&!saved.length))throw new MatchError('No hay un resultado único Available For Callout o Saved para '+po+'. Revisa la grilla del proveedor.');
 const source=callouts.length===1?'callout':'saved';
 const selectedSaved=source==='saved'?await latestSaved(saved,po):null;
 if(source==='saved'){
 setStage('opening-saved');
 // Resolve a remembered Shipping page before rendering it, using its own Edit Order link.
 let redirectError=null;
 const handler=async route=>{
  if(!route.request().isNavigationRequest()||route.request().frame()!==page.mainFrame())return route.continue();
  const response=await route.fetch();
  if(!(response.headers()['content-type']||'').includes('text/html'))return route.fulfill({response});
  const html=await response.text();
  const plain=html.replace(/<[^>]*>/g,' ').replace(/&nbsp;/gi,' ').replace(/\s+/g,' ');
  let target=null;
  if(/Customer\s+Reference\s*#/i.test(plain)&&/Shipping\s+Instructions/i.test(plain)){
   target='';const links=[...html.matchAll(/<a\b([^>]*)>([\s\S]*?)<\/a>/gi)].filter(m=>/^(?:3\s*)?Edit Order$/i.test(m[2].replace(/<[^>]*>/g,'').replace(/&nbsp;/gi,' ').trim()));
   if(links.length===1){const attr=links[0][1].match(/\bhref\s*=\s*(["'])(.*?)\1/i);if(attr){try{const href=new URL(attr[2].replace(/&amp;/gi,'&'),response.url()),from=new URL(response.url());if(href.origin===from.origin&&/^https?:$/.test(href.protocol)&&href.href!==from.href)target=href.href;}catch{}}}
  }
  if(target===null)return route.fulfill({response,body:html});
  if(!target){redirectError='El portal abrió Shipping, pero no expone un enlace directo Edit Order. No se avanzó para evitar el recorrido duplicado.';return route.abort();}
  return route.fulfill({status:302,headers:{location:target},body:''});
 };
 const rememberedShipping=/shipping/i.test(await selectedSaved.saved.getAttribute('href')||'');
 if(rememberedShipping)await page.route('**/*',handler);
 try{await selectedSaved.saved.click();}finally{if(rememberedShipping)await page.unroute('**/*',handler);}
 if(redirectError)throw new MatchError(redirectError);
 }
 else{
 setStage('creating-callout');
 const selected=callouts[0].row;
 const checkbox=selected.locator('input[type=checkbox]');
 if(await checkbox.count()!==1||!await checkbox.isEnabled())throw new MatchError('No se pudo seleccionar Available For Callout de '+po+'.');
 // Only the requested PO can participate in Create Order.
 for(const row of await page.locator('tr').all()){
  if(!await row.locator('td').count())continue;
  for(const box of await row.locator('input[type=checkbox]:checked').all())if(await box.isEnabled())await box.uncheck();
 }
 await checkbox.check();
 const checked=page.locator('tr:has(td) input[type=checkbox]:checked');
 if(await checked.count()!==1)throw new MatchError('Hay otras PO seleccionadas. No se creó la orden.');
 await page.getByRole('button',{name:/^Create Order$/i}).filter({visible:true}).first().click();
 }
 setStage('detecting-order-screen');
 const screen=await page.waitForFunction(po=>{
  const text=document.body?.innerText||'';
  const assignment=text.match(/Please select items for PO#\s*(PO-[0-9]+)/i);
  if(assignment){if(assignment[1]!==po)return 'wrong-po';return 'assign';}
  if(/Customer\s+Reference\s*#/i.test(text)&&/Shipping\s+Instructions/i.test(text))return 'shipping';
  if(document.querySelector('input._oiOrderedQty:not([id^="oid_MSRP_IN_"])'))return 'edit';
  return false;
 },po,{timeout:30000});
 const destination=await screen.jsonValue();await screen.dispose();
 if(destination==='wrong-po')throw new MatchError('La selección de artículos corresponde a otra PO. No se asignaron artículos.');
 if(destination==='shipping'){
  setStage('returning-to-edit-order');
  const edit=page.getByRole('link',{name:/^(?:3\s*)?Edit Order$/i}).filter({visible:true});
  if(await edit.count()!==1)throw new MatchError('Saved abrió Shipping and Billing. No se encontró un enlace único Edit Order para verificar las cantidades.');
  await edit.click();
  await page.locator('input._oiOrderedQty:not([id^="oid_MSRP_IN_"])').first().waitFor({state:'attached',timeout:45000});
  return source;
 }
 if(destination==='edit')return source;
 setStage('assigning-items');
 await page.getByText(new RegExp('Please select items for PO#\\s*'+po)).waitFor({timeout:20000});
 for(const item of ['VINCEH501 RFID','VINH300']){
  const assigned=page.locator('td:not(:has(table)),th:not(:has(table))').filter({hasText:new RegExp('^\\s*'+item.replace(/ /g,'\\s+')+'\\s*$','i')}).filter({visible:true});
  if(await assigned.count()===1)continue;
  if(await assigned.count()>1)throw new MatchError('El artículo '+item+' está duplicado.');
  const inputSelector='input:not([type]),input[type="text" i],input[type="search" i]';
  const inputs=page.locator(inputSelector).filter({visible:true});
  let search=null;
  for(const input of await inputs.all()){
   if(!await input.isEnabled()||await input.getAttribute('readonly')!==null)continue;
   const associated=await input.evaluate((element,selector)=>{
    const named=[element.getAttribute('aria-label'),element.getAttribute('placeholder'),...Array.from(element.labels||[]).map(label=>label.textContent)].join(' ');
    if(/quick\s*search/i.test(named))return true;
    for(let parent=element.parentElement,depth=0;parent&&depth<5;parent=parent.parentElement,depth++){
     if(parent===document.body||parent===document.documentElement)break;
     if(!/quick\s*search/i.test(parent.textContent||''))continue;
     const editable=Array.from(parent.querySelectorAll(selector)).filter(e=>!e.disabled&&!e.readOnly&&e.getClientRects().length);
     if(editable.length===1&&editable[0]===element)return true;
    }
    return false;
   },inputSelector);
   if(associated){if(search)throw new MatchError('Quick Search es ambiguo.');search=input;}
  }
  if(!search){const editable=[];for(const input of await inputs.all())if(await input.isEnabled()&&await input.getAttribute('readonly')===null)editable.push(input);if(editable.length===1)search=editable[0];}
  if(!search)throw new MatchError('No se encontró Quick Search para asignar '+item+'.');
  setStage('typing-item');
  await search.fill('');
  const fullName=item==='VINCEH501 RFID'?'VINCEH501 RFID|VINCE. RFID MAIN HANGTAG W/UPC':'VINH300|VINCE UPC STICKER';
  await search.fill(fullName);
  if(await search.inputValue()!==fullName)throw new MatchError('No se pudo completar el nombre exacto de '+item+'.');
  setStage('assigning-item');
  await page.getByRole('button',{name:/^Assign$/i}).click();
  try{await assigned.waitFor({state:'visible',timeout:15000});}catch{throw new MatchError('El proveedor no confirmó la asignación de '+item+' después de Assign. Revisa la lista de artículos.');}
 }
 const items=await page.locator('td:not(:has(table)),th:not(:has(table))').filter({hasText:/^\s*(VINCEH501\s+RFID|VINH300)\s*$/i}).filter({visible:true}).count();
 if(items!==2)throw new MatchError('No se confirmaron los dos artículos requeridos.');
 setStage('opening-edit-order');
 await page.getByRole('button',{name:/^Go To Edit Order$/i}).click();
 return source;
}
module.exports={openSelectedPo,ediDay};
