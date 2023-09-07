using System;
using UnityEngine;

namespace HexMap.Scripts
{
    public class HexMapCamera : MonoBehaviour
    {
        private static HexMapCamera instance;

        public static bool Locked
        {
            set => instance.enabled = !value;
        }

        // 控制旋转
        private Transform swivel;

        // 控制远近
        private Transform stick;

        // 0 最远，1 最近
        private float zoom = 1f;

        public float stickMinZoom = -250;
        public float stickMaxZoom = -45;

        public float swivelMinZoom = 90;
        public float swivelMaxZoom = 45;

        public float moveSpeedMinZoom = 400;
        public float moveSpeedMaxZoom = 100;

        public float rotationSpeed = 180;
        private float rotationAngle;

        public HexGrid grid;

        private void Awake()
        {
            swivel = transform.GetChild(0);
            stick = swivel.GetChild(0);
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void Update()
        {
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (Math.Abs(zoomDelta) > 0.0001f)
            {
                AdjustZoom(zoomDelta);
            }

            float rotationDelta = Input.GetAxis("Rotation");
            if (rotationDelta != 0)
            {
                AdjustRotation(rotationDelta);
            }

            float xDelta = Input.GetAxis("Horizontal");
            float zDelta = Input.GetAxis("Vertical");
            if (xDelta != 0 || zDelta != 0)
            {
                AdjustPosition(xDelta, zDelta);
            }
        }

        private void AdjustZoom(float delta)
        {
            zoom = Mathf.Clamp01(zoom + delta);
            float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
            stick.localPosition = new Vector3(0, 0, distance);
            float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
            swivel.localRotation = Quaternion.Euler(angle, 0, 0);
        }

        private void AdjustPosition(float xDelta, float zDelta)
        {
            Vector3 direction = transform.localRotation * new Vector3(xDelta, 0, zDelta).normalized;
            float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
            float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;
            Vector3 position = transform.localPosition;
            position += direction * distance;
            transform.localPosition = ClampPosition(position);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            float xMax = (grid.cellCountX - 0.5f) * (2f * HexMetrics.innerRadius);
            position.x = Mathf.Clamp(position.x, 0, xMax);
            float zMax = (grid.cellCountZ * HexMetrics.chunkSizeZ - 1) * (1.5f * HexMetrics.outerRadius);
            position.z = Mathf.Clamp(position.z, 0, zMax);
            return position;
        }

        private void AdjustRotation(float delta)
        {
            rotationAngle += delta * rotationSpeed * Time.deltaTime;
            if (rotationAngle < 0)
            {
                rotationAngle += 360;
            }
            else if (rotationAngle >= 360)
            {
                rotationAngle -= 360;
            }

            transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
        }

        public static void ValidatePosition()
        {
            instance.AdjustPosition(0f, 0f);
        }
    }
}