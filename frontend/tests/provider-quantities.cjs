const assert=require('node:assert/strict');
const {planQuantities}=require('../tools/provider-quantities.cjs');
const rows=[],portal=[];let id=0;
for(const color of ['LOTUS','TWILIGHT'])for(const [size,quantity] of [['L',135],['M',130],['S',60],['XL',70],['XS',10],['XXL',20]]){
 const common={customerStyle:'STYLE1 -',measure:size+'/TEST -',productionOrders:'NP1',purchased:quantity};
 rows.push({...common,item:'PT000001-PRICE TICKET VINCEH501RFID',combination:'1 - PO : PO-000001 COLOR : '+color+' '+color});
 rows.push({...common,item:'ST000001-STICKER VINH300',combination:'2 - COLOR : '+color+' '+color});
 for(const kind of ['RFID','UPC'])portal.push({id:String(++id),kind,style:'STYLE1',size,color:kind==='UPC'?color:'',colorCode:color,barcode:color+size,quantity:'999'});
}
const plan=planQuantities(rows,portal,'PO-000001');
assert.equal(plan.length,24);assert.equal(plan.filter(x=>x.kind==='RFID').length,12);assert.ok(plan.every(x=>x.previous==='999'&&x.value!=='999'));
assert.throws(()=>planQuantities([...rows,rows[0]],portal,'PO-000001'));
assert.throws(()=>planQuantities(rows,portal,'PO-000002'));
assert.throws(()=>planQuantities(rows,portal.slice(1),'PO-000001'));
assert.throws(()=>planQuantities(rows,portal.map(x=>({...x,color:''})),'PO-000001'));
assert.throws(()=>planQuantities([...rows,{...rows[0],combination:rows[0].combination.replace('PO-000001','PO-000002')}],portal,'PO-000001'));
assert.throws(()=>planQuantities(rows.map((x,i)=>i===0?{...x,purchased:NaN}:x),portal,'PO-000001'));
console.log('PASS: 24 overwrites; ambiguous/missing PO, duplicate, color, count and invalid quantity rejected.');const aliasRows=rows.map(x=>({...x,combination:x.combination.replaceAll('TWILIGHT','BURN HENNA')}));
const aliasPortal=portal.map(x=>({...x,color:x.color==='TWILIGHT'?'BURNT HENNA':x.color}));
assert.equal(planQuantities(aliasRows,aliasPortal,'PO-000001').length,24);
const unknown=portal.map(x=>({...x,color:x.color==='TWILIGHT'?'UNKNOWN COLOR':x.color}));
let mismatch;try{planQuantities(rows,unknown,'PO-000001');}catch(error){mismatch=error;}
assert.equal(mismatch.code,'COLOR_SELECTION');assert(mismatch.confirmations.every(c=>c.options.length>0));
console.log('PASS: unknown colors request explicit selection from Gestion colors.');
const longRows=rows.map(x=>({...x,combination:x.combination.replaceAll('TWILIGHT','BLACK/MED H GREY BLACK/ME').replaceAll('LOTUS','ARMY GREEN / H. GREY ARMY')}));
const longPortal=portal.map(x=>({...x,color:x.color==='TWILIGHT'?'BLACK/MED H GREY':x.color==='LOTUS'?'ARMY GREEN / H. GREY':x.color}));
let confirmation;try{planQuantities(longRows,longPortal,'PO-000001');}catch(error){confirmation=error;}
assert.equal(confirmation.code,'COLOR_CONFIRMATION');assert.equal(confirmation.confirmations.length,2);
assert.ok(confirmation.confirmations.some(c=>c.providerColor==='BLACK/MED H GREY'&&c.gestionColor==='BLACK/MED H GREY BLACK/ME'));
assert.equal(planQuantities(longRows,longPortal,'PO-000001',confirmation.confirmations).length,24);
assert.throws(()=>planQuantities(longRows,longPortal,'PO-000001',confirmation.confirmations.map(c=>({...c,style:'OTHER STYLE'}))),e=>e.code==='COLOR_CONFIRMATION');
assert.throws(()=>planQuantities(longRows.map(x=>({...x,combination:x.combination.replaceAll('H GREY BLACK/ME','H GREY BLACK/MEDIUM')})),longPortal,'PO-000001',confirmation.confirmations),e=>e.code==='COLOR_CONFIRMATION');
assert.throws(()=>planQuantities([...longRows,{...longRows[0],combination:longRows[0].combination.replaceAll('ARMY GREEN / H. GREY ARMY','ARMY GREEN / H. GREY EXTRA')}],longPortal,'PO-000001'),e=>['MATCH_ERROR','COLOR_SELECTION'].includes(e.code));
assert.throws(()=>planQuantities(longRows,longPortal.map((x,i)=>i===0?{...x,size:'MISSING'}:x),'PO-000001'),e=>e.code==='MATCH_ERROR');
console.log('PASS: long color text requires confirmation; approval works; stale, wrong-style, ambiguous and missing-size cases remain blocked.');

// Regression: ticket has long descriptive text, sticker has the plain color.
const mixedRows=longRows.map(x=>x.item.startsWith('ST')?{...x,combination:x.combination.replaceAll('BLACK/MED H GREY BLACK/ME','BLACK/MED H GREY').replaceAll('ARMY GREEN / H. GREY ARMY','ARMY GREEN / H. GREY')}:x);
let mixedConfirmation;try{planQuantities(mixedRows,longPortal,'PO-000001');}catch(error){mixedConfirmation=error;}
assert.equal(mixedConfirmation.code,'COLOR_CONFIRMATION');assert.equal(mixedConfirmation.confirmations.length,2);
assert.equal(planQuantities(mixedRows,longPortal,'PO-000001',mixedConfirmation.confirmations).length,24);
// A sticker with its own long text also requires approval of that exact text.
const otherText=mixedRows.map(x=>x.item.startsWith('ST')?{...x,combination:x.combination.replaceAll('BLACK/MED H GREY','BLACK/MED H GREY STICKER')}:x);
assert.throws(()=>planQuantities(otherText,longPortal,'PO-000001',mixedConfirmation.confirmations),e=>e.code==='COLOR_CONFIRMATION'&&e.confirmations.some(c=>c.gestionColor==='BLACK/MED H GREY STICKER'));
const duplicateSticker=mixedRows.find(x=>x.item.startsWith('ST'));
assert.throws(()=>planQuantities([...mixedRows,duplicateSticker],longPortal,'PO-000001'),e=>e.code==='MATCH_ERROR');
const otherPo={...mixedRows[0],combination:mixedRows[0].combination.replace('PO-000001','PO-000002')};
assert.throws(()=>planQuantities([...mixedRows,otherPo],longPortal,'PO-000001'),e=>e.code==='MATCH_ERROR');
console.log('PASS: differing ticket/sticker descriptions prompt correctly; independent sticker approval, duplicate and multi-PO guards preserved.');
const zeroSource=rows.filter(r=>!(r.combination.includes('LOTUS')&&r.measure.startsWith('XS/')));
const zeroPortal=portal.map(r=>r.colorCode==='LOTUS'&&r.size==='XS'?{...r,quantity:'0',poQuantity:0}:r);
let zeroConfirmation;try{planQuantities(zeroSource,zeroPortal,'PO-000001')}catch(e){zeroConfirmation=e}
assert.equal(zeroConfirmation.code,'COLOR_CONFIRMATION');assert.equal(zeroConfirmation.confirmations.length,2);assert(zeroConfirmation.confirmations.every(c=>c.gestionColor.includes('Conservar esta fila en 0')));
const zeroPlan=planQuantities(zeroSource,zeroPortal,'PO-000001',zeroConfirmation.confirmations);assert.equal(zeroPlan.length,22);assert(!zeroPlan.some(r=>r.size==='XS'&&r.color==='LOTUS'));
for(const modification of [{quantity:'1',poQuantity:0},{quantity:'0',poQuantity:1},{quantity:'0',poQuantity:null}])assert.throws(()=>planQuantities(zeroSource,zeroPortal.map(r=>r.colorCode==='LOTUS'&&r.size==='XS'?{...r,...modification}:r),'PO-000001'),e=>e.code==='MATCH_ERROR');
console.log('PASS: missing zero sizes prompt before preserving them unchanged; nonzero or unverified quantities stay blocked.');

const spacingRows=rows.map(r=>({...r,combination:r.combination.replaceAll('TWILIGHT','BAYLEAF')}));
const spacingPortal=portal.map(r=>({...r,color:r.color==='TWILIGHT'?'BAY LEAF':r.color}));
let spacing;try{planQuantities(spacingRows,spacingPortal,'PO-000001')}catch(e){spacing=e}
assert.equal(spacing.code,'COLOR_CONFIRMATION');
assert(spacing.confirmations.some(c=>c.providerColor==='BAY LEAF'&&c.gestionColor==='BAYLEAF'));
assert.equal(planQuantities(spacingRows,spacingPortal,'PO-000001',spacing.confirmations).length,24);
assert.throws(()=>planQuantities(spacingRows,spacingPortal,'PO-000001',spacing.confirmations.map(c=>({...c,gestionColor:'OTHER'}))),e=>e.code==='COLOR_CONFIRMATION');
assert.throws(()=>planQuantities([...spacingRows,...spacingRows.filter(r=>r.combination.includes('BAYLEAF')).map(r=>({...r,combination:r.combination.replace('BAYLEAF','BAY LEAF')}))],spacingPortal,'PO-000001'),e=>['MATCH_ERROR','COLOR_SELECTION'].includes(e.code));
console.log('PASS: BAY LEAF / BAYLEAF requires explicit approval, rejected/stale approval never fills; duplicate variants remain blocked.');
