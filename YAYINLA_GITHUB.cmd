@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\publish_to_github.ps1"
echo.
echo Komut ciktisini kontrol edin. Bir hata varsa yukaridaki metni paylasin.
pause
