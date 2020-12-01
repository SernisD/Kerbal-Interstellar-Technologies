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
        private static bool initialized; 

        /// <summary>
        /// Initializes the VesselEventData class, and hooks into the GameEvents code.
        /// </summary>
        static void initialize()
        {
            if (!initialized && HighLogic.LoadedSceneIsGame | HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselGoOnRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselPartCountChanged.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselGoOffRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselLoaded.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onPartDestroyed.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                GameEvents.onPartPriorityChanged.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                GameEvents.onPartDie.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                GameEvents.onPartDeCouple.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                // GameEvents.
                initialized = true;
            }
        }

        /// <summary>
        /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Part triggering the event</param>
        private static void refreshActiveParts(Part data)
        {
            if (data == null || data.vessel == null) return;
            var resourceMod = data.vessel.FindVesselModuleImplementing<KITResourceVesselModule>();
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
            var resourceMod = data.FindVesselModuleImplementing<KITResourceVesselModule>();
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }

        /// <summary>
        /// Dummy func to ensure the class is initialized.
        /// </summary>
        /// <returns>true</returns>
        public static bool Ready()
        {
            if(initialized == false) initialize();
            return initialized;
        }
    }

    /// <summary>
    /// KITResourceManager implements the Resource Manager code for Kerbal Interstellar Technologies.
    /// 
    /// <para>It acts as a scheduler for the KIT Part Modules (denoted by the IKITMod interface), calling them by their self reported priority. This occurs 
    /// every FixedUpdate(), and the IKITMods do not implemented their own FixedUpdate() interface.</para>
    /// 
    /// <para>It also manages what resources are available each FixedUpdate() tick for the modules, and once all modules have ran, it finalizes the reults. This eliminates the need for resource buffering implementations.</para>
    /// </summary>
    public class KITResourceVesselModule : VesselModule, IVesselResources
    {
        public bool refreshEventOccurred = true;

        [KSPField] double lastExecuted;
        [KSPField] bool catchUpNeeded;

        private double fixedDeltaTime;

        ResourceManager resourceManager;
        IResourceScheduler resourceScheduler;

        private bool needsRefresh
        {
            get { return catchUpNeeded || refreshEventOccurred; }
            set { refreshEventOccurred = value; }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (resourceManager == null)
            {
                resourceManager = new ResourceManager(this, RealCheatOptions.Instance);
                resourceScheduler = resourceManager;
            }

        }

        private SortedDictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>> variableSupplierModules = new SortedDictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>>();

        Dictionary<ResourceName, double> resourceAmounts = new Dictionary<ResourceName, double>(32);
        Dictionary<ResourceName, double> resourceMaxAmounts = new Dictionary<ResourceName, double>(32);

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

            if (lastExecuted == 0) catchUpNeeded = false;
            double currentTime = Planetarium.GetUniversalTime();
            var deltaTime = lastExecuted - currentTime;
            lastExecuted = currentTime;

            GatherResources(ref resourceAmounts, ref resourceMaxAmounts);

            if (catchUpNeeded)
            {
                resourceScheduler.ExecuteKITModules(deltaTime, ref resourceAmounts, ref resourceMaxAmounts);
                catchUpNeeded = false;
            }

            resourceScheduler.ExecuteKITModules(TimeWarp.fixedDeltaTime, ref resourceAmounts, ref resourceMaxAmounts);
            DisperseResources(ref resourceAmounts);
        }

        bool IVesselResources.VesselModified()
        {
            bool ret = catchUpNeeded | refreshEventOccurred;
            refreshEventOccurred = false;

            return ret;
        }

        SortedDictionary<ResourcePriorityValue, List<IKITMod>> sortedModules = new SortedDictionary<ResourcePriorityValue, List<IKITMod>>();
        Dictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>> tmpVariableSupplierModules = new Dictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>>();

        void IVesselResources.VesselKITModules(ref List<IKITMod> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules)
        {
            // Clear the inputs

            moduleList.Clear();
            variableSupplierModules.Clear();

            // Clear the temporary variables

            sortedModules.Clear();
            tmpVariableSupplierModules.Clear();

            List<IKITMod> KITMods;

            bool hasKITModules;

            var kitlist = vessel.FindPartModulesImplementing<IKITMod>();
            foreach (var mod in kitlist)
            {
                // Handle the KITFixedUpdate() side of things first.

                var priority = mod.ResourceProcessPriority();
                var prepend = (priority & ResourcePriorityValue.SupplierOnlyFlag) == ResourcePriorityValue.SupplierOnlyFlag;
                priority &= ~ResourcePriorityValue.SupplierOnlyFlag;

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

                // Now handle the variable consumption side of things

                var supmod = mod as IKITVariableSupplier;
                if (supmod == null) continue;

                foreach (ResourceName resource in supmod.ResourcesProvided())
                {
                    if (tmpVariableSupplierModules.ContainsKey(resource) == false)
                    {
                        tmpVariableSupplierModules[resource] = new SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>();
                    }
                    var modules = tmpVariableSupplierModules[resource];

                    if (modules.ContainsKey(priority) == false)
                    {
                        modules[priority] = new List<IKITVariableSupplier>(16);
                    }

                    if (prepend)
                    {
                        modules[priority].Prepend(supmod);
                    }
                    else
                    {
                        modules[priority].Append(supmod);
                    }

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
                moduleList.AddRange(list);
            }

            foreach (var resource in tmpVariableSupplierModules.Keys)
            {
                variableSupplierModules[resource] = new List<IKITVariableSupplier>(16);

                foreach(var list in tmpVariableSupplierModules[resource].Values)
                {
                    variableSupplierModules[resource].AddRange(list);
                }
            }
        }

        void GatherResources(ref Dictionary<ResourceName, double> amounts, ref Dictionary<ResourceName, double> maxAmounts)
        {
            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.maxAmount == 0) continue;

                    var resourceID = ResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        Debug.Log($"[KITResourceManager.GatherResources] ignoring unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (amounts.ContainsKey(resourceID) == false)
                    {
                        amounts[resourceID] = maxAmounts[resourceID] = 0;
                    }

                    amounts[resourceID] += resource.amount;
                    maxAmounts[resourceID] += resource.maxAmount;
                }
            }
        }

        void DisperseResources(ref Dictionary<ResourceName, double> available)
        {
            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources)
                {
                    var resourceID = ResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        Debug.Log($"[KITResourceManager.DisperseResources] ignoring unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (available.ContainsKey(resourceID) == false) return; // Shouldn't happen
                    if (available[resourceID] == 0) return;

                    var tmp = Math.Min(available[resourceID], resource.maxAmount - resource.amount);
                    available[resourceID] -= tmp;
                    resource.amount = tmp;
                }
            }
        }

        public void OnKITProcessingFinished(IResourceManager resourceManager)
        {
            // 
        }
    }

    class ResourceDummy : PartModule, IKITMod
    {
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "power priority level", isPersistant = true), UI_FloatRange(minValue = 1, maxValue = 5, stepIncrement = 1)]
       public float resourcePriority = 1;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Destroy"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool destroyInUpdate = false;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Supplier Only"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool SupplierOnly = false;

        int explosionCountdown = 5;

        public void KITFixedUpdate(double deltaTime)
        {
            if (destroyInUpdate)
            {
                Debug.Log("$[KITResourceUpdate] ready set go!");
                if (explosionCountdown-- == 0) { part.explode(); }
            }
        }

        public ResourcePriorityValue ResourceProcessPriority() => (ResourcePriorityValue)(resourcePriority) | ResourcePriorityValue.SupplierOnlyFlag;

        public string KITPartName() => "Resource Dummy";

        public void KITFixedUpdate(IResourceManager resMan)
        {
            throw new NotImplementedException();
        }
    }
}

