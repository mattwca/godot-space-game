using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlanetMeshInstance : MeshInstance3D
{
	private PerlinNoise3D perlinNoise3D;

	public PlanetMeshInstance() {
		this.perlinNoise3D = new PerlinNoise3D(1234);
		this.perlinNoise3D.OctaveCount = 2;
		this.perlinNoise3D.Frequency = 0.2f;
		this.perlinNoise3D.Persistence = 0.1f;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// var gridSize = 512;

		// var vertices = new Vector3[gridSize*gridSize*6];
		// var normals = new Vector3[gridSize*gridSize*6];
		// var colours = new Color[gridSize*gridSize*6];

		// var vertexIndex = 0;
		// var colourIndex = 0;
		// var normalsIndex = 0;

		// var quadSize = 10f / gridSize;

		// for (var i = 0; i < gridSize; i++) {
		// 	for (var j = 0; j < gridSize; j++) {
		// 		var x = i * quadSize;
		// 		var y = j * quadSize;

		// 		var pos01 = new Vector3(x, y, 0);
		// 		var pos02 = new Vector3(x + quadSize, y, 0);
		// 		var pos03 = new Vector3(x, y - quadSize, 0);
		// 		var pos04 = new Vector3(x + quadSize, y - quadSize, 0);

		// 		var noise01 = this.perlinNoise3D.GenerateNoiseValue(pos01.X, pos01.Y, pos01.Z);
		// 		var noise02 = this.perlinNoise3D.GenerateNoiseValue(pos02.X, pos02.Y, pos02.Z);
		// 		var noise03 = this.perlinNoise3D.GenerateNoiseValue(pos03.X, pos03.Y, pos03.Z);
		// 		var noise04 = this.perlinNoise3D.GenerateNoiseValue(pos04.X, pos04.Y, pos04.Z);

		// 		pos01.Z = noise01;
		// 		pos02.Z = noise02;
		// 		pos03.Z = noise03;
		// 		pos04.Z = noise04;

		// 		var normal01 = (pos02 - pos01).Cross(pos03 - pos01).Normalized() * -1;
		// 		var normal02 = (pos03 - pos04).Cross(pos02 - pos04).Normalized() * -1;

		// 		normals[normalsIndex++] = normal01;
		// 		normals[normalsIndex++] = normal01;
		// 		normals[normalsIndex++] = normal01;
				
		// 		normals[normalsIndex++] = normal02;
		// 		normals[normalsIndex++] = normal02;
		// 		normals[normalsIndex++] = normal02;

		// 		vertices[vertexIndex++] = pos01;
		// 		vertices[vertexIndex++] = pos02;
		// 		vertices[vertexIndex++] = pos03;

		// 		vertices[vertexIndex++] = pos04;
		// 		vertices[vertexIndex++] = pos03;
		// 		vertices[vertexIndex++] = pos02;

		// 		// colours[colourIndex++] = new Color(128, 32, 12);
		// 		// colours[colourIndex++] = new Color(128, 32, 12);
		// 		// colours[colourIndex++] = new Color(128, 32, 12);

		// 		// colours[colourIndex++] = new Color(128, 32, 12);
		// 		// colours[colourIndex++] = new Color(128, 32, 12);
		// 		// colours[colourIndex++] = new Color(128, 32, 12);
		// 	}
		// }
		
		// var arrayMesh = new ArrayMesh();
		// var arrays = new Godot.Collections.Array();
		// arrays.Resize((int)Mesh.ArrayType.Max);
		// arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		// arrays[(int)Mesh.ArrayType.Normal] = normals;
		// // arrays[(int)Mesh.ArrayType.Color] = colours;

		// arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		// this.Mesh = arrayMesh;

		// RenderingServer.SetDebugGenerateWireframes(true);
		// GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		Vector3 ProjectFromCubeToSphere(Vector3 cubePos) {
			var (xc, yc, zc) = cubePos;
			var sphereLoc = (float)Math.Sqrt(xc*xc + yc*yc + zc*zc);

			var x = xc / sphereLoc;
			var y = yc / sphereLoc;
			var z = zc / sphereLoc;

			return new Vector3(x, y, z);
		}

		var marchingCubes = new MarchingCubes.MarchingCubes();
		var circleFn = ((float x, float y, float z) pos) => {
				var (x, y, z) = pos;
				
				var position = new Vector3(x, y, z);
				var sphereCentre = new Vector3(16, 16, 16);
				var diff = position - sphereCentre;
				var distance = (float)Math.Sqrt(diff.X*diff.X + diff.Y*diff.Y + diff.Z*diff.Z);
				
				// Layer multiple noise frequencies
				var noise1 = perlinNoise3D.GenerateNoiseValue(x * 0.1f, y * 0.1f, z * 0.1f) * 4f;
				var noise2 = perlinNoise3D.GenerateNoiseValue(x * 0.3f, y * 0.3f, z * 0.3f) * 1f;
				
				var radius = 16f + noise1 + noise2;
				
				return distance - radius;
		};

		var circleObj = marchingCubes.MarchingCubes3D(circleFn, 128, 128, 128);

		var toVector = ((float x, float y, float z) a) => {
			return new Vector3(a.x, a.y, a.z);
		};

		var colours = circleObj.Indices.SelectMany((triangle) => {
			var pos01 = toVector(circleObj.Vertices[triangle.a]);
			var pos02 = toVector(circleObj.Vertices[triangle.b]);
			var pos03 = toVector(circleObj.Vertices[triangle.c]);

			return new List<Godot.Color>() {
				Colors.Red,
				Colors.Green,
				Colors.Blue,
			};
		});

		var material = new StandardMaterial3D();
		material.VertexColorUseAsAlbedo = true;

		var arrayMesh = new ArrayMesh();
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = circleObj.Vertices.Select(vertex => new Vector3(vertex.x, vertex.y, vertex.z)).Reverse().ToArray();
		// arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		//arrays[(int)Mesh.ArrayType.Index] = circleObj.Indices.SelectMany(index => new int[] { index.a, index.b, index.c }).ToArray();
		arrays[(int)Mesh.ArrayType.Color] = colours.ToArray();

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		arrayMesh.SurfaceSetMaterial(0, material);
		this.Mesh = arrayMesh;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
