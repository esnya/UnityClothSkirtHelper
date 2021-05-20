namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  public class ClothSkirtHelper : EditorWindow
  {
    [MenuItem("EsnyaTools/Cloth/Cloth Skirt Helper")]
    public static void Init()
    {
      var window = EditorWindow.GetWindow(typeof(ClothSkirtHelper)) as ClothSkirtHelper;
      window.Show();
    }
    public ClothSkirtHelperCore core = new ClothSkirtHelperCore();
    public ClothColliderCreator colliderCreator = new ClothColliderCreator();
    public ClothConstraintPainter constraintPainter= new ClothConstraintPainter();

    private void OnEnable() {
      titleContent = new GUIContent("Cloth Skirt Helper");
      core.OnEnable();
    }

    public Vector2 scroll;
    private void OnGUI() {
      var gizmos = ClothSkirtHelperGizmos.GetOrCreate();
      gizmos.drawGizmos = () => {
        if (core.skirt == null) return;
        constraintPainter.OnDrawGizmos(core);
      };

      using (var scrollScope = new EditorGUILayout.ScrollViewScope(scroll)) {
        scroll = scrollScope.scrollPosition;

        EditorGUILayout.Space();

        var valid = core.OnGUI();

        if (!valid || core.skirt == null || core.avatar == null || core.bones.Any(p => p.Value == null)) return;

        EditorGUILayout.Space();

        EditorGUILayout.Space();
        colliderCreator.OnGUI(core);
        EditorGUILayout.Space();

        constraintPainter.OnGUI(core);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledGroupScope(!valid)) {
          if (GUILayout.Button("Apply")) {
            Execute();
          }
        }
      }
    }

    private void Execute() {
      core.Execute();
      colliderCreator.Execute(core);
      constraintPainter.Execute(core);
    }
  }
}
