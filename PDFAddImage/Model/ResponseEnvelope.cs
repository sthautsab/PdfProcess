namespace PDFAddImage.Model
{
    public class ResponseEnvelope<T>
    {
        public string Status { get; set; }
        public int HttpStatusCode { get; set; }
        public int Code { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }
}
