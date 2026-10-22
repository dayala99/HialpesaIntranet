export interface ColorConfirmation {
    style: string;
    colorCode: string;
    providerColor: string;
    gestionColor: string;
}
export interface ProviderResponse {
    status: string;
    message: string;
    confirmations?: ColorConfirmation[];
}
export interface AgentTicket {
    token: string;
    expiresAt: string;
}
