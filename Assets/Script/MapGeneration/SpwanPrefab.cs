using UnityEngine;
using System.Collections.Generic;

public class SpwanPrefab : MonoBehaviour
{
    [Header("Prefabs de bâtiments")]
    public GameObject[] buildingPrefabs;

    [Header("Paramètres de génération")]
    public int buildingCount = 20;
    public float spawnRadius = 50f;
    public float tileSize = 4f;

    [Header("Options")]
    public bool avoidOverlap = false;

    private List<Vector2> placedPositions = new List<Vector2>();

    void Start()
    {
        GenerateBuildings();
    }

    void GenerateBuildings()
    {
        int safety = 0;
        int placed = 0;

        while (placed < buildingCount && safety < 1000)
        {
            safety++;

            GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];

            Vector2 pos = GetRandomPointInCircle(spawnRadius);

            pos.x = RoundToMultiple(pos.x, tileSize);
            pos.y = RoundToMultiple(pos.y, tileSize);

            if (avoidOverlap && IsOverlapping(pos, prefab))
                continue;

            Instantiate(prefab, new Vector3(pos.x, 0f, pos.y), Quaternion.identity, transform);
            placedPositions.Add(pos);

            placed++;
        }

        Debug.Log($"Génération terminée : {placed} bâtiments placés.");
    }

    Vector2 GetRandomPointInCircle(float radius)
    {
        float t = 2 * Mathf.PI * Random.value;
        float u = Random.value + Random.value;
        float r = (u > 1) ? (2 - u) : u;
        return new Vector2(radius * r * Mathf.Cos(t), radius * r * Mathf.Sin(t));
    }

    float RoundToMultiple(float n, float m)
    {
        return Mathf.Round(n / m) * m;
    }

    bool IsOverlapping(Vector2 pos, GameObject prefab)
    {
        float prefabSize = GetPrefabRadius(prefab);
        foreach (var p in placedPositions)
        {
            if (Vector2.Distance(p, pos) < prefabSize * tileSize)
                return true;
        }
        return false;
    }

    float GetPrefabRadius(GameObject prefab)
    {
        Renderer rend = prefab.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Vector3 size = rend.bounds.size;
            return Mathf.Max(size.x, size.z) / (2f * tileSize);
        }
        else
        {
            return 1f;
        }
    }
}
