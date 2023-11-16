using iTextSharp.text;
using iTextSharp.text.pdf;
using PDFAddImage.Setting;
using SkiaSharp;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace PDFAddImage.Service
{
    public class PdfFolderService : IPdfService
    {
        private readonly PdfImageSetting _setting;
        private readonly PdfStorageSetting _storage;
        private readonly HeaderSetting _headerSetting;
        private readonly HeaderIndexSetting _headerIndexSetting;

        private readonly string _headerLinePrintedFor;
        private readonly string _headerLineAddress;
        private readonly string _headerLine4;
        public PdfFolderService(PdfImageSetting setting, PdfStorageSetting storage, HeaderSetting headerSetting, HeaderIndexSetting headerIndexSetting)
        {
            _setting = setting;
            _storage = storage;
            _headerSetting = headerSetting;
            _headerLinePrintedFor = "ajay gupta, EBC Singar Nagar Office Gen Site";
            _headerLine4 = "SCC Online Web Edition: http://www.scconline.gen.in";
            _headerLineAddress = "EBC Publishing Pvt. Ltd., Lucknow.";
            _headerIndexSetting = headerIndexSetting;
        }

        public byte[] GetPdfIfExists(string pdfName)
        {
            string FolderPath = _storage.FolderPath;
            string FilePath = Path.Combine(FolderPath, pdfName);

            if (File.Exists(FilePath))
            {
                byte[] storedPdfBytes = File.ReadAllBytes(FilePath);
                return storedPdfBytes;
            }
            else
            {
                return null;
            }
        }

        public void SavePdf(byte[] logoAddedPdfBytes, string pdfName)
        {
            string FolderPath = _storage.FolderPath;
            bool exis = Directory.Exists(FolderPath);
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            string FilePath = Path.Combine(FolderPath, pdfName);
            File.WriteAllBytes(FilePath, logoAddedPdfBytes);
        }

        public byte[] AddHeader(byte[] pdfBytes)
        {
            var fontPath = Path.Combine(Environment.CurrentDirectory, "Font", "arial.ttf");
            byte[] pdfReturnBytes = null;
            using (var fs = new MemoryStream())
            {
                var reader = new PdfReader(pdfBytes);
                var stamper = new PdfStamper(reader, fs);

                string formattedDate = DateTime.Now.ToString("dddd, MMMM d, yyyy");
                string year = DateTime.Now.Year.ToString();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var pageSize = reader.GetPageSize(i);

                    float headerXPos = _setting.LeftMargin + _setting.ImageWidth + 10;
                    float headerYPos = pageSize.Height - _setting.TopMargin - 7;

                    //int maxLineWidth = Convert.ToInt32(pageSize.Width - (_setting.LeftMargin + _setting.ImageWidth + 10 + float.Parse(mr)));
                    int maxLineWidth = 396;
                    var contentByte = stamper.GetOverContent(i);

                    // Set the font and size for the header text
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    contentByte.SetFontAndSize(baseFont, 8);

                    string headerText = $"{_headerSetting.line1}\n{_headerSetting.line2}\n{_headerSetting.line3}\n{_headerSetting.line4}\n{_headerSetting.line5}\n{_headerSetting.line6}\n{_headerSetting.line7}";

                    // Split the header text into lines
                    string[] headerLines = headerText.Split('\n');

                    // Write each line of the header
                    float lineHeight = 10.5f; // Adjust the line height as needed
                    float currentY = headerYPos;
                    foreach (string line in headerLines)
                    {
                        var filledLine = string.Format(line, year, i, formattedDate, _headerLinePrintedFor, _headerLine4, _headerLineAddress);
                        float lineWidth = baseFont.GetWidthPoint(filledLine, 8);
                        // Check if the line width is greater than the maximum width
                        if (lineWidth > maxLineWidth)
                        {
                            string wrappedLine = WrapText(filledLine, maxLineWidth, baseFont, 8);
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
                            contentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, filledLine, headerXPos, currentY, 0);
                            contentByte.EndText();
                            currentY -= lineHeight;
                        }
                    }
                }
                stamper.Close();
                reader.Close();
                pdfReturnBytes = fs.ToArray();
                return pdfReturnBytes;
            }
        }

        public byte[] AddHeaderForMergedPdf(byte[] storedMergedPdfBytes)
        {
            var fontPath = Path.Combine(Environment.CurrentDirectory, "Font", "arial.ttf");
            byte[] pdfReturnBytes = null;
            using (var fs = new MemoryStream())
            {
                var reader = new PdfReader(storedMergedPdfBytes);
                var stamper = new PdfStamper(reader, fs);

                string formattedDate = DateTime.Now.ToString("dddd, MMMM d, yyyy");
                string year = DateTime.Now.Year.ToString();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var pageSize = reader.GetPageSize(i);
                    var contentByte = stamper.GetOverContent(i);

                    #region HeaderIndex
                    // Set the font and size for the digit
                    BaseFont digitFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    contentByte.SetFontAndSize(digitFont, _headerIndexSetting.indexFontSize);

                    string pageIndex = i.ToString(); // Adjust as needed
                    float digitWidth = digitFont.GetWidthPoint(pageIndex, _headerIndexSetting.indexFontSize);
                    float digitHeight = digitFont.GetAscentPoint(pageIndex, _headerIndexSetting.indexFontSize) - digitFont.GetDescentPoint(pageIndex, _headerIndexSetting.indexFontSize);

                    // Set the border color for the rectangle
                    contentByte.SetRgbColorStrokeF(0, 0, 0); // Black color

                    // Set the border width
                    float borderWidth = 1f; // Adjust as needed
                    contentByte.SetLineWidth(borderWidth);

                    // Set the position and dimensions of the rectangle
                    float rectWidth = _headerIndexSetting.rectangleBaseWidth + digitWidth; // Adjust as needed
                    float rectHeight = _headerIndexSetting.rectangleHeight; // Adjust as needed
                    float rectYPos = pageSize.Height - _setting.TopMargin - rectHeight - 0;  // Adjust as needed
                    float rectXPos = pageSize.Width - 100 - rectWidth;  // Adjust as needed

                    // Draw the rectangle with just the border
                    contentByte.Rectangle(rectXPos, rectYPos, rectWidth, rectHeight);
                    contentByte.Stroke();

                    // Calculate the middle point of the rectangle for index position
                    float middleX = rectXPos + rectWidth / 2;
                    float middleY = rectYPos + rectHeight / 2;

                    // Set the color for the digit
                    contentByte.SetRgbColorFillF(0, 0, 0); // Black color

                    // Write the digit in the middle of the rectangle
                    contentByte.BeginText();
                    contentByte.ShowTextAligned(PdfContentByte.ALIGN_CENTER, pageIndex, middleX, middleY - digitHeight / 2, 0);
                    contentByte.EndText();
                    #endregion

                    #region HeaderText
                    //HEADER TEXT

                    float headerXPos = _setting.LeftMargin + _setting.ImageWidth + 10;
                    float headerYPos = pageSize.Height - _setting.TopMargin - 7;

                    //int maxLineWidth = Convert.ToInt32(pageSize.Width - (_setting.LeftMargin + _setting.ImageWidth + 10 + float.Parse(mr)));
                    int maxLineWidth = 396 - Convert.ToInt32(rectWidth + 1);

                    // Set the font and size for the header text
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    contentByte.SetFontAndSize(baseFont, 8);
                    contentByte.SetRgbColorFillF(0, 0, 0);


                    string headerText = $"{_headerSetting.line1}\n{_headerSetting.line2}\n{_headerSetting.line3}\n{_headerSetting.line4}\n{_headerSetting.line5}\n{_headerSetting.line6}\n{_headerSetting.line7}";

                    // Split the header text into lines
                    string[] headerLines = headerText.Split('\n');

                    // Write each line of the header
                    float lineHeight = 10.5f; // Adjust the line height as needed
                    float currentY = headerYPos;
                    foreach (string line in headerLines)
                    {
                        var filledLine = string.Format(line, year, i, formattedDate, _headerLinePrintedFor, _headerLine4, _headerLineAddress);
                        float lineWidth = baseFont.GetWidthPoint(filledLine, 8);
                        // Check if the line width is greater than the maximum width
                        if (lineWidth > maxLineWidth)
                        {
                            string wrappedLine = WrapText(filledLine, maxLineWidth, baseFont, 8);
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
                            contentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, filledLine, headerXPos, currentY, 0);
                            contentByte.EndText();
                            currentY -= lineHeight;
                        }
                    }
                    #endregion
                }
                stamper.Close();
                reader.Close();
                pdfReturnBytes = fs.ToArray();
                return pdfReturnBytes;
            }
        }

        public byte[] AddLogo(byte[] pdfBytes)
        {
            var imagePath = Path.Combine(Environment.CurrentDirectory, "Image", "scconline.png");
            var imageToAdd = SKImage.FromEncodedData(imagePath);

            byte[] pdfReturnBytes = null;
            using (var fs = new MemoryStream())
            {
                var reader = new PdfReader(pdfBytes);
                var stamper = new PdfStamper(reader, fs);

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

                    img.SetAbsolutePosition(xPos, yPos);

                    var contentByte = stamper.GetOverContent(i);
                    contentByte.AddImage(img);
                }
                stamper.Close();
                reader.Close();
                pdfReturnBytes = fs.ToArray();
                return pdfReturnBytes;
            }
        }
        private string WrapText(string text, int maxLineWidth, BaseFont font, float fontSize)
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


        public byte[] MergePdf(List<byte[]> pdfBytesToMerge)
        {
            using (var ms = new MemoryStream())
            {
                var outputDocument = new Document();
                var writer = new PdfCopy(outputDocument, ms);
                outputDocument.Open();
                foreach (var doc in pdfBytesToMerge)
                {
                    var reader = new PdfReader(doc);
                    for (var i = 1; i <= reader.NumberOfPages; i++)
                    {
                        writer.AddPage(writer.GetImportedPage(reader, i));
                    }
                    writer.FreeReader(reader);
                    reader.Close();
                }
                writer.Close();
                outputDocument.Close();

                byte[] by = ms.ToArray();
                return by;

                //File.WriteAllBytes(Path.Combine(_storage.FolderPath, mergedPdfName), by);

            }

        }
        public string StringSha256Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }



        public List<byte[]> SplitPDF(byte[] inputPdfBytes, int maxSizeInMB)
        {
            List<byte[]> splitPdfList = new List<byte[]>();

            using (MemoryStream inputMemoryStream = new MemoryStream(inputPdfBytes))
            using (PdfReader pdfReader = new PdfReader(inputMemoryStream))
            {
                int totalPages = pdfReader.NumberOfPages;
                int currentPage = 1;

                while (currentPage <= totalPages)
                {
                    using (MemoryStream outputMemoryStream = new MemoryStream())
                    using (Document document = new Document())
                    using (PdfSmartCopy pdfCopy = new PdfSmartCopy(document, outputMemoryStream))
                    {
                        document.Open();

                        int currentPageSize = 0;

                        while (currentPageSize <= maxSizeInMB * 1024 * 1024 && currentPage <= totalPages)
                        {
                            document.NewPage();
                            PdfImportedPage importedPage = pdfCopy.GetImportedPage(pdfReader, currentPage);
                            pdfCopy.AddPage(importedPage);

                            currentPageSize = (int)outputMemoryStream.Length;

                            currentPage++;
                        }

                        document.Close();
                        splitPdfList.Add(outputMemoryStream.ToArray());
                    }
                }
            }

            return splitPdfList;
        }


        public byte[] ZipFiles(string[] fileNames, byte[][] fileContents)
        {
            if (fileNames == null || fileContents == null || fileNames.Length != fileContents.Length)
            {
                throw new ArgumentException("Invalid input parameters");
            }

            using (MemoryStream zipStream = new MemoryStream())
            {
                using (ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    for (int i = 0; i < fileNames.Length; i++)
                    {
                        string fileName = fileNames[i];
                        byte[] fileContent = fileContents[i];

                        var entry = zipArchive.CreateEntry(fileName);

                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(fileContent, 0, fileContent.Length);
                        }
                    }
                }

                return zipStream.ToArray();
            }
        }

    }
}
