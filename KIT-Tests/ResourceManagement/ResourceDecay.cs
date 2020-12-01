using KerbalInterstellarTechnologies;
using KerbalInterstellarTechnologies.ResourceManagement;
using KerbalInterstellarTechnologies.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT_Tests.ResourceManagement
{

    public class TestResourceManager : IResourceManager
    {
        ICheatOptions cheatOptions = RealCheatOptions.Instance;
        double fixedDeltaTime = 0.2;
        public Dictionary<ResourceName, double> resourceAmount = new Dictionary<ResourceName, double>();
        public Dictionary<ResourceName, double> resourceMaxAmount = new Dictionary<ResourceName, double>();

        public TestResourceManager(ICheatOptions cheatOptions, double fixedDeltaTime)
        {
            this.cheatOptions = cheatOptions;
            this.fixedDeltaTime = fixedDeltaTime;
        }

        public ICheatOptions CheatOptions() => this.cheatOptions;

        public double ConsumeResource(ResourceName resource, double wanted)
        {
            if (resourceAmount.ContainsKey(resource) == false)
            {
                return 0;
            }

            var modified = wanted * fixedDeltaTime;

            var tmp = Math.Min(modified, resourceAmount[resource]);
            resourceAmount[resource] -= tmp;

            return wanted * (tmp / modified);
        }

        public double FixedDeltaTime() => fixedDeltaTime;

        public void ProduceResource(ResourceName resource, double amount)
        {
            if (resourceAmount.ContainsKey(resource) == false)
            {
                resourceAmount[resource] = 0;
                resourceMaxAmount[resource] = 1000;
            }
            resourceAmount[resource] += amount * fixedDeltaTime;
        }
    }

    //public static List<string> PerformResourceDecayEffect(IResourceManager resourceManager, List<PartResource[]> partResources, Dictionary<String, DecayConfiguration> decayConfiguration)

    [TestClass]
    public class TestResourceDecay
    {
        [TestMethod]
        public void TestOneForOneConversion()
        {
            var trm = new TestResourceManager(RealCheatOptions.Instance, 1);

            var pr = new PartResource(new Part());
            pr.resourceName = "LiquidFuel";
            pr.amount = 1;
            pr.maxAmount = 1;

            List<PartResource[]> prs = new List<PartResource[]>();
            prs.Add(new PartResource[] { pr });

            var config = new DecayConfiguration();
            config.decayConstant = 1;
            config.decayProduct = ResourceName.MonoPropellant;
            config.densityRatio = 1;
            config.decayRatio = 1;

            var configdict = new Dictionary<string, DecayConfiguration>();
            configdict["LiquidFuel"] = config;

            var ret = KITResourceVesselModule.PerformResourceDecayEffect(trm, prs, configdict);

            Assert.IsTrue(ret.Count == 0, $"knew all inputs failed - {ret.Count}");
            Assert.IsTrue(trm.resourceAmount.ContainsKey(ResourceName.MonoPropellant), "Monopropellant not found");
            var equal = trm.resourceAmount[ResourceName.MonoPropellant] + pr.amount;
            Assert.IsTrue(equal == 1, $"not equal.. {equal}, {pr.amount}, {trm.resourceAmount[ResourceName.MonoPropellant]}");

            trm.resourceAmount.Clear();
            pr.amount = 1;
            config.densityRatio = 1.5;
            configdict["LiquidFuel"] = config;

            ret = KITResourceVesselModule.PerformResourceDecayEffect(trm, prs, configdict);
            Assert.IsTrue(trm.resourceAmount.ContainsKey(ResourceName.MonoPropellant), "Monopropellant not found");
            equal = trm.resourceAmount[ResourceName.MonoPropellant] + pr.amount;
            Assert.IsTrue(1.32 == Math.Round(equal, 2), $"not equal {Math.Round(equal, 2)}.. {equal}, {pr.amount}, {trm.resourceAmount[ResourceName.MonoPropellant]}");
        }
    }
}
