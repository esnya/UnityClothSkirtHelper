namespace EsnyaFactory {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEngine.Experimental.UIElements;
  using UnityEditor;
  using UnityEditor.Experimental.UIElements;

  public class SkirtMeshToolParameters : ScriptableObject {
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public bool[] subMeshFilters = {};
    public Collider[] colliders = {};
    public bool deleteInside;
    public bool includeBoundary;
    public string outputDirectory;

    public void Update() {
      if (subMeshFilters == null) subMeshFilters = new bool[0];
      if (colliders == null) colliders = new Collider[0];

      subMeshFilters = Enumerable
        .Range(0, skinnedMeshRenderer.sharedMesh.subMeshCount)
        .Select(i => i < subMeshFilters.Length ? subMeshFilters[i] : true)
        .ToArray();
    }

    public static Mesh ExtractMesh(SkinnedMeshRenderer srcRenderer, Func<Vector3, Vector3, bool> vertexPredicate, Func<int, bool> subMeshPredicate, bool includeBoundary) {
      var srcMesh = srcRenderer.sharedMesh;

      var worldVertices = srcMesh.vertices.Select(srcRenderer.transform.TransformPoint).ToArray();
      var worldNormals = srcMesh.normals.Select(srcRenderer.transform.TransformDirection).ToArray();

      var subMeshList = Enumerable
        .Range(0, srcMesh.subMeshCount)
        .Where(subMeshPredicate)
        .Select(subMesh =>
          srcMesh
            .GetTriangles(subMesh, true)
            .Select((v, i) => new { v, i })
            .GroupBy(b => b.i / 3)
            .Select(g => g.Select(b => b.v))
            .Where(f => {
              var count = f.Where(v => vertexPredicate(worldVertices[v], worldNormals[v])).Count();
              return includeBoundary && count >= 1 || count == 3;
            })
            .SelectMany(a => a)
            .ToArray()
        ).ToArray();

      var remapTable = Enumerable.Range(0, srcMesh.vertexCount).Select(v => -1).ToArray();
      subMeshList
        .SelectMany(a => a)
        .Distinct()
        .Select((src, dst) => new { src, dst })
        .ToList()
        .ForEach(a => {
          remapTable[a.src] = a.dst;
        });

      var dstMesh = new Mesh() {
        vertices = srcMesh.vertices.Where((a, i) => remapTable[i] >= 0).ToArray(),
        boneWeights = srcMesh.boneWeights.Where((a, i) => remapTable[i] >= 0).ToArray(),
        bindposes = srcMesh.bindposes.ToArray(), //.Where((a, i) => remapTable[i] >= 0).ToArray(),
        colors = srcMesh.colors.Where((a, i) => remapTable[i] >= 0).ToArray(),
        colors32 = srcMesh.colors32.Where((a, i) => remapTable[i] >= 0).ToArray(),
        normals = srcMesh.normals.Where((a, i) => remapTable[i] >= 0).ToArray(),
        tangents = srcMesh.tangents.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv = srcMesh.uv.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv2 = srcMesh.uv2.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv3 = srcMesh.uv3.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv4 = srcMesh.uv4.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv5 = srcMesh.uv5.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv6 = srcMesh.uv6.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv7 = srcMesh.uv7.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv8 = srcMesh.uv8.Where((a, i) => remapTable[i] >= 0).ToArray(),
      };

      dstMesh.subMeshCount = subMeshList.Length;

      subMeshList
        .Select((triangles, subMesh) => new { triangles = triangles.Select(v => remapTable[v]).ToArray(), subMesh })
        .ToList()
        .ForEach(a => {
          dstMesh.SetTriangles(a.triangles, a.subMesh, false, 0);
        });

      dstMesh.RecalculateBounds();

      return dstMesh;
    }

    public void Execute() {
      var originalMesh = skinnedMeshRenderer.sharedMesh;
      var verticesMap = originalMesh.vertices
        .Select(v => skinnedMeshRenderer.transform.TransformPoint(v))
        .ToArray();

      var skirtMesh = ExtractMesh(
        skinnedMeshRenderer,
        (vertex, normal) => {
          return (Vector3.Dot(vertex.normalized, normal.normalized) > 0 || !deleteInside)
            && colliders.Any(c => Vector3.Distance(c.ClosestPoint(vertex), vertex) < 0.001);
        },
        subMesh => subMeshFilters[subMesh],
        includeBoundary
      );
      skirtMesh.name = $"{originalMesh.name}_skirt";
      var otherMesh = ExtractMesh(
        skinnedMeshRenderer,
        (vertex, normal) => colliders.All(c => Vector3.Distance(c.ClosestPoint(vertex), vertex) >= 0.001),
        subMesh => true,
        !includeBoundary
      );
      otherMesh.name = $"{originalMesh.name}_other";

      var skirt = UnityEngine.Object.Instantiate(skinnedMeshRenderer.gameObject);
      skirt.name = skirtMesh.name;
      skirt.transform.SetParent(skinnedMeshRenderer.transform.parent);
      skirt.GetComponent<SkinnedMeshRenderer>().sharedMesh = skirtMesh;
      Undo.RegisterCreatedObjectUndo(skirt, "Skirt Created");

      if (otherMesh.triangles.Length > 0) {
        var other = UnityEngine.Object.Instantiate(skinnedMeshRenderer.gameObject);
        other.transform.SetParent(skinnedMeshRenderer.transform.parent);
        other.name = otherMesh.name;
        other.GetComponent<SkinnedMeshRenderer>().sharedMesh = otherMesh;
        Undo.RegisterCreatedObjectUndo(other, "Other Created");
      }

      Undo.RegisterFullObjectHierarchyUndo(skinnedMeshRenderer, "Deactivate source object");
      skinnedMeshRenderer.gameObject.SetActive(false);
      skinnedMeshRenderer.gameObject.tag = "EditorOnly";
    }
  }

  public class SkirtMeshTool : EditorWindow {
    public static T FindAsset<T>(string name) where T : UnityEngine.Object {
      return AssetDatabase
        .FindAssets($"{name} t:{typeof(T).Name}")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(p => p.Contains("Assets/ClothSkirtHelper/"))
        .Select(AssetDatabase.LoadAssetAtPath<T>)
        .FirstOrDefault();
    }

    public static string GetClothAvatarRank(int vertexCount) {
      if (vertexCount >= 200) return "Very Poor";
      else if (vertexCount >= 100) return "Poor";
      else if (vertexCount >= 50) return "Good";
      return "Excellent";
    }

    [MenuItem("EsnyaTools/Skirt Mesh Tool")]
    private static void ShowWindow() {
      var window = GetWindow<SkirtMeshTool>();
      // window.Show();
    }

    private Vector2 scroll;
    private SkirtMeshToolParameters parameters;
    private SerializedObject serializedParameters;

    private void OnEnable()
    {
      if (parameters == null) parameters = ScriptableObject.CreateInstance<SkirtMeshToolParameters>();
      serializedParameters = new SerializedObject(parameters);
      titleContent = new GUIContent("Skirt Mesh Tool");
    }

    private void MeshMetrics() {
      EditorGUILayout.LabelField("Vertex Count", $"{parameters.skinnedMeshRenderer.sharedMesh.vertexCount}");
      EditorGUILayout.LabelField("    Unnecessary", $"{parameters.skinnedMeshRenderer.sharedMesh.vertexCount - parameters.skinnedMeshRenderer.sharedMesh.triangles.Distinct().Count()}");
      EditorGUILayout.LabelField("Polygon Count", $"{parameters.skinnedMeshRenderer.sharedMesh.triangles.Length / 3}");
      EditorGUILayout.LabelField("SubMesh Count", $"{parameters.skinnedMeshRenderer.sharedMesh.subMeshCount}");
      EditorGUILayout.LabelField("Avatar Rank", GetClothAvatarRank(parameters.skinnedMeshRenderer.sharedMesh.vertexCount));
    }

    private void OnGUI() {
      serializedParameters.Update();

      EditorGUILayout.Space();

      using (var scrollScope = new EditorGUILayout.ScrollViewScope(scroll)) {
        scroll = scrollScope.scrollPosition;
        using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
          parameters.skinnedMeshRenderer = EditorGUILayout.ObjectField("Renderer", parameters.skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;

          if (parameters.skinnedMeshRenderer != null) MeshMetrics();
        }

        EditorGUILayout.Space();

        if (parameters.skinnedMeshRenderer != null) {
          parameters.Update();

          using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
            EditorGUILayout.PropertyField(serializedParameters.FindProperty("subMeshFilters"), true);
            EditorGUILayout.PropertyField(serializedParameters.FindProperty("colliders"), true);
            EditorGUILayout.PropertyField(serializedParameters.FindProperty("deleteInside"));
            EditorGUILayout.PropertyField(serializedParameters.FindProperty("includeBoundary"));
          }

          EditorGUILayout.Space();

          using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
            EditorGUILayout.PropertyField(serializedParameters.FindProperty("outputDirectory"));
          }

          EditorGUILayout.Space();

          if (GUILayout.Button("Split & Cleanup")) parameters.Execute();
        }

        serializedParameters.ApplyModifiedProperties();
      }
    }
  }
}
