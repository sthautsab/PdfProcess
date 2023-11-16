using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PDFAddImage.Service;
using PDFAddImage.Setting;

namespace PDFAddImage
{
    public class PDFFunction
    {
        private readonly ILogger _logger;
        private PdfImageSetting _setting;
        private IPdfService _pdfService;


        public PDFFunction(ILoggerFactory loggerFactory, PdfImageSetting setting, IPdfService pdfService)
        {
            _logger = loggerFactory.CreateLogger<PDFFunction>();
            _setting = setting;
            _pdfService = pdfService;
        }

        [Function("PDFFunction")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string url, string w, string mt, string mb, string ml, string mr, string n, string pb)
        {
            string pdfUrl = $"{_setting.SourceUrl}&url={url}&w={w}&mt={mt}&mb={mb}&ml={ml}&mr={mr}&n={n}&pb={pb}";
            byte[] downloadedPdfBytes = null;
            byte[] storedPdfBytes = null;
            byte[] pdfReturnBytes = null;

            string[] urls = { "https://tinyurl.com/2s3zrc5y/", "https://tinyurl.com/2s3u667z/" };
            string mergedPdfName = String.Empty;

            foreach (string urld in urls)
            {
                mergedPdfName += urld;
            }

            string pdfName = _pdfService.StringSha256Hash(url) + ".pdf";

            //null if pdf doesnot exists 
            storedPdfBytes = _pdfService.GetPdfIfExists(pdfName);
            if (storedPdfBytes != null)
            {
                pdfReturnBytes = _pdfService.AddHeaderForMergedPdf(storedPdfBytes);
            }
            else
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(pdfUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        downloadedPdfBytes = await response.Content.ReadAsByteArrayAsync();
                        storedPdfBytes = _pdfService.AddLogo(downloadedPdfBytes);

                        //save pdf to file if not exists
                        _pdfService.SavePdf(storedPdfBytes, pdfName);

                        pdfReturnBytes = _pdfService.AddHeaderForMergedPdf(storedPdfBytes);

                    }
                    else
                    {
                        Console.WriteLine($"HTTP request failed with status code: {response.StatusCode}");
                    }
                }
            }


            // Set the response content type
            var returnResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            returnResponse.Headers.Add("Content-Type", "application/pdf");

            // Specify the filename for the downloaded PDF
            returnResponse.Headers.Add("Content-Disposition", $"attachment; filename={n}");

            // Write the PDF content to the response
            await returnResponse.Body.WriteAsync(pdfReturnBytes, 0, pdfReturnBytes.Length);

            return returnResponse;





        }
    }
}
