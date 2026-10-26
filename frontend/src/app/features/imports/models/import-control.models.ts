export interface ControlOption {kind:string;code:string;description:string;}
export interface ControlCatalogs {options:ControlOption[];automaticNumbering:boolean;}
export interface ControlFilter {mode:string;number:string;annexType:string;annex:string;status:string;guide:string;origin:string;item:string;country:string;associated:string;policy:string;year:number;month:number;documentType:string;documentSeries:string;documentNumber:string;itemType:string;factory:string;productionOrder:string;from:string;to:string;page:number;}
export interface ControlRow {number:string;registeredat:string;annexname:string;annextype:string;annex:string;status:string;statusname:string;currency:string;itemtype:string;itemtypename:string;guide:string;guidedate:string|null;originname:string;country:string;countryname:string;document:string;principal:string;partial:string;orders:string;policy:string;fob:number;}
export interface ControlPage {rows:ControlRow[];totalRows:number;page:number;totalPages:number;hasMore:boolean;}
export interface ControlValues {number:string;annexType:string;annex:string;registeredAt:string;itemType:string;currency:string;origin:string;shipment:string;country:string;area:string;guide:string;guideDate:string|null;paymentDate:string|null;dueDate:string|null;observations:string;partial:boolean;principal:string;}
export interface ControlDocuments {proforma:string;confirmation:string;orders:string;invoice:string;policy:string;}
export interface ControlConcept {code:string;description:string;dollars:number;soles:number;exchangeRate:number;other:number;documentNumber:string;}
export interface ControlCondition {sequence:string;description:string;}
export interface ControlDetail {number:string;status:string;annexName:string;values:ControlValues;version:string;canEdit:boolean;canDelete:boolean;documents?:ControlDocuments;concepts?:ControlConcept[];conditions?:ControlCondition[];documentEntries?:ControlDocument[];order?:ControlOrder;}
export function localDate(){const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;}
export function newControlValues():ControlValues{const today=localDate();return {number:'',annexType:'P',annex:'',registeredAt:today,itemType:'',currency:'',origin:'',shipment:'',country:'',area:'05',guide:'',guideDate:today,paymentDate:today,dueDate:today,observations:'PROV. IMPORT. PROF/. ',partial:false,principal:''};}
export function newControlFilter():ControlFilter{return {mode:'1',number:'',annexType:'P',annex:'',status:'',guide:'',origin:'',item:'',country:'',associated:'',policy:'',year:new Date().getFullYear(),month:new Date().getMonth()+1,documentType:'FI',documentSeries:'',documentNumber:'',itemType:'',factory:'',productionOrder:'',from:'',to:'',page:1};}

export interface ControlDocument {kind:'PR'|'CP'|'FI';correlative:string;type:string;series:string;number:string;annexType:string;annex:string;annexName:string;ruc:string;sunat:string;issuedAt:string;registeredAt:string;dueDate:string|null;itemType:string;currency:string;exchangeRate:number;equivalence:number;description:string;soles:number;dollars:number;other:number;totalSoles:number;totalDollars:number;totalOther:number;}
export interface ControlOrder {warehouse:string;series:string;number:string;description?:string;}
export interface ControlPolicy {correlative:string;type:string;series:string;number:string;description:string;}
export interface ControlShipment {code:string;description:string;active:string;version:string;}
