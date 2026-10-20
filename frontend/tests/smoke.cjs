const { chromium } = require('@playwright/test');
const ExcelJS = require('exceljs');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { existsSync } = require('node:fs');

(async () => {
  const output = path.join(__dirname, '..', 'test-results');
  await fs.mkdir(output, { recursive: true });
  const book = new ExcelJS.Workbook();
  book.addWorksheet('Sin formato').addRow(['Otra columna']);
  book.addWorksheet('Matriz').addRows([
    ['NP','Estilo','Color','Talla','Cantidad','Fabrica','Presentacion','Destino'],
    ['NP-A','EST-001','Negro','S',21,'001','1',''],
    ['NP-B','EST-001','Negro','M',31,'001','1','']
  ]);
  const fixture = path.join(output, 'matriz-valida.xlsx');
  await book.xlsx.writeFile(fixture);
  const executablePath = [process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(p=>p && existsSync(p));
  const browser = await chromium.launch({ headless: true, executablePath });
  try {
    const page = await browser.newPage({ viewport: { width:1440, height:1000 } });
    page.setDefaultTimeout(15000);
    await page.emulateMedia({ reducedMotion: 'reduce' });
    const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    await page.goto('http://localhost:4200');
    await page.getByRole('heading', { name:'Tu espacio de trabajo' }).waitFor();
    await page.getByRole('link', { name:'Abrir compras de avíos' }).click();
    console.log('Página de avíos abierta');
    await page.getByRole('button', { name:'002-000001', exact:true }).click();
    await page.getByRole('cell', { name:'AV-DEMO-01', exact:false }).waitFor();
    console.log('OC consultada');
    await page.getByRole('button', { name:'Ver NPs' }).first().click();
    await page.getByRole('button', { name:'Usar como base de matriz' }).click();
    await page.getByText('Se cargó la base del ítem seleccionado.', { exact:false }).waitFor();
    await page.getByRole('button', { name:'Subir Excel' }).click();
    await page.locator('input[type=file]').setInputFiles(fixture);
    await page.getByRole('alert').filter({ hasText:'Falta la columna' }).waitFor();
    await page.getByLabel('Hoja', { exact:true }).selectOption('Matriz');
    await page.getByRole('button', { name:'Importar 2 filas' }).click();
    await page.getByLabel('Código de artículo').nth(1).fill('AV-DEMO-02');
    await page.getByLabel('Descripción', { exact:true }).nth(1).fill('Sticker de empaque');
    await page.evaluate(()=>window.scrollTo(0,0));
    await page.screenshot({ path:path.join(output,'avios-desktop.png'), fullPage:true });
    await page.getByRole('button', { name:'Previsualizar compra' }).click();
    await page.getByRole('button', { name:'Descargar para Calidad' }).waitFor();
    const metrics = await page.locator('.metrics').innerText();
    assert.match(metrics,/52/); assert.match(metrics,/120/); assert.match(metrics,/16/);
    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name:'Descargar para Calidad' }).click();
    const download = await downloadPromise;
    const exported = path.join(output,'calidad.xlsx');
    await download.saveAs(exported);
    const exportBook = new ExcelJS.Workbook(); await exportBook.xlsx.readFile(exported);
    assert.equal(exportBook.worksheets[0].rowCount,5);
    assert.equal(exportBook.worksheets[0].getRow(1).getCell(14).value,'Revision');
    await page.evaluate(()=>window.scrollTo(0,0));
    await page.screenshot({ path:path.join(output,'avios-resultados.png'), fullPage:true });
    for (const width of [320,390,768,1024,1440]) {
      await page.setViewportSize({width,height:950});
      const overflow = await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth);
      assert.equal(overflow,false,`Hay desbordamiento de página a ${width}px`);
      if(width<760) assert.ok(await page.locator('.sidebar').evaluate(el=>el.getBoundingClientRect().right<=1),'El menú móvil debe permanecer cerrado');
      if(width===390) { await page.evaluate(()=>window.scrollTo(0,0)); await page.screenshot({path:path.join(output,'avios-mobile.png'),fullPage:true}); }
    }
    await page.setViewportSize({width:390,height:950});
    await page.getByRole('button',{name:'Abrir navegación'}).click();
    await page.getByRole('link',{name:'Inicio',exact:true}).click();
    await page.getByRole('heading',{name:'Tu espacio de trabajo'}).waitFor();
    const invalid = await page.request.post('http://localhost:5080/api/v1/logistica/avios/previsualizacion',{data:{rows:[{productionOrder:'NP-A',style:'EST',color:'Negro',size:'S',quantity:21}],articles:[{itemCode:'A',description:'Etiqueta',multiple:0}]}});
    assert.equal(invalid.status(),400);
    assert.deepEqual(errors,[]);
    console.log('OK: consulta OC, requerimientos, importación con selección de hoja, validación, cálculo API, Excel de Calidad, navegación móvil y 5 anchos sin desbordamiento.');
  } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exitCode=1;});
