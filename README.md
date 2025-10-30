# Mortal Kombat Compiler (Cyrax Edition)

Un proyecto académico que simula un **compilador interactivo** basado en *Mortal Kombat 3 Ultimate*, desarrollado con **C# (.NET 8)**, **COCO/R**, y un **frontend en React + Vite**.

---

## Estructura del Proyecto

```
MortalKombatCompiler/
├── src/
│   ├── Common/                     # Modelos y constantes compartidas
│   ├── Compiler/                   # Analizador léxico, sintáctico y semántico (Coco/R)
│   ├── InputCapture/               # Módulo de lectura y serialización de inputs
│   ├── MortalKombatCompiler.Backend/
│   │   └── MortalKombatCompiler.API/   # API REST (.NET)
│   └── WebUI/
│       └── MortalKombatUI/
│           ├── ClientApp/          # Frontend React + Vite
│           └── wwwroot/            # Archivos estáticos
```

---

##  Requisitos Previos

Antes de comenzar, asegúrate de tener instalado:

* [Node.js](https://nodejs.org/) (versión 18 o superior)
* [.NET SDK 8.0+](https://dotnet.microsoft.com/en-us/download)
* [Git](https://git-scm.com/)

---

##  Instalación

1️ **Clonar el repositorio desde GitHub**

```bash
git clone https://github.com/LucianoEspejo42/MortalKombatCompiler.git
```

2️ **Abrir la carpeta del proyecto**

```bash
cd MortalKombatCompiler
```

3️ **Restaurar los paquetes de .NET**

```bash
dotnet restore
```

4️ **Instalar dependencias del frontend**

```bash
cd src/WebUI/MortalKombatUI/ClientApp
npm install
```

---

##  Ejecución

### Opción 1: Ejecutar todo desde Visual Studio / Rider

* Abre la solución `MortalKombatCompiler.sln`.
* Establece `MortalKombatUI` como proyecto de inicio.
* Ejecuta (F5).

### Opción 2: Manualmente desde terminal

1️ **Levantar el backend**

```bash
cd src/WebUI/MortalKombatUI
dotnet run
```

2️ **Levantar el frontend**

```bash
cd ClientApp
npm run dev
```

3️ Luego abre en el navegador:

```
http://localhost:5173/
```

---

##  Descripción Técnica

El proyecto se divide en **tres fases principales**:

### 1️ Precompilación

Captura de entradas desde joystick (o simuladas) para generar un código fuente delimitado por `{}`.

### 2️ Compilación

El código fuente pasa por:

* **Scanner** → Análisis léxico (COCO/R)
* **Parser** → Validación sintáctica y semántica
* **Generador de código intermedio** → Traduce combos válidos en comandos simbólicos (`Fatality`, `Brutality`, etc.)

### 3️ Interpretación

El resultado se interpreta en el frontend mostrando animaciones o errores según la secuencia detectada.

---

##  Tecnologías Utilizadas

* **C# / .NET 8**
* **COCO/R**
* **React + Vite**
* **TailwindCSS**
* **SignalR** *(para comunicación en tiempo real, opcional)*
* **XInput** *(para soporte de joystick Xbox)*


