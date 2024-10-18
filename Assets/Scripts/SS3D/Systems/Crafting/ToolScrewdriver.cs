using DG.Tweening;
using SS3D.Systems.Animations;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class ToolScrewdriver : MonoBehaviour, IInteractiveTool
    {
        private Tween _animation;

        [SerializeField]
        private Transform _interactionPoint;

        public GameObject GameObject => gameObject;

        public Transform InteractionPoint => _interactionPoint;

        public void PlayAnimation()
        {
            AnimateScrewdriver();
        }

        public void StopAnimation()
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
