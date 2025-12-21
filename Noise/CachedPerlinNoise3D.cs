
using System.Collections.Generic;

namespace GodotSpaceGameTest.Noise
{
  class CachedPerlinNoise3D : PerlinNoise3D, IPerlinNoise
  {
    private readonly PerlinNoise3D perlinNoise;
    private readonly Dictionary<(float x, float y, float z), float> cache;

    public CachedPerlinNoise3D(int seed) : base(seed)
    {
      this.cache = new Dictionary<(float x, float y, float z), float>();
    }

    public new float GenerateNoiseValue(float x, float y, float z)
    {
      if (this.cache.ContainsKey((x, y, z)))
      {
        return this.cache[(x, y, z)];
      }

      var value = base.GenerateNoiseValue(x, y, z);
      this.cache.Add((x, y, z), value);
      return value;
    }
  }
}