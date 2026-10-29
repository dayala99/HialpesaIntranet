import { calculateQuotation } from './quotation-calculations';
export interface AnalysisRow { garmentType?:string|null; clientStyle?:string|null;divisionName?:string|null;requiredQuantity?:number|null; np: string; programmedQuantity: number; commercialPrice: number | null; sewingTime: number | null; clientName?:string|null;lineName?:string|null;dispatchDate?:string|null;productCategory?:string|null;fabricDescription?:string|null;imagePath?:string|null;colorName?:string|null;sewingStartDate?:string|null;sewingEndDate?:string|null; }
export interface SavedSnapshot { source:AnalysisRow;usedTime:number;servicePrice:number;otherExpenses:number;calculated:ReturnType<typeof calculateQuotation>;analysisDate:string;ruleVersion:string;partialSelection?:PartialSelection|null;requotation?:Requotation|null;sectorAnalysis?:SectorAnalysis|null; }
export interface HistoryRow {id:number;revision:number;action:string;np:string;programmedQuantity:number;commercialPrice:number;sewingTime:number;usedTime:number;servicePrice:number;otherExpenses:number;gestionUser:string;savedAtUtc:string;snapshotJson:string|null;imagePath?:string|null;}
export interface HistoryPage {rows:HistoryRow[];totalRows:number;page:number;totalPages:number;}

export interface SelectedOperation {blockCode:string;blockName:string;sequence:string;code:string;variant:string;description:string;standardTime:number;}
export interface PartialSelection {tariff:string;variant:string;sector:string;sectorName:string;sectorTotalTime:number;operations:SelectedOperation[];}

export interface Requotation {enabled:boolean;baseServicePrice:number|null;percentage:number|null;}

export interface SectorAnalysis {tariff:string;variant:string;sector:string;sectorName:string;totalTime:number;totalOperations:number;selectedOperations:number;isComplete:boolean;}
