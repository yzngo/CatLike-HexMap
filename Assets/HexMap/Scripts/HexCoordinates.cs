using System.IO;
using UnityEngine;

namespace HexMap.Scripts
{
    // Cube Coordinates
    [System.Serializable]
    public struct HexCoordinates
    {
        [SerializeField]
        private int x, z;

        public int X => x;
        
        public int Y => -X - Z;
        public int Z => z;
        
        public HexCoordinates(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
        
        public static HexCoordinates FromOffsetCoordinates(int x, int z)
        {
            return new HexCoordinates(x - z / 2, z);
        }

        // 把 HexGrid 的局部坐标转换成 Hex Coordinates（Cube Coordinates）
        public static HexCoordinates FromPosition(Vector3 position)
        {
            float x = position.x / (HexMetrics.innerRadius * 2f);
            float y = -x;
            // 由于 z 轴的偏移，需要减去 z 轴的偏移量
            float offset = position.z / (HexMetrics.outerRadius * 3f);
            x -= offset;
            y -= offset;
            
            int iX = Mathf.RoundToInt(x);
            int iY = Mathf.RoundToInt(y);
            int iZ = Mathf.RoundToInt(-x - y);

            // 修正误差，大多发生在网格的边缘处
            if (iX + iY + iZ != 0)
            {
                float dX = Mathf.Abs(x - iX);
                float dY = Mathf.Abs(y - iY);
                float dZ = Mathf.Abs(-x - y - iZ);
                if (dX > dY && dX > dZ)
                {
                    iX = -iY - iZ;
                }
                else if (dZ > dY)
                {
                    iZ = -iX - iY;
                }
            }
            return new HexCoordinates(iX, iZ);
        }

        /// <summary>
        /// 计算两格子之间的最短距离
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int DistanceTo(HexCoordinates other)
        {
            // 本来XYZ是正负相互抵消，相加为0，
            // 全部取绝对值之后，正的加上负的刚好是距离的两倍，所以要除以2
            return ((X < other.X ? other.X - X : X - other.X) + 
                   (Y < other.Y ? other.Y - Y : Y - other.Y) +
                   (Z < other.Z ? other.Z - Z : Z - other.Z)) / 2;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }
        
        public string ToStringOnSeparateLines()
        {
            return X + "\n" + Y + "\n" +  Z;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(z);
        }

        // 因为 HexCoordinates 是一个 struct，所以不能直接 Load，必须重新构造一个
        public static HexCoordinates Load(BinaryReader reader)
        {
            HexCoordinates c;
            c.x = reader.ReadInt32();
            c.z = reader.ReadInt32();
            return c;
        }
    }
}