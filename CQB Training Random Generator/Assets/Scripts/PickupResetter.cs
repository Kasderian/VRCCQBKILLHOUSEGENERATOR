using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

public class PickupResetter : UdonSharpBehaviour
{
    [Header("Pickup Objects")]
    public GameObject[] pickups;

    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;

    private void Start()
    {
        if (pickups == null)
            return;

        originalPositions = new Vector3[pickups.Length];
        originalRotations = new Quaternion[pickups.Length];

        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] == null)
                continue;

            originalPositions[i] = pickups[i].transform.position;
            originalRotations[i] = pickups[i].transform.rotation;
        }
    }

    public override void Interact()
    {
        ResetAllPickups();
    }

    private void ResetAllPickups()
    {
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] == null)
                continue;

            VRCPlayerApi owner = Networking.GetOwner(pickups[i]);

            if (owner != Networking.LocalPlayer)
            {
                Networking.SetOwner(Networking.LocalPlayer, pickups[i]);
            }

            ResetPickup(i);
        }
    }

    private void ResetPickup(int index)
    {
        if (index < 0 || index >= pickups.Length)
            return;

        GameObject pickup = pickups[index];

        if (pickup == null)
            return;

        // Reset position and rotation.
        pickup.transform.position = originalPositions[index];
        pickup.transform.rotation = originalRotations[index];

        // Stop physics.
        Rigidbody rb = pickup.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Tell VRC Object Sync that the object has been moved.
        VRCObjectSync sync = pickup.GetComponent<VRCObjectSync>();

        if (sync != null)
        {
            sync.FlagDiscontinuity();
        }
    }
}