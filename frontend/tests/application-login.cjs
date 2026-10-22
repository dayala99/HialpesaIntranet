const {chromium}=require('playwright'),assert=require('node:assert/strict');
(async()=>{
 const password=process.env.INTRANET_TEST_PASSWORD;delete process.env.INTRANET_TEST_PASSWORD;if(!password)throw Error('Missing test password');
 const browser=await chromium.launch({channel:'chrome',headless:true});
 try{
  const context=await browser.newContext({viewport:{width:1280,height:900}}),request=context.request;
  const api='http://localhost:5080/api/v1',headers={'X-Intranet-Session':'1'};
  let response=await request.get(api+'/sesion');assert.equal(response.status(),401);assert.equal(response.headers()['www-authenticate'],undefined);
  response=await request.get(api+'/logistica/avios/ordenes');assert.equal(response.status(),401);
  response=await request.post(api+'/sesion/login',{headers,data:{username:'programador04',password:''}});assert.equal(response.status(),400);
  response=await request.post(api+'/sesion/login',{headers,data:{username:'programador04',password:'not-correct'}});assert.equal(response.status(),401);
  response=await request.post(api+'/sesion/login',{headers,data:{username:'programador04',password}});assert.equal(response.status(),200);
  const identity=await response.json();assert.equal(identity.displayName,'programador04');assert.equal(identity.gestionUser,'programador04');assert.equal(identity.canWriteDatabase,true);
  assert.match(response.headers()['set-cookie'],/httponly/i);
  response=await request.get(api+'/logistica/avios/ordenes?page=1&search=350000');assert.equal(response.status(),200);assert.ok((await response.json()).rows.length>0);
  response=await request.post(api+'/sesion/logout',{headers});assert.equal(response.status(),204);assert.equal((await request.get(api+'/sesion')).status(),401);
  console.log('PASS API: protected reads, no Windows challenge, blank/wrong password denied, valid login, read-all and logout.');
  const page=await context.newPage();await page.goto('http://localhost:4200/logistica/avios/nueva');
  await page.getByRole('heading',{name:/Iniciar sesi/}).waitFor();
  assert.ok(page.url().includes('/login'));const submit=page.getByRole('button',{name:'Ingresar',exact:true});assert.ok(await submit.isDisabled());
  await page.getByLabel('Usuario',{exact:true}).fill('programador04');
  await page.getByLabel('Contraseña',{exact:true}).fill('not-correct');await submit.click();await page.getByRole('alert').filter({hasText:/incorrectos/}).waitFor();
  await page.getByLabel('Contraseña',{exact:true}).fill(password);await submit.click();await page.getByRole('heading',{name:'Registro de orden de compra',exact:true}).waitFor().catch(async error=>{await page.screenshot({path:'test-results/login-failure.png'});console.error('Current route:',page.url());throw error;});
  await page.reload();await page.getByRole('heading',{name:'Registro de orden de compra',exact:true}).waitFor().catch(async error=>{await page.screenshot({path:'test-results/login-failure.png'});console.error('Current route:',page.url());throw error;});
  const cookies=await context.cookies('http://localhost:5080');assert.ok(cookies.find(x=>x.name==='Hialpesa.Intranet.Session'&&x.httpOnly));
  await page.getByRole('button',{name:'Cerrar sesion',exact:true}).click();await page.getByRole('heading',{name:/Iniciar sesi/}).waitFor();
  await page.screenshot({path:'test-results/login-desktop.png'});await page.setViewportSize({width:390,height:844});await page.screenshot({path:'test-results/login-mobile.png'});
  console.log('PASS UI: route guard, incorrect login, valid login, refresh restores session, logout, desktop/mobile.');
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exit(1)});

