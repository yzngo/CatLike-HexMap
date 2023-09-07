using UnityEngine;

namespace HexMap.Scripts
{
    public static class Bezier 
    {
        
        public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            t = Mathf.Clamp01(t);
            float r = 1f - t;
            return r * r * a + 2f * r * t * b + t * t * c;
        }
        
        public static Vector3 GetPointUnclamped(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float r = 1f - t;
            return r * r * a + 2f * r * t * b + t * t * c;
        }

        // 可以使用曲线的导数来确定单位的方向
        public static Vector3 GetDerivative(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            return 2f * ((1f - t) * (b - a) + t * (c - b));
        }
    }
}