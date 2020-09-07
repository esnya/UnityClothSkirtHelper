namespace EsnyaFactory.ClothSkirtHelper {
  using System.Collections.Generic;
  using UnityEngine;
  public class HumanoidUtility {
    public static Transform FindHumanoidAnimator(Transform t) {
      if (t == null) return null;

      var animator = t.GetComponent<Animator>();
      if (animator != null && animator.isHuman) return t;

      return FindHumanoidAnimator(t.parent);
    }

    public static List<HumanBodyBones> boneIds = new List<HumanBodyBones>() {
      HumanBodyBones.Spine,
      HumanBodyBones.Hips,
      HumanBodyBones.LeftUpperLeg,
      HumanBodyBones.LeftLowerLeg,
      HumanBodyBones.LeftFoot,
      HumanBodyBones.RightUpperLeg,
      HumanBodyBones.RightLowerLeg,
      HumanBodyBones.RightFoot,
      HumanBodyBones.LeftHand,
      HumanBodyBones.RightHand,
    };
    public static bool IsRight(HumanBodyBones boneId) {
      switch (boneId) {
        case HumanBodyBones.RightUpperLeg:
        case HumanBodyBones.RightLowerLeg:
        case HumanBodyBones.RightFoot:
        case HumanBodyBones.RightHand:
          return true;
        default:
          return false;
      }
    }
    public static bool IsLeft(HumanBodyBones boneId) {
      switch (boneId) {
        case HumanBodyBones.LeftUpperLeg:
        case HumanBodyBones.LeftLowerLeg:
        case HumanBodyBones.LeftFoot:
        case HumanBodyBones.LeftHand:
          return true;
        default:
          return false;
      }
    }

    public static HumanBodyBones ToRight(HumanBodyBones boneId) {
      switch (boneId) {
        case HumanBodyBones.LeftUpperLeg:
          return HumanBodyBones.RightUpperLeg;
        case HumanBodyBones.LeftLowerLeg:
          return HumanBodyBones.RightLowerLeg;
        case HumanBodyBones.LeftFoot:
          return HumanBodyBones.RightFoot;
        case HumanBodyBones.LeftHand:
          return HumanBodyBones.RightHand;
        default:
          return boneId;
      }
    }
  }
}
