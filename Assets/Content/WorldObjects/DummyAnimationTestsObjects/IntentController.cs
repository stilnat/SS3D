using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class IntentController : NetworkBehaviour
    {
        public event EventHandler<Intent> OnIntentChange;

        [SyncVar(OnChange = nameof(SyncIntent))]
        private Intent _intent;

        public Intent Intent => _intent;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }
        }

        // Update is called once per frame
        protected void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Space))
            {
                return;
            }

            UpdateIntent();
        }

        [ServerRpc]
        private void UpdateIntent()
        {
            _intent = _intent == Intent.Def ? Intent.Harm : Intent.Def;
        }

        private void SyncIntent(Intent prev, Intent next, bool asServer)
        {
            Debug.Log($"Selected intent of {Owner} is {_intent}");

            if (asServer)
            {
                return;
            }

            OnIntentChange?.Invoke(this, _intent);
        }
    }
}
