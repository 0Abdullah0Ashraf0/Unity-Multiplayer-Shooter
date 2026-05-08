using UnityEngine;
using Unity.Netcode;

public class PlayerBodyVisibility : NetworkBehaviour
{
    [SerializeField] private Renderer[] _bodyRenderers;
    [SerializeField] private GameObject[] _bodyObjects;

    public override void OnNetworkSpawn()
    {

        if (IsOwner)
        {
            // Move own body to hidden layer — main camera excludes this
            foreach (var go in _bodyObjects)
                go.layer = LayerMask.NameToLayer("FirstPersonBody");

            // Still cast shadows so it looks right to others
            foreach (var r in _bodyRenderers)
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        else
        {
            // Remote players stay on Default — fully visible to everyone
            foreach (var go in _bodyObjects)
                go.layer = LayerMask.NameToLayer("Default");
        }
    }
}