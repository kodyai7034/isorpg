using System;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// 3D orthographic camera controller for isometric view.
    /// Orbits around a look target at a fixed isometric pitch (30°).
    /// Q/E rotates 90° with smooth lerp. WASD pans relative to camera facing.
    /// Scroll zooms via orthographic size.
    ///
    /// The camera orbits actual 3D geometry — no grid re-projection needed.
    /// Rotation gives correct views of all block faces automatically.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 8f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 1f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 15f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 5f;

        private Camera _camera;
        private int _rotationIndex;
        private float _currentYaw;
        private float _targetYaw;
        private Vector3 _lookTarget;
        private bool _isDragging;
        private Vector3 _dragOrigin;

        /// <summary>Current rotation step (0-3).</summary>
        public int RotationIndex => _rotationIndex;

        /// <summary>Fired when camera rotation changes.</summary>
        public event Action<int> OnCameraRotated;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = GridConstants.DefaultOrthoSize;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 200f;

            _currentYaw = GridConstants.IsoYaw;
            _targetYaw = GridConstants.IsoYaw;

            UpdateCameraTransform();
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
            HandleRotation();
            HandleMiddleMouseDrag();
            UpdateCameraTransform();
        }

        private void HandlePan()
        {
            float h = 0, v = 0;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1;

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                // Pan relative to camera facing direction (projected onto XZ plane)
                var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                var move = (right * h + forward * v).normalized * panSpeed * Time.deltaTime;
                _lookTarget += move;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _camera.orthographicSize -= scroll * zoomSpeed;
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom);
            }
        }

        private void HandleRotation()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _rotationIndex = (_rotationIndex + 1) % 4;
                _targetYaw = GridConstants.IsoYaw + _rotationIndex * 90f;
                OnCameraRotated?.Invoke(_rotationIndex);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                _rotationIndex = (_rotationIndex + 3) % 4;
                _targetYaw = GridConstants.IsoYaw + _rotationIndex * 90f;
                OnCameraRotated?.Invoke(_rotationIndex);
            }

            // Smooth lerp to target yaw
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, rotationSpeed * Time.deltaTime);
        }

        private void HandleMiddleMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _isDragging = true;
                _dragOrigin = GetMouseWorldPos();
            }

            if (Input.GetMouseButton(2) && _isDragging)
            {
                var current = GetMouseWorldPos();
                var delta = _dragOrigin - current;
                delta.y = 0; // only pan on XZ plane
                _lookTarget += delta;
            }

            if (Input.GetMouseButtonUp(2))
                _isDragging = false;
        }

        private void UpdateCameraTransform()
        {
            // Camera rotation: fixed pitch, rotating yaw
            var rotation = Quaternion.Euler(GridConstants.IsoPitch, _currentYaw, 0);
            transform.rotation = rotation;

            // Position: offset from look target along camera's backward direction
            transform.position = _lookTarget - transform.forward * GridConstants.CameraDistance;
        }

        private Vector3 GetMouseWorldPos()
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return _lookTarget;
        }

        // --- Public API ---

        /// <summary>Smoothly pan camera to center on a world position.</summary>
        public void FocusOn(Vector3 worldPosition)
        {
            _lookTarget = worldPosition;
        }

        /// <summary>Instantly snap camera to look at position.</summary>
        public void SnapTo(Vector3 worldPosition)
        {
            _lookTarget = worldPosition;
            UpdateCameraTransform();
        }
    }
}
