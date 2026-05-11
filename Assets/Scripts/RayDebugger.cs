using UnityEngine;

public class RayDebugger : MonoBehaviour
{
    private Camera _cam;

    void Start() => _cam = GetComponent<Camera>();

    void Update()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Debug.DrawRay(ray.origin, ray.direction * 500f, Color.green);
    }
}