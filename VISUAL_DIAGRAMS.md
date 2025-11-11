# 🎨 DIAGRAMA VISUAL DEL SISTEMA

## 📊 Arquitectura Completa

```
╔══════════════════════════════════════════════════════════════════════╗
║                           UNITY PLAYER                                ║
║                                                                       ║
║  ┌────────────────────────────────────────────────────────────────┐ ║
║  │                    GameObject: Player                           │ ║
║  │                                                                  │ ║
║  │  Components:                                                    │ ║
║  │  • Rigidbody (physics)                                         │ ║
║  │  • Animator (animation control)                                │ ║
║  │  • InputManager (✅ YA CONFIGURADO)                            │ ║
║  │  • WeaponController                                            │ ║
║  │  • Camera (child object)                                       │ ║
║  └────────────────────────────────────────────────────────────────┘ ║
╚══════════════════════════════════════════════════════════════════════╝
                                    │
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌────────────────────┐          ┌────────────────────┐
        │   InputManager.cs  │          │   Animator         │
        │   (CÓDIGO LISTO)   │─────────▶│   Controller       │
        │                    │ SetBool()│   (CONFIGURAR)     │
        │  • Detecta Input   │ SetTrig()│                    │
        │  • Mueve Player    │          │   ⚠️ TU TRABAJO    │
        │  • Setea Animator  │          │                    │
        └────────────────────┘          └────────────────────┘
                                                   │
                        ┌──────────────────────────┴──────────────────────────┐
                        │                                                     │
                        ▼                                                     ▼
            ┌───────────────────────┐                         ┌───────────────────────┐
            │ LAYER 0: Movement     │                         │ LAYER 1: UpperBody    │
            │ ─────────────────────│                         │ ─────────────────────│
            │ Mask: None            │                         │ Mask: UpperBodyMask   │
            │ Weight: 1.0           │                         │ Weight: 1.0           │
            │                       │                         │                       │
            │ ┌─────────────────┐  │                         │ ┌─────────────────┐  │
            │ │ States:          │  │                         │ │ States:          │  │
            │ │ • Idle           │  │                         │ │ • UpperBody_Idle │  │
            │ │ • Walk           │  │                         │ │ • UpperBody_Aim  │  │
            │ │ • Run            │  │                         │ │ • UpperBody_Shoot│  │
            │ │ • Jump           │  │                         │ └─────────────────┘  │
            │ └─────────────────┘  │                         │                       │
            │                       │                         │ Controls ONLY:        │
            │ Controls FULL BODY    │                         │ • Arms                │
            └───────────────────────┘                         │ • Hands               │
                        │                                     │ • Head                │
                        │                                     │ • Spine               │
                        │                                     └───────────────────────┘
                        │                                                     │
                        └──────────────────┬──────────────────────────────────┘
                                           │
                                           │ BLENDED OUTPUT
                                           ▼
                    ┌─────────────────────────────────────────┐
                    │       3D MODEL FINAL RESULT             │
                    │                                         │
                    │           😐 (Head)         ← Layer 1  │
                    │          ┌┴┐                           │
                    │    ┌────┤ ├────┐                       │
                    │   🔫   │ │   🔫   ← Arms: Layer 1     │
                    │        │ │         (Aiming/Shooting)   │
                    │        │ │                             │
                    │       ┌┘ └┐        ← Spine: Both      │
                    │      ┌┘   └┐                          │
                    │     /       \       ← Legs: Layer 0   │
                    │    👟       👟      (Walking/Running)  │
                    │                                         │
                    └─────────────────────────────────────────┘
```

---

## 🎭 Avatar Mask Visual

```
┌──────────────────────────────────────────────────────┐
│          AVATAR MASK: UpperBodyMask                   │
│                                                       │
│                  ✅ HEAD                              │
│                     ●                                 │
│                     │                                 │
│              ✅ SPINE/CHEST                           │
│                   ╔═╧═╗                               │
│  ✅ LEFT ARM ════╣   ╠════ RIGHT ARM ✅               │
│    (Shoulder)     ║   ║    (Shoulder)                 │
│    (Elbow)        ║   ║    (Elbow)                    │
│    (Wrist)        ║   ║    (Wrist)                    │
│    (Hand) 👈      ║   ║      👉 (Hand)                │
│                   ║   ║                               │
│              ❌ ROOT ═╩═══                            │
│                     │                                 │
│                   ╔═╧═╗                               │
│     ❌ LEFT LEG ══╣   ╠══ RIGHT LEG ❌                │
│       (Hip)       ║   ║    (Hip)                      │
│       (Knee)      ║   ║    (Knee)                     │
│       (Ankle)     ║   ║    (Ankle)                    │
│       (Foot) 👟   ║   ║    👟 (Foot)                  │
│                                                       │
│   ✅ = CHECKED (Green) - Affected by Layer 1         │
│   ❌ = UNCHECKED (Red) - NOT affected by Layer 1     │
└──────────────────────────────────────────────────────┘
```

---

## 🎬 Animation Flow

### Ejemplo: Caminar + Apuntar + Disparar

```
TIME: ─────────────────────────────────────────────────────▶

INPUT:
  W pressed     ────────────────────────────────────────────
  RMB pressed        ─────────────────────────────────────
  LMB click               ↓

LAYER 0 (Movement - Full Body):
  ┌────┐
  │Idle│──────▶┌────┐───────────────────────────────────────
  └────┘       │Walk│  (Loop)
               └────┘

LAYER 1 (UpperBody - Arms/Torso Only):
  ┌────┐
  │Idle│──────▶┌────┐──────▶┌─────┐──▶┌────┐──────────────
  └────┘       │Aim │       │Shoot│    │Aim │ (Loop)
               └────┘       └─────┘    └────┘
                (Loop)      (Once)      (Loop)

VISUAL RESULT:
  😐           😐            😐🎯          💥🎯
  ┌┴┐         ┌┴┐          ┌┴┐          ┌┴┐
   │           │            │🔫          │🔫
  ╱ ╲         ╱ ╲          ╱ ╲          ╱ ╲
  👟👟        👟👟         👟👟         👟👟
  IDLE        WALK      WALK+AIM    WALK+SHOOT

           (Legs keep   (Legs keep   (Legs STILL
            walking)     walking)      walking!)
```

---

## 🔄 Parameter Flow

```
PLAYER INPUT                INPUTMANAGER                 ANIMATOR
────────────              ───────────────              ───────────

┌──────────┐
│ W Pressed│──────▶ moveInput.y = 1    ─────▶ SetBool(IsWalking, true)
└──────────┘       currentSpeed = walk          │
                                                 ▼
┌──────────┐                               ┌─────────┐
│W + Shift │──────▶ isRunning = true ─────▶│Movement │
└──────────┘       currentSpeed = run       │ Layer   │
                                             │ Idle    │
                                             │  ↓      │
┌──────────┐                                │ Walk    │
│  Space   │──────▶ rb.AddForce(jump) ─────▶│  ↓      │
└──────────┘       SetTrigger(Jump)         │ Run     │
                                             │  ↓      │
                                             │ Jump    │
┌──────────┐                                └─────────┘
│   RMB    │──────▶ isAiming = true    ─────▶┌─────────┐
└──────────┘       camera.FOV → aimFOV       │UpperBody│
                                             │  Layer  │
                                             │         │
┌──────────┐                                │ Idle    │
│   LMB    │──────▶ SetTrigger(Shoot)  ─────▶│  ↓      │
└──────────┘       weaponController.Fire()   │ Aim     │
                                             │  ↓      │
                                             │ Shoot   │
                                             └─────────┘

                    BOTH LAYERS ACTIVE SIMULTANEOUSLY!
```

---

## 📐 Layer Blending Visualization

```
┌────────────────────────────────────────────────────────────┐
│                    BLENDING PROCESS                         │
└────────────────────────────────────────────────────────────┘

STEP 1: Movement Layer (Weight 1.0) renders FULL BODY
┌─────────────────┐
│   Walk Anim     │ → Whole body walking animation
│   Frame 45/120  │    (includes arms in walking pose)
└─────────────────┘
         ↓
    [3D Model]
      😐 ← Head walking
     ┌┴┐
    / │ \ ← Arms swinging (from walk anim)
      │
     ╱ ╲ ← Legs stepping (from walk anim)
    👟 👟


STEP 2: UpperBody Layer (Weight 1.0) OVERRIDES upper body
┌─────────────────┐
│   Aim Anim      │ → Only upper body (mask filters)
│   Frame 10/60   │    (arms in aiming position)
└─────────────────┘
         ↓
    [3D Model]
      😐 ← Head aiming (OVERRIDE)
     ┌┴┐
    /│🔫│\ ← Arms aiming (OVERRIDE)
      │
     ╱ ╲ ← Legs stepping (unchanged from Layer 0)
    👟 👟


FINAL RESULT: Combination of both layers
    [3D Model]
      🎯 ← Head (Layer 1 - Aim)
     ┌┴┐
    /│🔫│\ ← Arms (Layer 1 - Aim)
      │   
     ╱ ╲ ← Legs (Layer 0 - Walk)
    👟 👟

    = Character walking with arms aiming!
```

---

## 🎯 State Machine Diagrams

### Layer 0: Movement (Full Body Control)

```
                    ┌─────────────────────────┐
                    │      Any State          │
                    └─────────────────────────┘
                              │
                  ┌───────────┴───────────┐
                  │ Jump trigger          │
                  │ + IsGrounded=false    │
                  └───────────┬───────────┘
                              ↓
        ┌─────────────────────────────────────┐
        │             JUMP                     │
        │         (no loop)                    │
        └─────────────────────────────────────┘
                 │        │         │
      ┌──────────┤        │         └─────────────┐
      │IsGrounded=true    │IsGrounded=true        │IsGrounded=true
      │IsWalking=F        │IsWalking=T            │IsRunning=T
      │                   │IsRunning=F            │
      ▼                   ▼                       ▼
┌──────────┐       ┌──────────┐           ┌──────────┐
│   IDLE   │◀─────▶│   WALK   │◀─────────▶│   RUN    │
│  (loop)  │IsWalk │  (loop)  │ IsRunning │  (loop)  │
└──────────┘       └──────────┘           └──────────┘
     ▲                                           │
     └───────────────────────────────────────────┘
                    !IsRunning
```

### Layer 1: UpperBody (Arms/Torso Control)

```
┌─────────────────┐
│UpperBody_Idle   │
│    (default)    │◀──┐
│     (loop)      │   │
└─────────────────┘   │
        │   ▲         │ !IsAiming
IsAiming│   │!IsAiming│ ExitTime 0.9
        ↓   │         │
┌─────────────────┐   │
│UpperBody_Aim    │   │
│     (loop)      │   │
└─────────────────┘   │
        │   ▲         │
  Shoot │   │IsAiming │
 trigger│   │ExitTime │
        ↓   │  0.9    │
┌─────────────────┐   │
│UpperBody_Shoot  │───┘
│   (no loop)     │
└─────────────────┘
     │
     │ Can Transition To Self: YES
     └─────────────────────────────┐
                                   │
                                   ▼
                        (Allows semi-auto shooting)
```

---

## 🎮 User Experience Flow

```
PLAYER ACTION                    SYSTEM RESPONSE
─────────────                   ────────────────

1. Game starts
   😐 Standing still
   
   ↓
   
2. Press W
   InputManager: IsWalking = true
   Layer 0: Idle → Walk
   😐 → 🚶
   👟👟 start walking
   
   ↓
   
3. Hold RMB (aim)
   InputManager: IsAiming = true
   Camera: FOV 60 → 40 (zoom)
   Layer 1: Idle → Aim
   🚶 → 🚶🎯
   👟👟 still walking
   🔫 arms aim
   
   ↓
   
4. Click LMB (shoot)
   InputManager: Shoot trigger
   WeaponController: Fire()
   Layer 1: Aim → Shoot → Aim
   🚶🎯 → 🚶💥 → 🚶🎯
   👟👟 STILL walking!
   
   ↓
   
5. Release RMB
   InputManager: IsAiming = false
   Camera: FOV 40 → 60 (unzoom)
   Layer 1: Aim → Idle
   🚶🎯 → 🚶
   👟👟 keep walking
   
   ↓
   
6. Press Space (jump)
   InputManager: Jump trigger, IsGrounded = false
   Physics: AddForce upward
   Layer 0: Walk → Jump
   🚶 → 🦘
   👟 off ground!
   
   ↓
   
7. In air, hold RMB + click LMB
   InputManager: IsAiming = true, Shoot trigger
   Layer 0: Still in Jump
   Layer 1: Idle → Aim → Shoot → Aim
   🦘 → 🦘🎯 → 🦘💥
   👟 in air, still jumping
   🔫 shooting in air!
   
   ↓
   
8. Land on ground
   InputManager: IsGrounded = true
   Layer 0: Jump → Walk (still holding W)
   🦘💥 → 🚶🎯
   👟👟 walking again
   
   ↓
   
9. Release all inputs
   InputManager: All bools = false
   Layer 0: Walk → Idle
   Layer 1: Aim → Idle
   🚶🎯 → 😐
   Back to standing
```

---

## 🔍 Debugging View

```
ANIMATOR WINDOW (RUNTIME VIEW):
┌─────────────────────────────────────────────────────────┐
│ Layers:                                                  │
│                                                          │
│ ▼ Movement (Weight: 1.0)                                │
│   ├─ Idle         ░░░░░░░░░░░░░░░░░░░░░░░░ 0%          │
│   ├─ Walk         ████████████████████████ 100% ← ACTIVE│
│   ├─ Run          ░░░░░░░░░░░░░░░░░░░░░░░░ 0%          │
│   └─ Jump         ░░░░░░░░░░░░░░░░░░░░░░░░ 0%          │
│                                                          │
│ ▼ UpperBody (Weight: 1.0) [Mask: UpperBodyMask]        │
│   ├─ UpperBody_Idle  ░░░░░░░░░░░░░░░░░░░░ 0%           │
│   ├─ UpperBody_Aim   ████████████████████ 100% ← ACTIVE│
│   └─ UpperBody_Shoot ░░░░░░░░░░░░░░░░░░░░ 0%           │
│                                                          │
│ Parameters:                                              │
│   IsWalking:  ✅ true                                    │
│   IsRunning:  ❌ false                                   │
│   IsAiming:   ✅ true    ← Both true = Walk + Aim!      │
│   IsGrounded: ✅ true                                    │
│   Jump:       ⚪ (trigger reset)                         │
│   Shoot:      ⚪ (trigger reset)                         │
│   AimBlend:   0.0                                        │
└─────────────────────────────────────────────────────────┘

RESULT: Character walking (legs) + aiming (arms)
```

---

## ✅ Success Indicators

```
✅ WORKING CORRECTLY:

1. Walk + Aim:
   Layer 0: Walk at 100%
   Layer 1: Aim at 100%
   Result: Legs animate, arms static in aim pose
   
2. Jump + Shoot:
   Layer 0: Jump progressing (40% → 60% → 80%)
   Layer 1: Shoot → Aim transition
   Result: Legs in air, arms shooting
   
3. No animation restart:
   Layer 0: Jump at 65%
   Press RMB: Layer 1 activates Aim
   Release RMB: Layer 1 back to Idle
   Layer 0: Still at 65% (NOT reset to 0%!)
   
4. Smooth transitions:
   All transitions show blend phase (0.15s)
   No sudden pops or jumps
   
❌ NOT WORKING:

1. Whole body aims:
   → Check: Legs UNCHECKED in UpperBodyMask
   
2. Arms don't react:
   → Check: UpperBody Layer Weight = 1.0
   → Check: Mask assigned to layer
   
3. Jump restarts when aiming:
   → This is EXPECTED with old system
   → With layers, it should NOT restart
```

---

**📖 Usa estos diagramas como referencia mientras configuras tu Animator!**
**🎯 Objetivo: Ambos layers activos simultáneamente = Walk + Aim working together!**
