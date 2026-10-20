const LOGIN='https://webservices.averydennison.com/specstar/login.aspx';
const isLogin=url=>/\/login\.aspx$|\/ReLogonForm\.html$/i.test(new URL(url).pathname);
const retail=url=>{const u=new URL(url);return u.protocol==='https:'&&['www.retailcompliance.averydennison.com','retailcompliance.averydennison.com'].includes(u.hostname)&&u.pathname.endsWith('/OrderSetup.html');};
async function authenticate(page,{username,password,loginUrl=LOGIN,setStage=()=>{}}){
 if(loginUrl!==LOGIN)throw Error('auth-destination');
 const deadline=Date.now()+100000;
 const visible=selector=>page.locator(selector).isVisible().catch(()=>false);
 async function navigate(action){
  const navigation=page.waitForNavigation({waitUntil:'domcontentloaded',timeout:15000}).catch(()=>null);
  await action();await navigation;
  if(page.isClosed())throw Error('auth-browser-closed');
 }
 try{
  if(page.isClosed())throw Error('auth-browser-closed');
  if(retail(page.url())&&await visible('#orderSearchButton'))return;
  for(let attempt=0;attempt<3&&Date.now()<deadline;attempt++){
   setStage('login');
   if(!isLogin(page.url())||new URL(page.url()).pathname.endsWith('/ReLogonForm.html'))
    await page.goto(LOGIN,{waitUntil:'domcontentloaded',timeout:25000});
   const url=new URL(page.url());
   if(url.protocol!=='https:'||!['webservices.averydennison.com','www.webservices.averydennison.com'].includes(url.hostname))throw Error('auth-destination');
   await page.locator('#SsLogin1_txtUsername').fill(username,{timeout:10000});
   await page.locator('#SsLogin1_txtPassword').fill(password,{timeout:10000});
   await navigate(()=>page.locator('#SsLogin1_btnLogin').click({timeout:10000}));
   if(isLogin(page.url())){
    const entered=await page.locator('#SsLogin1_txtUsername').inputValue({timeout:5000}).catch(()=>null);
    // A cleared form is a session reset. A populated rejected form needs review.
    if(entered!==''){setStage('login-rejected');throw Error('auth-rejected');}
    continue;
   }
   await page.locator('#Header1_haLogoff').waitFor({state:'visible',timeout:10000});
   setStage('product-ordering');
   await navigate(()=>page.getByRole('link',{name:'PRODUCT ORDERING',exact:true}).click({timeout:10000}));
   if(isLogin(page.url()))continue;
   setStage('vince');
   await navigate(()=>page.getByRole('link',{name:'VINCE',exact:true}).click({timeout:10000}));
   if(isLogin(page.url()))continue;
   await page.waitForURL(url=>retail(url.href),{waitUntil:'domcontentloaded',timeout:20000});
   await page.locator('#orderSearchButton').waitFor({state:'visible',timeout:15000});
   return;
  }
  setStage('login');throw Error('auth-session-reset-limit');
 }catch(error){
  // Never propagate Playwright fill logs, which could contain credential values.
  if(/^auth-/.test(error.message))throw error;
  throw Error(page.isClosed()?'auth-browser-closed':'auth-navigation');
 }
}
module.exports={authenticate,isLogin};
