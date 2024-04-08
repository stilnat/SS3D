using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntentController : MonoBehaviour
{
    private Intent _intent;

    public Intent Intent => _intent;

    public event EventHandler<Intent> OnIntentChange;
    
    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        _intent = _intent == Intent.Def ? Intent.Harm : Intent.Def; 
        
        OnIntentChange?.Invoke(this, _intent);
        
        Debug.Log($"Selected intent is {_intent}");
    }
}
