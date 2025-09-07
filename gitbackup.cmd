@echo off
set local on
set inifile=%~dpn0.ini

if not exist "%inifile%" (echo gitbackup_backupdir=
echo gitbackup_rootdir=) > "%inifile%" & echo "%inifile%" created. please edit and rerun & pause

for /F "usebackq delims== tokens=1,2" %%A in ("%inifile%") do set %%A=%%B

if "%gitbackup_backupdir%" == "" echo Missing gitbackup_backupdir in "%~0.ini" & pause & GOTO :EOF
if "%gitbackup_rootdir%" == "" echo Missing gitbackup_rootdir in "%~0.ini" & pause & GOTO :EOF

set GIT_DIR=%gitbackup_backupdir%
set rootdir=%gitbackup_rootdir%

if not exist "%GIT_DIR%" (
	mkdir "%GIT_DIR%"
	git init
	git config core.bare false
	git config gc.auto 0
	(echo *.vpx
	 echo *.directb2s
	 echo *.exe
	 echo *.cmd
	 echo *.bat
	 echo *.md
	 echo *.dll
	 echo *.log
	 echo *.webp
	 echo *.dat
	 echo *.html
	 echo *.txt
	 echo *.url
	 echo !VPMAlias.txt
	 echo !ScreenRes.txt
	 echo **/assets
	 echo **/Music
	 echo **/nvram
	 echo **/roms
	 echo **/samples
	 echo **/snap
	 echo **/Scripts
	 echo **/wave
	 echo **/XDMD
	 echo **/artwork
	 echo **/VPinMAMETest
	 echo **/DMDext
	 echo **/*DMD*/
	 echo **/doc*
	 echo **/installguide) > "%GIT_DIR%\info\exclude"
)
cd "%gitbackup_rootdir%"
git add -A 
git commit -m "Backup"