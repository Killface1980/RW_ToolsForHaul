namespace TFH_VehicleBase
{
    using Verse;

    public struct VehicleInfo
    {
        public VehicleInfo(Vehicle_Cart cart, Map map, Pawn driver)
        {
            this.driver = driver;
            this.cart = cart;
            this.map = map;
        }

        private Vehicle_Cart cart;

        private Map map;

        private Pawn driver;
    }
}