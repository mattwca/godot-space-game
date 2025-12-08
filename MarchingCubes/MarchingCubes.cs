using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MarchingCubes;

public record MarchingCubesResult(List<(float x, float y, float z)> Vertices, List<(int a, int b, int c)> Indices);

public class MarchingCubes {

    // Vertices for each of the 8 corners on a cube
    private readonly (float x, float y, float z)[] Vertices = {
        (0, 0, 0),
        (1, 0, 0),
        (1, 1, 0),
        (0, 1, 0),
        (0, 0, 1),
        (1, 0, 1),
        (1, 1, 1),
        (0, 1, 1),
    };

    // 12 edges of the cube
    private readonly (int a, int b)[] Edges = {
        (0, 1),
        (1, 2),
        (2, 3),
        (3, 0),
        (4, 5),
        (5, 6),
        (6, 7),
        (7, 4),
        (0, 4),
        (1, 5),
        (2, 6),
        (3, 7),
    };

    private (float x, float y, float z) EdgeToBoundaryVertex(int edge, float[] fEval, float x, float y, float z) {
        var (v0, v1) = this.Edges[edge];

        var f0 = fEval[v0];
        var f1 = fEval[v1];

        var t0 = 1 - this.Adapt(f0, f1);
        var t1 = 1 - t0;

        var vertPos0 = this.Vertices[v0];
        var vertPos1 = this.Vertices[v1];

        var result = (
            x + vertPos0.x * t0 + vertPos1.x * t1,
            y + vertPos0.y * t0 + vertPos1.y * t1,
            z + vertPos0.z * t0 + vertPos1.z * t1
        );

        return result;
    }

    private float Adapt(float v0, float v1) {
        return (0 - v0) / (v1 - v0);
    }

    private (List<(float x, float y, float z)> vertices, List<(int a, int b, int c)> indices) MarchingCubesForSingleCell(Func<(float x, float y, float z), float> f, float x, float y, float z, int vertexOffset) {
        var fEval = new float[8];

        for (var i = 0; i < 8; i++) {
            var vertexPosition = this.Vertices[i];
            fEval[i] = f((x + vertexPosition.x, y + vertexPosition.y, z + vertexPosition.z));
        }

        var cubeCaseIndex = 0;
        for (var i = 0; i < 8; i++) {
            if (fEval[i] > 0) {
                cubeCaseIndex += (int)Math.Pow(2, i);
            }
        }

        var outputVertices = new List<(float x, float y, float z)>();
        var outputIndices = new List<(int a, int b, int c)>();

        var faces = Cases.CubeCases[cubeCaseIndex];

        foreach (var face in faces) {
            var vertices = face.Select(face => this.EdgeToBoundaryVertex(face, fEval, x, y, z)).Reverse().ToList();
            var nextVertexIndex = vertexOffset + outputVertices.Count;

            var tri = (nextVertexIndex, nextVertexIndex + 1, nextVertexIndex + 2);
            outputVertices.AddRange(vertices);
            outputIndices.Add(tri);
        }

        return (outputVertices, outputIndices);
    }

    public MarchingCubesResult MarchingCubes3D(Func<(float x, float y, float z), float> f, int xCount, int yCount, int zCount) {
        var meshVertices = new List<(float x, float y, float z)>();
        var meshIndices = new List<(int a, int b, int c)>();

        var xMin = 0 - xCount / 2;
        var xMax = xCount / 2;
        var yMin = 0 - yCount / 2;
        var yMax = yCount / 2;
        var zMin = 0 - zCount / 2;
        var zMax = zCount / 2;

        for (var x = xMin; x < xMax; x++) {
            for (var y = yMin; y < yMax; y++) {
                for (var z = zMin; z < zMax; z++) {
                    var (vertices, indices) = this.MarchingCubesForSingleCell(f, x, y, z, meshVertices.Count);
                    meshVertices.AddRange(vertices);
                    meshIndices.AddRange(indices);
                }
            }
        }


        return new MarchingCubesResult(meshVertices, meshIndices);
    }
}