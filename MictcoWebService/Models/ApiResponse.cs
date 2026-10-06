namespace MictcoWebService.Models
{
    public class ApiResponse<T>
    {
        public string Status { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
