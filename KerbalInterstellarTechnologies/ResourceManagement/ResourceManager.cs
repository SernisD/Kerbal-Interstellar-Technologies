﻿using KerbalInterstellarTechnologies.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalInterstellarTechnologies.ResourceManagement
{
    public class ResourceManager : IResourceManager, IResourceScheduler
    {
        private ICheatOptions myCheatOptions = RealCheatOptions.Instance;
        private IVesselResources vesselResources;
        private static double fudgeFactor = 0.9999;
        private Dictionary<ResourceName, double> currentResources;

        private double fixedDeltaTime;

        HashSet<IKITMod> fixedUpdateCalledMods = new HashSet<IKITMod>(128);
        HashSet<IKITMod> modsCurrentlyRunning = new HashSet<IKITMod>(128);

        public bool UseThisToHelpWithTesting;

        public ResourceManager(IVesselResources vesselResources, ICheatOptions cheatOptions)
        {
            this.vesselResources = vesselResources;
            this.myCheatOptions = cheatOptions;
        }
        ICheatOptions IResourceManager.CheatOptions() => myCheatOptions;

        private bool inExecuteKITModules;

        /// <summary>
        /// Called by the IKITMod to consume resources present on a vessel. It automatically converts the wanted amount by the appropriate value to
        /// give you a per-second resource consumption.
        /// </summary>
        /// <param name="resource">Resource to consume</param>
        /// <param name="wanted">How much you want</param>
        /// <returns>How much you got</returns>
        double IResourceManager.ConsumeResource(ResourceName resource, double wanted)
        {
            ResourceSettings.ValidateResource(resource);

            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ConsumeResource] don't do this.");
                return 0;
            }
            if (myCheatOptions.InfiniteElectricity && resource == ResourceName.ElectricCharge) return wanted;

            if (currentResources.ContainsKey(resource) == false)
            {
                currentResources[resource] = 0;
            }

            double obtainedAmount = 0;
            double modifiedAmount = wanted * fixedDeltaTime;

            var tmp = Math.Min(currentResources[resource], modifiedAmount);
            obtainedAmount += tmp;
            currentResources[resource] -= tmp;
            if (obtainedAmount >= modifiedAmount) return wanted;

            obtainedAmount = CallVariableSuppliers(resource, wanted, obtainedAmount, modifiedAmount);

            //return obtainedAmount;

            // is it close enough to being fully requested? (accounting for precision issues)
            if (modifiedAmount * fudgeFactor <= obtainedAmount) return wanted;

            return wanted * (obtainedAmount / modifiedAmount);
        }

        double IResourceManager.FixedDeltaTime() => fixedDeltaTime;

        void RefreshActiveModules()
        {
            vesselResources.VesselKITModules(ref activeKITModules, ref variableSupplierModules);
        }

        /// <summary>
        /// Called by the IKITMod to produce resources on a vessel.It automatically converts the amount by the appropriate value to
        /// give a per-second resource production.
        /// </summary>
        /// <param name="resource">Resource to produce</param>
        /// <param name="amount">Amount you are providing</param>
        void IResourceManager.ProduceResource(ResourceName resource, double amount)
        {
            ResourceSettings.ValidateResource(resource);

            //Debug.Log($"ProduceResource {resource} - {amount}");

            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ProduceResource] don't do this.");
                return;
            }

            if (resource == ResourceName.WasteHeat && myCheatOptions.IgnoreMaxTemperature) return;

            if (currentResources.ContainsKey(resource) == false)
            {
                currentResources[resource] = 0;
            }
            currentResources[resource] += amount * fixedDeltaTime;
        }

        // private SortedDictionary<ResourcePriorityValue, List<IKITMod>> sortedModules = new SortedDictionary<ResourcePriorityValue, List<IKITMod>>();
        private List<IKITMod> activeKITModules = new List<IKITMod>(128);

        private Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules = new Dictionary<ResourceName, List<IKITVariableSupplier>>();

        private bool complainedToWaiterAboutOrder;

        /// <summary>
        /// ExecuteKITModules() does the heavy work of executing all the IKITMod FixedUpdate() equiv. It needs to be careful to ensure
        /// it is using the most recent list of modules, hence the odd looping code. In the case of no part updates are needed, it's
        /// relatively optimal.
        /// </summary>
        /// <param name="deltaTime">the amount of delta time that each module should use</param>
        /// <param name="resourcesAvailable">What resources are available for this call.</param>
        void IResourceScheduler.ExecuteKITModules(double deltaTime, ref Dictionary<ResourceName, double> resourcesAvailable)
        {
            int index = 0;

            currentResources = resourcesAvailable;
            
            tappedOutMods.Clear();
            fixedUpdateCalledMods.Clear();

            if(modsCurrentlyRunning.Count > 0 && complainedToWaiterAboutOrder == false)
            {
                Debug.Log($"[ResourceManager.ExecuteKITModules] modsCurrentlyRunning.Count != 0. there may be resource production / consumption issues.");
                complainedToWaiterAboutOrder = true;
                modsCurrentlyRunning.Clear();
            }

            if (vesselResources.VesselModified())
            {
                RefreshActiveModules();
                if (activeKITModules.Count == 0) return;
            }

            inExecuteKITModules = true;

            fixedDeltaTime = deltaTime;

            while (index != activeKITModules.Count)
            {
                var mod = activeKITModules[index];
                index++;

                if (fixedUpdateCalledMods.Contains(mod)) continue;
                fixedUpdateCalledMods.Add(mod);

                if (modsCurrentlyRunning.Contains(mod))
                {
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] This module {mod.KITPartName()} should not be marked busy at this stage");
                    continue;
                }

                modsCurrentlyRunning.Add(mod);

                if (UseThisToHelpWithTesting)
                {
                    mod.KITFixedUpdate(this);
                }
                else
                {
                    try
                    {
                        mod.KITFixedUpdate(this);
                    }
                    catch (Exception ex)
                    {
                        // XXX - part names and all that.
                        Debug.Log($"[KITResourceManager.ExecuteKITModules] Exception when processing [{mod.KITPartName()}, {(mod as PartModule).ClassName}]: {ex.ToString()}");
                    }
                }

                if (vesselResources.VesselModified())
                {
                    index = 0;
                    RefreshActiveModules();
                }

                modsCurrentlyRunning.Remove(mod);
            }

            currentResources = null;
            inExecuteKITModules = false;
        }

        HashSet<IKITVariableSupplier> tappedOutMods = new HashSet<IKITVariableSupplier>(128);

        private double CallVariableSuppliers(ResourceName resource, double originalAmount, double obtainedAmount, double modifiedAmount)
        {
            if (variableSupplierModules.ContainsKey(resource) == false) return 0;

            foreach (var mod in variableSupplierModules[resource])
            {
                var KITMod = mod as IKITMod;

                if (tappedOutMods.Contains(mod)) continue; // it's tapped out for this cycle.
                if (modsCurrentlyRunning.Contains(KITMod)) continue;

                modsCurrentlyRunning.Add(KITMod);

                if (fixedUpdateCalledMods.Contains(KITMod) == false)
                {
                    // Hasn't had it's KITFixedUpdate() yet? call that first.
                    fixedUpdateCalledMods.Add(KITMod);
                    KITMod.KITFixedUpdate(this);
                }

                double perSecondAmount = originalAmount * (1 - (obtainedAmount / modifiedAmount));

                try
                {
                    var canContinue = mod.ProvideResource(this, resource, perSecondAmount);
                    if (!canContinue) tappedOutMods.Add(mod);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[KITResourceManager.callVariableSuppliers] calling KITMod {KITMod.KITPartName()} resulted in {ex.ToString()}");
                }

                var tmp = Math.Min(currentResources[resource], (modifiedAmount - obtainedAmount));
                currentResources[resource] -= tmp;
                obtainedAmount += tmp;

                modsCurrentlyRunning.Remove(KITMod);

                if (obtainedAmount >= modifiedAmount) return modifiedAmount;
            }

            return obtainedAmount;
        }
    }
}
