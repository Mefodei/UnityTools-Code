﻿namespace UniTools.UniRoutine.Runtime
{
    using System.Collections;
    using UniModule.UnityTools.Interfaces;

    public static class UniRoutineExtension {

        public static IDisposableItem RunWithSubRoutines(this IEnumerator enumerator, 
            RoutineType routineType = RoutineType.UpdateStep)
        {
		
            return UniRoutineController.RunWithSubRoutines(enumerator,routineType);
		
            
        }
        
        
	
    }
}
