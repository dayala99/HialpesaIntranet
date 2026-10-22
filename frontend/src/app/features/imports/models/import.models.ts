export interface ImportSource {
    supplierCode: string;
    supplier: string;
    annexType: string;
    annex: string;
    currency: string;
    existingImport: string;
    supplierOrigin: string;
}
export interface ImportCatalogOption {
    Kind: string;
    Code: string;
    Name: string;
}
export interface ImportRecord {
    number: string;
    registeredAt: string;
    status: string;
    supplier: string;
    currency: string;
    origin: string;
    series: string;
    orderNumber: string;
}
export interface ImportRequest {
    itemType: string;
    origin: string;
    shipment: string;
    country: string;
    area: string;
    registeredAt: string;
    guideDate: string | null;
    paymentDate: string | null;
    dueDate: string | null;
    guide: string;
    observations: string;
}
export interface ImportSourceResponse {
    source: ImportSource;
    automaticNumbering: boolean;
    validAnnex: boolean;
}
export interface ImportListResponse {
    rows: ImportRecord[];
    page: number;
    hasMore: boolean;
    totalPages: number;
    totalRows: number;
}
export interface ImportCreatedResponse {
    number: string;
    series: string;
    orderNumber: string;
}
