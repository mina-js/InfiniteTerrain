using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];

        for(int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)
        {
            scale = 0.001f; //"clamp" it
        }

        float maxNoiseHeight = float.MinValue; // track these for normalizing later
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for(int y = 0; y < mapHeight; y++)
        {
            for(int x=0; x < mapWidth; x++)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = frequency * (x-halfWidth) / scale + octaveOffsets[i].x;
                    float sampleY = frequency * (y-halfHeight) / scale + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //to bound between -1 to 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence; //should decrease bc amplitude < 1
                    frequency *= lacunarity; //should increase bc lacunarity > 1
                }
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                noiseMap[x, y] = noiseHeight;

            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]); //returns value between 0-1 normalized based on the min and max
            }
        }


        return noiseMap;
    }
}
