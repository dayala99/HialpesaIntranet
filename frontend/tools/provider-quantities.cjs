// Match the exact Excel report data. No rounding, price substitution or row-position matching.
const aliases=require('./provider-color-aliases.json');
class MatchError extends Error {constructor(message){super(message);this.code='MATCH_ERROR';}}
const norm=v=>String(v??'').trim().toUpperCase().replace(/\s+/g,' ');
const style=v=>norm(v).replace(/\s*-\s*$/,'');
const size=v=>norm(v).replace(/\s*-\s*$/,'').split('/')[0].trim();
function rawColor(v){const text=norm(v).match(/COLOR\s*:\s*(.+)$/)?.[1]||'';const words=text.split(' ');const half=words.length/2;return Number.isInteger(half)&&words.slice(0,half).join(' ')===words.slice(half).join(' ')?words.slice(0,half).join(' '):text;}
function repeatedColor(v){const parts=norm(v).split(' ');for(let tail=Math.floor(parts.length/2);tail>=2;tail--){const first=parts.slice(0,parts.length-tail),last=parts.slice(-tail);if(first.length>=3&&last.join(' ')===first.slice(0,tail).join(' '))return first.join(' ');}return norm(v);}
const canonicalColor=v=>aliases[repeatedColor(v)]||repeatedColor(v);
const color=v=>canonicalColor(rawColor(v));
const list=values=>[...new Set(values.filter(Boolean))].slice(0,12).join(', ')||'(ninguno)';
const poOf=v=>norm(v).match(/\bPO\s*-\s*([0-9]+)\b/)?.[1];
const key=r=>[style(r.customerStyle),color(r.combination),size(r.measure),norm(r.productionOrders)].join('|');
class ColorConfirmationRequired extends Error {
 constructor(confirmations){super('Confirma la similitud de colores antes de completar cantidades.');this.code='COLOR_CONFIRMATION';this.confirmations=confirmations;}
}
const words=v=>norm(v).replace(/[^A-Z0-9]+/g,' ').trim().replace(/\s+/g,' ');
const confirmationKey=c=>JSON.stringify([norm(c.style),norm(c.colorCode),norm(c.providerColor),norm(c.gestionColor)]);
function planQuantities(rows,portal,po,confirmedColors=[]){
 const number=poOf(po);if(!number)throw new MatchError('PO no valido: '+norm(po));
 const fail=(reason,row)=>{const context=row?' | '+row.kind+' | estilo: '+norm(row.style)+' | color proveedor: '+norm(row.color)+' | codigo color: '+norm(row.colorCode)+' | talla: '+norm(row.size):'';throw new MatchError(po+context+'. '+reason);};
 const tickets=rows.filter(r=>/^PT\d+-/i.test(r.item)&&/VINCEH501RFID/i.test(r.item));
 const selected=tickets.filter(r=>poOf(r.combination)===number);
 if(!selected.length)fail('No hay price tickets VINCEH501RFID en el Excel para este PO. Revisa la OC y el articulo seleccionado.');
 const selections=new Map();
 for(const row of portal){
  const peers=portal.filter(x=>x.kind==='UPC'&&x.barcode&&x.barcode===row.barcode&&norm(x.style)===norm(row.style)&&norm(x.colorCode)===norm(row.colorCode));
  const names=[...new Set(peers.map(x=>canonicalColor(x.color)).filter(Boolean))];
  const name=row.color?canonicalColor(row.color):names.length===1?names[0]:'';
  if(!name)continue;
  const options=[...new Set(selected.filter(x=>style(x.customerStyle)===norm(row.style)).map(x=>rawColor(x.combination)))];
  if(!options.length||options.some(x=>canonicalColor(x)===name))continue;
  const approved=confirmedColors.find(c=>norm(c.style)===norm(row.style)&&norm(c.colorCode)===norm(row.colorCode)&&norm(c.providerColor)===norm(name)&&options.includes(c.gestionColor));
  if(approved)continue;
  const similar=options.filter(x=>(' '+words(x)+' ').includes(' '+words(name)+' ')||words(x).replace(/ /g,'')===words(name).replace(/ /g,''));
  if(similar.length===1)continue;
  const proposal={style:norm(row.style),colorCode:norm(row.colorCode),providerColor:name,gestionColor:'',options};
  selections.set(JSON.stringify([proposal.style,proposal.colorCode,name]),proposal);
 }
 if(selections.size){const error=new ColorConfirmationRequired([...selections.values()]);error.code='COLOR_SELECTION';throw error;}
 const plan=[];const used=new Set();const pending=new Map();
 for(const row of portal){
  if(!['RFID','UPC'].includes(row.kind))fail('Tipo de articulo no reconocido. Revisa RFID / UPC Stickers.',row);
  const peers=portal.filter(x=>x.kind==='UPC'&&x.barcode&&x.barcode===row.barcode&&norm(x.style)===norm(row.style)&&norm(x.colorCode)===norm(row.colorCode));
  const names=[...new Set(peers.map(x=>canonicalColor(x.color)).filter(Boolean))];
  const name=row.color?canonicalColor(row.color):names.length===1?names[0]:'';
  if(!name)fail('No se pudo identificar un color unico mediante barcode, estilo y codigo. Colores relacionados: '+list(names)+'. Revisa los datos de UPC Stickers.',row);
  let targetColor=name;
  const explicit=confirmedColors.find(c=>norm(c.style)===norm(row.style)&&norm(c.colorCode)===norm(row.colorCode)&&norm(c.providerColor)===norm(name)&&selected.some(x=>style(x.customerStyle)===norm(row.style)&&rawColor(x.combination)===c.gestionColor));
  if(explicit)targetColor=canonicalColor(explicit.gestionColor);
  const styleRows=selected.filter(x=>style(x.customerStyle)===norm(row.style));
  if(!explicit&&!styleRows.some(x=>color(x.combination)===name)){
   const compact=value=>words(value).replace(/ /g,'');
   const candidates=[...new Set(styleRows.filter(x=>(' '+words(rawColor(x.combination))+' ').includes(' '+words(name)+' ')||compact(rawColor(x.combination))===compact(name)).map(x=>rawColor(x.combination)))];
   if(candidates.length===1){
    const proposal={style:norm(row.style),colorCode:norm(row.colorCode),providerColor:name,gestionColor:candidates[0]};
    targetColor=canonicalColor(proposal.gestionColor);
    if(!confirmedColors.some(c=>confirmationKey(c)===confirmationKey(proposal)))pending.set(confirmationKey(proposal),proposal);
   }
  }
  const matches=selected.filter(x=>style(x.customerStyle)===norm(row.style)&&color(x.combination)===targetColor&&size(x.measure)===norm(row.size));
  if(matches.length===0&&styleRows.length&&styleRows.some(x=>color(x.combination)===targetColor)&&row.poQuantity===0&&String(row.quantity).trim()!==''&&Number(row.quantity)===0){
   const zeroKey=['zero',row.kind,norm(row.style),norm(row.colorCode),norm(row.size)].join('|');
   if(used.has(zeroKey))fail('Hay una fila sin cantidad duplicada en el proveedor.',row);used.add(zeroKey);
   const proposal={style:norm(row.style),colorCode:norm(row.colorCode),providerColor:name+' | talla '+norm(row.size)+' | '+row.kind,gestionColor:'Sin fila en Excel. PO Quantity y Quantity To Order son 0. La fila con Quantity To Order 0 se eliminará mediante Remove SKU después del llenado.'};
   if(!confirmedColors.some(c=>confirmationKey(c)===confirmationKey(proposal)))pending.set(confirmationKey(proposal),proposal);
   continue;
  }
  if(matches.length!==1){
   const sameStyle=selected.filter(x=>style(x.customerStyle)===norm(row.style));
   if(!sameStyle.length)fail('El estilo no coincide. Estilos en Gestion/Excel: '+list(selected.map(x=>style(x.customerStyle)))+'. Revisa el estilo de la OC y del proveedor.',row);
   const sameColor=sameStyle.filter(x=>color(x.combination)===targetColor);
   if(!sameColor.length)fail('El color no coincide. Color proveedor: '+name+'. Colores en Gestion/Excel: '+list(sameStyle.map(x=>rawColor(x.combination)))+'. Corrige el dato o confirma una equivalencia de color.',row);
   if(!matches.length)fail('La talla no coincide. Tallas en Gestion/Excel para este estilo y color: '+list(sameColor.map(x=>size(x.measure)))+'. Revisa la talla o su equivalencia.',row);
   fail('Hay '+matches.length+' price tickets para la misma combinacion. Coincidencias en Gestion/Excel: '+list(matches.map(x=>x.item.split('-')[0]+' / NP '+x.productionOrders+' / cantidad '+x.purchased))+'. Revisa los registros duplicados o la NP.',row);
  }
  let source=matches[0];
  if(row.kind==='UPC'){
   // Ticket and sticker descriptions may contain different extra text for the
   // same color. Keep style, size and NP exact, and validate color separately.
   const sameDimensions=x=>style(x.customerStyle)===style(source.customerStyle)&&size(x.measure)===size(source.measure)&&norm(x.productionOrders)===norm(source.productionOrders);
   const relatedColor=x=>color(x.combination)===targetColor||color(x.combination)===name||(' '+words(rawColor(x.combination))+' ').includes(' '+words(name)+' ');
   const relatedPos=new Set(tickets.filter(x=>sameDimensions(x)&&relatedColor(x)).map(x=>poOf(x.combination)).filter(Boolean));
   if(relatedPos.size!==1||!relatedPos.has(number))fail('El sticker se relaciona con varios PO: '+list([...relatedPos].map(x=>'PO-'+x))+'. NP: '+source.productionOrders+'. Revisa su asociacion antes de llenar.',row);
   const availableStickers=rows.filter(x=>/^ST\d+-/i.test(x.item)&&/VINH300/i.test(x.item)&&sameDimensions(x)&&(!poOf(x.combination)||poOf(x.combination)===number));
   const stickerApproval=confirmedColors.find(c=>norm(c.style)===norm(row.style)&&norm(c.colorCode)===norm(row.colorCode)&&norm(c.providerColor)===norm(name)&&availableStickers.some(x=>rawColor(x.combination)===c.gestionColor));
   const stickers=availableStickers.filter(x=>stickerApproval?rawColor(x.combination)===stickerApproval.gestionColor:relatedColor(x));
   if(!stickers.length&&availableStickers.length){
    const options=[...new Set(availableStickers.map(x=>rawColor(x.combination)).filter(Boolean))];
    if(options.length){const error=new ColorConfirmationRequired([{style:norm(row.style),colorCode:norm(row.colorCode),providerColor:name,gestionColor:'',options}]);error.code='COLOR_SELECTION';throw error;}
   }
   if(stickers.length!==1)fail('Se encontraron '+stickers.length+' stickers VINH300 en Gestion/Excel para esta combinacion y NP '+source.productionOrders+'. Revisa el articulo, color, talla y NP del sticker.',row);
   const sticker=stickers[0];
   if(color(sticker.combination)!==name){
    const proposal={style:norm(row.style),colorCode:norm(row.colorCode),providerColor:name,gestionColor:rawColor(sticker.combination)};
    if(!confirmedColors.some(c=>confirmationKey(c)===confirmationKey(proposal)))pending.set(confirmationKey(proposal),proposal);
   }
   source=sticker;
  }
  if(!Number.isFinite(source.purchased)||source.purchased<0)fail('La cantidad comprada en Gestion/Excel no es valida: '+source.purchased+'. Revisa la cantidad de la OC.',row);
  const identity=row.kind+'|'+key(source);
  if(used.has(identity))fail('Hay una fila duplicada en el proveedor para esta combinacion. Revisa las filas de '+row.kind+'.',row);used.add(identity);
  plan.push({id:row.id,kind:row.kind,color:name,size:row.size,value:String(source.purchased),previous:row.quantity,productionOrders:source.productionOrders,style:row.style,colorCode:row.colorCode,barcode:row.barcode});
 }
 if(plan.length!==selected.length*2||!plan.length)fail('El numero de filas no coincide: proveedor '+plan.length+', esperado '+(selected.length*2)+' ('+selected.length+' RFID y '+selected.length+' UPC). Revisa que ambas pestanas contengan todas las tallas y colores del PO.');
 if(pending.size)throw new ColorConfirmationRequired([...pending.values()]);
 return plan;
}
module.exports={planQuantities,MatchError,ColorConfirmationRequired};


