using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    TestGrid testGrid;
    Building spawnRoom;
    public List<Building> rooms = new List<Building>();
    public int _tileSize = 2;

    int currentRoomID = 1;

    public BuildManager(TestGrid grid)
    {
        testGrid = grid;

        // Création des types de salles
        rooms.Add(new Building(4, 4, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType4x4 1")));
        rooms.Add(new Building(3, 3, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType3x3")));
        rooms.Add(new Building(3, 2, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType3x2")));
        spawnRoom = new Building(2, 2, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType2x2"));

        SpawnRooms(5);
    }

    void SpawnRooms(int numberOfRooms)
    {
        Vector2Int roomPos = new Vector2Int(testGrid.width / 2, testGrid.height / 2);

        // Place la salle de départ
        PlaceBuilding(roomPos.x, roomPos.y, spawnRoom);
        Instantiate(spawnRoom.prefab, new Vector3(roomPos.x * _tileSize, 0, -roomPos.y * _tileSize), Quaternion.identity);

        Building currentBuilding = spawnRoom;
        List<B_D> availableDirections = SetDirection(direction.Up, spawnRoom);

        int attempts = 0;
        for (int i = 0; i < numberOfRooms;)
        {
            if (availableDirections.Count == 0)
            {
                Debug.Log("Plus de directions disponibles.");
                break;
            }

            Building nextBuilding = rooms[Random.Range(0, rooms.Count)];
            int randDirection = Random.Range(0, availableDirections.Count);
            B_D chosenDir = availableDirections[randDirection];

            Vector2Int newPos = GetNewBuildingPos(nextBuilding, chosenDir._direction, roomPos.x, roomPos.y, currentBuilding);

            if (PlaceBuilding(newPos.x, newPos.y, nextBuilding))
            {
                // Salle placée avec succès
                roomPos = new Vector2Int(newPos.x, newPos.y);
                currentBuilding = nextBuilding;

                List<B_D> newDirs = SetDirection(chosenDir._direction, nextBuilding);
                availableDirections.AddRange(newDirs);
                availableDirections.RemoveAt(randDirection);

                Instantiate(nextBuilding.prefab, new Vector3Int(roomPos.x * _tileSize, 0, -roomPos.y * _tileSize), Quaternion.identity);
                i++; // On compte seulement les salles placées
            }
            else
            {
                // Impossible de placer ici, on retire la direction testée
                availableDirections.RemoveAt(randDirection);
                attempts++;
                if (attempts > 200)
                {
                    Debug.LogWarning("Trop de tentatives pour placer des salles, arrêt de la génération.");
                    break;
                }
            }
        }
    }

    Vector2Int GetNewBuildingPos(Building nextBuilding, direction dir, int posX, int posY, Building currentBuilding)
    {
        switch (dir)
        {
            case direction.Up:
                return new Vector2Int(posX, posY + currentBuilding.height);
            case direction.Right:
                return new Vector2Int(posX + currentBuilding.width, posY);
            case direction.Left:
                return new Vector2Int(posX - nextBuilding.width, posY);
            default: // Down
                return new Vector2Int(posX, posY - nextBuilding.height);
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

        int roomID = currentRoomID++;

        for (int i = 0; i < building.height; i++)
        {
            for (int j = 0; j < building.width; j++)
            {
                testGrid.myGrid[posX + j, posY + i] = roomID;
            }
        }

        Debug.Log($"Salle placée avec ID {roomID} à ({posX},{posY})");
        return true;
    }

    bool CheckBoundsGrid(int posX, int posY, Building building)
    {
        return posX >= 0 && posY >= 0 && posX + building.width < testGrid.width && posY + building.height < testGrid.height;
    }

    bool CheckCanPlaceBuilding(int posX, int posY, Building building)
    {
        for (int i = 0; i < building.height; i++)
            for (int j = 0; j < building.width; j++)
                if (testGrid.myGrid[posX + j, posY + i] != TestGrid.zero)
                    return false;

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
