using Coimbra.Services.Events;

namespace SS3D.Interactions
{
    public partial struct IntentChanged : IEvent
    {
        public readonly IntentType Intent;
        public IntentChanged(IntentType intent)
        {
            Intent = intent;
        }
    }
}
