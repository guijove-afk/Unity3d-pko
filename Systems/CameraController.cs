using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 12, -8);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);

    [Header("Movement")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float defaultZoom = 12f;

    [Header("Rotation")]
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private float minAngle = 10f;
    [SerializeField] private float maxAngle = 80f;

    [Header("Collision")]
    [SerializeField] private bool avoidCollision = true;
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private float collisionRadius = 0.3f;

    private float currentZoom;
    private float currentRotationX;
    private float currentRotationY;
    private Vector3 currentVelocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    void Start()
    {
        currentZoom = defaultZoom;
        currentRotationY = 45f;

        if (target == null)
            FindLocalPlayer();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindLocalPlayer();
            return;
        }

        HandleInput();
        UpdatePosition();
    }

    private void FindLocalPlayer()
    {
        var players = FindObjectsOfType<PlayerMovement>();
        foreach (var player in players)
        {
            if (player.isLocalPlayer)
            {
                target = player.transform;
                break;
            }
        }
    }

    private void HandleInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        if (allowRotation && Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentRotationY += mouseX * rotationSpeed;
        }

        if (allowRotation && Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentRotationY += mouseX * rotationSpeed;
        }
    }

    private void UpdatePosition()
    {
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 desiredPosition = target.position + rotation * new Vector3(0, 0, -currentZoom);
        desiredPosition += offset;

        if (avoidCollision)
        {
            Vector3 direction = desiredPosition - (target.position + lookAtOffset);
            float distance = direction.magnitude;

            if (Physics.SphereCast(target.position + lookAtOffset, collisionRadius,
                direction.normalized, out RaycastHit hit, distance, collisionLayers))
            {
                desiredPosition = hit.point + hit.normal * collisionRadius;
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
            ref currentVelocity, 1f / followSpeed);

        transform.LookAt(target.position + lookAtOffset);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ResetCamera()
    {
        currentZoom = defaultZoom;
        currentRotationY = 45f;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}