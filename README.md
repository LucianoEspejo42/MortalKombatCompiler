# Compilador de Mortal Kombat 3 Ultimate - Cyrax Fatalities & Brutalities

Compilador completo para detectar y ejecutar las fatalities y brutalities de Cyrax usando inputs de un control Xbox (XInput).

## 🎮 Características

- ✅ Compilador completo con COCO/R
- ✅ Captura de inputs con XInput (Xbox Controller)
- ✅ Validación temporal (2 segundos entre inputs)
- ✅ Detección de 3 movimientos de Cyrax
- ✅ Interfaz web con SignalR en tiempo real
- ✅ Generación de código intermedio
- ✅ API REST para integración

## 📋 Requisitos

- .NET 8.0 SDK
- Visual Studio 2022 o VS Code
- Control Xbox compatible con XInput
- COCO/R (incluido en `tools/`)

## 🚀 Instalación

### 1. Clonar el repositorio
```bash
git clone https://github.com/tu-usuario/mortal-kombat-compiler.git
cd mortal-kombat-compiler
```

### 2. Restaurar paquetes NuGet
```bash
dotnet restore
```

### 3. Generar el Parser con COCO/R
```bash
cd src/Compiler/Grammar
./generate.bat  # Windows
# o
./generate.sh   # Linux/Mac
```

### 4. Compilar la solución
```bash
dotnet build
```

### 5. Ejecutar la aplicación
```bash
cd src/WebUI/MortalKombatUI
dotnet run
```

Abre tu navegador en: `https://localhost:5001`

## 🎯 Movimientos Implementados

### Fatality 1: Self-Destruct
**Secuencia:** DOWN, DOWN, UP, DOWN, HP

### Fatality 2: Helicopter
**Secuencia:** DOWN, DOWN, FORWARD, UP, RUN

### Brutality
**Secuencia:** HP, LK, HK, HK, LP, LP, HP, LP, LK, HK, LK

## 🕹️ Mapeo de Botones

| Xbox Controller | Comando MK |
|----------------|------------|
| D-Pad Up       | UP         |
| D-Pad Down     | DOWN       |
| D-Pad Left     | LEFT       |
| D-Pad Right    | RIGHT      |
| A Button       | LK         |
| B Button       | HK         |
| X Button       | LP         |
| Y Button       | HP         |
| LB             | RUN        |
| RB             | BLOCK      |

## 📖 Uso del Compilador

### Desde código C#
```csharp
using Compiler;
using Common.Models;

var compiler = new CompilerFacade();

// Desde código fuente
string source = @"
SEQUENCE_START
DOWN T:0
DOWN T:150
UP T:180
DOWN T:200
HP T:175
SEQUENCE_END
";

var result = compiler.CompileFromSource(source);

if (result.Success)
{
    Console.WriteLine($"{result.MoveType}: {result.MoveName}");
    Console.WriteLine(result.IntermediateCode);
}
```

### API REST
```bash
# Compilar código fuente
curl -X POST https://localhost:5001/api/compiler/compile \
  -H "Content-Type: application/json" \
  -d '{"sourceCode": "SEQUENCE_START\nDOWN T:0\n..."}'

# Compilar secuencia de inputs
curl -X POST https://localhost:5001/api/compiler/compile-sequence \
  -H "Content-Type: application/json" \
  -d '[{"command":"DOWN","millisecondsSincePrevious":0}, ...]'
```

## 🏗️ Arquitectura