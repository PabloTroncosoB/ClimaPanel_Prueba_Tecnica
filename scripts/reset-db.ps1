$ErrorActionPreference = "Stop"
$db = Join-Path $PSScriptRoot "..\src\ClimaPanel.Web\data\climapanel.db"
Get-ChildItem "$db*" -ErrorAction SilentlyContinue | Remove-Item -Force
Write-Host "Base local eliminada. Se recreará al iniciar la aplicación." -ForegroundColor Green
