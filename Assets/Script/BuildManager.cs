using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    TestGrid testGrid;
    Building spawnRoom;
    public List<Building> rooms = new List<Building>();
    public int _tileSize = 2;

    public int mapSeed = 0;
    public bool randomizeIfZero = true;

    int currentRoomID = 1;
    bool SpawnRoomCall = false;

    public BuildManager(TestGrid grid, int seed)
    {
        testGrid = grid;
        mapSeed = seed;

        if (randomizeIfZero && mapSeed == 0)
            mapSeed = System.Environment.TickCount;

        UnityEngine.Random.InitState(mapSeed);
        Debug.Log($"Map seed: {mapSeed}");

        rooms.Add(new Building(4, 4, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType4x4 1")));
        rooms.Add(new Building(3, 3, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType3x3")));
        rooms.Add(new Building(3, 2, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType3x2"), true));
        spawnRoom = new Building(2, 2, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType2x2"));

        SpawnRooms(10);
    }

    public void SetSeed(int s)
    {
        mapSeed = s;
        UnityEngine.Random.InitState(mapSeed);
        Debug.Log($"Set map seed: {mapSeed}");
    }

    public void SetSeedAndSpawn(int s, int numberOfRooms)
    {
        SetSeed(s);
        ResetGrid();
        currentRoomID = 1;
        SpawnRooms(numberOfRooms);
    }

    void ResetGrid()
    {
        if (testGrid == null || testGrid.myGrid == null) return;
        for (int x = 0; x < testGrid.width; x++)
            for (int y = 0; y < testGrid.height; y++)
                testGrid.myGrid[x, y] = TestGrid.zero;
    }

    void SpawnRooms(int numberOfRooms)
    {
        Vector2Int roomPos = new Vector2Int(testGrid.width / 2, testGrid.height / 2);

        PlaceBuilding(roomPos.x, roomPos.y, spawnRoom);
        GameObject _spawnRoom = Instantiate(spawnRoom.prefab,
            new Vector3(roomPos.x * _tileSize, 0, -roomPos.y * _tileSize), Quaternion.identity);
        GameObject _currentObject = _spawnRoom;
        _spawnRoom.name = "SpawnRoom " + currentRoomID;

        var _previousRoom = _currentObject.GetComponent<OpenDoor>();
        _previousRoom.roomId = currentRoomID;

        Building currentBuilding = spawnRoom;
        List<B_D> availableDirections = SetDirection(direction.Up, spawnRoom);

        currentRoomID++;

        int attempts = 0;
        for (int i = 0; i < numberOfRooms;)
        {
            if (availableDirections.Count == 0)
            {
                Debug.Log("Plus de directions disponibles.");
                break;
            }

            int _randDirection = UnityEngine.Random.Range(0, availableDirections.Count);
            B_D _chosenDir = availableDirections[_randDirection];
            Building _nextBuilding = PickNextBuilding(currentBuilding, i == numberOfRooms - 1);
            if (_nextBuilding == null)
            {
                availableDirections.RemoveAt(_randDirection);
                attempts++;
                if (attempts > 200)
                {
                    Debug.LogWarning("Trop de tentatives pour placer des salles, arrêt de la génération.");
                    break;
                }
                continue;
            }

            Vector2Int _newPos = GetNewBuildingPos(_nextBuilding, _chosenDir._direction, roomPos.x, roomPos.y,
                currentBuilding);

            if (PlaceBuilding(_newPos.x, _newPos.y, _nextBuilding))
            {
                roomPos = new Vector2Int(_newPos.x, _newPos.y);

                List<B_D> _newDirs = SetDirection(_chosenDir._direction, _nextBuilding);
                availableDirections.AddRange(_newDirs);

                _previousRoom = _currentObject.GetComponent<OpenDoor>();
                _previousRoom.building = currentBuilding;
                _previousRoom._directions = _chosenDir._direction;
                _previousRoom.testGrid = testGrid;

                GameObject _object = Instantiate(_nextBuilding.prefab,
                    new Vector3Int(roomPos.x * _tileSize, 0, -roomPos.y * _tileSize), Quaternion.identity);
                _object.name = "Room " + currentRoomID;
                var _newOpen = _object.GetComponent<OpenDoor>();
                _newOpen.building = _nextBuilding;
                _newOpen._directions = GetOppositeEnum(_chosenDir._direction);
                _newOpen.roomId = currentRoomID;
                _newOpen.testGrid = testGrid;

                currentBuilding = _nextBuilding;
                _currentObject = _object;
                currentRoomID++;
                i++;
            }
            else
            {
                availableDirections.RemoveAt(_randDirection);
                attempts++;
                if (attempts > 200)
                {
                    Debug.LogWarning("Trop de tentatives pour placer des salles, arrêt de la génération.");
                    break;
                }
            }
        }
        OpenAllDoorsAfterPlacement();
    }

    private void OpenAllDoorsAfterPlacement()
    {
        var allOpenDoors = FindObjectsOfType<OpenDoor>();
        foreach (var od in allOpenDoors)
        {
            if (od == null || od.building == null) continue;

            od.testGrid = testGrid;

            int originX = Mathf.RoundToInt(od.transform.position.x / _tileSize);
            int originY = Mathf.RoundToInt(-od.transform.position.z / _tileSize);

            List<direction> dirs = GetAdjacentConnections(originX, originY, od.building);

            if (dirs.Count == 0)
            {
                od.OpenDoors();
            }
            else
            {
                foreach (var d in dirs)
                {
                    od._directions = d;
                    od.OpenDoors();
                }
            }
        }
    }

    List<direction> GetAdjacentConnections(int posX, int posY, Building building)
    {
        List<direction> dirs = new List<direction>();

        int yUp = posY - 1;
        if (yUp >= 0)
        {
            for (int x = posX; x < posX + building.width; x++)
            {
                if (x >= 0 && x < testGrid.width && testGrid.myGrid[x, yUp] != TestGrid.zero)
                {
                    dirs.Add(direction.Up);
                    break;
                }
            }
        }

        int yDown = posY + building.height;
        if (yDown < testGrid.height)
        {
            for (int x = posX; x < posX + building.width; x++)
            {
                if (x >= 0 && x < testGrid.width && testGrid.myGrid[x, yDown] != TestGrid.zero)
                {
                    dirs.Add(direction.Down);
                    break;
                }
            }
        }

        int xLeft = posX - 1;
        if (xLeft >= 0)
        {
            for (int y = posY; y < posY + building.height; y++)
            {
                if (y >= 0 && y < testGrid.height && testGrid.myGrid[xLeft, y] != TestGrid.zero)
                {
                    dirs.Add(direction.Left);
                    break;
                }
            }
        }

        int xRight = posX + building.width;
        if (xRight < testGrid.width)
        {
            for (int y = posY; y < posY + building.height; y++)
            {
                if (y >= 0 && y < testGrid.height && testGrid.myGrid[xRight, y] != TestGrid.zero)
                {
                    dirs.Add(direction.Right);
                    break;
                }
            }
        }

        return dirs;
    }

    private direction GetOppositeEnum(direction _Current)
    {
        switch (_Current)
        {
            case direction.Up:
                return direction.Down;
            case direction.Down:
                return direction.Up;
            case direction.Left:
                return direction.Right;
            case direction.Right:
                return direction.Left;
            default: return _Current;
        }
    }

    Vector2Int GetNewBuildingPos(Building nextBuilding, direction dir, int posX, int posY, Building currentBuilding)
    {
        switch (dir)
        {
            case direction.Up:
                return new Vector2Int(posX, posY - nextBuilding.height);
            case direction.Right:
                return new Vector2Int(posX + currentBuilding.width, posY);
            case direction.Left:
                return new Vector2Int(posX - nextBuilding.width, posY);
            case direction.Down:
                return new Vector2Int(posX, posY + currentBuilding.height);
            default:
                return Vector2Int.zero;
        }
    }

    List<B_D> SetDirection(direction dir, Building building)
    {
        List<B_D> dirs = new List<B_D>();
        switch (dir)
        {
            case direction.Up:
                dirs.Add(new B_D(direction.Up, building));
                dirs.Add(new B_D(direction.Right, building));
                dirs.Add(new B_D(direction.Left, building));
                break;
            case direction.Right:
                dirs.Add(new B_D(direction.Up, building));
                dirs.Add(new B_D(direction.Right, building));
                dirs.Add(new B_D(direction.Down, building));
                break;
            case direction.Left:
                dirs.Add(new B_D(direction.Up, building));
                dirs.Add(new B_D(direction.Left, building));
                dirs.Add(new B_D(direction.Down, building));
                break;
            case direction.Down:
                dirs.Add(new B_D(direction.Down, building));
                dirs.Add(new B_D(direction.Right, building));
                dirs.Add(new B_D(direction.Left, building));
                break;
        }

        return dirs;
    }

    bool PlaceBuilding(int posX, int posY, Building building)
    {
        if (!CheckBoundsGrid(posX, posY, building) || !CheckCanPlaceBuilding(posX, posY, building))
            return false;
        if (building.isL && building.myBuildingGrid != null)
        {
            for (int y = 0; y < building.height; y++)
            {
                for (int x = 0; x < building.width; x++)
                {
                    if (building.myBuildingGrid[x, y] == TestGrid.one)
                    {
                        testGrid.myGrid[posX + x, posY + y] = currentRoomID;
                    }
                }
            }
        }
        else
        {
            for (int y = 0; y < building.height; y++)
            {
                for (int x = 0; x < building.width; x++)
                {
                    testGrid.myGrid[posX + x, posY + y] = currentRoomID;
                }
            }
        }

        Debug.Log($"Salle placée avec ID {currentRoomID} à ({posX},{posY})");
        return true;
    }

    public bool CheckBoundsGrid(int posX, int posY, Building building)
    {
        return posX >= 0 && posY >= 0 && posX + building.width < testGrid.width &&
               posY + building.height < testGrid.height;
    }

    bool CheckCanPlaceBuilding(int posX, int posY, Building building)
    {
        for (int i = 0; i < building.height; i++)
        for (int j = 0; j < building.width; j++)
            if (testGrid.myGrid[posX + j, posY + i] != TestGrid.zero)
                return false;

        return true;
    }

    private Building PickNextBuilding(Building currentBuilding, bool isLast)
    {
        int tries = 0;
        const int maxTries = 30;

        while (tries < maxTries)
        {
            var candidate = rooms[UnityEngine.Random.Range(0, rooms.Count)];
            if (currentBuilding != null && currentBuilding.isL && candidate.isL)
            {
                tries++;
                continue;
            }
            if (isLast && candidate.isL)
            {
                tries++;
                continue;
            }
            return candidate;
        }

        foreach (var b in rooms)
            if (!b.isL) return b;

        return rooms.Count > 0 ? rooms[0] : null;
    }
}

public enum direction
{
    Up,
    Right,
    Down,
    Left
}

[Serializable]
public struct B_D
{
    public direction _direction;
    public Building _Building;

    public B_D(direction dir, Building building)
    {
        _direction = dir;
        _Building = building;
    }
}
