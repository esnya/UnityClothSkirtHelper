namespace EsnyaFactory.ClothSkirtHelper {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEditor;

  class MeshUtility {
    public static void Deactivate(GameObject o) {
      Undo.RecordObject(o, "Deactivate");
      o.SetActive(false);
      o.tag = "EditorOnly";
    }

    public static SkinnedMeshRenderer ReplaceMesh(SkinnedMeshRenderer src, Mesh mesh, string outputDirectory) {
      var gameObject = new GameObject();
      gameObject.name = mesh.name;
      gameObject.transform.SetParent(src.transform.parent);

      var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
      renderer.bones = src.bones.ToArray();
      renderer.probeAnchor = src.probeAnchor;
      renderer.rootBone = src.rootBone;
      renderer.sharedMaterials = src.sharedMaterials.ToArray();
      renderer.sharedMesh = mesh;

      Enumerable.Range(0, mesh.blendShapeCount).ToList().ForEach(i => {
        var name = mesh.GetBlendShapeName(i);
        if (string.IsNullOrEmpty(name)) return;
        var j = src.sharedMesh.GetBlendShapeIndex(name);
        var weight = src.GetBlendShapeWeight(j);
        renderer.SetBlendShapeWeight(i, weight);
      });

      Undo.RegisterCreatedObjectUndo(gameObject, "Object Copied");
      AssetUtility.ForceCreateAsset(mesh, $"{outputDirectory}/{mesh.name}.asset");

      return renderer;
    }

    public static IEnumerable<int[]> EnumerateSubMeshTriangles(Mesh mesh) {
      return Enumerable.Range(0, mesh.subMeshCount).Select(subMesh => mesh.GetTriangles(subMesh, true));
    }

    public static void SetSubMeshTriangles(Mesh mesh, IEnumerable<IEnumerable<int>> tirangles) {
      foreach (var a in tirangles.Select((t, i) => new { t, i })) {
        mesh.SetTriangles(a.t.ToArray(), a.i);
      }
    }


    public struct BlendShape {
      public string name;
      public IEnumerable<BlendShapeFrame> frames;
    }
    public struct BlendShapeFrame {
      public float weight;
      public IEnumerable<Vector3> deltaVertices;
      public IEnumerable<Vector3> deltaNormals;
      public IEnumerable<Vector3> deltaTangents;
    }
    public static IEnumerable<BlendShape> EnumerateBlendShapeFrames(Mesh mesh) {
      return Enumerable
        .Range(0, mesh.blendShapeCount)
        .Select(shapeIndex => {
          var name = mesh.GetBlendShapeName(shapeIndex);
          var frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
          var frames = Enumerable
            .Range(0, frameCount)
            .Select(frameIndex => {
              var deltaVertices = new Vector3[mesh.vertexCount];
              var deltaNormals = new Vector3[mesh.vertexCount];
              var deltaTangents = new Vector3[mesh.vertexCount];
              mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
              return new BlendShapeFrame() {
                weight  = mesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex),
                deltaVertices = deltaVertices,
                deltaNormals =  deltaNormals,
                deltaTangents = deltaTangents,
              };
            });

          return new BlendShape() {
            name = name,
            frames = frames,
          };
        });
    }
    public static void SetBlendShapeFrames(Mesh mesh, IEnumerable<BlendShape> blendShapes) {
      foreach (var blendShape in blendShapes) {
        foreach (var frame in blendShape.frames) {
          mesh.AddBlendShapeFrame(blendShape.name, frame.weight, frame.deltaVertices.ToArray(), frame.deltaNormals.ToArray(), frame.deltaTangents.ToArray());
        }
      }
    }

    public static Mesh ExtractMesh(SkinnedMeshRenderer srcRenderer, Func<Vector3, Vector3, bool> vertexPredicate, Func<int, bool> subMeshPredicate, bool includeBoundary) {
      var srcMesh = srcRenderer.sharedMesh;

      var worldVertices = srcMesh.vertices.Select(srcRenderer.transform.TransformPoint).ToArray();
      var worldNormals = srcMesh.normals.Select(srcRenderer.transform.TransformDirection).ToArray();

      var subMeshList = Enumerable
        .Range(0, srcMesh.subMeshCount)
        .Where(subMeshPredicate)
        .Select(subMesh =>
          srcMesh
            .GetTriangles(subMesh, true)
            .Select((v, i) => new { v, i })
            .GroupBy(b => b.i / 3)
            .Select(g => g.Select(b => b.v))
            .Where(f => includeBoundary ? f.Any(v => vertexPredicate(worldVertices[v], worldNormals[v])) : f.All(v => vertexPredicate(worldVertices[v], worldNormals[v])))
            .Select(a => {
              if (a.Count() != 3) Debug.Log(a.Count());
              return a;
            })
            .SelectMany(a => a)
            .ToArray()
        )
        .Where(subMesh => subMesh.Length > 0)
        .ToArray();

      var remapTable = Enumerable.Range(0, srcMesh.vertexCount).Select(v => -1).ToArray();
      subMeshList
        .SelectMany(a => a)
        .OrderBy(a => a)
        .Distinct()
        .Select((src, dst) => new { src, dst })
        .ToList()
        .ForEach(a => {
          remapTable[a.src] = a.dst;
        });

      var dstMesh = new Mesh() {
        vertices = srcMesh.vertices.Where((a, i) => remapTable[i] >= 0).ToArray(),
        boneWeights = srcMesh.boneWeights.Where((a, i) => remapTable[i] >= 0).ToArray(),
        bindposes = srcMesh.bindposes.ToArray(),
        colors = srcMesh.colors.Where((a, i) => remapTable[i] >= 0).ToArray(),
        colors32 = srcMesh.colors32.Where((a, i) => remapTable[i] >= 0).ToArray(),
        normals = srcMesh.normals.Where((a, i) => remapTable[i] >= 0).ToArray(),
        tangents = srcMesh.tangents.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv = srcMesh.uv.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv2 = srcMesh.uv2.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv3 = srcMesh.uv3.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv4 = srcMesh.uv4.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv5 = srcMesh.uv5.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv6 = srcMesh.uv6.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv7 = srcMesh.uv7.Where((a, i) => remapTable[i] >= 0).ToArray(),
        uv8 = srcMesh.uv8.Where((a, i) => remapTable[i] >= 0).ToArray(),
      };

      dstMesh.subMeshCount = subMeshList.Length;

      subMeshList
        .Select((triangles, subMesh) => new { triangles = triangles.Select(v => remapTable[v]).ToArray(), subMesh })
        .ToList()
        .ForEach(a => {
          dstMesh.SetTriangles(a.triangles, a.subMesh, false, 0);
        });

      Enumerable
        .Range(0, srcMesh.blendShapeCount)
        .ToList()
        .ForEach(shapeIndex => {
          var name = srcMesh.GetBlendShapeName(shapeIndex);
          var frameCount = srcMesh.GetBlendShapeFrameCount(shapeIndex);
          Enumerable
            .Range(0, frameCount)
            .ToList()
            .ForEach(frameIndex => {
              var deltaVertices = new Vector3[srcMesh.vertexCount];
              var deltaNormals = new Vector3[srcMesh.vertexCount];
              var deltaTangents = new Vector3[srcMesh.vertexCount];
              srcMesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
              var weight = srcMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
              dstMesh.AddBlendShapeFrame(
                name,
                weight,
                deltaVertices.Where((a, i) => remapTable[i] >= 0).ToArray(),
                deltaNormals.Where((a, i) => remapTable[i] >= 0).ToArray(),
                deltaTangents.Where((a, i) => remapTable[i] >= 0).ToArray()
              );
            });
        });

      dstMesh.RecalculateBounds();

      return dstMesh;
    }

    public static string GetClothAvatarRank(int vertexCount) {
      if (vertexCount >= 200) return "Very Poor";
      else if (vertexCount >= 100) return "Poor";
      else if (vertexCount >= 50) return "Good";
      return "Excellent";
    }

    public static void MeshMetricsGUI(SkinnedMeshRenderer skinnedMeshRenderer) {
      if (skinnedMeshRenderer == null) return;

      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.LabelField("Vertex Count", $"{skinnedMeshRenderer.sharedMesh.vertexCount}");
        using (new EditorGUI.IndentLevelScope()) {
          EditorGUILayout.LabelField("Unnecessary", $"{skinnedMeshRenderer.sharedMesh.vertexCount - skinnedMeshRenderer.sharedMesh.triangles.Distinct().Count()}");
        }
        EditorGUILayout.LabelField("Polygon Count", $"{skinnedMeshRenderer.sharedMesh.triangles.Length / 3}");
        EditorGUILayout.LabelField("SubMesh Count", $"{skinnedMeshRenderer.sharedMesh.subMeshCount}");
        EditorGUILayout.LabelField("Avatar Rank", GetClothAvatarRank(skinnedMeshRenderer.sharedMesh.vertexCount));
      }
    }

    public static Bounds GetBounds(SkinnedMeshRenderer skinnedMeshRenderer) {
      var cloth = skinnedMeshRenderer.GetComponent<Cloth>();

      var v0 = cloth.vertices.First();

      var (min, max) = cloth.vertices.Skip(1).Aggregate(
        (v0, v0),
        (p, c) => (
          Vector3.Min(p.Item1, c),
          Vector3.Max(p.Item2, c)
        )
      );

      return new Bounds(
        skinnedMeshRenderer.bones.First().TransformPoint((max + min) * 0.5f),
        max - min
      );
    }
  }
}
