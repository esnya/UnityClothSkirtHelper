#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace EsnyaFactory {
  public class ClothSkirtHelper : EditorWindow
  {
    [MenuItem("EsnyaFactory/Cloth Skirt Helper")]
    static void Init()
    {
        var window = EditorWindow.GetWindow(typeof(ClothSkirtHelper)) as ClothSkirtHelper;
        window.Show();
    }

    [MenuItem("CONTEXT/Cloth/Cloth Skirt Helper")]
    static void InitByAsset(MenuCommand menuCommand)
    {
      var window = EditorWindow.GetWindow(typeof(ClothSkirtHelper)) as ClothSkirtHelper;
      window.Show();

      if (menuCommand.context is Cloth) {
        window.cloth = menuCommand.context as Cloth;
      }
    }

    Animator avatarAnimator;
    Cloth cloth;

    readonly HumanBodyBones[] boneIds = {
      HumanBodyBones.Hips,
      HumanBodyBones.LeftUpperLeg,
      HumanBodyBones.LeftLowerLeg,
      HumanBodyBones.LeftFoot,
      HumanBodyBones.RightUpperLeg,
      HumanBodyBones.RightLowerLeg,
      HumanBodyBones.RightFoot,
    };
    readonly HumanBodyBones[][] colliderPairs = {
      new HumanBodyBones[]{HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg},
      new HumanBodyBones[]{HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg},
      new HumanBodyBones[]{HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot},
      new HumanBodyBones[]{HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg},
      new HumanBodyBones[]{HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg},
      new HumanBodyBones[]{HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot},
    };

    Transform[] bones;

    bool removeRootBone = false;
    bool lowerLegColliders = false;
    float initialColliderRadius = 0.04f;
    bool applyRecommendedParameters = true;
    float fixedHeight = 0.1f;

    bool isRunning = false;
    int delay = 1000;

    void OnGUI()
    {
      titleContent = new GUIContent("Cloth Helper");

      avatarAnimator = EditorGUILayout.ObjectField("Avatar", avatarAnimator, typeof(Animator), true) as Animator;
      cloth = EditorGUILayout.ObjectField("Cloth", cloth, typeof(Cloth), true) as Cloth;

      EditorGUILayout.Space();

      if (bones == null || bones.Length != boneIds.Length) {
        bones = new Transform[boneIds.Length];
      }

      for (int i = 0; i < boneIds.Length; i++) {
        bones[i] = EditorGUILayout.ObjectField(boneIds[i].ToString(), bones[i], typeof(Transform), true) as Transform;
      }

      EditorGUI.BeginDisabledGroup(avatarAnimator == null);
      if (GUILayout.Button("Auto Detect")) {
        AutoDetectBones();
      }
      EditorGUI.EndDisabledGroup();

      EditorGUILayout.Space();

      applyRecommendedParameters  = EditorGUILayout.Toggle("Apply Recommended Parameters", applyRecommendedParameters);
      removeRootBone  = EditorGUILayout.Toggle("Remove Root Bone", removeRootBone);
      lowerLegColliders  = EditorGUILayout.Toggle("Lower Leg Colliders", lowerLegColliders);
      initialColliderRadius  = EditorGUILayout.FloatField("Initial Colldier Radius", initialColliderRadius);
      fixedHeight  = EditorGUILayout.FloatField("Fixed Height", fixedHeight);

      EditorGUI.BeginDisabledGroup(avatarAnimator == null || cloth == null || bones.Any(a => a == null) || isRunning);
      if (GUILayout.Button(isRunning ? "Setup is running" : "Setup")) {
        Setup();
      }

      EditorGUI.EndDisabledGroup();
    }

    int GetIndexById(HumanBodyBones boneId)
    {
      return boneIds.Select((value, i) => new { value, i }).First((a) => a.value == boneId).i;
    }

    SkinnedMeshRenderer GetClothRenderer()
    {
      return cloth.GetComponent<SkinnedMeshRenderer>();
    }

    void AutoDetectBones()
    {
      for (int i = 0; i < bones.Length; i++) {
        var boneId = boneIds[i];
        bones[i] = avatarAnimator.GetBoneTransform(boneId);
      }
    }

    bool CheckColliderEnabled(HumanBodyBones boneId)
    {
      var isFoot = boneId == HumanBodyBones.LeftFoot || boneId == HumanBodyBones.RightFoot;
      return !isFoot || lowerLegColliders;
    }

    void AddColliders()
    {
      for (int i = 0; i < bones.Length; i++) {
        var boneId = boneIds[i];
        var bone = bones[i];
        if (CheckColliderEnabled(boneId)) {
          foreach (var existingCollider in bone.GetComponents<Collider>()) {
            DestroyImmediate(existingCollider);
          }

          var collider = bone.gameObject.AddComponent<SphereCollider>();
          collider.isTrigger = true;
          collider.radius = initialColliderRadius;
        }
      }
    }

    void RemoveRootBone()
    {
      cloth.GetComponent<SkinnedMeshRenderer>().rootBone = null;
    }

    void SetupColliders()
    {
      var list = new List<ClothSphereColliderPair>();
      foreach (var idPair in colliderPairs) {
        if (CheckColliderEnabled(idPair[0]) && CheckColliderEnabled(idPair[1])) {
          var firstIndex = GetIndexById(idPair[0]);
          var secondIndex = GetIndexById(idPair[1]);
          var pair = new ClothSphereColliderPair(
            bones[firstIndex].GetComponent<SphereCollider>(),
            bones[secondIndex].GetComponent<SphereCollider>()
          );
          list.Add(pair);
        }
      }
      cloth.sphereColliders = list.ToArray();
    }

    async Task SetupMaxDistance()
    {
      var clothRender = GetClothRenderer();
      var rootBone = clothRender.rootBone;
      clothRender.rootBone = null;

      await Task.Delay(delay);
      var hipsY = bones[GetIndexById(HumanBodyBones.Hips)].position.y;
      var fixedLimit = hipsY - fixedHeight;

      var matrix = cloth.transform.localToWorldMatrix;

      cloth.coefficients = cloth.vertices.Select(vertex => {
        var coefficient = new ClothSkinningCoefficient();

        var worldPosition = matrix.MultiplyVector(vertex);

        if (worldPosition.y >= fixedLimit) {
          coefficient.maxDistance = 0;
        } else {
          coefficient.maxDistance = float.MaxValue;
        }

        coefficient.collisionSphereDistance = float.MaxValue;

        return coefficient;
      }).ToArray();

      await Task.Delay(delay);
      clothRender.rootBone = rootBone;
    }

    void ApplyRecommendedParameters()
    {
      cloth.stretchingStiffness = 0.5f;
      cloth.bendingStiffness = 0.5f;
      cloth.damping = 0.2f;
      cloth.worldVelocityScale = 0.0f;
      cloth.worldAccelerationScale = 0.0f;
      cloth.friction = 0.0f;
      cloth.sleepThreshold = 1.0f;
    }

    async Task Reselect()
    {
      Selection.activeObject = null;
      await Task.Delay(delay);
      Selection.activeObject = cloth.gameObject;
    }

    async void Setup()
    {
      try {
        isRunning = true;
        Repaint();

        await Reselect();

        cloth.ClearTransformMotion();

        AddColliders();

        if (removeRootBone) {
          RemoveRootBone();
        }

        SetupColliders();

        await SetupMaxDistance();

        if (applyRecommendedParameters) {
          ApplyRecommendedParameters();
        }

        await Reselect();
      } finally {
        isRunning = false;
      }
    }
  }
}
#endif
