function bindBrowserLifetime(browser,context,onClosed){
 let closing=false,finished=false,hadPages=context.pages().length>0,deadline;
 const finish=()=>{if(!finished){finished=true;clearInterval(poll);clearTimeout(deadline);onClosed();}};
 const check=()=>{
  if(closing||finished||!hadPages||context.pages().some(page=>!page.isClosed()))return;
  closing=true;deadline=setTimeout(finish,2000);deadline.unref();
  browser.close().then(finish,finish);
 };
 const poll=setInterval(check,500);poll.unref();
 browser.once('disconnected',finish);context.once('close',finish);
 const watch=page=>{hadPages=true;page.once('close',()=>setImmediate(check));};
 context.pages().forEach(watch);context.on('page',watch);
}
module.exports={bindBrowserLifetime};
