@echo off
echo Testing the military advisor dialogue system...
echo.
echo 1. Starting the game to test dialogue filtering
echo 2. Look for debug output in the console
echo 3. Test with different character combinations
echo.
echo Instructions:
echo - Run the game
echo - Try appointing different advisors
echo - Check the debug output for character IDs and matching scores
echo - Verify that exclusive dialogues are only used by the correct characters
echo.
echo Press any key to start the game...
pause > nul

cd /d "%~dp0"
start "" "WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe"