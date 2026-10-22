const assert=require('node:assert/strict');const {readProtocolJob}=require('./agent.cjs');
(async()=>{
 const server='http://192.168.70.109:8109/BackEnd/api/v1',token='A'.repeat(64),link='hialpesa-vince://run?server='+encodeURIComponent(server)+'&token='+token;
 let calls=0;const fake=async(url,options)=>{calls++;assert.equal(url,server+'/proveedor/recuperar-llenado');assert.equal(options.method,'POST');assert.equal(options.headers['X-Intranet-Agent'],'1');assert.equal(options.redirect,'error');assert.deepEqual(JSON.parse(options.body),{token});return new Response(JSON.stringify({payload:'signed',signature:'test'}));};
 assert.deepEqual(await readProtocolJob(link,[server],fake),{payload:'signed',signature:'test'});assert.equal(calls,1);
 for(const bad of [link.replace('run?','other?'),link+'&token='+token,link+'&x=1',link+'#bad',link.replace(token,'invalid'),link.replace(encodeURIComponent(server),encodeURIComponent('http://attacker.example/api/v1')),link.replace('hialpesa-vince:','https:')])await assert.rejects(()=>readProtocolJob(bad,[server],fake));
 assert.equal(calls,1);
 await assert.rejects(()=>readProtocolJob(link,[server],async()=>new Response('',{status:410})),/venció/);
 await assert.rejects(()=>readProtocolJob(link,[server],async()=>{throw Error('network')}),/conectar/);
 console.log('Protocolo: destino autorizado, token válido, sin redirecciones; enlaces ajenos, duplicados y vencidos rechazados.');
})().catch(e=>{console.error(e);process.exit(1)});
