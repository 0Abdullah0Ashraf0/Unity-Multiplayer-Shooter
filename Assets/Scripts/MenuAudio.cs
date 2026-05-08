using UnityEngine;

public class MenuAudio : MonoBehaviour
{
    public static MenuAudio Instance;

    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _connectingClip;  
    [SerializeField] private AudioClip _wrongIpClip;    
    [SerializeField] private AudioClip _connectedClip;   

    private AudioSource _source;

    void Awake()
    {
        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.spatialBlend = 0f; // 2D
    }

    public void PlayClick()      => _source.PlayOneShot(_clickClip);
    public void PlayWrongIp()    => _source.PlayOneShot(_wrongIpClip);
    public void PlayConnected()  => _source.PlayOneShot(_connectedClip);

    public void PlayConnecting()
    {
        _source.clip = _connectingClip;
        _source.loop = true;
        _source.Play();
    }

    public void StopConnecting() => _source.Stop();
}