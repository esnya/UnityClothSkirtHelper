namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  public class ClothSkirtHelper : EditorWindow
  {
    [MenuItem("EsnyaTools/Cloth Skirt Helper")]
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

    private List<(string, Action)> Validate() {
      var errors = new List<(string, Action)>();

      if (core.mesh.vertices.Distinct().Count() > 1000) {
        errors.Add(("Too many vertices.", null));
      }

      if (core.skirt.transform.localScale != Vector3.one) {
        errors.Add(("Scale of the skirt must be 1.", () => {
          core.skirt.transform.localScale = Vector3.one;
        }));
      }

      return errors;
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

        core.OnGUI();

        if (core.skirt == null || core.avatar == null || core.bones.Any(p => p.Value == null)) return;

        EditorGUILayout.Space();


        EditorGUILayout.Space();
        colliderCreator.OnGUI(core);
        EditorGUILayout.Space();

        constraintPainter.OnGUI(core);

        EditorGUILayout.Space();

        var errors = Validate();
        if (errors.Count > 0) {
          using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
            errors.ForEach(a => {
              using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Error", a.Item1);
                if (a.Item2 != null) {
                  if (GUILayout.Button("Fix", GUILayout.ExpandWidth(false))) a.Item2();
                }
              }
            });
          }
          EditorGUILayout.Space();
        }

        using (new EditorGUI.DisabledGroupScope(errors.Count != 0)) {
          if (GUILayout.Button("Execute")) {
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
