namespace PDFAddImage.Service
{
    public interface IPdfService
    {
        byte[] GetPdfIfExists(string pdfName);

        void SavePdf(byte[] logoAddedPdfBytes, string pdfName);
        byte[] AddLogo(byte[] pdfBytes);

        byte[] AddHeader(byte[] pdfBytes);

        string StringSha256Hash(string text);

        byte[] MergePdf(List<byte[]> pdfBytesToMerge);

        byte[] AddHeaderForMergedPdf(byte[] storedMergedPdfBytes);
        //List<byte[]> SplitPdf(byte[] pdfBytes, long maxFileSize);

        List<byte[]> SplitPDF(byte[] inputPdfBytes, int maxSizeInMB);

        byte[] ZipFiles(string[] fileNames, byte[][] fileContents);
    }
}
