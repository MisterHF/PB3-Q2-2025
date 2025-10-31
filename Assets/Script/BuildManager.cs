using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    TestGrid testGrid;
    Building spawnRoom;
    public List<Building> rooms = new List<Building>();
    public int _tileSize = 2;

    int currentRoomID = 1;
    bool SpawnRoomCall = false;
    public BuildManager(TestGrid grid)
    {
        testGrid = grid;

        // Création des types de salles
        rooms.Add(new Building(4, 4, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType4x4 1")));
        rooms.Add(new Building(3, 3, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType3x3")));
        rooms.Add(new Building(3, 2, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType3x2"), true));
        spawnRoom = new Building(2, 2, Resources.Load<GameObject>("Prefab/Rooms/LastTest/RoomType2x2"));

        SpawnRooms(4);
    }

    void SpawnRooms(int numberOfRooms)
    {
        Vector2Int roomPos = new Vector2Int(testGrid.width / 2, testGrid.height / 2);

        // Place la salle de départ
        PlaceBuilding(roomPos.x, roomPos.y, spawnRoom);
        GameObject _speawnRoom = Instantiate(spawnRoom.prefab, new Vector3(roomPos.x * _tileSize, 0, -roomPos.y * _tileSize), Quaternion.identity);
        GameObject _currentObject = _speawnRoom;
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

            Building nextBuilding = rooms[UnityEngine.Random.Range(0, rooms.Count)];
            int randDirection = UnityEngine.Random.Range(0, availableDirections.Count);
            B_D chosenDir = availableDirections[randDirection];

            Vector2Int newPos = GetNewBuildingPos(nextBuilding, chosenDir._direction, roomPos.x, roomPos.y, currentBuilding);

            if (PlaceBuilding(newPos.x, newPos.y, nextBuilding))
            {
                // Salle placée avec succès
                roomPos = new Vector2Int(newPos.x, newPos.y);

                _currentObject.GetComponent<OpenDoor>().building = currentBuilding;
                _currentObject.GetComponent<OpenDoor>()._directions = chosenDir._direction;
                _currentObject.GetComponent<OpenDoor>().roomId = (currentRoomID > 3) ? currentRoomID - 2 : 2;
                _currentObject.GetComponent<OpenDoor>().testGrid = testGrid;
                _currentObject.GetComponent<OpenDoor>().OpenDoors(nextBuilding);
                SpawnRoomCall = true;

                List<B_D> newDirs = SetDirection(chosenDir._direction, nextBuilding);
                availableDirections.AddRange(newDirs);
                availableDirections.RemoveAt(randDirection);

                GameObject _object = Instantiate(nextBuilding.prefab, new Vector3Int(roomPos.x * _tileSize, 0, -roomPos.y * _tileSize), Quaternion.identity);
                _object.GetComponent<OpenDoor>().building = nextBuilding;
                _object.GetComponent<OpenDoor>()._directions = GetOppositeEnum(chosenDir._direction);
                _object.GetComponent<OpenDoor>().roomId = currentRoomID - 2;
                _object.GetComponent<OpenDoor>().testGrid = testGrid;
                _object.GetComponent<OpenDoor>().OpenDoors(nextBuilding);
                currentBuilding = nextBuilding;
                _currentObject = _object;
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
                return new Vector2Int(posX, posY - currentBuilding.height);
            case direction.Right:
                return new Vector2Int(posX + currentBuilding.width, posY);
            case direction.Left:
                return new Vector2Int(posX - nextBuilding.width, posY);
            case direction.Down: // Down
                return new Vector2Int(posX, posY + nextBuilding.height);

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
            case direction.Down: // Down
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

        // 🔹 Si le bâtiment est en L (ou autre forme personnalisée)
        if (building.isL && building.myBuildingGrid != null)
        {
            for (int y = 0; y < building.height; y++)
            {
                for (int x = 0; x < building.width; x++)
                {
                    if (building.myBuildingGrid[x, y] == TestGrid.one) // Case occupée
                    {
                        testGrid.myGrid[posX + x, posY + y] = roomID;
                    }
                }
            }
        }
        else
        {
            // 🔸 Sinon (forme pleine / rectangle normal)
            for (int y = 0; y < building.height; y++)
            {
                for (int x = 0; x < building.width; x++)
                {
                    testGrid.myGrid[posX + x, posY + y] = roomID;
                }
            }
        }

        Debug.Log($"Salle placée avec ID {roomID} à ({posX},{posY})");
        return true;
    }


    public bool CheckBoundsGrid(int posX, int posY, Building building)
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




}

public enum direction { Up, Right, Down, Left }

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