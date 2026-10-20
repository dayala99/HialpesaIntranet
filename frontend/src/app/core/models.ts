export interface LookupOption { code: string; description: string; }
export interface RegistrationCatalogs { conditions: LookupOption[]; discounts: LookupOption[]; currencies: LookupOption[]; deliveryLocations: LookupOption[]; costCenters: LookupOption[]; }
export interface OrderReference { series: string; number: string; }
export interface CreateOrderRequest { supplierCode: string; conditionCode: string; discountCode: string; currencyCode: string; deliveryLocationCode: string; costCenterCode: string; manufacturingGroup: string; taxPercent: number; deliveryStart: string; deliveryEnd: string; observations: string; }
export interface PendingRequirement { key: string; factory: string; productionOrder: string; presentationCode: number; presentation: string; component: string; itemCode: string; description: string; combination: string; color: string; size: string; destination: string; style: string; providerReference: string; required: number; purchase: number; unitPrice: number; }
export interface DetailsSaved { series: string; number: string; requirementsAdded: number; itemsAdded: number; requiredTotal: number; purchaseTotal: number; }
export interface PrintLine { sequence: string; code: string; description: string; size: string; quantity: number; unitPrice: number; amount: number; }
export interface PrintOrder { order: Order; items: OrderItem[]; currency: string; conditions: string; deliveryLocation: string; manufacturingGroup: string; observations: string; taxPercent: number; deliveryStart: string | null; deliveryEnd: string | null; lines: PrintLine[]; reportRows: GestionPrintLine[]; breakdown: {sequence:string;item:string;combination:string;measure:string;customerStyle:string;productionOrder:string;required:number;purchased:number|null;calculated:boolean}[]; }
export interface Session { displayName: string; gestionUser: string; demo: boolean; canWriteDatabase: boolean; }
export interface Order { series: string; number: string; supplier: string; status: string; class: string; client: string; createdAt: string | null; }
export interface OrderItem { sequence: string; itemCode: string; description: string; size: string; style: string; required: number; purchased: number; received: number; }
export interface OrderDetail { order: Order; items: OrderItem[]; }
export interface Requirement { factory: string; productionOrder: string; presentationCode: string; presentation: string; component: string; itemCode: string; style: string; color: string; size: string; destination: string; quantity: number; }
export interface MatrixRow { factory: string; productionOrder: string; style: string; presentationCode: string; color: string; size: string; destination: string; quantity: number; }
export interface ArticleRule { itemCode: string; description: string; multiple: number; }
export interface CalculatedRow { source: MatrixRow; itemCode: string; article: string; multiple: number; required: number; purchase: number; extra: number; }
export interface Preview { rows: CalculatedRow[]; baseUnits: number; purchaseUnits: number; extraUnits: number; }

export interface GestionPrintLine {productionOrders:string;item:string;combination:string;color:string;measure:string;destination:string;customerStyle:string;unit:string;unitPrice:number;required:number;purchased:number;received:number;conversion:number;supplierItem:string;providerReference:string;}
