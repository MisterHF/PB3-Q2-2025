using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    TestGrid TestGrid;
    Building SpawnRoom;
    public List<Building> rooms = new List<Building>();

    GameObject prefabBat1 = Resources.Load<GameObject>("Prefab/Rooms/LastTest/TestSpawnRoom/Bat");
    GameObject prefabBat2 = Resources.Load<GameObject>("Prefab/Rooms/LastTest/TestSpawnRoom/Bat (2)");
    GameObject prefabBat3 = Resources.Load<GameObject>("Prefab/Rooms/LastTest/TestSpawnRoom/Bat(3)");
    GameObject prefabSpawnRoom = Resources.Load<GameObject>("Prefab/Rooms/LastTest/TestSpawnRoom/SpawnRoom");

    int currentRoomID = 1;

    public BuildManager(TestGrid testGrid)
    {
        TestGrid = testGrid;

        rooms.Add(new Building(4, 4, prefabBat1));
        rooms.Add(new Building(2, 2, prefabBat2));
        rooms.Add(new Building(6, 6, prefabBat3));
        SpawnRoom = new Building(4, 4, prefabSpawnRoom);

        foreach (Building b in rooms)
            b.SetFullGrid();
        SpawnRoom.SetFullGrid();

        SpawnRooms(15);
    }

    void SpawnRooms(int numberOfRooms)
    {
        Vector2Int roomPos = new Vector2Int(TestGrid.width / 2, TestGrid.height / 2);
        Building currentBuilding = SpawnRoom;

        PlaceBuilding(roomPos.x, roomPos.y, SpawnRoom);

        List<B_D> availableDirections = SetDirection(direction.Up, SpawnRoom);

        int attempts = 0;
        for (int i = 0; i < numberOfRooms;)
        {
            if (availableDirections.Count == 0)
            {
                Debug.Log("Plus de directions disponibles.");
                break;
            }

            Building nextBuilding = rooms[Random.Range(0, rooms.Count)];
            int randIndex = Random.Range(0, availableDirections.Count);
            B_D chosenDir = availableDirections[randIndex];

            Vector2 newPos = GetNewBuildingPos(nextBuilding, chosenDir._direction, roomPos.x, roomPos.y, currentBuilding);

            if (PlaceBuilding((int)newPos.x, (int)newPos.y, nextBuilding))
            {
                roomPos = new Vector2Int((int)newPos.x, (int)newPos.y);
                currentBuilding = nextBuilding;
                List<B_D> newDirs = SetDirection(chosenDir._direction, nextBuilding);
                availableDirections.AddRange(newDirs);
                availableDirections.RemoveAt(randIndex);
                i++;
            }
            else
            {
                attempts++;
                if (attempts > 100)
                {
                    Debug.Log("Boucle infinie");
                    break;
                }
            }
        }
    }

    Vector2 GetNewBuildingPos(Building nextBuilding, direction dir, int posX, int posY, Building currentBuilding)
    {
        switch (dir)
        {
            case direction.Up:
                return new Vector2(posX, posY + currentBuilding.height);
            case direction.Right:
                return new Vector2(posX + currentBuilding.width, posY);
            case direction.Left:
                return new Vector2(posX - nextBuilding.width, posY);
            default: // Down
                return new Vector2(posX, posY - nextBuilding.height);
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
            default: // Down
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

        //Assign a unique ID to this room that has just been generated
        int roomID = currentRoomID;
        currentRoomID++;

        for (int i = 0; i < building.height; i++)
        {
            for (int j = 0; j < building.width; j++)
            {
                TestGrid.myGrid[posX + j, posY + i] = roomID;
            }
        }

        Debug.Log($"Salle placée avec ID {roomID} à ({posX},{posY})");
        return true;
    }

    bool CheckBoundsGrid(int posX, int posY, Building building)
    {
        if (posX < 0 || posY < 0 || posX + building.width > TestGrid.width || posY + building.height > TestGrid.height)
            return false;
        return true;
    }

    bool CheckCanPlaceBuilding(int posX, int posY, Building building)
    {
        for (int i = 0; i < building.height; i++)
        {
            for (int j = 0; j < building.width; j++)
            {
                if (TestGrid.myGrid[posX + j, posY + i] != TestGrid.zero)
                    return false;
            }
        }
        return true;
    }

    public enum direction { Up, Right, Down, Left }

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
}
