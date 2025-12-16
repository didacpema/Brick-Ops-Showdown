using UnityEngine;

namespace BrickOps.Utils
{
    public class ColliderVisualizer : MonoBehaviour
    {
        // --- INTERRUPTOR GLOBAL ---
        public static bool ShowGizmos = false; 
        // --------------------------

        public Color boxColor = Color.green;
        public Color capsuleColor = Color.cyan;
        public Color meshColor = Color.yellow;

        private Material lineMaterial;

        void Awake()
        {
            // Configuración del material (igual que antes)
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        void OnRenderObject()
        {
            // 1. CHEQUEO MAESTRO: Si la variable global es false, ADIÓS.
            if (!ShowGizmos) return;

            // 2. Seguridad extra: Si no hay material, fuera.
            if (lineMaterial == null) return;

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            foreach (Collider col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                
                // Evitamos dibujarnos a nosotros mismos si el visualizador tuviera collider (raro pero posible)
                if (col.GetComponent<ColliderVisualizer>()) continue;

                if (col is BoxCollider box) DrawBox(box);
                else if (col is CapsuleCollider capsule) DrawCapsule(capsule);
                else if (col is MeshCollider mesh) DrawMesh(mesh);
            }

            GL.End();
            GL.PopMatrix();
        }

        #region Draw Logic (Copia esto tal cual tenías antes)
        void DrawBox(BoxCollider box)
        {
            GL.Color(boxColor);
            Matrix4x4 matrix = box.transform.localToWorldMatrix;
            Vector3 c = box.center;
            Vector3 s = box.size * 0.5f;
            Vector3[] p = new Vector3[8];
            p[0] = matrix.MultiplyPoint(c + new Vector3(-s.x, -s.y, -s.z));
            p[1] = matrix.MultiplyPoint(c + new Vector3(s.x, -s.y, -s.z));
            p[2] = matrix.MultiplyPoint(c + new Vector3(s.x, s.y, -s.z));
            p[3] = matrix.MultiplyPoint(c + new Vector3(-s.x, s.y, -s.z));
            p[4] = matrix.MultiplyPoint(c + new Vector3(-s.x, -s.y, s.z));
            p[5] = matrix.MultiplyPoint(c + new Vector3(s.x, -s.y, s.z));
            p[6] = matrix.MultiplyPoint(c + new Vector3(s.x, s.y, s.z));
            p[7] = matrix.MultiplyPoint(c + new Vector3(-s.x, s.y, s.z));
            
            // Aristas
            DrawLine(p[0], p[1]); DrawLine(p[1], p[2]); DrawLine(p[2], p[3]); DrawLine(p[3], p[0]);
            DrawLine(p[4], p[5]); DrawLine(p[5], p[6]); DrawLine(p[6], p[7]); DrawLine(p[7], p[4]);
            DrawLine(p[0], p[4]); DrawLine(p[1], p[5]); DrawLine(p[2], p[6]); DrawLine(p[3], p[7]);
        }

        void DrawCapsule(CapsuleCollider c)
        {
            GL.Color(capsuleColor);
            float height = Mathf.Max(0, c.height / 2f - c.radius);
            Vector3 localTop, localBot;
            if (c.direction == 0) { localTop = c.center + Vector3.right * height; localBot = c.center - Vector3.right * height; }
            else if (c.direction == 2) { localTop = c.center + Vector3.forward * height; localBot = c.center - Vector3.forward * height; }
            else { localTop = c.center + Vector3.up * height; localBot = c.center - Vector3.up * height; }
            GL.Vertex(c.transform.TransformPoint(localTop)); GL.Vertex(c.transform.TransformPoint(localBot));
        }

        void DrawMesh(MeshCollider m)
        {
            if (m.sharedMesh == null) return;
            GL.Color(meshColor);
            Mesh mesh = m.sharedMesh;
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            Matrix4x4 matrix = m.transform.localToWorldMatrix;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v1 = matrix.MultiplyPoint(verts[tris[i]]);
                Vector3 v2 = matrix.MultiplyPoint(verts[tris[i + 1]]);
                Vector3 v3 = matrix.MultiplyPoint(verts[tris[i + 2]]);
                GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v2); GL.Vertex(v3); GL.Vertex(v3); GL.Vertex(v1);
            }
        }
        void DrawLine(Vector3 a, Vector3 b) { GL.Vertex(a); GL.Vertex(b); }
        #endregion
    }
}