using System;
using UnityEngine;

namespace IsoRPG.Map
{
    /// <summary>
    /// Orthographic camera controller for isometric battle view.
    /// Supports WASD/arrow pan, scroll zoom, Q/E rotation, middle-mouse drag, and smooth follow.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 5f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 1f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 10f;

        [Header("Follow")]
        [SerializeField] private float followSpeed = 5f;

        private Camera _camera;
        private int _rotationIndex;
        private Vector3 _focusTarget;
        private bool _isFocusing;
        private Vector3 _dragOrigin;
        private bool _isDragging;

        /// <summary>Current rotation step (0-3). Each step is 90° clockwise.</summary>
        public int RotationIndex => _rotationIndex;

        /// <summary>Fired when camera rotation changes. Passes new rotation index (0-3).</summary>
        public event Action<int> OnCameraRotated;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            if (_camera.orthographicSize < minZoom || _camera.orthographicSize > maxZoom)
                _camera.orthographicSize = 5f;
        }

        private void Update()
        {
            HandleKeyboardPan();
            HandleMiddleMouseDrag();
            HandleZoom();
            HandleRotation();
            HandleFocus();
        }

        private void HandleKeyboardPan()
        {
            var move = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;

            if (move.sqrMagnitude > 0.01f)
            {
                _isFocusing = false;
                transform.position += move.normalized * (panSpeed * Time.deltaTime);
            }
        }

        private void HandleMiddleMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _isDragging = true;
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
                _isFocusing = false;
            }

            if (Input.GetMouseButton(2) && _isDragging)
            {
                var currentWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
                var delta = _dragOrigin - currentWorld;
                transform.position += delta;
            }

            if (Input.GetMouseButtonUp(2))
            {
                _isDragging = false;
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
                OnCameraRotated?.Invoke(_rotationIndex);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                _rotationIndex = (_rotationIndex + 3) % 4; // +3 == -1 mod 4
                OnCameraRotated?.Invoke(_rotationIndex);
            }
        }

        private void HandleFocus()
        {
            if (!_isFocusing) return;

            var target = new Vector3(_focusTarget.x, _focusTarget.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);

            // Stop focusing when close enough
            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                transform.position = target;
                _isFocusing = false;
            }
        }

        /// <summary>
        /// Smoothly pan camera to center on a world position.
        /// </summary>
        /// <param name="worldPosition">Target position to center on.</param>
        public void FocusOn(Vector3 worldPosition)
        {
            _focusTarget = worldPosition;
            _isFocusing = true;
        }

        /// <summary>
        /// Instantly snap camera to a world position (no lerp).
        /// </summary>
        /// <param name="worldPosition">Target position to center on.</param>
        public void SnapTo(Vector3 worldPosition)
        {
            _isFocusing = false;
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }
    }
}
