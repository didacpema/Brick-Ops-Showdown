# 🎮 Guía de Configuración del Player Prefab

Esta guía explica cómo configurar correctamente el prefab del jugador con la nueva arquitectura optimizada.

## 📋 Estructura Recomendada del Prefab

```
Player (Prefab Root)
├── [Components]
│   ├── PlayerController        ⭐ NUEVO - Coordina todo
│   ├── Rigidbody               ✅ REQUERIDO
│   ├── CapsuleCollider         ✅ REQUERIDO
│   ├── Animator                ✅ REQUERIDO
│   ├── PlayerHealth            ✅ REQUERIDO
│   ├── WeaponController        ✅ REQUERIDO
│   ├── InputManager            ⚠️ Solo para local
│   ├── RemotePlayerAnimator    ⚠️ Solo para remoto
│   └── Renderer (MeshRenderer) ✅ Para el modelo visual
│
├── Model (GameObject hijo)     [OPCIONAL]
│   ├── MeshFilter
│   └── MeshRenderer
│
└── Camera (GameObject hijo)
    ├── Camera
    ├── AudioListener
    ├── CameraController        ✅ REQUERIDO
    └── CameraShake            ✅ REQUERIDO
```

## ✅ Configuración Paso a Paso

### 1. Crear el GameObject Base

1. **Crear GameObject vacío**: `GameObject` → `Create Empty`
2. **Renombrar** a "Player"
3. **Añadir Tag**: Crear tag "Player" y asignarlo

### 2. Añadir Componentes al Root

**En el Inspector del GameObject "Player":**

#### A) PlayerController (NUEVO) ⭐
```
Add Component → Scripts → PlayerController

[Player Identity]
Player Id: -1 (se asigna automáticamente)
Is Local Player: false (se configura automáticamente)

[Components References]
Input Manager: (arrastrar referencia)
Camera Controller: (arrastrar referencia al hijo Camera)
Remote Animator: (arrastrar referencia)
```

#### B) Rigidbody
```
Add Component → Physics → Rigidbody

Mass: 1
Drag: 0
Angular Drag: 0.05
Use Gravity: ✓
Is Kinematic: ✗ (PlayerController lo maneja)
Interpolate: Interpolate
Collision Detection: Discrete
Constraints: None (PlayerController congela rotaciones X/Z)
```

#### C) CapsuleCollider
```
Add Component → Physics → Capsule Collider

Center: (0, 1, 0)
Radius: 0.5
Height: 2
Direction: Y-Axis
Is Trigger: ✗
Material: None
```

#### D) Animator
```
Add Component → Animation → Animator

Controller: (asignar tu Animator Controller)
Avatar: (asignar tu Avatar)
Apply Root Motion: ✗
Update Mode: Normal
Culling Mode: Always Animate
```

#### E) PlayerHealth
```
Add Component → Scripts → PlayerHealth

[Stats]
Max Health: 100

[UI]
Health Bar: (opcional - arrastrar slider)
Health Bar Canvas: (opcional - arrastrar canvas)

[Visual Feedback]
Damage Material: (opcional)
Damage Flash Duration: 0.1

[Respawn]
Respawn Delay: 3
```

#### F) WeaponController
```
Add Component → Scripts → WeaponController

[Referencias]
Muzzle Point: (arrastrar Transform del cañón)
Player Camera: (se asigna automáticamente)

[Configuración del Arma]
Damage: 25
Range: 100
Fire Rate: 0.15
Spread: 0.02

[Efectos Visuales]
Muzzle Flash Prefab: (arrastrar prefab)
Impact Effect Prefab: (arrastrar prefab)
Bullet Tracer: (opcional)
Tracer Duration: 0.05

[Audio]
Shoot Sound: (arrastrar clip)
Impact Sound: (arrastrar clip)

[Layers]
Hit Layers: Everything (configurar según necesites)
```

#### G) InputManager
```
Add Component → Scripts → InputManager

[Movement]
Walk Speed: 3
Run Speed: 6

[Rotation]
Mouse Sensitivity: 2
Keyboard Rotate Speed: 100

[Jump]
Jump Force: 4
Jump Cooldown: 1
Ground Check Distance: 1.1
Ground Layers: Everything

[Shooting]
Shoot Cooldown: 0.4
```

#### H) RemotePlayerAnimator
```
Add Component → Scripts → RemotePlayerAnimator

(No requiere configuración manual)
```

#### I) MeshRenderer (para el modelo visual)
```
Si el modelo está en el root:
- MeshFilter: (asignar mesh del jugador)
- MeshRenderer: 
  - Materials: (asignar material)
  - Cast Shadows: On
  - Receive Shadows: On

Si usas modelo hijo, este componente va en el hijo.
```

### 3. Configurar GameObject Hijo: Camera

1. **Crear hijo**: Click derecho en "Player" → `Create Empty`
2. **Renombrar** a "Camera"
3. **Posición local**: (0, 1.6, 0) - Altura de ojos

**Añadir componentes:**

#### A) Camera
```
Add Component → Rendering → Camera

Clear Flags: Skybox
Background: (color)
Culling Mask: Everything
Projection: Perspective
Field of View: 60
Clipping Planes: Near 0.3, Far 1000
```

#### B) AudioListener
```
Add Component → Audio → Audio Listener
(Solo uno por escena - PlayerController desactiva en remotos)
```

#### C) CameraController
```
Add Component → Scripts → CameraController

[Target Settings]
Target: (arrastrar el transform del padre "Player")
Offset: (0, 2, -5)

[Movement Settings]
Follow Speed: 0.1
Rotation Speed: 0.1

[Free Look Settings]
Look Sensitivity: 2
Max Vertical Angle: 80
Min Vertical Angle: -60
Free Look Key: Left Alt

[Physics Settings]
Enable Collision: ✓
Collision Radius: 0.3
Collision Layers: Everything (excluir Player layer)

[Zoom Settings]
Normal FOV: 60
Aim FOV: 40
Zoom Speed: 10
```

#### D) CameraShake
```
Add Component → Scripts → CameraShake

[Shake Profiles]
Shoot Shake:
  - Duration: 0.15
  - Magnitude: 0.08
  - Frequency: 30
  - Damping Curve: (curva por defecto)

Impact Shake:
  - Duration: 0.25
  - Magnitude: 0.15
  - Frequency: 20

Explosion Shake:
  - Duration: 0.5
  - Magnitude: 0.3
  - Frequency: 15

[Settings]
Global Intensity: 1
```

### 4. Conectar Referencias en PlayerController

Vuelve al GameObject root "Player" y en el componente **PlayerController**:

```
[Components References]
Input Manager: Arrastrar componente InputManager del mismo GameObject
Camera Controller: Arrastrar componente CameraController del hijo "Camera"
Remote Animator: Arrastrar componente RemotePlayerAnimator del mismo GameObject
```

### 5. Guardar como Prefab

1. Arrastra el GameObject "Player" a la carpeta `Assets/Prefabs/`
2. Renombrar a "PlayerPrefab"
3. Eliminar la instancia de la escena

## 🔧 Configuración en PlayerManager

En el GameObject `PlayerManager` de la escena:

```
[Prefabs]
Player Prefab: (arrastrar el prefab que acabas de crear)

[Spawn Configuration]
Spawn Points: (arrastrar Transforms de spawn)

[Visual Configuration]
Local Player Material: (material para jugador local)
Remote Player Material: (material para jugadores remotos)
Local Player Color: Blue
Remote Player Color: Red
```

## ⚡ Ventajas de Esta Arquitectura

### ✅ Antes (Problemas)
```csharp
// ❌ Componentes añadidos en runtime
health = player.AddComponent<PlayerHealth>();
rb = player.AddComponent<Rigidbody>();

// ❌ Configuración dispersa
rb.constraints = ...;
rb.isKinematic = ...;

// ❌ Difícil de debuggear
```

### ✅ Ahora (Solución)
```csharp
// ✅ Todo está en el prefab
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(Rigidbody))]

// ✅ Configuración centralizada
playerController.InitializeAsLocal(id);
playerController.InitializeAsRemote(id);

// ✅ Fácil de testear
```

## 🎯 Beneficios

1. **No más AddComponent** - Todo está en el prefab
2. **RequireComponent** - Unity garantiza que existen
3. **Configuración visual** - Todo en el Inspector
4. **Fácil testing** - Puedes probar el prefab directamente
5. **Menos código** - PlayerManager mucho más simple
6. **Type-safe** - El compilador verifica las referencias
7. **Mejor performance** - GetComponent en Awake, no en runtime

## 🐛 Troubleshooting

### Error: "PlayerController component missing on prefab!"
**Solución:** Asegúrate de añadir el componente `PlayerController` al root del prefab.

### Error: "RequireComponent of type X has not been added"
**Solución:** Añade el componente faltante al prefab. Unity te dirá cuál falta.

### La cámara no funciona para jugadores locales
**Solución:** Verifica que la referencia `Camera Controller` en `PlayerController` esté asignada.

### Los jugadores remotos no se animan
**Solución:** Verifica que el componente `RemotePlayerAnimator` exista y esté referenciado.

### InputManager no funciona
**Solución:** Verifica que la referencia `Input Manager` en `PlayerController` esté asignada.

## 📊 Comparación

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **Componentes en prefab** | 5 | 9 |
| **AddComponent en código** | 3-4 | 0 |
| **Líneas en PlayerManager** | ~200 | ~80 |
| **Facilidad de setup** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Mantenibilidad** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Debuggeabilidad** | ⭐⭐ | ⭐⭐⭐⭐⭐ |

## 🎨 Opcional: Modelo Visual Separado

Si prefieres tener el modelo 3D como hijo:

```
Player (Root con componentes)
├── Model
│   ├── MeshFilter
│   └── MeshRenderer
└── Camera
    └── ...
```

En este caso, el `Animator` debe estar en el hijo "Model" y el `PlayerController` lo encontrará automáticamente con `GetComponentInChildren`.

## ✨ Próximos Pasos

1. Configurar el prefab según esta guía
2. Asignar el prefab en PlayerManager
3. Probar en escena
4. Ajustar valores según gameplay
