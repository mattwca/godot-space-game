using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlanetLOD : Node3D
{
	private PerlinNoise3D perlinNoise3D;
	private Camera3D camera;
	
	// LOD settings
	private float[] lodDistances = { 30f, 60f, 120f, 240f }; // Distance thresholds for each LOD
	private int[] lodResolutions = { 32, 24, 16, 8 }; // Resolution for each LOD level
	
	// Chunk management
	private Dictionary<Vector3I, (MeshInstance3D mesh, int currentLOD)> chunks = new Dictionary<Vector3I, (MeshInstance3D, int)>();
	private Vector3 sphereCenter = new Vector3(0, 0, 0);
	private float sphereRadius = 16f;
	private int chunkCount = 4; // Number of chunks per axis (4x4x4 = 64 chunks total)

	public PlanetLOD()
	{
		this.perlinNoise3D = new PerlinNoise3D(1234);
		this.perlinNoise3D.OctaveCount = 2;
		this.perlinNoise3D.Frequency = 0.2f;
		this.perlinNoise3D.Persistence = 0.1f;
	}

	public override void _Ready()
	{
		camera = GetViewport().GetCamera3D() as Camera3D;

    
		RenderingServer.SetDebugGenerateWireframes(true);
		GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		
		// Initialize all chunks
		GenerateInitialChunks();
	}

	private void GenerateInitialChunks()
	{
		var chunkSize = (sphereRadius * 2f) / chunkCount;
		
		for (int x = 0; x < chunkCount; x++)
		{
			for (int y = 0; y < chunkCount; y++)
			{
				for (int z = 0; z < chunkCount; z++)
				{
					var chunkPos = new Vector3I(x, y, z);
					var worldPos = GetChunkWorldPosition(chunkPos, chunkSize);
					
					// Only generate chunks that intersect with the sphere
					if (IsChunkNearSphere(worldPos, chunkSize))
					{
						GenerateChunk(chunkPos, 3); // Start with lowest LOD
					}
				}
			}
		}
	}

	private Vector3 GetChunkWorldPosition(Vector3I chunkPos, float chunkSize)
	{
		var offset = sphereCenter - new Vector3(sphereRadius, sphereRadius, sphereRadius);
		return offset + new Vector3(chunkPos.X * chunkSize, chunkPos.Y * chunkSize, chunkPos.Z * chunkSize);
	}

	private bool IsChunkNearSphere(Vector3 chunkWorldPos, float chunkSize)
	{
		// Check if chunk bounding box intersects with sphere
		var chunkCenter = chunkWorldPos + new Vector3(chunkSize / 2f, chunkSize / 2f, chunkSize / 2f);
		var distance = chunkCenter.DistanceTo(sphereCenter);
		var chunkRadius = chunkSize * (float)Math.Sqrt(3) / 2f; // Diagonal of cube
		
		return distance < (sphereRadius + chunkRadius + 10f); // Add margin
	}

	private void GenerateChunk(Vector3I chunkPos, int lodLevel)
	{
		var resolution = lodResolutions[lodLevel];
		var chunkSize = (sphereRadius * 2f) / chunkCount;
		var worldPos = GetChunkWorldPosition(chunkPos, chunkSize);
		var cellSize = chunkSize / resolution;

		var marchingCubes = new MarchingCubes.MarchingCubes();
		
		// Create density function for this chunk - convert local coords to world coords
		Func<(float x, float y, float z), float> circleFn = (pos) =>
		{
			var (localX, localY, localZ) = pos;
			
			// Convert from marching cubes local coordinates to world coordinates
			var worldX = worldPos.X + localX * cellSize;
			var worldY = worldPos.Y + localY * cellSize;
			var worldZ = worldPos.Z + localZ * cellSize;
			
			var position = new Vector3(worldX, worldY, worldZ);
			var diff = position - sphereCenter;
			var distance = diff.Length();
			
			// Layer multiple noise frequencies
			var noise1 = perlinNoise3D.GenerateNoiseValue(worldX * 0.1f, worldY * 0.1f, worldZ * 0.1f) * 4f;
			var noise2 = perlinNoise3D.GenerateNoiseValue(worldX * 0.3f, worldY * 0.3f, worldZ * 0.3f) * 1f;
			
			var radius = sphereRadius + noise1 + noise2;
			
			return distance - radius;
		};

		var circleObj = marchingCubes.MarchingCubes3D(circleFn, resolution, resolution, resolution);

		// Remove old chunk if it exists
		if (chunks.ContainsKey(chunkPos))
		{
			chunks[chunkPos].mesh.QueueFree();
			chunks.Remove(chunkPos);
		}

		// Create mesh instance
		var meshInstance = new MeshInstance3D();
		meshInstance.Position = worldPos;
		AddChild(meshInstance);

		// Generate vertex colors
		var colours = circleObj.Indices.SelectMany((triangle) =>
		{
			return new List<Color>() { Colors.Red, Colors.Green, Colors.Blue };
		}).ToArray();

		// Create material
		var material = new StandardMaterial3D();
		material.VertexColorUseAsAlbedo = true;

		// Build mesh - convert vertices to world coordinates and adjust for mesh position
		var arrayMesh = new ArrayMesh();
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = circleObj.Vertices
			.Select(vertex => new Vector3(
				vertex.x * cellSize,
				vertex.y * cellSize,
				vertex.z * cellSize
			))
			.Reverse()
			.ToArray();
		arrays[(int)Mesh.ArrayType.Color] = colours;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		arrayMesh.SurfaceSetMaterial(0, material);
		meshInstance.Mesh = arrayMesh;

		// Store chunk
		chunks[chunkPos] = (meshInstance, lodLevel);
	}

	public override void _Process(double delta)
	{
		if (camera == null) return;

		// Check each chunk and update LOD if needed
		var chunksToUpdate = new List<(Vector3I pos, int newLOD)>();
		
		foreach (var kvp in chunks)
		{
			var chunkPos = kvp.Key;
			var (mesh, currentLOD) = kvp.Value;
			
			// Calculate distance from camera to chunk
			var chunkWorldPos = mesh.GlobalPosition;
			var chunkSize = (sphereRadius * 2f) / chunkCount;
			var chunkCenter = chunkWorldPos + new Vector3(chunkSize / 2f, chunkSize / 2f, chunkSize / 2f);
			var distance = camera.GlobalPosition.DistanceTo(chunkCenter);
			
			// Determine appropriate LOD level
			var newLOD = DetermineLODLevel(distance);
			
			if (newLOD != currentLOD)
			{
				chunksToUpdate.Add((chunkPos, newLOD));
			}
		}

		// Update chunks (limit updates per frame to avoid stuttering)
		var maxUpdatesPerFrame = 2;
		foreach (var (pos, newLOD) in chunksToUpdate.Take(maxUpdatesPerFrame))
		{
			GenerateChunk(pos, newLOD);
		}
	}

	private int DetermineLODLevel(float distance)
	{
		for (int i = 0; i < lodDistances.Length; i++)
		{
			if (distance < lodDistances[i])
			{
				return i;
			}
		}
		return lodDistances.Length - 1; // Return lowest detail
	}
}
