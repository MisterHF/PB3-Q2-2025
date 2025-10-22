﻿using UnityEngine;
using UnityEngine.InputSystem;

namespace Script.Camera
{
    public class PlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float lookSpeed = 2f;

        private Vector2 lookDelta;
        private Vector2 moveInput;
        private bool isAscending = false;
        private bool isDescending = false;
        private bool isRightMouseDown = false;

        public static bool IsRightMouseDown;

        private InputSystem_Actions actions;

        private void OnEnable()
        {
            actions = new InputSystem_Actions();
            actions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            actions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
            actions.Player.Look.performed += ctx => lookDelta = ctx.ReadValue < Vector2>();
            actions.Player.Move.canceled += ctx => lookDelta = Vector2.zero;
            actions.Player.Enable();
        }

        private void OnDisable()
        {
            actions.Player.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
            actions.Player.Move.canceled -= ctx => moveInput = Vector2.zero;
            actions.Player.Look.performed -= ctx => lookDelta = ctx.ReadValue<Vector2>();
            actions.Player.Move.canceled -= ctx => lookDelta = Vector2.zero;
            actions.Player.Disable();
        }


        private void Update()
        {

        }

    }
}