@echo off
cd %~dp0

SET directory=%1
ECHO "%directory%"

if not defined directory set directory=%ProgramFiles%\Alpacka

ECHO "%directory%"

CALL :NORMALIZEPATH "%directory%"
SET directory=%RETVAL%

ECHO "%directory%"

echo "setting %%ALPACKA_CLI%% to %~dp0\%directory%"
set ALPACKA_CLI="%directory%"
echo "%%ALPACKA_CLI%% = %ALPACKA_CLI%"
setx ALPACKA_CLI %ALPACKA_CLI%

:: copy bin into target location (program files)
robocopy %~dp0\bin "%directory%" *.* /MIR

:: test if the alpacka bin is findeable
where alpacka > NUL 2> NUL 
if errorlevel 1 goto :notInPath
goto :END

:notInPath
echo "not in path"
echo "trying to add '%%ALPACKA_CLI%%' to path"
:: DOS is horrible for forcing us to do THIS

for /F "tokens=2* delims= " %%f IN ('reg query "HKCU\Environment" /v Path ^| findstr /i path') do set OLD_USER_PATH=%%g
if defined OLD_USER_PATH setx PATH "%OLD_USER_PATH%;%%ALPACKA_CLI%%"


REM for /F "tokens=2* delims= " %%f IN ('reg query "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path ^| findstr /i path') do set OLD_SYSTEM_PATH=%%g
REM if defined OLD_SYSTEM_PATH setx PATH "%OLD_SYSTEM_PATH%;%%ALPACKA_CLI%%;" -m
REM setx ALPACKA_CLI "%ALPACKA_CLI%" -m


:END
pause

:: ========== FUNCTIONS ==========
EXIT /B
:NORMALIZEPATH
  SET RETVAL=%~dpfn1
  EXIT /B
