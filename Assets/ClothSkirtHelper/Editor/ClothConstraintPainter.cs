namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  public class ClothConstraintAdvancedPainter {
    public float weight = 0.0f;
    public virtual void OnGUI(ClothSkirtHelperCore core) {}
    public virtual void OnDrawGizmos(ClothSkirtHelperCore core, float fixedHeight) {}
    public virtual float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) => float.MaxValue;
  }

  [Serializable]
  public class ClothConstraintPainter {
    public float height = 0.1f;

    public List<ClothConstraintAdvancedPainter> advancedPainters = new List<ClothConstraintAdvancedPainter>() {
      new SpreadConstraintPainter(),
      new InsideConstraintPainter(),
    };

    public void OnGUI(ClothSkirtHelperCore core) {
      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        EditorGUILayout.LabelField("Top Edge Constraint");
        height = EditorGUILayout.FloatField("Height (m)", height);
      }

      if (core.advancedMode) {
        advancedPainters.ForEach(p => {
          EditorGUILayout.Space();
          using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
            p.OnGUI(core);
          }
        });
      }
    }

    public void OnDrawGizmos(ClothSkirtHelperCore core) {
      var bounds = core.mesh.bounds;
      Gizmos.color = Color.red;

      var size = new Vector3(bounds.size.x, height, bounds.size.y);
      var center = core.worldTop - new Vector3(0, height / 2, 0);
      Gizmos.DrawWireCube(center, size);

      advancedPainters.ForEach(p => {
        if (p.weight == 0) return;
        p.OnDrawGizmos(core, height);
      });
    }

    private float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition) {
      var thresholdY = core.worldTop.y - height;

      if (localPosition.y > thresholdY) return 0;
      if (!core.advancedMode) return float.MaxValue;

      var totalWeight = advancedPainters.Select(p => p.weight).Sum();
      if (totalWeight <= 0.001) return float.MaxValue;

      // return (float)(1.0 / advancedPainters.Select(p => (double)p.weight / p.GetMaxDistance(core, localPosition, height)).Sum());
      return advancedPainters.Select(p => p.GetMaxDistance(core, localPosition, height) * p.weight / totalWeight).Sum();
    }

    public void Execute(ClothSkirtHelperCore core) {
      var cloth = core.cloth;

      cloth.coefficients = core.mesh.vertices
        .Distinct()
        .Select(v => {
          return new ClothSkinningCoefficient() {
            collisionSphereDistance = float.MaxValue,
            maxDistance = GetMaxDistance(core, v),
          };
        })
        .ToArray();
    }
  }

  public class InsideConstraintPainter : ClothConstraintAdvancedPainter {
    public float radius = 0.05f;
    public Transform center;

    public override void OnGUI(ClothSkirtHelperCore core) {
      EditorGUILayout.LabelField("Inside Constraint");
      weight = EditorGUILayout.Slider("Weight", weight, 0, 1);

      EditorGUILayout.Space();

      radius = EditorGUILayout.FloatField("Inner Radius", radius);
    }

    public override void OnDrawGizmos(ClothSkirtHelperCore core, float fixedHeight) {
      if (weight == 0.0) return;


      var bounds = core.mesh.bounds;

      var top = core.worldTop - new Vector3(0, fixedHeight, 0);
      var bottom = core.worldBottom;

      new List<Vector3>() {
        new Vector3(1, 0, 0),
        new Vector3(-1, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(0, 0, -1),
      }.ForEach(v => {
        Gizmos.DrawLine(
          top + v * radius,
          bottom + v * radius
        );
      });
    }

    public override float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) {
      var distance = Vector3.Scale(localPosition + core.avatar.transform.position - core.worldCenter, new Vector3(1, 0, 1)).magnitude;
      return Mathf.Max(0, distance - radius);
    }
  }

  public class SpreadConstraintPainter : ClothConstraintAdvancedPainter {
    public float angle = 45.0f;

    public override void OnGUI(ClothSkirtHelperCore core) {
      EditorGUILayout.LabelField("Spreading Constraint");
      weight = EditorGUILayout.Slider("Weight", weight, 0, 1);

      EditorGUILayout.Space();

      angle = EditorGUILayout.Slider("Maximam spread Angle", angle, 0, 90);
    }

    private (Vector3, Vector3, Vector3) Preprocess(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) {
      return MeshUtility.Spreading(core.worldVertices, core.avatar.transform.position, core.center, localPosition, core.worldTop.y - fixedHeight, angle);
      // var worldPosition = localPosition + core.avatar.transform.position;
      // var xz = Vector3.Scale(worldPosition - core.worldCenter, new Vector3(1, 0, 1));

      // var nearestFixed = core.worldVertices
      //   .Where(v => v.y > core.worldTop.y - fixedHeight)
      //   .OrderBy(v => Vector3.Distance(worldPosition - core.worldCenter, Vector3.Scale(v - core.worldCenter, new Vector3(1, 1, 1))))
      //   .First();

      // var radius = Vector3.Scale(nearestFixed - core.worldCenter, new Vector3(1, 0, 1)).magnitude;

      // var dir = xz.normalized;
      // var from = core.worldTop + dir * radius - new Vector3(0, fixedHeight, 0);
      // var length = (worldPosition - from).magnitude;

      // var rad = angle * Mathf.Deg2Rad;
      // var to = from + dir * length * Mathf.Sin(rad) - new Vector3(0, length * Mathf.Cos(rad), 0);

      // return (worldPosition, from, to);
    }

    public override void OnDrawGizmos(ClothSkirtHelperCore core, float fixedHeight) {
      if (weight == 0.0) return;


      var bounds = core.mesh.bounds;

      new List<Vector3>() {
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1),
        new Vector3(-1, 0, 1),
        new Vector3(-1, 0, 0),
        new Vector3(-1, 0, -1),
        new Vector3(0, 0, -1),
        new Vector3(1, 0, -1),
      }
        .Select(s => Vector3.Scale(bounds.extents, s) + core.worldBottom - core.avatar.transform.position)
        .ToList()
        .ForEach(localPosition => {
          var (worldPosition, from, to) = Preprocess(core, localPosition, fixedHeight);

          Gizmos.color = Color.white;
          Gizmos.DrawLine(from, to);

          Gizmos.color = Color.green;
          Gizmos.DrawLine(core.worldBottom, to);
        });
    }

    public override float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) {
      var (worldPosition, from, to) = Preprocess(core, localPosition, fixedHeight);
      return (to - worldPosition).magnitude;
    }
  }
}
