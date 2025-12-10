using System;
using System.Collections.Generic;

namespace MarchingCubes;

public class AlternativeMarchingCubes
{
    // Cube corner positions (8 corners)
    private static readonly (float x, float y, float z)[] CornerOffsets = {
        (0, 0, 0), // 0
        (1, 0, 0), // 1
        (1, 1, 0), // 2
        (0, 1, 0), // 3
        (0, 0, 1), // 4
        (1, 0, 1), // 5
        (1, 1, 1), // 6
        (0, 1, 1), // 7
    };

    // Edge connections (12 edges connecting the 8 corners)
    private static readonly (int v0, int v1)[] EdgeConnections = {
        (0, 1), // Edge 0
        (1, 2), // Edge 1
        (2, 3), // Edge 2
        (3, 0), // Edge 3
        (4, 5), // Edge 4
        (5, 6), // Edge 5
        (6, 7), // Edge 6
        (7, 4), // Edge 7
        (0, 4), // Edge 8
        (1, 5), // Edge 9
        (2, 6), // Edge 10
        (3, 7), // Edge 11
    };

    /// <summary>
    /// Interpolates between two corner values to find the surface intersection point
    /// </summary>
    private (float x, float y, float z) InterpolateEdge(
        int edgeIndex,
        float[] cornerValues,
        float cellX,
        float cellY,
        float cellZ)
    {
        var (v0, v1) = EdgeConnections[edgeIndex];
        var corner0 = CornerOffsets[v0];
        var corner1 = CornerOffsets[v1];

        var val0 = cornerValues[v0];
        var val1 = cornerValues[v1];

        // Linear interpolation factor
        var t = (0 - val0) / (val1 - val0);
        t = Math.Clamp(t, 0f, 1f);

        // Interpolate position
        var x = cellX + corner0.x + t * (corner1.x - corner0.x);
        var y = cellY + corner0.y + t * (corner1.y - corner0.y);
        var z = cellZ + corner0.z + t * (corner1.z - corner0.z);

        return (x, y, z);
    }

    /// <summary>
    /// Process a single cell and generate triangles
    /// </summary>
    private (List<(float x, float y, float z)> vertices, List<(int a, int b, int c)> indices) ProcessCell(
        Func<(float x, float y, float z), float> densityFunction,
        float cellX,
        float cellY,
        float cellZ)
    {
        var vertices = new List<(float x, float y, float z)>();
        var indices = new List<(int a, int b, int c)>();

        // Sample density at each corner
        var cornerValues = new float[8];
        for (int i = 0; i < 8; i++)
        {
            var corner = CornerOffsets[i];
            cornerValues[i] = densityFunction((cellX + corner.x, cellY + corner.y, cellZ + corner.z));
        }

        // Calculate cube configuration index (0-255)
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cornerValues[i] > 0)
            {
                cubeIndex |= (1 << i);
            }
        }

        // Get triangulation for this configuration
        var triangulation = Cases.CubeCases[cubeIndex];
        if (triangulation.Count == 0)
        {
            return (vertices, indices);
        }

        // Generate vertices and triangles
        foreach (var triangle in triangulation)
        {
            // Each triangle has 3 edge indices
            var v0 = InterpolateEdge(triangle[0], cornerValues, cellX, cellY, cellZ);
            var v1 = InterpolateEdge(triangle[1], cornerValues, cellX, cellY, cellZ);
            var v2 = InterpolateEdge(triangle[2], cornerValues, cellX, cellY, cellZ);

            int baseIndex = vertices.Count;
            
            // Add vertices in reverse order to match original implementation
            vertices.Add(v2);
            vertices.Add(v1);
            vertices.Add(v0);

            // Add triangle indices
            indices.Add((baseIndex, baseIndex + 1, baseIndex + 2));
        }

        return (vertices, indices);
    }

    /// <summary>
    /// Main marching cubes algorithm - generates mesh from density function
    /// </summary>
    /// <param name="f">Density function that returns negative inside, positive outside</param>
    /// <param name="xCount">Number of cells in X direction</param>
    /// <param name="yCount">Number of cells in Y direction</param>
    /// <param name="zCount">Number of cells in Z direction</param>
    /// <returns>Result containing vertices and indices</returns>
    public MarchingCubesResult MarchingCubes3D(
        Func<(float x, float y, float z), float> f,
        int xCount,
        int yCount,
        int zCount)
    {
        var allVertices = new List<(float x, float y, float z)>();
        var allIndices = new List<(int a, int b, int c)>();

        // Calculate grid bounds (centered at origin)
        int xMin = -xCount / 2;
        int xMax = xCount / 2;
        int yMin = -yCount / 2;
        int yMax = yCount / 2;
        int zMin = -zCount / 2;
        int zMax = zCount / 2;

        // Process each cell in the grid
        for (int x = xMin; x < xMax; x++)
        {
            for (int y = yMin; y < yMax; y++)
            {
                for (int z = zMin; z < zMax; z++)
                {
                    var (cellVertices, cellIndices) = ProcessCell(f, x, y, z);
                    
                    if (cellVertices.Count > 0)
                    {
                        allVertices.AddRange(cellVertices);
                        allIndices.AddRange(cellIndices);
                    }
                }
            }
        }

        return new MarchingCubesResult(allVertices, allIndices);
    }
}
