using System;
using MarchingCubes;

public class Program
{
    static void Main(string[] args)
    {
        var marchingCubes = new MarchingCubes.MarchingCubes();
        var circleFn = ((float x, float y, float z) pos) =>
        {
            var (x, y, z) = pos;
            return 2.5f - (float)Math.Sqrt(x * x + y * y + z * z);
        };
        var circleObj = marchingCubes.MarchingCubes3D(circleFn, 12, 12, 12);

        foreach (var tri in circleObj.Indices) {
            Console.WriteLine(circleObj.Vertices[tri.a]);
            Console.WriteLine(circleObj.Vertices[tri.b]);
            Console.WriteLine(circleObj.Vertices[tri.c]);
        }

        Console.WriteLine("Vertices: " + circleObj.Vertices.Count);
        Console.WriteLine("Triangles: " + circleObj.Indices.Count);
    }
}