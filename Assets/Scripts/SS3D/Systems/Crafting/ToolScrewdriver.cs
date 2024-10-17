using DG.Tweening;
using SS3D.Systems.Animations;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class ToolScrewdriver : Tool
    {
        private Tween _animation;

        public override void PlayAnimation()
        {
            AnimateScrewdriver();
        }

        public override void StopAnimation()
        {
            if (_animation != null && _animation.IsPlaying())
            {
                _animation.Kill(); // Stop and kill the rotation animation
            }
        }

        private void AnimateScrewdriver()
        {
            _animation = transform.DORotate(new Vector3(0, 30, 0), 0.3f, RotateMode.LocalAxisAdd)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }
}
