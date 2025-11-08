import React, { useState, useEffect, useRef } from 'react';
import selfDestructGif from './assets/animations/fatalities/self-destruct.gif';
import helicopterGif from './assets/animations/fatalities/helicopter.gif';
import brutalityGif from './assets/animations/brutalities/cyrax-brutality.gif';
import friendshipGif from './assets/animations/friendship/cyrax-friendship.gif';
import logoImage from './assets/logo/logo.webp';
import mk_Image from './assets/logo/mk3.png';
import errorGif from './assets/animations/error/error.gif';

const CyraxCompiler = () => {
    // Estado de la aplicación
    const [inputSequence, setInputSequence] = useState([]);
    const [currentMove, setCurrentMove] = useState(null);
    const [compilationStatus, setCompilationStatus] = useState('idle');
    const [isAnimating, setIsAnimating] = useState(false);
    const [logs, setLogs] = useState([]);
    const [stats, setStats] = useState({ attempts: 0, success: 0, failed: 0 });
    const [gamepadConnected, setGamepadConnected] = useState(false);
    const [debounceTime, setDebounceTime] = useState(200);
    const [sourceCode, setSourceCode] = useState('');
    const [compilationResult, setCompilationResult] = useState(null);
    const [notifications, setNotifications] = useState([]);
    const [pendingCompilation, setPendingCompilation] = useState(false); // ← NUEVO
    //const [sequenceHistory, setSequenceHistory] = useState([]); // ← NUEVO: para múltiples secuencias
    const [lastCompilationResult, setLastCompilationResult] = useState(null);

    const lastButtonStateRef = useRef({});
    const buttonCooldownRef = useRef({});
    const startTimeRef = useRef(null);


    // Cargar la fuente cuando el componente se monte
    useEffect(() => {
        const link = document.createElement('link');
        link.href = 'https://fonts.googleapis.com/css2?family=Press+Start+2P&display=swap';
        link.rel = 'stylesheet';
        document.head.appendChild(link);
    }, []);

    // Definiciones de movimientos de Cyrax
    const cyraxMoves = {
        fatality1: {
            name: "SELF DESTRUCT",
            type: "FATALITY",
            sequence: ["DOWN", "DOWN", "UP", "DOWN", "HP"],
            color: "from-red-600 to-orange-600",
            description: "Cyrax se autodestruye",
            animation: selfDestructGif,
            duration: 7100
        },
        fatality2: {
            name: "HELICOPTER",
            type: "FATALITY",
            sequence: ["DOWN", "DOWN", "FORWARD", "UP", "RUN"],
            color: "from-purple-600 to-pink-600",
            description: "Cyrax usa sus hélices",
            animation: helicopterGif,
            duration: 8000
        },
        brutality: {
            name: "CYRAX",
            type: "BRUTALITY",
            sequence: ["HP", "LK", "HK", "HK", "LP", "LP", "HP", "LP", "LK", "HK", "LK"],
            color: "from-yellow-600 to-red-600",
            description: "Combinación devastadora de 11 golpes",
            animation: brutalityGif,
            duration: 8700
        },
        friendship: {
            name: "CYRAX",
            type: "FRIENDSHIP",
            sequence: ["RUN", "RUN", "RUN", "UP"],
            color: "from-green-600 to-blue-600",
            description: "Cyrax ofrece una amistad",
            animation: friendshipGif,
            duration: 11000
        },
        error: {
            name: "ERROR",
            type: "COMPILATION FAILED",
            sequence: [],
            color: "from-gray-600 to-black",
            description: "Secuencia inválida o timeout",
            animation: errorGif,
            duration: 4500
        }
    };

    // Mapeo de botones del gamepad
    const buttonMap = {
        12: "UP",
        13: "DOWN",
        14: "LEFT",
        15: "FORWARD",
        2: "LP",
        3: "HP",
        0: "LK",
        1: "HK",
        5: "BL",
        4: "RUN"
    };

    const compileWithCSharp = async (sourceCode) => {
        const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5267';

        console.log('🔧 [3.1] compileWithCSharp INICIADO con:', sourceCode);
        console.log('🌐 Usando API URL:', API_URL);

        try {
            const response = await fetch(`${API_URL}/api/compiler/compile`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ sourceCode })
            });

            console.log('📡 [3.2] Estado de la respuesta:', response.status, response.statusText);

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Error HTTP ${response.status}: ${errorText}`);
            }

            const result = await response.json();
            console.log('✅ [3.3] Resultado recibido del backend:', result);
            return result;

        } catch (error) {
            console.error('❌ [3.4] Error en compileWithCSharp:', error);
            addLog('❌ Error conectando con el compilador: ' + error.message, 'error');
            return {
                success: false,
                errors: ['Error de conexión: ' + error.message]
            };
        }
    };


    const addInput = (command) => {
        if (compilationStatus === 'success' || pendingCompilation) return;

        const now = Date.now();
        const timeSinceLast = startTimeRef.current ? now - startTimeRef.current : 0;

        startTimeRef.current = now;

        const newInput = {
            command,
            timestamp: now,
            millisecondsSincePrevious: timeSinceLast
        };

        const newSequence = [...inputSequence, newInput];
        setInputSequence(newSequence);

        addLog(`Input: ${command} (${timeSinceLast}ms)`, 'info');

        const newSourceCode = generateSourceCode(newSequence);
        setSourceCode(newSourceCode);

        // SOLO verificar prefijos para feedback, NO compilar automáticamente
        checkSequencePrefix(newSequence);
    };

    const generateSourceCode = (sequence) => {
        if (!sequence || sequence.length === 0) return '';

        console.log('📊 Generando código desde secuencia de:', sequence.length, 'comandos');

        const commands = sequence.map((input, index) => {
            if (index === 0) {
                return `${input.command} T:0`;
            }
            return `${input.command} T:${input.millisecondsSincePrevious}`;
        }).join('\n');

        const sourceCode = `SEQUENCE_START\n${commands}\nSEQUENCE_END`;

        const expectedCommands = sequence.length;
        const actualCommands = sourceCode.split('\n').length - 2;

        console.log(`✅ Código generado: ${expectedCommands} comandos esperados, ${actualCommands} en código`);

        return sourceCode;
    };

    const playNotificationSound = (type) => {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            if (type === 'success') {
                oscillator.frequency.value = 523.25;
                gainNode.gain.setValueAtTime(0.2, audioContext.currentTime);
            } else if (type === 'error') {
                oscillator.frequency.value = 392.00;
                gainNode.gain.setValueAtTime(0.15, audioContext.currentTime);
            } else {
                oscillator.frequency.value = 659.25;
                gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
            }

            oscillator.type = 'sine';
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.3);
        } catch (e) {
            console.log('Audio no disponible para notificaciones');
        }
    };

    const addNotification = (message, type = 'info', duration = 3000) => {
        const id = Date.now();
        setNotifications(prev => [...prev, { id, message, type }]);

        if (type === 'success' || type === 'error') {
            playNotificationSound(type);
        }

        setTimeout(() => {
            setNotifications(prev => prev.filter(notif => notif.id !== id));
        }, duration);
    };

    

    const checkSequencePrefix = (sequence) => {
        const commands = sequence.map(inp => inp.command);

        // Solo dar feedback de progreso, NO compilar
        for (const move of Object.values(cyraxMoves)) {
            if (isValidPrefix(commands, move.sequence)) {
                if (commands.length >= 3) {
                    const progress = `${commands.length}/${move.sequence.length}`;
                    addNotification(`📈 ${move.name}: ${progress} completado`, 'info', 1500);
                }
                return;
            }
        }

        // Si no es prefijo válido de ningún movimiento, dar advertencia
        if (commands.length >= 4 && !isValidPrefixForAnyMove(commands)) {
            addNotification('⚠️ Secuencia no reconocida - Usa "Compilar y ejecutar"', 'warning', 2000);
        }
    };

    const isValidPrefixForAnyMove = (commands) => {
        for (const move of Object.values(cyraxMoves)) {
            if (isValidPrefix(commands, move.sequence)) {
                return true;
            }
        }
        return false;
    };

    const resetSequence = () => {
        setInputSequence([]);
        setCurrentMove(null);
        setCompilationStatus('idle');
        setSourceCode('');
        setCompilationResult(null);
        startTimeRef.current = null;
    };

    const getCommandIcon = (cmd) => {
        const icons = {
            UP: "↑", DOWN: "↓", LEFT: "←", RIGHT: "→",
            FORWARD: "→", BACK: "←",
            HP: "🥊", LP: "👊", HK: "🦵", LK: "🦶",
            BL: "🛡️", RUN: "🏃"
        };
        return icons[cmd] || cmd;
    };

    const Icons = {
        Gamepad: () => (<svg className="w-8 h-8" fill="currentColor" viewBox="0 0 24 24"><path d="M6 9a1 1 0 011-1h2V6a1 1 0 112 0v2h2a1 1 0 110 2H11v2a1 1 0 11-2 0V10H7a1 1 0 01-1-1zm13 0a3 3 0 11-6 0 3 3 0 016 0zM7 19a3 3 0 100-6 3 3 0 000 6z" /></svg>),
        Skull: ({ className }) => (
            <img
                src={logoImage}
                className={className}
                alt="Cyrax Logo"
                style={{
                    width: '9rem',
                    height: '8rem',
                    objectFit: 'contain'
                }}
            />
        ),
        mk3Image: ({ className }) => (
            <img
                src={mk_Image}
                className={className}
                alt="Mortal Kombat 3 Logo"
                style={{
                    width: '9rem',
                    height: '8rem',
                    objectFit: 'contain',
                    //paddingrigth: '25rem'
                }}
            />
        ),
        Timer: ({ className }) => (<svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" strokeWidth="2" /><path d="M12 6v6l4 2" strokeWidth="2" strokeLinecap="round" /></svg>),
        Zap: ({ className }) => (<svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M13 2L3 14h8l-1 8 10-12h-8l1-8z" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>),
        CheckCircle: ({ className }) => (<svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" strokeWidth="2" /><path d="M9 12l2 2 4-4" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>),
        XCircle: ({ className }) => (<svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" strokeWidth="2" /><path d="M15 9l-6 6M9 9l6 6" strokeWidth="2" strokeLinecap="round" /></svg>),
        Play: ({ className }) => (<svg className={className} fill="currentColor" viewBox="0 0 24 24"><path d="M8 5v14l11-7z" /></svg>),
        //RotateCcw: ({ className }) => (<svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M1 4v6h6M23 20v-6h-6" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /><path d="M20.49 9A9 9 0 005.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 013.51 15" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>)
    };


    const playSound = () => {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();
            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);
            oscillator.frequency.value = 300;
            oscillator.type = 'square';
            gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);
            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.5);
        } catch (e) {
            console.log('Audio no disponible');
        }
    };

    //const arraysEqual = (a, b) => a.length === b.length && a.every((val, idx) => val === b[idx]);
    const isValidPrefix = (commands, target) => commands.length < target.length && commands.every((cmd, idx) => cmd === target[idx]);
    const addLog = (message, type) => setLogs(prev => [{ message, type, timestamp: new Date().toLocaleTimeString() }, ...prev].slice(0, 15));

    useEffect(() => {
        let animationFrame;
        const pollGamepad = () => {
            const gamepads = navigator.getGamepads();
            const gamepad = gamepads[0] || gamepads[1] || gamepads[2] || gamepads[3];
            if (gamepad && gamepad.connected) {
                if (!gamepadConnected) {
                    setGamepadConnected(true);
                    addLog('🎮 Gamepad conectado: ' + gamepad.id, 'success');
                }
                const now = Date.now();
                gamepad.buttons.forEach((button, index) => {
                    const wasPressed = lastButtonStateRef.current[index];
                    const isPressed = button.pressed;
                    const lastCooldown = buttonCooldownRef.current[index] || 0;
                    if (isPressed && !wasPressed && (now - lastCooldown) > debounceTime) {
                        const command = buttonMap[index];
                        if (command) {
                            addInput(command);
                            buttonCooldownRef.current[index] = now;
                        }
                    }
                    lastButtonStateRef.current[index] = isPressed;
                });
                Object.keys(buttonCooldownRef.current).forEach(key => {
                    if (now - buttonCooldownRef.current[key] > 5000) delete buttonCooldownRef.current[key];
                });
            } else if (gamepadConnected) {
                setGamepadConnected(false);
                addLog('🎮 Gamepad desconectado', 'error');
            }
            animationFrame = requestAnimationFrame(pollGamepad);
        };
        animationFrame = requestAnimationFrame(pollGamepad);
        return () => {
            if (animationFrame) cancelAnimationFrame(animationFrame);
            //if (timeoutRef.current) clearInterval(timeoutRef.current);
        };
    }, [gamepadConnected, inputSequence, compilationStatus, debounceTime]);

    const handleCompileAndExecute = async () => {
        console.log('🚀 [COMPILADOR] Iniciando compilación y ejecución...');

        if (inputSequence.length === 0) {
            addNotification('❌ No hay secuencia para compilar', 'error', 3000);
            return;
        }

        setPendingCompilation(true);
        setCompilationStatus('compiling');

        const finalSourceCode = generateSourceCode(inputSequence);
        console.log('📝 [COMPILADOR] Código fuente generado:', finalSourceCode);

        setSourceCode(finalSourceCode);
        addLog(`📤 Compilando ${inputSequence.length} inputs...`, 'info');
        addNotification('🔧 Compilando secuencia...', 'info', 2000);

        try {
            const result = await compileWithCSharp(finalSourceCode);
            console.log('🎉 [COMPILADOR] Resultado recibido:', result);

            setCompilationResult(result);
            setLastCompilationResult(result); // ← GUARDAR para persistencia

            if (result.success) {
                const move = Object.values(cyraxMoves).find(m =>
                    m.name === result.moveName && m.type === result.moveType
                );

                if (move) {
                    setCurrentMove(move);
                    setCompilationStatus('success');
                    setIsAnimating(true);
                    setStats(prev => ({ ...prev, success: prev.success + 1, attempts: prev.attempts + 1 }));
                    addLog(`✅ ${result.moveType} COMPILADO: ${result.moveName}`, 'success');
                    addNotification(`🎉 ¡${result.moveType} EJECUTADO!`, 'success', 5000);
                    playSound();
                } else {
                    addLog(`⚠️ Movimiento no reconocido: ${result.moveName}`, 'warning');
                    addNotification('🔍 Movimiento no reconocido - Código generado', 'info', 3000);
                    setCompilationStatus('idle');
                }
            } else {
                const errorMove = cyraxMoves.error;
                setCurrentMove(errorMove);
                setCompilationStatus('error');
                setIsAnimating(true);
                setStats(prev => ({ ...prev, failed: prev.failed + 1, attempts: prev.attempts + 1 }));
                addLog(`❌ Error de compilación: ${result.errors?.join(', ')}`, 'error');
                addNotification(`💥 Error de compilación`, 'error', 4000);
                playSound();
            }
        } catch (error) {
            console.error('❌ [COMPILADOR] Error:', error);
            const errorMove = cyraxMoves.error;
            setCurrentMove(errorMove);
            setCompilationStatus('error');
            setIsAnimating(true);
            setStats(prev => ({ ...prev, failed: prev.failed + 1, attempts: prev.attempts + 1 }));
            addLog(`❌ Error de conexión: ${error.message}`, 'error');
            addNotification('💥 Error de conexión con el compilador', 'error', 4000);
            playSound();
        }

        setPendingCompilation(false);
        console.log('🏁 [COMPILADOR] Proceso finalizado');
    };

    useEffect(() => {
        const handleConnect = (e) => {
            setGamepadConnected(true);
            addLog('🎮 Gamepad conectado: ' + e.gamepad.id, 'success');
        };
        const handleDisconnect = () => {
            setGamepadConnected(false);
            addLog('🎮 Gamepad desconectado', 'error');
        };
        window.addEventListener('gamepadconnected', handleConnect);
        window.addEventListener('gamepaddisconnected', handleDisconnect);
        return () => {
            window.removeEventListener('gamepadconnected', handleConnect);
            window.removeEventListener('gamepaddisconnected', handleDisconnect);
        };
    }, []);

    useEffect(() => {
        if (isAnimating && currentMove) {
            console.log(`🎬 Iniciando animación suave para: ${currentMove.name}`);
            const animationDuration = currentMove.duration || 10000;

            const timer = setTimeout(() => {
                console.log(`⏹️ Finalizando animación de ${currentMove.name}`);
                setIsAnimating(false);
                setTimeout(() => {
                    resetSequence();
                }, 800);
            }, animationDuration);

            return () => {
                clearTimeout(timer);
            };
        }
    }, [isAnimating, currentMove]);


    // Estilos SEGA Genesis del index.html
    const segaStyles = {
        container: {
            backgroundImage: "url('https://cdn.pixabay.com/photo/2017/08/30/01/05/milky-way-2695569_1280.jpg')",
            backgroundSize: 'cover',
            backgroundPosition: 'center',
            backgroundAttachment: 'fixed',
            color: '#f1f1f1',
            minHeight: '100vh',
            padding: '1rem'
        },
        header: {
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            //borderBottom: '4px solid #ffd700',
            padding: '1rem',
            marginBottom: '2rem',
            boxShadow: '0 0 60px rgba(255, 215, 0, 0.6)'
        },
        title: {
            textShadow: '3px 3px 0px #ff0000, 6px 6px 0px #000000',
            color: '#ffd700',
            fontSize: '2rem',
            letterSpacing: '2px',
            fontFamily: "'Press Start 2P', cursive",
            textAlign: 'center',
            marginBottom: '1rem',

        },
        subtitle: {
            fontSize: '1rem',
            color: '#00ffff',

            marginBottom: '0.5rem',
            textShadow: '2px 2px 0px #000000',
            fontFamily: "'Press Start 2P', cursive",
        },
        segaBorder: {
            border: '4px solid #0073e6',
            boxShadow: '0 0 0 2px #000, 0 0 0 6px #ff0000, 0 0 10px rgba(0, 0, 0, 0.8)'
        },
        segaButton: {
            background: 'linear-gradient(to bottom, #ff0000, #990000)',
            color: 'white',
            textShadow: '1px 1px 0 #000',
            border: '2px solid #000',
            padding: '0.5rem 1rem',
            fontFamily: "'Press Start 2P', cursive",
            cursor: 'pointer'
        },
        card: {
            backgroundColor: 'rgba(30, 30, 30, 0.9)',
            border: '3px solid #505050',
            padding: '0.5rem',
            cursor: 'pointer',
            transition: 'transform 0.2s, border-color 0.2s',
            position: 'relative',
            overflow: 'hidden'
        },
        inputButton: {
            backgroundColor: '#333',
            color: '#f1f1f1',
            padding: '1rem',
            border: '2px solid #505050',
            cursor: 'pointer',
            fontFamily: "'Press Start 2P', cursive",
            fontSize: '0.7rem',
            textAlign: 'center',
            transition: 'background-color 0.2s'
        },
        footer: {
            marginTop: '2rem',
            textAlign: 'center',
            fontFamily: "'Press Start 2P', cursive",
            fontSize: '0.8rem',
            color: '#888'
        }
    };

    return (
        <div style={segaStyles.container}>
            {/* Header con estilo SEGA */}
            <div className="max-w-7xl mx-auto mb-8" style={segaStyles.header}>
                <div className="flex items-center justify-between mb-4 gap-4">
                    {/* Columna izquierda: Logo */}
                    <div className="flex items-center gap-4 flex-1 justify-start">
                        <Icons.Skull className="w-12 h-12" />
                    </div>

                    {/* Columna central: Título - Centrado */}
                    <div className="flex flex-col items-center justify-center flex-1 text-center">
                        <h1 style={segaStyles.title}>CYRAX COMPILER</h1>
                        <p style={segaStyles.subtitle}>Mortal Kombat 3 Ultimate</p>
                    </div>

                    {/* Columna derecha: Imagen MK3 y estado del gamepad */}
                    <div className="flex items-center gap-4 flex-1 justify-end">
                        <Icons.mk3Image className="mr-4" />
                        <div className={`flex items-center gap-3 px-4 py-2 rounded-lg ${gamepadConnected ? 'bg-green-900/30' : 'bg-red-900/30'}`}>
                            <Icons.Gamepad />
                            <span className="text-sm font-semibold">{gamepadConnected ? 'Conectado' : 'Desconectado'}</span>
                        </div>
                    </div>
                </div>

                {/* Stats con borde SEGA */}
                <div className="grid grid-cols-3 gap-3 md:gap-4" style={segaStyles.segaBorder}>
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-3 md:p-4 border border-gray-700">
                        <div className="text-xl md:text-2xl font-bold text-blue-400">{stats.attempts}</div>
                        <div className="text-xs md:text-sm text-gray-400">Intentos</div>
                    </div>
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-3 md:p-4 border border-gray-700">
                        <div className="text-xl md:text-2xl font-bold text-green-400">{stats.success}</div>
                        <div className="text-xs md:text-sm text-gray-400">Éxitos</div>
                    </div>
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-3 md:p-4 border border-gray-700">
                        <div className="text-xl md:text-2xl font-bold text-red-400">{stats.failed}</div>
                        <div className="text-xs md:text-sm text-gray-400">Fallos</div>
                    </div>
                </div>
            </div>

            <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Main Content */}
                <div className="lg:col-span-2 space-y-6">
                    

                    {/* Input Sequence Display con estilo SEGA */}
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-4 md:p-6" style={segaStyles.segaBorder}>
                        <h2 className="text-lg md:text-xl font-bold mb-4 flex items-center gap-2">
                            <Icons.Play className="w-5 h-5" />
                            Secuencia de Inputs
                        </h2>

                        <div className="flex flex-wrap gap-2 md:gap-3 min-h-[100px] justify-center md:justify-start">
                            {inputSequence.length === 0 ? (
                                <div className="w-full text-center py-8 text-gray-500 text-sm md:text-base">
                                    {gamepadConnected ? 'Presiona los botones...' : 'Conecta un control o usa los botones de prueba'}
                                </div>
                            ) : (
                                inputSequence.map((input, idx) => (
                                    <div key={idx} className="relative" style={segaStyles.card}>
                                        <div className="bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg p-3 md:p-4 shadow-lg transform hover:scale-110 transition-transform">
                                            <div className="text-2xl md:text-3xl">{getCommandIcon(input.command)}</div>
                                            <div className="text-xs mt-1 font-mono">{input.command}</div>
                                        </div>
                                        {idx > 0 && (
                                            <div className="absolute -top-2 -right-2 bg-yellow-500 text-black text-xs px-2 py-1 rounded-full font-bold">
                                                {input.millisecondsSincePrevious}ms
                                            </div>
                                        )}
                                    </div>
                                ))
                            )}
                        </div>

                        {/* BOTÓN COMPILAR Y EJECUTAR - NUEVA POSICIÓN */}
                        {inputSequence.length > 0 && (
                            <div className="mt-6 flex justify-center">
                                <button
                                    onClick={handleCompileAndExecute}
                                    disabled={pendingCompilation}
                                    style={{
                                        ...segaStyles.segaButton,
                                        background: 'linear-gradient(to bottom, #00cc00, #009900)',
                                        padding: '0.75rem 2rem',
                                        fontSize: '1rem'
                                    }}
                                    className="rounded-lg font-bold flex items-center justify-center gap-2 hover:bg-green-700 transition-colors disabled:bg-gray-600 disabled:cursor-not-allowed transform hover:scale-105 transition-transform"
                                >
                                    {pendingCompilation ? (
                                        <>
                                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                                            Compilando...
                                        </>
                                    ) : (
                                        <>
                                            Compilar y Ejecutar
                                        </>
                                    )}
                                </button>
                            </div>
                        )}

                        {/* Status Indicator */}
                        <div className="mt-6 flex items-center justify-center gap-3">
                            {compilationStatus === 'idle' && (
                                <div className="flex items-center gap-2 text-gray-400">
                                    <div className="w-3 h-3 bg-gray-500 rounded-full" />
                                    <span className="text-sm md:text-base">
                                        {inputSequence.length === 0
                                            ? 'Esperando inputs...'
                                            : 'Listo para compilar'
                                        }
                                    </span>
                                </div>
                            )}
                            {compilationStatus === 'compiling' && (
                                <div className="flex items-center gap-2 text-blue-400">
                                    <div className="w-3 h-3 bg-blue-500 rounded-full animate-pulse" />
                                    <span className="text-sm md:text-base">
                                        {inputSequence.length > 0
                                            ? `Compilando... (${inputSequence.length} inputs)`
                                            : 'Compilando...'
                                        }
                                    </span>
                                </div>
                            )}
                            {compilationStatus === 'success' && (
                                <div className="flex items-center gap-2 text-green-400">
                                    <Icons.CheckCircle className="w-5 h-5" />
                                    <span className="font-bold text-sm md:text-base">¡COMPILACIÓN EXITOSA!</span>
                                </div>
                            )}
                            {(compilationStatus === 'error' || compilationStatus === 'timeout') && (
                                <div className="flex items-center gap-2 text-red-400">
                                    <Icons.XCircle className="w-5 h-5" />
                                    <span className="font-bold text-sm md:text-base">
                                        {compilationStatus === 'timeout' ? 'TIMEOUT' : 'ERROR DE COMPILACIÓN'}
                                    </span>
                                </div>
                            )}
                        </div>
                    </div>

                    {/* Código Fuente Generado con estilo SEGA */}
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-4 md:p-6" style={segaStyles.segaBorder}>
                        <h2 className="text-lg md:text-xl font-bold mb-4">Código Fuente Generado</h2>
                        <div className="bg-black rounded-lg p-4 font-mono text-sm overflow-x-auto">
                            {sourceCode || <span className="text-gray-500">// El código fuente aparecerá aquí...</span>}
                        </div>
                    </div>

                    {/* Animation Display con estilo SEGA */}
                    {currentMove && (
                        <div className={`bg-gradient-to-br ${currentMove.color} rounded-lg p-6 md:p-8 border-4 border-yellow-400 shadow-2xl relative transition-all duration-300`} style={segaStyles.segaBorder}>
                            {isAnimating && (
                                <button
                                    onClick={() => {
                                        setIsAnimating(false);
                                        setTimeout(resetSequence, 1000);
                                    }}
                                    className="absolute top-4 right-4 bg-red-600 hover:bg-red-700 text-white p-2 rounded-full z-10 transition-colors shadow-lg"
                                    title="Cerrar animación"
                                >
                                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                    </svg>
                                </button>
                            )}

                            <div className="text-center">
                                <Icons.Zap className="w-12 h-12 md:w-16 md:h-16 mx-auto mb-4 text-yellow-300" />
                                <h2 className="text-3xl md:text-4xl font-bold mb-2 text-white drop-shadow-lg">{currentMove.type}</h2>
                                <h3 className="text-xl md:text-2xl font-semibold mb-2 text-white drop-shadow-lg">{currentMove.name}</h3>
                                <p className="text-sm md:text-base text-gray-200 mb-4 drop-shadow">{currentMove.description}</p>

                                {isAnimating && currentMove.animation && (
                                    <div className="my-6 transition-all duration-500 transform">
                                        <div className="relative inline-block">
                                            <img
                                                src={currentMove.animation}
                                                alt={`${currentMove.name} Animation`}
                                                className="mx-auto rounded-lg border-4 border-yellow-400 shadow-2xl max-w-full max-h-64 md:max-h-96 lg:max-h-120 transition-transform duration-300 hover:scale-105"
                                                onLoad={() => console.log(`GIF cargado: ${currentMove.name}`)}
                                                onError={(e) => {
                                                    console.error(`Error cargando GIF: ${currentMove.name}`);
                                                    e.target.style.display = 'none';
                                                }}
                                            />
                                            <div className="absolute inset-0 rounded-lg border-2 border-yellow-300 opacity-30 animate-pulse-slow pointer-events-none"></div>
                                        </div>

                                        <div className="mt-4 flex flex-col items-center gap-2">
                                            <div className="text-sm text-yellow-200 font-semibold drop-shadow">
                                                 {currentMove.type} en ejecución...
                                            </div>
                                            <div className="text-xs text-gray-300 bg-black bg-opacity-50 px-3 py-1 rounded-full">
                                                La animación se cerrará automáticamente en {currentMove.duration ? currentMove.duration / 1000 : 10} segundos
                                            </div>
                                            <button
                                                onClick={() => {
                                                    setIsAnimating(false);
                                                    setTimeout(resetSequence, 1000);
                                                }}
                                                style={segaStyles.segaButton}
                                                className="mt-2 px-4 py-2 rounded-lg text-sm transition-all duration-200 transform hover:scale-105 shadow-lg"
                                            >
                                                Cerrar ahora
                                            </button>
                                        </div>
                                    </div>
                                )}

                                {isAnimating && !currentMove.animation && (
                                    <div className="my-6">
                                        <div className="text-4xl md:text-6xl mt-6 animate-bounce-slow">
                                            💀☠️💀
                                        </div>
                                        <div className="text-sm text-yellow-300 mt-4 bg-black bg-opacity-50 px-3 py-2 rounded-lg">
                                            Animación no disponible - Mostrando efectos alternativos
                                        </div>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Test Controls con estilo SEGA */}
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-4 md:p-6" style={segaStyles.segaBorder}>
                        <h3 className="font-bold mb-4 text-sm md:text-base">Controles de Prueba</h3>
                        <div className="grid grid-cols-4 gap-2">
                            {Object.values(buttonMap).filter((v, i, a) => a.indexOf(v) === i).map((cmd) => (
                                <button
                                    key={cmd}
                                    onClick={() => addInput(cmd)}
                                    disabled={compilationStatus === 'success' || pendingCompilation}
                                    style={segaStyles.inputButton}
                                    className="hover:bg-gray-600 active:bg-blue-600 disabled:bg-gray-800 disabled:opacity-50 transition-colors"
                                >
                                    {cmd}
                                </button>
                            ))}
                        </div>

                        <button
                            onClick={resetSequence}
                            style={segaStyles.segaButton}
                            className="w-full mt-2 p-2 md:p-3 rounded-lg font-bold flex items-center justify-center gap-2 text-sm md:text-base hover:bg-red-700 transition-colors"
                        >
                            Resetear
                        </button>
                    </div>

                    {/* Resultado de Compilación - PERMANENTE con scroll */}
                  
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-4 md:p-6" style={segaStyles.segaBorder}>
                        <h2 className="text-lg md:text-xl font-bold mb-4">Código Intermedio Generado</h2>

                        {lastCompilationResult ? (
                            < div className={`rounded ${lastCompilationResult.success ? 'bg-green-900/30' : 'bg-red-900/30'}`}>
                        <div className={`p-3 font-bold border-b ${lastCompilationResult.success ? 'border-green-700 text-green-300' : 'border-red-700 text-red-300'}`}>
                            {lastCompilationResult.success ? 'COMPILACIÓN EXITOSA' : 'ERROR DE COMPILACIÓN'}
                        </div>

                        {lastCompilationResult.success && (
                            <div className="p-3 border-b border-green-700">
                                <div className="text-green-300 text-sm mb-1">
                                    <span className="font-semibold">Movimiento:</span> {lastCompilationResult.moveType} - {lastCompilationResult.moveName}
                                </div>
                            </div>
                        )}

                        {!lastCompilationResult.success && lastCompilationResult.errors && (
                            <div className="p-3 border-b border-red-700">
                                <div className="text-red-300 text-sm font-semibold mb-1">Errores encontrados:</div>
                                <ul className="text-red-400 text-xs space-y-1">
                                    {lastCompilationResult.errors.map((error, idx) => (
                                        <li key={idx}>• {error}</li>
                                    ))}
                                </ul>
                            </div>
                        )}

                        {/* Código con scroll */}
                        <div className="p-3 max-h-[400px] overflow-y-auto">
                            <pre className={`text-xs font-mono whitespace-pre-wrap ${lastCompilationResult.success ? 'text-green-200' : 'text-red-200'}`}>
                                {lastCompilationResult.generatedCode}
                            </pre>
                        </div>
                    </div>
                    ) : (
                    <div className="text-gray-500 text-sm text-center py-8 bg-gray-900/30 rounded">
                        El código intermedio aparecerá aquí después de compilar...
                    </div>
    )}
                </div>
                </div>

                {/* Sidebar */}
                <div className="space-y-6">
                    {/* Move Reference con estilo SEGA */}
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-4 md:p-6" style={segaStyles.segaBorder}>
                        <h2 className="text-lg md:text-xl font-bold mb-4">Movimientos de Cyrax</h2>
                        {Object.values(cyraxMoves)
                            .filter(move => move.type !== "COMPILATION FAILED") // Filtra el movimiento de error
                            .map((move, idx) => (
                                <div key={idx} className="mb-4 pb-4 border-b border-gray-700 last:border-0" style={segaStyles.card}>
                                    <div className={`text-xs md:text-sm font-bold mb-1 bg-gradient-to-r ${move.color} bg-clip-text text-transparent`}>
                                        {move.type}
                                    </div>
                                    <div className="font-semibold mb-1 text-sm md:text-base">{move.name}</div>
                                    <div className="text-xs text-gray-400 mb-2">{move.description}</div>
                                    <div className="flex flex-wrap gap-1">
                                        {move.sequence.map((cmd, i) => (
                                            <span key={i} className="bg-gray-700 px-2 py-1 rounded text-xs font-mono">{cmd}</span>
                                        ))}
                                    </div>
                                </div>
                            ))}
                    </div>

                    {/* Compilation Log con estilo SEGA */}
                    <div className="bg-gray-800/50 backdrop-blur rounded-lg p-4 md:p-6" style={segaStyles.segaBorder}>
                        <h2 className="text-lg md:text-xl font-bold mb-4">Log de Compilación</h2>
                        <div className="space-y-2 max-h-[400px] overflow-y-auto">
                            {logs.length === 0 ? (
                                <div className="text-gray-500 text-sm text-center py-4">Sin eventos registrados</div>
                            ) : (
                                logs.map((log, idx) => (
                                    <div key={idx} className={`text-xs md:text-sm p-2 rounded ${log.type === 'success' ? 'bg-green-900/30 text-green-300' : log.type === 'error' ? 'bg-red-900/30 text-red-300' : 'bg-blue-900/30 text-blue-300'}`}>
                                        <span className="text-gray-500 text-xs">{log.timestamp}</span>
                                        <div>{log.message}</div>
                                    </div>
                                ))
                            )}
                        </div>
                    </div>
                </div>
            </div>

            {/* Footer con estilo SEGA */}
            <footer className="footer text-center mt-8 py-4 border-t border-gray-700">
                <p style={segaStyles.footer}>TRABAJO FINAL - COMPILADORES - UNSJ © 2025</p>
            </footer>
        </div>
    );
};

export default CyraxCompiler;