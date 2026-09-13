using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class InputManager : MonoBehaviour
    {
        public float SteerInput { get; private set; }
        public float ThrottleInput { get; private set; }
        public bool IsDriftPressed { get; private set; }
        public event Action OnWhipTap;

        private void Update()
        {
            SteerInput = Input.GetAxis("Horizontal");
            ThrottleInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
            IsDriftPressed = Input.GetKey(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                OnWhipTap?.Invoke();
            }
        }
    }
}
