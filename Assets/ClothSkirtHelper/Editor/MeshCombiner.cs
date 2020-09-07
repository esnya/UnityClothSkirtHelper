namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  [Serializable]
  class MeshCombiner : ToolBase {
    public SkinnedMeshRenderer otherSkinnedMeshRenderer;

    public SkinnedMeshRenderer Execute(SkinnedMeshRenderer skinnedMeshRenderer, string outputDirectory) {
      var srcMesh = skinnedMeshRenderer.sharedMesh;
      var otherMesh = otherSkinnedMeshRenderer.sharedMesh;

      var dstMesh = new Mesh() {
        vertices = srcMesh.vertices.Concat(otherMesh.vertices).ToArray(),
        boneWeights = srcMesh.boneWeights.Concat(otherMesh.boneWeights).ToArray(),
        bindposes = srcMesh.bindposes.ToArray(),
        colors = srcMesh.colors.Concat(otherMesh.colors).ToArray(),
        colors32 = srcMesh.colors32.Concat(otherMesh.colors32).ToArray(),
        normals = srcMesh.normals.Concat(otherMesh.normals).ToArray(),
        tangents = srcMesh.tangents.Concat(otherMesh.tangents).ToArray(),
        uv = srcMesh.uv.Concat(otherMesh.uv).ToArray(),
        uv2 = srcMesh.uv2.Concat(otherMesh.uv2).ToArray(),
        uv3 = srcMesh.uv3.Concat(otherMesh.uv3).ToArray(),
        uv4 = srcMesh.uv4.Concat(otherMesh.uv4).ToArray(),
        uv5 = srcMesh.uv5.Concat(otherMesh.uv5).ToArray(),
        uv6 = srcMesh.uv6.Concat(otherMesh.uv6).ToArray(),
        uv7 = srcMesh.uv7.Concat(otherMesh.uv7).ToArray(),
        uv8 = srcMesh.uv8.Concat(otherMesh.uv8).ToArray(),
      };
      dstMesh.name = $"{srcMesh.name}_and_{otherMesh.name}";

      dstMesh.subMeshCount = srcMesh.subMeshCount + otherMesh.subMeshCount;

      MeshUtility.SetSubMeshTriangles(
        dstMesh,
        MeshUtility
          .EnumerateSubMeshTriangles(srcMesh)
          .Concat(
            MeshUtility
              .EnumerateSubMeshTriangles(otherMesh)
              .Select(triangles => triangles.Select(v => srcMesh.vertexCount + v))
          )
      );

      var otherBlendShapes = MeshUtility.EnumerateBlendShapeFrames(otherMesh).ToList();
      MeshUtility.SetBlendShapeFrames(dstMesh, MeshUtility.EnumerateBlendShapeFrames(srcMesh).Select(shape => {
        return new MeshUtility.BlendShape() {
          name = shape.name,
          frames = shape.frames.Select(frame => new MeshUtility.BlendShapeFrame() {
            weight = frame.weight ,
            deltaVertices = frame.deltaVertices.Concat(Enumerable.Repeat(Vector3.zero, otherMesh.vertexCount)),
            deltaNormals = frame.deltaNormals.Concat(Enumerable.Repeat(Vector3.zero, otherMesh.vertexCount)),
            deltaTangents = frame.deltaTangents.Concat(Enumerable.Repeat(Vector3.zero, otherMesh.vertexCount)),
          }),
        };
      }));

      var newRenderer = MeshUtility.ReplaceMesh(skinnedMeshRenderer, dstMesh, outputDirectory);
      newRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials.Concat(otherSkinnedMeshRenderer.sharedMaterials).ToArray();

      MeshUtility.Deactivate(skinnedMeshRenderer.gameObject);
      MeshUtility.Deactivate(otherSkinnedMeshRenderer.gameObject);

      return newRenderer;
    }

    public void OnGUI(SkinnedMeshRenderer skinnedMeshRenderer) {
      otherSkinnedMeshRenderer = EditorGUILayout.ObjectField("Skinned Mesh Renderer", otherSkinnedMeshRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
    }

    public bool Validate() {
      return otherSkinnedMeshRenderer != null;
    }
  }
}
