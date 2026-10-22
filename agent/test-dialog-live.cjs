const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),{spawnSync}=require('node:child_process');
const output=path.join(os.tmpdir(),'vince-dialog-visible-'+process.pid+'.txt');
let script=fs.readFileSync(path.join(__dirname,'dialog.ps1'),'utf8');
script=script.replace("try{$form.ShowDialog().ToString()}finally{$form.Dispose()}","$form.Opacity=0;$probe=New-Object Windows.Forms.Timer;$probe.Interval=500;$probe.Add_Tick({[IO.File]::WriteAllText('"+output.replace(/'/g,"''")+"','visible='+$form.Visible+';shown='+$form.IsHandleCreated);$probe.Stop();$form.DialogResult=[Windows.Forms.DialogResult]::No;$form.Close()});$probe.Start();try{$form.ShowDialog().ToString()}finally{$probe.Dispose();$form.Dispose()}");
const code="$ErrorActionPreference='Stop';[Console]::InputEncoding=[Text.UTF8Encoding]::new();[Console]::OutputEncoding=[Text.UTF8Encoding]::new();$confirm=$true;"+script;
const r=spawnSync('powershell.exe',['-NoProfile','-Command',code],{windowsHide:true,encoding:'utf8',input:'¿Confirmas estas equivalencias de color?\nProveedor: OPTIC WHITE\nGestión: OPTIC WHITE OPTIC WHITE',timeout:8000});
console.log(JSON.stringify({status:r.status,error:r.error?.code,probe:fs.existsSync(output)?fs.readFileSync(output,'utf8'):'no-show',result:r.stdout?.trim(),stderr:r.stderr?.slice(0,400)}));if(r.status!==0)process.exitCode=1;
