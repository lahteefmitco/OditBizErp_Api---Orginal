namespace MictcoWebService.Models
{
    public class SkipShop
    {
        public int route_id { get; set; }

        public int shop_id { get; set; }

        public string latitude { get; set; }

        public string longitude { get; set; }

        public string skip_reason { get; set; }
    }
}
