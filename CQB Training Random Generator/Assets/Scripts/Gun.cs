using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Gun : UdonSharpBehaviour
{
    [Header("Gun References")]
    public ParticleSystem particle;
    public VRC_Pickup pickup;
    public AudioSource audioSource;
    public AudioClip gunshotClip;

    [Header("Fire Settings")]
    public bool fullAuto = false;
    public int fireRate = 600; // rounds per minute

    private float nextFireTime;
    private bool triggerWasHeld;

    private void Update()
    {
        if (!fullAuto || !triggerWasHeld)
            return;

        if (pickup == null || !pickup.IsHeld || pickup.currentPlayer != Networking.LocalPlayer)
        {
            triggerWasHeld = false;
            return;
        }

        if (!Networking.IsOwner(gameObject))
        {
            triggerWasHeld = false;
            return;
        }

        TryFire();
    }

    public override void OnPickupUseDown()
    {
        if (pickup == null || !pickup.IsHeld || pickup.currentPlayer != Networking.LocalPlayer)
            return;

        if (!Networking.IsOwner(gameObject))
            return;

        triggerWasHeld = true;
        TryFire();
    }

    public override void OnPickupUseUp()
    {
        triggerWasHeld = false;
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + 60f / fireRate;

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Fire));
    }

    public void Fire()
    {
        if (particle == null)
            return;

        particle.Emit(1);

        if (audioSource != null && gunshotClip != null)
        {
            audioSource.PlayOneShot(gunshotClip);
        }
    }
}