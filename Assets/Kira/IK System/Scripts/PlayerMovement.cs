using UnityEngine;

#pragma warning disable CS0649
#pragma warning disable CS0414

namespace Kira
{
    [RequireComponent(typeof(Animator))]
    public class PlayerMovement : MonoBehaviour
    {
        private float inputX, inputY;
        private Animator animator;
        private Transform cachedTransform;

        private Camera cachedCamera;
        private Transform camTransform;
        private Vector3 desiredMoveDirection;
        private float speedMagnitude;

        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float allowPlayerRotation = 0.01f;
        [SerializeField] private float startAnimTime = 0.15f;
        [SerializeField] private float stopAnimTime = 0.2f;

        private static readonly int Anim_InputMagnitude = Animator.StringToHash("InputMagnitude");

        [SerializeField] private bool lockCursor = true;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            cachedTransform = transform;

            cachedCamera = Camera.main;
            if (cachedCamera != null) camTransform = cachedCamera.transform;

            HandleCursor();
        }

        private void HandleCursor()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            inputX = Input.GetAxis("Horizontal");
            inputY = Input.GetAxis("Vertical");
            InputMagnitude();
        }

        private void InputMagnitude()
        {
            var deltaTime = Time.deltaTime;
            speedMagnitude = new Vector2(inputX, inputY).sqrMagnitude;

            //Move Player
            if (speedMagnitude > allowPlayerRotation)
            {
                animator.SetFloat(Anim_InputMagnitude, speedMagnitude, startAnimTime, deltaTime * 2f);
                TurnPlayerByCam();
            }
            else if (speedMagnitude < allowPlayerRotation)
            {
                animator.SetFloat(Anim_InputMagnitude, speedMagnitude, stopAnimTime, deltaTime * 2f);
            }
        }

        private void TurnPlayerByCam()
        {
            var forward = camTransform.forward;
            var right = camTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            desiredMoveDirection = forward * inputY + right * inputX;
            cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, Quaternion.LookRotation(desiredMoveDirection), rotationSpeed * Time.deltaTime);
        }
    }
}