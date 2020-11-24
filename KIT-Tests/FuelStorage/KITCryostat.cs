using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbalInterstellarTechnologies;
using System.Collections.Generic;
using System.Diagnostics;

namespace KIT_Tests
{
    [TestClass]
    public class TestKITCryostat
    {
        private KerbalInterstellarTechnologies.FuelStorage.KITCryostatConfig LqdHeliumConfig()
        {
            var c = new KerbalInterstellarTechnologies.FuelStorage.KITCryostatConfig();

            c.resourceName = "LqdHelium";
            c.resourceGUIName = "Liquid Helium";
            c.boilOffRate = 0;
            c.boilOffTemp = 4.222;
            c.boilOffMultiplier = 1;
            c.boilOffBase = 1000;
            c.boilOffAddition = 8.97215e-8;

            return c;
        }

        private PartResource NewPartResource(double maxAmount, double amount)
        {
            PartResource pr = new PartResource((Part)null);

            pr.maxAmount = maxAmount;
            pr.amount = amount;

            return pr;
        }

        private KerbalInterstellarTechnologies.FuelStorage.KITCryostatConfig LiquidFuelConfig()
        {
            var c = new KerbalInterstellarTechnologies.FuelStorage.KITCryostatConfig();

            c.resourceName = "LiquidFuel";
            c.resourceGUIName = "LiquidFuel";
            c.boilOffRate = 0;
            c.boilOffTemp = 480;
            c.boilOffMultiplier = 1;
            c.boilOffBase = 1000;
            c.boilOffAddition = 8.97215e-8;

            return c;
	    }

        [TestMethod]
        public void TestInfiniteElectricityDrawsNoCharge()
        {
         
            Dictionary<string, double> resources = new Dictionary<string, double>();
            List<KeyValuePair<string, double>> consumed = new List<KeyValuePair<string, double>>();

            KerbalismResourceInterface kri = new KerbalismResourceInterface(resources, consumed);

            var mco = new ConfigurableCheatOptions();
            mco.InfiniteElectricity = true;

            var config = LqdHeliumConfig();

            double testTemp = 200;

            var BOC = KerbalInterstellarTechnologies.FuelStorage.KITCryostatBoiloff.BoilOffCalculator(kri, config , mco);

            var pr = NewPartResource(100, 100);

            var ret = BOC(pr, testTemp, true);

            Assert.IsTrue(consumed.Count == 0, "should not consume resources when InfiniteElectricity is enabled");
            Assert.IsTrue(ret == true, "return result should be true");
        }

        [TestMethod]
        public void TestIgnoreMaxHeatPreventsBoilOff()
        {
            KerbalismResourceInterface kri = new KerbalismResourceInterface();

            var mco = new ConfigurableCheatOptions();
            mco.IgnoreMaxTemperature = true;

            var config = LqdHeliumConfig();
            var BOC = KerbalInterstellarTechnologies.FuelStorage.KITCryostatBoiloff.BoilOffCalculator(kri, config, mco);

            var pr = NewPartResource(100, 100);
            double testTemp = 2000;
            var ret = BOC(pr, testTemp, true);

            Assert.IsTrue(kri.consumed.Count == 0, "should not consume resources when ignoring max temp");
            Assert.IsTrue(ret == true, "return result should be true");
        }

        [TestMethod]
        public void TestTankContents()
        {
            KerbalismResourceInterface kri = new KerbalismResourceInterface();
            kri.available["ElectricCharge"] = 5000;

            var mco = new ConfigurableCheatOptions();
            var config = LqdHeliumConfig();

            var BOC = KerbalInterstellarTechnologies.FuelStorage.KITCryostatBoiloff.BoilOffCalculator(kri, config, mco);

            var pr = NewPartResource(100, 0);

            double testTemp = 200;

            var ret = BOC(pr, testTemp, true);
            Assert.IsTrue(ret, "empty tanks don't need cooling");

            pr.amount = 100;
            ret = BOC(pr, testTemp, true);
            Assert.IsTrue(ret, "should be happy that power is supplied");

            // kri.available.Remove("ElectricCharge");

            kri.available["ElectricCharge"] = 0;
            ret = BOC(pr, testTemp, true);
            Assert.IsFalse(ret, $"Should not be able to draw enough ElectricCharge.");

            kri.consumed.ForEach(x =>
            {
                //Trace.TraceInformation($"{x.Key}: {x.Value}");
                // Debug.WriteLine($"{x.Key}: {x.Value}");
                Assert.IsTrue(false, "Key: {x.Key} Value: {x.Value}");
            });
        }

        [TestMethod]
        public void TestBoilOffProcess()
        {
        }
    }
}
