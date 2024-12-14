using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interaction in two times : First a delay, in which we make a particular check (CanInteract).
/// After this delay, we make another type of check (CanKeepInteracting).
/// The interaction keep running undefinitely until its cancelled.
/// </summary>
public abstract class ContinuousInteraction : Interaction
{
    
        public bool HasStarted { get; private set; }

        public bool HasDelayedStarted { get; private set; }

        public float Delay { get; set; }

        private float _startTime;

        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // If, before delayed start, something prevent interaction, stop interacting.
            if (_startTime + Delay > Time.time && HasStarted && !CanInteract(interactionEvent))
            {
                interactionEvent.Source.CancelInteraction(reference);
                return false;
            }

            // If delayed start has run and something prevent keeping interaction, stop interacting.
            if (_startTime + Delay <= Time.time && HasDelayedStarted && !CanKeepInteracting(interactionEvent, reference))
            {
                interactionEvent.Source.CancelInteraction(reference);
                return false;
            }

            // Run delayed start after given time.
            if (_startTime + Delay <= Time.time && !HasDelayedStarted)
            {
                StartDelayed(interactionEvent, reference);
                HasDelayedStarted = true;
            }
            
            // Check if interaction can keep going after delayed start
            return CanKeepInteracting(interactionEvent, reference);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            HasStarted = true;
            _startTime = Time.time;
            return true;
        }

        protected abstract bool CanKeepInteracting(InteractionEvent interactionEvent, InteractionReference reference);

        protected abstract void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference);
}
