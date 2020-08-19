#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace EsnyaFactory {
  public class ClothSkirtHelper : EditorWindow
  {
    [MenuItem("EsnyaTools/Cloth Skirt Helper")]
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

    public enum ColliderMode {
      AttachToBone,
      CreateNewObject,
      None,
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
    ColliderMode colliderMode = ColliderMode.AttachToBone;
    float initialColliderRadius = 0.04f;
    bool applyRecommendedParameters = true;
    float fixedHeight = 0.1f;

    bool isRunning = false;
    int delay = 1000;

    bool advancedMode = false;
    bool fillConstraints = false;
    float waistRadius = 0.18f;
    float spreadAngle = 30.0f;
    float constraintBlending = 0.0f;
    float constraintBias = 0.05f;

    void OnGUI()
    {
      titleContent = new GUIContent("Cloth Skirt Helper");

      avatarAnimator = EditorGUILayout.ObjectField("Avatar", avatarAnimator, typeof(Animator), true) as Animator;
      cloth = EditorGUILayout.ObjectField("Cloth", cloth, typeof(Cloth), true) as Cloth;

      EditorGUILayout.Space();

      EditorGUILayout.BeginVertical(GUI.skin.box);
      EditorGUILayout.LabelField("Bones", new GUIStyle(){ fontStyle = FontStyle.Bold });
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
      EditorGUILayout.EndVertical();

      EditorGUILayout.Space();

      EditorGUILayout.BeginVertical(GUI.skin.box);
      EditorGUILayout.LabelField("Basic Options", new GUIStyle(){ fontStyle = FontStyle.Bold });
      applyRecommendedParameters  = EditorGUILayout.Toggle("Apply Recommended Parameters", applyRecommendedParameters);
      removeRootBone  = EditorGUILayout.Toggle("Remove Root Bone", removeRootBone);
      lowerLegColliders  = EditorGUILayout.Toggle("Lower Leg Colliders", lowerLegColliders);
      colliderMode = (ColliderMode)EditorGUILayout.EnumPopup("Collider Creation Mode", colliderMode);
      initialColliderRadius  = EditorGUILayout.FloatField("Initial Colldier Radius", initialColliderRadius);
      fixedHeight  = EditorGUILayout.FloatField("Fixed Height", fixedHeight);
      EditorGUILayout.EndVertical();

      EditorGUILayout.Space();

      EditorGUILayout.BeginVertical(GUI.skin.box);
      advancedMode = EditorGUILayout.Toggle("Advanced Mode", advancedMode);

      delay = advancedMode ? EditorGUILayout.IntField("Editor Delay (ms)", delay) : 1000;

      if (advancedMode) {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        fillConstraints = EditorGUILayout.Toggle("Fill Constraints", fillConstraints);
        if (fillConstraints) {
          EditorGUILayout.BeginVertical(GUI.skin.box);
          EditorGUILayout.LabelField("Loose Constraint", new GUIStyle(){ fontStyle = FontStyle.Bold });
          waistRadius = EditorGUILayout.FloatField("Waist Radius", waistRadius);
          spreadAngle = EditorGUILayout.Slider("Spread Angle", spreadAngle, 0.0f, 90.0f);
          EditorGUILayout.EndVertical();

          EditorGUILayout.BeginVertical(GUI.skin.box);
          EditorGUILayout.LabelField("Hard Constraint", new GUIStyle(){ fontStyle = FontStyle.Bold });
          constraintBias = EditorGUILayout.FloatField("Hard Constraint Bias", constraintBias);
          EditorGUILayout.EndVertical();

          constraintBlending = EditorGUILayout.Slider("Loose <-> Hard", constraintBlending, 0.0f, 1.0f);

        }
        EditorGUILayout.EndVertical();
      } else {
        fillConstraints = false;
      }
      EditorGUILayout.EndVertical();

      var scaleDiff = cloth != null ? cloth.transform.localScale - Vector3.one : Vector3.zero;
      var scaleInvalid = Mathf.Max(Mathf.Abs(scaleDiff.x), Mathf.Abs(scaleDiff.y), Mathf.Abs(scaleDiff.z)) > 0.01f;

      if (scaleInvalid) {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Error", new GUIStyle() { fontStyle = FontStyle.Bold });
        EditorGUILayout.LabelField("Scale of cloth object must be (1, 1, 1)");
        if (GUILayout.Button("Auto Fix")) {
          cloth.transform.localScale = Vector3.one;
        }
        EditorGUILayout.EndVertical();
      }

      EditorGUILayout.Space();

      EditorGUI.BeginDisabledGroup(avatarAnimator == null || cloth == null || bones.Any(a => a == null) || isRunning || scaleInvalid);
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

    Transform GetColliderObject(int index)
    {
      if (colliderMode == ColliderMode.None) {
        return null;
      }

      var bone = bones[index];
      if (colliderMode == ColliderMode.AttachToBone) {
        return bone;
      }

      var boneId = boneIds[index];
      var name = $"SkirtCollider_{boneId}";
      var existing = bone.Find(name);
      if (existing) {
        return existing;
      }

      var colliderObject = new GameObject(name);
      Undo.RegisterCreatedObjectUndo(colliderObject, "Create Collider GameObject");
      colliderObject.transform.SetParent(bone);
      colliderObject.transform.localPosition = Vector3.zero;
      colliderObject.transform.localRotation = Quaternion.identity;
      colliderObject.transform.localScale = Vector3.one;

      return colliderObject.transform;
    }

    void AddColliders()
    {
      for (int i = 0; i < bones.Length; i++) {
        var boneId = boneIds[i];
        if (CheckColliderEnabled(boneId)) {
          var colliderObject = GetColliderObject(i);
          foreach (var existingCollider in colliderObject.GetComponents<Collider>()) {
            DestroyImmediate(existingCollider);
          }

          var collider = colliderObject.gameObject.AddComponent<SphereCollider>();
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
            GetColliderObject(firstIndex).GetComponent<SphereCollider>(),
            GetColliderObject(secondIndex).GetComponent<SphereCollider>()
          );
          list.Add(pair);
        }
      }
      cloth.sphereColliders = list.ToArray();
    }

    ClothSkinningCoefficient ComputeCoefficient(Vector3 vertex, float fixedLimit)
    {
      var worldPosition = cloth.transform.TransformPoint(vertex.x, vertex.y, vertex.z);

      var coefficient = new ClothSkinningCoefficient();
      coefficient.collisionSphereDistance = float.MaxValue;

      if (fillConstraints) {
        if (worldPosition.y >= fixedLimit) {
          coefficient.maxDistance = 0;
        } else {
          var h = Mathf.Abs(fixedLimit - worldPosition.y);
          var r = h * Mathf.Sin(spreadAngle * Mathf.Deg2Rad);
          var looseDistance = r + waistRadius;
          var hardDistance = Mathf.Max(new Vector3(worldPosition.x, 0, worldPosition.z).magnitude - constraintBias, 0);
          coefficient.maxDistance = Mathf.Lerp(looseDistance, hardDistance, constraintBlending);
        }
      } else {
        if (worldPosition.y >= fixedLimit) {
          coefficient.maxDistance = 0;
        } else {
          coefficient.maxDistance = float.MaxValue;
        }
      }

      return coefficient;
    }

    async Task SetupMaxDistance()
    {
      var clothRender = GetClothRenderer();
      var rootBone = clothRender.rootBone;
      clothRender.rootBone = null;

      await Task.Delay(delay);
      var hipsY = bones[GetIndexById(HumanBodyBones.Hips)].position.y;
      var fixedLimit = hipsY - fixedHeight;

      cloth.coefficients = cloth.vertices.Select(vertex => ComputeCoefficient(vertex, fixedLimit)).ToArray();

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

        await Reselect();

        cloth.ClearTransformMotion();

        if (colliderMode != ColliderMode.None) {
          AddColliders();
          SetupColliders();
        }

        if (removeRootBone) {
          RemoveRootBone();
        }


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
