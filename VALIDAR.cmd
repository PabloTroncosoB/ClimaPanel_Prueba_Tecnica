@echo off
setlocal
echo Validando ClimaPanel...
dotnet --version || goto error
dotnet restore || goto error
dotnet build --configuration Debug --no-restore || goto error
dotnet test --configuration Debug --no-build || goto error
echo.
echo VALIDACION COMPLETADA CORRECTAMENTE.
exit /b 0

:error
echo.
echo LA VALIDACION NO PUDO COMPLETARSE. Revise el mensaje anterior.
exit /b 1
