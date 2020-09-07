namespace EsnyaFactory.ClothSkirtHelper {
  using UnityEngine;
  using UnityEditor;

  class AssetUtility {
    public static void ForceCreateAsset(Object o, string path) {
      if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) {
        AssetDatabase.DeleteAsset(path);
      }
      AssetDatabase.CreateAsset(o, path);
    }
  }
}
