namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  public class InsideDeleter : ToolBase {
    public bool includeBoundary = true;

    public SkinnedMeshRenderer Execute(SkinnedMeshRenderer skinnedMeshRenderer, string outputDirectory) {
      var originalMesh = skinnedMeshRenderer.sharedMesh;

      var newMesh = MeshUtility.ExtractMesh(
        skinnedMeshRenderer,
        (vertex, normal) => Vector3.Dot(vertex.normalized, normal.normalized) > 0,
        subMesh => true,
        includeBoundary
      );
      newMesh.name = $"{originalMesh.name}_single";

      var newObject = UnityEngine.Object.Instantiate(skinnedMeshRenderer.gameObject);
      newObject.name = newMesh.name;
      newObject.transform.SetParent(skinnedMeshRenderer.transform.parent);
      newObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = newMesh;
      Undo.RegisterCreatedObjectUndo(newObject, "Skirt Created");

      AssetUtility.ForceCreateAsset(newMesh, $"{outputDirectory}/{newMesh.name}.asset");

      Undo.RegisterFullObjectHierarchyUndo(skinnedMeshRenderer, "Deactivate source object");
      skinnedMeshRenderer.gameObject.SetActive(false);
      skinnedMeshRenderer.gameObject.tag = "EditorOnly";

      return newObject.GetComponent<SkinnedMeshRenderer>();
    }

    public void OnGUI(SkinnedMeshRenderer skinnedMeshRenderer) {
      includeBoundary = EditorGUILayout.Toggle("Include Boundary", includeBoundary);
    }
  }
}
