const { chromium } = require('@playwright/test');
const { existsSync } = require('node:fs');
const assert = require('node:assert/strict');

(async()=>{
  const executablePath=[process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,'C:/Program Files/Google/Chrome/Application/chrome.exe','C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p=>p&&existsSync(p));
  const browser=await chromium.launch({headless:true,executablePath});
  try{
    const page=await browser.newPage({viewport:{width:1440,height:1000}});
    page.setDefaultTimeout(20000);
    const sessionPromise=page.waitForResponse(r=>r.url()==='http://localhost:5080/api/v1/sesion');
    await page.goto('http://localhost:4200/logistica/avios');
    const sessionResponse=await sessionPromise;
    assert.equal(sessionResponse.status(),200);
    const session=await sessionResponse.json();
    assert.equal(session.demo,false);
    assert.equal(session.gestionUser,'programador04');
    await page.getByLabel('Serie',{exact:true}).fill('002');
    await page.getByLabel('Número de OC',{exact:true}).fill('349873');
    const orderPromise=page.waitForResponse(r=>r.url().endsWith('/ordenes/002/349873'));
    await page.locator('.search-form button[type="submit"]').click();
    const response=await orderPromise;
    assert.equal(response.status(),200);
    const detail=await response.json();
    assert.equal(detail.items.length,298);
    assert.match(detail.order.supplier,/AVERY/);
    await page.getByText('298 secuencias').waitFor();
    const requirementsPromise=page.waitForResponse(r=>r.url().includes('/items/')&&r.url().endsWith('/requerimientos'));
    await page.getByRole('button',{name:'Ver NPs'}).first().click();
    const requirementsResponse=await requirementsPromise;
    assert.equal(requirementsResponse.status(),200);
    assert.ok((await requirementsResponse.json()).length>0);
    await page.getByRole('button',{name:'Usar como base de matriz'}).waitFor();
    console.log('OK: Angular → API Windows programador04 → SQL real; OC 002-349873 con 298 ítems y requerimientos recuperados. Sin escrituras.');
  }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
