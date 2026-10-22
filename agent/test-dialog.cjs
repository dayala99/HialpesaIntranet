const fs=require('node:fs'),path=require('node:path'),{spawnSync}=require('node:child_process'),assert=require('node:assert/strict');
const source=fs.readFileSync(path.join(__dirname,'dialog.ps1'),'utf8').replace(/^\uFEFF/,'');
for(const confirm of [false,true]){
 const target=path.join(__dirname,'../frontend/test-results/agent-dialog-'+(confirm?'confirm':'info')+'.png');
 const replacement="$form.Opacity=0;$form.ShowInTaskbar=$false;$form.Show();$form.CreateControl();if(!$form.TopMost){throw 'Dialogo sin prioridad visual'};if($ok.Text -ne '"+(confirm?'Sí, confirmar':'Aceptar')+"'){throw 'Botón incorrecto'};if($text.Text -ne $message){throw 'Texto incorrecto'};$bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height);$form.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$form.Width,$form.Height));$bitmap.Save('"+target.replace(/'/g,"''")+"',[Drawing.Imaging.ImageFormat]::Png);$bitmap.Dispose();'OK'";
 const code="$ErrorActionPreference='Stop';[Console]::InputEncoding=[Text.UTF8Encoding]::new();[Console]::OutputEncoding=[Text.UTF8Encoding]::new();$confirm=$"+(confirm?'true':'false')+';'+source.replace('$form.ShowDialog().ToString()',replacement);
 const result=spawnSync('powershell.exe',['-NoProfile','-Command',code],{input:confirm?'¿Confirmas estas equivalencias de color?\nProveedor: BLACK/MED H GREY\nGestión: BLACK/MED H GREY BLACK/ME':'Cierra el navegador cuando termines.',encoding:'utf8',windowsHide:true});assert.equal(result.status,0,result.stderr);assert.equal(result.stdout.trim(),'OK');assert(fs.statSync(target).size>1000);
}
console.log('Diálogos Windows: construidos y renderizados sin abrir ventanas; texto, acentos y botones correctos.');


