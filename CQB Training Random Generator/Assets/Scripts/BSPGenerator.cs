using UdonSharp;
using UnityEngine;

public class BSPGenerator : UdonSharpBehaviour
{
    // ============================================================
    // SETTINGS
    // ============================================================

    [Header("Room Limits")]
    public int maxRooms = 32;

    // Minimum logical room size in grid cells.
    public int minRoomWidth = 2;
    public int minRoomLength = 2;

    [Header("BSP Settings")]
    public int maxIterations = 128;

    // Prefer splitting rooms that are larger than this.
    public int preferredMaxRoomDimension = 8;

    // Preferred split range as a percentage.
    public int splitMinPercent = 35;
    public int splitMaxPercent = 65;


    // ============================================================
    // GENERATED ROOM DATA
    // ============================================================

    // These arrays contain the final rooms.
    //
    // Room i is:
    // X      = roomX[i]
    // Z      = roomZ[i]
    // Width  = roomWidth[i]
    // Length = roomLength[i]

    private int[] roomX;
    private int[] roomZ;
    private int[] roomWidth;
    private int[] roomLength;

    private int roomCount;


    // ============================================================
    // BSP REGION DATA
    // ============================================================

    // Each region occupies one slot in these arrays.
    //
    // Regions are processed until no more useful splits can be made.

    private int[] regionX;
    private int[] regionZ;
    private int[] regionWidth;
    private int[] regionLength;

    private bool[] regionActive;

    private int regionCount;


    // ============================================================
    // SEEDED RANDOM
    // ============================================================

    private uint randomState;


    // ============================================================
    // PUBLIC ACCESS
    // ============================================================

    public int GetRoomCount()
    {
        return roomCount;
    }

    public int GetRoomX(int index)
    {
        return roomX[index];
    }

    public int GetRoomZ(int index)
    {
        return roomZ[index];
    }

    public int GetRoomWidth(int index)
    {
        return roomWidth[index];
    }

    public int GetRoomLength(int index)
    {
        return roomLength[index];
    }


    // ============================================================
    // GENERATE
    // ============================================================

    public void Generate(int houseWidth, int houseLength, int seed)
    {
        // --------------------------------------------------------
        // Reset
        // --------------------------------------------------------

        roomCount = 0;
        regionCount = 0;

        // --------------------------------------------------------
        // Allocate arrays.
        //
        // We use primitive arrays instead of creating custom
        // classes with "new", because this is much safer for
        // UdonSharp.
        // --------------------------------------------------------

        roomX = new int[maxRooms];
        roomZ = new int[maxRooms];
        roomWidth = new int[maxRooms];
        roomLength = new int[maxRooms];

        regionX = new int[maxRooms * 2];
        regionZ = new int[maxRooms * 2];
        regionWidth = new int[maxRooms * 2];
        regionLength = new int[maxRooms * 2];
        regionActive = new bool[maxRooms * 2];

        // --------------------------------------------------------
        // Initialize deterministic random state.
        // --------------------------------------------------------

        randomState = (uint)seed;

        if (randomState == 0)
        {
            randomState = 1;
        }

        // --------------------------------------------------------
        // Create root BSP region.
        // --------------------------------------------------------

        AddRegion(
            0,
            0,
            houseWidth,
            houseLength
        );

        // --------------------------------------------------------
        // Repeatedly attempt to split regions.
        // --------------------------------------------------------

        int iterations = 0;

        while (iterations < maxIterations)
        {
            iterations++;

            bool didSplit = false;

            // Scan existing regions.
            for (int i = 0; i < regionCount; i++)
            {
                if (!regionActive[i])
                {
                    continue;
                }

                // Don't exceed our room safety limit.
                if (regionCount >= maxRooms)
                {
                    break;
                }

                if (!ShouldSplit(
                    regionWidth[i],
                    regionLength[i]))
                {
                    continue;
                }

                if (TrySplitRegion(i))
                {
                    didSplit = true;
                    break;
                }
            }

            // No more useful splits.
            if (!didSplit)
            {
                break;
            }
        }

        // --------------------------------------------------------
        // Convert active BSP regions into final rooms.
        // --------------------------------------------------------

        for (int i = 0; i < regionCount; i++)
        {
            if (!regionActive[i])
            {
                continue;
            }

            if (roomCount >= maxRooms)
            {
                break;
            }

            roomX[roomCount] = regionX[i];
            roomZ[roomCount] = regionZ[i];
            roomWidth[roomCount] = regionWidth[i];
            roomLength[roomCount] = regionLength[i];

            roomCount++;
        }
    }


    // ============================================================
    // REGION CREATION
    // ============================================================

    private void AddRegion(
        int x,
        int z,
        int width,
        int length)
    {
        if (regionCount >= regionX.Length)
        {
            return;
        }

        regionX[regionCount] = x;
        regionZ[regionCount] = z;
        regionWidth[regionCount] = width;
        regionLength[regionCount] = length;
        regionActive[regionCount] = true;

        regionCount++;
    }


    // ============================================================
    // SHOULD SPLIT?
    // ============================================================

    private bool ShouldSplit(int width, int length)
    {
        // Can't split a 1-cell-wide dimension.
        bool canSplitWidth = width >= minRoomWidth * 2;
        bool canSplitLength = length >= minRoomLength * 2;

        if (!canSplitWidth && !canSplitLength)
        {
            return false;
        }

        // If either dimension is very large, encourage splitting.
        if (width > preferredMaxRoomDimension ||
            length > preferredMaxRoomDimension)
        {
            return true;
        }

        // Otherwise, probabilistically split.
        return RandomRange(0, 100) < 65;
    }


    // ============================================================
    // TRY SPLIT
    // ============================================================

    private bool TrySplitRegion(int index)
    {
        int width = regionWidth[index];
        int length = regionLength[index];

        bool canSplitWidth =
            width >= minRoomWidth * 2;

        bool canSplitLength =
            length >= minRoomLength * 2;

        if (!canSplitWidth && !canSplitLength)
        {
            return false;
        }

        // --------------------------------------------------------
        // Decide split direction.
        //
        // Larger dimension gets a higher chance of being split.
        // --------------------------------------------------------

        bool splitWidth;

        if (canSplitWidth && !canSplitLength)
        {
            splitWidth = true;
        }
        else if (!canSplitWidth && canSplitLength)
        {
            splitWidth = false;
        }
        else
        {
            if (width > length)
            {
                splitWidth = RandomRange(0, 100) < 70;
            }
            else if (length > width)
            {
                splitWidth = RandomRange(0, 100) < 30;
            }
            else
            {
                splitWidth = RandomRange(0, 100) < 50;
            }
        }

        // --------------------------------------------------------
        // Split vertically along X.
        // --------------------------------------------------------

        if (splitWidth)
        {
            int minSplit = minRoomWidth;
            int maxSplit = width - minRoomWidth;

            if (maxSplit < minSplit)
            {
                return false;
            }

            int splitPosition = GetSplitPosition(
                minSplit,
                maxSplit
            );

            int firstWidth = splitPosition;
            int secondWidth = width - splitPosition;

            // Mark old region inactive.
            regionActive[index] = false;

            // Create first child.
            AddRegion(
                regionX[index],
                regionZ[index],
                firstWidth,
                length
            );

            // Create second child.
            AddRegion(
                regionX[index] + splitPosition,
                regionZ[index],
                secondWidth,
                length
            );

            return true;
        }

        // --------------------------------------------------------
        // Split horizontally along Z.
        // --------------------------------------------------------

        int minSplitZ = minRoomLength;
        int maxSplitZ = length - minRoomLength;

        if (maxSplitZ < minSplitZ)
        {
            return false;
        }

        int splitPositionZ = GetSplitPosition(
            minSplitZ,
            maxSplitZ
        );

        int firstLength = splitPositionZ;
        int secondLength = length - splitPositionZ;

        // Mark old region inactive.
        regionActive[index] = false;

        // Create first child.
        AddRegion(
            regionX[index],
            regionZ[index],
            width,
            firstLength
        );

        // Create second child.
        AddRegion(
            regionX[index],
            regionZ[index] + splitPositionZ,
            width,
            secondLength
        );

        return true;
    }


    // ============================================================
    // SPLIT POSITION
    // ============================================================

    private int GetSplitPosition(
        int minSplit,
        int maxSplit)
    {
        if (maxSplit <= minSplit)
        {
            return minSplit;
        }

        // Calculate the preferred middle range.
        int total = maxSplit;

        int preferredMin =
            (total * splitMinPercent) / 100;

        int preferredMax =
            (total * splitMaxPercent) / 100;

        // Clamp to valid values.
        if (preferredMin < minSplit)
        {
            preferredMin = minSplit;
        }

        if (preferredMax > maxSplit)
        {
            preferredMax = maxSplit;
        }

        if (preferredMax < preferredMin)
        {
            preferredMin = minSplit;
            preferredMax = maxSplit;
        }

        return RandomRange(
            preferredMin,
            preferredMax + 1
        );
    }


    // ============================================================
    // DETERMINISTIC RANDOM
    // ============================================================

    private int RandomRange(
        int minInclusive,
        int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        // Xorshift32.
        randomState ^= randomState << 13;
        randomState ^= randomState >> 17;
        randomState ^= randomState << 5;

        // Convert to int before using modulo.
        int stateAsInt = (int)(randomState & 0x7FFFFFFF);

        int range = maxExclusive - minInclusive;

        return minInclusive + (stateAsInt % range);
    }
}