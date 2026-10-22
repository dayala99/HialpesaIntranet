import { Injectable } from '@angular/core';
import { MatrixRow, Preview, PrintOrder } from '../../core/models';

@Injectable({ providedIn: 'root' })
export class ExcelService {
  private async createWorkbook(){
    const excel=await import('exceljs');
    const Workbook=excel.Workbook ?? (excel as unknown as {default?:typeof excel}).default?.Workbook;
    if(!Workbook)throw new Error('No se pudo cargar la herramienta de Excel. Recarga la página e inténtalo nuevamente.');
    return new Workbook();
  }
  async open(file: File) {
    if (!/\.xlsx$/i.test(file.name)) throw Error('Selecciona un archivo Excel .xlsx.');
    if (file.size > 5 * 1024 * 1024) throw Error('El archivo debe pesar como máximo 5 MB.');
    const workbook = await this.createWorkbook();
    await workbook.xlsx.load(await file.arrayBuffer());
    if (!workbook.worksheets.length) throw Error('El archivo no contiene hojas.');
    return workbook;
  }
  read(workbook: import('exceljs').Workbook, sheetName: string): MatrixRow[] {
    const sheet = workbook.getWorksheet(sheetName);
    if (!sheet) throw Error('Selecciona una hoja válida.');
    if (sheet.rowCount > 501 || sheet.columnCount > 30) throw Error('La plantilla admite hasta 500 filas y 30 columnas.');
    const normalize = (s: string) => s.trim().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const headers = new Map<string, number>();
    sheet.getRow(1).eachCell((cell, index) => {
      const name = normalize(cell.text);
      if (name && headers.has(name)) throw Error(`Encabezado duplicado: ${cell.text}.`);
      if (name) headers.set(name, index);
    });
    for (const name of ['np', 'estilo', 'color', 'talla', 'cantidad']) if (!headers.has(name)) throw Error(`Falta la columna ${name}. Descarga la plantilla para revisar el formato.`);
    const rows: MatrixRow[] = [];
    const seen = new Set<string>();
    for (let index = 2; index <= sheet.rowCount; index++) {
      const row = sheet.getRow(index);
      if (!row.hasValues) continue;
      const get = (name: string) => headers.has(name) ? row.getCell(headers.get(name)!).text.trim() : '';
      const cell = row.getCell(headers.get('cantidad')!);
      if (cell.type === 6) throw Error(`Fila ${index}: usa un valor de cantidad, sin fórmula.`);
      const quantity = typeof cell.value === 'number' ? cell.value : Number(get('cantidad'));
      if (!get('np') || !get('estilo') || !get('color') || !get('talla') || !get('cantidad') || !Number.isFinite(quantity) || quantity < 0 || quantity > 100000000) throw Error(`Fila ${index}: completa NP, estilo, color, talla y una cantidad numérica válida.`);
      const value: MatrixRow = { productionOrder: get('np'), style: get('estilo'), color: get('color'), size: get('talla'), quantity, factory: get('fabrica'), presentationCode: get('presentacion'), destination: get('destino') };
      const key = JSON.stringify([value.factory,value.productionOrder,value.style,value.presentationCode,value.color,value.size,value.destination].map(v=>v.toLowerCase()));
      if (seen.has(key)) throw Error(`Fila ${index}: combinación duplicada de NP, presentación, estilo, color, talla y destino.`);
      seen.add(key); rows.push(value);
    }
    if (!rows.length) throw Error('La hoja seleccionada no tiene información.');
    return rows;
  }
  async template() {
    const workbook = await this.createWorkbook();
    const sheet = workbook.addWorksheet('Matriz');
    sheet.addRows([['NP','Estilo','Color','Talla','Cantidad','Fabrica','Presentacion','Destino'],['NP-EJEMPLO','EST-001','Negro','S',21,'001','1','']]);
    await this.download(workbook, 'plantilla-matriz-avios.xlsx');
  }
  async export(preview: Preview, quality: boolean) {
    const workbook = await this.createWorkbook();
    const sheet = workbook.addWorksheet(quality ? 'Revision de cantidades' : 'Datos de compra');
    const headers = ['Fabrica','NP','Estilo','Presentacion','Color','Talla','Destino','Codigo articulo','Articulo','Cantidad base','Multiplo','Cantidad compra','Ajuste por redondeo'];
    if (quality) headers.push('Revision','Observaciones');
    sheet.addRow(headers);
    preview.rows.forEach(r => {
      const values: (string | number)[] = [r.source.factory,r.source.productionOrder,r.source.style,r.source.presentationCode,r.source.color,r.source.size,r.source.destination,r.itemCode,r.article,r.required,r.multiple,r.purchase,r.extra];
      if (quality) values.push('Pendiente','');
      sheet.addRow(values);
    });
    await this.download(workbook, `${quality ? 'calidad-cantidades' : 'datos-compra'}-${new Date().toISOString().slice(0,10)}.xlsx`);
  }
  async purchaseOrder(oc:PrintOrder){
    const book=await this.createWorkbook();book.creator='Hialpesa Intranet';
    const sheet=book.addWorksheet('Detalle OC');
    sheet.columns=[60,65,18,24,20,22].map(width=>({width}));
    sheet.mergeCells('A1:F1');
    sheet.getCell('A1').value='Orden de Compra: '+oc.order.series+' - '+oc.order.number;
    sheet.getCell('A1').font={bold:true,size:16};
    sheet.getCell('A1').alignment={horizontal:'left',vertical:'middle'};
    sheet.getRow(1).height=34;
    sheet.addRow([]);
    sheet.addRow(['ÍTEM','COMB.','MEDIDA','EST. CLIENTE','CANT. COMPRADA','NP']);
    sheet.getRow(3).height=32;
    sheet.getRow(3).eachCell(cell=>{cell.font={bold:true};cell.fill={type:'pattern',pattern:'solid',fgColor:{argb:'FFC8FFFF'}};cell.alignment={horizontal:'center',vertical:'middle',wrapText:true};});
    for(const line of oc.reportRows){
      const row=sheet.addRow([line.item,line.combination,line.measure,line.customerStyle,line.purchased,line.productionOrders]);
      row.height=30;row.eachCell({includeEmpty:true},cell=>{cell.alignment={vertical:'middle',wrapText:true};});
      row.getCell(5).numFmt=Number.isInteger(line.purchased) ? '#,##0' : '#,##0.#####';
    }
    sheet.eachRow(row=>row.eachCell({includeEmpty:true},cell=>cell.border={top:{style:'thin'},bottom:{style:'thin'},left:{style:'thin'},right:{style:'thin'}}));
    sheet.views=[{state:'frozen',ySplit:3}];sheet.autoFilter={from:'A3',to:'F3'};
    sheet.pageSetup={orientation:'landscape',paperSize:9,fitToPage:true,fitToWidth:1,fitToHeight:0,printTitlesRow:'1:3'};
    const info=book.addWorksheet('Datos OC');info.columns=[{width:35},{width:100}];
    info.addRows([['Orden de compra',oc.order.series+'-'+oc.order.number],['Proveedor',oc.order.supplier],['Estado',oc.order.status],['Moneda',oc.currency],['Condición de pago',oc.conditions],['Grupo',oc.manufacturingGroup],['Cantidad comprada registrada',oc.lines.reduce((sum,line)=>sum+line.quantity,0)],['Observaciones',oc.observations],['Nota del desglose','Detalle obtenido de UP_SEL_ORDCOMPITEM, la misma fuente que utiliza OrdCompDetalle.xlt de Gestión. Se conserva una fila por registro devuelto y sus cantidades compradas, sin repartirlas entre requerimientos.']]);
    info.eachRow(row=>{row.getCell(1).font={bold:true};row.getCell(2).alignment={wrapText:true,vertical:'top'};});info.getRow(9).height=60;
    await this.download(book,`OC-${oc.order.series}-${oc.order.number}.xlsx`,false);
  }
  private async download(workbook: import('exceljs').Workbook, name: string, style = true) {
    for (const sheet of style ? workbook.worksheets : []) {
      sheet.getRow(1).font = { bold: true, color: { argb: 'FFFFFFFF' } };
      sheet.getRow(1).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF173E41' } };
      sheet.views = [{ state: 'frozen', ySplit: 1 }];
      sheet.columns.forEach(c => c.width = 21);
      sheet.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: sheet.columnCount } };
    }
    const data = await workbook.xlsx.writeBuffer();
    const url = URL.createObjectURL(new Blob([data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }));
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = name; anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}

