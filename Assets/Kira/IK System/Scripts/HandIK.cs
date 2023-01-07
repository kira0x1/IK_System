using UnityEngine;

#pragma warning disable CS0649
#pragma warning disable CS0414

namespace Kira.IK
{
    [RequireComponent(typeof(Animator))]
    public class HandIK : MonoBehaviour
    {
        private Animator m_Animator;
        [SerializeField] private bool m_IKEnabled = true;
        [SerializeField] private Transform m_RightHandObject;
        [SerializeField] private Transform m_LookObject;


        private void Start()
        {
            m_Animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (m_IKEnabled)
            {
                if (m_LookObject != null)
                {
                    IkLook();
                }

                if (m_RightHandObject != null)
                {
                    IkHandHold(AvatarIKGoal.RightHand, m_RightHandObject.position, m_RightHandObject.rotation);
                }

                return;
            }

            //If ik is not enabled then reset the right hand and head back to their original positions
            IKResetHand(AvatarIKGoal.RightHand);
        }

        private void IkLook()
        {
            m_Animator.SetLookAtWeight(1);
            m_Animator.SetLookAtPosition(m_LookObject.position);
        }

        private void IkHandHold(AvatarIKGoal goal, Vector3 targetPosition, Quaternion targetRotation)
        {
            m_Animator.SetIKPositionWeight(goal, 1);
            m_Animator.SetIKRotationWeight(goal, 1);

            m_Animator.SetIKPosition(goal, targetPosition);
            m_Animator.SetIKRotation(goal, targetRotation);
        }

        private void IKResetHand(AvatarIKGoal hand)
        {
            m_Animator.SetIKPositionWeight(hand, 0);
            m_Animator.SetIKRotationWeight(hand, 0);
            m_Animator.SetLookAtWeight(0);
        }
    }
}