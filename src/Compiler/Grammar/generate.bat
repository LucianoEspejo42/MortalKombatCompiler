@echo off
echo ========================================
echo   Generando Parser de Mortal Kombat
echo ========================================
echo.

REM Verificar que existe COCO/R
if not exist "..\..\..\tools\Coco.exe" (
    echo ERROR: No se encuentra Coco.exe en tools/
    echo Por favor descarga COCO/R desde: http://www.ssw.uni-linz.ac.at/Coco/
    pause
    exit /b 1
)

REM Limpiar archivos anteriores
echo Limpiando archivos generados anteriormente...
if exist "..\Generated\Scanner.cs" del "..\Generated\Scanner.cs"
if exist "..\Generated\Parser.cs" del "..\Generated\Parser.cs"
if exist "..\Generated\Errors.cs" del "..\Generated\Errors.cs"

echo.
echo Generando scanner y parser con COCO/R...
echo.

REM Ejecutar COCO/R
..\..\..\tools\Coco.exe MortalKombat.atg -namespace Compiler.Generated -o ..\Generated

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: La generacion fallo. Revisa la gramatica.
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Generacion completada exitosamente
echo ========================================
echo.
echo Archivos generados en: src\Compiler\Generated\
echo   - Scanner.cs
echo   - Parser.cs  
echo   - Errors.cs
echo.

pause