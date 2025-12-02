using UnityEngine;
using UnityEditor;
using BrickOps.Players;

namespace BrickOps.Editor
{
    /// <summary>
    /// Herramienta de editor para configurar automáticamente el Player Prefab
    /// Menú: Tools → Brick Ops → Setup Player Prefab
    /// </summary>
    public class PlayerPrefabSetup : EditorWindow
    {
        private GameObject playerPrefab;
        private GameObject cameraObject;

        [MenuItem("Tools/Brick Ops/Setup Player Prefab")]
        public static void ShowWindow()
        {
            GetWindow<PlayerPrefabSetup>("Player Prefab Setup");
        }

        void OnGUI()
        {
            GUILayout.Label("Player Prefab Auto-Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            playerPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Player Prefab", 
                playerPrefab, 
                typeof(GameObject), 
                true
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Setup Prefab", GUILayout.Height(40)))
            {
                if (playerPrefab != null)
                {
                    SetupPrefab();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a Player Prefab first!", "OK");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Este wizard configurará automáticamente:\n" +
                "• PlayerController\n" +
                "• Todos los componentes requeridos\n" +
                "• CameraController y CameraShake\n" +
                "• Referencias entre componentes", 
                MessageType.Info
            );
        }

        void SetupPrefab()
        {
            // Verificar que es un prefab o instancia
            if (PrefabUtility.IsPartOfPrefabAsset(playerPrefab) || 
                PrefabUtility.IsPartOfPrefabInstance(playerPrefab))
            {
                // Si es prefab asset, necesitamos trabajar con una instancia
                string path = AssetDatabase.GetAssetPath(playerPrefab);
                GameObject instance = PrefabUtility.LoadPrefabContents(path);
                
                SetupComponents(instance);
                
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                PrefabUtility.UnloadPrefabContents(instance);
                
                EditorUtility.DisplayDialog("Success", 
                    "Player Prefab configured successfully!\n\n" +
                    "Next steps:\n" +
                    "1. Check the prefab in Inspector\n" +
                    "2. Assign references in PlayerController\n" +
                    "3. Configure component values as needed", 
                    "OK");
            }
            else
            {
                // Es una instancia en escena
                SetupComponents(playerPrefab);
                
                EditorUtility.DisplayDialog("Success", 
                    "Player GameObject configured successfully!\n\n" +
                    "Don't forget to save as prefab!", 
                    "OK");
            }
        }

        void SetupComponents(GameObject player)
        {
            // 1. PlayerController (el orquestador)
            if (player.GetComponent<PlayerController>() == null)
            {
                player.AddComponent<PlayerController>();
                Debug.Log("✓ Added PlayerController");
            }

            // 2. Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody>();
                Debug.Log("✓ Added Rigidbody");
            }
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // 3. CapsuleCollider
            CapsuleCollider collider = player.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = player.AddComponent<CapsuleCollider>();
                Debug.Log("✓ Added CapsuleCollider");
            }
            collider.center = new Vector3(0, 1, 0);
            collider.radius = 0.5f;
            collider.height = 2f;
            collider.direction = 1; // Y-axis

            // 4. Animator (si no existe, advertir)
            if (player.GetComponent<Animator>() == null)
            {
                Debug.LogWarning("⚠ Animator not found - please add manually and assign controller");
            }

            // 5. PlayerHealth
            if (player.GetComponent<PlayerHealth>() == null)
            {
                PlayerHealth health = player.AddComponent<PlayerHealth>();
                health.maxHealth = 100f;
                health.respawnDelay = 3f;
                Debug.Log("✓ Added PlayerHealth");
            }

            // 6. WeaponController
            if (player.GetComponent<WeaponController>() == null)
            {
                WeaponController weapon = player.AddComponent<WeaponController>();
                weapon.bodyDamage = 25f;
                weapon.headDamage = 75f;
                weapon.range = 100f;
                weapon.fireRate = 0.15f;
                weapon.standingSpread = 0.2f;
                weapon.standingAimSpread = 0.005f;
                weapon.walkingSpread = 0.04f;
                weapon.walkingAimSpread = 0.015f;

                Debug.Log("✓ Added WeaponController");
            }

            // 7. InputManager
            if (player.GetComponent<InputManager>() == null)
            {
                InputManager input = player.AddComponent<InputManager>();
                input.walkSpeed = 3f;
                input.runSpeed = 6f;
                input.mouseSensitivity = 2f;
                input.jumpForce = 4f;
                input.jumpCooldown = 1f;
                input.groundCheckDistance = 1.1f;
                input.shootCooldown = 0.4f;
                Debug.Log("✓ Added InputManager");
            }

            // 8. RemotePlayerAnimator
            if (player.GetComponent<RemotePlayerAnimator>() == null)
            {
                player.AddComponent<RemotePlayerAnimator>();
                Debug.Log("✓ Added RemotePlayerAnimator");
            }

            // 9. Setup Camera hijo
            Transform cameraTransform = player.transform.Find("Camera");
            if (cameraTransform == null)
            {
                GameObject camera = new GameObject("Camera");
                camera.transform.SetParent(player.transform);
                camera.transform.localPosition = new Vector3(0, 1.6f, 0);
                cameraTransform = camera.transform;
                Debug.Log("✓ Created Camera GameObject");
            }

            GameObject cameraObj = cameraTransform.gameObject;

            // Camera component
            Camera cam = cameraObj.GetComponent<Camera>();
            if (cam == null)
            {
                cam = cameraObj.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                Debug.Log("✓ Added Camera component");
            }

            // AudioListener
            if (cameraObj.GetComponent<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
                Debug.Log("✓ Added AudioListener");
            }

            // CameraController
            if (cameraObj.GetComponent<CameraController>() == null)
            {
                CameraController camController = cameraObj.AddComponent<CameraController>();
                camController.playerRoot = player.transform;
                camController.cameraDistance = 3f;
                camController.followSpeed = 10f;
                camController.mouseSensitivity = 2f;
                camController.maxVerticalAngle = 60f;
                camController.minVerticalAngle = -40f;
                camController.enableCameraCollision = true;
                camController.collisionRadius = 0.3f;
                camController.aimFOV = 40f;
                camController.sprintFOV = 70f;
                camController.zoomSpeed = 10f;
                camController.walkShakeIntensity = 0.005f;
                camController.runShakeIntensity = 0.015f;
                camController.jumpShakeIntensity = 0.03f;
                camController.shakeFrequency = 10f;
                camController.jumpShakeDuration = 0.3f;
                camController.shoulderSwitchSpeed = 10f;
                Debug.Log("✓ Added CameraController");
            }

            // CameraShake
            if (cameraObj.GetComponent<CameraShake>() == null)
            {
                CameraShake shake = cameraObj.AddComponent<CameraShake>();
                shake.globalIntensity = 1f;
                Debug.Log("✓ Added CameraShake");
            }

            // 10. Conectar referencias en PlayerController
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                // Usar SerializedObject para poder modificar en modo prefab
                SerializedObject so = new SerializedObject(controller);
                
                so.FindProperty("inputManager").objectReferenceValue = player.GetComponent<InputManager>();
                so.FindProperty("cameraController").objectReferenceValue = cameraObj.GetComponent<CameraController>();
                so.FindProperty("remoteAnimator").objectReferenceValue = player.GetComponent<RemotePlayerAnimator>();
                
                so.ApplyModifiedProperties();
                Debug.Log("✓ Connected references in PlayerController");
            }

            Debug.Log("=== Player Prefab Setup Complete ===");
        }
    }
}
