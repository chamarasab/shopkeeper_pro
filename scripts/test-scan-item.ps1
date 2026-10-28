# Simulate typing barcode '123456789' into the active window and pressing Enter
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.SendKeys]::SendWait("123456789{ENTER}")
Start-Sleep -Milliseconds 800
[System.Windows.Forms.SendKeys]::SendWait("3*123456788{ENTER}")
Start-Sleep -Milliseconds 800
