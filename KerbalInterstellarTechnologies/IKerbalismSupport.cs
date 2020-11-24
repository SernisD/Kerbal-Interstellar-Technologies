using System;
using System.Collections.Generic;

namespace KerbalInterstellarTechnologies
{
    interface IKerbalismSupport
    {
        string PlannerUpdate(List<KeyValuePair<string, double>> resources, CelestialBody body, Dictionary<string, double> environment);
        string BackgroundUpdate(Vessel v,
    ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot,
    PartModule proto_part_module, Part proto_part,
    Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest,
    double elapsed_s);

        string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest);
    }

    public class KerbalismResourceInterface : IResourceInterface
    {
        public Dictionary<string, double> available;
        public List<KeyValuePair<string, double>> consumed;

        public KerbalismResourceInterface()
        {
            available = new Dictionary<string, double>();
            consumed = new List<KeyValuePair<string, double>>();
        }

        public KerbalismResourceInterface(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
        {
            available = availableResources;
            consumed = resourceChangeRequest;
        }

        public double Consume(string resourceName, double requestedAmount)
        {
            double avail;
            var ok = available.TryGetValue(resourceName, out avail);
            if (!ok) return 0;

            double ret = Math.Min(avail, requestedAmount);
            consumed.Add(new KeyValuePair<string, double>(resourceName, ret));
            return ret;
        }

        public void Produce(string resourceName, double amount)
        {
            consumed.Add(new KeyValuePair<string, double>(resourceName, amount));
        }
    }
}
