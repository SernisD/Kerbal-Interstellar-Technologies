using System.Collections.Generic;

namespace KerbalInterstellarTechnologies
{
    public interface IResourceInterface
    {
        void Produce(string resourceName, double amount);
        double Consume(string resourceName, double requestedAmount);
    }

    public interface IKITMod
    {
        int ResourceProcessPriority();
        void KITFixedUpdate(double deltaTime);
        string KITPartName();
    }

    public interface KITResourceSupplier
    {
        int ResourceProcessPriority();
        string KITResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest);
    }

    public class StockResourceInterface : IResourceInterface
    {
        private Part _part;
        public StockResourceInterface(Part part)
        {
            _part = part;
        }

        public double Consume(string resourceName, double requestedAmount)
        {
            return _part.RequestResource(resourceName, requestedAmount);
        }

        public void Produce(string resourceName, double amount)
        {
            _part.RequestResource(resourceName, -amount);
        }
    }

}