namespace EsnyaFactory.ClothSkirtHelper {
  using UnityEngine;

  interface ToolBase {
    SkinnedMeshRenderer Execute(SkinnedMeshRenderer skinnedMeshRenderer, string outputDirectory);
    void OnGUI(SkinnedMeshRenderer skinnedMeshRenderer);
  }
}
