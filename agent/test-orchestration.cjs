// Offline orchestration test: no registry changes, DPAPI access or external browser.
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),os=require('node:os'),crypto=require('node:crypto'),Module=require('node:module'),{EventEmitter}=require('node:events'),{PassThrough}=require('node:stream');
const original=Module._load,realRead=fs.readFileSync;const keys=crypto.generateKeyPairSync('rsa',{modulusLength:2048});
const publicPem=keys.publicKey.export({type:'spki',format:'pem'});let messages=[],writes=[],approve=true,child,notice;
Module._load=function(name,...rest){
 if(name==='node:child_process')return{spawnSync(command,args,options){
  if(command==='reg.exe')throw Error('Unexpected registry operation');
  if(args.join(' ').includes('ProtectedData]::Unprotect'))return{status:0,stdout:JSON.stringify({Username:'test-user',Password:'test-secret',LoginUrl:'https://webservices.averydennison.com/specstar/login.aspx'})};
  messages.push(options.input);return{status:0,stdout:options.input?.startsWith('¿Confirmas')?(approve?'Yes':'No'):'Yes'};
 },spawn(command,args,options){
 if(command==='powershell.exe'){notice=new EventEmitter();notice.stdin=new EventEmitter();notice.stdin.end=message=>messages.push(message);notice.killed=false;notice.kill=()=>{notice.killed=true;};return notice;}
 assert(args[0].endsWith('provider-login.cjs'));assert.equal(options.env.AVERY_PASSWORD,'test-secret');child=new EventEmitter();child.stdout=new PassThrough();child.stdin={destroy(){},write(line){const command=JSON.parse(line);writes.push(command);queueMicrotask(()=>{child.stdout.write(JSON.stringify(writes.length===1?{status:'confirmation-required',confirmations:[{style:'TEST',colorCode:'C',providerColor:'BLUE',gestionColor:'BLUE TEXT'}]}:{status:'po-filled'})+'\n');});}};child.kill=()=>child.emit('exit');return child;}};
 return original.call(this,name,...rest);
};
fs.readFileSync=function(file,...args){if(String(file)===path.join(__dirname,'public.pem'))return publicPem;return realRead.call(this,file,...args)};
const {main}=require('./agent.cjs');
(async()=>{
 const oldLocal=process.env.LOCALAPPDATA,oldArgs=process.argv,oldFetch=global.fetch;
 const temp=fs.mkdtempSync(path.join(os.tmpdir(),'vince-offline-'));process.env.LOCALAPPDATA=temp;
 try{
 for(const mode of ['file','protocol'])for(const choice of [false,true]){
  approve=choice;messages=[];writes=[];notice=null;
  const payload=Buffer.from(JSON.stringify({schema:'hialpesa-vince/1',issuedAt:new Date().toISOString(),expiresAt:new Date(Date.now()+600000).toISOString(),series:'002',number:'350000',po:'PO-123',rows:[{item:'PT1-VINCEH501RFID',combination:'PO-123',purchased:135}]}));
  const file=path.join(temp,'test.hialpesa-vince');fs.writeFileSync(file,JSON.stringify({payload:payload.toString('base64'),signature:crypto.sign('RSA-SHA256',payload,keys.privateKey).toString('base64')}));process.argv=['node','agent',file];
  if(mode==='protocol'){
   const data=fs.readFileSync(file,'utf8');global.fetch=async()=>new Response(data);
   process.argv=['node','agent','hialpesa-vince://run?server='+encodeURIComponent('http://localhost:5080/api/v1')+'&token='+'A'.repeat(64)];
  }
  await main();await new Promise(resolve=>setImmediate(resolve));
  assert.equal(messages.filter(x=>x?.startsWith('¿Completar')).length,mode==='protocol'?0:1);
  assert.equal(messages.filter(x=>x?.startsWith('¿Confirmas')).length,1);
  assert.equal(writes.length,choice?2:1);assert.deepEqual(writes[0].colorConfirmations,[]);if(choice)assert.equal(writes[1].colorConfirmations[0].providerColor,'BLUE');
  assert(messages.some(x=>x.includes(choice?'Cierra el navegador cuando termines.':'Llenado cancelado')));child.stdout.end();child.emit('exit');if(choice)assert.equal(notice.killed,true);assert.equal(fs.existsSync(path.join(temp,'Hialpesa','Intranet','VinceAgent','agent.lock')),false);
 }
 console.log('Agente offline: protocolo inicia sin confirmación duplicada; archivo conserva confirmación; Sí/No de colores se mantiene en ambos modos.');
 }finally{process.env.LOCALAPPDATA=oldLocal;process.argv=oldArgs;global.fetch=oldFetch;Module._load=original;fs.readFileSync=realRead;}
})().catch(e=>{console.error(e);process.exitCode=1});



