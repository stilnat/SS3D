using DG.Tweening;
using SS3D.Systems.Animations;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class ToolWrench : Tool, IPlayAnimation
    {
        private Tween _animation;

        private Vector3 _positionAtAnimationStart;

        private Quaternion _rotationAtAnimationStart;

        public override void PlayAnimation()
        {
            RotateAroundPivot();
        }

        public override void StopAnimation()
        {
            if (_animation != null && _animation.IsPlaying())
            {
                _animation.Kill(); // Stop and kill the rotation animation
            }
        }


        // Method to rotate the screwdriver around the pivot point
        private void RotateAroundPivot()
        {
            // Calculate the direction from the pivot point to the screwdriver
            Vector3 pivotToScrewdriver = transform.position - InteractionPoint.position;

            _positionAtAnimationStart = transform.position;
            _rotationAtAnimationStart = transform.rotation;

            // Use DoTween to rotate around the pivot by modifying the screwdriver's position
            _animation = DOTween.To(
                    () => 0f,                  
                    (x) => RotateScrewdriver(x), 
                    25f,               
                    0.5f                     
                )
                .SetLoops(-1, LoopType.Yoyo) 
                .SetEase(Ease.InOutSine);    
        }

        // This method updates both position and rotation based on the current rotation angle
        private void RotateScrewdriver(float currentRotation)
        {
            transform.position = _positionAtAnimationStart;
            transform.rotation = _rotationAtAnimationStart;
            transform.RotateAround(InteractionPoint.position, InteractionPoint.right, currentRotation);
        }
    }
}