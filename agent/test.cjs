const assert=require('node:assert/strict'),crypto=require('node:crypto');
const {validateJob}=require('../agent/agent.cjs');
const {privateKey,publicKey}=crypto.generateKeyPairSync('rsa',{modulusLength:2048});
const now=Date.now();
const job={schema:'hialpesa-vince/1',issuedAt:new Date(now).toISOString(),expiresAt:new Date(now+1800000).toISOString(),series:'002',number:'350000',po:'PO-00054154',rows:[{item:'PT1-VINCEH501RFID',combination:'PO-00054154',purchased:135}]};
const sign=value=>{const data=Buffer.from(JSON.stringify(value));return{payload:data.toString('base64'),signature:crypto.sign('RSA-SHA256',data,privateKey).toString('base64')}};
assert.deepEqual(validateJob(sign(job),publicKey,now),job);
const tampered=sign(job);tampered.payload=Buffer.from(JSON.stringify({...job,po:'PO-OTHER'})).toString('base64');assert.throws(()=>validateJob(tampered,publicKey,now),/firma/);
assert.throws(()=>validateJob(sign(job),crypto.generateKeyPairSync('rsa',{modulusLength:2048}).publicKey,now),/firma/);
assert.throws(()=>validateJob(sign(job),publicKey,now+1800001),/venció/);
assert.throws(()=>validateJob(sign({...job,issuedAt:new Date(now+120000).toISOString()}),publicKey,now),/venció/);
assert.throws(()=>validateJob(sign({...job,rows:[]}),publicKey,now),/cantidades/);
assert.throws(()=>validateJob(sign({...job,rows:[{...job.rows[0],purchased:-1}]}),publicKey,now),/cantidades/);
assert.throws(()=>validateJob(sign({...job,po:'PO-evil'}),publicKey,now),/válida/);
assert.throws(()=>validateJob(sign({...job,schema:'other'}),publicKey,now),/válida/);
assert.throws(()=>validateJob({},publicKey,now),/válido/);
console.log('10 comprobaciones de firma, manipulación, vencimiento y validación correctas.');

const poJob={schema:job.schema,issuedAt:job.issuedAt,expiresAt:job.expiresAt,po:job.po,mode:'preserve-provider',rows:[]};
assert.deepEqual(validateJob(sign(poJob),publicKey,now),poJob);
assert.throws(()=>validateJob(sign({...poJob,mode:'unknown'}),publicKey,now),/Modo/);
console.log('Modo PO sin OC firmado: conserva cantidades; modo desconocido rechazado.');

assert.deepEqual(validateJob(sign({...poJob,mode:'po-required',rows:job.rows}),publicKey,now).rows,job.rows);
assert.throws(()=>validateJob(sign({...poJob,mode:'po-required',rows:[]}),publicKey,now),/cantidades/);
