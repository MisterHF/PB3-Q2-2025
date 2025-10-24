// csharp
using PurrNet;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : NetworkBehaviour
{
    [Header("Mouvement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Camera / Souris")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSpeed = 100f;

    private CharacterController controller;
    private InputSystem_Actions actions;

    private Camera playerCamera;
    private AudioListener playerAudioListener;

    private Vector2 moveInput;
    private Vector2 lookDelta;
    private bool _willJump;

    private float xRotation = 0f;
    private float verticalVelocity = 0f;

    private void OnEnable()
    {
        actions = new InputSystem_Actions();

        actions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        actions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        actions.Player.Look.performed += ctx => lookDelta = ctx.ReadValue<Vector2>();
        actions.Player.Look.canceled += ctx => lookDelta = Vector2.zero;

        actions.Player.Jump.started += ctx => _willJump = true;

        // Activation des actions uniquement pour le joueur local dans Start()
    }

    private void OnDisable()
    {
        if (actions != null)
        {
            // safe to call Disable même si pas activé
            try { actions.Player.Disable(); } catch { }
        }
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Si pas assigné, chercher une camera enfant locale puis fallback sur Camera.main
        if (cameraTransform == null)
        {
            var camChild = GetComponentInChildren<Camera>();
            if (camChild != null)
                cameraTransform = camChild.transform;
            else if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        // Récupère Camera et AudioListener si présents
        if (cameraTransform != null)
        {
            playerCamera = cameraTransform.GetComponent<Camera>();
            playerAudioListener = cameraTransform.GetComponent<AudioListener>();
            if (playerCamera != null)
                playerCamera.enabled = isOwner; // active seulement pour le propriétaire local
            if (playerAudioListener != null)
                playerAudioListener.enabled = isOwner;
        }

        if (isOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            networkManager.onTick += OnTick;

            // Activer les actions d'input uniquement pour le joueur local
            try { actions.Player.Enable(); } catch { }
        }
        else
        {
            // s'assurer que le rendu local du joueur distant n'affiche pas sa caméra
            if (playerCamera != null) playerCamera.enabled = false;
            if (playerAudioListener != null) playerAudioListener.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
            networkManager.onTick -= OnTick;
    }

    private void Update()
    {
        if (!isOwner) return;

        HandleCamera();
    }

    private void HandleCamera()
    {
        float mouseX = lookDelta.x * lookSpeed * Time.deltaTime;
        float mouseY = lookDelta.y * lookSpeed * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void OnTick(bool asServer)
    {
        if (!isOwner) return;

        InputData inputData = new InputData
        {
            move = moveInput,
            jump = _willJump
        };
        _willJump = false;

        MoveServerRpc(inputData);
    }

    [ServerRpc]
    private void MoveServerRpc(InputData inputData)
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        Vector3 move = transform.right * inputData.move.x + transform.forward * inputData.move.y;
        Vector3 velocity = move * moveSpeed;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (inputData.jump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
        }

        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.fixedDeltaTime);
    }

    private struct InputData
    {
        public Vector2 move;
        public bool jump;
    }
}
