@echo off
rem Compile et lance les tests de la logique.
setlocal
set ERR=0
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
cd /d "%~dp0"
if not exist dist mkdir dist
"%CSC%" /nologo /codepage:65001 /out:dist\tests.exe /r:System.Core.dll src\Json.cs src\Core.cs src\Lang.cs test\Tests.cs || exit /b 1
dist\tests.exe
set ERR=%ERRORLEVEL%
if not defined CI pause
exit /b %ERR%
