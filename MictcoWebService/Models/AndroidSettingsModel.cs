namespace MictcoWebService.Models
{
    public class AndroidSettingsModel
    {
        public int ans_id { get; set; }
        public string ans_status { get; set; }
        public string ans_remark { get; set; }
        // BACKEND | FRONTEND | BOTH
        public string ans_used_by { get; set; }
    }
}
