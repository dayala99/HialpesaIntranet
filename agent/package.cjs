const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),settingsPath=path.join(root,'backend/src/Intranet.Api/appsettings.Local.json');
const settings=JSON.parse(fs.readFileSync(settingsPath,'utf8').replace(/^\uFEFF/,''));
settings.ProviderAgent??={};
if(!settings.ProviderAgent.SigningPrivateKey){const keys=crypto.generateKeyPairSync('rsa',{modulusLength:3072,privateKeyEncoding:{type:'pkcs8',format:'pem'},publicKeyEncoding:{type:'spki',format:'pem'}});settings.ProviderAgent.SigningPrivateKey=keys.privateKey;fs.writeFileSync(settingsPath,JSON.stringify(settings,null,2)+'\n');}
const publicKey=crypto.createPublicKey(settings.ProviderAgent.SigningPrivateKey).export({type:'spki',format:'pem'});
fs.writeFileSync(path.join(root,'agent/public.pem'),publicKey);
const output=path.join(root,'Compilados','Agente');fs.mkdirSync(output,{recursive:true});
for(const file of ['agent.cjs','process-control.cjs','dialog.ps1','agent-config.json','public.pem','Instalar agente.cmd','Configurar acceso.cmd','LEEME.md'])fs.copyFileSync(path.join(root,'agent',file),path.join(output,file));
const runtimeTarget=path.join(output,'node.exe');
const digest=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
if(!fs.existsSync(runtimeTarget)||digest(runtimeTarget)!==digest(process.execPath))fs.copyFileSync(process.execPath,runtimeTarget);
fs.mkdirSync(path.join(output,'tools'),{recursive:true});
for(const file of ['provider-remove-zero-skus.cjs','provider-preview.cjs','provider-shipping.cjs','provider-order-setup.cjs','provider-login.cjs','provider-color-dialog.cjs','provider-fields.cjs','provider-lifecycle.cjs','provider-auth.cjs','provider-quantities.cjs','provider-color-aliases.json'])fs.copyFileSync(path.join(root,'frontend/tools',file),path.join(output,'tools',file));
for(const name of ['playwright','playwright-core'])fs.cpSync(path.join(root,'frontend/node_modules',name),path.join(output,'node_modules',name),{recursive:true});
console.log('Agente preparado con Node y Playwright incluidos. Chrome debe estar instalado.');







const visited=new Set();function bundlePackage(name,from){const manifest=require.resolve(name+'/package.json',{paths:[from]});if(visited.has(manifest))return;visited.add(manifest);const dir=path.dirname(manifest),rel=path.relative(path.join(root,'frontend/node_modules'),dir);if(rel.startsWith('..'))throw Error('Dependency outside frontend node_modules');fs.cpSync(dir,path.join(output,'node_modules',rel),{recursive:true});const pkg=JSON.parse(fs.readFileSync(manifest,'utf8'));for(const dep of Object.keys(pkg.dependencies||{}))bundlePackage(dep,dir);}
bundlePackage('exceljs',path.join(root,'frontend'));
