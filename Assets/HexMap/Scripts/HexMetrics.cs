using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexMap.Scripts
{
    // 六边形网格度量
    public static class HexMetrics
    {
        public const int chunkSizeX = 5;
        public const int chunkSizeZ = 5;

        // 外径
        public const float outerRadius = 10f;

        // 内径 outRadius * sqrt(3) / 2
        public const float innerRadius = outerRadius * outerToInner;
        public const float outerToInner = 0.866025404f;

        public const float innerToOuter = 1f / outerToInner;

        // 每个格子的顶点局部坐标定义
        private static Vector3[] corners =
        {
            new Vector3(0, 0, outerRadius),
            new Vector3(innerRadius, 0, 0.5f * outerRadius),
            new Vector3(innerRadius, 0, -0.5f * outerRadius),
            new Vector3(0, 0, -outerRadius),
            new Vector3(-innerRadius, 0, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0, 0.5f * outerRadius),
            new Vector3(0, 0, outerRadius),
        };


        // 每一段提升的高度(0~6)
        public const float elevationStep = 3f;

        // 每个斜坡的平台数量
        public const int terracesPerSlope = 2;

        // 每个斜坡的台阶数量
        public const int terraceSteps = terracesPerSlope * 2 + 1;
        public const float horizontalTerraceStepSize = 1f / terraceSteps;
        public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

        // 河床海拔偏移
        public const float streamBedElevationOffset = -1.75f;

        // 河面海拔偏移
        public const float waterElevationOffset = -0.5f;

        // 混合区域的比例
        public const float solidFactor = 0.8f;
        public const float blendFactor = 1f - solidFactor;

        public static Vector3 GetFirstCorner(HexDirection direction)
        {
            return corners[(int)direction];
        }

        public static Vector3 GetSecondCorner(HexDirection direction)
        {
            return corners[(int)direction + 1];
        }

        public static Vector3 GetFirstSolidCorner(HexDirection direction)
        {
            return corners[(int)direction] * solidFactor;
        }

        public static Vector3 GetSecondSolidCorner(HexDirection direction)
        {
            return corners[(int)direction + 1] * solidFactor;
        }

        public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
        {
            return (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
        }

        public static Vector3 GetBridge(HexDirection direction)
        {
            return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
        }

        // slope 插值，沿斜坡逐步提升，并形成平台
        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
        {
            float h = step * horizontalTerraceStepSize;
            // 这种形式的线性插值，解读为沿着 (b-a) 的方向移动一段距离
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;
            float v = (int)((step + 1) / 2) * verticalTerraceStepSize;
            a.y += (b.y - a.y) * v;
            return a;
        }

        public static Color TerraceLerp(Color a, Color b, int step)
        {
            float h = step * horizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }

        public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
        {
            if (elevation1 == elevation2)
            {
                return HexEdgeType.Flat;
            }

            int delta = elevation2 - elevation1;
            if (delta is 1 or -1)
            {
                return HexEdgeType.Slope;
            }

            return HexEdgeType.Cliff;
        }

        // 噪声扰动参数
        public static Texture2D noiseSource;
        public const float noiseScale = 0.003f;
        public const float cellPerturbStrength = 4f;

        public const float elevationPerturbStrength = 1.5f;

        // 采样噪声贴图
        public static Vector4 SampleNoise(Vector3 worldPos)
        {
            return noiseSource.GetPixelBilinear(worldPos.x * noiseScale, worldPos.z * noiseScale);
        }

        // 使用噪声扰乱顶点
        public static Vector3 Perturb(Vector3 worldPos)
        {
            Vector4 sample = SampleNoise(worldPos);
            worldPos.x += (sample.x * 2f - 1f) * cellPerturbStrength;
            worldPos.z += (sample.z * 2f - 1f) * cellPerturbStrength;
            return worldPos;
        }
        
        
        
        
        public const int hashGridSize = 256;
        public const float hashGridScale = 0.25f;
        
        private static HexHash[] hashGrid;
        public static void InitializeHashGrid(int seed)
        {
            hashGrid = new HexHash[hashGridSize * hashGridSize];
            Random.State currentState = Random.state;
            Random.InitState(seed);
            for (int i = 0; i < hashGrid.Length; i++)
            {
                hashGrid[i] = HexHash.Create();
            }
            Random.state = currentState;
        }

        public static HexHash SampleHashGrid(Vector3 position)
        {
            int x = (int)(position.x * hashGridScale) % hashGridSize;
            if (x < 0)
            {
                x += hashGridSize;
            }
            int z = (int)(position.z * hashGridScale) % hashGridSize;
            if (z < 0)
            {
                z += hashGridSize;
            }
            return hashGrid[x + z * hashGridSize];
        }

        private static float[][] featureThresholds =
        {
            new float[] { 0.0f, 0.0f, 0.4f },
            new float[] { 0.0f, 0.4f, 0.6f },
            new float[] { 0.4f, 0.6f, 0.8f },
        };
        
        public static float[] GetFeatureThresholds(int level)
        {
            return featureThresholds[level];
        }
        
        public const float wallHeight = 3f;
        public const float wallYOffset = -1f;
        public const float wallThickness = 0.75f;
        public const float wallTowerThreshold = 0.5f;

        public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
        {
            Vector3 offset;
            offset.x = far.x - near.x;
            offset.y = 0;
            offset.z = far.z - near.z;
            return offset.normalized * (wallThickness * 0.5f);
        }

        public const float wallElevationOffset = verticalTerraceStepSize;

        public static Vector3 WallLerp(Vector3 near, Vector3 far)
        {
            near.x += (far.x - near.x) * 0.5f;
            near.z += (far.z - near.z) * 0.5f;
            float v = near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
            near.y += (far.y - near.y) * v + wallYOffset;
            return near;
        }
        
        public const float bridgeDesignLength = 7f;

    }
}