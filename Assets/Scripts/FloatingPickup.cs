using UnityEngine;

public class FloatingPickup : MonoBehaviour
{
    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 1f, 0f);

    private Vector3 _startPosition;
    private Vector3 _startScale;

    private void Start()
    {
        _startPosition = transform.position;
        _startScale = transform.localScale;
    }

    private void Update()
    {
        // float up / down
        float yOffset =
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.position = new Vector3(
            _startPosition.x,
            _startPosition.y + yOffset,
            _startPosition.z);

        transform.Rotate(
            rotationAxis.normalized,
            Space.Self);

    }
}