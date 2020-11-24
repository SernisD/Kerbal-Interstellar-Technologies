namespace KerbalInterstellarTechnologies
{
    public interface IResourceInterface
    {
        void Produce(string resourceName, double amount);
        double Consume(string resourceName, double requestedAmount);
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