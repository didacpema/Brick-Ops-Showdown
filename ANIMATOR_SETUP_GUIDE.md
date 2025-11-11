# 🎬 Guía Completa de Configuración del Animator Controller

## 📋 Estados y Parámetros

### **Parámetros del Animator:**
- `IsWalking` (Bool) - Indica si el jugador está caminando
- `IsRunning` (Bool) - Indica si el jugador está corriendo
- `IsAiming` (Bool) - Indica si el jugador está apuntando
- `IsGrounded` (Bool) - **NUEVO** - Indica si el jugador está en el suelo (CRÍTICO para salto)
- `Jump` (Trigger) - Activa la animación de salto
- `Shoot` (Trigger) - Activa la animación de disparo

### **Estados del Animator:**
1. **ArmatureIdle** - Estado de reposo (sin movimiento)
2. **ArmatureWalk** - Caminando normal
3. **ArmatureRun** - Corriendo (con Shift)
4. **ArmatureJump** - Saltando
5. **ArmatureShoot** - Disparando
6. **ArmatureAim** - Apuntando

---

## 🔀 Transiciones Detalladas

### **1. Entry → ArmatureIdle**
- **Tipo:** Transición inicial
- **Condiciones:** Ninguna (automática al iniciar)
- **Settings:**
  - Has Exit Time: ✅ Activado
  - Exit Time: 0
  - Transition Duration: 0
  - Interruption Source: None

---

### **2. ArmatureIdle ↔️ ArmatureWalk**

#### **Idle → Walk** (Empezar a caminar)
- **Condiciones:**
  - `IsWalking` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1 - 0.15 (transición suave)
  - Interruption Source: Current State
- **Lógica:** Se activa cuando el jugador presiona WASD

#### **Walk → Idle** (Dejar de caminar)
- **Condiciones:**
  - `IsWalking` is **false**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.15 - 0.2 (más suave al parar)
  - Interruption Source: Current State
- **Lógica:** Se activa cuando el jugador suelta las teclas

---

### **3. ArmatureWalk ↔️ ArmatureRun**

#### **Walk → Run** (Empezar a correr)
- **Condiciones:**
  - `IsRunning` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1 (cambio rápido)
  - Interruption Source: Current State
- **Lógica:** Se activa cuando el jugador presiona Shift mientras camina

#### **Run → Walk** (Dejar de correr)
- **Condiciones:**
  - `IsRunning` is **false**
  - `IsWalking` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.15 (desaceleración natural)
  - Interruption Source: Current State
- **Lógica:** Se activa cuando el jugador suelta Shift pero sigue moviendo

---

### **4. ArmatureRun → ArmatureIdle**
- **Condiciones:**
  - `IsWalking` is **false**
  - `IsRunning` is **false**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.2 (parada más gradual desde carrera)
  - Interruption Source: Current State
- **Lógica:** Se activa cuando el jugador suelta todo desde carrera

---

### **5. Any State → ArmatureJump**

#### **Cualquier Estado → Jump** (Saltar)
- **Condiciones:**
  - `Jump` trigger is **set**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.05 - 0.1 (transición rápida)
  - Interruption Source: None
  - Can Transition To Self: ❌ Desactivado
- **Lógica:** Se activa cuando el jugador presiona Space

#### **Jump → Idle** (Aterrizar a reposo)
- **Condiciones:**
  - `IsGrounded` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado (se controla por IsGrounded)
  - Transition Duration: 0.1 - 0.15
  - Interruption Source: Current State
- **Lógica:** Cuando aterriza y no hay input de movimiento

#### **Jump → Walk** (Aterrizar caminando)
- **Condiciones:**
  - `IsGrounded` is **true**
  - `IsWalking` is **true**
  - `IsRunning` is **false**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1
  - Interruption Source: Current State
- **Lógica:** Cuando aterriza con input de movimiento

#### **Jump → Run** (Aterrizar corriendo)
- **Condiciones:**
  - `IsGrounded` is **true**
  - `IsRunning` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1
  - Interruption Source: Current State
- **Lógica:** Cuando aterriza con Shift presionado

---

### **6. Any State → ArmatureShoot**

#### **Cualquier Estado → Shoot** (Disparar)
- **Condiciones:**
  - `Shoot` trigger is **set**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.05 (muy rápida)
  - Interruption Source: None
  - Can Transition To Self: ✅ Activado (para disparos consecutivos)
- **Lógica:** Se activa cuando el jugador hace click izquierdo

#### **Shoot → Aim** (Terminar disparo apuntando) ⭐ **PRIORIDAD ALTA**
- **Condiciones:**
  - `IsAiming` is **true**
- **Settings:**
  - Has Exit Time: ✅ Activado
  - Exit Time: 0.85 - 0.95 (dejar que termine casi completamente)
  - Transition Duration: 0.05 - 0.1 (rápida pero no instantánea)
  - Interruption Source: None o Next State Then Self
  - **IMPORTANTE:** Esta transición debe estar **ANTES** que las otras en el orden
- **Lógica:** Cuando terminas el disparo pero sigues apuntando
- **Nota:** Esta es la transición más importante para disparar mientras apuntas

#### **Shoot → Idle** (Terminar disparo en reposo)
- **Condiciones:**
  - **Ninguna** (se sale por Exit Time)
- **Settings:**
  - Has Exit Time: ✅ Activado
  - Exit Time: 0.9 - 0.95 (casi al final)
  - Transition Duration: 0.05 - 0.1 (rápida)
  - Interruption Source: Next State
- **Lógica:** Cuando termina el disparo y no hay input

#### **Shoot → Walk** (Terminar disparo caminando)
- **Condiciones:**
  - `IsWalking` is **true**
  - `IsRunning` is **false**
- **Settings:**
  - Has Exit Time: ✅ Activado
  - Exit Time: 0.8 - 0.9
  - Transition Duration: 0.1
  - Interruption Source: Next State
- **Lógica:** Cuando termina el disparo con movimiento

#### **Shoot → Run** (Terminar disparo corriendo)
- **Condiciones:**
  - `IsRunning` is **true**
- **Settings:**
  - Has Exit Time: ✅ Activado
  - Exit Time: 0.8 - 0.9
  - Transition Duration: 0.1
  - Interruption Source: Next State
- **Lógica:** Cuando termina el disparo corriendo

---

### **7. Cualquier Estado ↔️ ArmatureAim**

#### **Idle → Aim** (Empezar a apuntar desde reposo)
- **Condiciones:**
  - `IsAiming` is **true**
  - `IsGrounded` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1 - 0.15
  - Interruption Source: Current State
- **Lógica:** Cuando el jugador presiona click derecho en el suelo

#### **Aim → Idle** (Dejar de apuntar)
- **Condiciones:**
  - `IsAiming` is **false**
  - `IsGrounded` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.15 - 0.2
  - Interruption Source: Current State
- **Lógica:** Cuando el jugador suelta click derecho

#### **Walk → Aim** (Apuntar mientras camina - OPCIONAL)
- **Condiciones:**
  - `IsAiming` is **true**
  - `IsWalking` is **true**
  - `IsGrounded` is **true**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1
  - Interruption Source: Current State
- **Nota:** Solo si quieres permitir apuntar mientras te mueves

#### ⚠️ **IMPORTANTE: NO crear transición Any State → Aim**
- **Razón:** Causaría conflictos con la animación de disparo
- **En su lugar:** Usa transiciones específicas desde estados concretos (Idle, Walk, Run)
- **Excepción:** La transición **Shoot → Aim** debe existir (ver sección 6)

---

### **8. Sistema de Apuntado en el Aire** ⭐ **NUEVO - CRÍTICO**

Este es el sistema para que puedas apuntar mientras saltas sin romper la animación de salto.

⚠️ **ADVERTENCIA: Limitación de Unity Animator**

El problema es que cuando vuelves de Aim a Jump, Unity reinicia la animación de Jump desde el frame 0, no desde donde estaba. Esto es una limitación del sistema de estados del Animator.

**Hay 3 soluciones posibles:**

#### **Solución 1: NO HACER TRANSICIÓN - Apuntar con Blend Tree (RECOMENDADA)** ⭐

En lugar de usar estados separados, usa un **Blend Tree** que mezcle las animaciones:

1. **Crear un Blend Tree "InAir"** en lugar de tener Jump y Aim separados
2. Parámetro Float: `AimBlend` (0 = Jump normal, 1 = Jump + Aim)
3. El código envía el blend:
   ```csharp
   float aimBlend = isAiming ? 1f : 0f;
   animator.SetFloat("AimBlend", aimBlend);
   ```
4. **Ventaja:** La animación NUNCA se reinicia, solo hace blend suave
5. **Desventaja:** Necesitas crear una animación "Jump + Aim" mezclada

#### **Solución 2: Animation Layers (COMPLEJA)**

Usar layers separados en el Animator:

1. **Base Layer:** Contiene las animaciones de movimiento (Idle, Walk, Run, Jump)
2. **Upper Body Layer:** Contiene solo las animaciones de torso (Aim, Shoot)
3. El Upper Body Layer tiene **Weight** que se activa/desactiva
4. **Ventaja:** Profesional, usado en juegos AAA
5. **Desventaja:** Complejo de configurar, requiere máscaras de avatar

#### **Solución 3: Aceptar el Reinicio (SIMPLE)** 

Simplemente acepta que la animación se reinicia y ajusta la animación de Jump:

1. **NO crear transiciones Aim ↔ Jump en el aire**
2. Apuntar en el aire simplemente cambia a Aim (sin volver a Jump)
3. Al aterrizar (`IsGrounded = true`), va directamente a Idle/Walk/Run
4. **Ventaja:** Simple, no requiere cambios complejos
5. **Desventaja:** La animación se reinicia (puede verse raro)

---

### **Implementación de Solución 1 (Blend Tree):** ⭐ **RECOMENDADA**

#### **Paso 1: Crear Blend Tree**

1. En el Animator, elimina el estado **ArmatureJump** (o renómbralo)
2. Crea un nuevo estado: **InAir (Blend Tree)**
3. Doble click en InAir → Selecciona **1D Blend**
4. Parámetro: `AimBlend` (Float, 0 a 1)

#### **Paso 2: Configurar Blend Tree**

Campos del Blend Tree:
- **Threshold 0.0:** ArmatureJump (animación normal de salto)
- **Threshold 1.0:** ArmatureJumpAim (animación de salto apuntando)

Si no tienes animación separada de "Jump + Aim", puedes:
- Duplicar la animación de Aim
- O usar la misma animación de Jump en ambos (solo visual)

#### **Paso 3: Crear transiciones**

- **Any State → InAir:** Condición `Jump` (trigger)
- **InAir → Idle:** Condición `IsGrounded = true` + sin input
- **InAir → Walk:** Condición `IsGrounded = true` + `IsWalking = true`
- **InAir → Run:** Condición `IsGrounded = true` + `IsRunning = true`

#### **Paso 4: Actualizar el código**

Agrega esto al InputManager:

```csharp
// En Animation Parameter Hashes
private static readonly int HashAimBlend = Animator.StringToHash("AimBlend");

// En UpdateAnimations()
// AimBlend: Para blend tree en el aire
if (!isGrounded && animator != null)
{
    float targetBlend = isAiming ? 1f : 0f;
    float currentBlend = animator.GetFloat(HashAimBlend);
    float newBlend = Mathf.Lerp(currentBlend, targetBlend, Time.deltaTime * 10f);
    animator.SetFloat(HashAimBlend, newBlend);
}
else
{
    animator.SetFloat(HashAimBlend, 0f);
}
```

**Resultado:** La animación de salto NUNCA se reinicia, solo hace blend suave entre normal y apuntando.

---

### **Implementación de Solución 3 (Simple):** 

Si prefieres la solución simple:

#### **Jump → Aim** (Apuntar en el aire)
- **Condiciones:**
  - `IsAiming` is **true**
  - `IsGrounded` is **false**
- **Settings:**
  - Has Exit Time: ❌ Desactivado
  - Transition Duration: 0.1
  - Interruption Source: None
- **Nota:** La animación cambiará completamente a Aim

#### **Aim → Idle/Walk/Run** (Aterrizar desde Aim)
- **NO crear transición Aim → Jump**
- En su lugar, crear transiciones directas:
  - **Aim → Idle:** `IsGrounded = true` + no input
  - **Aim → Walk:** `IsGrounded = true` + `IsWalking = true`
  - **Aim → Run:** `IsGrounded = true` + `IsRunning = true`

**Resultado:** Cuando apuntas en el aire, quedas en Aim hasta aterrizar. La animación de Jump no vuelve.

---

## 🎯 Configuración Óptima por Estado

### **ArmatureIdle**
- **Loop:** ✅ Activado
- **Speed:** 1.0

### **ArmatureWalk**
- **Loop:** ✅ Activado
- **Speed:** 1.0 - 1.2 (ajustar según la velocidad de movimiento)

### **ArmatureRun**
- **Loop:** ✅ Activado
- **Speed:** 1.3 - 1.5 (más rápido que Walk)

### **ArmatureJump**
- **Loop:** ❌ Desactivado PERO con configuración especial
- **Speed:** 1.0
- **Duración:** Debe tener 3 fases:
  1. **Anticipación** (frames 0-20%): Preparación para saltar
  2. **Vuelo/Aire** (frames 20-80%): ESTA PARTE debe poder loopearse
  3. **Aterrizaje** (frames 80-100%): Contacto con suelo
- **IMPORTANTE:** Configura un loop parcial en la parte del aire si Unity lo permite, o la animación debe ser lo suficientemente larga para cubrir el tiempo en el aire

### **ArmatureShoot**
- **Loop:** ❌ Desactivado (disparo semi-automático)
- **Speed:** 1.2 - 1.5 (ajustar según cadencia deseada)
- **Duración:** Corta (~0.3-0.5 segundos)

### **ArmatureAim**
- **Loop:** ✅ Activado
- **Speed:** 1.0

---

## ⚙️ Configuraciones Globales del Animator

### **Animator Component Settings:**
- **Apply Root Motion:** ❌ Desactivado (el movimiento lo controla el Rigidbody)
- **Update Mode:** Normal
- **Culling Mode:** Based On Renderers

### **Transiciones Generales:**
- **Default Transition Duration:** 0.1 - 0.15 segundos
- **Default Transition Offset:** 0
- **Interruption Source:** Current State (para permitir cancelar animaciones)

---

## 🔧 Tips de Optimización

1. **Triggers vs Bools:**
   - Usa **Triggers** para acciones instantáneas (Jump, Shoot)
   - Usa **Bools** para estados continuos (IsWalking, IsRunning, IsAiming)

2. **Exit Time:**
   - ❌ Desactiva para transiciones de movimiento (más responsive)
   - ✅ Activa para animaciones que deben completarse (Jump, Shoot)

3. **Transition Duration:**
   - **Corta (0.05-0.1):** Para acciones rápidas (disparar, saltar)
   - **Media (0.1-0.2):** Para cambios de movimiento
   - **Larga (0.2-0.3):** Para transiciones muy suaves (opcional)

4. **Interruption:**
   - **Current State:** Permite cancelar la animación actual
   - **Next State:** Espera a que termine la transición
   - **None:** No se puede interrumpir

---

## 📊 Prioridad de Transiciones

**Orden de prioridad (de mayor a menor):**
1. **Shoot** (Any State) - Más prioritario (puede interrumpir todo)
2. **Jump** (Any State) - Alta prioridad
3. **Shoot → Aim** - CRÍTICO: debe permitir que termine el disparo antes de volver a Aim
4. **Run/Walk** - Prioridad media
5. **Aim** (desde estados específicos, NO desde Any State) - Prioridad baja
6. **Idle** - Por defecto

**⚠️ ORDEN DE TRANSICIONES EN EL ANIMATOR:**
En Unity, el orden de las transiciones importa. Para el estado **ArmatureShoot**, asegúrate de que:
1. **Shoot → Aim** esté PRIMERO en la lista
2. **Shoot → Run** esté después
3. **Shoot → Walk** esté después
4. **Shoot → Idle** esté al final (fallback)

---

## ✅ Checklist de Verificación

- [ ] Todos los parámetros están creados correctamente
- [ ] **IsGrounded** (Bool) está creado y configurado ⭐ **NUEVO**
- [ ] Las transiciones tienen las condiciones correctas
- [ ] Transiciones de salto usan **IsGrounded** en lugar de Exit Time
- [ ] **Jump → Aim** existe con condición `IsGrounded = false`
- [ ] **Aim → Jump** existe con condición `IsGrounded = false` ⭐ **CRÍTICO**
- [ ] Exit Time configurado según el tipo de animación
- [ ] Transition Duration es apropiada para cada caso
- [ ] Can Transition To Self está activado solo en Shoot
- [ ] Interruption Source configurado correctamente
- [ ] Las animaciones de loop están marcadas como loop
- [ ] Apply Root Motion está desactivado
- [ ] Las velocidades de animación están ajustadas

---

## 🎮 Comportamiento Esperado

### **Escenario 1: Movimiento básico**
1. Player presiona W → `IsWalking = true` → Transición a **Walk**
2. Player presiona Shift → `IsRunning = true` → Transición a **Run**
3. Player suelta Shift → `IsRunning = false` → Transición a **Walk**
4. Player suelta W → `IsWalking = false` → Transición a **Idle**

### **Escenario 2: Salto**
1. Player presiona Space → Trigger `Jump` → Transición a **Jump**
2. `IsGrounded` cambia a **false** (está en el aire)
3. Animación de salto se reproduce
4. Al aterrizar, `IsGrounded` cambia a **true**
5. Según el input:
   - Si no hay input → **Idle**
   - Si hay W → **Walk**
   - Si hay W + Shift → **Run**

### **Escenario 2.5: Apuntar en el aire** ⭐ **NUEVO - IMPORTANTE**
1. Player presiona Space → Trigger `Jump` → Transición a **Jump**
2. Mientras está en el aire, presiona click derecho → `IsAiming = true` + `IsGrounded = false`
3. Transición **Jump → Aim** (la animación de Jump sigue corriendo invisiblemente)
4. Player suelta click derecho → `IsAiming = false` + `IsGrounded = false`
5. **CRÍTICO:** Transición **Aim → Jump** (retoma la animación de Jump donde debería estar)
6. La animación de Jump continúa normalmente
7. Al aterrizar (`IsGrounded = true`), transiciona según input (Idle/Walk/Run)

### **Escenario 3: Disparo semi-automático**
1. Player hace click → Trigger `Shoot` → Transición a **Shoot**
2. Animación se reproduce UNA VEZ
3. Vuelve al estado anterior (Idle/Walk/Run/Aim)
4. Player puede hacer otro click para disparar de nuevo

### **Escenario 4: Apuntar y disparar** ⭐ **CASO IMPORTANTE**
1. Player mantiene click derecho → `IsAiming = true` → Transición a **Aim**
2. **IMPORTANTE:** Al apuntar, se aplica un zoom en la cámara (FOV reducido)
3. **RESTRICCIÓN:** No se puede correr mientras apuntas (Shift deshabilitado)
4. Player hace click izquierdo → Trigger `Shoot` → Transición a **Shoot**
5. **CRÍTICO:** La animación de disparo se completa (Exit Time ~0.85-0.95)
6. Como `IsAiming` sigue siendo **true**, transiciona a **Aim** (NO a Idle)
7. Player puede seguir disparando mientras mantiene el apuntado
8. Player suelta click derecho → `IsAiming = false` → Transición a **Idle**
9. El zoom de la cámara vuelve suavemente al FOV normal

### **Escenario 5: Apuntar básico**
1. Player mantiene click derecho → `IsAiming = true` → Transición a **Aim**
2. Cámara hace zoom suave (FOV normal → FOV reducido)
3. Player suelta click derecho → `IsAiming = false` → Transición al estado anterior
4. Cámara vuelve al zoom normal suavemente

---

## 🐛 Troubleshooting

### **Problema: Animaciones no cambian**
- ✅ Verifica que el Animator esté asignado en el GameObject
- ✅ Comprueba que el Animator Controller esté asignado
- ✅ Revisa que los nombres de parámetros coincidan exactamente
- ✅ Asegúrate de que el código llama correctamente a `SetBool()` y `SetTrigger()`

### **Problema: Transiciones muy bruscas**
- ✅ Aumenta el Transition Duration (0.15-0.2)
- ✅ Desactiva Has Exit Time si está activado

### **Problema: Animaciones se interrumpen**
- ✅ Revisa el Interruption Source
- ✅ Activa Has Exit Time para animaciones que deben completarse
- ✅ Ajusta el Exit Time más cerca de 1.0

### **Problema: El disparo es automático en lugar de semi-automático**
- ✅ Usa `Input.GetMouseButtonDown(0)` en lugar de `Input.GetMouseButton(0)`
- ✅ Asegúrate de que **Can Transition To Self** esté activado en Shoot
- ✅ El trigger debe resetear automáticamente

### **Problema: Al disparar mientras apunto, vuelve demasiado rápido a Aim** ⭐
- ✅ **SOLUCIÓN PRINCIPAL:** Crea una transición específica **Shoot → Aim**
- ✅ Configura Exit Time entre 0.85 - 0.95 (deja que termine casi completamente)
- ✅ Condición: `IsAiming` is **true**
- ✅ Has Exit Time: ✅ **ACTIVADO**
- ✅ Transition Duration: 0.05 - 0.1
- ✅ Interruption Source: None o Next State Then Self
- ✅ **IMPORTANTE:** Esta transición debe estar PRIMERO en el orden de transiciones desde Shoot
- ❌ **NO** crear transición Any State → Aim (causa este problema)

### **Problema: Al apuntar en el aire, la animación de salto no continúa al soltar** ⭐ **NUEVO**
- ✅ **SOLUCIÓN:** Crea el parámetro `IsGrounded` (Bool)
- ✅ Actualiza el código para que envíe `IsGrounded` al Animator
- ✅ Crea transición **Jump → Aim** con condiciones: `IsAiming = true` + `IsGrounded = false`
- ✅ **CRÍTICO:** Crea transición **Aim → Jump** con condiciones: `IsAiming = false` + `IsGrounded = false`
- ✅ Has Exit Time: ❌ Desactivado en ambas
- ✅ Transition Duration: 0.05 - 0.1 (muy rápida)
- ✅ Las transiciones de salida de Jump deben usar `IsGrounded = true` en lugar de Exit Time
- ✅ La animación de Jump debe tener frames "lopeables" en la parte del aire (frames del medio)

### **Problema: Puedo correr mientras apunto**
- ✅ Ya está solucionado en el código: `isRunning` solo se activa si `!isAiming`
- ✅ Al mantener click derecho para apuntar, Shift no activará la carrera
- ✅ Esto da más precisión al apuntar y es más realista

### **Problema: No hay zoom al apuntar**
- ✅ Verifica que la cámara esté asignada en el jugador
- ✅ Configura `normalFOV` (60 por defecto) y `aimFOV` (40 por defecto)
- ✅ Ajusta `zoomSpeed` (10 por defecto) para transiciones más rápidas o lentas
- ✅ El zoom se aplica automáticamente al mantener click derecho

### **Problema: No puede saltar múltiples veces**
- ✅ Verifica que el trigger `Jump` se resetee automáticamente
- ✅ Comprueba la detección de suelo (`isGrounded`)
- ✅ Asegúrate de que Exit Time permita volver a otros estados

---

## 🎨 Recomendaciones de Animación

1. **Idle:** Animación sutil (respiración, movimiento ligero)
2. **Walk:** Ciclo natural de caminar, bien lopeada
3. **Run:** ~1.5x más rápido que Walk
4. **Jump:** Anticipación → Vuelo → Aterrizaje (puede ser una sola animación)
5. **Shoot:** Corta y punchy (~0.3-0.5 seg), retroceso visible
6. **Aim:** Pose estática o ligero movimiento

---

## 🗺️ Diagrama de Flujo de Transiciones

```
                    ┌─────────────────────────────────────┐
                    │         Any State                    │
                    │  (Prioridad: Shoot > Jump)          │
                    └─────────────────────────────────────┘
                              │        │
                    ┌─────────┘        └─────────┐
                    │ Shoot (Trigger)   Jump (Trigger)│
                    ▼                              ▼
            ┌───────────────┐              ┌──────────────┐
            │ ArmatureShoot │              │ ArmatureJump │
            │  (NO loop)    │              │  (NO loop)   │
            └───────────────┘              └──────────────┘
                    │                              │
        ┌───────────┼───────────┐                 │
        │           │           │                 │
        │ IsAiming? │ IsWalking?│ IsRunning?     │ Exit Time
        │  (1st)    │  (3rd)    │  (4th)         │ 0.8-0.9
        │           │           │                 │
        ▼           ▼           ▼                 ▼
    ┌────────┐ ┌────────┐ ┌─────────┐     ┌──────────┐
    │  Aim   │ │  Idle  │ │  Walk   │     │   Idle   │
    │(loop)  │ │ (loop) │ │ (loop)  │     │  Walk    │
    └────────┘ └────────┘ └─────────┘     │   Run    │
        │           │           │           └──────────┘
        │           │           │
        │      IsWalking?  IsRunning?
        │           │           │
        │           ▼           ▼
        │      ┌────────┐ ┌─────────┐
        │      │  Walk  │ │   Run   │
        │      │ (loop) │ │ (loop)  │
        │      └────────┘ └─────────┘
        │           │           │
        │           └─────┬─────┘
        │                 │
        └────IsAiming─────┘
```

### **Leyenda del Diagrama:**
- **Any State:** Transiciones que pueden ocurrir desde cualquier estado
- **Orden de prioridad en Shoot:** 1st = Aim, 2nd = Idle, 3rd = Walk, 4th = Run
- **Loop:** Indica que la animación se repite continuamente
- **NO loop:** Animación única que debe completarse
- **Triggers:** Se activan una vez y se resetean automáticamente
- **Bools:** Estados continuos que permanecen hasta cambiar

---

**✨ Con esta configuración, tu personaje debería tener animaciones fluidas y profesionales!**

