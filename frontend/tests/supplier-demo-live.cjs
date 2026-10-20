const {chromium}=require('@playwright/test');
const {existsSync}=require('node:fs');
const assert=require('node:assert/strict');

(async()=>{
 const executablePath=[process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,'C:/Program Files/Google/Chrome/Application/chrome.exe','C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p=>p&&existsSync(p));
 const browser=await chromium.launch({headless:true,executablePath});
 try{
  const context=await browser.newContext();let blockedWrites=0;
  await context.route('**/api/v1/**',route=>{
   if(!['GET','OPTIONS'].includes(route.request().method())){blockedWrites++;return route.abort();}
   return route.continue();
  });
  const page=await context.newPage();page.setDefaultTimeout(20000);
  await page.goto('http://localhost:4200/logistica/avios');
  await page.getByLabel('Serie',{exact:true}).fill('002');await page.getByLabel('Número de OC',{exact:true}).fill('349873');
  const responsePromise=page.waitForResponse(r=>r.url().endsWith('/ordenes/002/349873'));
  await page.locator('.search-form button[type=submit]').click();const response=await responsePromise;
  assert.equal(response.status(),200);const detail=await response.json();assert.ok(detail.items.length>0);
  const popupPromise=page.waitForEvent('popup');await page.getByRole('button',{name:'Probar llenado en página nueva',exact:true}).click();
  const popup=await popupPromise;popup.setDefaultTimeout(20000);
  await popup.getByRole('status').filter({hasText:`Llenado completo: ${detail.items.length} campos`}).waitFor();
  const received=await popup.locator('input[type=number]').evaluateAll(inputs=>inputs.map(input=>({sequence:input.dataset.sequence,required:input.valueAsNumber})));
  assert.deepEqual(received,detail.items.map(item=>({sequence:item.sequence,required:item.required})));
  assert.equal(blockedWrites,0);
  console.log(`OK lectura real: OC 002-349873, ${received.length} campos completados con sus cantidades requeridas originales. Sin POST ni escrituras SQL.`);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
