namespace EsnyaFactory
{
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  [CreateAssetMenu(fileName = "ClothPreset", menuName = "EsnyaTools/ClothPreset", order = 0)]
  public class ClothPreset : ScriptableObject {
    [System.Serializable]
    public class CoefficientPreset {
      public float maxDistance;
      public float collisionSphereDistance;

      static public CoefficientPreset Serialize(ClothSkinningCoefficient c) {
        return new CoefficientPreset() {
          maxDistance = c.maxDistance,
          collisionSphereDistance = c.collisionSphereDistance,
        };
      }

      static public ClothSkinningCoefficient Deserialize(CoefficientPreset c) {
        return new ClothSkinningCoefficient() {
          maxDistance = c.maxDistance,
          collisionSphereDistance = c.collisionSphereDistance,
        };
      }
    }

    [System.Serializable]
    public class SphereColliderPreset {
      public HumanBodyBones bone;
      
      public Vector3 position;
      public Quaternion rotation;
      public Vector3 scale;

      public Vector3 center;
      public float radius;

      static public SphereColliderPreset Serialize(SphereCollider sphereCollider, IDictionary<Transform, HumanBodyBones> boneTable)
      {
        var t1 = sphereCollider.transform;
        var t2 = boneTable.ContainsKey(t1) ? t1 : t1.parent;
        
        if (!boneTable.ContainsKey(t2)) return null;

        return new SphereColliderPreset() {
          bone = boneTable[t2],
          position = t2.localPosition,
          rotation = t2.localRotation,
          scale = t2.localScale,
          center = sphereCollider.center,
          radius = sphereCollider.radius,
        };
      }
    }

    public float stretchingStiffness;
    public float bendingStiffness;
    public bool useTethers;
    public bool useGravity;
    public float damping;

    public Vector3 externalAcceleration;
    public Vector3 randomAcceleration;
    public float worldVelocityScale;
    public float worldAccelerationScale;
    public float friction;
    public float collisionMassScale;
    public bool enableContinuousCollision;
    public bool useVirtualParticles;
    public float clothSolverFrequency;
    public float sleepThreshold;
    // ToDo: Cupsle Colliders
    public List<(SphereColliderPreset, SphereColliderPreset)> sphereColliders;
    public List<Vector3> virtualParticleWeigts;
    // ToDo: Self collision
    // public float selfCollisionDistance;
    // public float selfCollisionStiffnes;
    // public bool selfCollision;

    public List<Vector3> vertices;
    public List<Vector3> normals;
    public List<CoefficientPreset> coefficients;
    // public float stiffnessFrequency; ??
  }
}
