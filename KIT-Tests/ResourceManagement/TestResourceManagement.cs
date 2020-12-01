using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KerbalInterstellarTechnologies;
using KerbalInterstellarTechnologies.Settings;
using KerbalInterstellarTechnologies.ResourceManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbalInterstellarTechnologies.Electrical;

namespace KIT_Tests.ResourceManagement
{
    public class TestVesselResources : IVesselResources
    {
        public bool vesselModifiedCalled;
        public bool OnKITProcessingFinishedCalled;
        public bool vesselModified;
        public List<IKITMod> moduleList = new List<IKITMod>();
        public Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules = new Dictionary<ResourceName, List<IKITVariableSupplier>>();

        void IVesselResources.OnKITProcessingFinished(IResourceManager resourceManager)
        {
            OnKITProcessingFinishedCalled = true;
        }

        void IVesselResources.VesselKITModules(ref List<IKITMod> updatedModuleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> updatedVariableSupplierModules)
        {
            updatedModuleList = new List<IKITMod>(moduleList);
            updatedVariableSupplierModules = new Dictionary<ResourceName, List<IKITVariableSupplier>>(variableSupplierModules);
        }

        bool IVesselResources.VesselModified()
        {
            vesselModifiedCalled = true;
            return vesselModified;
        }
    }

    public class TestIKITMod : IKITMod
    {
        public string testName = "A girl has no name";
        public ResourcePriorityValue priority;
        public Action<IResourceManager> callBack;

        void IKITMod.KITFixedUpdate(IResourceManager resMan)
        {
            if (callBack != null)
            {
                callBack(resMan);
            }
        }

        string IKITMod.KITPartName() => $"TestIKITMod [{testName}]";

        ResourcePriorityValue IKITMod.ResourceProcessPriority() => priority;
    }

    public class TestIKITModSuppliable : TestIKITMod, IKITVariableSupplier
    {
        public Func<IResourceManager, ResourceName, double, bool> provideRequestCallback;
        public Func<ResourceName[]> resourcesProvidedCallback;

        bool IKITVariableSupplier.ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            if (provideRequestCallback != null)
            {
                return provideRequestCallback(resMan, resource, requestedAmount);
            }

            return false;
        }

        ResourceName[] IKITVariableSupplier.ResourcesProvided()
        {
            return (resourcesProvidedCallback != null) ? resourcesProvidedCallback() : new ResourceName[] { };
        }
    }

    [TestClass]
    public class TestResourceManagement
    {
        [TestMethod]
        public void TestVesselFinishedCallback()
        {
            var tvr = new TestVesselResources();
            tvr.vesselModified = true;

            bool allGood = false;

            var _rm = new KerbalInterstellarTechnologies.ResourceManagement.ResourceManager(tvr, RealCheatOptions.Instance);
            _rm.UseThisToHelpWithTesting = true;
            var rm = _rm as IResourceScheduler;

            var resources = new Dictionary<ResourceName, double>();
            var maxims = new Dictionary<ResourceName, double>();
            rm.ExecuteKITModules(1, ref resources, ref maxims);

            // This code should never get called, as there was no processing performed
            Assert.IsFalse(tvr.OnKITProcessingFinishedCalled, "called OnKITProcessingFinishedCalled when unexpected");

            var consumer = new TestIKITMod();
            consumer.callBack = (IResourceManager resMan) =>
            {
                allGood = true;
            };

            tvr.moduleList.Add(consumer);

            rm.ExecuteKITModules(1, ref resources, ref maxims);

            Assert.IsTrue(allGood, "failed to execute IKITMod");
            Assert.IsTrue(tvr.OnKITProcessingFinishedCalled, "did not call OnKITProcessingFinishedCalled");
        }

        [TestMethod]
        public void TestVesselModificationandRTG()
        {
            var tvr = new TestVesselResources();
            tvr.vesselModified = true;
            
            var resources = new Dictionary<ResourceName, double>();
            var maximums = new Dictionary<ResourceName, double>();
            maximums.Add(ResourceName.ElectricCharge, 5);

            var _rm = new KerbalInterstellarTechnologies.ResourceManagement.ResourceManager(tvr, RealCheatOptions.Instance);
            _rm.UseThisToHelpWithTesting = true;
            var rm = _rm as IResourceScheduler;

            rm.ExecuteKITModules(1, ref resources, ref maximums);

            Assert.IsTrue(true == tvr.vesselModifiedCalled, "Did not perform IVesselResources.VesselModified() callback");

            var producer = new TestIKITMod();
            producer.callBack = (IResourceManager resMan) =>
            {
                resMan.ProduceResource(ResourceName.ElectricCharge, 0.75);
            };

            tvr.moduleList.Add(producer);

            rm.ExecuteKITModules(1, ref resources, ref maximums);

            Assert.IsTrue(resources.ContainsKey(ResourceName.ElectricCharge), "did not generate ElectricCharge");
            Assert.IsTrue(resources[ResourceName.ElectricCharge] == 0.75, "did not generate expected amount of ElectricCharge");

            resources.Clear();

            tvr.vesselModified = tvr.vesselModifiedCalled = false;

            rm.ExecuteKITModules(0.2, ref resources, ref maximums);
            Assert.IsTrue(resources.ContainsKey(ResourceName.ElectricCharge), "did not generate ElectricCharge");
            Assert.IsTrue(resources[ResourceName.ElectricCharge] == 0.75 * 0.2, "did not generate expected amount of ElectricCharge");
        }

        [TestMethod]
        public void TestVariableResourceSuppliable()
        {
            var tvr = new TestVesselResources();
            tvr.vesselModified = true;

            var resources = new Dictionary<ResourceName, double>();

            var _rm = new KerbalInterstellarTechnologies.ResourceManagement.ResourceManager(tvr, RealCheatOptions.Instance);
            _rm.UseThisToHelpWithTesting = true;
            var rm = _rm as IResourceScheduler;

            bool requestCallbackRan = false;
            bool fixedUpdateCalledBeforeProvide = false;
            double canProvide = 0;

            var tims = new TestIKITModSuppliable();
            tims.provideRequestCallback = (IResourceManager resman, ResourceName resource, double amount) =>
            {
                requestCallbackRan = true;
                Assert.IsTrue(fixedUpdateCalledBeforeProvide, "fixed update must be called before providing resources");

                var toSend = Math.Min(amount, canProvide);
                canProvide -= toSend;

                if(toSend > 0) resman.ProduceResource(resource, toSend);

                return canProvide > 0;
            };

            tims.resourcesProvidedCallback = () => new ResourceName[] { ResourceName.ElectricCharge };

            tims.testName = "ElectricCharge Callback";
            tims.priority = ResourcePriorityValue.Fifth;
            tims.callBack = (IResourceManager resman) =>
            {
                fixedUpdateCalledBeforeProvide = true;
                canProvide = 5;
            };

            var consumer = new TestIKITMod();
            consumer.callBack = (IResourceManager resMan) =>
            {
                Assert.IsTrue(1 == resMan.ConsumeResource(ResourceName.ElectricCharge, 1), "failed to consume electric charge");
                Assert.IsTrue(1 == resMan.ConsumeResource(ResourceName.ElectricCharge, 1), "failed to consume electric charge");
                Assert.IsTrue(1 == resMan.ConsumeResource(ResourceName.ElectricCharge, 1), "failed to consume electric charge");
                Assert.IsTrue(1 == resMan.ConsumeResource(ResourceName.ElectricCharge, 1), "failed to consume electric charge");
                Assert.IsTrue(1 == resMan.ConsumeResource(ResourceName.ElectricCharge, 1), "failed to consume electric charge");
                
                var result = resMan.ConsumeResource(ResourceName.ElectricCharge, 2);
                Assert.IsTrue(0 == result, $"failed to NOT consume electric charge. got {result} EC");
            };

            tvr.moduleList.Add(consumer);
            tvr.moduleList.Add(tims);

            tvr.variableSupplierModules[ResourceName.ElectricCharge] = new List<IKITVariableSupplier>();
            tvr.variableSupplierModules[ResourceName.ElectricCharge].Add(tims);

            var empty = new Dictionary<ResourceName, double>();
            rm.ExecuteKITModules(1, ref empty, ref empty);

            var noEC = new Dictionary<ResourceName, double>();
            noEC[ResourceName.ElectricCharge] = 0;

            Assert.IsFalse(tims.provideRequestCallback(rm as IResourceManager, ResourceName.ElectricCharge, 20));

            Assert.IsTrue(requestCallbackRan, "callback code has NOT been ran");
            Assert.AreEqual(empty[ResourceName.ElectricCharge], noEC[ResourceName.ElectricCharge], $"resources are expected to be empty .. {empty} vs {noEC}");
        }

        [TestMethod]
        public void TestBatteriesGetFilled()
        {
            var tvr = new TestVesselResources();
            tvr.vesselModified = true;

            var resources = new Dictionary<ResourceName, double>();

            var _rm = new KerbalInterstellarTechnologies.ResourceManagement.ResourceManager(tvr, RealCheatOptions.Instance);
            _rm.UseThisToHelpWithTesting = true;
            var rm = _rm as IResourceScheduler;

            bool fixedUpdateCalledBeforeProvide = false;
            double canProvide = 0;

            var tims = new TestIKITModSuppliable();
            tims.provideRequestCallback = (IResourceManager resman, ResourceName resource, double amount) =>
            {
                Assert.IsTrue(fixedUpdateCalledBeforeProvide, "fixed update must be called before providing resources");

                var toSend = Math.Min(amount, canProvide);
                canProvide -= toSend;

                if (toSend > 0) resman.ProduceResource(resource, toSend);

                return canProvide > 0;
            };

            tims.resourcesProvidedCallback = () => new ResourceName[] { ResourceName.ElectricCharge };

            tims.testName = "ElectricCharge Callback";
            tims.priority = ResourcePriorityValue.Fifth;
            tims.callBack = (IResourceManager resman) =>
            {
                fixedUpdateCalledBeforeProvide = true;
                canProvide = 5;
            };

            tvr.moduleList.Add(tims);

            tvr.variableSupplierModules[ResourceName.ElectricCharge] = new List<IKITVariableSupplier>();
            tvr.variableSupplierModules[ResourceName.ElectricCharge].Add(tims);

            var empty = new Dictionary<ResourceName, double>();
            empty.Add(ResourceName.ElectricCharge, 0);
            var maximums = new Dictionary<ResourceName, double>();
            maximums.Add(ResourceName.ElectricCharge, 20);
            rm.ExecuteKITModules(1, ref empty, ref maximums);

            Assert.IsTrue(empty[ResourceName.ElectricCharge] == 5, $"did not fill battery - it is {empty[ResourceName.ElectricCharge]}");
        }

        [TestMethod]
        public void TestFixedDeltaTime()
        {
            var tvr = new TestVesselResources();
            tvr.vesselModified = true;

            var resources = new Dictionary<ResourceName, double>();

            var _rm = new KerbalInterstellarTechnologies.ResourceManagement.ResourceManager(tvr, RealCheatOptions.Instance);
            _rm.UseThisToHelpWithTesting = true;
            var rm = _rm as IResourceScheduler;

            var consumer = new TestIKITMod();
            consumer.callBack = (IResourceManager resMan) =>
            {
                Assert.IsTrue(1 == resMan.ConsumeResource(ResourceName.ElectricCharge, 1), "failed to consume electric charge");
            };

            tvr.moduleList.Add(consumer);

            var empty = new Dictionary<ResourceName, double>();
            empty.Add(ResourceName.ElectricCharge, 20);
            var maximums = new Dictionary<ResourceName, double>();
            maximums.Add(ResourceName.ElectricCharge, 20);

            rm.ExecuteKITModules(1, ref empty, ref maximums);
            Assert.IsTrue(empty[ResourceName.ElectricCharge] == 19, $"1 seconds should result in 19 EC left");
            rm.ExecuteKITModules(2, ref empty, ref maximums);
            Assert.IsTrue(empty[ResourceName.ElectricCharge] == 17, $"2 seconds should result in 17 EC left. Instead got {empty[ResourceName.ElectricCharge]}");
            rm.ExecuteKITModules(0.2, ref empty, ref maximums);
            Assert.IsTrue(empty[ResourceName.ElectricCharge] == 16.8, $".2 seconds should result in 16.8 EC left. Instead got {empty[ResourceName.ElectricCharge]}");

        }
    }
}
