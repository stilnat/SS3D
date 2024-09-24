using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Interactions
{
    /// <summary>
    /// Basically a copy of HumanoidBodyPartTargetSelector.cs
    /// This manages intent and it's done to easily support other intents
    /// </summary>
    public class IntentController : NetworkActor
    {
        public event EventHandler<IntentType> OnIntentChange;

        [SyncVar(OnChange = nameof(SyncIntent))]
        private IntentType _intent;

        public IntentType Intent => _intent;

        private Image _intentImage;

        private Sprite _spriteHelp;
        private Sprite _spriteHarm;

        private Color _colorHarm;
        private Color _colorHelp;

        private Button _intentButton;

        protected override void OnStart()
        {
            base.OnStart();

           // _intentButton = GetComponent<Button>();
           // _intentButton.onClick.AddListener(HandleIntentButtonPressed);
        }


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
            //UpdateIntentUI();
        }

        [ServerRpc]
        private void UpdateIntent()
        {
            _intent = _intent == IntentType.Help ? IntentType.Harm : IntentType.Help;
        }

        private void SyncIntent(IntentType prev, IntentType next, bool asServer)
        {
            Debug.Log($"Selected intent of {Owner} is {_intent}");

            if (asServer)
            {
                return;
            }

            OnIntentChange?.Invoke(this, _intent);
        }


        public void HandleIntentButtonPressed()
        {
            UpdateIntent();
            //UpdateIntentUI();
        }

        /// <summary>
        /// Switches between Help and Harm intent
        /// </summary>
        public void UpdateIntentUI()
        {
            _intentImage.sprite = _intent == IntentType.Help ? _spriteHelp : _spriteHarm;
            _intentImage.color = _intent == IntentType.Help ? _colorHelp : _colorHarm;
        }
    }
}
