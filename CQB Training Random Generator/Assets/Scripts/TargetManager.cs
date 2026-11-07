// ============================================================================
// Manages the fixed pool of targets.
// Targets are moved and activated/deactivated; they are not instantiated.
//
// NETWORKING MODEL
// Whoever presses a spawn/clear button becomes the owner of this object,
// writes the spawn parameters (count, width, length, random seed) into synced
// variables and calls RequestSerialization().
// Every client (including late joiners) then runs ApplyState(), which builds
// the exact same target layout from the synced seed and updates the UI text.
// ============================================================================

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TargetManager : UdonSharpBehaviour
{
    // =========================================================================
    // REFERENCES
    // =========================================================================

    [Header("References")]

    // Assign all Target objects here.
    public TargetHit[] targets;

    // Where inactive targets are moved when cleared.
    public Transform clearPosition;

    public Transform generatedHouse;

    public GeneratorUI generatorUI;

    // =========================================================================
    // SYNCED STATE
    // =========================================================================

    [UdonSynced] public int spawnedTargets = 0;
    [UdonSynced] private int syncWidth = 0;
    [UdonSynced] private int syncLength = 0;
    [UdonSynced] private int syncSeed = 0;

    // =========================================================================
    // CONSTANTS
    // =========================================================================

    private const float GRID_SIZE = 1.0f;

    // The target center can move +-0.2m from the center of its grid cell.
    private const float SPAWN_HALF_SIZE = 0.2f;

    // Target is 1.7m tall. Use if target is floating.
    private const float TARGET_CENTER_Y = 0.0f;


    // =========================================================================
    // GENERATE TARGETS (called by the local player who clicked a button)
    // =========================================================================

    public void GenerateTargets(
        int width,
        int length,
        int requestedCount
    )
    {
        if (generatedHouse == null)
        {
            Debug.LogError("TargetManager: Generated House is not assigned.");
            return;
        }

        if (targets == null || targets.Length == 0)
        {
            Debug.LogError("TargetManager: No targets are assigned.");
            return;
        }

        int totalCells = width * length;

        if (totalCells <= 0)
        {
            Debug.LogWarning(
                "TargetManager: Width and Length must be greater than zero."
            );
            return;
        }

        int targetCount = requestedCount;

        if (targetCount > targets.Length) targetCount = targets.Length;
        if (targetCount > totalCells) targetCount = totalCells;
        if (targetCount < 0) targetCount = 0;

        // Become the owner so our synced values are actually sent.
        TakeOwnership();

        syncWidth = width;
        syncLength = length;
        spawnedTargets = targetCount;
        syncSeed = Random.Range(1, 2147483647);

        RequestSerialization();

        // The owner does not receive OnDeserialization, so apply locally.
        ApplyState();
    }


    // =========================================================================
    // CLEAR (networked)
    // =========================================================================

    public void ClearTargets()
    {
        TakeOwnership();

        spawnedTargets = 0;
        syncSeed = Random.Range(1, 2147483647);

        RequestSerialization();

        ApplyState();
    }


    // =========================================================================
    // NETWORK RECEIVE (everyone except the owner, and late joiners)
    // =========================================================================

    public override void OnDeserialization()
    {
        ApplyState();
    }


    // =========================================================================
    // APPLY SYNCED STATE LOCALLY
    // =========================================================================

    private void ApplyState()
    {
        ClearAllLocal();

        int count = spawnedTargets;
        int width = syncWidth;
        int length = syncLength;
        int totalCells = width * length;

        if (count > 0 &&
            totalCells > 0 &&
            generatedHouse != null &&
            targets != null)
        {
            if (count > targets.Length) count = targets.Length;
            if (count > totalCells) count = totalCells;

            // Same seed on every client = identical layout everywhere.
            Random.InitState(syncSeed);

            int[] cells = new int[totalCells];

            for (int i = 0; i < totalCells; i++)
            {
                cells[i] = i;
            }

            // Fisher-Yates shuffle
            for (int i = totalCells - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);

                int temp = cells[i];
                cells[i] = cells[randomIndex];
                cells[randomIndex] = temp;
            }

            for (int i = 0; i < count; i++)
            {
                int cellIndex = cells[i];
                int cellX = cellIndex % width;
                int cellZ = cellIndex / width;

                Vector3 localPosition = GetRandomPositionInCell(cellX, cellZ);
                Vector3 worldPosition =
                    generatedHouse.TransformPoint(localPosition);

                targets[i].ActivateAt(worldPosition);
            }

            // Un-sync the global random state so later local Random calls
            // (e.g. the "random seed" button) differ per client again.
            int playerId = 0;
            if (Networking.LocalPlayer != null)
            {
                playerId = Networking.LocalPlayer.playerId;
            }
            Random.InitState(
                (int)(Time.realtimeSinceStartup * 1000f) + playerId * 7919
            );
        }

        if (generatorUI != null)
        {
            generatorUI.RefreshTargetSpawnedDisplay();
        }
    }


    // =========================================================================
    // LOCAL-ONLY CLEAR (no networking)
    // =========================================================================

    private void ClearAllLocal()
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            if (clearPosition != null)
            {
                targets[i].DeactivateAt(clearPosition.position);
            }
            else
            {
                targets[i].Deactivate();
            }
        }
    }


    // =========================================================================
    // HELPERS
    // =========================================================================

    private void TakeOwnership()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }

    private Vector3 GetRandomPositionInCell(int cellX, int cellZ)
    {
        // Grid cell (0,0) spans X 0..1, Z 0..1, so its center is (0.5, 0.5).
        float centerX = (cellX * GRID_SIZE) + (GRID_SIZE * 0.5f);
        float centerZ = (cellZ * GRID_SIZE) + (GRID_SIZE * 0.5f);

        float randomX = Random.Range(-SPAWN_HALF_SIZE, SPAWN_HALF_SIZE);
        float randomZ = Random.Range(-SPAWN_HALF_SIZE, SPAWN_HALF_SIZE);

        return new Vector3(
            centerX + randomX,
            TARGET_CENTER_Y,
            centerZ + randomZ
        );
    }
}
