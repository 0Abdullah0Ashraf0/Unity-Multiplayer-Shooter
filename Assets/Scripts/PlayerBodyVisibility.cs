using UnityEngine;
using Unity.Netcode;

public class PlayerBodyVisibility : NetworkBehaviour
{
    [SerializeField] private Renderer[] _bodyRenderers;
    [SerializeField] private GameObject[] _bodyObjects;

    [Header("First Person Gun (what YOU see)")]
    [SerializeField] private GameObject[] _firstPersonGun;   // CameraWeapon layer

    [Header("Third Person Gun (what OTHERS see)")]
    [SerializeField] private GameObject[] _thirdPersonGun;   // Default layer

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            foreach (var go in _bodyObjects)
                go.layer = LayerMask.NameToLayer("FirstPersonBody");
            foreach (var r in _bodyRenderers)
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            foreach (var go in _firstPersonGun)
            {
                go.layer = LayerMask.NameToLayer("CameraWeapon");
                go.SetActive(true);
            }

            foreach (var go in _thirdPersonGun)
                go.SetActive(false);
        }
        else
        {
            foreach (var go in _bodyObjects)
                go.layer = LayerMask.NameToLayer("Default");

            foreach (var go in _firstPersonGun)
                go.SetActive(false);
            foreach (var go in _thirdPersonGun)
            {
                go.layer = LayerMask.NameToLayer("Default");
                go.SetActive(true);
            }
        }
    }
}