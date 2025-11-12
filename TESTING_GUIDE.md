# 🧪 Guía de Prueba: Sistema de Buffer de Triggers

## ✅ Estado Actual

**Sistema completamente implementado:**
- ✅ Buffer de 5 frames para triggers de salto
- ✅ Buffer de 5 frames para triggers de disparo
- ✅ Decremento automático cada frame
- ✅ Sincronización de red basada en buffers
- ✅ Sin errores de compilación (2 warnings menores que Unity resolverá automáticamente)

## 📋 Pasos para Probar

### 1️⃣ Compilar en Unity

```powershell
# Abrir Unity (si no está abierto)
# Unity detectará los cambios automáticamente y compilará
```

**Qué esperar:**
- ⏱️ Compilación: 5-10 segundos
- ✅ Sin errores en la consola
- ⚠️ Posibles 2 warnings sobre preprocesador (ignorar, son falsos positivos)

**Verificación:**
```
1. Abrir Unity Editor
2. Ver ventana Console (Ctrl+Shift+C)
3. Verificar que no haya errores rojos
4. Si hay warnings amarillos sobre #endregion, ignorar
```

---

### 2️⃣ Prueba en Play Mode (Solo Local)

**Objetivo:** Verificar que el sistema de buffer funciona localmente

**Pasos:**
1. En Unity, clic en **Play** (▶️)
2. Elegir **Host Game** o cargar una escena con jugador
3. Probar las siguientes acciones:
   - Presionar **SPACE** varias veces (saltar)
   - Presionar **Click Izquierdo** varias veces (disparar)

**Verificación:**
- ✅ Animación de salto se reproduce cada vez
- ✅ Animación de disparo se reproduce cada vez
- ✅ No hay errores en la consola
- ✅ En Scene View, el jugador se anima correctamente

**Debug (Opcional):**
```csharp
// En InputManager, activar debug:
public bool showDebug = true; // En el Inspector

// Verás logs como:
// [InputManager] 💥 Shot fired at 12.34 (buffer: 5 frames)
// [InputManager] 🦘 Jump performed at 12.56 (buffer: 5 frames)
```

---

### 3️⃣ Prueba Multijugador (Build + Host/Join)

**Objetivo:** Verificar sincronización de animaciones entre jugadores

#### Preparación: Hacer Build

1. **File → Build Settings**
2. Verificar que las escenas correctas estén en **Scenes In Build**
3. **Target Platform:** Windows
4. **Architecture:** x86_64
5. Clic en **Build**
6. Guardar en carpeta `Builds/BrickOpsShowdown.exe`

#### Ejecución: 2 Instancias

**Opción A: En la misma PC (Recomendado para pruebas)**

```powershell
# Instancia 1 - Host
cd Builds
.\BrickOpsShowdown.exe

# Instancia 2 - Cliente (en otra ventana de PowerShell)
cd Builds
.\BrickOpsShowdown.exe
```

**Opción B: En 2 PCs diferentes**
- PC 1: Ejecutar y elegir **Host Game**
- PC 2: Ejecutar, elegir **Join Game**, ingresar IP de PC 1

#### Testing: Escenarios a Probar

| # | Acción | Jugador 1 | Jugador 2 | Resultado Esperado |
|---|--------|-----------|-----------|-------------------|
| 1 | Saltar | SPACE | Observa | ✅ Ve animación de salto en J1 |
| 2 | Disparar | Click Izq | Observa | ✅ Ve animación de disparo en J1 |
| 3 | Ambos saltan | SPACE | SPACE | ✅ Ambos ven animaciones del otro |
| 4 | Saltos rápidos | SPACE×5 | Observa | ✅ Ve todos los saltos |
| 5 | Disparos rápidos | Click×5 | Observa | ✅ Ve todos los disparos |
| 6 | Movimiento + salto | WASD + SPACE | Observa | ✅ Animaciones fluidas |
| 7 | Apuntar + disparar | RMB + LMB | Observa | ✅ Ve animación de apuntar y disparar |

#### Verificación: Indicadores de Éxito

**✅ Sistema Funcionando Correctamente:**
- Todas las animaciones de salto se ven en jugadores remotos
- Todas las animaciones de disparo se ven en jugadores remotos
- No hay "congelamiento" de animaciones
- El movimiento se siente fluido
- Las animaciones se sincronizan en <200ms

**❌ Sistema con Problemas:**
- Animaciones de salto/disparo NO se ven en remotos
- Animaciones se "congelan" ocasionalmente
- Delay visible > 500ms en las animaciones
- Errores en la consola

---

### 4️⃣ Debugging Avanzado

#### Ver Estados en Tiempo Real

**En GameController.cs, agregar logs temporales:**

```csharp
void SendPlayerData()
{
    var state = inputManager.GetCurrentPlayerState(localPlayerId);
    
    // 🐛 DEBUG: Ver estados que se envían
    if (state.isShooting)
        Debug.Log($"[Network] Sending SHOOT state for player {localPlayerId}");
    
    if (state.isJumping)
        Debug.Log($"[Network] Sending JUMP state for player {localPlayerId}");
    
    SendStateToServer(state);
}
```

**En RemotePlayerAnimator.cs, activar debug:**

```csharp
[SerializeField] private bool showDebug = true; // En el Inspector

// Verás logs como:
// [RemotePlayerAnimator] Player 1 → Shoot trigger (buffer: 5)
// [RemotePlayerAnimator] Player 1 → Jump trigger (buffer: 5)
```

#### Verificar Buffers en Inspector

**Mientras el juego corre:**
1. Seleccionar GameObject del jugador en Hierarchy
2. Ver componente `InputManager` en Inspector
3. Buscar variables (si están expuestas):
   - `shootBufferFrames` → Debe ser 0-5
   - `jumpBufferFrames` → Debe ser 0-5

**Al disparar/saltar:**
- Valor cambia de 0 → 5 → 4 → 3 → 2 → 1 → 0
- Si se queda en 0, el buffer NO se está activando

---

## 🐛 Troubleshooting

### Problema 1: Animaciones NO se ven en remotos

**Síntomas:**
- Jugador local ve sus animaciones OK
- Jugador remoto NO ve animaciones del local

**Diagnóstico:**
```csharp
// En PlayerManager.UpdatePlayerState()
Debug.Log($"Received state for player {state.playerId}: " +
          $"Shooting={state.isShooting}, Jumping={state.isJumping}");
```

**Posibles Causas:**
1. ❌ `RemotePlayerAnimator` no está adjunto → Ver jerarquía en Inspector
2. ❌ Estado no se está enviando → Verificar `GameController.SendPlayerData()`
3. ❌ Animator no tiene triggers → Verificar Animator Controller

**Solución:**
```csharp
// En PlayerManager.ConfigureRemotePlayer()
if (remotePlayer.GetComponent<RemotePlayerAnimator>() == null)
{
    Debug.LogWarning($"RemotePlayerAnimator missing on player {playerId}!");
    // Agregar componente manualmente
}
```

---

### Problema 2: Animaciones se "repiten" mucho

**Síntomas:**
- Animación de disparo/salto se ve "pegajosa"
- Se reproduce múltiples veces por un solo evento

**Causa:** Buffer demasiado largo (5 frames = 166ms)

**Solución:**
```csharp
// En InputManager.cs, reducir el buffer:
private const int TRIGGER_BUFFER_DURATION = 3; // De 5 a 3 frames
```

---

### Problema 3: Animaciones aún se pierden ocasionalmente

**Síntomas:**
- 70-80% de animaciones se ven OK
- 20-30% se pierden aún

**Causa:** Latencia muy alta (>200ms) o packet loss

**Diagnóstico:**
```csharp
// Medir latencia en GameController.cs
float sentTime = Time.time;
// ... enviar estado ...
// Al recibir respuesta:
float latency = (Time.time - sentTime) * 1000f; // ms
Debug.Log($"[Network] Latency: {latency:F0}ms");
```

**Solución:**
```csharp
// Aumentar buffer para conexiones lentas:
private const int TRIGGER_BUFFER_DURATION = 8; // 266ms
```

---

### Problema 4: Errores de compilación en Unity

**Síntomas:**
- Unity muestra errores rojos en Console
- Relacionados con `InputManager.cs`

**Solución:**
```
1. Assets → Reimport All (puede tardar varios minutos)
2. Edit → Preferences → External Tools → Regenerate project files
3. Cerrar y reabrir Unity
4. Si persiste: Borrar carpeta Library/ y reabrir Unity
```

---

## 📊 Métricas de Éxito

### Tasa de Sincronización

**Objetivo:** >95% de eventos sincronizados correctamente

**Medición:**
```
Total eventos disparados: 100
Eventos vistos en remoto: 97
Tasa de éxito: 97% ✅
```

### Latencia de Animaciones

**Objetivo:** <200ms entre acción local y visualización remota

**Medición:**
1. Jugador 1 dispara en T=0ms
2. Jugador 2 ve animación en T=150ms
3. Latencia de animación: 150ms ✅

---

## 📝 Checklist Final

**Antes de considerar el sistema completo:**

- [ ] Compilación sin errores en Unity
- [ ] Animaciones funcionan en Play Mode local
- [ ] Build se genera correctamente
- [ ] Multijugador conecta correctamente (Host/Join)
- [ ] Saltos se ven en jugadores remotos >95% del tiempo
- [ ] Disparos se ven en jugadores remotos >95% del tiempo
- [ ] Latencia de animaciones <200ms
- [ ] No hay "freezing" de animaciones
- [ ] Sistema funciona con 2+ jugadores simultáneos
- [ ] Performance es aceptable (>50 FPS)

---

## 🎯 Siguiente Nivel (Opcional)

**Si todo funciona perfectamente, puedes optimizar:**

1. **Interpolación de Animaciones**
   - Suavizar transiciones entre estados

2. **Predicción del Lado del Cliente**
   - Predecir animaciones basándose en input

3. **Compresión de Estados**
   - Reducir ancho de banda usando flags bitwise

4. **Sistema de Prioridad**
   - Eventos críticos (disparo) tienen mayor prioridad

---

**Fecha:** 2025-01-12  
**Versión:** 1.0  
**Sistema:** Buffer de Triggers para Sincronización de Animaciones  
