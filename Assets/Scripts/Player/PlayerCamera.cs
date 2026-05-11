using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    private Camera _camera;
    private AudioListener _audioListener;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        _audioListener = GetComponent<AudioListener>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            _camera.enabled = false;

            if (_audioListener != null)
                _audioListener.enabled = false;
        }
    }
}