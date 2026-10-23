export type EventRow=Record<string,any>;
export interface EventOption {kind:string;code:string;description:string;}
export interface EventFilter {mode:number;from:string;to:string;batch:string;eventCode:string;}
export interface EventUpdate {action:string;batch:string;eventCode:string;eventSequence:number;date:string;status:string;auditor:string;observations:string;reason:string;externalResult:string;location:string;productionObservations:string;deviation:string;cause:string;forcedError:boolean;dispatched:boolean;confirmApproved:boolean;items:{sequence:number;kilos:number}[];expectedVersion:string|null;}
