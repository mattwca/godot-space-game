interface IPerlinNoise
{
  int OctaveCount { get; set; }
  float Lacunarity { get; set; }
  float Persistence { get; set; }
  float Frequency { get; set; }

  float getNoiseValue(float x, float y, float z);
  float GenerateNoiseValue(float x, float y, float z);
}