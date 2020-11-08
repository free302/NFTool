using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using System.Drawing;

namespace NFT.NavyReader
{
    class Ocr : IDisposable
    {
        public Ocr()
        {
            _engine = new TesseractEngine(".", "eng", EngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", "0123456789");//_engine
            _engine.DefaultPageSegMode = PageSegMode.SingleBlock;
        }
        TesseractEngine _engine;        
        public Bitmap Capture((int sx, int xy) origin, (int w, int h) size)
        {
            var image = new Bitmap(size.w, size.h);
            Graphics g = Graphics.FromImage(image);
            g.CopyFromScreen(origin.sx, origin.xy, 0, 0, new Size(size.w, size.h));
            return image;
        }
        public (Bitmap image, Bitmap imgBw, string text) Process(Bitmap image, int level)
        {
            var imgBw = toBW(image, level);

            Pix pix = PixConverter.ToPix(imgBw);
            var result = _engine.Process(pix);
            var text = result.GetText();
            return (image, imgBw, text);
        }

        static Bitmap toBW(Bitmap image, int level)
        {
            var imgBw = new Bitmap(image, new Size(image.Width * 10, image.Height * 10));
            for (int i = 0; i < imgBw.Width; i++)
            {
                for (int j = 0; j < imgBw.Height; j++)
                {
                    var c = imgBw.GetPixel(i, j);
                    if (c.R < level && c.G < level && c.B < level) imgBw.SetPixel(i, j, Color.White);
                    else imgBw.SetPixel(i, j, Color.Black);
                }
            }

            return imgBw;
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}
