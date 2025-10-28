using PurrNet;
using Script.Object;
using Script.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Script
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMovement : NetworkBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 1f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Header("Look Settings")] [SerializeField]
        private float lookSensitivity = 15f;

        [SerializeField] private float maxLookAngle = 80f;

        [Header("References")] [SerializeField]
        private Camera playerCamera;
        [SerializeField]
        private GameObject holder;

        public GameObject Holder => holder;

        public Camera PlayerCamera => playerCamera;

        private CharacterController characterController;
        private Vector3 velocity;
        private float verticalRotation = 0f;
        private Vector2 input;
        private Vector2 look;
        private bool jump;

        private InputSystem_Actions actions;

        private Inventory inventory;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
            playerCamera.gameObject.SetActive(isOwner);
            inventory = GetComponent<Inventory>();
            SetActions();
        }
    
        private void SetActions()
        {
            actions = new InputSystem_Actions();
            actions.Enable();
            actions.Player.Move.performed += _Ctx => input = _Ctx.ReadValue<Vector2>();
            actions.Player.Move.canceled += _Ctx => input = Vector2.zero;
            actions.Player.Jump.started += _Ctx => jump = true;
            actions.Player.Jump.canceled += _Ctx => jump = false;
            actions.Player.Look.performed += _Ctx => look = _Ctx.ReadValue<Vector2>();
            actions.Player.Look.canceled += _Ctx => look = Vector2.zero;
            actions.Player.Interact.started += Interact;
        }

        private void Interact(InputAction.CallbackContext _Obj)
        {
            if(!isOwner && !isServer) return;
            var _ray = Physics.RaycastAll(playerCamera.transform.position + Vector3.up * 0.03f, playerCamera.transform.forward, 20f);
            if (_ray == null || _ray.Length <= 0) return;
            foreach (var _object in _ray)
            {
                if (_object.transform.TryGetComponent(out Objects _obj))
                {
                    inventory.AddItem(_obj.GetObjects(this));
                }
            }
        }

        private void OnDisable()
        {
            if (!isOwner) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            actions.Disable();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleJump();
        }

        private void HandleMovement()
        {
            bool _isGrounded = IsGrounded();
            if (_isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            Vector3 _moveDirection = transform.right * input.x + transform.forward * input.y;
            _moveDirection = Vector3.ClampMagnitude(_moveDirection, 1f);

            float _currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
            characterController.Move(_moveDirection * _currentSpeed * Time.deltaTime);
        }

        private void HandleJump()
        {
            if (jump && IsGrounded())
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleRotation()
        {
            float _mouseX = look.x / lookSensitivity;
            float _mouseY = look.y / lookSensitivity;

            verticalRotation -= _mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

            transform.Rotate(Vector3.up * _mouseX);
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.03f, Vector3.down, groundCheckDistance);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.03f, Vector3.down * groundCheckDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerCamera.transform.position + Vector3.up * 0.03f, playerCamera.transform.forward * 20f);
        }
#endif
    }
}