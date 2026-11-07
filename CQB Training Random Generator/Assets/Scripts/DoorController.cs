using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class DoorController : UdonSharpBehaviour
{
    [Header("Door References")]
    public Transform doorPivot;
    public Transform handle;
    public Transform doorMesh;
    public Transform handleMesh;

    [Header("Components")]
    public VRC_Pickup pickup;
    public Rigidbody handleRigidbody;
    public VRCObjectSync handleObjectSync;

    [Header("Door Settings")]
    public float minAngle = -90f;
    public float maxAngle = 90f;

    [Header("Handle Settings")]
    public float stopVelocity = 0.05f;
    public float stopAngularVelocity = 0.05f;
    public float handleResetDelay = 1f;

    [UdonSynced(UdonSyncMode.Linear)]
    private float syncedAngle = 0f;

    private bool wasHeld = false;
    private bool waitingForHandleToStop = false;
    private float handleReleaseTime = 0f;


    private void Start()
    {
        ApplyDoorRotation(syncedAngle);
    }
    private void Update()
    {
        if (pickup == null)
            return;

        bool held = pickup.IsHeld;

        // --------------------------------
        // Grabbed
        // --------------------------------

        if (held && !wasHeld)
        {
            // Cancel the release timer.
            waitingForHandleToStop = false;
            handleReleaseTime = 0f;

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(
                    Networking.LocalPlayer,
                    gameObject
                );
            }
        }

        // --------------------------------
        // While being held
        // --------------------------------

        if (held && Networking.IsOwner(gameObject))
        {
            CalculateDoorAngle();
        }

        // --------------------------------
        // Released
        // --------------------------------

        if (!held && wasHeld)
        {
            waitingForHandleToStop = true;
            handleReleaseTime = Time.time;
        }

        // --------------------------------
        // Wait for Rigidbody to stop
        // --------------------------------

        if (!held && waitingForHandleToStop)
        {
            CalculateDoorAngle();
            bool handleStopped = HandleHasStopped();

            bool timeoutReached =
                Time.time - handleReleaseTime >= handleResetDelay;

            if (handleStopped || timeoutReached)
            {
                MoveHandleToVisibleHandle();

                waitingForHandleToStop = false;
                handleReleaseTime = 0f;
            }
        }

        wasHeld = held;
    }

    private void CalculateDoorAngle()
    {
        Vector3 localPosition =
            doorPivot.InverseTransformPoint(handle.position);

        // Door points along +X at 0 degrees.
        float angle =
            -Mathf.Atan2(localPosition.z, localPosition.x)
            * Mathf.Rad2Deg;

        angle = Mathf.Clamp(
            angle,
            minAngle,
            maxAngle
        );

        syncedAngle = angle;

        ApplyDoorRotation(angle);
    }

    private void ApplyDoorRotation(float angle)
    {
        doorMesh.localRotation =
            Quaternion.Euler(0f, angle, 0f);
    }

    private bool HandleHasStopped()
    {
        if (handleRigidbody == null)
            return true;

        return handleRigidbody.velocity.magnitude <
                   stopVelocity
            &&
               handleRigidbody.angularVelocity.magnitude <
                   stopAngularVelocity;
    }

    private void MoveHandleToVisibleHandle()
    {
        if (handleMesh == null)
            return;

        // Stop the Rigidbody first.
        if (handleRigidbody != null)
        {
            handleRigidbody.velocity = Vector3.zero;
            handleRigidbody.angularVelocity = Vector3.zero;
        }

        // Teleport the pickup exactly to the visible handle.
        if (handleObjectSync != null)
        {
            handleObjectSync.FlagDiscontinuity();
            handleObjectSync.TeleportTo(handleMesh);
        }
        else
        {
            handle.position = handleMesh.position;
            handle.rotation = handleMesh.rotation;
        }

        // Make absolutely sure there is no leftover physics movement.
        if (handleRigidbody != null)
        {
            handleRigidbody.velocity = Vector3.zero;
            handleRigidbody.angularVelocity = Vector3.zero;
        }
    }

    public override void OnDeserialization()
    {
        ApplyDoorRotation(syncedAngle);
    }
}