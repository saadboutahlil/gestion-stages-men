Write-Host "Lancement du Backend (.NET 8)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd backend\src\Api; dotnet run"

Write-Host "Lancement du Frontend (Angular 21)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd frontend; npm start"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Les serveurs sont en cours de démarrage dans de nouvelles fenêtres." -ForegroundColor Cyan
Write-Host "Backend  : http://localhost:5145" -ForegroundColor Yellow
Write-Host "Frontend : http://localhost:4200" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
