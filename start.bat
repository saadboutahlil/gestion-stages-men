@echo off
echo Lancement du Backend (.NET 8)...
start cmd /k "cd backend\src\Api && dotnet run"

echo Lancement du Frontend (Angular 21)...
start cmd /k "cd frontend && npm start"

echo ========================================================
echo Les serveurs sont en cours de demarrage dans de nouvelles fenetres.
echo Backend  : http://localhost:5145
echo Frontend : http://localhost:4200
echo ========================================================
pause
