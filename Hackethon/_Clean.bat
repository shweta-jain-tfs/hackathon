@echo off
setlocal enabledelayedexpansion

rem Remember original directory so we can restore it at the end
set "ORIG_DIR=%CD%"

rem Ensure we run from the solution directory (location of this script)
cd /d "%~dp0"

echo Cleaning all bin and obj folders for every .csproj under:
cd

echo.
echo Searching for projects...
for /r %%f in (*.csproj) do (
    set "projdir=%%~dpf"

    if exist "!projdir!bin" (
        echo Deleting "!projdir!bin"...
        rmdir /s /q "!projdir!bin"
    )

    if exist "!projdir!obj" (
        echo Deleting "!projdir!obj"...
        rmdir /s /q "!projdir!obj"
    )
)

echo.
echo Done deleting bin and obj folders.
echo.

set /p DELETE_VS="Do you also want to delete the .vs folder in %cd% (y/N)? "
if /I "%DELETE_VS%"=="Y" (
    if exist "%cd%\.vs" (
        echo Deleting "%cd%\.vs"...
        rmdir /s /q "%cd%\.vs"
    ) else (
        echo .vs folder not found in %cd%.
    )
) else (
    echo Skipping deletion of .vs folder.
)

echo.
pause
cd /d "%ORIG_DIR%"
endlocal
