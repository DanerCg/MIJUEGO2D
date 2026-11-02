using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Rescate
{
    /// <summary>
    /// Capa de entrada unificada del juego.
    /// Fuentes (todas simultáneas, se suman):
    ///  - Input Actions del proyecto (Assets/Game/Resources/RescateInput.inputactions):
    ///    teclado, ratón y gamepad genérico (DualSense, Xbox, etc.).
    ///  - Botones táctiles de Android (banderas TouchX, multitáctil).
    ///  - Respaldo de teclado directo si el asset de acciones no está disponible.
    /// </summary>
    public static class InputHub
    {
        public static bool TouchLeft, TouchRight, TouchJump, TouchAttack, TouchRun;

        const string ActionsResource = "RescateInput";
        const float StickDeadzone = 0.25f;

        static bool prevJump, prevAttack;

#if ENABLE_INPUT_SYSTEM
        static InputActionAsset actions;
        static InputAction moveAction, jumpAction, attackAction, runAction, magicAction, pauseAction;
        static InputAction cancelAction, submitAction;
        static bool initialized;

        /// <summary>Limpia el estado estático al entrar en Play Mode.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            actions = null;
            moveAction = jumpAction = attackAction = runAction = magicAction = pauseAction = null;
            cancelAction = submitAction = null;
            initialized = false;
            prevJump = prevAttack = false;
            TouchLeft = TouchRight = TouchJump = TouchAttack = TouchRun = false;
        }

        static void EnsureActions()
        {
            if (initialized) return;
            initialized = true;

            actions = Resources.Load<InputActionAsset>(ActionsResource);
            if (actions == null)
            {
                Debug.LogWarning("[InputHub] No se encontró Resources/" + ActionsResource +
                                 ".inputactions. Se usará el respaldo de teclado/ratón.");
                return;
            }

            moveAction = actions.FindAction("Gameplay/Move", false);
            jumpAction = actions.FindAction("Gameplay/Jump", false);
            attackAction = actions.FindAction("Gameplay/Attack", false);
            runAction = actions.FindAction("Gameplay/Run", false);
            magicAction = actions.FindAction("Gameplay/Magic", false);
            pauseAction = actions.FindAction("Gameplay/Pause", false);
            cancelAction = actions.FindAction("UI/Cancel", false);
            submitAction = actions.FindAction("UI/Submit", false);

            InputActionMap gameplay = actions.FindActionMap("Gameplay", false);
            if (gameplay != null) gameplay.Enable();

            // A nivel de código solo se leen Cancel y Submit; la navegación de los
            // menús la resuelve el InputSystemUIInputModule del EventSystem.
            if (cancelAction != null) cancelAction.Enable();
            if (submitAction != null) submitAction.Enable();
        }

        static bool Pressed(InputAction a) { return a != null && a.WasPressedThisFrame(); }
        static bool Held(InputAction a) { return a != null && a.IsPressed(); }
        static Vector2 ReadValue(InputAction a) { return a != null ? a.ReadValue<Vector2>() : Vector2.zero; }

        /// <summary>True si hay un gamepad conectado (DualSense u otro compatible).</summary>
        public static bool GamepadConnected { get { return Gamepad.current != null; } }
#else
        public static bool GamepadConnected { get { return false; } }
#endif

        public static float Horizontal
        {
            get
            {
                float h = 0f;
                if (TouchLeft) h -= 1f;
                if (TouchRight) h += 1f;
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (moveAction != null)
                {
                    float x = ReadValue(moveAction).x;
                    if (Mathf.Abs(x) >= StickDeadzone) h += x;
                }
                else
                {
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
                        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                    }
                }
#else
                h += UnityEngine.Input.GetAxisRaw("Horizontal");
#endif
                return Mathf.Clamp(h, -1f, 1f);
            }
        }

        public static bool JumpHeld
        {
            get
            {
                if (TouchJump) return true;
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (jumpAction != null) return Held(jumpAction);
                var kb = Keyboard.current;
                return kb != null && kb.spaceKey.isPressed;
#else
                return UnityEngine.Input.GetKey(KeyCode.Space);
#endif
            }
        }

        public static bool AttackHeld
        {
            get
            {
                if (TouchAttack) return true;
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (attackAction != null) return Held(attackAction);
                var kb = Keyboard.current;
                return kb != null && kb.jKey.isPressed;
#else
                return UnityEngine.Input.GetKey(KeyCode.J);
#endif
            }
        }

        public static bool RunHeld
        {
            get
            {
                if (TouchRun) return true;
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (runAction != null) return Held(runAction);
                var kb = Keyboard.current;
                if (kb == null) return false;
                return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
#else
                return UnityEngine.Input.GetKey(KeyCode.LeftShift);
#endif
            }
        }

        public static bool MagicPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (magicAction != null) return Pressed(magicAction);
                var kb = Keyboard.current;
                bool key = kb != null && (kb.fKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame);
                var mouse = Mouse.current;
                bool click = mouse != null && mouse.rightButton.wasPressedThisFrame;
                return key || click;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.F);
#endif
            }
        }

        /// <summary>Escape (teclado) o botón Options/Start (gamepad).</summary>
        public static bool PausePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (pauseAction != null) return Pressed(pauseAction);
                var kb = Keyboard.current;
                return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        /// <summary>Enter/Espacio (teclado) o Cross/A (gamepad).</summary>
        public static bool ConfirmPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (submitAction != null) return Pressed(submitAction);
                var kb = Keyboard.current;
                if (kb == null) return false;
                return kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.Return);
#endif
            }
        }

        /// <summary>Circle/B (gamepad) o Escape (teclado): volver/cancelar en menús.</summary>
        public static bool CancelPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                EnsureActions();
                if (cancelAction != null) return Pressed(cancelAction);
                var kb = Keyboard.current;
                return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        public static bool JumpPressed { get { return JumpHeld && !prevJump; } }
        public static bool JumpReleased { get { return !JumpHeld && prevJump; } }
        public static bool AttackPressed { get { return AttackHeld && !prevAttack; } }

        /// <summary>Debe llamarse al final del frame del jugador para calcular los flancos.</summary>
        public static void Tick()
        {
            prevJump = JumpHeld;
            prevAttack = AttackHeld;
        }

        public static void ResetTouch()
        {
            TouchLeft = TouchRight = TouchJump = TouchAttack = TouchRun = false;
        }
    }
}
