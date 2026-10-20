const {chromium}=require('playwright');const {authenticate}=require('./provider-auth.cjs');const fs=require('fs');
(async()=>{
 const username=process.env.AVERY_USERNAME,password=process.env.AVERY_PASSWORD;delete process.env.AVERY_USERNAME;delete process.env.AVERY_PASSWORD;
 const browser=await chromium.launch({channel:'chrome',headless:false});const context=await browser.newContext({viewport:{width:1280,height:900}});const page=await context.newPage();let stage='starting';
 try{
  const options={username,password,setStage:s=>{stage=s;}};
  await authenticate(page,options);
  console.log(JSON.stringify({status:'first-login-ready'}));
  await page.goto('https://www.retailcompliance.averydennison.com/prd1/acs/sf/ReLogonForm.html',{waitUntil:'domcontentloaded'});
  await authenticate(page,options);
  await page.screenshot({path:'test-results/avery-auth-recovered.png'});
  fs.writeFileSync('test-results/avery-auth-recovered.json',JSON.stringify({firstLogin:true,recoveredAfterLoginReturn:true,orderModified:false,time:new Date().toISOString()}));
  console.log(JSON.stringify({status:'recovered-search-ready'}));
  const keep=setInterval(()=>{},30000);browser.on('disconnected',()=>{clearInterval(keep);process.exit(0)});
 }catch(error){await page.screenshot({path:'test-results/avery-auth-recovery-failure.png'}).catch(()=>{});console.log(JSON.stringify({status:'failed',stage,reason:error.message}));await browser.close();process.exitCode=1;}
})().catch(()=>{console.log(JSON.stringify({status:'failed',stage:'launch'}));process.exit(1)});