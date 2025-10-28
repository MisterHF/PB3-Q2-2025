using UnityEngine;

public class ConfigurePivotPrefab : MonoBehaviour
{
    [SerializeField] private Transform pivotPoint;

    public void Rotate()
    {
        if (pivotPoint == null) { return; }
        //if (Input.GetKeyDown(KeyCode.Escape))
        {
            transform.RotateAround(pivotPoint.position, Vector3.up, 90);
        }
    }
}
