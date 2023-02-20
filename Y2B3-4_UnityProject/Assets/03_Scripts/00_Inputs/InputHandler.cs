using System;

using UnityEngine;
using UnityEngine.InputSystem;

using JetBrains.Annotations;

using F32   = System.Single;
using F32x2 = Unity.Mathematics.float2;

namespace Game.Inputs
{
    [PublicAPI]
    public sealed class InputHandler : MonoBehaviour
    {
        #region Variables
        
        //NOTE: [Walter] If we later want to have multiple cars you'll likely want to use the `PlayerInput` component.
        //For this reason I used methods such as OnThrottleInputStarted, OnThrottleInputPerformed, and OnThrottleInputCanceled to handle the input.
        //This leaves open the possibility of having multiple cars with very few changes, just comment out the Input Actions, add the `PlayerInput` component, and link up the events.
        
        [SerializeField] private InputActionReference         moveInputActionReference;
        [SerializeField, HideInInspector] private InputAction moveInputAction;
        
        [SerializeField] private InputActionReference         dashInputActionReference;
        [SerializeField, HideInInspector] private InputAction dashInputAction;

        [SerializeField] private InputActionReference         primaryFireInputActionReference;
        [SerializeField, HideInInspector] private InputAction primaryFireInputAction;


        [SerializeField] private InputActionReference         secondaryFireInputActionReference;
        [SerializeField, HideInInspector] private InputAction secondaryFireInputAction;

        [field:SerializeField] public F32x2                   MoveInput          { get; private set; }
        [field:SerializeField] public Boolean                 DashInput          { get; private set; }
        [field:SerializeField] public Boolean                 PrimaryFireInput   { get; private set; }
        [field:SerializeField] public Boolean                 SecondaryFireInput { get; private set; }
        
        public static Vector2                                 MousePosition => Mouse.current.position.ReadValue();
        
        #endregion

        #region Methods
        
        #if UNITY_EDITOR
        private void Reset()
        {
            //NOTE [Walter] On creation of the script (in the editor) the actions will automatically be configured.
            UpdateInputActions();
        }

        private void OnValidate()
        {
            //NOTE: [Walter] If inputs are assigned via the editor the actions have to be reconfigured.
            UpdateInputActions();
        }

        private void UpdateInputActions()
        {
            if (moveInputActionReference != null)
            {
                moveInputAction = moveInputActionReference.action;
            }

            if (dashInputActionReference != null)
            {
                dashInputAction = dashInputActionReference.action;
            }
            
            if (primaryFireInputActionReference != null)
            {
                primaryFireInputAction = primaryFireInputActionReference.action;
            }
            
            if (secondaryFireInputActionReference != null)
            {
                secondaryFireInputAction = secondaryFireInputActionReference.action;
            }
        }
        #endif

        private void OnEnable()
        {
            //NOTE: [Walter] This shouldn't be necessary, but apparently it is, I'm getting a null reference exception if I don't do this.
            moveInputAction.Enable();
            dashInputAction.Enable();
            primaryFireInputAction.Enable();
            secondaryFireInputAction.Enable();
        }
        
        private void OnDisable()
        {
            moveInputAction.Disable();
            dashInputAction.Disable();
            primaryFireInputAction.Disable();
            secondaryFireInputAction.Disable();
        }
        
        private void Awake()
        {
            moveInputAction.started            += OnMoveInputStarted;
            dashInputAction.started            += OnDashInputStarted;
            primaryFireInputAction.started     += OnPrimaryFireInputStarted;
            secondaryFireInputAction.started   += OnSecondaryFireInputStarted;

            moveInputAction.performed          += OnMoveInputPerformed;
            dashInputAction.performed          += OnDashInputPerformed;
            primaryFireInputAction.performed   += OnPrimaryFireInputPerformed;
            secondaryFireInputAction.performed += OnSecondaryFireInputPerformed;
            
            moveInputAction.canceled           += OnMoveInputCanceled;
            dashInputAction.canceled           += OnDashInputCanceled;
            primaryFireInputAction.canceled    += OnPrimaryFireInputCanceled;
            secondaryFireInputAction.canceled  += OnSecondaryFireInputCanceled;
        }

        private void OnDestroy()
        {
            moveInputAction.started            -= OnMoveInputStarted;
            dashInputAction.started            -= OnDashInputStarted;
            primaryFireInputAction.started     -= OnPrimaryFireInputStarted;
            secondaryFireInputAction.started   -= OnSecondaryFireInputStarted;

            moveInputAction.performed          -= OnMoveInputPerformed;
            dashInputAction.performed          -= OnDashInputPerformed;
            primaryFireInputAction.performed   -= OnPrimaryFireInputPerformed;
            secondaryFireInputAction.performed -= OnSecondaryFireInputPerformed;
                                                    
            moveInputAction.canceled           -= OnMoveInputCanceled;
            dashInputAction.canceled           -= OnDashInputCanceled;
            primaryFireInputAction.canceled    -= OnPrimaryFireInputCanceled;
            secondaryFireInputAction.canceled  -= OnSecondaryFireInputCanceled;
        }
        
        
        //NOTE: [Walter] Unfortunately these methods have to be public if you want to use `PlayerInputs` components (using Unity Events). With the current setup that isn't required.
        private void OnMoveInputStarted(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
        }
        private void OnMoveInputPerformed(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
        }
        private void OnMoveInputCanceled(InputAction.CallbackContext ctx)
        {
            MoveInput = F32x2.zero;
        }


        private void OnDashInputStarted(InputAction.CallbackContext ctx)
        {
            DashInput = ctx.ReadValueAsButton();
        }
        private void OnDashInputPerformed(InputAction.CallbackContext ctx)
        {
            DashInput = ctx.ReadValueAsButton();
        }
        private void OnDashInputCanceled(InputAction.CallbackContext ctx)
        {
            DashInput = false;
        }
        
        
        private void OnPrimaryFireInputStarted(InputAction.CallbackContext ctx)
        {
            PrimaryFireInput = ctx.ReadValueAsButton();
        }

        private void OnPrimaryFireInputPerformed(InputAction.CallbackContext ctx)
        {
            PrimaryFireInput = ctx.ReadValueAsButton();
        }

        private void OnPrimaryFireInputCanceled(InputAction.CallbackContext ctx)
        {
            PrimaryFireInput = false;
        }
        
        
        private void OnSecondaryFireInputStarted(InputAction.CallbackContext ctx)
        {
            SecondaryFireInput = ctx.ReadValueAsButton();
        }

        private void OnSecondaryFireInputPerformed(InputAction.CallbackContext ctx)
        {
            SecondaryFireInput = ctx.ReadValueAsButton();
        }

        private void OnSecondaryFireInputCanceled(InputAction.CallbackContext ctx)
        {
            SecondaryFireInput = false;
        }


        #endregion
    }
}