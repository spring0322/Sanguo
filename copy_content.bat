@echo off
echo Copying Content folder to target directories...

REM Get the current directory (where this batch file is located)
set "SOURCE_DIR=%~dp0Content"
set "TARGET1=%~dp0WorldOfTheThreeKingdoms\bin\x64\Debug"
set "TARGET2=%~dp0WorldOfTheThreeKingdomsEditor\bin\Debug"

echo.
echo Source: %SOURCE_DIR%
echo Target 1: %TARGET1%
echo Target 2: %TARGET2%
echo.

REM Check if source directory exists
if not exist "%SOURCE_DIR%" (
    echo ERROR: Source directory "%SOURCE_DIR%" does not exist!
    pause
    exit /b 1
)

REM Create target directories if they don't exist
if not exist "%TARGET1%" (
    echo Creating directory: %TARGET1%
    mkdir "%TARGET1%"
)

if not exist "%TARGET2%" (
    echo Creating directory: %TARGET2%
    mkdir "%TARGET2%"
)

echo.
echo Copying to WorldOfTheThreeKingdoms\bin\x64\Debug...
robocopy "%SOURCE_DIR%" "%TARGET1%\Content" /E /R:3 /W:1 /NFL /NDL /NJH /NJS
if %ERRORLEVEL% GEQ 8 (
    echo ERROR: Failed to copy to first target directory!
    pause
    exit /b 1
) else (
    echo Successfully copied to first target directory.
)

echo.
echo Copying to WorldOfTheThreeKingdomsEditor\bin\Debug...
robocopy "%SOURCE_DIR%" "%TARGET2%\Content" /E /R:3 /W:1 /NFL /NDL /NJH /NJS
if %ERRORLEVEL% GEQ 8 (
    echo ERROR: Failed to copy to second target directory!
    pause
    exit /b 1
) else (
    echo Successfully copied to second target directory.
)

echo.
echo Content folder copied successfully to both target directories!
echo.
pause
