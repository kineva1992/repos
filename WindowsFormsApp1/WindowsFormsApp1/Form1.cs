using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace test
{
    class OpenCV : IDisposable
    {
        IplImage gray;
        IplImage bina;

        public IplImage GrayScale(IplImage src)
        {
            gray = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.CvtColor(src, gray, ColorConversion.BgrToGray);
            return gray;
        }

        public IplImage BinarizerMethod(IplImage src)
        {
            bina = new IplImage(src.Size, BitDepth.U8, 1);
            gray = this.GrayScale(src);

            Binarizer.Nick(gray, bina, 61, 0.3);
            //Binarizer.Niblack(gray, bina, 61, -0.5);
            //Binarizer.NiblackFast(gray, bina, 61, -0.5);
            //Binarizer.Sauvola(gray, bina, 77, 0.2, 64);
            //Binarizer.SauvolaFast(gray, bina, 77, 0.2, 64);
            //Binarizer.Bernsen(gray, bina, 51, 60, 150);

            return bina;
        }

        public void Dispose()
        {
            if (gray != null) Cv.ReleaseImage(gray);
            if (bina != null) Cv.ReleaseImage(bina);
        }
    }
}