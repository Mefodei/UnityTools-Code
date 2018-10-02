﻿using System.Collections;
using Modules.UnityToolsModule.Tools.UnityTools.Interfaces;
using UniStateMachine;
using UnityEngine;

namespace GamePlay.States {
    
    [CreateAssetMenu(menuName = "States/States/SetAnimationState", fileName = "SetAnimationState")]
    public class SetAnimationState : UniStateBehaviour {

        #region inspector data
        
        [SerializeField]
        private string _animation;

        #endregion
        
        private int _animationId;

        protected override IEnumerator ExecuteState(IContextProvider context) {

            var animator = context.GetContext<Animator>();
            _animationId = Animator.StringToHash(_animation);
            
            animator.Play(_animationId);
            yield break;

        }

    }

}
