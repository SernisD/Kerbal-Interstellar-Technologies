using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalInterstellarTechnologies.FuelStorage
{
    /*
    public static class KITCryostatBoiloff
     {

     public static Action<PartResource, double> BoilOffCalculator(IResourceInterface resourceInterface, KITCryostatConfig config)
     {
         return (PartResource resource, double externalTemp) =>
         {
             // config.previousPowerMet etc

             if (cheats.IgnoreMaxTemperature) return;

             if (double.IsNaN(externalTemp) || double.IsInfinity(externalTemp))
             {
                 Debug.Log($"[KITCryostatBoiloff] externalTemp is either NaN or Infinity");
                 // Since we're doing nothing, the power requirements have been met.
                 return;
             }

             // Empty tanks don't need cooling
             if (resource.amount < 0.0000001) return;

             var atmosphereModifier = 1; // var atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + part.atmDensity / (convectionMod + 1);

             var temperatureModifier = Math.Max(0, externalTemp - config.boilOffTemp) / 300; //273.15;
             var environmentFactor = atmosphereModifier * temperatureModifier;

             var currentPowerReq = config.powerReqKW * 0.2 * environmentFactor * config.powerReqMult;

             double consumed = cheats.InfiniteElectricity ? currentPowerReq : 0;

             if (consumed < currentPowerReq)
             {
                 consumed += resourceInterface.Consume("ElectricCharge", currentPowerReq);
             }

             // and other stuff.

         };
     }
     */


public class KITCryostat : PartModule
{
        private bool kerbalismDetected;
        private static string kerbalismName = "Cryostat";

        [KSPField]
        public bool previousPowerMet = true;

        [KSPField]
        public string resourceName = "";
        [KSPField]
        public string resourceGUIName = "";
        [KSPField]
        public double boilOffRate = 0;
        [KSPField]
        public double powerReqKW = 0;
        [KSPField]
        public double powerReqMult = 1;
        [KSPField]
        public double boilOffMultiplier = 0;
        [KSPField]
        public double boilOffBase = 10000;
        [KSPField]
        public double boilOffAddition = 0;
        [KSPField]
        public double boilOffTemp = 20.271;
        [KSPField]
        public double convectionMod = 1;

        private Action<PartResource, double> BoilOffCalculator;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor) return;

            /*
            BoilOffCalculator = KITCryostatBoiloff.BoilOffCalculator(
                new StockResourceInterface(part),
                new KITCryostatConfig(this),
                RealCheatOptions.Instance()
            ); */
        }

        private bool recentlyConfigured;

        private bool ReconfigureModule()
        {
            resourceName = "";

            var mods = part.FindModulesImplementing<KITCryostat>();
            mods.Remove(this);

            var rootConfigNodes = GameDatabase.Instance.GetConfigNodes("KIT_CRYOSTAT_CONFIG");
            if (rootConfigNodes == null || rootConfigNodes.Length == 0)
            {
                Debug.Log("[Cryostat] ReconfigureModule - Can't GetConfigNodes(KIT_CRYOSTAT_CONFIG)");
                return false;
            }
            var configNode = rootConfigNodes[0];

            var resourceNode = new ConfigNode();
            var found = false;

            foreach (PartResource resource in part.Resources)
            {
                var resource_already_configured = mods.Find(item => item.resourceName == resource.resourceName) != null;
                if (resource_already_configured)
                {
                    if (recentlyConfigured == false) Debug.Log($"[ReconfigureModule] Ignoring already configured resource {resource.resourceName}");
                    continue;
                }

                resourceNode.ClearData();
                if (configNode.TryGetNode(resource.resourceName, ref resourceNode) == true)
                {
                    found = true;
                    break;
                }

                if (recentlyConfigured == false) Debug.Log($"[ReconfigureModule] Could not find a ConfigNode for {resource.resourceName}, checking next resource");
            }

            recentlyConfigured = true;
            if (!found) return false;

            recentlyConfigured = false;

            Debug.Log($"[ReconfigureModule] Found resource that can be configured -> {resourceNode.name}, configuring..");

            if (false == resourceNode.TryGetValue("resourceGUIName", ref this.resourceGUIName))
            {
                Debug.Log("  [ReconfigureModule] node.TryGetValue(resourceGUIName) failed");
                return false;
            }

            if (false == resourceNode.TryGetValue("boilOffRate", ref this.boilOffRate))
            {
                Debug.Log("  [ReconfigureModule] node.TryGetValue(boilOffRate) failed");
                return false;
            }

            if (false == resourceNode.TryGetValue("boilOffTemp", ref this.boilOffTemp))
            {
                Debug.Log("  [ReconfigureModule] node.TryGetValue(boilOffTemp) failed");
                return false;
            }

            if (false == resourceNode.TryGetValue("boilOffMultiplier", ref this.boilOffMultiplier))
            {
                Debug.Log("  [ReconfigureModule] node.TryGetValue(boilOffMultiplier) failed");
                return false;
            }

            if (false == resourceNode.TryGetValue("boilOffBase", ref this.boilOffBase))
            {
                Debug.Log("  [ReconfigureModule] node.TryGetValue(boilOffBase) failed");
                return false;
            }

            if (false == resourceNode.TryGetValue("boilOffAddition", ref this.boilOffAddition))
            {
                Debug.Log("  [ReconfigureModule] node.TryGetValue(boilOffAddition) failed");
                return false;
            }

            this.resourceName = resourceNode.name;
            return true;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if(part.Resources[resourceName] == null)
            {
                if (ReconfigureModule() == false)
                {
                    // disable fields
                    return;
                }
            }

            // BoilOffCalculator(part.Resources[resourceName], part.temperature);
        }

        // For Kerbalism background processing
        //   - extract the config values from the ProtoPartModule
        //   - put it in the Config struct
        //   - call the boiloff calculator
        //   - store changes if needed to the proto part module

    }
}
