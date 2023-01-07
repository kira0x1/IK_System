using System;
using UnityEngine;

#pragma warning disable CS0649

namespace Kira.IK
{
    [SelectionBase]
    [RequireComponent(typeof(Animator))]
    public class IKManager : MonoBehaviour
    {
        #region Needs Initialization

        //These variables need initialization
        private Transform m_CachedTransform;
        private Animator m_Animator;

        #endregion

        #region IK Variables

        //Feet Positions
        private Vector3 m_RightFootPosition, m_LeftFootPosition;

        //Feet IK Positions
        private Vector3 m_RightFootIkPosition, m_LeftFootIkPosition;

        //Feet IK Rotations
        private Quaternion m_RightFootIkRotation, m_LeftFootIkRotation;

        //Last Positions
        private float m_lastPelvisPositionY, m_lastRightFootPositionY, m_LastLeftPositionY;


        [Header("Feet Grounder")] [SerializeField]
        private bool m_EnableFeetIk = true;

        [Tooltip("If enabled will rotate feet IK")] [SerializeField]
        private bool m_EnableIkRotation = true;

        [Range(0f, 2f)] [SerializeField] private float m_HeightFromGroundRaycast = 1.14f;
        [Range(0f, 2f)] [SerializeField] private float m_RaycastDownDistance = 1.5f;
        [Range(0, 1f)] [SerializeField] private float m_ForwardFootOffset = 0.1f;
        [SerializeField] private LayerMask m_EnvironmentLayer;
        [SerializeField] private float m_PelvisOffset = 0f;
        [Range(0f, 80f)] [SerializeField] private float m_PelvisUpAndDownSpeed = 15f;
        [Range(0f, 80f)] [SerializeField] private float m_FeetToIkPositionSpeed = 12f;

        //Left Foot Animation Controller Variable Name
        private const string LEFT_FOOT_CURVE_NAME = "LeftFootCurve";

        //Right Foot Animation Controller Variable Name
        private const string RIGHT_FOOT_CURVE_NAME = "RightFootCurve";

        //Show raycasts
        [SerializeField] private bool m_ShowSolverDebug = true;

        #endregion

        #region Animation Variables

        private static readonly int RightFootCurve = Animator.StringToHash(RIGHT_FOOT_CURVE_NAME);
        private static readonly int LeftFootCurve = Animator.StringToHash(LEFT_FOOT_CURVE_NAME);

        #endregion

        private void Awake()
        {
            m_CachedTransform = transform;
            m_Animator = GetComponent<Animator>();
        }


        /// <summary>
        /// Move, and handle Foot Ik
        /// </summary>
        private void FixedUpdate()
        {
            if (!m_EnableFeetIk) return;

            //Adjust feet positions
            AdjustFeetTarget(out m_RightFootPosition, HumanBodyBones.RightFoot);
            AdjustFeetTarget(out m_LeftFootPosition, HumanBodyBones.LeftFoot);

            //Find raycast to ground to find positions
            FeetPositionSolver(m_RightFootPosition, out m_RightFootIkPosition, ref m_RightFootIkRotation);
            FeetPositionSolver(m_LeftFootPosition, out m_LeftFootIkPosition, ref m_LeftFootIkRotation);
        }

        /// <summary>
        /// Calculate and Move Foot IK, and Pelvis Position
        /// </summary>
        /// <param name="layerIndex"></param>
        private void OnAnimatorIK(int layerIndex)
        {
            if (!m_EnableFeetIk) return;
            MovePelvisHeight();

            //Right Foot
            m_Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);

            if (m_EnableIkRotation)
                m_Animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, m_Animator.GetFloat(RightFootCurve));

            MoveFeetToIkPoint(AvatarIKGoal.RightFoot, m_RightFootIkPosition, m_RightFootIkRotation, ref m_lastRightFootPositionY);

            //Left Foot
            m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);

            if (m_EnableIkRotation)
                m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, m_Animator.GetFloat(LeftFootCurve));

            MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, m_LeftFootIkPosition, m_LeftFootIkRotation, ref m_LastLeftPositionY);
        }


        /// <summary>
        /// Move Feet to Point
        /// </summary>
        /// <param name="foot"></param>
        /// <param name="positionIkHolder"></param>
        /// <param name="rotationIkHolder"></param>
        /// <param name="lastFootPositionY"></param>
        private void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder, ref float lastFootPositionY)
        {
            Vector3 targetIkPosition = m_Animator.GetIKPosition(foot);
            if (positionIkHolder != Vector3.zero)
            {
                targetIkPosition = m_CachedTransform.InverseTransformPoint(targetIkPosition);
                positionIkHolder = m_CachedTransform.InverseTransformPoint(positionIkHolder);

                var yLerp = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, m_FeetToIkPositionSpeed * Time.fixedDeltaTime);
                targetIkPosition.y += yLerp;
                lastFootPositionY = yLerp;
                targetIkPosition = m_CachedTransform.TransformPoint(targetIkPosition);
                m_Animator.SetIKRotation(foot, rotationIkHolder);
            }

            m_Animator.SetIKPosition(foot, targetIkPosition);
        }

        /// <summary>
        /// Calculate and Set Pelvis Position
        /// </summary>
        private void MovePelvisHeight()
        {
            if (m_RightFootIkPosition == Vector3.zero || m_LeftFootIkPosition == Vector3.zero || Math.Abs(m_lastPelvisPositionY) < 0.1f)
            {
                m_lastPelvisPositionY = m_Animator.bodyPosition.y;
                return;
            }

            var position = m_CachedTransform.position;
            float leftOffsetPosition = m_LeftFootIkPosition.y - position.y;
            float rightOffsetPosition = m_RightFootIkPosition.y - position.y;

            float totalOffset = (leftOffsetPosition < rightOffsetPosition) ? leftOffsetPosition : rightOffsetPosition;

            Vector3 newPelvisPosition = m_Animator.bodyPosition + Vector3.up * totalOffset;
            newPelvisPosition.y = Mathf.Lerp(m_lastPelvisPositionY, newPelvisPosition.y, m_PelvisUpAndDownSpeed * Time.fixedDeltaTime);

            m_Animator.bodyPosition = newPelvisPosition;
            m_lastPelvisPositionY = m_Animator.bodyPosition.y;
        }

        /// <summary>
        /// Raycast and then Calculate Feet IK Position and Rotation
        /// </summary>
        /// <param name="footPosition"></param>
        /// <param name="feetIkPositions"></param>
        /// <param name="feetIkRotations"></param>
        private void FeetPositionSolver(Vector3 footPosition, out Vector3 feetIkPositions, ref Quaternion feetIkRotations)
        {
            var forwardDir = m_CachedTransform.TransformVector(Vector3.forward * m_ForwardFootOffset);
            footPosition += forwardDir;

            if (m_ShowSolverDebug)
                Debug.DrawLine(footPosition, footPosition + Vector3.down * (m_RaycastDownDistance + m_HeightFromGroundRaycast), Color.yellow);

            if (Physics.Raycast(footPosition, Vector3.down, out RaycastHit feetOutHit, m_RaycastDownDistance + m_HeightFromGroundRaycast, m_EnvironmentLayer))
            {
                feetIkPositions = footPosition;
                feetIkPositions.y = feetOutHit.point.y + m_PelvisOffset;
                feetIkRotations = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * m_CachedTransform.rotation;
                return;
            }

            feetIkPositions = Vector3.zero; // failed
        }

        private void AdjustFeetTarget(out Vector3 feetPositions, HumanBodyBones foot)
        {
            feetPositions = m_Animator.GetBoneTransform(foot).position;
            feetPositions += m_CachedTransform.TransformVector(Vector3.forward * m_ForwardFootOffset);
            feetPositions.y = m_CachedTransform.position.y + m_HeightFromGroundRaycast;
        }
    }
}