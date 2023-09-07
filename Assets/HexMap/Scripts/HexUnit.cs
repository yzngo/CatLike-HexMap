using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HexMap.Scripts
{
    public class HexUnit : MonoBehaviour
    {
        public static HexUnit unitPrefab;

        public float travelSpeed = 4f;
        public float rotationSpeed = 180f;

        private HexCell location;

        // 此 unit 当前所处的格子
        public HexCell Location
        {
            get => location;
            set
            {
                if (location)
                {
                    location.Unit = null;
                }

                location = value;
                value.Unit = this;
                transform.localPosition = value.Position;
            }
        }

        public void ValidateLocation()
        {
            transform.localPosition = location.Position;
        }

        private float orientation;

        public float Orientation
        {
            get => Orientation;
            set
            {
                orientation = value;
                transform.localRotation = Quaternion.Euler(0, value, 0);
            }
        }

        private IEnumerator LookAt(Vector3 point)
        {
            point.y = transform.localPosition.y;
            Quaternion fromRotation = transform.localRotation;
            Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);
            float angle = Quaternion.Angle(fromRotation, toRotation);
            if (angle > 0)
            {
                float speed = rotationSpeed / angle;
                for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
                {
                    transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                    yield return null;
                }
                transform.LookAt(point);
                orientation = transform.localRotation.eulerAngles.y;
            }
        }

        private void OnEnable()
        {
            if (location)
            {
                transform.localPosition = location.Position;
            }
        }

        public void Die()
        {
            location.Unit = null;
            Destroy(gameObject);
        }

        public bool IsValidDestination(HexCell cell)
        {
            return !cell.IsUnderwater && !cell.Unit;
        }

        public void Save(BinaryWriter writer)
        {
            location.coordinates.Save(writer);
            writer.Write(orientation);
        }

        public static void Load(BinaryReader reader, HexGrid grid)
        {
            HexCoordinates coordinates = HexCoordinates.Load(reader);
            float orientation = reader.ReadSingle();
            grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
        }

        private List<HexCell> pathToTravel;

        public void Travel(List<HexCell> path)
        {
            Location = path[^1];
            pathToTravel = path;
            StopAllCoroutines();
            StartCoroutine(TravelPath());
        }

        private IEnumerator TravelPath()
        {
            // 每次都到达邻居 cell 的中心，之后再到达邻居的邻居的中心
            // for (int i = 1; i < pathToTravel.Count; i++)
            // {
            //     Vector3 a = pathToTravel[i - 1].Position;
            //     Vector3 b = pathToTravel[i].Position;
            //     for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
            //     {
            //         transform.localPosition = Vector3.Lerp(a, b, t);
            //         yield return null;
            //     }
            // }

            // 不到达邻居的中心，而是沿edge到edge移动 - Edge-based Paths
            Vector3 a;
            Vector3 b;
            Vector3 c = pathToTravel[0].Position;
            transform.localPosition = c;
            yield return LookAt(pathToTravel[1].Position);
            float t = Time.deltaTime * travelSpeed;
            
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                a = c;
                b = pathToTravel[i - 1].Position;
                c = (b + pathToTravel[i].Position) * 0.5f;
                for (; t < 1f; t += Time.deltaTime * travelSpeed)
                {
                    transform.localPosition = Bezier.GetPoint(a, b, c, t);
                    Vector3 d = Bezier.GetDerivative(a, b, c, t);
                    d.y = 0f;
                    transform.localRotation = Quaternion.LookRotation(d);
                    yield return null;
                }
                t -= 1f;
            }

            a = c;
            b = pathToTravel[^1].Position;
            c = b;
            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            transform.localPosition = location.Position;
            orientation = transform.localRotation.eulerAngles.y;
            ListPool<HexCell>.Add(pathToTravel);
            pathToTravel = null;
        }

        private void OnDrawGizmos()
        {
            if (pathToTravel == null || pathToTravel.Count == 0) return;
            // OnDrawGizmosCenterToCenter();
            // OnDrawGizmosEdgeToEdge();
            OnDrawGizmosEdgeToEdgeSmooth();
        }


        // 每次都到达邻居 cell 的中心，之后再到达邻居的邻居的中心
        private void OnDrawGizmosCenterToCenter()
        {
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                Vector3 a = pathToTravel[i - 1].Position;
                Vector3 b = pathToTravel[i].Position;
                for (float t = 0; t < 1f; t += 0.2f)
                {
                    Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 2f);
                }
            }
        }

        // 不到达邻居的中心，而是沿edge到edge移动 - Edge-based Paths
        private void OnDrawGizmosEdgeToEdge()
        {
            Vector3 a;
            Vector3 b = pathToTravel[0].Position;
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                a = b;
                b = (pathToTravel[i - 1].Position + pathToTravel[i].Position) * 0.5f;
                for (float t = 0f; t < 1f; t += 0.2f)
                {
                    Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 2f);
                }
            }

            a = b;
            b = pathToTravel[^1].Position;
            for (float t = 0; t < 1f; t += 0.2f)
            {
                Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 2f);
            }
        }
        
        // Edge-based Paths, 且使用Bezier平滑
        private void OnDrawGizmosEdgeToEdgeSmooth()
        {
            Vector3 a;
            Vector3 b;
            Vector3 c = pathToTravel[0].Position;
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                a = c;
                b = pathToTravel[i - 1].Position;
                c = (b + pathToTravel[i].Position) *0.5f;
                for (float t = 0f; t < 1f; t += 0.2f)
                {
                    Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
                }
            }

            a = c;
            b = pathToTravel[^1].Position;
            c = b;
            for (float t = 0; t < 1f; t += 0.2f)
            {
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }
        }
        
    }
}