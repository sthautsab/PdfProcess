using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PDFAddImage.Service;
using PDFAddImage.Setting;
using System.Net;

namespace PDFAddImage
{
    public class MergePdfFunction
    {
        private readonly ILogger _logger;
        private IPdfService _pdfService;
        private PdfImageSetting _setting;

        public MergePdfFunction(ILoggerFactory loggerFactory, IPdfService pdfService, PdfImageSetting setting)
        {
            _logger = loggerFactory.CreateLogger<MergePdfFunction>();
            _pdfService = pdfService;
            _setting = setting;

        }

        [Function("MergePdfFunction")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, string url, string w, string mt, string mb, string ml, string mr, string n, string pb)
        {
            //_pdfService.MergePdf();
            byte[] downloadedPdfBytes = null;
            byte[] storedPdfBytes = null;
            byte[] pdfReturnBytes = null;

            List<byte[]> splitBytes = new List<byte[]>();
            //not null when pdf size greater and need to split
            byte[] zipBytes = null;
            byte[] singleUrlPdfZipBytes = null;

            int sizeToSplitInMb = 2;

            //string[] urls = { "https://tinyurl.com/j43v7kc8/", "https://tinyurl.com/2s3zrc5y/", "https://tinyurl.com/3fa74j5s/" };
            string[] urls = { "https://tinyurl.com/j43v7kc8/" };
            string initialMergedPdfName = String.Empty;
            string finalMergedPdfName = String.Empty;

            List<string> pdfNames = new List<string>();
            List<byte[]> pdfBytesToMerge = new List<byte[]>();

            bool multipleUrl = false;
            if (urls.Length > 1)
            {
                multipleUrl = true;
            }


            foreach (string singleUrl in urls)
            {
                string pdfUrl = $"{_setting.SourceUrl}&url={singleUrl}&w={w}&mt={100}&mb={25}&ml={25}&mr={25}&n={"down"}&pb={false}";

                string pdfName = _pdfService.StringSha256Hash(singleUrl) + ".pdf";
                pdfNames.Add(pdfName);

                //null if pdf doesnot exists 
                storedPdfBytes = _pdfService.GetPdfIfExists(pdfName);
                if (storedPdfBytes != null)
                {
                    pdfBytesToMerge.Add(storedPdfBytes);
                    pdfReturnBytes = _pdfService.AddHeader(storedPdfBytes);
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

                            //bytes to merge
                            pdfBytesToMerge.Add(storedPdfBytes);

                            pdfReturnBytes = _pdfService.AddHeader(storedPdfBytes);

                            if (!multipleUrl)
                            {
                                //checking if size greater
                                if (pdfReturnBytes.Length > (sizeToSplitInMb * 1024 * 1024))
                                {
                                    splitBytes = _pdfService.SplitPDF(pdfReturnBytes, sizeToSplitInMb);

                                    string[] fileNames = new string[splitBytes.Count];
                                    for (int i = 0; i < splitBytes.Count; i++)
                                    {
                                        fileNames[i] = $"split_singleUrlPdf{i + 1}.pdf";
                                    }

                                    //Zip Pdf
                                    singleUrlPdfZipBytes = _pdfService.ZipFiles(fileNames, splitBytes.ToArray());
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"HTTP request failed with status code: {response.StatusCode}");
                        }
                    }
                }
                //Just to get avoid multiple .pdf in finalMergedPdfName
                initialMergedPdfName = initialMergedPdfName + pdfName.Replace(".pdf", "");
            }

            #region MergedPdfProcess
            //Runs only if multiple url is available
            if (multipleUrl)
            {
                finalMergedPdfName = "_merged" + initialMergedPdfName + ".pdf";

                //check if pdf file exists in directory
                byte[] storedMergedPdfBytes = _pdfService.GetPdfIfExists(finalMergedPdfName);
                byte[] mergedPdfReturnBytes = null;
                if (storedMergedPdfBytes != null)
                {
                    mergedPdfReturnBytes = _pdfService.AddHeaderForMergedPdf(storedMergedPdfBytes);

                    //checking if size greater
                    if (mergedPdfReturnBytes.Length > (sizeToSplitInMb * 1024 * 1024))
                    {
                        splitBytes = _pdfService.SplitPDF(mergedPdfReturnBytes, sizeToSplitInMb);

                        string[] fileNames = new string[splitBytes.Count];
                        for (int i = 0; i < splitBytes.Count; i++)
                        {
                            fileNames[i] = $"split_pdf_{i + 1}.pdf";
                        }

                        //Zip Pdf
                        zipBytes = _pdfService.ZipFiles(fileNames, splitBytes.ToArray());
                    }
                }

                //if the merged file is not saved previously
                else
                {
                    //Merge Pdf
                    byte[] mergedPdfBytes = _pdfService.MergePdf(pdfBytesToMerge);
                    //save pdf without header
                    _pdfService.SavePdf(mergedPdfBytes, finalMergedPdfName);

                    //For spliting and dowloading pdf
                    mergedPdfReturnBytes = _pdfService.AddHeaderForMergedPdf(mergedPdfBytes);

                    //checking if size greater
                    if (mergedPdfReturnBytes.Length > (sizeToSplitInMb * 1024 * 1024))
                    {
                        splitBytes = _pdfService.SplitPDF(mergedPdfReturnBytes, sizeToSplitInMb);

                        string[] fileNames = new string[splitBytes.Count];
                        for (int i = 0; i < splitBytes.Count; i++)
                        {
                            fileNames[i] = $"split_pdf_{i + 1}.pdf";
                        }

                        //Zip Pdf
                        zipBytes = _pdfService.ZipFiles(fileNames, splitBytes.ToArray());
                    }

                }

                #endregion
                // Incase of returning zip file
                if (zipBytes != null)
                {
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/zip");
                    response.Headers.Add("Content-Disposition", "attachment; filename=splitPdfs.zip");

                    await response.Body.WriteAsync(zipBytes, 0, zipBytes.Length);
                    return response;
                }

                //If the size is not greater and no splitting is needed
                else
                {
                    // Set the response content type
                    var returnResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);

                    returnResponse.Headers.Add("Content-Type", "application/pdf");

                    // Specify the filename for the downloaded PDF
                    returnResponse.Headers.Add("Content-Disposition", "attachment; filename=mergedPdf.pdf");

                    // Write the PDF content to the response
                    await returnResponse.Body.WriteAsync(mergedPdfReturnBytes, 0, mergedPdfReturnBytes.Length);

                    return returnResponse;
                }
            }

            //If single url 
            else
            {
                if (singleUrlPdfZipBytes != null)
                {
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/zip");
                    response.Headers.Add("Content-Disposition", "attachment; filename=splitSingleUrlPdfs.zip");

                    await response.Body.WriteAsync(singleUrlPdfZipBytes, 0, singleUrlPdfZipBytes.Length);
                    return response;
                }
                // Set the response content type
                var returnResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                returnResponse.Headers.Add("Content-Type", "application/pdf");
                // Specify the filename for the downloaded PDF
                returnResponse.Headers.Add("Content-Disposition", "attachment; filename=mergedPdf.pdf");
                // Write the PDF content to the response
                await returnResponse.Body.WriteAsync(pdfReturnBytes, 0, pdfReturnBytes.Length);

                return returnResponse;
            }

        }


    }
}

