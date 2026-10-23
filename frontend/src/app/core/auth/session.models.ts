export interface Session {
    isGlobalUser?: boolean;
    allowedOptions?: string[];
    profiles?: {code:string;name:string}[];
    displayName: string;
    gestionUser: string;
    demo: boolean;
    canWriteDatabase: boolean;
}
