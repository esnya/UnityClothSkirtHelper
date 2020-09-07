namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  class MeshExtractor : ToolBase {
    public List<bool> subMeshFilters = new List<bool>() { true };
    public List<Collider> colliders = new List<Collider>() { null };
    public bool includeBoundary = true;

    public SkinnedMeshRenderer Execute(SkinnedMeshRenderer skinnedMeshRenderer, string outputDirectory) {
      var originalMesh = skinnedMeshRenderer.sharedMesh;
      var validColliders = colliders.Where(c => c != null).ToList();

      var skirtMesh = MeshUtility.ExtractMesh(
        skinnedMeshRenderer,
        (vertex, normal) => {
          return validColliders.Count == 0 || validColliders.Any(c => Vector3.Distance(c.ClosestPoint(vertex), vertex) < 0.001);
        },
        subMesh => subMeshFilters[subMesh],
        includeBoundary
      );
      skirtMesh.name = $"{originalMesh.name}_skirt";
      var otherMesh = MeshUtility.ExtractMesh(
        skinnedMeshRenderer,
        (vertex, normal) => validColliders.Count > 0 && validColliders.All(c => Vector3.Distance(c.ClosestPoint(vertex), vertex) >= 0.001),
        subMesh => true,
        !includeBoundary
      );
      otherMesh.name = $"{originalMesh.name}_other";

      var skirtRenderer = MeshUtility.ReplaceMesh(skinnedMeshRenderer, skirtMesh, outputDirectory);

      if (otherMesh.triangles.Length > 0) {
        MeshUtility.ReplaceMesh(skinnedMeshRenderer, otherMesh, outputDirectory);
      }

      Undo.RegisterFullObjectHierarchyUndo(skinnedMeshRenderer, "Deactivate source object");
      skinnedMeshRenderer.gameObject.SetActive(false);
      skinnedMeshRenderer.gameObject.tag = "EditorOnly";

      return skirtRenderer;
    }

    public void OnGUI(SkinnedMeshRenderer skinnedMeshRenderer) {
      EditorGUILayout.LabelField("Filter by SubMesh");
      using (new EditorGUI.IndentLevelScope()) {
        subMeshFilters = skinnedMeshRenderer.sharedMaterials
          .Select((material, i) => {
            using (new EditorGUILayout.HorizontalScope()) {
              EditorGUILayout.ObjectField(material, typeof(Material), false);
              var newValue = EditorGUILayout.Toggle(subMeshFilters.Skip(i).Append(true).First(), GUILayout.ExpandWidth(false));
              return newValue;
            }
          })
          .ToList();
      }

      using (new EditorGUILayout.HorizontalScope()) {
        EditorGUILayout.LabelField("Filter by Collider (Box, Sphere or Capsule)");
        if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) colliders.Add(null);
      }
      using (new EditorGUI.IndentLevelScope()) {
        colliders = colliders
          .Select((collider, i) => {
            using (new EditorGUILayout.HorizontalScope()) {
              var newValue =  EditorGUILayout.ObjectField(collider, typeof(Collider), true) as Collider;
              if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) colliders.RemoveAt(i);
              return newValue;
            }
          })
          .ToList();
      }

      includeBoundary = EditorGUILayout.Toggle("Include Boundary", includeBoundary);
    }

    public bool Validate() => true;
  }
}
