# 🎯 QUICK START: Animation Layers en 5 Minutos

## Paso 1: Crear Avatar Mask (2 min) 🎭

```
1. Project Panel → Right-click
2. Create → Avatar Mask
3. Nombre: "UpperBodyMask"
4. Select it → Inspector:

   ✅ CHECK THESE (Green):
   [✓] Body (Spine, Chest)
   [✓] Head
   [✓] Left Arm (all joints)
   [✓] Right Arm (all joints)

   ❌ UNCHECK THESE (Red):
   [ ] Root
   [ ] Left Leg (all joints)
   [ ] Right Leg (all joints)
   [ ] IK (if present)
```

**Visual Check:**
```
    ✅ HEAD         ← CHECKED
       |
    ✅ ARMS         ← CHECKED
       |
    ❌ LEGS         ← UNCHECKED ⚠️ CRITICAL!
```

---

## Paso 2: Configurar Layers (2 min) 🏗️

```
1. Open Animator Controller (double-click)

2. Layers Panel (top-left):
   
   LAYER 0: "Movement"
   ├─ Mask: None
   ├─ Weight: 1.0
   └─ States: Keep Idle, Walk, Run, Jump
              DELETE: Aim, Shoot (move to Layer 1)

3. Click [+] to add new layer

   LAYER 1: "UpperBody"
   ├─ Mask: UpperBodyMask ← DRAG FROM PROJECT
   ├─ Weight: 1.0
   ├─ Blending: Override
   └─ States: Create 3 new states:
       • UpperBody_Idle (default ⭐)
       • UpperBody_Aim
       • UpperBody_Shoot
```

---

## Paso 3: Transiciones Esenciales (1 min) 🔀

### Movement Layer:
```
Idle ⟷ Walk        (IsWalking = true/false)
Walk ⟷ Run         (IsRunning = true/false)
Any → Jump         (Jump trigger + IsGrounded=false)
Jump → Idle/Walk   (IsGrounded = true)
```

### UpperBody Layer:
```
Idle ⟷ Aim         (IsAiming = true/false)
Aim → Shoot        (Shoot trigger)
Shoot → Aim        (IsAiming=true + Exit Time 0.9)
Shoot → Idle       (IsAiming=false + Exit Time 0.9)
```

**⚠️ IMPORTANT:** Shoot → Aim must be FIRST in list (priority)

---

## Testing Checklist ✅

Play mode → Test estos escenarios:

- [ ] **W** → Camina (piernas)
- [ ] **W + Shift** → Corre (piernas)
- [ ] **W + Click Derecho** → Camina + Brazos apuntan ⭐
- [ ] **Space** → Salta
- [ ] **Space + Click Derecho** → Salta + Brazos apuntan ⭐
- [ ] **Click Derecho + Click Izquierdo** → Dispara (brazos)

**Si los brazos no apuntan:**
→ Check UpperBody Layer Weight = 1.0
→ Check UpperBodyMask está asignado

**Si todo el cuerpo apunta:**
→ Check UpperBodyMask tiene piernas UNCHECKED (rojas)

---

## Common Mistakes 🚫

❌ **Piernas desmarcadas en el mask**
   → TODO el cuerpo apunta, no camina

❌ **Mask no asignado a UpperBody Layer**
   → Brazos no reaccionan

❌ **Weight de UpperBody = 0**
   → Brazos no se mueven

❌ **Estados de Aim/Shoot en Movement Layer**
   → Sistema no funciona, deben estar en UpperBody

---

## Settings Rápidos 📋

### Transitions Settings:
```
Normal transitions (Idle/Walk/Run/Aim):
- Has Exit Time: NO ❌
- Duration: 0.15s
- Interruption: Current State

Jump (Any State → Jump):
- Has Exit Time: NO ❌
- Duration: 0.05s
- Conditions: Jump trigger + IsGrounded=false

Shoot → Aim/Idle:
- Has Exit Time: YES ✅
- Exit Time: 0.85-0.95
- Duration: 0.1s
```

---

## Resultado Esperado 🎬

```
ANTES (Sin Layers):
├─ Caminando → Todo el cuerpo camina
└─ Apuntar → TODO el cuerpo para y apunta

DESPUÉS (Con Layers):
├─ Caminando + Apuntando → Piernas caminan, brazos apuntan
└─ Saltando + Disparando → Piernas saltan, brazos disparan
```

**Visual:**
```
    😐           →        🎯
   /|\                   /|\🔫
    |                     |
   / \                   / \ 
  👟👟                  👟👟
  IDLE               WALK + AIM
                    (Simultáneo!)
```

---

## Debug Tips 🔍

1. **Animator Window en Play Mode:**
   - Window → Animation → Animator
   - Selecciona Player
   - Ve ambos layers activos simultáneamente

2. **Parameters en Runtime:**
   - Observa IsWalking, IsAiming cambiar en tiempo real
   - Triggers deben resetear automáticamente

3. **Layer Progress Bars:**
   - Movement Layer: Verde (siempre activo)
   - UpperBody Layer: Azul (activo cuando aim/shoot)

---

## Si Algo No Funciona... 🆘

**Check en orden:**

1. [ ] UpperBodyMask creado y piernas UNCHECKED
2. [ ] Mask asignado a UpperBody Layer
3. [ ] UpperBody Layer Weight = 1.0
4. [ ] Estados en las layers correctas
5. [ ] Parameters creados en Animator
6. [ ] Transiciones configuradas
7. [ ] InputManager.cs en tu Player GameObject

**Still not working?**
→ Lee ANIMATOR_SETUP_GUIDE.md sección Troubleshooting

---

## Next Steps 🚀

✅ **Sistema básico funcionando**
   → Sigue con ANIMATOR_SETUP_GUIDE.md para optimizar

📚 **Features adicionales:**
   - Reload animation
   - Melee attack
   - Crouch system
   - IK hand positioning
   - Hit reactions

---

**⏱️ Total time: ~5 minutos**
**💪 Resultado: Sistema profesional de animaciones**
**🎮 Listo para jugar!**
