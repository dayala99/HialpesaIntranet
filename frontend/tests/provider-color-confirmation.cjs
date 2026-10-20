const {chromium}=require('playwright'),assert=require('node:assert/strict');
const initial=[{style:'MO1909170B',colorCode:'019BHG',providerColor:'BLACK/MED H GREY',gestionColor:'BLACK/MED H GREY BLACK/ME'}];
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true});
 try{for(const choice of ['No','S\u00ed, confirmar y llenar','Escape']){
  const page=await browser.newPage({viewport:{width:1280,height:900}});const calls=[];
  await page.route('**/api/v1/**',async route=>{
   const url=route.request().url();let data;
   if(url.endsWith('/sesion'))data={displayName:'Prueba',gestionUser:'test',canWriteDatabase:false,demo:false};
   else if(url.endsWith('/pos'))data=['PO-00054166'];
   else if(url.endsWith('/iniciar-sesion')){
    const body=route.request().postDataJSON();calls.push(body);
    data=body.colorConfirmations?.length?{status:'po-filled',message:'Cantidades completadas (prueba simulada)'}:{status:'confirmation-required',message:'Confirma los colores',confirmations:initial};
   }else if(url.includes('/ordenes?'))data={rows:[{series:'002',number:'350000',supplier:'Proveedor de prueba',class:'01',status:'L'}],page:1,hasMore:false};
   else throw Error('Unexpected mocked endpoint');
   await route.fulfill({json:data});
  });
  await page.goto('http://localhost:4200/logistica/avios/nueva');
  await page.getByRole('button',{name:'Abrir Avery Dennison para OC 002-350000',exact:true}).click();
  await page.getByRole('dialog').getByRole('button',{name:'PO-00054166',exact:true}).click();
  const popup=page.getByRole('dialog');await popup.getByRole('heading',{name:'Confirmar coincidencia de colores'}).waitFor();
  await popup.getByText('BLACK/MED H GREY',{exact:true}).waitFor();await popup.getByText('BLACK/MED H GREY BLACK/ME',{exact:true}).waitFor();
  assert.equal(calls.length,1);assert.deepEqual(calls[0].colorConfirmations,[]);
  if(choice==='No'){
   await popup.screenshot({path:'test-results/color-confirmation-popup.png'});
   await page.setViewportSize({width:390,height:844});await popup.screenshot({path:'test-results/color-confirmation-popup-mobile.png'});
  }
  if(choice==='Escape')await page.keyboard.press('Escape');else await popup.getByRole('button',{name:choice,exact:true}).click();
  if(choice==='S\u00ed, confirmar y llenar'){
   await page.getByText('Cantidades completadas (prueba simulada)',{exact:true}).waitFor();assert.equal(calls.length,2);assert.deepEqual(calls[1].colorConfirmations,initial);assert.equal(calls[1].po,'PO-00054166');
  }else{
   await page.getByText('Llenado cancelado. No se cambiaron cantidades.',{exact:true}).waitFor();assert.equal(calls.length,1);
  }
  await page.close();console.log('PASS popup: '+choice);
 }}finally{await browser.close();}
})().catch(error=>{console.error(error);process.exit(1)});