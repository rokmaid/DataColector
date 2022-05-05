namespace AviaturCollectDataPrices
{
    public enum KINDOFTRIP : int { Disabled = 0, OneWay = 1, RoundTrip = 2 }
    public struct OriDesQuery
    {
        public OriDesQuery(string origin, string destination, KINDOFTRIP kindOfTrip)
        {
            Origin = origin;
            Destination = destination;
            KindOfTrip = kindOfTrip;
        }

        public string Origin { get; private set; }
        public string Destination { get; private set; }
        public KINDOFTRIP KindOfTrip { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}_{1}_type{2}", Origin, Destination, KindOfTrip); 
        }
    }
}
