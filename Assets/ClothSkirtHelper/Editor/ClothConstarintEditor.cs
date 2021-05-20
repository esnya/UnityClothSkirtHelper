namespace EsnyaFactory {
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEditor;
    
    class ClothConstraintEditor : EditorWindow
    {
        [MenuItem("EsnyaTools/Cloth Constraint Editor")]
        static void ShowWindow()
        {
            var window = GetWindow<ClothConstraintEditor>();
            window.Show();
        }
        
        Cloth cloth;
        List<int> indexTable;
        Mesh mesh, maxDistanceMesh, colliderSphereDistanceMesh;
        
        bool visualize;
        float displayScale = 1.0f;
        float distanceClip = 2.0f;
        float distanceScale = 1.0f;
        CompareFunction displayZTest = CompareFunction.LessEqual;
        
        bool edit;
        Vector2Int editDivisions = new Vector2Int 
        {
            x = 4, 
            y = 3,
        };
        List<List<float>> controlPoints = new List<List<float>>();
        
        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }
        
        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }
        
        void OnGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                cloth = EditorGUILayout.ObjectField("Cloth", cloth, typeof(Cloth), true) as Cloth;
                if (cloth == null) return;
                
                if (!cloth.enabled)
                {
                    EditorGUILayout.LabelField("Cloth Component must to be enabled.");
                    return;
                }
                
                if (check.changed)
                {
                    var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
                    mesh = new Mesh();
                    renderer.BakeMesh(mesh);
                }
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Vertex Count", cloth.vertices.Length);
                    EditorGUILayout.ObjectField("Root Bone", cloth.GetComponent<SkinnedMeshRenderer>().rootBone, typeof(Transform), true);
                }
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                visualize = EditorGUILayout.Toggle("Visualize", visualize);
                displayZTest = (CompareFunction)EditorGUILayout.EnumPopup("Z Test", displayZTest);
                distanceClip = EditorGUILayout.FloatField("Distance Clip (m)", distanceClip);
                distanceScale = EditorGUILayout.FloatField("Distance Scale", distanceScale);
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (var check = new EditorGUI.ChangeCheckScope()) 
                {
                    edit = EditorGUILayout.Toggle("Edit", edit);
                    editDivisions = EditorGUILayout.Vector2IntField("Divisions", editDivisions);
                    
                    if (edit) 
                    {
                        controlPoints = controlPoints
                            .Select(row => row.Concat<float>(Enumerable.Repeat(0.2f, editDivisions.x)).Take(editDivisions.x).ToList())
                            .Concat<List<float>>(Enumerable.Repeat(Enumerable.Repeat(0.2f, editDivisions.x).ToList(), editDivisions.y))
                            .ToList();
                    }
                }
                
            }
        }
        
        
        static Matrix4x4 GetClothTransform(Cloth cloth)
        {
            var t = cloth.GetComponent<SkinnedMeshRenderer>().rootBone ?? cloth.transform;
            return t.localToWorldMatrix;
        }
        
        static Gradient histgramGradient = new Gradient() 
        {
            alphaKeys = new []
            {
                new GradientAlphaKey() { time = 0.0f, alpha = 1.0f },
                new GradientAlphaKey() { time = 1.0f, alpha = 1.0f },
            },
            colorKeys = new []
            {
                new GradientColorKey() { time = 0.00f, color = Color.red },
                new GradientColorKey() { time = 0.33f, color = Color.magenta },
                new GradientColorKey() { time = 0.66f, color = Color.yellow },
                new GradientColorKey() { time = 1.00f, color = Color.green },
            },
        };
        
        static Color FloatToColor(float value, float max)
        {
            return histgramGradient.Evaluate(max == 0 ? value : value / max);
        }
        
        static float inf = 10000.0f;
        
        void OnSceneGUI(SceneView sceneView)
        {
            if (cloth == null) return;
       
            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            var transform =  GetClothTransform(cloth);
            
            if (visualize) {
                var max = new ClothSkinningCoefficient() {
                    maxDistance = cloth.coefficients.Select(c => c.maxDistance).Where(a => a < float.MaxValue).Append(0f).Max(),
                    collisionSphereDistance = cloth.coefficients.Select(c => c.collisionSphereDistance).Where(a => a < float.MaxValue).Append(0f).Max(),
                };
                
                Handles.zTest = displayZTest;
                
                for (int i = 0; i < cloth.vertices.Length && i < cloth.coefficients.Length; i++)
                {
                    Vector3 vertex = transform * ((Vector4)cloth.vertices[i] + new Vector4(0, 0, 0, 1));
                    var normal = transform * cloth.normals[i];
                    var rotation = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    var coefficient = cloth.coefficients[i];
                    
                    Handles.color = Color.black;
                    Handles.color = coefficient.maxDistance < inf
                        ? FloatToColor(coefficient.maxDistance, max.maxDistance)
                        : Color.black;
                            
                    Handles.SphereHandleCap(0, vertex, Quaternion.Euler(0, 0, 0), 0.002f, EventType.Repaint);
                    
                    if (coefficient.maxDistance < distanceClip)
                    {
                        Handles.ArrowHandleCap(
                            0,
                            vertex,
                            rotation,
                            coefficient.maxDistance * distanceScale,
                            EventType.Repaint
                        );
                    }
                    
                    if (coefficient.collisionSphereDistance < distanceClip)
                    {
                        Handles.color = FloatToColor(coefficient.collisionSphereDistance, max.collisionSphereDistance * distanceScale);
                        Handles.ArrowHandleCap(
                            0,
                            vertex,
                            rotation * Quaternion.Euler(180, 0, 0),
                            coefficient.collisionSphereDistance * displayScale,
                            EventType.Repaint
                        );
                    }
                }
            }
            
            if (edit)
            {
                Handles.zTest = CompareFunction.Always;
                
                var bounds = renderer.bounds;
                
                for (int j = 0; j < editDivisions.x; j++)
                {
                    var angle = 360.0f * j / editDivisions.x;
                    var rotation = Quaternion.Euler(0, angle, 0);
                    
                    var positions = new Vector3[editDivisions.y];
                    
                    for (int i = 0; i < editDivisions.y; i++)
                    {
                        var v = (float)i / (editDivisions.y - 1);
                        var oy = bounds.extents.y * (1 - v * 2);
                        var radius = controlPoints[i][j];
                        
                        positions[i] = bounds.center + Vector3.up * oy;
                        Handles.ArrowHandleCap(0, positions[i], rotation, radius, EventType.Repaint);
                    }
                    
                    Handles.DrawAAPolyLine(positions);
                }
            }
        }
    }
}
