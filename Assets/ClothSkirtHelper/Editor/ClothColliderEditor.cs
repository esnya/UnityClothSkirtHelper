namespace EsnyaFactory {
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEditor;


  public class ClothColliderEditor : EditorWindow {
   [MenuItem("EsnyaTools/Cloth Collider Editor")]
    public static ClothColliderEditor Init()
    {
        var window = EditorWindow.GetWindow<ClothColliderEditor>();
        window.Show();
      return window;
    }

    [MenuItem("CONTEXT/Cloth/Cloth Collider Editor")]
    public static ClothColliderEditor InitByAsset(MenuCommand menuCommand)
    {
      var window = Init();
      if (menuCommand.context is Cloth) {
        window.cloth = menuCommand.context as Cloth;
      }

      return window;
    }


    private static readonly string[][] mirrorPatterns = {
      new [] { "Left", "Right" },
      new [] { "_LEFT", "_RIGHT" },
      new [] { ".LEFT", ".RIGHT" },
      new [] { " LEFT", " RIGHT" },
      new [] { "_L", "_R" },
      new [] { "_l", "_l" },
      new [] { ".L", ".R" },
      new [] { ".l", ".l" },
      new [] { " L", " R" },
      new [] { " l", " l" },
    };

    private Cloth cloth;
    private int selectedColliderIndex;
    private Collider[] colliders;
    private bool mirror = false;
    private Vector3 mirrorScale = new Vector3(-1, 1, 1);

    private void OnEnable()
    {
      titleContent = new GUIContent("Cloth Collider Editor");
    }

    private void OnGUI() {
      cloth = EditorGUILayout.ObjectField("Cloth", cloth, typeof(Cloth), true) as Cloth;

      if (cloth == null) return;

      var colliders = cloth.sphereColliders
        .SelectMany(pair => new List<Collider>() { pair.first, pair.second })
        .Concat(cloth.capsuleColliders)
        .Distinct()
        .ToList();

      if (colliders.Count > 0) {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        selectedColliderIndex = EditorGUILayout.Popup(selectedColliderIndex, colliders.Select(c => c.name).ToArray());

        if (selectedColliderIndex >= 0 && selectedColliderIndex < colliders.Count) {
          var collider = colliders[selectedColliderIndex];

          var mirroredName = mirrorPatterns.Select(p => {
            if (collider.name.Contains(p[0])) return collider.name.Replace(p[0], p[1]);
            else if (collider.name.Contains(p[1])) return collider.name.Replace(p[1], p[0]);
            return null;
          }).FirstOrDefault(a => a != null);
          var mirroredCollider = mirroredName != null ? colliders.FirstOrDefault(c => c.name == mirroredName) : null;

          EditorGUI.BeginDisabledGroup(mirroredCollider == null);
          EditorGUILayout.BeginHorizontal();
          mirror = EditorGUILayout.ToggleLeft("Mirror", mirror);
          EditorGUILayout.ObjectField(mirroredCollider, collider.GetType(), true);
          EditorGUILayout.EndHorizontal();
          mirrorScale.x = EditorGUILayout.ToggleLeft("Flip X", mirrorScale.x < 0) ? -1 : 1;
          mirrorScale.y = EditorGUILayout.ToggleLeft("Flip Y", mirrorScale.y < 0) ? -1 : 1;
          mirrorScale.z = EditorGUILayout.ToggleLeft("Flip Z", mirrorScale.z < 0) ? -1 : 1;
          EditorGUI.EndDisabledGroup();

          if (collider is SphereCollider) {
            var sphereCollider = collider as SphereCollider;
            sphereCollider.center = EditorGUILayout.Vector3Field("Center", sphereCollider.center);
            sphereCollider.radius = EditorGUILayout.FloatField("Radius", sphereCollider.radius);
            if (mirror && mirroredCollider is SphereCollider) {
              var casted = mirroredCollider as SphereCollider;
              casted.center = Vector3.Scale(sphereCollider.center, mirrorScale);
              casted.radius = sphereCollider.radius;
            }
          } else if (collider is CapsuleCollider) {
            var capsuleCollider = collider as CapsuleCollider;
            capsuleCollider.center = EditorGUILayout.Vector3Field("Center", capsuleCollider.center);
            capsuleCollider.radius = EditorGUILayout.FloatField("Radius", capsuleCollider.radius);
            capsuleCollider.height = EditorGUILayout.FloatField("Radius", capsuleCollider.height);
            if (mirror && mirroredCollider is CapsuleCollider) {
              var casted = mirroredCollider as CapsuleCollider;
              casted.center = Vector3.Scale(capsuleCollider.center, mirrorScale);
              casted.radius = capsuleCollider.radius;
              casted.height = capsuleCollider.height;
            }
          }
        }

        EditorGUILayout.EndVertical();


        EditorGUILayout.Space();
      }

      if (GUILayout.Button("Open Cloth Skirt Helper")) {
        ClothSkirtHelper.InitByAsset(new MenuCommand(cloth, 0));
      }
    }
  }
}
