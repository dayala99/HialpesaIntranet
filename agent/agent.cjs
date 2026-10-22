'use strict';
const {inspectLock,stopChild}=require('./process-control.cjs');
const fs=require('node:fs');const path=require('node:path');const crypto=require('node:crypto');
function validateJob(envelope,publicKey,now=Date.now()){
 if(!envelope||typeof envelope.payload!=='string'||typeof envelope.signature!=='string')throw Error('Archivo de llenado no válido.');
 const data=Buffer.from(envelope.payload,'base64');
 if(!crypto.verify('RSA-SHA256',data,publicKey,Buffer.from(envelope.signature,'base64')))throw Error('La firma del archivo no corresponde a esta instalación de la intranet.');
 const job=JSON.parse(data.toString('utf8'));
 const poOnly=['preserve-provider','po-required'].includes(job.mode);
 if(job.mode!==undefined&&!poOnly)throw Error('Modo de agente no válido.');
 if(job.schema!=='hialpesa-vince/1'||(!poOnly&&(!/^\d{3}$/.test(job.series)||!/^\d{6}$/.test(job.number)))||!/^PO-\d{1,20}$/.test(job.po))throw Error('Orden de compra no válida.');
 const issued=Date.parse(job.issuedAt),expires=Date.parse(job.expiresAt);
 if(!Number.isFinite(issued)||!Number.isFinite(expires)||issued>now+60000||expires<=now||expires-issued>31*60000)throw Error('La operación venció. Selecciona nuevamente el PO en la intranet.');
 if(!Array.isArray(job.rows)||(job.mode!=='preserve-provider'&&!job.rows.length)||job.rows.length>20000||job.rows.some(r=>!r||typeof r.item!=='string'||typeof r.combination!=='string'||typeof r.purchased!=='number'||!Number.isFinite(r.purchased)||r.purchased<0))throw Error('Los datos de cantidades no son válidos.');
 return job;
}
async function readProtocolJob(link,servers,fetcher=fetch){
 const uri=new URL(link);
 if(uri.protocol!=='hialpesa-vince:'||uri.hostname!=='run'||uri.username||uri.password||uri.port||!['','/'].includes(uri.pathname)||uri.hash||[...uri.searchParams.keys()].some(k=>!['server','token'].includes(k))||uri.searchParams.getAll('server').length!==1||uri.searchParams.getAll('token').length!==1)throw Error('Enlace VINCE no válido.');
 const server=uri.searchParams.get('server'),token=uri.searchParams.get('token');
 if(!Array.isArray(servers)||!servers.includes(server)||!/^[A-F0-9]{64}$/.test(token||''))throw Error('El enlace no corresponde a un servidor de la intranet autorizado.');
 let response;
 try{response=await fetcher(server+'/proveedor/recuperar-llenado',{method:'POST',headers:{'Content-Type':'application/json','X-Intranet-Agent':'1'},body:JSON.stringify({token}),redirect:'error',signal:AbortSignal.timeout(15000)});}catch{throw Error('No se pudo conectar con la intranet. Comprueba la red y selecciona nuevamente el PO.');}
 if(response.status===410)throw Error('La apertura venció o ya fue utilizada. Selecciona nuevamente el PO en la intranet.');
 if(!response.ok)throw Error('La intranet no autorizó la apertura del agente. Selecciona nuevamente el PO.');
 const chunks=[];let size=0;
 for await(const chunk of response.body){size+=chunk.length;if(size>10*1024*1024)throw Error('La información de llenado excede el tamaño permitido.');chunks.push(chunk);}
 return JSON.parse(Buffer.concat(chunks).toString('utf8'));
}
module.exports={validateJob,readProtocolJob};

function ps(code,options={}){const r=require('node:child_process').spawnSync('powershell.exe',['-NoProfile','-Command',"$ErrorActionPreference='Stop'; [Console]::InputEncoding=[Text.UTF8Encoding]::new(); [Console]::OutputEncoding=[Text.UTF8Encoding]::new(); "+code],{windowsHide:true,encoding:'utf8',...options});if(r.status!==0)throw Error('No se pudo completar la operación de Windows.');return r.stdout?.trim();}
function dialog(message,confirm){const script=fs.readFileSync(path.join(__dirname,'dialog.ps1'),'utf8').replace(/^\uFEFF/,'');return ps('$confirm=$'+(confirm?'true':'false')+';'+script,{input:message})==='Yes';}
function showNotice(message){
 const script=fs.readFileSync(path.join(__dirname,'dialog.ps1'),'utf8');
 const child=require('node:child_process').spawn('powershell.exe',['-NoProfile','-Command',"$ErrorActionPreference='Stop';[Console]::InputEncoding=[Text.UTF8Encoding]::new();[Console]::OutputEncoding=[Text.UTF8Encoding]::new();$confirm=$false;"+script],{windowsHide:true,stdio:['pipe','ignore','ignore']});
 child.on('error',()=>{});child.stdin.on('error',()=>{});child.stdin.end(message);return child;
}
const secretPathScript="$p=Join-Path $env:LOCALAPPDATA 'Hialpesa/Intranet/avery.protected';";
function configure(){ps("Add-Type -AssemblyName System.Security; "+secretPathScript+"$u=Read-Host 'Usuario Avery';$s=Read-Host 'Clave Avery' -AsSecureString;$b=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($s);try{$v=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($b);if(!$u -or !$v){throw 'Usuario y clave obligatorios'};$json=@{Username=$u;Password=$v;LoginUrl='https://webservices.averydennison.com/specstar/login.aspx'}|ConvertTo-Json -Compress;$bytes=[Text.Encoding]::UTF8.GetBytes($json);$encrypted=[Security.Cryptography.ProtectedData]::Protect($bytes,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser);[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($p))|Out-Null;[IO.File]::WriteAllBytes($p,$encrypted)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b);if($bytes){[Array]::Clear($bytes,0,$bytes.Length)}}",{stdio:'inherit',windowsHide:false});}
function loadSecret(){try{return JSON.parse(ps("Add-Type -AssemblyName System.Security; "+secretPathScript+"if(!(Test-Path -LiteralPath $p)){throw 'Configura las credenciales'};$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($p),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser);try{[Console]::Out.Write([Text.Encoding]::UTF8.GetString($bytes))}finally{[Array]::Clear($bytes,0,$bytes.Length)}"));}catch{throw Error('No se pudo leer el acceso Avery de este usuario Windows. Ejecuta Configurar acceso.cmd en la carpeta del agente y vuelve a seleccionar el PO en la intranet.');}}
function install(){
 const reg=require('node:child_process').spawnSync;
 const entries=[['HKCU\\Software\\Classes\\hialpesa-vince','URL:Agente VINCE'],['HKCU\\Software\\Classes\\hialpesa-vince\\shell\\open\\command','"'+process.execPath+'" "'+__filename+'" "%1"'],['HKCU\\Software\\Classes\\.hialpesa-vince','Hialpesa.Vince.Job'],['HKCU\\Software\\Classes\\Hialpesa.Vince.Job','Llenado VINCE'],['HKCU\\Software\\Classes\\Hialpesa.Vince.Job\\shell\\open\\command','"'+process.execPath+'" "'+__filename+'" "%1"']];
 for(const [key,value] of entries){if(reg('reg.exe',['add',key,'/ve','/t','REG_SZ','/d',value,'/f'],{windowsHide:true,stdio:'ignore'}).status!==0)throw Error('No se pudo registrar la apertura de archivos VINCE.');}
 if(reg('reg.exe',['add','HKCU\\Software\\Classes\\hialpesa-vince','/v','URL Protocol','/t','REG_SZ','/d','','/f'],{windowsHide:true,stdio:'ignore'}).status!==0)throw Error('No se pudo registrar el protocolo VINCE.');
 console.log('Agente instalado para este usuario Windows. Requiere Google Chrome.');
 if(!fs.existsSync(path.join(process.env.LOCALAPPDATA,'Hialpesa','Intranet','avery.protected')))configure();
 dialog('Agente instalado. En la intranet selecciona el PO y confirma Abrir agente VINCE.',false);
}
async function main(){
 if(process.platform!=='win32')throw Error('El agente requiere Windows.');
 if(process.argv[2]==='--install')return install();
 if(process.argv[2]==='--configure')return configure();
 const file=process.argv[2];const fromIntranet=Boolean(file?.startsWith('hialpesa-vince:'));let envelope;
 if(fromIntranet){
  const config=JSON.parse(fs.readFileSync(path.join(__dirname,'agent-config.json'),'utf8').replace(/^\uFEFF/,''));
  envelope=await readProtocolJob(file,config.servers);
 }else{
  if(!file||!file.toLowerCase().endsWith('.hialpesa-vince')||fs.statSync(file).size>10*1024*1024)throw Error('Inicia el agente desde el botón de la intranet.');
  envelope=JSON.parse(fs.readFileSync(file,'utf8'));
 }
 const job=validateJob(envelope,fs.readFileSync(path.join(__dirname,'public.pem')));
 if(!fromIntranet&&!dialog('¿Completar '+job.po+' de la OC '+job.series+'-'+job.number+' en VINCE?\n\nSe sobrescribirán Quantity To Order de RFID y UPC Stickers. MSRP no se modifica. No se pulsará Save Order ni se enviará el pedido.',true))return;
 const credentials=loadSecret();if(!credentials.Username||!credentials.Password||credentials.LoginUrl!=='https://webservices.averydennison.com/specstar/login.aspx')throw Error('Configura el acceso Avery con Configurar acceso.cmd.');
 const work=path.join(process.env.LOCALAPPDATA,'Hialpesa','Intranet','VinceAgent');fs.mkdirSync(path.join(work,'test-results'),{recursive:true});
 // A lock prevents concurrent browser fills under this Windows account; stale locks are detected by PID.
 const lock=path.join(work,'agent.lock');
 if(fs.existsSync(lock)){const state=inspectLock(fs.readFileSync(lock,'utf8'));if(state==='active')throw Error('Ya hay un llenado abierto. Cierra primero el navegador del agente.');if(state==='denied')throw Error('Windows no permite comprobar el proceso de una sesión anterior del agente. Cierra sus ventanas. Si ya están cerradas, elimina únicamente el archivo '+lock+' y vuelve a abrir el agente desde la intranet.');fs.unlinkSync(lock);}
 const fd=fs.openSync(lock,'wx');fs.writeSync(fd,String(process.pid));fs.closeSync(fd);const clean=()=>{try{fs.unlinkSync(lock)}catch{}};process.on('exit',clean);
 const worker=require('node:child_process').spawn(process.execPath,[path.join(__dirname,'tools','provider-login.cjs')],{cwd:work,windowsHide:true,stdio:['pipe','pipe','ignore'],env:{...process.env,AVERY_COLOR_CONFIRMATION_UI:'browser',AVERY_USERNAME:credentials.Username,AVERY_PASSWORD:credentials.Password,AVERY_LOGIN_URL:credentials.LoginUrl}});
 credentials.Password='';
 const trace=(event,details={})=>{try{const file=path.join(work,'test-results','agent-events.jsonl');if(fs.existsSync(file)&&fs.statSync(file).size>1048576)fs.writeFileSync(file,'');fs.appendFileSync(file,JSON.stringify({time:new Date().toISOString(),po:job.po,event,...details})+'\n');}catch{}};
 const approved=[];const send=()=>worker.stdin.write(JSON.stringify({po:job.po,series:job.series,number:job.number,rows:job.rows,mode:job.mode,colorConfirmations:approved})+'\n');
 let replied=false,notice,stopped=false;
 const timeoutAction=()=>{replied=true;stopChild(worker);dialog('El proveedor excedió el tiempo de espera. Selecciona nuevamente el PO en la intranet.',false);};
 let timer=setTimeout(timeoutAction,240000);
 worker.on('error',()=>{replied=true;clearTimeout(timer);clean();dialog('No se pudo iniciar el navegador del agente.',false);});
 const lines=require('node:readline').createInterface({input:worker.stdout});let rounds=0;
 const finishWorker=()=>{
  if(stopped)return;stopped=true;clearTimeout(timer);clean();
  if(notice&&!notice.killed)stopChild(notice);
  lines.close();worker.stdin.destroy();worker.stdout.destroy();
  if(!replied)dialog('El navegador se cerró antes de completar el llenado. Selecciona nuevamente el PO para reintentar.',false);
  if(require.main===module)process.exit(0);
 };
 worker.once('exit',finishWorker);worker.once('close',finishWorker);
 lines.on('line',line=>{
  try{
   const result=JSON.parse(line);replied=true;
   if(result.status==='awaiting-mismatch-acknowledgment'){clearTimeout(timer);trace('browser-mismatch-shown');return;}
   if(result.status==='failed'&&result.displayedInBrowser){clearTimeout(timer);trace('browser-mismatch-acknowledged');return;}
   if(result.status==='awaiting-color-confirmation'){clearTimeout(timer);trace('browser-color-confirmation-requested',{count:result.count});return;}
   if(result.status==='color-confirmed'){trace('browser-color-confirmation-accepted');clearTimeout(timer);timer=setTimeout(timeoutAction,240000);return;}
   if(result.status==='cancelled'){clearTimeout(timer);trace('browser-color-confirmation-cancelled');notice=showNotice('Llenado cancelado. No se cambiaron cantidades.');return;}
   if(result.status==='confirmation-required'){
    if(++rounds>6||!Array.isArray(result.confirmations)||!result.confirmations.length)throw Error('No se pudo confirmar la equivalencia.');
    const detail=result.confirmations.map(c=>'Estilo: '+c.style+' / código: '+c.colorCode+'\nProveedor: '+c.providerColor+'\nGestión: '+c.gestionColor).join('\n\n');
    trace('color-confirmation-requested',{count:result.confirmations.length});
    const accepted=dialog('¿Confirmas estas equivalencias de color para '+job.po+'?\n\n'+detail,true);
    trace('color-confirmation-response',{accepted});
    if(!accepted){clearTimeout(timer);dialog('Llenado cancelado. No se cambiaron cantidades.',false);return;}
    approved.push(...result.confirmations);send();return;
   }
   clearTimeout(timer);
   if(!['signed-in','po-filled'].includes(result.status))throw Error(result.detail||'El portal detuvo el llenado en: '+(result.stage||'inicio')+'. Revisa la ventana del proveedor.');
   notice=showNotice(result.report?'Reporte guardado en:\n'+result.report+'\n\nCierra el navegador cuando termines.':'Cierra el navegador cuando termines.');
  }catch(e){trace('agent-error',{message:e.message});clearTimeout(timer);dialog(e.message,false);}
 });
 send();
}

if(require.main===module)main().catch(e=>{console.error(e.message);try{dialog(e.message,false);}catch{}process.exitCode=1;});

module.exports.main=main;

module.exports.checkAccess=()=>{const c=loadSecret();return Boolean(c.Username&&c.Password&&c.LoginUrl==='https://webservices.averydennison.com/specstar/login.aspx');};











