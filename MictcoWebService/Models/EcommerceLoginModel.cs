namespace MictcoWebService.Models
{
    public class EcommerceUserCreate
    {
        public string gel_login_id { get; set; }
        public string gel_role { get; set; }
        public int? gel_ledger_id { get; set; }
        //public string? gel_profile_image { get; set; }

    }
    public class EcommerceUserUpdate
    {
        public int? gel_id { get; set; }
        public string gel_login_id { get; set; }
        public string gel_role { get; set; }
        public int? gel_ledger_id { get; set; }
        public int? gel_active { get; set; }
        //public string? gel_profile_image { get; set; }


    }
    public class EcommerceUser
    {
        public string UserId { get; set; }
        public string LoginId { get; set; }
        public string Role { get; set; }
        public string LedgerId { get; set; }
        public string gel_profile_image { get; set; }


    }
}
