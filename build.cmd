@echo off
rem Compile LP-AccesClaude.exe avec le compilateur C# integre a Windows (.NET Framework 4.x).
rem Aucun outil a installer. Resultat : dist\LP-AccesClaude.exe
setlocal
set ERR=0
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (echo Compilateur csc.exe introuvable. & goto :fin)
cd /d "%~dp0"
if not exist dist mkdir dist
rem /codepage:65001 : les sources sont en UTF-8 (accents)
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /win32icon:assets\app.ico /out:dist\LP-AccesClaude.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll src\*.cs
if errorlevel 1 (echo Echec de la compilation. & set ERR=1 & goto :fin)
echo OK : dist\LP-AccesClaude.exe
:fin
if not defined CI pause
exit /b %ERR%
