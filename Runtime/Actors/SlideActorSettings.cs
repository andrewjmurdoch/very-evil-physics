using System;
using UnityEngine;

namespace VED.Physics
{
    [CreateAssetMenu(fileName = "SlideActorSettings", menuName = "VED/Physics/SlideActorSettings", order = 0)]
    public class SlideActorSettings : ScriptableObject
    {
        public static AnimationCurve DefaultCurve => new AnimationCurve(new Keyframe[2] { new Keyframe(0f, 1f, 0f, -0.45f), new Keyframe(6f, 0f, 0f, 0f) });

        [Serializable]
        public class ConversionCurves
        {
            [SerializeField] public AnimationCurve Positive = DefaultCurve;
            [SerializeField] public AnimationCurve Negative = DefaultCurve;

            public AnimationCurve this[float sign] => sign > 0 ? Positive : Negative;
        }

        [Serializable]
        public class ResetVelocity
        {
            [SerializeField] public bool Square   = true;
            [SerializeField] public bool Triangle = true;
            [SerializeField] public bool Circle   = true;
        }

        #region Up
        [SerializeField] public bool CanSlideUpMovingLeft = true; // whether actor can slide up, when moving left
        [SerializeField] public bool CanSlideUpMovingRight = true; // whether actor can slide up, when moving right
        [SerializeField] public float MaxSlideUpDist = 0.2f;
        [SerializeField] public ConversionCurves SlideUpConversionCurve;
        [SerializeField] public ResetVelocity SlideUpResetVelocity = new ResetVelocity();
        #endregion

        #region Down
        [SerializeField] public bool CanSlideDownMovingLeft = true; // whether actor can slide down, when moving left
        [SerializeField] public bool CanSlideDownMovingRight = true; // whether actor can slide down, when moving right
        [SerializeField] public float MaxSlideDownDist = 0.2f;
        [SerializeField] public ConversionCurves SlideDownConversionCurve;
        [SerializeField] public ResetVelocity SlideDownResetVelocity = new ResetVelocity();
        #endregion

        #region Left
        [SerializeField] public bool CanSlideLeftMovingUp = true; // whether actor can slide left, when moving up
        [SerializeField] public bool CanSlideLeftMovingDown = true; // whether actor can slide left, when moving down, useful for actors with gravity
        [SerializeField] public float MaxSlideLeftDist = 0.2f;
        [SerializeField] public ConversionCurves SlideLeftConversionCurve;
        [SerializeField] public ResetVelocity SlideLeftResetVelocity = new ResetVelocity();
        #endregion

        #region Right
        [SerializeField] public bool CanSlideRightMovingUp = true; // whether actor can slide right, when moving up
        [SerializeField] public bool CanSlideRightMovingDown = true; // whether actor can slide right, when moving down, useful for actors with gravity
        [SerializeField] public float MaxSlideRightDist = 0.2f;
        [SerializeField] public ConversionCurves SlideRightConversionCurve;
        [SerializeField] public ResetVelocity SlideRightResetVelocity = new ResetVelocity();
        #endregion
    }
}