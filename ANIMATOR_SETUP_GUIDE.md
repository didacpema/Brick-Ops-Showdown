# 🎬 Guía Completa de Configuración del Animator Controller
## Sistema Profesional con Animation Layers

## ⚠️ IMPORTANTE: Sistema de Capas (RECOMENDADO)

Esta guía está actualizada para usar **Animation Layers con Avatar Mask**. Este es el sistema PROFESIONAL que permite:
- ✅ **Aim y Shoot solo afectan brazos y torso** (la parte superior del cuerpo)
- ✅ **Las piernas continúan con su animación** (caminar, correr, saltar)
- ✅ **Resultado:** Puedes caminar mientras apuntas, correr mientras disparas, saltar mientras apuntas
- ✅ **Sin reinicio de animaciones** al cambiar entre estados

Si prefieres el sistema simple (sin capas), ve a la **Sección 10: Sistema Alternativo Sin Capas**.

---

## 📋 Estados y Parámetros

### **Parámetros del Animator:**
- `IsWalking` (Bool) - Indica si el jugador está caminando
- `IsRunning` (Bool) - Indica si el jugador está corriendo
- `IsAiming` (Bool) - Indica si el jugador está apuntando
- `IsGrounded` (Bool) - Indica si el jugador está en el suelo (CRÍTICO para salto)
- `Jump` (Trigger) - Activa la animación de salto
- `Shoot` (Trigger) - Activa la animación de disparo
- `AimBlend` (Float) - **OPCIONAL** - Para blend tree en el aire (0-1)

### **Animation Layers (2 capas):**

#### **Layer 0: Movement (Base Layer)**
Controla el **cuerpo completo** - movimiento de piernas y postura base
- **Mask:** None (cuerpo completo)
- **Weight:** 1.0
- **Blending:** Override

**Estados:**
1. **Idle** - Estado de reposo
2. **Walk** - Caminando normal
3. **Run** - Corriendo (con Shift)
4. **Jump** - Saltando (o "InAir" si usas Blend Tree)

#### **Layer 1: UpperBody**
Controla **solo brazos y torso** - acciones de combate
- **Mask:** UpperBodyMask (solo brazos, manos, cabeza, spine)
- **Weight:** 1.0
- **Blending:** Override

**Estados:**
1. **UpperBody_Idle** - Pose neutral de brazos
2. **UpperBody_Aim** - Apuntando
3. **UpperBody_Shoot** - Disparando

---

## 🎭 PASO 1: Crear Avatar Mask (Upper Body Only)

### **1.1 Crear el Asset**
1. En el **Project Panel**, navega a tu carpeta de Assets
2. Right-click → **Create → Avatar Mask**
3. Nombra el archivo: `UpperBodyMask`

### **1.2 Configurar la Máscara**
1. Selecciona `UpperBodyMask` en el Project
2. En el **Inspector**, verás secciones para Humanoid

### **1.3 Activar Solo Partes Superiores**

**✅ Marcar (Verde = Activo):**
- ✅ **Body** - Spine, Chest
- ✅ **Head** - Cabeza y cuello
- ✅ **Left Arm** - Hombro, brazo, antebrazo, mano izquierda
- ✅ **Right Arm** - Hombro, brazo, antebrazo, mano derecha

**❌ Desmarcar (Rojo = Inactivo):**
- ❌ **Root** (Root Motion)
- ❌ **Left Leg** - Muslo, pierna, pie izquierdo
- ❌ **Right Leg** - Muslo, pierna, pie derecho
- ❌ **IK** (si aparece)

### **Visual Reference:**
```
    ✅ HEAD
       |
    ✅ SPINE ────┬──── ✅ LEFT ARM
                 │
                 └──── ✅ RIGHT ARM
       |
    ❌ ROOT
       |
    ┌──┴──┐
❌ LEFT ❌ RIGHT
   LEG    LEG
```

**⚠️ CRÍTICO:** Si marcas las piernas, las animaciones de aim/shoot afectarán TODO el cuerpo. ¡Asegúrate de que estén DESMARCADAS!

---

## 🔀 PASO 2: Configurar Animation Layers

### **2.1 Abrir Animator Controller**
1. En el Project, localiza tu `PlayerAnimatorController`
2. Double-click para abrirlo
3. Verás la ventana del Animator

### **2.2 Configurar Base Layer (Movement)**

#### **Renombrar (Opcional pero Recomendado):**
1. Click en **"Layers"** (esquina superior izquierda)
2. Click en "Base Layer"
3. Renombra a: `Movement`

#### **Mantener Estados Existentes:**
- ✅ Idle (Armature_Idle o similar)
- ✅ Walk (Armature_Walk)
- ✅ Run (Armature_Run)
- ✅ Jump (Armature_Jump)

#### **Eliminar Estados de Combate:**
- ❌ **ELIMINAR**: Aim state (mover a UpperBody layer)
- ❌ **ELIMINAR**: Shoot state (mover a UpperBody layer)

### **2.3 Crear UpperBody Layer**

#### **Crear la Capa:**
1. En el panel **Layers**, click el botón **"+"**
2. Nombre: `UpperBody`
3. Selecciona la nueva capa

#### **Configurar Settings:**
Click en el ⚙️ (gear icon) de la capa `UpperBody`:

| Setting | Value | Descripción |
|---------|-------|-------------|
| **Weight** | `1.0` | Blend completo (máxima influencia) |
| **Mask** | `UpperBodyMask` | Drag desde Project |
| **Blending** | `Override` | Sobreescribe base layer |
| **Sync** | ❌ Unchecked | NO sincronizar con base |
| **IK Pass** | ✅ Checked | Permite Inverse Kinematics |
| **Timing** | ❌ Unchecked | Timing independiente |

#### **Asignar el Mask:**
1. En el campo **"Mask"**, haz drag & drop de `UpperBodyMask` desde el Project
2. O click en el círculo → Selecciona `UpperBodyMask`

---

## 🏃 PASO 3: Transiciones Movement Layer (Base)

### **Estados en Movement Layer:**
- `Idle` (default state)
- `Walk`
- `Run`
- `Jump`

### **3.1 Entry → Idle**
- **Automática** (transición por defecto)
- Has Exit Time: ✅
- Exit Time: 0
- Duration: 0

### **3.2 Idle ↔️ Walk**

#### **Idle → Walk:**
- **Condición:** `IsWalking = true`
- Has Exit Time: ❌ No
- Duration: 0.15s
- Interruption: Current State

#### **Walk → Idle:**
- **Condición:** `IsWalking = false`
- Has Exit Time: ❌ No
- Duration: 0.15s
- Interruption: Current State

### **3.3 Walk ↔️ Run**

#### **Walk → Run:**
- **Condición:** `IsRunning = true`
- Has Exit Time: ❌ No
- Duration: 0.1s
- Interruption: Current State

#### **Run → Walk:**
- **Condición:** `IsRunning = false`
- Has Exit Time: ❌ No
- Duration: 0.15s
- Interruption: Current State

### **3.4 Any State → Jump**

⚠️ **IMPORTANTE:** Usa **Any State** para permitir saltar desde cualquier estado

- **Condiciones:**
  - Trigger: `Jump`
  - `IsGrounded = false` (evita múltiples saltos)
- Has Exit Time: ❌ No
- Duration: 0.05s (muy rápida)
- Interruption: Current State
- **Can Transition To Self:** ❌ No

### **3.5 Jump → Ground States**

#### **Jump → Idle:**
- **Condición:** `IsGrounded = true` + `IsWalking = false`
- Has Exit Time: ❌ No
- Duration: 0.2s
- Interruption: Next State

#### **Jump → Walk:**
- **Condición:** `IsGrounded = true` + `IsWalking = true` + `IsRunning = false`
- Has Exit Time: ❌ No
- Duration: 0.15s
- Interruption: Next State

#### **Jump → Run:**
- **Condición:** `IsGrounded = true` + `IsRunning = true`
- Has Exit Time: ❌ No
- Duration: 0.15s
- Interruption: Next State

---

## 🎯 PASO 4: Transiciones UpperBody Layer

### **Estados en UpperBody Layer:**

#### **Crear Estados:**
1. Asegúrate de estar en la capa **UpperBody** (selecciónala en Layers)
2. Right-click en el grid → **Create State → Empty**
3. Crea 3 estados:

| Estado | Animation Clip | Loop | Default |
|--------|---------------|------|---------|
| `UpperBody_Idle` | Tu idle upper body o empty | ✅ Yes | ✅ Yes |
| `UpperBody_Aim` | Tu aim animation | ✅ Yes | ❌ No |
| `UpperBody_Shoot` | Tu shoot animation | ❌ No | ❌ No |

⚠️ **Nota sobre animaciones:**
- Si no tienes animaciones específicas de upper body, puedes usar las mismas
- El Avatar Mask se encargará de mostrar solo brazos/torso
- Considera crear poses separadas para mejor resultado

### **4.1 Entry → UpperBody_Idle**
- **Automática** (default state)

### **4.2 UpperBody_Idle ↔️ UpperBody_Aim**

#### **Idle → Aim:**
- **Condición:** `IsAiming = true`
- Has Exit Time: ❌ No
- Exit Time: 0
- Duration: 0.15s (transición suave)
- Interruption: Current State

#### **Aim → Idle:**
- **Condición:** `IsAiming = false`
- Has Exit Time: ❌ No
- Exit Time: 0
- Duration: 0.15s
- Interruption: Current State

### **4.3 UpperBody_Aim → UpperBody_Shoot**

⚠️ **SHOOTING MIENTRAS APUNTA:**

- **Condición:** Trigger `Shoot`
- Has Exit Time: ❌ No
- Duration: 0.05s (muy rápida, casi instantánea)
- Interruption: Current State
- **Can Transition To Self:** ✅ **SÍ** (permite disparos múltiples)

### **4.4 UpperBody_Shoot → UpperBody_Aim**

**Return to aiming after shooting:**

- **Condición:** `IsAiming = true`
- Has Exit Time: ✅ **SÍ** (debe terminar el disparo)
- Exit Time: **0.85 - 0.95** (cerca del final)
- Duration: 0.1s
- Interruption: Next State
- **PRIORIDAD:** Esta debe ser la **PRIMERA** transición desde Shoot

### **4.5 UpperBody_Shoot → UpperBody_Idle**

**Return to idle if not aiming:**

- **Condición:** `IsAiming = false`
- Has Exit Time: ✅ **SÍ**
- Exit Time: **0.85 - 0.95**
- Duration: 0.15s
- Interruption: Next State
- **PRIORIDAD:** Segunda transición desde Shoot

---

## ✅ PASO 5: Verificación y Testing

### **5.1 Checklist de Configuración:**

#### **Avatar Mask:**
- [ ] `UpperBodyMask` creado
- [ ] ✅ Head, Arms, Spine marcados
- [ ] ❌ Root, Legs desmarcados

#### **Layers:**
- [ ] Layer `Movement` existe (o Base Layer)
  - [ ] Weight: 1.0
  - [ ] Mask: None
- [ ] Layer `UpperBody` existe
  - [ ] Weight: 1.0
  - [ ] Mask: UpperBodyMask asignado
  - [ ] Blending: Override

#### **Movement Layer States:**
- [ ] Idle (default)
- [ ] Walk
- [ ] Run
- [ ] Jump
- [ ] Transiciones configuradas correctamente

#### **UpperBody Layer States:**
- [ ] UpperBody_Idle (default)
- [ ] UpperBody_Aim
- [ ] UpperBody_Shoot
- [ ] Transiciones configuradas correctamente

#### **Parameters:**
- [ ] IsWalking (Bool)
- [ ] IsRunning (Bool)
- [ ] IsAiming (Bool)
- [ ] IsGrounded (Bool)
- [ ] Jump (Trigger)
- [ ] Shoot (Trigger)
- [ ] AimBlend (Float) - opcional

### **5.2 Escenarios de Testing:**

#### **Test 1: Caminar + Apuntar**
1. ▶️ Play mode
2. Presiona **W** → El personaje camina
3. Mantén **Click Derecho** → Los brazos apuntan, las piernas siguen caminando
4. ✅ **Esperado:** Piernas = Walk animation, Brazos = Aim pose

#### **Test 2: Correr + Apuntar + Disparar**
1. Presiona **W + Shift** → El personaje corre
2. Mantén **Click Derecho** → Los brazos apuntan (el personaje deja de correr por restricción)
3. ✅ **Esperado:** Personaje camina (no corre), brazos apuntan
4. Click **Click Izquierdo** → Dispara
5. ✅ **Esperado:** Brazos disparan y vuelven a aim, piernas caminando

#### **Test 3: Saltar + Apuntar en el Aire**
1. Presiona **Espacio** → El personaje salta
2. **Mientras está en el aire**, mantén **Click Derecho** → Los brazos apuntan
3. Suelta **Click Derecho** → Los brazos vuelven a idle
4. ✅ **Esperado:** Piernas = Jump animation continua, Brazos = Aim → Idle

#### **Test 4: Saltar + Disparar**
1. Presiona **Espacio** → Salta
2. **En el aire**, mantén **Click Derecho** + click **Click Izquierdo**
3. ✅ **Esperado:** Piernas saltando, brazos disparando

#### **Test 5: Idle + Apuntar + Disparar**
1. Sin moverse, mantén **Click Derecho**
2. Click **Click Izquierdo** varias veces
3. ✅ **Esperado:** Cuerpo completo en idle, brazos disparan repetidamente

### **5.3 Debugging en Runtime:**

#### **Abrir Animator Window en Play Mode:**
1. Con el juego corriendo (▶️)
2. Selecciona tu Player en Hierarchy
3. Abre Window → Animation → Animator
4. Observa:
   - **Base Layer Progress Bar** (movimiento)
   - **UpperBody Layer Progress Bar** (combate)
   - **Parameters Values** en tiempo real

#### **Verificar Layer Blending:**
1. Mientras juegas, observa el **Animator window**
2. Los dos layers deben mostrar estados **simultáneamente**:
   - Movement Layer: Walk/Run/Jump
   - UpperBody Layer: Aim/Shoot
3. **IMPORTANTE:** Si solo ves un layer activo, revisa el Weight de UpperBody

#### **Common Issues:**

**❌ Problema: Todo el cuerpo apunta, las piernas no caminan**
- ✅ Revisa que `UpperBodyMask` tenga las piernas **DESMARCADAS**
- ✅ Verifica que el mask esté **asignado** en UpperBody Layer

**❌ Problema: Los brazos no apuntan**
- ✅ Revisa que UpperBody Layer Weight = **1.0**
- ✅ Verifica que las animaciones de aim estén en **UpperBody Layer**, no en Movement
- ✅ Comprueba que `IsAiming` se está seteando correctamente en código

**❌ Problema: Animaciones se cortan o se ven raras**
- ✅ Asegúrate de que las animaciones de upper body tengan keyframes solo para brazos
- ✅ Si usas animaciones full-body, el mask las filtrará pero puede verse extraño
- ✅ Considera crear animaciones específicas para upper body

---

## 🎬 PASO 6: Comportamientos Esperados

### **Escenario 1: Movimiento Básico (Sin combate)**
1. Player presiona **WASD** → `IsWalking = true` → Transición a **Walk**
2. Player mantiene **Shift** → `IsRunning = true` → Transición a **Run**
3. Player suelta **Shift** → `IsRunning = false` → Transición a **Walk**
4. Player suelta **WASD** → `IsWalking = false` → Transición a **Idle**
5. **UpperBody Layer:** Permanece en `UpperBody_Idle` todo el tiempo

### **Escenario 2: Salto Simple**
1. Player presiona **Espacio** → Trigger `Jump` + `IsGrounded = false`
2. **Movement Layer:** Any State → Transición a **Jump**
3. Player toca el suelo → `IsGrounded = true`
4. **Depende del input:**
   - Si no se mueve → Jump → **Idle**
   - Si presiona WASD → Jump → **Walk**
   - Si presiona WASD+Shift → Jump → **Run**
5. **UpperBody Layer:** Permanece en `UpperBody_Idle`

### **Escenario 3: Caminar + Apuntar**
1. Player presiona **WASD** → **Movement Layer:** Walk
2. Player mantén **Click Derecho** → `IsAiming = true`
   - **UpperBody Layer:** UpperBody_Idle → **UpperBody_Aim**
3. ✨ **Resultado Visual:**
   - **Piernas:** Animación de caminar (Movement Layer)
   - **Brazos:** Pose de apuntar (UpperBody Layer Override)
4. **Zoom de cámara** se aplica (FOV 60 → 40)
5. **RESTRICCIÓN:** Si presiona Shift, NO corre (código previene `isRunning = true`)
6. Player suelta **Click Derecho** → `IsAiming = false`
   - **UpperBody Layer:** UpperBody_Aim → **UpperBody_Idle**

### **Escenario 4: Saltar + Apuntar + Disparar**
1. Player presiona **Espacio** → **Movement Layer:** Jump
2. **En el aire**, mantén **Click Derecho** → `IsAiming = true`
   - **UpperBody Layer:** UpperBody_Idle → **UpperBody_Aim**
3. ✨ **Resultado:**
   - **Piernas:** Continúan animación de salto
   - **Brazos:** Pose de apuntar
4. Player click **Click Izquierdo** → Trigger `Shoot`
   - **UpperBody Layer:** UpperBody_Aim → **UpperBody_Shoot**
5. Animación de disparo termina (Exit Time ~0.9)
   - **UpperBody Layer:** UpperBody_Shoot → **UpperBody_Aim** (IsAiming = true)
6. **CRÍTICO:** La animación de jump **NO se reinicia** porque está en otra layer
7. Player suelta **Click Derecho** → `IsAiming = false`
   - **UpperBody Layer:** UpperBody_Aim → **UpperBody_Idle**
   - **Movement Layer:** Jump continúa hasta aterrizar

### **Escenario 5: Disparar Semi-Automático**
1. Player mantén **Click Derecho** → **UpperBody Layer:** UpperBody_Aim
2. Player click **Click Izquierdo** → Trigger `Shoot`
   - **UpperBody Layer:** UpperBody_Aim → **UpperBody_Shoot**
3. Animación completa (~0.4s cooldown en código)
4. Vuelve a **UpperBody_Aim** (Exit Time 0.9)
5. Player puede volver a disparar (semi-automático, no automático)
6. **WeaponController** maneja el cooldown de 0.4s entre disparos

---

## 🎨 Recomendaciones de Animación

### **Para Movement Layer (Full Body):**
- **Idle:** Animación sutil (respiración, balance)
  - Loop: ✅ Yes
  - Duration: ~2-3 segundos
  
- **Walk:** Ciclo natural de caminar
  - Loop: ✅ Yes
  - Speed: Normal walking pace
  
- **Run:** ~1.5x más rápido que Walk
  - Loop: ✅ Yes
  - Speed: Athletic running
  
- **Jump:** Secuencia completa
  - Loop: ❌ No
  - Fases: Crouch → Launch → Air → Land
  - Duration: ~0.6-1.0 segundos

### **Para UpperBody Layer (Upper Body Only):**
- **UpperBody_Idle:** Brazos neutrales
  - Loop: ✅ Yes
  - Pose: Brazos relajados al costado o sosteniendo arma baja
  
- **UpperBody_Aim:** Pose de apuntar
  - Loop: ✅ Yes
  - Pose: Brazos extendidos, arma al frente
  - Considerar ligero sway para realismo
  
- **UpperBody_Shoot:** Retroceso de disparo
  - Loop: ❌ No
  - Duration: ~0.3-0.4 segundos
  - Frames: Ready → Fire → Recoil → Recovery

### **Consejos Profesionales:**

1. **Authoring Tips:**
   - Crea animaciones de UpperBody con **solo keyframes en brazos/spine**
   - Las piernas deben estar en "reference pose" o sin keyframes
   - Esto evita conflictos con el Avatar Mask

2. **Additive Animations (Avanzado):**
   - Considera usar **Additive Blending** para UpperBody Layer
   - Permite animaciones más naturales que se "suman" a la base
   - Requiere configurar animations como Additive Reference en Import Settings

3. **IK (Inverse Kinematics):**
   - Si usas IK para apuntar (manos siguiendo el arma), activa **IK Pass** en UpperBody Layer
   - Usa `OnAnimatorIK()` en tu script para controlar hand positions

---

## 🐛 Troubleshooting

### **Problema: Animaciones no cambian**
- ✅ Verifica que el Animator esté asignado en el GameObject
- ✅ Comprueba que el Animator Controller esté asignado
- ✅ Revisa que los nombres de parámetros coincidan exactamente
- ✅ Asegúrate de que el código llama correctamente a `SetBool()` y `SetTrigger()`
- ✅ Verifica que ambas layers tengan Weight = 1.0

### **Problema: Todo el cuerpo hace aim, piernas no caminan**
- ✅ **SOLUCIÓN:** Revisa `UpperBodyMask` - las piernas deben estar **DESMARCADAS** (rojas)
- ✅ Asegúrate de que el mask esté **asignado** en UpperBody Layer settings
- ✅ Verifica que Blending = Override (no Additive en este caso)

### **Problema: Los brazos no reaccionan a aim/shoot**
- ✅ Verifica que UpperBody Layer Weight = **1.0**
- ✅ Comprueba que las animaciones aim/shoot estén en **UpperBody Layer**, no Movement
- ✅ Revisa que `IsAiming` y trigger `Shoot` se estén seteando en código
- ✅ Mira el Animator window en play mode para ver los parameter values

### **Problema: Animaciones se ven raras o glitchy**
- ✅ Asegúrate de que las animaciones de upper body tengan **solo keyframes para brazos**
- ✅ Si usas animations full-body, el mask filtrará pero puede causar artifacts
- ✅ Reduce Transition Duration si las transiciones se ven estiradas
- ✅ Verifica que no haya keyframes en Root Motion si no lo usas

### **Problema: Disparos muy rápidos o se saltan**
- ✅ Asegúrate de que **Can Transition To Self** esté ✅ ACTIVADO en UpperBody_Shoot
- ✅ Verifica que el cooldown en código (0.4s) esté funcionando
- ✅ Comprueba que usas `Input.GetMouseButtonDown(0)` (no GetMouseButton)
- ✅ Exit Time en Shoot → Aim debe ser ~0.85-0.95 para completar la animación

### **Problema: No puede disparar mientras salta**
- ✅ Esto es NORMAL con el sistema de layers - debería funcionar
- ✅ Verifica que las dos layers estén activas simultáneamente en Animator window
- ✅ Comprueba que UpperBody Layer no tenga condiciones basadas en IsGrounded

### **Problema: Al disparar mientras camino, vuelve demasiado rápido a Idle**
- ✅ Asegúrate de que la transición **Shoot → Aim** tenga **PRIORIDAD** sobre Shoot → Idle
- ✅ Arrastra la transición Shoot → Aim para que esté **PRIMERO** en la lista
- ✅ Verifica que Exit Time esté cerca de 0.9 (deja terminar la animación)

### **Problema: El personaje corre mientras apunta**
- ✅ Ya está solucionado en código: `isRunning = Input.GetKey(LeftShift) && !isAiming`
- ✅ Al mantener Click Derecho, Shift no activará sprint
- ✅ Esto es intencional para mayor precisión al apuntar

---

## 🗺️ Diagrama del Sistema de Layers

```
╔═══════════════════════════════════════════════════════════════════╗
║                         ANIMATOR CONTROLLER                        ║
╚═══════════════════════════════════════════════════════════════════╝

┌───────────────────────────────────────────────────────────────────┐
│  LAYER 0: MOVEMENT (Base Layer)                                   │
│  Mask: None (Full Body) | Weight: 1.0 | Blending: Override        │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│    ┌─────┐  IsWalking  ┌──────┐  IsRunning  ┌─────┐            │
│    │Idle │ ──────────→ │ Walk │ ──────────→ │ Run │            │
│    └─────┘ ←────────── └──────┘ ←────────── └─────┘            │
│       ↕                                                           │
│   Any State ──[Jump Trigger + !IsGrounded]──→ ┌──────┐          │
│                                                 │ Jump │          │
│                   [IsGrounded=true] ────────────┴──────┘          │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
                               │
                               │ BLENDED WITH
                               ↓
┌───────────────────────────────────────────────────────────────────┐
│  LAYER 1: UPPERBODY                                               │
│  Mask: UpperBodyMask (Arms+Torso) | Weight: 1.0 | Override       │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│    ┌─────────────┐  IsAiming  ┌──────────────┐                  │
│    │UpperBody    │ ─────────→ │UpperBody     │                  │
│    │   _Idle     │ ←───────── │   _Aim       │                  │
│    └─────────────┘            └──────────────┘                   │
│                                       │   ↑                       │
│                                [Shoot]│   │ [Exit Time 0.9]      │
│                                       ↓   │ [IsAiming=true]      │
│                              ┌──────────────┐                     │
│                              │UpperBody     │                     │
│                              │  _Shoot      │                     │
│                              └──────────────┘                     │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════
                        VISUAL RESULT
═══════════════════════════════════════════════════════════════════

Player walking + aiming:

    Movement Layer (Full Body):     UpperBody Layer (Upper Only):
    ┌─────────────────────┐         ┌─────────────────────┐
    │   Walk Animation    │    +    │   Aim Pose          │
    │                     │         │                     │
    │      😐             │         │      🎯             │
    │     ┌┴┐            │         │     ┌┴┐            │
    │    ┌┘ └┐           │         │    ┌┘ └┐ ← OVERRIDE │
    │   /│   │\          │         │   /│🔫│\            │
    │  / │   │ \         │         │  / │   │ \          │
    │    │   │            │         │    │   │            │
    │   ┌┘   └┐           │         │   ┌┘   └┐           │
    │  /       \          │         │  /       \          │
    │ 👟  👟  (WALKING)   │         │ (mask ignores legs) │
    └─────────────────────┘         └─────────────────────┘
              ↓                               ↓
              └────────── BLENDED ────────────┘
                          ↓
              ┌─────────────────────┐
              │   FINAL RESULT:     │
              │     😐 🎯           │
              │    ┌┴┐             │
              │   /│🔫│\           │
              │  / │   │ \         │
              │    │   │            │
              │   ┌┘   └┐           │
              │  /       \          │
              │ 👟  👟  ← Walking! │
              └─────────────────────┘
```

---

## 📊 Comparación: Con Layers vs Sin Layers

| Feature | ❌ Sin Layers (Sistema Simple) | ✅ Con Layers (Sistema Profesional) |
|---------|-------------------------------|-------------------------------------|
| **Caminar + Apuntar** | Todo el cuerpo apunta, no camina | Piernas caminan, brazos apuntan |
| **Saltar + Apuntar** | Animación se reinicia al soltar aim | Jump continúa sin reinicio |
| **Complejidad Setup** | Baja (solo transiciones) | Media (layers + masks) |
| **Calidad Visual** | Básica | Profesional AAA |
| **Animaciones Requeridas** | Full body para todo | Upper body separado (mejor) |
| **Flexibilidad** | Limitada | Alta (fácil añadir gestos) |
| **Performance** | Igual | Igual (insignificante) |

---

## 💡 Notas Importantes y Tips Avanzados

### **🎯 Sobre Avatar Masks:**
- **Humanoid Rigs Only:** Avatar Masks solo funcionan con rigs humanoides
- **Generic Rigs:** Si tu modelo es Generic, necesitas usar Transform Masks (más complejo)
- **Verificación:** En el Inspector del modelo, debe decir "Rig: Humanoid"

### **🔊 Audio Tips:**
- Puedes añadir Animation Events en las animaciones de shoot para sincronizar el sonido
- Coloca el event en el frame exacto del disparo para mejor feedback

### **📐 IK (Inverse Kinematics):**
Si quieres que las manos sigan el arma con precision:
```csharp
void OnAnimatorIK(int layerIndex)
{
    if (layerIndex == 1) // UpperBody layer
    {
        if (isAiming && weaponTransform != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, weaponTransform.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, weaponTransform.rotation);
        }
    }
}
```

### **🎨 Additive Layers (Avanzado):**
Para animaciones más sutiles (respiración, recoil), considera usar **Additive Blending**:
1. Crea una nueva layer: `Additive_Effects`
2. Set Blending: **Additive**
3. Weight: 0.5 - 1.0
4. Añade animaciones con Reference Pose como base

### **⚡ Optimización:**
- Usa `Animator.StringToHash()` para parámetros (ya implementado en InputManager)
- Culling Mode: Always Animate (para multiplayer) o Based On Renderers (single player)
- Evita `SetTrigger()` en Update si no es necesario

### **🎮 Testing Tips:**
1. Usa el **Animator window** en Play Mode para ver layers en tiempo real
2. Activa **Debug Mode** en InputManager para ver logs de parámetros
3. Prueba todas las combinaciones: Walk+Aim, Run+Shoot, Jump+Aim, etc.

---

## 🚀 Próximos Pasos y Mejoras

### **Implementaciones Sugeridas:**

1. **Reload Animation:**
   - Añadir parámetro `Reload` (Trigger)
   - Estado `UpperBody_Reload` en UpperBody Layer
   - Solo brazos recargan, piernas siguen moviéndose

2. **Hit Reaction:**
   - Layer adicional: `Hit_Reaction` (Additive)
   - Animaciones de flinch que se añaden a la animación actual

3. **Melee Attack:**
   - Estado `UpperBody_Melee` en UpperBody Layer
   - Trigger `Melee`

4. **Crouch System:**
   - Parámetro `IsCrouching` (Bool)
   - Estados: Crouch_Idle, Crouch_Walk en Movement Layer
   - Aim funciona igual en UpperBody Layer

5. **Weapon Switching:**
   - Parámetro `WeaponType` (Int)
   - Blend Tree en UpperBody_Idle basado en arma equipada

---

## ✅ Checklist Final

Antes de dar por terminado el setup:

- [ ] ✅ Avatar Mask creado y configurado correctamente
- [ ] ✅ UpperBody Layer creada con mask asignado
- [ ] ✅ Todos los estados de Movement Layer funcionan (Idle, Walk, Run, Jump)
- [ ] ✅ Todos los estados de UpperBody Layer funcionan (Idle, Aim, Shoot)
- [ ] ✅ Transiciones configuradas con settings correctos
- [ ] ✅ Parameters creados en Animator Controller
- [ ] ✅ Código actualizado (InputManager.cs ya incluye todo)
- [ ] ✅ Tested: Walk + Aim funciona (piernas caminan, brazos apuntan)
- [ ] ✅ Tested: Jump + Aim funciona (sin reinicio de animación)
- [ ] ✅ Tested: Shoot semi-automático funciona
- [ ] ✅ Camera zoom funciona al apuntar
- [ ] ✅ Sprint restringido mientras apuntas
- [ ] ✅ Jump cooldown funciona
- [ ] ✅ Ground detection precisa (no activa IsGrounded antes de tiempo)

---

**✨ ¡Con este sistema profesional de Animation Layers, tu juego tendrá animaciones al nivel de títulos AAA!**

**🎯 El código en InputManager.cs ya está 100% preparado para este sistema. Solo necesitas configurar el Animator Controller siguiendo esta guía paso a paso.**

**🚀 Happy Animating!**

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

