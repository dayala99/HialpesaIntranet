export interface QuotationInputs { commercialPrice:number|null;totalTime:number|null;usedTime:number|null;servicePrice:number|null;otherExpenses:number|null;forcePartial?:boolean;isComplete?:boolean;sectorName?:string|null;sectorCode?:string|null;partialSectorName?:string|null; }
export function calculateQuotation(input:QuotationInputs){
 const finite=(n:number|null):n is number=>n!==null&&Number.isFinite(n);
 const divide=(a:number|null,b:number|null)=>finite(a)&&finite(b)&&b!==0?a/b:null;
 const {commercialPrice,totalTime,usedTime,servicePrice,otherExpenses}=input;
 const baseVm=divide(commercialPrice,totalTime);
 const commercialTotal=finite(usedTime)&&finite(commercialPrice)&&finite(totalTime)&&totalTime!==0?usedTime*commercialPrice/totalTime:null;
 const serviceVm=divide(servicePrice,usedTime);
 const difference=finite(commercialTotal)&&finite(servicePrice)?commercialTotal-servicePrice:null;
 const profitability=divide(difference,commercialTotal);
 const removedTime=finite(totalTime)&&finite(usedTime)?totalTime-usedTime:null;
 const complete=finite(totalTime)&&finite(usedTime)?(input.isComplete??(!input.forcePartial&&totalTime===usedTime)):null;
 const timeLabel=complete===null?'SIN DATO':complete?'TIEMPO COMPLETO':'TIEMPO PARCIAL';
 const observation=complete===null?'SIN DATO':complete?(input.sectorName?input.sectorName.toUpperCase()+(input.sectorCode==='CS'?' COMPLETA':' COMPLETO'):'COSTURA COMPLETA'):(input.sectorName?.toUpperCase()??input.partialSectorName?.toUpperCase()??'COSTURA')+' PARCIAL';
 const serviceWithExpenses=finite(servicePrice)&&finite(otherExpenses)?servicePrice+otherExpenses:null;
 return {baseVm,commercialTotal,serviceVm,difference,profitability,removedTime,timeLabel,observation,serviceWithExpenses};
}
