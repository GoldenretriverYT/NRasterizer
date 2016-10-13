﻿using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NRasterizer.CLI
{
    public class NRasterizerProgram
    {
        private void Draw(Raster raster, Typeface typeface, string text, int size)
        {
            new Rasterizer(typeface).Rasterize(text, size, raster, false);
        }

        private static Rectangle Entire(Bitmap b)
        {
            return new Rectangle(0, 0, b.Width, b.Height);
        }

        private static void BlitTo(Raster raster, Bitmap bitmap)
        {
            var b = bitmap;
            BitmapData bitmapData = null;
            try
            {
                bitmapData = b.LockBits(Entire(b), ImageLockMode.WriteOnly, b.PixelFormat);
                int si = 0;
                IntPtr di = bitmapData.Scan0;
                for (int y = 0; y < b.Height; y++) 
                {
                    Marshal.Copy(raster.Pixels, si, di, b.Width);
                    si += raster.Stride;
                    di += bitmapData.Stride;
                }
            }
            finally
            {
                if (bitmapData != null)
                {
                    b.UnlockBits(bitmapData);
                }
            }
        }

        private void Draw(FileInfo fontPath, FileInfo target) 
        {
            const int width = 200;
            const int height = 80;
            var raster = new Raster(width, height, width, 72);

            using (var input = fontPath.OpenRead())
            {
                var typeface = new OpenTypeReader().Read(input);
                Draw(raster, typeface, "cefhijl", 48);
            }

            using (Bitmap b = new Bitmap(width, height, PixelFormat.Format8bppIndexed))
            {
                BlitTo(raster, b);
                b.Save(target.FullName, ImageFormat.Png);
            }
        }

        public static void Main(string[] args)
        {
            string fontPath = args[0];
            string target = args[1];

            var program = new NRasterizerProgram();
            program.Draw(new FileInfo(fontPath), new FileInfo(target));
        }
    }
}