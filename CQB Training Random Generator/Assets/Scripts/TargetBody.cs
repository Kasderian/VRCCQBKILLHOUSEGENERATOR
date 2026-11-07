// ============================================================================
// Place this in the Target's body to detect particle hits.
// ============================================================================

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TargetBody : UdonSharpBehaviour
{
    [SerializeField] private TargetHit parentTarget;

    void OnParticleCollision(GameObject other)
    {
        parentTarget.SetTargetActive(false);
    }
}