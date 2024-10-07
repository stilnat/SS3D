using System;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Base class for interactions which execute after a delay
    /// </summary>
    public abstract class GradualInteraction : Interaction
    {

        private bool _hasStarted;

        private float _startTime;
        private float _lastCheck;

        /// <summary>
        /// The delay in seconds before performing the interaction
        /// </summary>
        public float Delay { get; set; }

        /// <summary>
        /// The interval in seconds in which CanInteract is checked
        /// </summary>
        protected float CheckInterval { get; set; }

        public bool HasStarted => _hasStarted;

        public float StartTime => _startTime;

        /// <summary>
        /// Creates a client-side interaction object for this interaction
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            // Don't create client interaction if delay too small
            if (Math.Abs(Delay) < 0.1f)
            {
                return null;
            }

            return new ClientDelayedInteraction
            {
                Delay = Delay
            };
        }

        public abstract override string GetName(InteractionEvent interactionEvent);
        public override Sprite GetIcon(InteractionEvent interactionEvent) { return Icon; }
        public abstract override bool CanInteract(InteractionEvent interactionEvent);

        /// <summary>
        /// Sets up the time the interaction will take
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            StartCounter();
            return true;
        }

        /// <summary>
        /// Cancel the interaction in the given delay if conditions are not met anymore
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (_lastCheck + CheckInterval < Time.time && _hasStarted)
            {
                if (!CanInteract(interactionEvent))
                {
                    // Cancel the interaction
                    interactionEvent.Source.CancelInteraction(reference);
                    return true;
                }

                _lastCheck = Time.time;
            }

            if (_startTime + Delay < Time.time && _hasStarted)
            {
                return false;
            }

            return true;
        }

        protected void StartCounter()
        {
            _startTime = Time.time;
            _lastCheck = _startTime;
            _hasStarted = true;
        }

        /// <inheritdoc />
        public abstract override void Cancel(InteractionEvent interactionEvent, InteractionReference reference);
    }
}