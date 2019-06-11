﻿using System.Collections;
using UniModule.UnityTools.Interfaces;
using UniModule.UnityTools.ResourceSystem;
using UniModule.UnityTools.UniStateMachine.Extensions;
using UnityEngine;
using UniNodeSystem;

namespace UniStateMachine.CommonNodes
{
    using UniGreenModules.UniCore.Runtime.Interfaces;
    using UniGreenModules.UniCore.Runtime.ObjectPool;

    public class GameObjectNode : UniNode
    {
        private string _optionsPortName;

        public ResourceItem Asset;

        public bool CreateInstance;
       
        public ObjectInstanceData Options;
                       
        //public UnityEvent Action;

        public override string GetName()
        {
            var assetName = Asset.ItemName;
            return string.IsNullOrEmpty(assetName) ? name : assetName;
        }

        protected override IEnumerator OnExecuteState(IContext context)
        {
            var target = CreateTarget(context);

            var lifeTime = LifeTime;
            lifeTime.AddCleanUpAction(() => { RemoveTarget(target, context); });
            
            return base.OnExecuteState(context);
        }

        protected override void OnUpdatePortsCache()
        {
            _optionsPortName = nameof(Options);
            //option input values port
            this.UpdatePortValue(_optionsPortName, PortIO.Input);
            base.OnUpdatePortsCache();
        }

        private GameObject CreateTarget(IContext context)
        {
            var assetTarget = Asset.Load<GameObject>();
            
            if (!CreateInstance) return assetTarget;
            if (!assetTarget) return assetTarget;

            var optionsValue = GetPortValue(_optionsPortName);
            var options = optionsValue.Contains<ObjectInstanceData>() ? 
                optionsValue.Get<ObjectInstanceData>():
                Options;
            
            var target = ObjectPool.Spawn(assetTarget,options.Position,
                Quaternion.identity,options.Parent,
                options.StayAtWorld);
            
            if (options.Immortal)
            {
                DontDestroyOnLoad(target);
            }

            return target;
            
        }

        private void RemoveTarget(GameObject target,IContext context)
        {

            if (!CreateInstance || !target) return;
            
            ObjectPool.Despawn(target);

        }
        
        
    }

}

