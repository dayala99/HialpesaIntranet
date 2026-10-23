-- Crear manualmente en HIALPESA. No modifica los SP de Gestión ni datos de negocio.
-- Lectura con bloqueo dentro de la transacción del backend; solo para Tela Por Acabar.
CREATE PROCEDURE dbo.PA_TI_ORDTRA_TINTORERIA_ITEMS_CC_POR_ACABAR_S0001
 @MODO CHAR(1)='L', @COD_ORDTRA CHAR(5)='', @NUM_SECUENCIA INT=0, @COD_EVENTO_CC CHAR(3)=''
AS
BEGIN
 SET NOCOUNT ON;
 IF @MODO<>'L'
 BEGIN
  RAISERROR('Modo no válido.',16,1);
  RETURN;
 END;
 SELECT * FROM dbo.TI_ORDTRA_TINTORERIA_ITEMS_CC_POR_ACABAR WITH(UPDLOCK,HOLDLOCK)
 WHERE Cod_OrdTra=@COD_ORDTRA AND Num_Secuencia=@NUM_SECUENCIA AND Cod_Evento_CC=@COD_EVENTO_CC;
END;
GO
