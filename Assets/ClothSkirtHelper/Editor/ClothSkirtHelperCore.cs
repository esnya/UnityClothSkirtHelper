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
    public Transform xyCenter;

    public bool applyRecommendedParameters = true;
    public bool advancedMode = false;

    public Mesh mesh;
    public Cloth cloth;

    public Vector3 center {
      get => new Vector3(xyCenter.position.x, avatar.transform.position.y, xyCenter.position.z);
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
        xyCenter = null;
      } else {
        bones = HumanoidUtility.boneIds
          .Select(boneId => new KeyValuePair<HumanBodyBones, Transform>(boneId, avatar.GetBoneTransform(boneId)))
          .ToDictionary(p => p.Key, p => p.Value);
        xyCenter = bones[HumanBodyBones.Hips];
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

    public void OnGUI() {
      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        using (var changeCheckScope = new EditorGUI.ChangeCheckScope()) {
          skirt = EditorGUILayout.ObjectField("Skirt", skirt, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
          if (changeCheckScope.changed) OnSkirtCanged();
        }

        if (skirt == null) return;
        MeshUtility.MeshMetricsGUI(skirt);

        EditorGUILayout.Space();

        using (var changeCheckScope = new EditorGUI.ChangeCheckScope()) {
          avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(Animator), true) as Animator;
          if (changeCheckScope.changed) OnAvatarChanged();
        }

        if (avatar == null) return;
        using (new EditorGUI.IndentLevelScope()) {
          HumanoidUtility.boneIds.ForEach(boneId => {
            bones[boneId] = EditorGUILayout.ObjectField(boneId.ToString(), bones.ContainsKey(boneId) ? bones[boneId] : null, typeof(Transform), true) as Transform;
          });
        }
      }

      EditorGUILayout.Space();

      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        applyRecommendedParameters = EditorGUILayout.Toggle("Apply Recommended", applyRecommendedParameters);
        advancedMode = EditorGUILayout.Toggle("Advanced Mode", advancedMode);

        var hips = bones.FirstOrDefault(p => p.Key == HumanBodyBones.Hips).Value;
        if (!advancedMode) {
          xyCenter = hips;
        } else {
          xyCenter = EditorGUILayout.ObjectField("XZ Center", xyCenter, typeof(Transform), true) as Transform ?? hips;
        }
      }

      if (skirt != null && mesh == null) {
        BakeMesh();
      }
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
  }
}
