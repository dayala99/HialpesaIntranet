const fs=require('node:fs'),assert=require('node:assert/strict');
const {validateJob,readProtocolJob}=require('./agent.cjs');
(async()=>{
 const base='http://localhost:5080/api/v1';const password=process.env.INTRANET_TEST_PASSWORD;if(!password)throw Error('Falta contraseña de prueba');
 const anonymous=await fetch(base+'/proveedor/preparar-llenado',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'});assert.equal(anonymous.status,401);
 const login=await fetch(base+'/sesion/login',{method:'POST',headers:{'Content-Type':'application/json','X-Intranet-Session':'1'},body:JSON.stringify({username:'programador04',password})});assert.equal(login.status,200);const cookie=login.headers.get('set-cookie').split(';')[0];
 try{
  const headers={'Cookie':cookie,'Content-Type':'application/json','X-Intranet-Provider':'1'};
  const posResponse=await fetch(base+'/proveedor/ordenes/002/350000/pos',{headers});assert.equal(posResponse.status,200);const pos=await posResponse.json();assert(pos.length);
  const missingHeader=await fetch(base+'/proveedor/preparar-llenado',{method:'POST',headers:{Cookie:cookie,'Content-Type':'application/json'},body:JSON.stringify({series:'002',number:'350000',po:pos[0]})});assert.equal(missingHeader.status,403);
  const wrongPo=await fetch(base+'/proveedor/preparar-llenado',{method:'POST',headers,body:JSON.stringify({series:'002',number:'350000',po:'PO-99999999999999999999'})});assert.equal(wrongPo.status,400);
  const response=await fetch(base+'/proveedor/preparar-llenado',{method:'POST',headers,body:JSON.stringify({series:'002',number:'350000',po:pos[0]})});assert.equal(response.status,200);assert.equal(response.headers.get('cache-control'),'no-store');
  const job=validateJob(await response.json(),fs.readFileSync(__dirname+'/public.pem'));assert.equal(job.po,pos[0]);assert(job.rows.length);
  const launch=await fetch(base+'/proveedor/preparar-apertura',{method:'POST',headers,body:JSON.stringify({series:'002',number:'350000',po:pos[0]})});assert.equal(launch.status,200);const ticket=await launch.json();
  const link='hialpesa-vince://run?server='+encodeURIComponent(base)+'&token='+ticket.token;
  const recovered=validateJob(await readProtocolJob(link,[base]),fs.readFileSync(__dirname+'/public.pem'));assert.equal(recovered.po,pos[0]);
  const consumed=await fetch(base+'/proveedor/recuperar-llenado',{method:'POST',headers:{'Content-Type':'application/json','X-Intranet-Agent':'1'},body:JSON.stringify({token:ticket.token})});assert.equal(consumed.status,410);
  console.log('Protocolo/API real: operación preparada, recuperada sin cookie y firma verificada; segunda recuperación rechazada.');
  console.log('API: acceso anónimo y cabecera faltante bloqueados; PO ajeno rechazado; archivo real firmado y validado. Sin acceso al portal del proveedor.');
 }finally{await fetch(base+'/sesion/logout',{method:'POST',headers:{Cookie:cookie,'X-Intranet-Session':'1'}})}
})().catch(e=>{console.error(e);process.exit(1)});

