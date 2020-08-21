namespace EsnyaFactory {
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  public class MeshCleaner : EditorWindow {

    [MenuItem("EsnyaTools/Mesh Cleaner")]
    private static void ShowWindow() {
      var window = GetWindow<MeshCleaner>();
      window.titleContent = new GUIContent("Mesh Cleaner");
      window.Show();
    }

    public struct MeshInfo {
      public HashSet<int> neccesaryVertices;
    }

    private SkinnedMeshRenderer target;
    // private Mesh mesh;
    private Mesh mesh {
      get {
        return target?.sharedMesh;
      }
    }
    private List<MeshInfo> meshInfo;
    private HashSet<int> neccesaries;
    private void OnGUI() {
      var newTarget = EditorGUILayout.ObjectField("Target", target, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
      if (target == null || newTarget != target) {
        neccesaries = null;
      }
      target = newTarget;

      if (mesh != null) {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Mesh Metrics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Vertecies", $"{mesh.vertexCount}");
        EditorGUILayout.LabelField("Sub Meshes", $"{mesh.subMeshCount}");
        EditorGUILayout.LabelField("Polygons", $"{mesh.triangles.Length / 3}");
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
      }

      EditorGUILayout.BeginVertical(GUI.skin.box);
      EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
      if (neccesaries != null) {
        EditorGUILayout.LabelField("Unnessesary Vertices", $"{mesh.vertexCount - neccesaries.Count}");
      }
      EditorGUILayout.EndVertical();
      EditorGUILayout.Space();

      EditorGUI.BeginDisabledGroup(mesh == null);
      {
        if (GUILayout.Button("Analyze")) {
          Analyze();
        }
      }
      EditorGUI.EndDisabledGroup();

      EditorGUI.BeginDisabledGroup(mesh == null || neccesaries == null);
      {
        if (GUILayout.Button("Cleanup")) {
          CleanUp();
        }
      }
      EditorGUI.EndDisabledGroup();
    }

    private void Analyze() {
      neccesaries = new HashSet<int>();
      for (int i = 0; i < mesh.subMeshCount; i++) {
        // mesh.GetTriangles(i, true).Select((a, b) => new { a, b }).GroupBy(a => a.b / 3).Select(g => string.Join(", ", g.Select(a => a.a))).ToList().ForEach(Debug.Log);
        mesh.GetTriangles(i, true).ToList().ForEach(index => {
          neccesaries.Add(index);
        });
        // mesh.GetTriangles(i, true).ToList().ForEach(index => Debug.Log(neccesaries.Contains(index)));
      }
    }

    private IEnumerable<T> FilterUnneccesary<T>(T[] source) {
      if (source == null) return null;
      if (source.Length == 0) return new List<T>();
      return neccesaries.Select(i => source[i]).ToList();
    }

    private Vector3[] AddEach(Vector3[] a, Vector3[] b) {
      return a.Zip(b, (c, d) => c  + d).ToArray();
    }
    private Vector4[] AddEach(Vector4[] a, Vector4[] b) {
      return a.Zip(b, (c, d) => c  + d).ToArray();
    }

    private void CleanUp() {
      var path = EditorUtility.SaveFilePanel("Save Mesh", "Assets", "Mesh", "asset");
      if (string.IsNullOrEmpty(path)) {
        Debug.LogError("Save file path is empty");
        return;
      }
      path = path.Replace(Application.dataPath, "Assets");

      var dstContainer = new GameObject($"{target.gameObject.name}_Clean");
      dstContainer.transform.SetParent(target.transform.parent);
      dstContainer.transform.localPosition = target.transform.localPosition;
      dstContainer.transform.localRotation = target.transform.localRotation;
      dstContainer.transform.localRotation = target.transform.localRotation;
      var renderer = dstContainer.AddComponent<SkinnedMeshRenderer>();
      renderer.bones = target.bones.ToArray();
      renderer.sharedMaterials = target.sharedMaterials.ToArray();
      renderer.probeAnchor = target.probeAnchor;
      renderer.rootBone = target.rootBone;
      renderer.sharedMaterials = target.sharedMaterials;
      renderer.sharedMesh= Instantiate(target.sharedMesh);
      Enumerable.Range(0, target.sharedMesh.blendShapeCount).ToList().ForEach(i => renderer.SetBlendShapeWeight(i, target.GetBlendShapeWeight(i)));

      var mesh = target.sharedMesh;
      var dst = renderer.sharedMesh;

      var table = neccesaries
        .Select((oldIndex, newIndex) => new { oldIndex, newIndex })
        .ToDictionary(p => p.oldIndex, p => p.newIndex);

      dst.Clear();

      dst.SetVertices(FilterUnneccesary(mesh.vertices).Select((v, i) => i < 0 ? Vector3.zero : v).ToList());
      dst.SetNormals(FilterUnneccesary(mesh.normals).ToList());
      dst.SetUVs(0, FilterUnneccesary(mesh.uv).ToList());
      dst.SetUVs(1, FilterUnneccesary(mesh.uv2).ToList());
      dst.SetUVs(2, FilterUnneccesary(mesh.uv3).ToList());
      dst.SetUVs(3, FilterUnneccesary(mesh.uv4).ToList());
      dst.SetUVs(4, FilterUnneccesary(mesh.uv5).ToList());
      dst.SetUVs(5, FilterUnneccesary(mesh.uv6).ToList());
      dst.SetUVs(6, FilterUnneccesary(mesh.uv7).ToList());
      dst.SetUVs(7, FilterUnneccesary(mesh.uv8).ToList());
      dst.SetTangents(FilterUnneccesary(mesh.tangents).ToList());
      dst.boneWeights = FilterUnneccesary(mesh.boneWeights).ToArray();

      for (int i = 0; i < mesh.subMeshCount; i++) {
        var indices = mesh.GetTriangles(i, true).Select(j => table[j]);
        if (indices.Any(j => j < 0)) {
          Debug.LogError("Unknown Error");
          return;
        }

        var broken = indices
          .Select((v, j) => new { v, j })
          .GroupBy(x => x.j / 3)
          .Select(g => g.Select(x => x.v).ToList())
          .Select(f => f[0] == f[1] || f[1] == f[2] || f[2] == f[0])
          .Where(f => f)
          .FirstOrDefault();
        if (broken) {
          Debug.LogError("Triangle broken");
          return;
        }


        dst.SetIndices(indices.ToArray(), mesh.GetTopology(i), i, false, 0);
      }

      for (int i = 0; i < mesh.blendShapeCount; i++) {
        for (int j = 0; j < mesh.GetBlendShapeFrameCount(i); j++) {
          var dVertices = new Vector3[mesh.vertexCount];
          var dNormal = new Vector3[mesh.vertexCount];
          var dTangents = new Vector3[mesh.vertexCount];

          mesh.GetBlendShapeFrameVertices(i, j, dVertices, dNormal, dTangents);
          var weight = mesh.GetBlendShapeFrameWeight(i, j);

          dst.AddBlendShapeFrame(
            mesh.GetBlendShapeName(i),
            weight,
            FilterUnneccesary(dVertices).ToArray(),
            FilterUnneccesary(dNormal).ToArray(),
            FilterUnneccesary(dTangents).ToArray()
          );
        }
      }

      AssetDatabase.CreateAsset(dst, path);
      target.gameObject.SetActive(false);

      neccesaries = null;
    }
  }
}