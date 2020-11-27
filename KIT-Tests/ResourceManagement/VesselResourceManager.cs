using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbalInterstellarTechnologies;

namespace KIT_Tests.ResourceManager
{
    [TestClass]
    public class TestVesselResourceManager
    {
        
        private KerbalInterstellarTechnologies.ResourceManagement.KITResourceManager Setup()
        {
            var kitrm = new KerbalInterstellarTechnologies.ResourceManagement.KITResourceManager();
            kitrm.Vessel = new Vessel();
            kitrm.Vessel.vesselName = "Test Vessel";

            kitrm.Vessel.parts = new List<Part>();
            return kitrm;
        }

        private Part NewTestPart(PartModule[] modules)
        {
            var ret = new Part();
            ret.partName = "Test Part";
            // foreach (var s in moduleNames) ret.AddModule(s);

            var fi = ret.Assembly;


            return ret;
        }

        [TestMethod]
        public void TestDoesNothingWhenNoModules()
        {
            var rm = Setup();
            rm.FixedUpdate();
        }

        [TestMethod]
        public void TestPriorityValues()
        {
            var rm = Setup();
            rm.FixedUpdate();

            string[] mods = { "VRMPriorityPartModule" };

            for (var i = 1; i < 6; i++)
            {
                var p = NewTestPart(mods);
                Assert.Equals(mods.Length, p.Modules.Count);
                rm.Vessel.Parts.Add(p);
            }
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
        Action<double> CallBack;
        public string PartName;


        public VRMPriorityPartModule(int priority, string partName, Action<double> callback)
        {
            Priority = priority;
            CallBack = callback;
            PartName = partName;
        }

        public void KITFixedUpdate(double deltaTime) => CallBack(deltaTime);

        public string KITPartName() => PartName;

        public int ResourceProcessPriority() => Priority;
    }
}
