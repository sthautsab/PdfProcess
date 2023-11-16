using iTextSharp.text.pdf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PDFAddImage.Setting;
using SkiaSharp;
using System.Text;

namespace PdfProcess
{
    public class GetAndSetPdfFunction
    {
        private readonly ILogger _logger;
        private PdfImageSetting _setting;
        public GetAndSetPdfFunction(ILoggerFactory loggerFactory, PdfImageSetting setting)
        {
            _logger = loggerFactory.CreateLogger<GetAndSetPdfFunction>();
            _setting = setting;
        }

        [Function("GetAndSetPdfFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string url, string w, string mt, string mb, string ml, string mr, string n, string pb)
        {
            //Fetch and store the PDF
            #region FetchStorePDF

            string pdfUrl = $"{_setting.SourceUrl}&url={url}&w={w}&mt={mt}&mb={mb}&ml={ml}&mr={mr}&n={n}&pb={pb}";
            byte[] downloadedPdfBytes = null;
            byte[] pdfReturnBytes = null;
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(pdfUrl);

                if (response.IsSuccessStatusCode)
                {
                    downloadedPdfBytes = await response.Content.ReadAsByteArrayAsync();
                    pdfReturnBytes = AddLogoAndHeader(downloadedPdfBytes, mr);
                    Console.WriteLine("PDF file downloaded successfully.");
                }
                else
                {
                    Console.WriteLine($"HTTP request failed with status code: {response.StatusCode}");
                }
            }
            #endregion

            //#region AddImageToPDF
            //var imagePath = Path.Combine(Environment.CurrentDirectory, "Image", "scconline.png");
            //var fontPath = Path.Combine(Environment.CurrentDirectory, "Font", "arial.ttf");
            //var imageToAdd = SKImage.FromEncodedData(imagePath);
            //byte[] pdfReturnBytes = null;
            //using (var fs = new MemoryStream())
            //{
            //    var reader = new PdfReader(downloadedPdfBytes);
            //    var stamper = new PdfStamper(reader, fs);

            //    string formattedDate = DateTime.Now.ToString("dddd, MMMM d, yyyy");
            //    string year = DateTime.Now.Year.ToString();

            //    var skBitmap = SKBitmap.FromImage(imageToAdd);
            //    iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(skBitmap, null);
            //    img.ScaleAbsolute(_setting.ImageWidth, _setting.ImageHeight);
            //    float imgHeight = img.ScaledHeight;
            //    float imgWeight = img.ScaledWidth;

            //    for (int i = 1; i <= reader.NumberOfPages; i++)
            //    {
            //        var pageSize = reader.GetPageSize(i);
            //        float xPos = _setting.LeftMargin; // Left edge of the page
            //        float yPos = pageSize.Height - imgHeight - _setting.TopMargin;

            //        float headerXPos = xPos + imgWeight + 10;
            //        float headerYPos = pageSize.Height - _setting.TopMargin - 7;
            //        img.SetAbsolutePosition(xPos, yPos);

            //        var contentByte = stamper.GetOverContent(i);

            //        // Set the font and size for the header text
            //        BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            //        contentByte.SetFontAndSize(baseFont, 8);

            //        // Define the header content
            //        string headerText = $"SCC Online Web Edition, © {year} EBC Publishing Pvt. Ltd.\nPage {i}        {formattedDate}\nPrinted For: ajay gupta, EBC Singar Nagar Office Gen Site\nSCC Online Web Edition: http://www.scconline.gen.in\n© {year} EBC Publishing Pvt. Ltd., Lucknow.\n--------------------------------------------------------------------------------------------------------------------------------------------------------";

            //        // Split the header text into lines
            //        string[] headerLines = headerText.Split('\n');

            //        // Write each line of the header
            //        float lineHeight = 10.5f; // Adjust the line height as needed
            //        float currentY = headerYPos;
            //        foreach (string line in headerLines)
            //        {
            //            contentByte.BeginText();
            //            contentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, line, headerXPos, currentY, 0);
            //            contentByte.EndText();
            //            currentY -= lineHeight;
            //        }
            //        contentByte.AddImage(img);
            //    }

            //    // Close the stamper and reader
            //    stamper.Close();
            //    reader.Close();
            //    pdfReturnBytes = fs.ToArray();
            //}
            //#endregion
            // Read the PDF file


            // Set the response content type
            var returnResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            returnResponse.Headers.Add("Content-Type", "application/pdf");

            // Specify the filename for the downloaded PDF
            returnResponse.Headers.Add("Content-Disposition", $"attachment; filename={n}");

            // Write the PDF content to the response
            await returnResponse.Body.WriteAsync(pdfReturnBytes, 0, pdfReturnBytes.Length);

            return returnResponse;
        }

        private byte[] AddLogoAndHeader(byte[] downloadedPdfBytes, string mr)
        {
            var imagePath = Path.Combine(Environment.CurrentDirectory, "Image", "scconline.png");
            var fontPath = Path.Combine(Environment.CurrentDirectory, "Font", "arial.ttf");
            var imageToAdd = SKImage.FromEncodedData(imagePath);
            byte[] pdfReturnBytes = null;
            using (var fs = new MemoryStream())
            {
                var reader = new PdfReader(downloadedPdfBytes);
                var stamper = new PdfStamper(reader, fs);

                string formattedDate = DateTime.Now.ToString("dddd, MMMM d, yyyy");
                string year = DateTime.Now.Year.ToString();

                var skBitmap = SKBitmap.FromImage(imageToAdd);
                iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(skBitmap, null);
                img.ScaleAbsolute(_setting.ImageWidth, _setting.ImageHeight);
                float imgHeight = img.ScaledHeight;
                float imgWeight = img.ScaledWidth;

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var pageSize = reader.GetPageSize(i);
                    float xPos = _setting.LeftMargin; // Left edge of the page
                    float yPos = pageSize.Height - imgHeight - _setting.TopMargin;

                    float width = pageSize.Width;

                    float headerXPos = xPos + imgWeight + 10;
                    float headerYPos = pageSize.Height - _setting.TopMargin - 7;

                    //int maxLineWidth = Convert.ToInt32(pageSize.Width - (_setting.LeftMargin + _setting.ImageWidth + 10 + float.Parse(mr)));
                    int maxLineWidth = 396;
                    img.SetAbsolutePosition(xPos, yPos);

                    var contentByte = stamper.GetOverContent(i);

                    // Set the font and size for the header text
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    contentByte.SetFontAndSize(baseFont, 8);

                    // Define the header content
                    HeaderSetting headerSetting = new HeaderSetting()
                    {
                        line1 = "GGGGGGGGGGGGGG GGGGGGG GGGGGGGGGGGGGG G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G G GG GG G G GG G G G G G G G G G G G G G G G G G GG GGGGGGGGG G G G G G G G G G G G G G G G SCC Online Web Edition",
                        line2 = $" © {year} EBC Publishing Pvt. Ltd.",
                        line3 = $"Page {i}        {formattedDate}",
                        line4 = $"Printed For: ajay gupta, EBC Singar Nagar Office Gen Site",
                        line5 = $"SCC Online Web Edition: http://www.scconline.gen.in",
                        line6 = $"© {year} EBC Publishing Pvt. Ltd., Lucknow.",
                        line7 = $"-------------------------------------------------------------------------------------------------------------------------------------------------------- "
                    };
                    //string headerText = $"GGGGGGGGGGGGGG GGGGGGGGGGGGGGGGGGGGGGGGGGGGG GGGGGGGGGGGGGGGGGGGG GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG GGGGGGGGGGGGGGGGGGGGGGGGG GGGGGGGGGGGGGGGGGGGGGGGGGGGG GGGGGGGGGGGGGGG GGGGGGGGGGGGGGGGG GGGGGGGGGGGGG GGGGGGGGGGGGGGG SCC Online Web Edition, © {year} EBC Publishing Pvt. Ltd.\nPage {i}        {formattedDate}\nPrinted For: ajay gupta, EBC Singar Nagar Office Gen Site\nSCC Online Web Edition: http://www.scconline.gen.in\n© {year} EBC Publishing Pvt. Ltd., Lucknow.\n-------------------------------------------------------------------------------------------------------------------------------------------------------- ";

                    string headerText = $"{headerSetting.line1}\n{headerSetting.line2}\n{headerSetting.line3}\n{headerSetting.line4}\n{headerSetting.line5}\n{headerSetting.line6}\n{headerSetting.line7}";

                    // Split the header text into lines
                    string[] headerLines = headerText.Split('\n');

                    // Write each line of the header
                    float lineHeight = 10.5f; // Adjust the line height as needed
                    float currentY = headerYPos;
                    foreach (string line in headerLines)
                    {
                        float lineWidth = baseFont.GetWidthPoint(line, 8);

                        // Check if the line width is greater than the maximum width
                        if (lineWidth > maxLineWidth)
                        {
                            string wrappedLine = WrapText(line, maxLineWidth, baseFont, 8);
                            string[] wrappedHeaderLines = wrappedLine.Split('\n');
                            foreach (string wrapLine in wrappedHeaderLines)
                            {
                                contentByte.BeginText();
                                contentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, wrapLine, headerXPos, currentY, 0);
                                contentByte.EndText();
                                currentY -= lineHeight;
                            }
                        }
                        else
                        {
                            contentByte.BeginText();
                            contentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, line, headerXPos, currentY, 0);
                            contentByte.EndText();
                            currentY -= lineHeight;
                        }


                    }
                    contentByte.AddImage(img);
                }
                stamper.Close();
                reader.Close();
                pdfReturnBytes = fs.ToArray();
                return pdfReturnBytes;

            }
        }
        public string WrapText(string text, int maxLineWidth, BaseFont font, float fontSize)
        {
            string[] words = text.Split(' ');
            StringBuilder wrappedText = new StringBuilder();
            string line = "";

            foreach (string word in words)
            {
                string testLine = line + (string.IsNullOrEmpty(line) ? "" : " ") + word;
                float lineWidth = font.GetWidthPoint(testLine, fontSize);

                if (lineWidth > maxLineWidth)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        wrappedText.AppendLine(line);
                    }

                    line = word;
                }
                else
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        line += " ";
                    }

                    line += word;
                }
            }

            if (!string.IsNullOrEmpty(line))
            {
                wrappedText.Append(line);
            }

            return wrappedText.ToString();
        }
    }
}
