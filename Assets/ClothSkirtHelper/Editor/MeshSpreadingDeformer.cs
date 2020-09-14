namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  class MeshSpreadingDeformer : ToolBase {
    public Animator avatar;
    public float startHeight = 0.1f;
    public float angle = 30.0f;
    public bool extendAngle = false;
    public Transform xzCenter;

    public void OnGUI(SkinnedMeshRenderer skinnedMeshRenderer) {
      avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(Animator), true) as Animator;
      xzCenter = EditorGUILayout.ObjectField("XZ Center", xzCenter, typeof(Transform), true) as Transform ?? skinnedMeshRenderer.transform;
      startHeight = EditorGUILayout.FloatField("Start Height from Top(m)", startHeight);
      angle = EditorGUILayout.Slider("Angle", angle, 0, extendAngle ? 90 : 45);
      extendAngle = EditorGUILayout.Toggle("Extend Angle", extendAngle);
    }

    public SkinnedMeshRenderer Execute(SkinnedMeshRenderer skinnedMeshRenderer, string outputDirectory) {
      var bakedMesh = new Mesh();
      skinnedMeshRenderer.BakeMesh(bakedMesh);
      bakedMesh.RecalculateBounds();

      var mesh = Mesh.Instantiate(skinnedMeshRenderer.sharedMesh);
      mesh.name = $"{skinnedMeshRenderer.sharedMesh.name}_spread";

      var offset = avatar.transform.position;
      var worldVertices = bakedMesh.vertices.Distinct().Select(v => v + offset).ToList();
      var topY = bakedMesh.bounds.center.y + bakedMesh.bounds.extents.y - startHeight + offset.y;
      var rotation = avatar.GetBoneTransform(HumanBodyBones.Hips).localRotation;
      var rotationInv = Quaternion.Inverse(rotation);

      mesh.vertices = mesh.vertices
        .Select(v => rotation * v)
        .Select(localPosition => {
          if (localPosition.y + offset.y > topY) return localPosition;
          var (worldPosition, from, to) = MeshUtility.Spreading(worldVertices, offset, xzCenter.position, localPosition, topY, angle);
          Debug.Log(worldPosition - offset - localPosition);
          return (to - offset);
        })
        .Select(v => rotationInv * v)
        .ToArray();

      var newRenderer = MeshUtility.ReplaceMesh(skinnedMeshRenderer, mesh, outputDirectory);
      MeshUtility.Deactivate(skinnedMeshRenderer.gameObject);

      return newRenderer;
    }

    public bool Validate() {
      return avatar != null && xzCenter != null;
    }
  }
}