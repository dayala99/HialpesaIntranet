import { Injectable } from '@angular/core';
import type { jsPDF } from 'jspdf';
import type { UserOptions } from 'jspdf-autotable';
import { environment } from '../../../../../environments/environment';
import { AnalysisRow, PartialSelection, Requotation, SectorAnalysis } from './analysis.models';
import { calculateQuotation } from './quotation-calculations';
export interface AnalysisPdfData {source:AnalysisRow;usedTime:number|null;servicePrice:number|null;otherExpenses:number|null;calculated:ReturnType<typeof calculateQuotation>;analysisDate:string;partialSelection?:PartialSelection|null;requotation?:Requotation|null;historyId?:number|null;sectorAnalysis?:SectorAnalysis|null;}
@Injectable({providedIn:'root'})
export class AnalysisPdfService {
 private async image(data:AnalysisPdfData):Promise<{url:string;width:number;height:number}|null>{
  const url=environment.apiBaseUrl+'/logistica/analisis-rb/imagen'+(data.historyId?'/historial':'')+'?np='+encodeURIComponent(data.source.np)+(data.historyId?'&id='+data.historyId:'');
  return this.loadImage(url);
 }
 private async loadImage(url:string):Promise<{url:string;width:number;height:number}|null>{
  let objectUrl='';try{const response=await fetch(url,{credentials:'include'});if(!response.ok)return null;objectUrl=URL.createObjectURL(await response.blob());const img=new Image();img.src=objectUrl;await img.decode();const canvas=document.createElement('canvas');canvas.width=img.naturalWidth;canvas.height=img.naturalHeight;canvas.getContext('2d')!.drawImage(img,0,0);return {url:canvas.toDataURL('image/png'),width:canvas.width,height:canvas.height};}catch{return null;}finally{if(objectUrl)URL.revokeObjectURL(objectUrl);}
 }
 async download(data:AnalysisPdfData){
  const [{jsPDF},{autoTable},image,logo]=await Promise.all([import('jspdf'),import('jspdf-autotable'),this.image(data),this.loadImage(new URL('assets/images/logos/logo-hialpesa.png',document.baseURI).href)]);
  const doc=new jsPDF({orientation:'portrait',unit:'mm',format:'a4',compress:true});
  doc.setProperties({title:'Analíticas de Costura - '+data.source.np,subject:'Análisis de costura',creator:'Hialpesa Intranet'});
  const margin=12,width=doc.internal.pageSize.getWidth(),height=doc.internal.pageSize.getHeight(),bodyWidth=width-2*margin;
  if(logo){const scale=Math.min(34/logo.width,17/logo.height);doc.addImage(logo.url,'PNG',margin,2,logo.width*scale,logo.height*scale);}
  const source=data.source,q=data.calculated;let y=24;
  const number=(value:number|null|undefined,decimals=2)=>value==null||!Number.isFinite(value)?'Sin dato':new Intl.NumberFormat('en-US',{minimumFractionDigits:decimals,maximumFractionDigits:decimals}).format(value);
  const percent=(value:number|null)=>value==null?'Sin dato':number(value*100,2)+'%';
  const date=(value?:string|null)=>value?value.slice(0,10).split('-').reverse().join('/'):'Sin dato';
  const text=(value?:string|null)=>value?.trim()||'Sin dato';
  const blue:[number,number,number]=[187,206,231],green:[number,number,number]=[204,229,200],yellow:[number,number,number]=[255,243,174];
  const table=(options:UserOptions)=>{autoTable(doc,{startY:y,margin:{left:margin,right:margin,top:15,bottom:16},theme:'grid',styles:{font:'helvetica',fontSize:7,cellPadding:1.5,textColor:[27,49,53],lineColor:[168,182,190],lineWidth:.15,overflow:'linebreak'},headStyles:{fillColor:blue,textColor:[27,49,53],fontStyle:'bold',halign:'center'},bodyStyles:{halign:'center'},rowPageBreak:'avoid',...options});y=(doc as jsPDF&{lastAutoTable:{finalY:number}}).lastAutoTable.finalY+3;};
  const room=(needed:number)=>{if(y+needed>height-16){doc.addPage();y=15;}};
  doc.setFont('helvetica','bold');doc.setFontSize(18);doc.setTextColor(27,49,53);doc.text('Analíticas de Costura',width/2,15,{align:'center'});
  if(data.historyId){doc.setFontSize(8);doc.setFont('helvetica','normal');doc.text('Registro histórico '+data.historyId,width/2,20,{align:'center'});y=27;}
  const metaWidth=bodyWidth;
  if(image)y=Math.max(y,38);
  const label=(content:string)=>({content,styles:{fontStyle:'bold' as const}});
  table({tableWidth:metaWidth,theme:'plain',styles:{font:'helvetica',fontSize:9,cellPadding:{top:2,right:2,bottom:2,left:0},textColor:[27,49,53]},bodyStyles:{halign:'left'},columnStyles:{0:{cellWidth:34},1:{cellWidth:(metaWidth-68)/2},2:{cellWidth:34},3:{cellWidth:(metaWidth-68)/2}},body:[
   [label('NP:'),source.np,label('Cliente:'),text(source.clientName)],
   [label('Estilo cliente:'),text(source.clientStyle),label('División:'),text(source.divisionName)],
   [label('Cantidad requerida:'),number(source.requiredQuantity,0),label('Cantidad programada:'),number(source.programmedQuantity,0)],
   [label('Costura:'),(data.sectorAnalysis?.isComplete??!data.partialSelection)?'Completa':'Parcial',label('Tipo:'),text(source.garmentType)],
   [label('Nombre Línea / Servicio:'),{content:text(source.lineName),colSpan:3}],
   [label('Tipo de Tela:'),{content:text(source.fabricDescription),colSpan:3}],
   [label('Color:'),{content:text(source.colorName),colSpan:3}],
   [label('Fecha Inicio Costura:'),date(source.sewingStartDate),label('Fecha Fin Costura:'),date(source.sewingEndDate)],
   [label('Fecha Despacho:'),date(source.dispatchDate),label('Fecha Actual:'),data.analysisDate]
  ]});
  if(image){const scale=Math.min(32/image.width,30/image.height),iw=image.width*scale,ih=image.height*scale;doc.addImage(image.url,'PNG',width-margin-32+(32-iw)/2,3,iw,ih);}
  table({head:[['PRECIO COMERCIAL $','TIEMPO TOTAL','V.M.','TIEMPO OP. RESTADAS']],body:[[number(source.commercialPrice,4),number(data.sectorAnalysis?.totalTime??source.sewingTime),number(q.baseVm),number(q.removedTime)]]});
  if(data.requotation?.enabled){room(29);doc.setFillColor(241,248,238);doc.roundedRect(margin,y,bodyWidth,26,2,2,'F');doc.setDrawColor(34,89,78);doc.setLineWidth(.4);doc.rect(margin+4,y+3,3,3);doc.line(margin+4.5,y+4.5,margin+5.2,y+5.4);doc.line(margin+5.2,y+5.4,margin+6.6,y+3.5);doc.setFont('helvetica','bold');doc.setFontSize(9);doc.text('Es Prenda Nueva / Recotización',margin+10,y+5.8);doc.setFont('helvetica','normal');doc.setFontSize(8);doc.text('Precio Servicio',margin+4,y+11);doc.text('Porcentaje',margin+74,y+11);doc.setDrawColor(169,199,160);doc.setFillColor(255,255,255);doc.rect(margin+4,y+13,60,8,'FD');doc.rect(margin+74,y+13,60,8,'FD');doc.text(number(data.requotation.baseServicePrice),margin+7,y+18.5);doc.text(number(data.requotation.percentage),margin+77,y+18.5);y+=31;}
  table({head:[['PRECIO COMERCIAL TOTAL $',q.timeLabel,'PRECIO POR SERVICIO $','V.M.','DIF+$','RENTABILIDAD %','OBSERVACIONES']],body:[[number(q.commercialTotal,3),number(data.usedTime,3),number(data.servicePrice),number(q.serviceVm,3),number(q.difference,3),percent(q.profitability),q.observation]],didParseCell:cell=>{if(cell.section==='head'&&(cell.column.index===2||cell.column.index===3))cell.cell.styles.fillColor=green;}});
  table({head:[['PRECIO COMERCIAL PARCIAL','TIEMPO PARCIAL','PRECIO POR SERVICIO $','V.M.','DIF+$','DIF %','OBSERVACIÓN']],body:[
   [number(q.commercialTotal,3),number(data.usedTime,3),number(data.servicePrice),number(q.serviceVm,3),number(q.difference,3),percent(q.profitability),q.observation],
   ['', '',number(data.otherExpenses),'','','','OTROS GASTOS'],
   [number(q.commercialTotal,3),number(data.usedTime),number(q.serviceWithExpenses),number(q.serviceVm,3),number(q.difference,3),percent(q.profitability),'']
  ],didParseCell:cell=>{if(cell.section==='head'&&(cell.column.index===2||cell.column.index===3))cell.cell.styles.fillColor=green;if(cell.section==='body'&&cell.row.index===2){cell.cell.styles.fillColor=yellow;cell.cell.styles.fontStyle='bold';}}});
  if(data.partialSelection?.operations.length){const selection=data.partialSelection;const sectionGap=height*.01;room(sectionGap+22);y+=sectionGap;doc.setFont('helvetica','bold');doc.setFontSize(12);doc.text('Procesos seleccionados',margin,y);y+=6;doc.setFontSize(8);doc.setFont('helvetica','normal');doc.text(selection.sectorName+' - Tarifado '+selection.tariff+' - Variante '+selection.variant,margin,y);y+=4;table({head:[['Bloque','Secuencia','Operación','Variante','Descripción','Tiempo (min)']],body:selection.operations.map(operation=>[operation.blockCode+' - '+operation.blockName,operation.sequence,operation.code,operation.variant,operation.description,number(operation.standardTime,3)]),columnStyles:{0:{cellWidth:27},1:{cellWidth:14},2:{cellWidth:18},3:{cellWidth:14},4:{cellWidth:bodyWidth-98,halign:'left'},5:{cellWidth:25,halign:'right'}},foot:[[{content:'Tiempo de los procesos seleccionados',colSpan:5},number(selection.operations.reduce((sum,row)=>sum+row.standardTime,0),3)]],footStyles:{fillColor:[237,245,241],textColor:[27,49,53],fontStyle:'bold'},showFoot:'lastPage'});}
  const pages=doc.getNumberOfPages();for(let page=1;page<=pages;page++){doc.setPage(page);doc.setFont('helvetica','normal');doc.setFontSize(7);doc.setTextColor(100);doc.text('Hialpesa - NP '+source.np,margin,height-7);doc.text('Página '+page+' de '+pages,width-margin,height-7,{align:'right'});}
  doc.save('Analiticas_Costura_'+source.np+(data.historyId?'_Historial_'+data.historyId:'')+'.pdf');
 }
}




