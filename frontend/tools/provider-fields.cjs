function quantityInput(page,id){
 if(typeof id!=='string'||!id||/["\\\r\n]/.test(id)||/^oid_MSRP_IN_/i.test(id))throw Error('El campo seleccionado no es Quantity To Order. MSRP no se modifica.');
 return page.locator('input._oiOrderedQty[id="'+id+'"]:not([id^="oid_MSRP_IN_"])');
}
module.exports={quantityInput};
