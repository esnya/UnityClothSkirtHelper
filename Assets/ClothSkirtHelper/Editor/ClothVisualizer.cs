using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory.UnityClothSkirtHelper
{
    public class ClothVisualizerParameters : ScriptableSingleton<ClothVisualizerParameters>
    {
        public bool enabled = true;
        public Cloth cloth;
        public int vertexIndex = 0;
        public bool enableMD = true, enableSP = true;
        public float gizmoOpacity = 0.5f;
        public float pointRadius = 0.005f;
    }

    public class ClothVisualizer : EditorWindow
    {
        private static class Colors
        {
            public static Color
                maxDistance = Color.magenta,
                surfacePenetration = Color.cyan,
                vertexToParticle = Color.white,
                vertex = Color.white,
                particle = Color.green,
                particleNormal = Color.green;
        }

        [MenuItem("EsnyaTools/Cloth/Visualizer")]
        private static void ShowWindow()
        {
            var window = GetWindow<ClothVisualizer>();
            window.titleContent = new GUIContent("Cloth Visualizer");
            window.Show();
        }

        private void OnGUI()
        {
            var p = ClothVisualizerParameters.instance;

            using (new EditorGUILayout.HorizontalScope())
            {
                p.cloth = EditorGUILayout.ObjectField("Target Cloth", p.cloth, typeof(Cloth), true) as Cloth;

                var selectedCloth = Selection.activeGameObject?.GetComponent<Cloth>();
                using (new EditorGUI.DisabledGroupScope(selectedCloth == null))
                {
                    if (GUILayout.Button("From Scene Selection", EditorStyles.miniButton, GUILayout.ExpandWidth(false))) p.cloth = selectedCloth;
                }
            }

            p.enabled = EditorGUILayout.Toggle("Visualize", p.enabled);
            p.vertexIndex = p.cloth == null ? Mathf.Max(EditorGUILayout.IntField("Target Index", p.vertexIndex), 0) : EditorGUILayout.IntSlider("Target Index", p.vertexIndex, 0, p.cloth.vertices.Length - 1);

            EditorGUILayout.LabelField("Visualize Range");
            using (new EditorGUI.IndentLevelScope())
            {
                p.enableMD = EditorGUILayout.Toggle("Max Distance", p.enableMD);
                p.enableSP = EditorGUILayout.Toggle("Surface Penetration", p.enableSP);
            }

            p.gizmoOpacity = EditorGUILayout.Slider("Opacity", p.gizmoOpacity, 0.0f, 1.0f);
            p.pointRadius = EditorGUILayout.FloatField("Point Radius", p.pointRadius);

            EditorGUILayout.Space();

            var currentColor = GUI.contentColor;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = Colors.vertex; EditorGUILayout.LabelField("Vertex", GUILayout.ExpandWidth(false));
                GUI.color = Colors.particle; EditorGUILayout.LabelField("Cloth Particle", GUILayout.ExpandWidth(false));
                GUI.color = Colors.maxDistance; EditorGUILayout.LabelField("Max Distance", GUILayout.ExpandWidth(false));
                GUI.color = Colors.surfacePenetration; EditorGUILayout.LabelField("Surface Penetration", GUILayout.ExpandWidth(false));
            }
            GUI.color = currentColor;
        }

        static Mesh icoMesh;
        private static void DrawIcoSphere(Vector3 position, float radius)
        {
            if (icoMesh == null) icoMesh = Resources.Load<Mesh>("Ico3");
            Gizmos.DrawMesh(icoMesh, position, Quaternion.identity, Vector3.one * radius * 2.0f);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        private static void DrawClothGizmo(Cloth cloth, GizmoType gizmoType)
        {
            var p = ClothVisualizerParameters.instance;
            if (p?.enabled != true || cloth != p.cloth) return;

            var target = p.vertexIndex;
            if (target >= cloth.vertices.Length) return;

            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();

            var mesh = new Mesh();
            renderer.BakeMesh(mesh);

            var merged = mesh.vertices.Select((vertex, index) => (vertex, index)).GroupBy(t => t.vertex, t => t.index).Select(g => g.First()).Select(i =>
            {
                var vertex = mesh.vertices[i];
                var normal = mesh.normals[i];
                return (vertex, normal);
            }).ToArray();

            var worldPosition = cloth.transform.TransformPoint(merged[target].vertex);
            var worldNormal = cloth.transform.TransformDirection(merged[target].normal);

            var md = cloth.coefficients[target].maxDistance;
            var sp = cloth.coefficients[target].collisionSphereDistance;

            var rootBone = cloth.GetComponent<SkinnedMeshRenderer>().rootBone ?? cloth.transform;

            var clothVertex = rootBone.TransformPoint(cloth.vertices[target]);
            var clothNormal = rootBone.TransformDirection(cloth.normals[target]);

            Gizmos.color = Colors.vertex;
            Gizmos.DrawSphere(worldPosition, p.pointRadius);

            Gizmos.color = Colors.maxDistance;
            Gizmos.DrawRay(worldPosition, worldNormal * md);
            if (p.enableMD)
            {
                Gizmos.color = Colors.maxDistance - Color.black * (1.0f - p.gizmoOpacity);
                DrawIcoSphere(worldPosition, md);
            }

            Gizmos.color = Colors.surfacePenetration;
            Gizmos.DrawRay(worldPosition, -worldNormal * sp);
            if (p.enableSP)
            {
                Gizmos.color = Colors.surfacePenetration - Color.black * (1.0f - p.gizmoOpacity);
                DrawIcoSphere(worldPosition - worldNormal * (sp + md * 2), md * 2);
            }

            Gizmos.color = Colors.vertexToParticle;
            Gizmos.DrawLine(worldPosition, clothVertex);

            Gizmos.color = Colors.particle;
            Gizmos.DrawSphere(clothVertex, p.pointRadius);

            Gizmos.color = Colors.particleNormal;
            Gizmos.DrawRay(clothVertex, clothNormal * md);
        }
    }
}
