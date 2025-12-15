@echo off
echo Copying Content folder to Desktop project output directory...

REM Get the current directory (where this batch file is located)
set "SOURCE_DIR=%~dp0Content"
set "TARGET_DIR=%~dp0WorldOfTheThreeKingdoms.Desktop\bin\DesktopGL\AnyCPU\Debug"

echo.
echo Source: %SOURCE_DIR%
echo Target: %TARGET_DIR%
echo.

REM Check if source directory exists
if not exist "%SOURCE_DIR%" (
    echo ERROR: Source directory "%SOURCE_DIR%" does not exist!
    pause
    exit /b 1
)

REM Create target directory if it doesn't exist
if not exist "%TARGET_DIR%" (
    echo Creating directory: %TARGET_DIR%
    mkdir "%TARGET_DIR%"
)

echo.
echo Copying Content files to Desktop project output directory...
robocopy "%SOURCE_DIR%" "%TARGET_DIR%\Content" /E /R:3 /W:1 /NFL /NDL /NJH /NJS
if %ERRORLEVEL% GEQ 8 (
    echo ERROR: Failed to copy to target directory!
    pause
    exit /b 1
) else (
    echo Successfully copied Content files.
)

echo.
echo Creating Portraits directory...
if not exist "%TARGET_DIR%\Portraits" (
    mkdir "%TARGET_DIR%\Portraits"
    echo Portraits directory created.
) else (
    echo Portraits directory already exists.
)

echo.
echo Content folder copied successfully to Desktop project output directory!
echo.
pause