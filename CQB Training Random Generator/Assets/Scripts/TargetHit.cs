// ============================================================================
// Place this in the Target to add target logic. Must use with targetBody script.
// ============================================================================

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TargetHit : UdonSharpBehaviour
{
    [SerializeField] private GameObject targetBody;
    public AudioSource audioSource;
    public AudioClip targetHitClip;

    [UdonSynced] private bool targetActive = false;
    [UdonSynced] private Vector3 targetPosition;

    private void Start()
    {
        // Remember the target's original scene position.
        targetPosition = transform.position;

        ApplyState();
    }
    public void ActivateAt(Vector3 position)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        targetPosition = position;
        targetActive = true;

        ApplyState();
        RequestSerialization();
    }
    public void Deactivate()
    {
        //Debug.Log("Deactivated via UI");
        SetTargetActive(false);
    }
    public void DeactivateAt(Vector3 position)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        targetPosition = position;
        targetActive = false;

        ApplyState();
        RequestSerialization();
    }
    public void SetTargetActive(bool active)
    {
        //Debug.Log("SetTargetActive: " + active);
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        targetActive = active;


        if (audioSource != null && targetHitClip != null)
        {
            audioSource.PlayOneShot(targetHitClip);
        }

        ApplyState();
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        ApplyState();
    }

    private void ApplyState()
    {
        transform.position = targetPosition;
        targetBody.SetActive(targetActive);
    }
}