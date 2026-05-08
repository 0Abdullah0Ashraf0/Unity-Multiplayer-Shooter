using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerAudio : NetworkBehaviour
{
    [Header("Audio Sources (on Body child)")]
    [SerializeField] private AudioSource _weaponSource;
    [SerializeField] private AudioSource _movementSource;
    [SerializeField] private AudioSource _footstepSource;

    [Header("Weapon Clips")]
    [SerializeField] private AudioClip _shootClip;
    [SerializeField] private AudioClip _reloadClip;
    [SerializeField] private AudioClip _emptyClip;

    [Header("Movement Clips")]
    [SerializeField] private AudioClip _jumpClip;
    // [SerializeField] private AudioClip _landClip;
    [SerializeField] private AudioClip _footstepClip;

    [Header("Player Clips")]
    [SerializeField] private AudioClip _hurtClip1;
    [SerializeField] private AudioClip _hurtClip2;
    [SerializeField] private AudioClip _deathClip;

    [Header("Kill")]
    [SerializeField] private AudioClip _killClip;
    private bool _hurtToggle;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Owner hears their own sounds flat/2D — no positional weirdness
        _weaponSource.spatialBlend   = 0f;
        _movementSource.spatialBlend = 0f;
        _footstepSource.spatialBlend = 0f;
    }


    // WEAPON
    public void PlayShoot()  => PlayWeaponServerRpc(SoundID.Shoot);
    public void PlayReload() => PlayWeaponServerRpc(SoundID.Reload);
    public void PlayEmpty()  => _weaponSource.PlayOneShot(_emptyClip); // local only

    // MOVEMENT
    public void PlayJump()     => PlayMovementServerRpc(SoundID.Jump);
    public void PlayJumpLocal() => _movementSource.PlayOneShot(_jumpClip);
    public void PlayLand()     => PlayMovementServerRpc(SoundID.Land);

    // FOOTSTEPS 
    public bool isFootstepPlaying() => _footstepSource.isPlaying;
    public void PlayFootstep() => PlayFootstepServerRpc();
    public void PlayFootstepLocal() {
        _footstepSource.clip = _footstepClip;
        _footstepSource.loop = true;
        _footstepSource.Play();
    }
    public void StopFootsteps()
    {
        _footstepSource.Stop();          // local
        StopFootstepServerRpc();         // remote
    }

    // HEALTH
    public void PlayHurt()
    {
        AudioClip clip = _hurtToggle ? _hurtClip1 : _hurtClip2;
        _hurtToggle = !_hurtToggle;
        _movementSource.PlayOneShot(clip);
    }
    public void PlayDeath() => _weaponSource.PlayOneShot(_deathClip); // local only

    public void PlayKill() => _weaponSource.PlayOneShot(_killClip); //local only

    // ── Networking ──────────────────────────────────────────────────────

    [ServerRpc]
    private void PlayWeaponServerRpc(SoundID id) => PlayWeaponClientRpc(id);

    [ServerRpc]
    private void PlayMovementServerRpc(SoundID id) => PlayMovementClientRpc(id);
    [ServerRpc]
    private void PlayFootstepServerRpc() => PlayFootstepClientRpc();
    [ServerRpc]
    private void StopFootstepServerRpc() => StopFootstepClientRpc();

    [ClientRpc]
    private void PlayFootstepClientRpc()
    {
        if (IsOwner) return; // already playing local

        _footstepSource.clip = _footstepClip;
        _footstepSource.loop = true;
        _footstepSource.Play();
    }

    [ClientRpc]
    private void StopFootstepClientRpc()
    {
        if (IsOwner) return;
        _footstepSource.Stop();
    }

    [ClientRpc]
    private void PlayWeaponClientRpc(SoundID id)
    {
        AudioClip clip = id switch
        {
            SoundID.Shoot  => _shootClip,
            SoundID.Reload => _reloadClip,
            _ => null
        };
        if (clip) _weaponSource.PlayOneShot(clip);
    }

    [ClientRpc]
    private void PlayMovementClientRpc(SoundID id)
    {
        if (IsOwner) return;

        AudioClip clip = id switch
        {
            SoundID.Jump     => _jumpClip,
            // SoundID.Land     => _landClip,
            _ => null
        };
        if (clip) _movementSource.PlayOneShot(clip);
    }

    private enum SoundID : byte
    {
        Shoot, Reload, Jump, Land
    }
}