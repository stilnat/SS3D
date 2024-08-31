using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class IntentController : MonoBehaviour
    {
        public event EventHandler<Intent> OnIntentChange;

        private Intent _intent;

        public Intent Intent => _intent;

        // Update is called once per frame
        protected void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Space))
            {
                return;
            }

            _intent = _intent == Intent.Def ? Intent.Harm : Intent.Def;

            OnIntentChange?.Invoke(this, _intent);

            Debug.Log($"Selected intent is {_intent}");
        }
    }
}
