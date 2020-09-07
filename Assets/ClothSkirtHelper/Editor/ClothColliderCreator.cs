namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  public class ClothColliderCreator {
    private static Dictionary<HumanBodyBones, ColliderOption> initialColliders = new Dictionary<HumanBodyBones, ColliderOption>() {
      { HumanBodyBones.Spine, new ColliderOption() { pairs = { (HumanBodyBones.Hips, false), (HumanBodyBones.LeftUpperLeg, true), (HumanBodyBones.RightUpperLeg, true) } }},
      { HumanBodyBones.Hips, new ColliderOption() { sphere = true, pairs = { (HumanBodyBones.LeftUpperLeg, true), (HumanBodyBones.RightUpperLeg, true) } }},
      { HumanBodyBones.LeftUpperLeg, new ColliderOption() { sphere = true, pairs = { (HumanBodyBones.LeftLowerLeg, true) } }},
      { HumanBodyBones.LeftLowerLeg, new ColliderOption() { pairs = { (HumanBodyBones.LeftFoot, true) } }},
      { HumanBodyBones.LeftHand, new ColliderOption() },
    };

    private bool createColliders = true;
    private float initialRadius = 0.1f;
    private class ColliderOption {
      public bool visible = false;
      public bool sphere = false;
      public bool capsule = false;
      public List<(HumanBodyBones, bool)> pairs = new List<(HumanBodyBones, bool)>() {};
    }

    private Dictionary<HumanBodyBones, ColliderOption> colliders = new Dictionary<HumanBodyBones, ColliderOption>() {
      { HumanBodyBones.Spine, new ColliderOption() { pairs = { (HumanBodyBones.Hips, false), (HumanBodyBones.LeftUpperLeg, true), (HumanBodyBones.RightUpperLeg, true) } }},
      { HumanBodyBones.Hips, new ColliderOption() { sphere = true, pairs = { (HumanBodyBones.LeftUpperLeg, true), (HumanBodyBones.RightUpperLeg, true) } }},
      { HumanBodyBones.LeftUpperLeg, new ColliderOption() { sphere = true, pairs = { (HumanBodyBones.LeftLowerLeg, true) } }},
      { HumanBodyBones.LeftLowerLeg, new ColliderOption() { pairs = { (HumanBodyBones.LeftFoot, true) } }},
      { HumanBodyBones.LeftHand, new ColliderOption() },
    };

    public void OnGUI(ClothSkirtHelperCore core) {

      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        createColliders = EditorGUILayout.Toggle("Create Colliders", createColliders);

        initialRadius = EditorGUILayout.FloatField("Initial Radius", initialRadius);

        if (!core.advancedMode) {
          HumanoidUtility.boneIds.Where(b => !HumanoidUtility.IsRight(b)).ToList().ForEach(boneId => {
            if (!colliders.ContainsKey(boneId)) return;

            if (boneId == HumanBodyBones.LeftLowerLeg) {
              colliders[boneId].sphere = true;
              colliders[boneId].capsule = false;
              colliders[boneId].pairs = new List<(HumanBodyBones, bool)>() { (HumanBodyBones.LeftFoot, EditorGUILayout.Toggle("LowerLeg Collider", colliders[boneId].pairs[0].Item2)) };
            } else {
              colliders[boneId] = initialColliders[boneId];
              colliders[boneId].pairs = initialColliders[boneId].pairs.ToList();
            }
          });
        } else {
          EditorGUILayout.Space();

          HumanoidUtility.boneIds.Where(b => !HumanoidUtility.IsRight(b)).ToList().ForEach(boneId => {
            if (!colliders.ContainsKey(boneId)) return;

            colliders[boneId].visible = EditorGUILayout.Foldout(colliders[boneId].visible, boneId.ToString().Replace("Left", ""));
            if (!colliders[boneId].visible) return;

            using (new EditorGUI.IndentLevelScope()) {
              colliders[boneId].sphere = EditorGUILayout.Toggle("Sphere Collider", colliders[boneId].sphere);

              if (colliders[boneId].pairs.Count > 0 && colliders[boneId].sphere) {
                EditorGUILayout.LabelField("Pair With");
                using (new EditorGUI.IndentLevelScope()) {
                  colliders[boneId].pairs = colliders[boneId].pairs
                    .Select(pair => {
                      if (HumanoidUtility.IsRight(pair.Item1)) {
                        return (pair.Item1, colliders[boneId].pairs.First(p =>HumanoidUtility.ToRight(p.Item1) == pair.Item1).Item2);
                      }
                      return (pair.Item1, EditorGUILayout.Toggle(pair.Item1.ToString().Replace("Left", ""), pair.Item2));
                    })
                    .ToList();
                }
                EditorGUILayout.Space();
              }

              colliders[boneId].capsule = EditorGUILayout.Toggle("Capsule Collider", colliders[boneId].capsule);
            }

            EditorGUILayout.Space();
          });
        }
      }
    }

    public static SphereCollider GetOrCreateCollider(HumanBodyBones boneId, Animator avatar, float initialRadius) {
      var name = $"Skirt_{boneId}_SphereCollider";

      var bone = avatar.GetBoneTransform(boneId);
      var t = bone.Find(name);
      if (t == null) {
        t = new GameObject(name).transform;
        t.SetParent(bone);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(t.gameObject, "Create Collider");
      }

      var collider = t.GetComponent<SphereCollider>();
      if (collider == null) {
        collider = t.gameObject.AddComponent<SphereCollider>();
        collider.radius = initialRadius;
      }

      return collider;
    }

    public void Execute(ClothSkirtHelperCore core) {
      if (!createColliders) return;

      var cloth = core.cloth;
      var avatar = core.avatar;

      var mirrored = colliders.Concat(
        colliders
          .Where(c => HumanoidUtility.IsLeft(c.Key))
          .Select(c => {
            return new KeyValuePair<HumanBodyBones, ColliderOption>(
              HumanoidUtility.ToRight(c.Key),
              new ColliderOption() {
                capsule = c.Value.capsule,
                pairs = c.Value.pairs.Select(p => (HumanoidUtility.ToRight(p.Item1), p.Item2)).ToList(),
                sphere = c.Value.sphere,
                visible = c.Value.sphere,
              }
            );
          })
      );

      cloth.capsuleColliders = mirrored
        .Where(c => c.Value.capsule)
        .Select(p => {
          var bone = avatar.GetBoneTransform(p.Key);

          var name = $"Skirt_{p.Key}_CapsuleCollider";
          var t = bone.Find(name);
          if (t == null) {
            t = new GameObject(name).transform;
            t.SetParent(bone);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
          }

          var c = t.GetComponent<CapsuleCollider>();
          if (c == null) {
            c = t.gameObject.AddComponent<CapsuleCollider>();
            c.radius = initialRadius;
          }

          return c;
        })
        .ToArray();

      cloth.sphereColliders = mirrored
        .Where(c => c.Value.sphere)
        .SelectMany(p => {
          var collider = GetOrCreateCollider(p.Key, avatar, initialRadius);
          var pairs = p.Value.pairs.Where(a => a.Item2).Select(a => GetOrCreateCollider(a.Item1, avatar, initialRadius)).ToList();
          if (pairs.Count == 0) return Enumerable.Repeat(new ClothSphereColliderPair(collider), 1);

          return pairs.Select(a => new ClothSphereColliderPair(collider, a));
        })
        .ToArray();

      cloth.sphereColliders
        .SelectMany(p => new Collider[] { p.first, p.second })
        .Concat(cloth.capsuleColliders)
        .Where(c => c != null)
        .Distinct()
        .ToList()
        .ForEach(c => {
          c.isTrigger = true;
        });
    }
  }
}
