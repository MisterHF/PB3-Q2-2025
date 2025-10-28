using UnityEngine;

public class OrientOnActiveCamera : MonoBehaviour
{
    [Tooltip("Verrouille la rotation sur l'axe Y pour rester vertical")]
    public bool LockY = true;

    [Tooltip("Vitesse de lissage (0 = instantané)")]
    public float Smooth = 10f;

    private Camera targetCamera;

    void OnEnable()
    {
        UpdateTargetCamera();
        FaceCamera(true);
    }

    void LateUpdate()
    {
        if (targetCamera == null) UpdateTargetCamera();
        if (targetCamera == null) return;

        FaceCamera(false);
    }

    private void UpdateTargetCamera()
    {
        targetCamera = Camera.main ?? Camera.current;
    }

    private void FaceCamera(bool _Instant)
    {
        Vector3 _dir = transform.position - targetCamera.transform.position;
        if (LockY) _dir.y = 0f;

        if (_dir.sqrMagnitude <= 0.0001f) return;

        Quaternion _targetRot = Quaternion.LookRotation(_dir.normalized, Vector3.up);

        if (_Instant || Smooth <= 0f)
            transform.rotation = _targetRot;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Smooth * Time.deltaTime);
    }
}