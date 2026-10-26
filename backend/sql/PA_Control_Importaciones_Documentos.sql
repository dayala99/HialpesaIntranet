-- Procedimientos complementarios. SQL Server 2008 R2. No modifica procedures de Gestión.
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0007 @Number varchar(10)=''
AS
BEGIN
 SET NOCOUNT ON;
 SELECT CASE WHEN d.Num_Corre=i.Num_Corre_Proforma THEN 'PR' ELSE 'CP' END Kind,RTRIM(d.Num_Corre) Correlative,RTRIM(d.Cod_TipDoc) Type,RTRIM(d.Ser_Docum) Series,RTRIM(d.Num_Docum) Number,
 RTRIM(d.Cod_TipAnex) AnnexType,RTRIM(d.Cod_Anxo) Annex,RTRIM(ISNULL(a.Des_Anexo,'')) AnnexName,RTRIM(ISNULL(a.Num_Ruc,'')) Ruc,RTRIM(ISNULL(t.Doc_Sunat,'')) Sunat,
 d.Fec_EmiDoc IssuedAt,d.Fec_RegDoc RegisteredAt,d.Fec_VenDoc DueDate,RTRIM(d.Cod_TipIte) ItemType,RTRIM(d.Cod_Moneda) Currency,
 ISNULL(d.Tipo_Cambio,0) ExchangeRate,ISNULL(d.FacEquMon,0) Equivalence,RTRIM(ISNULL(d.Glosa,'')) Description,
 ISNULL(d.Imp_OtrosSol,0) Soles,ISNULL(d.Imp_OtrosDol,0) Dollars,ISNULL(d.Imp_OtrosOtros,0) Other,
 ISNULL(d.Sol_ImpTot,0) TotalSoles,ISNULL(d.Dol_ImpTot,0) TotalDollars,ISNULL(d.Otr_ImpTot,0) TotalOther
 FROM LG_IMPORTACIONES i INNER JOIN CN_DOCUM_AUXILIAR d ON d.Num_Corre=i.Num_Corre_Proforma OR d.Num_Corre=i.Num_Corre_ConfirmacionPedido
 LEFT JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=d.Cod_TipAnex AND a.Cod_Anxo=d.Cod_Anxo
 LEFT JOIN CN_TIPOSDOCUM t ON t.Cod_TipDoc=d.Cod_TipDoc
 WHERE @Number<>'' AND i.Num_Importacion=@Number
 UNION ALL SELECT 'FI' Kind,RTRIM(d.Num_Corre) Correlative,RTRIM(d.Cod_TipDoc) Type,RTRIM(d.Ser_Docum) Series,RTRIM(d.Num_Docum) Number,
 RTRIM(d.Cod_TipAnex) AnnexType,RTRIM(d.Cod_Anxo) Annex,RTRIM(ISNULL(a.Des_Anexo,'')) AnnexName,RTRIM(ISNULL(a.Num_Ruc,'')) Ruc,RTRIM(ISNULL(t.Doc_Sunat,'')) Sunat,
 d.Fec_EmiDoc IssuedAt,d.Fec_RegDoc RegisteredAt,d.Fec_VenDoc DueDate,RTRIM(d.Cod_TipIte) ItemType,RTRIM(d.Cod_Moneda) Currency,
 ISNULL(d.Tipo_Cambio,0) ExchangeRate,ISNULL(d.FacEquMon,0) Equivalence,RTRIM(ISNULL(d.Glosa,'')) Description,
 ISNULL(d.Imp_OtrosSol,0) Soles,ISNULL(d.Imp_OtrosDol,0) Dollars,ISNULL(d.Imp_OtrosOtros,0) Other,
 ISNULL(d.Sol_ImpTot,0) TotalSoles,ISNULL(d.Dol_ImpTot,0) TotalDollars,ISNULL(d.Otr_ImpTot,0) TotalOther
 FROM LG_IMPORTACIONES i INNER JOIN CN_DOCUM d ON d.Num_Corre=i.Num_Corre_FacturaComercial
 LEFT JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=d.Cod_TipAnex AND a.Cod_Anxo=d.Cod_Anxo
 LEFT JOIN CN_TIPOSDOCUM t ON t.Cod_TipDoc=d.Cod_TipDoc
 WHERE @Number<>'' AND i.Num_Importacion=@Number;
 SELECT RTRIM(Cod_TipoAlmacen) Warehouse,RTRIM(Ser_OrdComp) Series,RTRIM(Cod_OrdComp) Number FROM LG_IMPORTACIONES WHERE @Number<>'' AND Num_Importacion=@Number AND ISNULL(Cod_OrdComp,'')<>'';
END
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0008 @Kind varchar(12),@AnnexType char(1)='',@Annex char(4)='',@Search varchar(70)=''
AS
BEGIN
 SET NOCOUNT ON;
 IF @Kind='catalogs'
 BEGIN
 SELECT 'documentTypes' Kind,RTRIM(Cod_TipDoc) Code,RTRIM(Des_TipDoc) Description,RTRIM(Doc_Sunat) Extra FROM CN_TIPOSDOCUM WHERE Cod_TipDoc IN ('PR','CP','FI')
 UNION ALL SELECT 'documentItems',RTRIM(Cod_TipIte),RTRIM(Des_TipIte),'' FROM CN_TIPOITEM
 UNION ALL SELECT 'warehouses',RTRIM(Cod_TipoAlmacen),RTRIM(Des_TipoAlmacen),'' FROM TG_TIPOALMACEN
 UNION ALL SELECT 'currencyTypes',RTRIM(Cod_Moneda),RTRIM(Nom_Moneda),RTRIM(Flg_TipMonDoc) FROM TG_MONEDA;
 END
 IF @Kind='orders'
 SELECT TOP 100 'C' Warehouse,RTRIM(o.Ser_OrdComp) Series,RTRIM(o.Cod_OrdComp) Number,RTRIM(p.Des_Proveedor) Description
 FROM LG_ORDCOMP o INNER JOIN LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor
 INNER JOIN CN_ANEXOSCONTABLES a ON a.Num_Ruc=p.Num_Ruc AND a.Cod_TipAnex=@AnnexType AND a.Cod_Anxo=@Annex
 WHERE o.Cod_OrdComp LIKE '%'+@Search+'%' ORDER BY o.Ser_OrdComp,o.Cod_OrdComp DESC;
 IF @Kind='annex'
 SELECT RTRIM(Num_Ruc) Ruc,RTRIM(Des_Anexo) Description FROM CN_ANEXOSCONTABLES WHERE Cod_TipAnex=@AnnexType AND Cod_Anxo=@Annex;
 IF @Kind='policyRead'
 SELECT RTRIM(Cod_TipAnex) AnnexType,RTRIM(Cod_Anxo) Annex,RTRIM(Cod_TipDoc) Type,RTRIM(Ser_Docum) Series,RTRIM(Num_Docum) Number FROM CN_DOCUM WHERE Num_Corre=@Search AND Cod_TipDoc<>'99' AND ISNULL(Flg_Doc,'')<>'A';
 IF @Kind='policies'
 SELECT TOP 100 RTRIM(d.Num_Corre) Correlative,RTRIM(d.Cod_TipDoc) Type,RTRIM(d.Ser_Docum) Series,RTRIM(d.Num_Docum) Number,RTRIM(a.Des_Anexo) Description,d.Fec_EmiDoc IssuedAt,d.Sol_ImpTot Soles,d.Dol_ImpTot Dollars
 FROM CN_DOCUM d INNER JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=d.Cod_TipAnex AND a.Cod_Anxo=d.Cod_Anxo
 WHERE d.Cod_TipDoc<>'99' AND d.Cod_TipAnex=@AnnexType AND d.Cod_Anxo=@Annex AND d.Num_Docum LIKE '%'+@Search+'%' AND ISNULL(d.Flg_Doc,'')<>'A'
 ORDER BY d.Fec_EmiDoc DESC,d.Num_Corre DESC;
END
GO
