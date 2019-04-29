﻿using System.Collections;
using UniModule.UnityTools.ProfilerTools;
using UnityEngine;

namespace UniModule.UnityTools.UniStateMachine
{
    public class DummyStateBehaviour : StateBehaviour
    {
        
        protected override IEnumerator ExecuteState()
        {
            GameLog.Log("IM DUMMY STATE",Color.cyan);
            while (IsActive)
            {
                yield return null;
            }
        }


    }
}