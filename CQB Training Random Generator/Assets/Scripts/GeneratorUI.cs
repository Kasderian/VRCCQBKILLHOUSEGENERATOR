// ============================================================================
// Place this on a canvas UI parent object.
// ============================================================================

using UdonSharp;
using UnityEngine;
using TMPro;

public class GeneratorUI : UdonSharpBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    // Drag the HouseGenerator GameObject here.
    public HouseGenerator houseGenerator;
    public TargetManager targetManager;
    public CameraManager cameraManager;


    [Header("Input Fields")]

    // Width input field.
    public TMP_InputField widthInput;

    // Length input field.
    public TMP_InputField lengthInput;

    // Seed input field.
    public TMP_InputField seedInput;

    // Maximum number of targets to spawn.
    public TMP_InputField maxTargetsInput;


    [Header("Current Settings Display")]

    // Text displaying the currently generated settings.
    //
    // Example:
    // Width: 12
    // Length: 10
    // Seed: 12345
    public TMP_Text currentSettingsText;
    public TMP_Text targetSpawnedText;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        RefreshCurrentSettingsDisplay();
        RefreshTargetSpawnedDisplay();
    }


    // ============================================================
    // GENERATE BUTTON
    // ============================================================

    // Connect the Generate Button's On Click() event to this method.
    public void Generate()
    {
        if (houseGenerator == null)
        {
            Debug.LogError(
                "GeneratorUI: HouseGenerator is not assigned."
            );

            return;
        }

        // --------------------------------------------------------
        // Make sure all input fields exist.
        // --------------------------------------------------------

        if (widthInput == null ||
            lengthInput == null ||
            seedInput == null)
        {
            Debug.LogError(
                "GeneratorUI: One or more input fields are not assigned."
            );

            return;
        }

        // --------------------------------------------------------
        // Make sure none of the fields are empty.
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(widthInput.text) ||
            string.IsNullOrWhiteSpace(lengthInput.text) ||
            string.IsNullOrWhiteSpace(seedInput.text))
        {
            Debug.LogWarning(
                "GeneratorUI: All three fields must be filled in."
            );

            return;
        }

        // --------------------------------------------------------
        // Try to convert the input into integers.
        // --------------------------------------------------------

        int newWidth;
        int newLength;
        int newSeed;

        if (!int.TryParse(widthInput.text, out newWidth))
        {
            Debug.LogWarning(
                "GeneratorUI: Width must be a valid integer."
            );

            return;
        }

        if (!int.TryParse(lengthInput.text, out newLength))
        {
            Debug.LogWarning(
                "GeneratorUI: Length must be a valid integer."
            );

            return;
        }

        if (!int.TryParse(seedInput.text, out newSeed))
        {
            Debug.LogWarning(
                "GeneratorUI: Seed must be a valid integer."
            );

            return;
        }

        // --------------------------------------------------------
        // Send the settings to HouseGenerator.
        // --------------------------------------------------------

        houseGenerator.SetSettingsAndGenerate(
            newWidth,
            newLength,
            newSeed
        );

        // --------------------------------------------------------
        // Update the feedback display.
        // --------------------------------------------------------

        RefreshCurrentSettingsDisplay();
    }


    // ============================================================
    // RANDOM SEED BUTTON
    // ============================================================

    // Generates a positive random seed and puts it into the
    // Seed input field.
    //
    // This does NOT generate the house.
    // The player must press Generate afterward.
    public void RandomizeSeed()
    {
        if (seedInput == null)
        {
            Debug.LogError(
                "GeneratorUI: Seed Input Field is not assigned."
            );

            return;
        }

        int randomSeed = Random.Range(
            1,
            2147483647
        );

        seedInput.SetTextWithoutNotify(
            randomSeed.ToString()
        );
    }

    // ============================================================
    // RANDOM Size
    // ============================================================

    // Generates a positive random width and length into both width and length inputs.
    public void RandomizeSize()
    {
        if (widthInput == null)
        {
            Debug.LogError(
                "GeneratorUI: Width Input Field is not assigned."
            );

            return;
        }

        if (lengthInput == null)
        {
            Debug.LogError(
                "GeneratorUI: Length Input Field is not assigned."
            );

            return;
        }

        int randomWidth = Random.Range(
            1,
            65
        );
        int randomLength = Random.Range(
            1,
            65
        );

        widthInput.SetTextWithoutNotify(
            randomWidth.ToString()
        );
        lengthInput.SetTextWithoutNotify(
            randomLength.ToString()
        );
    }


    // ============================================================
    // CURRENT SETTINGS DISPLAY
    // ============================================================

    public void RefreshCurrentSettingsDisplay()
    {
        if (currentSettingsText == null)
        {
            return;
        }

        if (houseGenerator == null)
        {
            currentSettingsText.text =
                "Current Settings:\n" +
                "No generator assigned.";

            return;
        }

        currentSettingsText.text =
            "Current Settings:\n" +
            "Seed: " + houseGenerator.seed + "\n" +
            "Width: " + houseGenerator.width + "\n" +
            "Length: " + houseGenerator.length
            ;

        if (cameraManager != null)
        {
            cameraManager.SetCameraUp();
        }
    }

    // ============================================================
    // SPAWN INPUT TARGETS BUTTON
    // ============================================================
    public void SpawnInputTargets()
    {
        if (targetManager == null)
        {
            Debug.LogError(
                "GeneratorUI: TargetManager is not assigned."
            );

            return;
        }

        if (houseGenerator == null)
        {
            Debug.LogError(
                "GeneratorUI: HouseGenerator is not assigned."
            );

            return;
        }

        if (maxTargetsInput == null)
        {
            Debug.LogError(
                "GeneratorUI: Max Targets Input Field is not assigned."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(maxTargetsInput.text))
        {
            Debug.LogWarning(
                "GeneratorUI: Max Targets field is empty."
            );

            return;
        }

        int noOfTargets;

        if (!int.TryParse(
            maxTargetsInput.text,
            out noOfTargets))
        {
            Debug.LogWarning(
                "GeneratorUI: Max Targets must be a valid integer."
            );

            return;
        }

        if (noOfTargets < 0)
        {
            Debug.LogWarning(
                "GeneratorUI: Max Targets cannot be negative."
            );

            return;
        }

        // Use the dimensions of the HOUSE THAT IS CURRENTLY GENERATED.
        targetManager.GenerateTargets(
            houseGenerator.width,
            houseGenerator.length,
            noOfTargets
        );
    }
    // ============================================================
    // SPAWN RANDOM TARGETS BUTTON
    // ============================================================
    public void SpawnRandomTargets()
    {
        if (targetManager == null)
        {
            Debug.LogError(
                "GeneratorUI: TargetManager is not assigned."
            );

            return;
        }

        if (houseGenerator == null)
        {
            Debug.LogError(
                "GeneratorUI: HouseGenerator is not assigned."
            );

            return;
        }

        int maxTargets, noOfTargets = 0;

        if (targetManager.targets != null)
        {
            maxTargets = targetManager.targets.Length;
            //Debug.Log($"Total targets in array: {count}");
            // Upper bound of Random.Range(int,int) is exclusive, so +1.
            noOfTargets = Random.Range(1, maxTargets + 1);
            if (maxTargetsInput != null)
            {
                maxTargetsInput.SetTextWithoutNotify(noOfTargets.ToString());
            }
        }
        else
        {
            //Debug.LogWarning("[MenuUI] Targets array is null!");
            return;
        }


        if (maxTargets < 0 || noOfTargets < 0)
        {
            Debug.LogWarning(
                "GeneratorUI: Max Targets cannot be negative."
            );

            return;
        }

        // Use the dimensions of the HOUSE THAT IS CURRENTLY GENERATED.
        targetManager.GenerateTargets(
            houseGenerator.width,
            houseGenerator.length,
            noOfTargets
        );
    }

    // ============================================================
    // SPAWN MAX TARGETS BUTTON
    // ============================================================
    public void SpawnMaxTargets()
    {
        if (targetManager == null)
        {
            Debug.LogError(
                "GeneratorUI: TargetManager is not assigned."
            );

            return;
        }

        if (houseGenerator == null)
        {
            Debug.LogError(
                "GeneratorUI: HouseGenerator is not assigned."
            );

            return;
        }

        int maxTargets = 0;

        if (targetManager.targets != null)
        {
            maxTargets = targetManager.targets.Length;
            if (maxTargetsInput != null)
            {
                maxTargetsInput.SetTextWithoutNotify(maxTargets.ToString());
            }
        }
        else
        {
            //Debug.LogWarning("[MenuUI] Targets array is null!");
            return;
        }

        if (maxTargets < 0)
        {
            Debug.LogWarning(
                "GeneratorUI: Max Targets cannot be negative."
            );

            return;
        }

        // Use the dimensions of the HOUSE THAT IS CURRENTLY GENERATED.
        targetManager.GenerateTargets(
            houseGenerator.width,
            houseGenerator.length,
            maxTargets
        );
    }

    // ============================================================
    // CLEAR TARGETS BUTTON
    // ============================================================

    // Connect the Clear Targets Button's On Click()
    // event to this method.
    public void ClearTargets()
    {
        if (targetManager == null)
        {
            Debug.LogError(
                "GeneratorUI: TargetManager is not assigned."
            );

            return;
        }

        targetManager.ClearTargets();
    }

    // ============================================================
    // TARGET SPAWNED TEXT DISPLAY, called by TargetManager
    // ============================================================

    public void RefreshTargetSpawnedDisplay()
    {
        if (targetSpawnedText == null)
        {
            return;
        }

        if (targetManager == null)
        {
            targetSpawnedText.text =
                "Target Spawned: " +
                "No targets assigned.";

            return;
        }

        targetSpawnedText.text =
            "Target Spawned: " + targetManager.spawnedTargets;
    }
}
