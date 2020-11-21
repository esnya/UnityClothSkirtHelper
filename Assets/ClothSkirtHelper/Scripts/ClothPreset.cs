namespace EsnyaFactory
{
  using System.Collections.Generic;
  using UnityEngine;

  [CreateAssetMenu(fileName = "ClothPreset", menuName = "EsnyaTools/ClothPreset", order = 0)]
  public class ClothPreset : ScriptableObject {
    [System.Serializable]
    public class Coefficient {
      public float maxDistance;
      public float collisionSphereDistance;

      static public Coefficient Serialize(ClothSkinningCoefficient c) {
        return new Coefficient() {
          maxDistance = c.maxDistance,
          collisionSphereDistance = c.collisionSphereDistance,
        };
      }

      static public ClothSkinningCoefficient Deserialize(Coefficient c) {
        return new ClothSkinningCoefficient() {
          maxDistance = c.maxDistance,
          collisionSphereDistance = c.collisionSphereDistance,
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
    // ToDo: Sphere Colliders
    // public List<Vector3> virtualParticleWeigts; // ToDo
    // ToDo: Self collision
    // public float selfCollisionDistance;
    // public float selfCollisionStiffnes;
    // public bool selfCollision;

    public List<Vector3> vertices;
    public List<Vector3> normals;
    public List<Coefficient> coefficients;
    // public float stiffnessFrequency; ??
  }
}
