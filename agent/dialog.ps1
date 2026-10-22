Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()
$message=[Console]::In.ReadToEnd()
$form=New-Object System.Windows.Forms.Form
$form.TopMost=$true;$form.ShowInTaskbar=$true;$form.Add_Shown({$form.Activate()});
$form.Text='Agente VINCE';$form.StartPosition='CenterScreen';$form.FormBorderStyle='FixedDialog';$form.MaximizeBox=$false;$form.MinimizeBox=$false
$form.BackColor=[Drawing.Color]::White;$form.Font=New-Object Drawing.Font('Segoe UI',10);$form.AutoScaleMode='Dpi'
$height= [Math]::Min(440,[Math]::Max(125,[Windows.Forms.TextRenderer]::MeasureText($message,$form.Font,[Drawing.Size]::new(454,2000),[Windows.Forms.TextFormatFlags]::WordBreak).Height+24))
$form.ClientSize=[Drawing.Size]::new(510,112+$height+76)
$header=New-Object Windows.Forms.Panel;$header.SetBounds(0,0,510,100);$header.BackColor=[Drawing.ColorTranslator]::FromHtml('#22594e');$form.Controls.Add($header)
$brand=New-Object Windows.Forms.Label;$brand.Text='HIALPESA  /  INTRANET';$brand.SetBounds(26,18,454,22);$brand.ForeColor=[Drawing.ColorTranslator]::FromHtml('#cbe5d9');$brand.Font=New-Object Drawing.Font('Segoe UI',9,[Drawing.FontStyle]::Bold);$header.Controls.Add($brand)
$title=New-Object Windows.Forms.Label;$title.Text='Agente VINCE';$title.SetBounds(24,44,454,36);$title.ForeColor=[Drawing.Color]::White;$title.Font=New-Object Drawing.Font('Segoe UI',19,[Drawing.FontStyle]::Bold);$header.Controls.Add($title)
$body=New-Object Windows.Forms.Panel;$body.SetBounds(26,118,458,$height-12);$body.AutoScroll=$true;$form.Controls.Add($body)
$text=New-Object Windows.Forms.Label;$text.Text=$message;$text.SetBounds(0,0,434,[Windows.Forms.TextRenderer]::MeasureText($message,$form.Font,[Drawing.Size]::new(434,2000),[Windows.Forms.TextFormatFlags]::WordBreak).Height+12);$text.AutoSize=$false;$text.ForeColor=[Drawing.ColorTranslator]::FromHtml('#203637');$text.Font=$form.Font;$body.Controls.Add($text)
$footer=New-Object Windows.Forms.Panel;$footer.SetBounds(0,112+$height,510,76);$footer.BackColor=[Drawing.ColorTranslator]::FromHtml('#f4f7f5');$form.Controls.Add($footer)
$ok=New-Object Windows.Forms.Button;$ok.Text=if($confirm){'Sí, confirmar'}else{'Aceptar'};$ok.SetBounds(332,18,152,40);$ok.FlatStyle='Flat';$ok.FlatAppearance.BorderSize=0;$ok.BackColor=[Drawing.ColorTranslator]::FromHtml('#22594e');$ok.ForeColor=[Drawing.Color]::White;$ok.Font=New-Object Drawing.Font('Segoe UI',10,[Drawing.FontStyle]::Bold);$ok.Cursor='Hand';$ok.DialogResult=if($confirm){[Windows.Forms.DialogResult]::Yes}else{[Windows.Forms.DialogResult]::OK};$footer.Controls.Add($ok);$form.AcceptButton=$ok
if($confirm){$no=New-Object Windows.Forms.Button;$no.Text='No';$no.SetBounds(226,18,94,40);$no.FlatStyle='Flat';$no.FlatAppearance.BorderColor=[Drawing.ColorTranslator]::FromHtml('#cfdbd7');$no.BackColor=[Drawing.Color]::White;$no.ForeColor=[Drawing.ColorTranslator]::FromHtml('#203637');$no.DialogResult=[Windows.Forms.DialogResult]::No;$no.Cursor='Hand';$footer.Controls.Add($no);$form.CancelButton=$no;$form.Add_Shown({$no.Focus()})}else{$form.CancelButton=$ok}
try{$form.ShowDialog().ToString()}finally{$form.Dispose()}


