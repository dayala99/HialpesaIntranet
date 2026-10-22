const fs=require('node:fs'),path=require('node:path'),os=require('node:os');
const {MatchError}=require('./provider-quantities.cjs');
async function openPreview(page,command,setStage=()=>{}){
 setStage('preview-next');
 let shippingScope;for(const frame of page.frames())if(await frame.evaluate(()=>/Customer\s+Reference\s*#/i.test(document.body?.innerText||'')).catch(()=>false)){shippingScope=frame;break;}if(!shippingScope)throw new MatchError('No se encontró el formulario de envío antes de Preview.');
 const next=shippingScope.getByRole('button',{name:/^Next\s*>?$/i}).filter({visible:true}).last();await next.click();
 let scope;const deadline=Date.now()+45000;
 while(!scope&&Date.now()<deadline){for(const frame of page.frames())if(await frame.evaluate(()=>/Master Web Order Number/i.test(document.body?.innerText||'')).catch(()=>false)){scope=frame;break;}if(!scope)await page.waitForTimeout(200);}
 if(!scope)throw new MatchError('No se encontró la pantalla Preview después de Next.');
 const pos=await scope.evaluate(()=>[...new Set((document.body.innerText.match(/PO-\d+/g)||[]))]);
 if(pos.length!==1||pos[0]!==command.po)throw new MatchError('Preview no corresponde únicamente a '+command.po+'.');
 setStage('preview-expand');
 // The first cell of the PO/Item header contains the portal's expand-all arrow.
 const expandAll=await scope.evaluate(()=>{
  const visible=e=>!!e.getClientRects().length;
  const norm=s=>s.replace(/\s+/g,' ').trim().toUpperCase();
  const rows=[...document.querySelectorAll('tr')].filter(tr=>{const cells=[...tr.cells];return cells.some(c=>norm(c.textContent)==='PO#')&&cells.some(c=>norm(c.textContent)==='ITEM')&&cells.some(c=>norm(c.textContent)==='TOTAL QUANTITY');});
  const arrows=rows.flatMap(tr=>[...tr.cells[0].querySelectorAll('img,input[type="image"],button,a,[role="button"]')].filter(visible));
  const leaf=arrows.filter(e=>!arrows.some(other=>other!==e&&e.contains(other)));
  if(leaf.length!==1)return false;
  const arrow=leaf[0],label=[arrow.getAttribute('src'),arrow.getAttribute('alt'),arrow.getAttribute('title')].join(' ');
  if(/collapse|(?:^|[/_ -])up(?:[._ -]|$)/i.test(label)||arrow.getAttribute('aria-expanded')==='true')return false;
  arrow.setAttribute('data-vince-expand-all','true');return true;
 });
 if(expandAll){
  await scope.locator('[data-vince-expand-all="true"]').click();
  await scope.waitForFunction(()=>[...document.querySelectorAll('table')].filter(t=>t.getClientRects().length&&!t.querySelector('table')&&/Quantity To Order/i.test(t.textContent)&&/BARCODE/i.test(t.textContent)).length>=2,null,{timeout:15000});
 }

 for(let pass=0;pass<100;pass++){
  const marked=await scope.evaluate(()=>{
   const candidates=[...document.querySelectorAll('img,[aria-expanded="false"]')].filter(e=>e.getClientRects().length&&!e.hasAttribute('data-vince-expanded'));
   const e=candidates.find(e=>{const table=e.closest('table');if(!table||!/VINCEH501\s*RFID|VINH300/i.test(table.textContent))return false;const label=[e.getAttribute('src'),e.getAttribute('alt'),e.getAttribute('title')].join(' ');return e.getAttribute('aria-expanded')==='false'||(/down|expand|plus/i.test(label)&&!/collapse|up\./i.test(label));});
   if(!e)return false;e.setAttribute('data-vince-expanded','pending');return true;
  });
  if(!marked)break;
  await scope.locator('[data-vince-expanded="pending"]').click();
  await scope.locator('[data-vince-expanded="pending"]').evaluate(e=>e.setAttribute('data-vince-expanded','done')).catch(()=>{});
 }
 await scope.waitForFunction(()=>[...document.querySelectorAll('table')].filter(t=>t.getClientRects().length&&/Quantity To Order/i.test(t.textContent)&&/BARCODE/i.test(t.textContent)&&!t.querySelector('table')).length>=2,null,{timeout:15000});
 const snapshot=await scope.evaluate(()=>{
  const norm=s=>s.replace(/\s+/g,' ').trim();const details=[];
  const itemCells=[...document.querySelectorAll('td,th')].filter(e=>!e.querySelector('table')&&/^(VINCEH501\s+RFID|VINH300)(?:\s+Edit)?$/i.test(norm(e.innerText)));
  for(const table of document.querySelectorAll('table')){
   if(!table.getClientRects().length||table.querySelector('table'))continue;
   const trs=[...table.rows];const index=trs.findIndex(tr=>/Quantity To Order/i.test(tr.innerText)&&/BARCODE/i.test(tr.innerText));if(index<0)continue;
   const headers=[...trs[index].cells].map(c=>norm(c.innerText).toUpperCase());
   const preceding=itemCells.filter(c=>c.compareDocumentPosition(table)&Node.DOCUMENT_POSITION_FOLLOWING).pop();
   const item=preceding?norm(preceding.innerText).replace(/\s+Edit$/i,''):'';
   for(const tr of trs.slice(index+1)){const cells=[...tr.cells].map(c=>norm(c.innerText));if(cells.length!==headers.length)continue;const row=Object.fromEntries(headers.map((h,i)=>[h,cells[i]]));if(row.BARCODE)details.push({item,...row});}
  }
  return {webOrder:document.body.innerText.match(/Master Web Order Number\s*:?\s*(AD-[0-9]+)/i)?.[1]||'',details};
 });
 if(!snapshot.details.some(r=>/^VINCEH501\s+RFID$/i.test(r.item))||!snapshot.details.some(r=>r.item==='VINH300'))throw new MatchError('No se pudieron leer las tablas desplegadas de RFID y stickers.');
 setStage('preview-report');
 const plans=JSON.parse(fs.readFileSync('test-results/avery-quantities.json','utf8')).rows||[];
 const report=await writeReport(command,snapshot,plans);
 fs.writeFileSync('test-results/avery-preview.json',JSON.stringify({po:command.po,...snapshot,report,submitted:false},null,2));
 return report;
}
async function writeReport(command,snapshot,plans,folder=process.env.VINCE_REPORT_DIRECTORY||path.join(os.homedir(),'Downloads','Reportes VINCE')){
 const ExcelJS=require('exceljs');const book=new ExcelJS.Workbook();book.creator='HIALPESA';
 const summary=book.addWorksheet('Resumen');summary.columns=[{header:'NP',width:16},{header:'PO',width:23},{header:'ESTILO',width:22},{header:'COD. COLOR',width:16},{header:'COLOR',width:28},{header:'PRECIO',width:13}];
 const norm=s=>String(s||'').trim().toUpperCase();const records=new Map();const seen=new Set();const detail=book.addWorksheet('Detalle Preview');
 detail.columns=['ITEM','PO','NP','ESTILO','COD. COLOR','COLOR','TALLA','CANT. PEDIR','CANT. RECIBIR','UOM','BARCODE','MSRP'].map(h=>({header:h,width:h==='ITEM'?23:18}));
 for(const row of snapshot.details){
  const kind=/RFID/i.test(row.item)?'RFID':'UPC',style=row.STYLE,colorCode=row['COLOR CODE']||row.COLOR||'',size=row['SIZE CODE']||row.SIZE;
  const matches=plans.filter(p=>p.kind===kind&&norm(p.style)===norm(style)&&norm(p.colorCode)===norm(colorCode)&&norm(p.size)===norm(size)&&norm(p.barcode)===norm(row.BARCODE));
  if(matches.length!==1){if(Number(row['QUANTITY TO ORDER'].replace(/,/g,''))===0)continue;throw new MatchError('No hay una NP única validada para '+row.item+' / '+style+' / '+colorCode+' / '+size+'.');}
  const plan=matches[0];if(seen.has(plan.id))throw new MatchError('Fila duplicada en Preview: '+plan.id);seen.add(plan.id);const quantity=Number(row['QUANTITY TO ORDER'].replace(/,/g,''));if(quantity!==Number(plan.value))throw new MatchError('La cantidad en Preview cambió para '+row.item+' / '+size+'.');
  const price=row.MSRP==null?null:Number(row.MSRP.replace(/,/g,''));if(price!==null&&!Number.isFinite(price))throw new MatchError('MSRP no válido en Preview.');
  const color=row['COLOR DESCRIPTION']||plan.color;const key=JSON.stringify([plan.productionOrders,style,colorCode]);
  const record=records.get(key)||{np:plan.productionOrders,style,colorCode,color,price:null};if(price!==null){if(record.price!==null&&record.price!==price)throw new MatchError('Hay precios distintos para el mismo estilo y color.');record.price=price;}records.set(key,record);
  detail.addRow([row.item,command.po,plan.productionOrders,style,colorCode,color,size,quantity,Number(row['QUANTITY TO RECEIVE'].replace(/,/g,'')),row['QUANTITY UOM'],row.BARCODE,price]);
 }
 if(plans.some(p=>!seen.has(p.id)))throw new MatchError('Faltan filas de cantidades en Preview.');
 if(!records.size)throw new MatchError('Preview no contiene filas validadas para generar el reporte.');
 for(const r of records.values())if(r.price===null)throw new MatchError('Falta MSRP para '+r.style+' / '+r.colorCode+'.');
 for(const r of records.values())summary.addRow([r.np,command.po,r.style,r.colorCode,r.color,r.price]);
 summary.getColumn(6).numFmt='0.00';
 for(const sheet of [summary,detail]){sheet.views=[{state:'frozen',ySplit:1}];sheet.autoFilter={from:'A1',to:{row:sheet.rowCount,column:sheet.columnCount}};sheet.eachRow((row,index)=>{row.height=index===1?32:21;row.eachCell({includeEmpty:true},cell=>{cell.font={name:'Arial',size:10,bold:index===1};cell.alignment={vertical:'middle',horizontal:'center',wrapText:true};cell.border={top:{style:'thin'},left:{style:'thin'},bottom:{style:'thin'},right:{style:'thin'}};});});}
 fs.mkdirSync(folder,{recursive:true});const name='OC '+command.series+'-'+command.number;let file=path.join(folder,name+'.xlsx');for(let copy=2;fs.existsSync(file);copy++)file=path.join(folder,name+' ('+copy+').xlsx');await book.xlsx.writeFile(file);return file;
}
module.exports={openPreview,writeReport};
