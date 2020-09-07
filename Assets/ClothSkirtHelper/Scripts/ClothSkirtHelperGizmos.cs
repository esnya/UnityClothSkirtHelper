namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using UnityEngine;

  public class ClothSkirtHelperGizmos : MonoBehaviour {

    public static ClothSkirtHelperGizmos GetOrCreate() {
      var c = GameObject.FindObjectOfType<ClothSkirtHelperGizmos>();
      if (c == null) {
        var o = new GameObject("ClothSkirtHelper");
        o.tag = "EditorOnly";
        c = o.AddComponent<ClothSkirtHelperGizmos>();
      }
      return c;
    }

    public Action drawGizmos;
    private void OnDrawGizmos() {
      if (drawGizmos != null) drawGizmos();
    }
  }
}
