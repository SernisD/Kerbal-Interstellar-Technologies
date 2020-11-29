using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbalInterstellarTechnologies;
using KerbalInterstellarTechnologies.Electrical;
using KerbalInterstellarTechnologies.ResourceManagement;
using KerbalInterstellarTechnologies.Settings;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace KIT_Tests.ResourceManager
{
    [TestClass]
    public class TestVesselResourceManager
    {
        private KITResourceManager Setup()
        {
            var kitrm = new KITResourceManager();
            kitrm.Vessel = new Vessel();
            kitrm.Vessel.vesselName = "Test Vessel";

            kitrm.Vessel.parts = new List<Part>();
            return kitrm;
        }

        private Part RTGPart()
        {
            var ret = new Part();

            var partmod = new KITPlutoniumRTG();
            var partmodlist = new PartModuleList(ret);
            partmodlist.Add(partmod);

            return ret;
        }

        private Part CallbackPart(int priority, string name, Action<IResourceManager> callback)
        {
            var ret = new Part();

            var partmod = new VRMPriorityPartModule(priority, name, callback);
            var partmodlist = new PartModuleList(ret);
            partmodlist.Add(partmod);

            return ret;
        }

        [TestMethod]
        public void TestSimpleResourceGeneration()
        {
            double expected = 0.75, got = 0;

            var rm = Setup();
            rm.Vessel.parts.Add(CallbackPart(5, "TestSimpleResourceGeneration", (IResourceManager resMan) =>
            {
                got = resMan.ConsumeResource(ResourceName.ElectricCharge, expected);
            }));

            rm.FixedUpdate();

            Assert.IsTrue(expected == got, $"[TestSimpleResourceGeneration] did not receieve {expected} of EC, got {got}");
        }

        [TestMethod]
        public void TestPriorityValues()
        {
            /*
            var rm = Setup();
            rm.FixedUpdate();

            string[] mods = { "VRMPriorityPartModule" };

            for (var i = 1; i < 6; i++)
            {
                var p = NewTestPart(mods);
                Assert.Equals(mods.Length, p.Modules.Count);
                rm.Vessel.Parts.Add(p);
            }
            */
        }

        [TestMethod]
        public void TestExceptionsDontPropagate()
        {

        }

        [TestMethod]
        public void TestCatchupHappens()
        {
            // HighLogic.CurrentGame.UniversalTime for Planetarium.GetUniversalTime()
        }

        [TestMethod]
        public void TestRefreshesDontReexecute()
        {
            // partmodule.explode -> sets the singleton event. set that, fail test if that happens.
        }

    }

    public class VRMPriorityPartModule : PartModule, IKITMod
    {
        public int Priority;
        Action<IResourceManager> CallBack;
        public string PartName;


        public VRMPriorityPartModule(int priority, string partName, Action<IResourceManager> callback)
        {
            Priority = priority;
            CallBack = callback;
            PartName = partName;
        }

        public string KITPartName() => PartName;

        public ResourcePriorityValue ResourceProcessPriority() => (ResourcePriorityValue)Priority;

        public void KITFixedUpdate(IResourceManager resMan) => CallBack(resMan);
    }
}
