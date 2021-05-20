using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Timeline;

namespace EsnyaFactory.UnityClothSkirtHelper
{
  public class ClothFromVertexColor : EditorWindow {

    [MenuItem("EsnyaTools/Cloth/From VertexColor")]
    private static void ShowWindow() {
      var window = GetWindow<ClothFromVertexColor>();
      window.titleContent = new GUIContent("Cloth From VertexColor");
      window.Show();
    }

    public Cloth cloth;
    private Mesh mesh {
      get {
        return cloth?.GetComponent<SkinnedMeshRenderer>()?.sharedMesh;
      }
    }

    public float mdScale = 1.0f, spScale = 1.0f;

    private void DrawClothGUI()
    {
      using (new EditorGUILayout.HorizontalScope())
      {
        cloth = EditorGUILayout.ObjectField("Cloth", cloth, typeof(Cloth), true) as Cloth;

        var selectedCloth = Selection.activeGameObject?.GetComponent<Cloth>();
        using (new EditorGUI.DisabledGroupScope(selectedCloth == null))
        {
          if (GUILayout.Button("From Scene Selection", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))cloth = selectedCloth;
        }
      }
    }

    private void DrawVertexColorsGUI()
    {
      if (cloth == null) {
        EditorGUILayout.LabelField("Select Cloth Component");
        return;
      }
      if (!cloth.enabled) {
        EditorGUILayout.LabelField("Enable Cloth Component");
        return;
      }
      if (mesh == null) {
        EditorGUILayout.LabelField("Need to assign mesh to SkinnedMeshRenderer");
        return;
      }
      if (mesh.colors == null || mesh.colors.Length != mesh.vertices.Length)
      {
        EditorGUILayout.LabelField("Mesh must includes vertex colors");
        return;
      }

      EditorGUILayout.LabelField("Scale");
      using (new EditorGUI.IndentLevelScope())
      {
        mdScale = EditorGUILayout.FloatField("Max Distance", mdScale);
        spScale = EditorGUILayout.FloatField("Surface Penetration", spScale);
      }

      if (GUILayout.Button("Apply"))
      {
        Apply();
      }
    }

    private void OnGUI() {
      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) DrawClothGUI();
      EditorGUILayout.Space();
      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) DrawVertexColorsGUI();
    }

    private void Apply()
    {
      var vertexColors = mesh.vertices.Select((vertex, index) => (vertex, index)).GroupBy(t => t.vertex, t => t.index).Select(g => mesh.colors[g.First()]).ToArray();

      if (vertexColors.Length != cloth.vertices.Length) {
        Debug.LogError("Vertex count mismatched!!");
      }

      cloth.coefficients = vertexColors.Select(c => new ClothSkinningCoefficient() {
        maxDistance = c.r * mdScale,
        collisionSphereDistance = c.g * spScale,
      }).ToArray();
      EditorUtility.SetDirty(cloth);
    }
  }
}
