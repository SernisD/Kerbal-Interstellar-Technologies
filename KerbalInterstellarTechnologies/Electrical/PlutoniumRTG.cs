using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KerbalInterstellarTechnologies.Settings;

namespace KerbalInterstellarTechnologies.Electrical
{
    public class KITPlutoniumRTG : PartModule, IKITMod
    {
        [KSPField] public double amount = 0.75;
        [KSPField] public double age;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            // Per wikipedia, decays by  0.787% per year. TODO
            var tmp = amount - 0;
            resMan.ProduceResource(ResourceName.ElectricCharge, tmp);

            age += resMan.FixedDeltaTime();
        }

        public string KITPartName() => "Plutonium RTG";

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First | ResourcePriorityValue.SupplierOnlyFlag;
    }
}
