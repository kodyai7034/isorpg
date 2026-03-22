using UnityEngine;

namespace IsoRPG.Map
{
    [RequireComponent(typeof(Camera))]
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 5f;
        [SerializeField] private bool enableEdgePan = true;
        [SerializeField] private float edgePanThreshold = 20f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 1f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 10f;

        [Header("Follow")]
        [SerializeField] private float followSpeed = 5f;

        private Camera _camera;
        private Transform _followTarget;
        private bool _isFollowing;
        private int _rotationIndex; // 0-3 for 4-way rotation

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
            HandleRotation();
            HandleFollow();
        }

        private void HandlePan()
        {
            var move = Vector3.zero;

            // WASD / Arrow keys
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;

            // Edge pan
            if (enableEdgePan)
            {
                var mousePos = Input.mousePosition;
                if (mousePos.x < edgePanThreshold) move.x -= 1;
                if (mousePos.x > Screen.width - edgePanThreshold) move.x += 1;
                if (mousePos.y < edgePanThreshold) move.y -= 1;
                if (mousePos.y > Screen.height - edgePanThreshold) move.y += 1;
            }

            if (move != Vector3.zero)
            {
                _isFollowing = false;
                transform.position += move.normalized * panSpeed * Time.deltaTime;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
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
                // TODO: Re-sort all tiles and units for new rotation
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                _rotationIndex = (_rotationIndex + 3) % 4;
            }
        }

        private void HandleFollow()
        {
            if (_isFollowing && _followTarget != null)
            {
                var target = new Vector3(_followTarget.position.x, _followTarget.position.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);
            }
        }

        public void FollowTarget(Transform target)
        {
            _followTarget = target;
            _isFollowing = true;
        }

        public void CenterOn(Vector3 position)
        {
            _isFollowing = false;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        public int RotationIndex => _rotationIndex;
    }
}
