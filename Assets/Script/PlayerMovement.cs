using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Script
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("Mouvement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Caméra / Souris")]
        [SerializeField] private float lookSpeed = 100f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private InputSystem_Actions actions;

        private Vector2 moveInput;
        private Vector2 lookDelta;
        private Vector3 velocity;
        private float xRotation = 0f;

        private void OnEnable()
        {
            actions = new InputSystem_Actions();

            actions.Player.Move.performed += OnMove;
            actions.Player.Move.canceled += OnMoveCanceled;

            actions.Player.Look.performed += OnLook;
            actions.Player.Look.canceled += OnLookCanceled;

            actions.Player.Jump.started += OnJump;

            actions.Player.Enable();
        }

        private void OnDisable()
        {
            actions.Player.Move.performed -= OnMove;
            actions.Player.Move.canceled -= OnMoveCanceled;
            actions.Player.Look.performed -= OnLook;
            actions.Player.Look.canceled -= OnLookCanceled;

            actions.Player.Jump.started -= OnJump;

            actions.Player.Disable();
        }

        private void Start()
        {
            controller = GetComponent<CharacterController>();

            if (cameraTransform == null)
                cameraTransform = UnityEngine.Camera.main.transform;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleMovement();
            HandleCamera();
        }

        private void HandleMovement()
        {
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0)
                velocity.y = -2f;

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
        
        private void HandleCamera()
        {
            float mouseX = lookDelta.x * lookSpeed * Time.deltaTime;
            float mouseY = lookDelta.y * lookSpeed * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        // === EVENTS INPUT SYSTEM ===
        private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;
        private void OnLook(InputAction.CallbackContext ctx) => lookDelta = ctx.ReadValue<Vector2>();
        private void OnLookCanceled(InputAction.CallbackContext ctx) => lookDelta = Vector2.zero;

        // Jump immédiat quand l'action démarre
        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (controller != null && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }
}
