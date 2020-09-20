namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  public class ClothSkirtHelperCore {
    public SkinnedMeshRenderer skirt;
    public Animator avatar;
    public Dictionary<HumanBodyBones, Transform> bones = new Dictionary<HumanBodyBones, Transform>();
    public Transform xzCenter;

    public bool applyRecommendedParameters = true;
    public bool advancedMode = false;

    public Mesh mesh;
    public Cloth cloth;

    public Vector3 center {
      get => new Vector3(xzCenter.position.x, avatar.transform.position.y, xzCenter.position.z);
    }

    public Vector3 worldCenter {
      get => mesh.bounds.center + center;
    }

    public Vector3 worldTop {
      get => worldCenter + new Vector3(0, mesh.bounds.extents.y, 0);
    }

    public Vector3 worldBottom {
      get => worldCenter + new Vector3(0, -mesh.bounds.extents.y, 0);
    }

    public IEnumerable<Vector3> worldVertices {
      get => mesh.vertices.Distinct().Select(v => v + avatar.transform.position);
    }

    public void OnEnable() {
      OnSkirtCanged();
    }

    public void BakeMesh() {
      mesh = new Mesh() {
        name = $"{skirt.sharedMesh.name}_baked",
      };
      skirt.BakeMesh(mesh);
    }

    private void OnAvatarChanged() {
      if (avatar == null) {
        bones.Clear();
        xzCenter = null;
      } else {
        bones = HumanoidUtility.boneIds
          .Select(boneId => new KeyValuePair<HumanBodyBones, Transform>(boneId, avatar.GetBoneTransform(boneId)))
          .ToDictionary(p => p.Key, p => p.Value);
        xzCenter = bones[HumanBodyBones.Hips];
      }
    }

    private void OnSkirtCanged() {
      if (skirt == null) {
        avatar = null;
        mesh = null;
      } else {
        avatar = skirt.bones.Select(HumanoidUtility.FindHumanoidAnimator).FirstOrDefault()?.GetComponent<Animator>();
        BakeMesh();
      }
      OnAvatarChanged();
    }

    public bool OnGUI() {
      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        using (var changeCheckScope = new EditorGUI.ChangeCheckScope()) {
          skirt = EditorGUILayout.ObjectField("Skirt", skirt, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
          if (changeCheckScope.changed) OnSkirtCanged();
        }


        if (skirt == null) return false;

        MeshUtility.MeshMetricsGUI(skirt);

        EditorGUILayout.Space();


        using (var changeCheckScope = new EditorGUI.ChangeCheckScope()) {
          avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(Animator), true) as Animator;
          if (changeCheckScope.changed) OnAvatarChanged();
        }

        if (avatar == null) return false;

        using (new EditorGUI.IndentLevelScope()) {
          HumanoidUtility.boneIds.ForEach(boneId => {
            bones[boneId] = EditorGUILayout.ObjectField(boneId.ToString(), bones.ContainsKey(boneId) ? bones[boneId] : null, typeof(Transform), true) as Transform;
          });
        }
      }

      EditorGUILayout.Space();

      var errors = Validate();
      if (errors.Count > 0) {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
          errors.ForEach(a => {
            using (new EditorGUILayout.HorizontalScope()) {
              EditorGUILayout.LabelField("Error", a.Item1);
              if (a.Item2 != null) {
                if (GUILayout.Button("Auto Fix", GUILayout.ExpandWidth(false))) a.Item2();
              }
            }
          });
        }
        EditorGUILayout.Space();

        return false;
      }

      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        applyRecommendedParameters = EditorGUILayout.Toggle("Apply Recommended", applyRecommendedParameters);
        advancedMode = EditorGUILayout.Toggle("Advanced Mode", advancedMode);

        var hips = bones.FirstOrDefault(p => p.Key == HumanBodyBones.Hips).Value;
        if (!advancedMode) {
          xzCenter = hips;
        } else {
          xzCenter = EditorGUILayout.ObjectField("XZ Center", xzCenter, typeof(Transform), true) as Transform ?? hips;
        }
      }

      if (skirt != null && mesh == null) {
        BakeMesh();
      }

      return true;
    }

    public void Execute() {
      cloth = skirt.GetComponent<Cloth>();
      if (cloth == null) {
        cloth = skirt.gameObject.AddComponent<Cloth>();
      }

      BakeMesh();

      if (applyRecommendedParameters) {
        cloth.stretchingStiffness = 0.8f;
        cloth.bendingStiffness = 0.8f;
        cloth.damping = 0.2f;
        cloth.worldVelocityScale = 0.0f;
        cloth.worldAccelerationScale = 0.0f;
        cloth.friction = 0.0f;
        cloth.sleepThreshold = 1.0f;
      }
    }

    private List<(string, Action)> Validate() {
      var errors = new List<(string, Action)>();

      if (mesh.vertices.Distinct().Count() > 1500) {
        errors.Add(("Too many vertices.", null));
      }

      if (skirt.transform.localPosition != Vector3.zero) {
        errors.Add(("Position of the skirt must be 0.", () => {
          skirt.transform.localPosition = Vector3.zero;
          OnSkirtCanged();
        }));
      }
      if (skirt.transform.localRotation != Quaternion.identity) {
        errors.Add(("Rotation of the skirt must be 0.", () => {
          skirt.transform.localRotation = Quaternion.identity;
          OnSkirtCanged();
        }));
      }
      if (skirt.transform.localScale != Vector3.one) {
        errors.Add(("Scale of the skirt must be 1.", () => {
          skirt.transform.localScale = Vector3.one;
          OnSkirtCanged();
        }));
      }

      return errors;
    }
  }
}
