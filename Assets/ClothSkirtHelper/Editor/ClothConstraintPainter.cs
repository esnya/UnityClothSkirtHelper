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
      new InsideConstraintPainter(),
      new SpreadConstraintPainter(),
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

      var worldExtent = Vector3.Scale(bounds.extents, new Vector3(2, 0, 2)) + new Vector3(0, height, 0);
      var worldCenter = core.skirt.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(0, 1, 0)) - new Vector3(0, height / 2, 0));
      Gizmos.DrawWireCube(worldCenter, worldExtent);

      advancedPainters.ForEach(p => {
        if (p.weight == 0) return;
        p.OnDrawGizmos(core, height);
      });
    }

    private float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition) {
      var bounds = core.mesh.bounds;
      var thresholdY = bounds.center.y + bounds.extents.y - height;

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

      radius = EditorGUILayout.FloatField("Radius", radius);
    }

    public override void OnDrawGizmos(ClothSkirtHelperCore core, float fixedHeight) {
      if (weight == 0.0) return;

      Gizmos.color = Color.white;

      var bounds = core.mesh.bounds;
      var worldCenter = new Vector3(core.center.position.x, bounds.center.y, core.center.position.z);

      var top = worldCenter + new Vector3(0, bounds.extents.y - fixedHeight, 0);
      var bottom = worldCenter - new Vector3(0, bounds.extents.y, 0);

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
      var xyCenter = Vector3.Scale(core.center.position, new Vector3(1, 0, 1));
      var xyPosition = Vector3.Scale(localPosition, new Vector3(1, 0, 1));
      var distance = Vector3.Distance(xyPosition, xyCenter);

      return Mathf.Max(0, distance - radius);
    }
  }

  public class SpreadConstraintPainter : ClothConstraintAdvancedPainter {
    public float radius = 0.1f;
    public float angle = 45.0f;

    public override void OnGUI(ClothSkirtHelperCore core) {
      EditorGUILayout.LabelField("Spreading Constraint");
      weight = EditorGUILayout.Slider("Weight", weight, 0, 1);

      EditorGUILayout.Space();

      radius = EditorGUILayout.FloatField("Waist Radius", radius);
      angle = EditorGUILayout.Slider("Angle", angle, 0, 90);
    }

    public override void OnDrawGizmos(ClothSkirtHelperCore core, float fixedHeight) {
      if (weight == 0.0) return;

      Gizmos.color = Color.white;

      var bounds = core.mesh.bounds;

      var height = bounds.extents.y * 2 - fixedHeight;
      var top = new Vector3(core.center.position.x, bounds.center.y + bounds.extents.y - fixedHeight, core.center.position.z);

      for (int a = 0; a < 360; a += 90) {
        var r = Mathf.Deg2Rad * a;
        var q = Quaternion.Euler(0, a, 0);
        var from = top + q * new Vector3(radius, 0, 0);
        var to = from + q * Quaternion.Euler(0, 0, angle) * new Vector3(0, -height, 0);
        Gizmos.DrawLine(from, to);
      }
    }

    public override float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) {
      var position = core.skirt.transform.TransformPoint(localPosition);
      var top = new Vector3(
        core.center.position.x,
        core.avatar.transform.position.y + core.mesh.bounds.center.y + core.mesh.bounds.extents.y - fixedHeight,
        core.center.position.z
      );

      // var x = top - position.y;
      // var y = Vector3.Scale(position - core.center.position, new Vector3(1, 0, 1)).magnitude - radius;
      // var a1 = Mathf.Atan2(y, x);
      // if (a1 <= 0) return 0;
      var dir = (new Vector3(position.x, top.y, position.z) - top).normalized;
      var from = top + dir * radius;
      var l = (position - from).magnitude;
      var to = from + dir.normalized * l;

      return (position - to).magnitude * Mathf.Sin(angle * Mathf.Deg2Rad);
    }
  }
}
