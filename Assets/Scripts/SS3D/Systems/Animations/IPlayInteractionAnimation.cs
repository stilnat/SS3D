using SS3D.Systems.Interactions;

namespace SS3D.Systems.Animations
{
    public interface IPlayInteractionAnimation
    {
        public void PlayAnimation(InteractionType interactionType);

        public void StopAnimation();
    }
}
