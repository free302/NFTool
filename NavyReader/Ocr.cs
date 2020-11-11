using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;

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
        static Ocr()
        {
            float r = 0.3f, g = 0.59f, b = 0.11f;
            var f = new float[5][];
            f[0] = new float[] { r, r, r, 0, 0 };
            f[1] = new float[] { g, g, g, 0, 0 };
            f[2] = new float[] { b, b, b, 0, 0 };
            f[3] = new float[] { 0, 0, 0, 1, 0 };
            f[4] = new float[] { 0, 0, 0, 0, 1 };
            _cmGray = new ColorMatrix(f);

            f = new float[5][];
            f[0] = new float[] { 1, 0, 0, 0, 0 };
            f[1] = new float[] { 0, 1, 0, 0, 0 };
            f[2] = new float[] { 0, 0, 1, 0, 0 };
            f[3] = new float[] { 0, 0, 0, 1, 0 };
            f[4] = new float[] { 1, 1, 1, 0, 1 };
            _cmInversion = new ColorMatrix(f);
        }

        TesseractEngine _engine;
        public static Bitmap Capture((int sx, int xy) origin, (int w, int h) size)
        {
            Bitmap image = new Bitmap(size.w, size.h);
            //Bitmap image = new Bitmap(size.w, size.h, PixelFormat.Format16bppRgb555);
            Graphics g = Graphics.FromImage(image);
            g.CopyFromScreen(origin.sx, origin.xy, 0, 0, new Size(size.w, size.h));
            return image;
        }
        public (Bitmap image, Bitmap imgBw, string text) Process(Bitmap image, int level)
        {
            //var imgBw = toBW(image, level);
            var imgBw = toGray(image, 4, level);

            Pix pix = PixConverter.ToPix(imgBw);
            var result = _engine.Process(pix);
            var text = result.GetText();
            return (image, imgBw, text);
        }

        static Bitmap toBW(Bitmap image, int level)
        {
            var imgBw = new Bitmap(image, new Size(image.Width * 4, image.Height * 4));
            for (int y = 0; y < imgBw.Height; y++)
            {
                for (int x = 0; x < imgBw.Width; x++)
                {
                    var c = imgBw.GetPixel(x, y);
                    if (c.R < level && c.G < level && c.B < level) imgBw.SetPixel(x, y, Color.White);
                    else imgBw.SetPixel(x, y, Color.Black);
                }
            }
            return imgBw;
        }
        static ColorMatrix _cmGray;
        static ColorMatrix _cmInversion;
        static Bitmap toGray(Bitmap bitmap, int scale, int level)
        {
            using var imgS = new Bitmap(bitmap, new Size(bitmap.Width * scale, bitmap.Height * scale));//
            using var imgC = new Bitmap(imgS.Width, imgS.Height);//, PixelFormat.Format16bppGrayScale);
            for (int y = 0; y < imgS.Height; y++)
            {
                for (int x = 0; x < imgS.Width; x++)
                {
                    var c = imgS.GetPixel(x, y);
                    if (c.R < level && c.G < level && c.B < level) imgC.SetPixel(x, y, Color.White);
                    else imgC.SetPixel(x, y, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                }
            }

            var destRect = new Rectangle(0, 0, imgS.Width, imgS.Height);
            using var attributes = new ImageAttributes();

            //using var gC = Graphics.FromImage(imgC);
            //attributes.SetColorMatrix(_cmInversion);
            //gC.DrawImage(imgS, destRect, 0, 0, imgC.Width, imgC.Height, GraphicsUnit.Pixel, attributes);

            var imgG = new Bitmap(imgC.Width, imgC.Height);//, PixelFormat.Format16bppGrayScale);
            using var gG = Graphics.FromImage(imgG);
            attributes.SetColorMatrix(_cmGray);
            gG.DrawImage(imgC, destRect, 0, 0, imgG.Width, imgG.Height, GraphicsUnit.Pixel, attributes);
            return imgG;
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}
