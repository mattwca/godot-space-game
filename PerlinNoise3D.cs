using System;
using System.Runtime.ConstrainedExecution;
using Godot;

public class PerlinNoise3D {
	private static readonly int PERMUTATION_SIZE = 256;

	private int _seed;
	public int Seed {
		get => _seed;
		set
		{
			if (value != this._seed) {
				this._seed = value;
				this.shufflePermutations();
			}
		}
	}

	public int OctaveCount { get; set; } = 4;

	public float Lacunarity { get; set; } = 2.0f;

	public float Persistence { get; set; } = 0.5f;

	public float Frequency { get; set; } = 2f;

	private int[] permutations;

	private float[][] gradientTable = new float[][] {
		new float[] {0, 1, 1},
		new float[] {0, 1, -1},
		new float[] {0, -1, 1},
		new float[] {0, -1, -1},
		new float[] {1, 0, 1},
		new float[] {1, 0, -1},
		new float[] {-1, 0, 1},
		new float[] {-1, 0, -1},
		new float[] {1, 1, 0},
		new float[] {1, -1, 0},
		new float[] {-1, 1, 0},
		new float[] {-1, -1, 0}
	};

	public PerlinNoise3D(int seed) {
		// Create permutation table
		permutations = new int[PERMUTATION_SIZE];

		for (var i = 0; i < PERMUTATION_SIZE; i++) {
			permutations[i] = i;
		}

		this.Seed = seed;
	}

	private void shufflePermutations() {
		Random r = new Random(this.Seed);
		for (var i = 0; i < PERMUTATION_SIZE; i++) {
			var newPosition = r.Next(PERMUTATION_SIZE - 1);
			var oldValue = permutations[newPosition];
			permutations[newPosition] = permutations[i];
			permutations[i] = oldValue;
		}
	}

	private int getPermutation(int x) {
		return permutations[x & (PERMUTATION_SIZE - 1)];
	}

	private float[] getGradient(int x, int y, int z) {
		int lookup = getPermutation(x + getPermutation(y + getPermutation(z)));
		return gradientTable[lookup % 12];
	}
	private float dot(float[] x, float[] y) {
		return x[0] * y[0] + x[1] * y[1] + x[2] * y[2];
	}

	private float[] sub(float[] x, float[] y) {
		return new float[] {x[0] - y[0], x[1] - y[1], x[2] - y[2]};
	}

	public float getNoiseValue(float x, float y, float z) {
		int floorX = (int) Math.Floor(x);
		int floorY = (int) Math.Floor(y);
		int floorZ = (int) Math.Floor(z);

		float[] s = getGradient(floorX, floorY, floorZ);
		float[] t = getGradient(floorX + 1, floorY, floorZ);

		float[] u = getGradient(floorX, floorY + 1, floorZ);
		float[] v = getGradient(floorX + 1, floorY + 1, floorZ);

		float[] o = getGradient(floorX, floorY, floorZ + 1);
		float[] p = getGradient(floorX + 1, floorY, floorZ + 1);

		float[] q = getGradient(floorX, floorY + 1, floorZ + 1);
		float[] r = getGradient(floorX + 1, floorY + 1, floorZ + 1);

		float[] noisePosition = new float[] {x, y, z};

		float dot01 = dot(s, sub(noisePosition, new float[] {floorX, floorY, floorZ}));
		float dot02 = dot(t, sub(noisePosition, new float[] {floorX + 1, floorY, floorZ}));

		float dot03 = dot(u, sub(noisePosition, new float[] {floorX, floorY + 1, floorZ}));
		float dot04 = dot(v, sub(noisePosition, new float[] {floorX + 1, floorY + 1, floorZ}));

		float dot05 = dot(o, sub(noisePosition, new float[] {floorX, floorY, floorZ + 1}));
		float dot06 = dot(p, sub(noisePosition, new float[] {floorX + 1, floorY, floorZ + 1}));

		float dot07 = dot(q, sub(noisePosition, new float[] {floorX, floorY + 1, floorZ + 1}));
		float dot08 = dot(r, sub(noisePosition, new float[] {floorX + 1, floorY + 1, floorZ + 1}));

		float fadeX = 6 * (float) Math.Pow(x - floorX, 5) - 15 * (float) Math.Pow(x - floorX, 4) + 10 * (float)Math.Pow(x - floorX, 3);
		float fadeY = 6 * (float) Math.Pow(y - floorY, 5) - 15 * (float) Math.Pow(y - floorY, 4) + 10 * (float)Math.Pow(y - floorY, 3);
		float fadeZ = 6 * (float) Math.Pow(z - floorZ, 5) - 15 * (float) Math.Pow(z - floorZ, 4) + 10 * (float)Math.Pow(z - floorZ, 3);

		float a = (float) Mathf.Lerp(dot01, dot02, fadeX);
		float b = (float) Mathf.Lerp(dot03, dot04, fadeX);
		float c = (float) Mathf.Lerp(dot05, dot06, fadeX);
		float d = (float) Mathf.Lerp(dot07, dot08, fadeX);

		float e = (float) Mathf.Lerp(a, b, fadeY);
		float f = (float) Mathf.Lerp(c, d, fadeY);

		float g = (float) Mathf.Lerp(e, f, fadeZ);

		return g;
	}

	public float GenerateNoiseValue(float x, float y, float z) {
		float total = 0.0f;
		float localFrequency = this.Frequency;
		float amplitude = this.Persistence;

		for (int i = 0; i < this.OctaveCount; i++) {
			total += getNoiseValue(x * localFrequency, y * localFrequency, z * localFrequency) * amplitude;
			localFrequency *= this.Lacunarity;
			amplitude *= this.Persistence;
		}

		return total;
	}
}
