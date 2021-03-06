﻿namespace UniModules.UniStateMachine.Runtime
{
    using System.Diagnostics;
    using global::UniCore.Runtime.ProfilerTools;
    using UniCore.Runtime.ProfilerTools;
    using UnityEngine;

    public static class StateLogger
    {

        [Conditional("STATE_MACHINE_LOG")]
        public static void LogStateChanged(object stateMachine,object fromState, object toState) {
            if (fromState == toState)
                return;
            GameLog.LogFormatRuntime("STATE CHANGED AT {0} FROM : {1} TO {2}",stateMachine,
                fromState == null ? "NULL" : fromState, toState == null ? "NULL" : toState);
        }

        [Conditional("STATE_MACHINE_LOG")]
        public static void LogState(string message,Object source = null) {

            if (string.IsNullOrEmpty(message))
                return;

            GameLog.LogRuntime(message,source);
            
        }
        
        [Conditional("VALIDATOR_MACHINE_LOG")]
        public static void LogValidator(string message, params object[] values)
        {
            GameLog.LogFormatRuntime(message,values);
        }
        
    }
}