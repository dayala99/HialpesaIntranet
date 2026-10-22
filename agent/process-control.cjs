function inspectLock(value,probe=pid=>process.kill(pid,0)){
 const text=String(value).trim();if(!/^[1-9]\d*$/.test(text)||!Number.isSafeInteger(Number(text)))return 'stale';
 try{probe(Number(text));return 'active';}catch(error){if(error.code==='ESRCH')return 'stale';if(error.code==='EPERM'||error.code==='EACCES')return 'denied';throw error;}
}
function stopChild(child){if(!child||child.killed||child.exitCode!==null)return false;try{return child.kill();}catch(error){if(['EPERM','EACCES','ESRCH'].includes(error.code))return false;throw error;}}
module.exports={inspectLock,stopChild};
