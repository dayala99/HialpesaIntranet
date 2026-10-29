import { Injectable } from '@angular/core';
import { AnalysisRow, PartialSelection, Requotation, SectorAnalysis } from './analysis.models';
export interface OtherSearchState {opened?:boolean;price:number|null;time:number|null;type:string;rows:AnalysisRow[];searched:boolean;hasMore:boolean;nextCursor:string|null;cursors:(string|null)[];page:number;applied:{price:number|null;time:number|null;type:string};}
export interface AnalysisDraft {searchTab?:'np'|'others';analysisTab?:'np'|'others';otherSearch?:OtherSearchState; np:string;rows:AnalysisRow[];usedTime:number|null;servicePrice:number|null;otherExpenses:number|null;saveSignature:string;requestId:string;lastSavedSignature:string;saveMessage:string;partialSelection?:PartialSelection|null;requotation?:Requotation|null;sectorAnalysis?:SectorAnalysis|null;analysisReady?:boolean; }
@Injectable({providedIn:'root'})
export class AnalysisDraftService { draft:AnalysisDraft|null=null;pendingAnalysis:{np:string;selection:PartialSelection|null;sectorAnalysis:SectorAnalysis}|null=null;operationsLocation:{np:string;variant:string;sector:string}|null=null; }
