using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalInterstellarTechnologies.ResourceManagement
{
    /*
     * What is this for?
     * When to use it?
     */

    public static class VesselEventData
    {
        static VesselEventData() {
            GameEvents.onVesselGoOnRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onPartDestroyed.Add(new EventData<Part>.OnEvent(refreshActiveParts));
            GameEvents.onPartPriorityChanged.Add(new EventData<Part>.OnEvent(refreshActiveParts));
            GameEvents.onVesselPartCountChanged.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
        }

        public static bool RefreshNeeded { get; set; } = true;

        private static void refreshActiveParts(Part data)
        {
            RefreshNeeded = true;
        }
        private static void refreshActiveParts(Vessel data)
        {
            RefreshNeeded = true;
        }
    }

    public class KITResourceManager : VesselModule
    {
        public static int SupplierOnlyFlag = 0x80;

        private bool hasKITModules;
        private bool initialized;

        [KSPField] double lastExecuted;
        [KSPField] bool catchUpNeeded;

        private bool needsRefresh
        {
            get { return !initialized || catchUpNeeded || VesselEventData.RefreshNeeded; }
            set { initialized = true; VesselEventData.RefreshNeeded = value; }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            Debug.Log($"[KITResourceManager] I'm awake now.. on {vessel.vesselName}");
            refreshActiveModules();
        }

        private SortedDictionary<int, List<IKITMod>> sortedModules = new SortedDictionary<int, List<IKITMod>>();
        private List<IKITMod> activeKITModules = new List<IKITMod>(128);

        private void refreshActiveModules()
        {
            sortedModules.Clear();
            activeKITModules.Clear();

            if (vessel == null || vessel.parts == null) return;
            Debug.Log($"[{this.GetType().Name} and {vessel.vesselName}] refreshParts - refreshing!");

            List<IKITMod> KITMods;

            var kitlist = vessel.FindPartModulesImplementing<IKITMod>();
            foreach(var mod in kitlist)
            {
                var priority = mod.ResourceProcessPriority();
                var prepend = (priority & SupplierOnlyFlag) == SupplierOnlyFlag;
                priority &= ~SupplierOnlyFlag;

                if (sortedModules.TryGetValue(priority, out KITMods) == false)
                {
                    sortedModules[priority] = new List<IKITMod>(32);
                }

                if(prepend)
                {
                    sortedModules[priority].Prepend(mod);
                }
                else
                {
                    sortedModules[priority].Append(mod);
                }
            }

            if(sortedModules.Count() == 0)
            {
                // Nothing found
                hasKITModules = needsRefresh = false;
                return;
            }

            hasKITModules = true;
            needsRefresh = false;

            foreach(List<IKITMod> list in sortedModules.Values)
            {
                activeKITModules.AddRange(list);
            }
        }

        List<IKITMod> visited = new List<IKITMod>(128);
        private void ExecuteKITModules(double deltaTime)
        {
            bool checkVisited = false;
            int index = 0;

            visited.Clear();

            while(index != activeKITModules.Count)
            {
                var mod = activeKITModules[index];
                index++;

                if(checkVisited) if (visited.Contains(mod)) continue;
                visited.Add(mod);

                try
                {
                    mod.KITFixedUpdate(deltaTime);
                }
                catch(Exception ex)
                {
                    // XXX - part names and all that.
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] Exception when processing [XXX NAME HERE]: {ex.ToString()}");
                }

                if(needsRefresh)
                {
                    index = 0;
                    checkVisited = true;
                    refreshActiveModules();
                }
            }

        }

        public void FixedUpdate()
        {
            if (!vessel.loaded)
            {
                catchUpNeeded = true;
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight) return;

            var deltaTime = lastExecuted - Planetarium.GetUniversalTime();
            lastExecuted = Planetarium.GetUniversalTime();

            if (needsRefresh) refreshActiveModules();
            if (hasKITModules == false) return;

            if(catchUpNeeded)
            {
                Debug.Log($"[KITResourceManager] catching up with a delta of {deltaTime}");

                ExecuteKITModules(deltaTime);

                catchUpNeeded = false;
            }

            ExecuteKITModules(TimeWarp.fixedDeltaTime);
        }

    }

    class ResourceDummy : PartModule, IKITMod
    {
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "power priority level", isPersistant = true), UI_FloatRange(minValue = 1, maxValue = 5, stepIncrement = 1)]
        public float resourcePriority = 1;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Destroy"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool destroyInUpdate = false;

        int explosionCountdown = 20;

        public void KITFixedUpdate(double deltaTime)
        {
            if (destroyInUpdate)
            {
                Debug.Log("$[KITResourceUpdate] ready set go!");
                if (explosionCountdown-- == 0) { part.explode(); }
            }
        }

        public int ResourceProcessPriority() => ((int)resourcePriority & 1) == 1 ? (int)resourcePriority : KITResourceManager.SupplierOnlyFlag | (int)resourcePriority;

        public string KITPartName() => "Resource Dummy";
    }

}
