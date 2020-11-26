using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbalInterstellarTechnologies;

namespace KIT_Tests
{
    [TestClass]
    class TestVesselResourceManager
    {
        private KerbalInterstellarTechnologies.ResourceManagement.KITResourceManager Setup()
        {
            var kitrm = new KerbalInterstellarTechnologies.ResourceManagement.KITResourceManager();
            kitrm.Vessel = new Vessel();
            kitrm.Vessel.vesselName = "Test Vessel";

            kitrm.Vessel.parts = new List<Part>();
            return kitrm;
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

            // <= 0, > 5.

            // is called in order
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
}
