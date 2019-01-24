﻿using System;
using System.Collections;
using UniModule.UnityTools.Interfaces;
using UniModule.UnityTools.UniRoutine;
using UniModule.UnityTools.UniStateMachine.Interfaces;

namespace UniModule.UnityTools.UniStateMachine.ContextStateMachine
{
    public class ContextStateExecutor : IContextStateExecutor<IEnumerator>
    {


        public IDisposable Execute(IContextState<IEnumerator> state, 
            IContext context)
        {
            var routine = state.Execute(context);
            var dispose = routine.RunWithSubRoutines();
            return dispose;
        }

        public void Dispose()
        {
            
        }
    }
}
