using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using KerbalInterstellarTechnologies.Settings;

namespace KerbalInterstellarTechnologies.ResourceManagement
{
    /// <summary>
    /// VesselEventData is used to track game events occurring. Once it picks up an event occurring, it tries to
    /// find the corresponding KITResourceManager and lets it know to refresh it's part module cache.
    /// </summary>
    public static class VesselEventData
    {
        /// <summary>
        /// Initializes the VesselEventData class, and hooks into the GameEvents code.
        /// </summary>
        static VesselEventData() {
            GameEvents.onVesselGoOnRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onVesselPartCountChanged.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onVesselGoOffRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onVesselLoaded.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
            GameEvents.onPartDestroyed.Add(new EventData<Part>.OnEvent(refreshActiveParts));
            GameEvents.onPartPriorityChanged.Add(new EventData<Part>.OnEvent(refreshActiveParts));
            GameEvents.onPartDie.Add(new EventData<Part>.OnEvent(refreshActiveParts));
            
        }

        /// <summary>
        /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Part triggering the event</param>
        private static void refreshActiveParts(Part data)
        {
            if (data == null || data.vessel == null) return;
            var resourceMod = data.vessel.FindVesselModuleImplementing<KITResourceManager>();
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }
        /// <summary>
        /// /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Vessel triggering the event</param>
        private static void refreshActiveParts(Vessel data)
        {
            if (data == null) return;
            var resourceMod = data.FindVesselModuleImplementing<KITResourceManager>();
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }

        /// <summary>
        /// Dummy func to ensure the class is initialized.
        /// </summary>
        /// <returns>true</returns>
        public static bool Ready() => true;
    }

    /// <summary>
    /// KITResourceManager implements the Resource Manager code for Kerbal Interstellar Technologies.
    /// 
    /// <para>It acts as a scheduler for the KIT Part Modules (denoted by the IKITMod interface), calling them by their self reported priority. This occurs 
    /// every FixedUpdate(), and the IKITMods do not implemented their own FixedUpdate() interface.</para>
    /// 
    /// <para>It also manages what resources are available each FixedUpdate() tick for the modules, and once all modules have ran, it finalizes the reults. This eliminates the need for resource buffering implementations.</para>
    /// </summary>
    public class KITResourceManager : VesselModule, IResourceManager
    {
        public static int SupplierOnlyFlag = 0x80;

        public bool refreshEventOccurred;

        private bool hasKITModules;
        private bool initialized;

        [KSPField] double lastExecuted;
        [KSPField] bool catchUpNeeded;

        private double fixedDeltaTime;
        public ICheatOptions myCheatOptions = RealCheatOptions.Instance;

        private bool needsRefresh
        {
            get { return !initialized || catchUpNeeded || refreshEventOccurred; }
            set { initialized = true; refreshEventOccurred = value; }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            VesselEventData.Ready(); // ensure the static class gets initialized
            refreshActiveModules();
        }

        private SortedDictionary<int, List<IKITMod>> sortedModules = new SortedDictionary<int, List<IKITMod>>();
        private List<IKITMod> activeKITModules = new List<IKITMod>(128);

        /// <summary>
        /// refreshActiveModules() .. refreshes the list of active IKITMod modules. (pretty surprising).
        /// </summary>
        private void refreshActiveModules()
        {
            sortedModules.Clear();
            activeKITModules.Clear();

            if (vessel == null || vessel.parts == null) return;
            Debug.Log($"[{this.GetType().Name} and {vessel.vesselName}] refreshParts - refreshing!");

            List<IKITMod> KITMods;

            var kitlist = vessel.FindPartModulesImplementing<IKITMod>();
            foreach (var mod in kitlist)
            {
                var priority = mod.ResourceProcessPriority();
                var prepend = (priority & SupplierOnlyFlag) == SupplierOnlyFlag;
                priority &= ~SupplierOnlyFlag;

                if (sortedModules.TryGetValue(priority, out KITMods) == false)
                {
                    sortedModules[priority] = new List<IKITMod>(32);
                }

                if (prepend)
                {
                    sortedModules[priority].Prepend(mod);
                }
                else
                {
                    sortedModules[priority].Append(mod);
                }
            }

            if (sortedModules.Count() == 0)
            {
                // Nothing found
                hasKITModules = needsRefresh = false;
                return;
            }

            hasKITModules = true;
            needsRefresh = false;

            foreach (List<IKITMod> list in sortedModules.Values)
            {
                activeKITModules.AddRange(list);
            }
        }

        List<IKITMod> visited = new List<IKITMod>(128);

        private bool inExecuteKITModules;

        /// <summary>
        /// ExecuteKITModules() does the heavy work of executing all the IKITMod FixedUpdate() equiv. It needs to be careful to ensure
        /// it is using the most recent list of modules, hence the odd looping code. In the case of no part updates are needed, it's
        /// relatively optimal.
        /// </summary>
        /// <param name="deltaTime">the amount of delta time that each module should use</param>
        private void ExecuteKITModules(double deltaTime)
        {
            bool checkVisited = false;
            int index = 0;

            inExecuteKITModules = true;

            fixedDeltaTime = deltaTime;
            visited.Clear();

            while (index != activeKITModules.Count)
            {
                var mod = activeKITModules[index];
                index++;

                if (checkVisited) if (visited.Contains(mod)) continue;
                visited.Add(mod);

                try
                {
                    mod.KITFixedUpdate(this);
                }
                catch (Exception ex)
                {
                    // XXX - part names and all that.
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] Exception when processing [{mod.KITPartName()}, {(mod as PartModule).ClassName}]: {ex.ToString()}");
                }

                if (needsRefresh)
                {
                    index = 0;
                    checkVisited = true;
                    refreshActiveModules();
                }
            }
            inExecuteKITModules = false;
        }

        /// <summary>
        /// FixedUpdate() triggers the ExecuteKITModules() function call above. It implements automatic catch up processing for each module.
        /// </summary>
        public void FixedUpdate()
        {
            if (!vessel.loaded)
            {
                catchUpNeeded = true;
                return;
            }
            
            if (!HighLogic.LoadedSceneIsFlight || vessel.vesselType == VesselType.SpaceObject ||
                vessel.isEVA || vessel.vesselType == VesselType.Debris) return;
            
            var deltaTime = lastExecuted - Planetarium.GetUniversalTime();
            lastExecuted = Planetarium.GetUniversalTime();

            if (needsRefresh) refreshActiveModules();
            if (hasKITModules == false) return;

            if (catchUpNeeded)
            {
                Debug.Log($"[KITResourceManager] catching up with a delta of {deltaTime}");

                ExecuteKITModules(deltaTime);

                catchUpNeeded = false;
            }

            ExecuteKITModules(TimeWarp.fixedDeltaTime);
        }

        /// <summary>
        /// Called by the IKITMod to consume resources present on a vessel. It automatically converts the wanted amount by the appropriate value to
        /// give you a per-second resource consumption.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="wanted">How much you want</param>
        /// <returns>How much you got</returns>
        double IResourceManager.ConsumeResource(string name, double wanted)
        {
            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ConsumeResource] don't do this.");
                return 0;
            }
            if (name == ResourceSettings.ElectricCharge && myCheatOptions.InfiniteElectricity) return wanted;
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by the IKITMod to produce resources on a vessel.It automatically converts the amount by the appropriate value to
        /// give a per-second resource production.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="amount">Amount you are providing</param>
        void IResourceManager.ProduceResource(string name, double amount)
        {
            if (!inExecuteKITModules) return;
            if (name == ResourceSettings.WasteHeat && myCheatOptions.IgnoreMaxTemperature) return;
            throw new NotImplementedException();
        }

        // don't use global variable
        double IResourceManager.FixedDeltaTime() => fixedDeltaTime;

        // Instead of accessing a global variable
        ICheatOptions IResourceManager.CheatOptions() => myCheatOptions;

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

            public void KITFixedUpdate(IResourceManager resMan)
            {
                throw new NotImplementedException();
            }
        }
    }
}
