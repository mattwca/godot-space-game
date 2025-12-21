using Godot;
using GodotSpaceGameTest.MarchingCubes;
using GodotSpaceGameTest.Noise;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AlternativePlanetLOD : Node3D
{
  private IPerlinNoise noiseGenerator;
  private Camera3D camera;

  // LOD settings
  private float[] lodDistances = { 5f, 10f, 15f, 20f };
  private int[] lodResolutions = { 64, 32, 16, 8 };

  // Planet settings
  private Vector3 planetCenter = Vector3.Zero;
  private float planetRadius = 16f;
  private int chunksPerAxis = 4;

  // Chunk management
  private Dictionary<Vector3I, ChunkData> activeChunks = new Dictionary<Vector3I, ChunkData>();

  private class ChunkData
  {
    public MeshInstance3D MeshInstance;
    public int CurrentLOD;
    public Vector3 WorldMin;
    public Vector3 WorldMax;
  }

  public AlternativePlanetLOD()
  {
    this.noiseGenerator = new CachedPerlinNoise3D(1234);
    this.noiseGenerator.OctaveCount = 1;
    this.noiseGenerator.Frequency = 3f;
    this.noiseGenerator.Persistence = 0.2f;
    this.noiseGenerator.Lacunarity = 2f;
  }

  public override void _Ready()
  {
    camera = GetViewport().GetCamera3D() as Camera3D;

    RenderingServer.SetDebugGenerateWireframes(true);
    GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

    InitializeChunks();
  }

  private void InitializeChunks()
  {
    float chunkSize = (planetRadius * 2f) / chunksPerAxis;

    // Create chunks in a grid around the planet
    for (int x = 0; x < chunksPerAxis; x++)
    {
      for (int y = 0; y < chunksPerAxis; y++)
      {
        for (int z = 0; z < chunksPerAxis; z++)
        {
          var chunkIndex = new Vector3I(x, y, z);
          var chunkMin = CalculateChunkWorldMin(chunkIndex, chunkSize);
          var chunkMax = chunkMin + new Vector3(chunkSize, chunkSize, chunkSize);

          // Only create chunks that might contain part of the sphere
          if (DoesChunkIntersectSphere(chunkMin, chunkMax))
          {
            CreateChunk(chunkIndex, chunkMin, chunkMax, lodLevel: 3); // Start with lowest detail
          }
        }
      }
    }

    GD.Print($"Initialized {activeChunks.Count} chunks");
  }

  private Vector3 CalculateChunkWorldMin(Vector3I chunkIndex, float chunkSize)
  {
    // Calculate the minimum corner of this chunk in world space
    // Chunks are arranged so the entire grid is centered on the planet
    float gridMin = -planetRadius;
    return new Vector3(
      gridMin + chunkIndex.X * chunkSize,
      gridMin + chunkIndex.Y * chunkSize,
      gridMin + chunkIndex.Z * chunkSize
    );
  }

  private bool DoesChunkIntersectSphere(Vector3 chunkMin, Vector3 chunkMax)
  {
    // Find closest point in chunk to sphere center
    var closest = new Vector3(
      Mathf.Clamp(planetCenter.X, chunkMin.X, chunkMax.X),
      Mathf.Clamp(planetCenter.Y, chunkMin.Y, chunkMax.Y),
      Mathf.Clamp(planetCenter.Z, chunkMin.Z, chunkMax.Z)
    );

    float distance = closest.DistanceTo(planetCenter);
    
    // Add margin to catch chunks near the surface
    return distance < planetRadius + 5f;
  }

  private void CreateChunk(Vector3I chunkIndex, Vector3 chunkMin, Vector3 chunkMax, int lodLevel)
  {
    var resolution = lodResolutions[lodLevel];
    var chunkSize = chunkMax - chunkMin;
    
    // Use the alternative marching cubes implementation
    var marchingCubes = new MarchingCubes();

    var halfRes = resolution / 2f;

    // Create density function that maps from marching cubes space to world space
    Func<(float x, float y, float z), float> densityFunction = (pos) =>
    {
      // Marching cubes gives us coordinates in range [-resolution/2, resolution/2]
      // Map this to our chunk's world space [chunkMin, chunkMax]
      var (mcX, mcY, mcZ) = pos;
      
      // Convert from marching cubes centered coordinates to 0-based
      float normalizedX = (mcX + halfRes) / resolution;
      float normalizedY = (mcY + halfRes) / resolution;
      float normalizedZ = (mcZ + halfRes) / resolution;
      
      // Map to world space
      var worldX = chunkMin.X + normalizedX * chunkSize.X;
      var worldY = chunkMin.Y + normalizedY * chunkSize.Y;
      var worldZ = chunkMin.Z + normalizedZ * chunkSize.Z;
      
      var worldPos = new Vector3(worldX, worldY, worldZ);
      float distanceToCenter = worldPos.DistanceTo(planetCenter);
      
      // Optional: Add noise for terrain features
      float noise = this.noiseGenerator.GenerateNoiseValue(worldX * 0.1f, worldY * 0.1f, worldZ * 0.1f);
      
      float effectiveRadius = planetRadius + noise;
      
      // Negative inside, positive outside
      return distanceToCenter - effectiveRadius;
    };

    var result = marchingCubes.MarchingCubes3D(densityFunction, resolution, resolution, resolution);

    // If no geometry was generated, skip this chunk
    if (result.Vertices.Count == 0)
    {
      return;
    }

    // Convert vertices from marching cubes space to world space
    var worldVertices = result.Vertices.Select(v =>
    {
      // Map from centered marching cubes coordinates to world space
      float normalizedX = (v.x + resolution / 2f) / resolution;
      float normalizedY = (v.y + resolution / 2f) / resolution;
      float normalizedZ = (v.z + resolution / 2f) / resolution;
      
      return new Vector3(
        chunkMin.X + normalizedX * chunkSize.X,
        chunkMin.Y + normalizedY * chunkSize.Y,
        chunkMin.Z + normalizedZ * chunkSize.Z
      );
    }).ToArray();

    // Remove old chunk if it exists
    if (activeChunks.TryGetValue(chunkIndex, out var oldChunk))
    {
      oldChunk.MeshInstance.QueueFree();
      activeChunks.Remove(chunkIndex);
    }

    // Create new mesh instance
    var meshInstance = new MeshInstance3D();
    meshInstance.Position = Vector3.Zero; // Vertices are already in world space
    AddChild(meshInstance);

    // Create material
    var material = new StandardMaterial3D();
    material.VertexColorUseAsAlbedo = false;
    material.AlbedoColor = Colors.Red;

    // Build mesh
    var arrayMesh = new ArrayMesh();
    var arrays = new Godot.Collections.Array();
    arrays.Resize((int)Mesh.ArrayType.Max);
    arrays[(int)Mesh.ArrayType.Vertex] = worldVertices.Reverse().ToArray();

    arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    arrayMesh.SurfaceSetMaterial(0, material);
    meshInstance.Mesh = arrayMesh;

    // Store chunk data
    var chunkData = new ChunkData
    {
      MeshInstance = meshInstance,
      CurrentLOD = lodLevel,
      WorldMin = chunkMin,
      WorldMax = chunkMax
    };
    activeChunks[chunkIndex] = chunkData;
  }

  public override void _Process(double delta)
  {
    if (camera == null) return;

    UpdateChunkLODs();
  }

  private void UpdateChunkLODs()
  {
    var chunksToUpdate = new List<(Vector3I index, Vector3 min, Vector3 max, int newLOD)>();

    foreach (var kvp in activeChunks)
    {
      var chunkIndex = kvp.Key;
      var chunkData = kvp.Value;

      // Calculate distance from camera to chunk center
      var chunkCenter = (chunkData.WorldMin + chunkData.WorldMax) / 2f;
      float distance = camera.GlobalPosition.DistanceTo(chunkCenter);

      int desiredLOD = DetermineLODLevel(distance);

      if (desiredLOD != chunkData.CurrentLOD)
      {
        chunksToUpdate.Add((chunkIndex, chunkData.WorldMin, chunkData.WorldMax, desiredLOD));
      }
    }

    // Limit updates per frame to avoid performance spikes
    int maxUpdatesPerFrame = 2;
    foreach (var (index, min, max, newLOD) in chunksToUpdate.Take(maxUpdatesPerFrame))
    {
      CreateChunk(index, min, max, newLOD);
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
    return lodDistances.Length - 1;
  }
}
