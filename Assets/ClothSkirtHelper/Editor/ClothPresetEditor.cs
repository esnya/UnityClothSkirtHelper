namespace EsnyaFactory
{
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [CustomEditor(typeof(ClothPreset))]
  public class ClothPresetEditor : Editor {
    private ClothSkinningCoefficient defaultCoefficient = new ClothSkinningCoefficient() {
      maxDistance = float.MaxValue,
      collisionSphereDistance = float.MaxValue,
    };

    public enum VertexMatchingMode {
      Index,
      Position,
    }

    public Cloth cloth;
    public bool onlyCoefficients;
    public VertexMatchingMode vertexMatchingMode;
    public ClothPreset presetToConcat;

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      EditorGUILayout.Space();

      var clothPreset = target as ClothPreset;

      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        EditorGUILayout.LabelField("Import/Export Tools", new GUIStyle() { fontStyle = FontStyle.Bold });

        cloth = EditorGUILayout.ObjectField("Cloth", cloth, typeof(Cloth), true) as Cloth;
        onlyCoefficients = EditorGUILayout.Toggle("Only Coefficients", onlyCoefficients);
        vertexMatchingMode = (VertexMatchingMode)EditorGUILayout.EnumPopup("Vertex Matching Mode", vertexMatchingMode);

        using (new EditorGUI.DisabledGroupScope(cloth == null || !cloth.enabled)) {
          using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("Import Preset from Cloth")) {
              if (!onlyCoefficients) {
                clothPreset.stretchingStiffness = cloth.stretchingStiffness;
                clothPreset.bendingStiffness = cloth.bendingStiffness;
                clothPreset.useTethers = cloth.useTethers;
                clothPreset.useGravity = cloth.useGravity;
                clothPreset.damping = cloth.damping;
                clothPreset.externalAcceleration = cloth.externalAcceleration;
                clothPreset.randomAcceleration = cloth.randomAcceleration;
                clothPreset.worldVelocityScale = cloth.worldVelocityScale;
                clothPreset.worldAccelerationScale = cloth.worldAccelerationScale;
                clothPreset.friction = cloth.friction;
                clothPreset.collisionMassScale = cloth.collisionMassScale;
                clothPreset.enableContinuousCollision = cloth.enableContinuousCollision;
                clothPreset.clothSolverFrequency = cloth.clothSolverFrequency;
                clothPreset.sleepThreshold = cloth.sleepThreshold;
              }

              clothPreset.vertices = cloth.vertices.ToList();
              clothPreset.normals = cloth.normals.ToList();
              clothPreset.coefficients = cloth.coefficients.Select(ClothPreset.Coefficient.Serialize).ToList();

              EditorUtility.SetDirty(clothPreset);
              AssetDatabase.Refresh();
            }
            if (GUILayout.Button("Apply Preset into Cloth")) {
              if (!onlyCoefficients) {
                cloth.stretchingStiffness = clothPreset.stretchingStiffness;
                cloth.bendingStiffness = clothPreset.bendingStiffness;
                cloth.useTethers = clothPreset.useTethers;
                cloth.useGravity = clothPreset.useGravity;
                cloth.damping = clothPreset.damping;
                cloth.externalAcceleration = clothPreset.externalAcceleration;
                cloth.randomAcceleration = clothPreset.randomAcceleration;
                cloth.worldVelocityScale = clothPreset.worldVelocityScale;
                cloth.worldAccelerationScale = clothPreset.worldAccelerationScale;
                cloth.friction = clothPreset.friction;
                cloth.collisionMassScale = clothPreset.collisionMassScale;
                cloth.enableContinuousCollision = clothPreset.enableContinuousCollision;
                cloth.clothSolverFrequency = clothPreset.clothSolverFrequency;
                cloth.sleepThreshold = clothPreset.sleepThreshold;
              }

              switch (vertexMatchingMode) {
                case VertexMatchingMode.Index:
                  cloth.coefficients = clothPreset
                    .coefficients
                    .Take(cloth.vertices.Length)
                    .Select(ClothPreset.Coefficient.Deserialize)
                    .Concat(Enumerable.Repeat(defaultCoefficient, cloth.vertices.Length - clothPreset.vertices.Count))
                    .ToArray();
                  break;
                case VertexMatchingMode.Position:
                  cloth.coefficients = Enumerable
                    .Range(0, cloth.vertices.Length)
                    .Select(i => {
                      var vertex = cloth.vertices[i];
                      var src = Enumerable
                        .Range(0, clothPreset.vertices.Count)
                        .OrderBy(j => Vector3.Distance(vertex, clothPreset.vertices[j]))
                        .FirstOrDefault();
                      return clothPreset.coefficients[src];
                    })
                    .Select(ClothPreset.Coefficient.Deserialize)
                    .ToArray();
                  break;
              }
            }

            if (cloth != null && !cloth.enabled) {
              EditorGUILayout.LabelField("Coth component must be enabled");
            }
          }
        }
      }

      EditorGUILayout.Space();

      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        EditorGUILayout.LabelField("Merge Tools", new GUIStyle() { fontStyle = FontStyle.Bold });
        presetToConcat = EditorGUILayout.ObjectField("Cloth Preset To Merge", presetToConcat, typeof(ClothPreset), true) as ClothPreset;
        using (new EditorGUI.DisabledGroupScope(presetToConcat == null)) {
          if (GUILayout.Button("Merge Coefficients")) {
            clothPreset.vertices = clothPreset.vertices.Concat(presetToConcat.vertices).ToList();
            clothPreset.normals = clothPreset.normals.Concat(presetToConcat.normals).ToList();
            clothPreset.coefficients = clothPreset.coefficients.Concat(presetToConcat.coefficients).ToList();
          }
        }
      }
    }
  }
}