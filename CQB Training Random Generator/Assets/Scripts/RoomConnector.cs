
using UdonSharp;
using UnityEngine;

public class RoomConnector : UdonSharpBehaviour
{
    [Header("References")]
    public BSPGenerator bspGenerator;

    [Header("Limits")]
    public int maxRooms = 32;
    public int maxConnections = 64;

    [Header("Door Settings")]
    public int doorLength = 1;

    [Header("Debug")]
    public bool logConnections = true;

    // --------------------------------------------------
    // Final room connection data
    // --------------------------------------------------

    private int[] connectionRoomA;
    private int[] connectionRoomB;

    // 0 = horizontal wall
    // 1 = vertical wall
    private int[] connectionDirection;

    // 0 = 0 degrees
    // 1 = 90 degrees
    // 2 = 180 degrees
    // 3 = 270 degrees
    private int[] connectionRotation;

    // Grid coordinate of the FIRST 1m segment
    // occupied by the door.
    private int[] connectionX;
    private int[] connectionZ;

    private int connectionCount;

    // --------------------------------------------------
    // Temporary room data
    // --------------------------------------------------

    private bool[] roomConnected;

    // Deterministic random state.
    private uint randomState;


    // --------------------------------------------------
    // Public getters
    // --------------------------------------------------

    public int GetConnectionCount()
    {
        return connectionCount;
    }

    public int GetConnectionRoomA(int index)
    {
        return connectionRoomA[index];
    }

    public int GetConnectionRoomB(int index)
    {
        return connectionRoomB[index];
    }

    public int GetConnectionDirection(int index)
    {
        return connectionDirection[index];
    }

    public int GetConnectionX(int index)
    {
        return connectionX[index];
    }

    public int GetConnectionZ(int index)
    {
        return connectionZ[index];
    }

    public int GetConnectionRotation(int index)
    {
        return connectionRotation[index];
    }


    // --------------------------------------------------
    // Main generation function
    // --------------------------------------------------

    public void GenerateConnections(int seed)
    {
        if (bspGenerator == null)
        {
            Debug.LogError(
                "RoomConnector: BSPGenerator is not assigned."
            );

            return;
        }

        connectionCount = 0;

        connectionRoomA = new int[maxConnections];
        connectionRoomB = new int[maxConnections];
        connectionDirection = new int[maxConnections];

        connectionRotation = new int[maxConnections];

        connectionX = new int[maxConnections];
        connectionZ = new int[maxConnections];

        roomConnected = new bool[maxRooms];

        // Start deterministic random generator.
        randomState = (uint)seed;

        if (randomState == 0)
        {
            randomState = 1;
        }

        int roomCount = bspGenerator.GetRoomCount();

        if (roomCount <= 1)
        {
            Debug.Log(
                "RoomConnector: " +
                roomCount +
                " room(s), no connections required."
            );

            return;
        }


        // --------------------------------------------------
        // STEP 1
        //
        // Find every pair of rooms that share a wall.
        // --------------------------------------------------

        int possibleMax = maxConnections;

        int[] possibleA = new int[possibleMax];
        int[] possibleB = new int[possibleMax];
        int[] possibleDirection = new int[possibleMax];

        int[] possibleX = new int[possibleMax];
        int[] possibleZ = new int[possibleMax];

        int possibleCount = 0;


        for (int roomA = 0; roomA < roomCount; roomA++)
        {
            for (
                int roomB = roomA + 1;
                roomB < roomCount;
                roomB++
            )
            {
                if (possibleCount >= possibleMax)
                {
                    break;
                }

                int ax = bspGenerator.GetRoomX(roomA);
                int az = bspGenerator.GetRoomZ(roomA);

                int aw = bspGenerator.GetRoomWidth(roomA);
                int al = bspGenerator.GetRoomLength(roomA);

                int bx = bspGenerator.GetRoomX(roomB);
                int bz = bspGenerator.GetRoomZ(roomB);

                int bw = bspGenerator.GetRoomWidth(roomB);
                int bl = bspGenerator.GetRoomLength(roomB);


                // --------------------------------------------------
                // Room B is to the RIGHT of Room A.
                // Shared wall is vertical.
                // --------------------------------------------------

                if (ax + aw == bx)
                {
                    int overlapStart = Max(az, bz);
                    int overlapEnd = Min(
                        az + al,
                        bz + bl
                    );

                    int overlapLength =
                        overlapEnd - overlapStart;

                    if (overlapLength >= doorLength)
                    {
                        int doorZ = GetDoorPosition(
                            overlapStart,
                            overlapEnd
                        );

                        possibleA[possibleCount] = roomA;
                        possibleB[possibleCount] = roomB;

                        possibleDirection[possibleCount] = 1;

                        possibleX[possibleCount] = bx;
                        possibleZ[possibleCount] = doorZ;

                        possibleCount++;
                    }

                    continue;
                }


                // --------------------------------------------------
                // Room B is to the LEFT of Room A.
                // Shared wall is vertical.
                // --------------------------------------------------

                if (bx + bw == ax)
                {
                    int overlapStart = Max(az, bz);
                    int overlapEnd = Min(
                        az + al,
                        bz + bl
                    );

                    int overlapLength =
                        overlapEnd - overlapStart;

                    if (overlapLength >= doorLength)
                    {
                        int doorZ = GetDoorPosition(
                            overlapStart,
                            overlapEnd
                        );

                        possibleA[possibleCount] = roomA;
                        possibleB[possibleCount] = roomB;

                        possibleDirection[possibleCount] = 1;

                        possibleX[possibleCount] = ax;
                        possibleZ[possibleCount] = doorZ;

                        possibleCount++;
                    }

                    continue;
                }


                // --------------------------------------------------
                // Room B is ABOVE Room A.
                // Shared wall is horizontal.
                // --------------------------------------------------

                if (az + al == bz)
                {
                    int overlapStart = Max(ax, bx);
                    int overlapEnd = Min(
                        ax + aw,
                        bx + bw
                    );

                    int overlapLength =
                        overlapEnd - overlapStart;

                    if (overlapLength >= doorLength)
                    {
                        int doorX = GetDoorPosition(
                            overlapStart,
                            overlapEnd
                        );

                        possibleA[possibleCount] = roomA;
                        possibleB[possibleCount] = roomB;

                        possibleDirection[possibleCount] = 0;

                        possibleX[possibleCount] = doorX;
                        possibleZ[possibleCount] = bz;

                        possibleCount++;
                    }

                    continue;
                }


                // --------------------------------------------------
                // Room B is BELOW Room A.
                // Shared wall is horizontal.
                // --------------------------------------------------

                if (bz + bl == az)
                {
                    int overlapStart = Max(ax, bx);
                    int overlapEnd = Min(
                        ax + aw,
                        bx + bw
                    );

                    int overlapLength =
                        overlapEnd - overlapStart;

                    if (overlapLength >= doorLength)
                    {
                        int doorX = GetDoorPosition(
                            overlapStart,
                            overlapEnd
                        );

                        possibleA[possibleCount] = roomA;
                        possibleB[possibleCount] = roomB;

                        possibleDirection[possibleCount] = 0;

                        possibleX[possibleCount] = doorX;
                        possibleZ[possibleCount] = az;

                        possibleCount++;
                    }
                }
            }
        }


        // --------------------------------------------------
        // STEP 2
        //
        // Select connections that guarantee every room
        // is reachable.
        // --------------------------------------------------

        BuildConnectedNetwork(
            roomCount,
            possibleCount,
            possibleA,
            possibleB,
            possibleDirection,
            possibleX,
            possibleZ
        );


        // --------------------------------------------------
        // DEBUG
        // --------------------------------------------------

        if (logConnections)
        {
            Debug.Log(
                "RoomConnector: Created " +
                connectionCount +
                " room connections."
            );

            for (int i = 0; i < connectionCount; i++)
            {
                Debug.Log(
                    "Connection " +
                    i +
                    ": Room " +
                    connectionRoomA[i] +
                    " <-> Room " +
                    connectionRoomB[i] +
                    " Door X=" +
                    connectionX[i] +
                    " Z=" +
                    connectionZ[i] +
                    " Direction=" +
                    connectionDirection[i] +
                    " Rotation=" +
                    GetConnectionRotation(i) +
                    "°"
                );
            }
        }
    }


    // --------------------------------------------------
    // Choose a random valid door position.
    //
    // overlapStart is inclusive.
    // overlapEnd is exclusive.
    // --------------------------------------------------

    private int GetDoorPosition(
        int overlapStart,
        int overlapEnd)
    {
        int validPositions =
            (overlapEnd - overlapStart) -
            doorLength +
            1;

        if (validPositions <= 1)
        {
            return overlapStart;
        }

        int offset = RandomRange(
            0,
            validPositions
        );

        return overlapStart + offset;
    }


    // --------------------------------------------------
    // Build connected network.
    //
    // Uses a simple spanning-tree approach.
    // --------------------------------------------------

    private void BuildConnectedNetwork(
        int roomCount,
        int possibleCount,
        int[] possibleA,
        int[] possibleB,
        int[] possibleDirection,
        int[] possibleX,
        int[] possibleZ)
    {
        for (int i = 0; i < roomCount; i++)
        {
            roomConnected[i] = false;
        }

        roomConnected[0] = true;

        int connectedRooms = 1;


        while (
            connectedRooms < roomCount &&
            connectionCount < maxConnections
        )
        {
            bool foundConnection = false;


            for (int i = 0; i < possibleCount; i++)
            {
                int roomA = possibleA[i];
                int roomB = possibleB[i];

                bool aConnected =
                    roomConnected[roomA];

                bool bConnected =
                    roomConnected[roomB];


                // We need exactly one room to already
                // belong to our connected network.
                if (aConnected == bConnected)
                {
                    continue;
                }


                connectionRoomA[connectionCount] =
                    roomA;

                connectionRoomB[connectionCount] =
                    roomB;

                connectionDirection[connectionCount] =
                    possibleDirection[i];

                connectionX[connectionCount] =
                    possibleX[i];

                connectionZ[connectionCount] =
                    possibleZ[i];

                // Seed-dependent door orientation.
                if (possibleDirection[i] == 0)
                {
                    // Width-wise wall: 0° or 180°
                    connectionRotation[connectionCount] =
                        RandomRange(0, 2) * 180;
                }
                else
                {
                    // Length-wise wall: 90° or 270°
                    connectionRotation[connectionCount] =
                        90 + RandomRange(0, 2) * 180;
                }


                connectionCount++;


                if (!aConnected)
                {
                    roomConnected[roomA] = true;
                }

                if (!bConnected)
                {
                    roomConnected[roomB] = true;
                }


                connectedRooms++;

                foundConnection = true;

                break;
            }


            if (!foundConnection)
            {
                break;
            }
        }


        if (connectedRooms != roomCount)
        {
            Debug.LogWarning(
                "RoomConnector: Could not connect every room. " +
                "Connected " +
                connectedRooms +
                " of " +
                roomCount +
                " rooms."
            );
        }
        else
        {
            Debug.Log(
                "RoomConnector: All " +
                roomCount +
                " rooms are connected."
            );
        }
    }


    // --------------------------------------------------
    // Deterministic random number generator.
    // --------------------------------------------------

    private int RandomRange(
        int minInclusive,
        int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }


        randomState ^= randomState << 13;
        randomState ^= randomState >> 17;
        randomState ^= randomState << 5;


        int stateAsInt =
            (int)(randomState & 0x7FFFFFFF);

        int range =
            maxExclusive - minInclusive;


        return minInclusive +
            (stateAsInt % range);
    }


    // --------------------------------------------------
    // Integer helpers.
    // --------------------------------------------------

    private int Max(int a, int b)
    {
        if (a > b)
        {
            return a;
        }

        return b;
    }


    private int Min(int a, int b)
    {
        if (a < b)
        {
            return a;
        }

        return b;
    }
}

