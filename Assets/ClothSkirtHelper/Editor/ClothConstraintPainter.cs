namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  public class ClothConstraintAdvancedPainter {
    public float weight = 0.0f, spWeight = 0.0f;
    public virtual void OnGUI(ClothSkirtHelperCore core) {
      EditorGUILayout.LabelField("Weight");
      using (new EditorGUI.IndentLevelScope()) {
        weight = EditorGUILayout.Slider("Max Distance", weight, 0, 1);
        if (core.paintSP) spWeight = EditorGUILayout.Slider("Surface Penetration", spWeight, 0, 1);
      }

    }
    public virtual void OnDrawGizmos(ClothSkirtHelperCore core, float fixedHeight) {}
    public virtual float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) => float.MaxValue;
    public virtual float GetSurfacePenetration(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) => GetMaxDistance(core, localPosition, fixedHeight);
  }

  [Serializable]
  public class ClothConstraintPainter {
    public float height = 0.1f;
    public float bias = 0.0f, spBias = 0.0f;

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

        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
          EditorGUILayout.LabelField("Bias");
          using (new EditorGUI.IndentLevelScope()) {
            bias = EditorGUILayout.FloatField("Max Distance", bias);
            if (core.paintSP) spBias = EditorGUILayout.FloatField("Surface Penetration", spBias);
          }
        }
      }
    }

    public void OnDrawGizmos(ClothSkirtHelperCore core) {
      var bounds = core.mesh.bounds;
      Gizmos.color = Color.red;

      var size = new Vector3(bounds.size.x, height, bounds.size.y);
      var center = core.worldTop - new Vector3(0, height / 2, 0);
      Gizmos.DrawWireCube(center, size);

      if (!core.advancedMode) return;

      advancedPainters.ForEach(p => {
        if (p.weight == 0) return;
        p.OnDrawGizmos(core, height);
      });
    }

    private float GetMaxDistance(ClothSkirtHelperCore core, Vector3 localPosition) {
      var thresholdY = core.worldTop.y - height;

      if (localPosition.y > thresholdY) return 0;
      if (!core.advancedMode) {
        var painter = new SpreadConstraintPainter();
        painter.angle = 80;
        return painter.GetMaxDistance(core, localPosition, height);
      }

      var totalWeight = advancedPainters.Select(p => p.weight).Sum();
      if (totalWeight <= 0.001) return float.MaxValue;

      return advancedPainters.Select(p => p.GetMaxDistance(core, localPosition, height) * p.weight / totalWeight).Sum() + bias;
    }

    private float GetSurfacePenetration(ClothSkirtHelperCore core, Vector3 localPosition) {
      if (!core.paintSP || !core.advancedMode) return float.MaxValue;
      // var thresholdY = core.worldTop.y - height;

      // if (localPosition.y > thresholdY) return 0;

      var totalWeight = advancedPainters.Select(p => p.spWeight).Sum();
      if (totalWeight <= 0.001) return float.MaxValue;

      return advancedPainters.Select(p => p.GetSurfacePenetration(core, localPosition, height) * p.spWeight / totalWeight).Sum() + spBias;
    }

    public void Execute(ClothSkirtHelperCore core) {
      var cloth = core.cloth;

      cloth.coefficients = core.mesh.vertices
        .Distinct()
        .Select(v => {
          return new ClothSkinningCoefficient() {
            collisionSphereDistance = GetSurfacePenetration(core, v),
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
      base.OnGUI(core);
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
      base.OnGUI(core);
      angle = EditorGUILayout.Slider("Maximam spread Angle", angle, 0, 90);
    }

    private (Vector3, Vector3, Vector3) Preprocess(ClothSkirtHelperCore core, Vector3 localPosition, float fixedHeight) {
      return MeshUtility.Spreading(core.worldVertices, core.avatar.transform.position, core.center, localPosition, core.worldTop.y - fixedHeight, angle);
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
