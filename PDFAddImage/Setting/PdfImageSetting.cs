using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFAddImage.Setting
{
    public class PdfImageSetting
    {
        public string SourceUrl { get; set; }
        public int LeftMargin { get; set; } = 0;
        public int RightMargin { get; set; } = 0;
        public int TopMargin { get; set; } = 0;

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }
}
