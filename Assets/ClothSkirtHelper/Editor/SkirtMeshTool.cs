namespace EsnyaFactory.ClothSkirtHelper {
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  public class SkirtMeshTool : EditorWindow {
    [MenuItem("EsnyaTools/Skirt Mesh Tool")]
    private static void ShowWindow() {
      var window = GetWindow<SkirtMeshTool>();
      window.Show();
    }

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private string outputDirectory = "Assets";
    private Vector2 scroll;
    private List<ToolBase> tools = new List<ToolBase>() {
      new MeshExtractor(),
      new InsideDeleter(),
      new MeshCombiner(),
      new MeshSpreadingDeformer(),
    };

    private void OnEnable()
    {
      titleContent = new GUIContent("Skirt Mesh Tool");
    }

    private void OnGUI() {
      EditorGUILayout.Space();

      using (var scrollScope = new EditorGUILayout.ScrollViewScope(scroll)) {
        scroll = scrollScope.scrollPosition;
        using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
          skinnedMeshRenderer = EditorGUILayout.ObjectField("Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
          MeshUtility.MeshMetricsGUI(skinnedMeshRenderer);
          using (new EditorGUILayout.HorizontalScope()) {
            outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
            if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false))) {
              var newDir = EditorUtility.SaveFolderPanel(titleContent.text, outputDirectory.Replace("Assets", Application.dataPath), "Mesh")?.Replace(Application.dataPath, "Assets");
              if (!string.IsNullOrEmpty(newDir)) {
                outputDirectory = newDir;
              }
            }
          }
        }

        EditorGUILayout.Space();

        if (skinnedMeshRenderer != null) {
          tools.ForEach(tool => {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
              EditorGUILayout.LabelField(tool.GetType().Name, new GUIStyle() { fontStyle = FontStyle.Bold });
              EditorGUILayout.Space();
              tool.OnGUI(skinnedMeshRenderer);
              EditorGUILayout.Space();

              using (new EditorGUI.DisabledGroupScope(!tool.Validate())) {
                if (GUILayout.Button("Execute")) {
                  Undo.RecordObject(this, $"Execute {tool.GetType()}");
                  skinnedMeshRenderer = tool.Execute(skinnedMeshRenderer, outputDirectory);
                }
              }
            }
            EditorGUILayout.Space();
          });
        }
      }
    }

  }
}
