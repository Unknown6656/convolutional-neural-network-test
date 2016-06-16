using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.IO;
using System;

using ManOCL.Internal;
using ManOCL.IO;
using ManOCL;

using bmpdata = global::System.Tuple<byte[], global::System.Drawing.Imaging.BitmapData>;
using res = global::CNN.Properties.Resources;

namespace CNN
{

    public static unsafe class Program
    {

        public static int Main(string[] args)
        {
            try
            {
                CLTest();

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("nope: {0}\n{1}\n{2}\n{3}", e.Message, e.StackTrace, e.HResult, e.Source);

                return -1;
            }
        }

        public static void CLTest()
        {
            const int SIZE = 2000;

            using (Bitmap srcimg = new Bitmap(SIZE, SIZE) { Palette = Get8BitGrayScale() })
            using (Bitmap dstimg = new Bitmap(SIZE, SIZE) { Palette = Get8BitGrayScale() })
            {
                srcimg.ResizeDraw(res.trump, SIZE);

                bmpdata srcdat = srcimg.LockBits(SIZE);
                bmpdata dstdat = dstimg.LockBits(SIZE);

                DeviceGlobalMemory src = srcdat.Item1;
                DeviceGlobalMemory dst = dstdat.Item1;
                DeviceGlobalMemory krnl = new int[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
                DeviceGlobalMemory wdh = new int[] { SIZE };

                Kernel kernel = Kernel.Create("convolveKernel", File.ReadAllText("convolver.c"), src, dst, krnl, wdh);
                Event evt = kernel.Execute(256, 256);

                kernel.CommandQueue.Finish();

                Console.WriteLine("Done, operation took {0}", Profiler.DurationSeconds(evt));

                srcimg.UnlockBits(srcdat.Item2);
                dstimg.UnlockBits(dstdat.Item2);
                dstimg.Save(DateTime.Now.Ticks + ".png", ImageFormat.Png);
            }
        }

        public static void ResizeDraw(this Bitmap target, Bitmap src, int size)
        {
            using (Graphics g = Graphics.FromImage(target))
            {
                float sc = Math.Max(size / src.Height, size / src.Width);
                float h = sc * src.Height;
                float w = sc * src.Width;
                float y = Math.Min((size - h) / 2, 0);
                float x = Math.Min((size - w) / 2, 0);

                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(src, x, y, w, h);
            }
        }

        public static bmpdata LockBits(this Bitmap bmp, int size)
        {
            BitmapData dat = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte[] arr = new byte[size * size];

            Marshal.Copy(dat.Scan0, arr, 0, size * size);

            return new bmpdata(arr, dat);
        }

        // C# only has 16Bit-Grayscale or 8Bit-Colors defined -- not 8Bit-Grayscale
        public static ColorPalette Get8BitGrayScale()
        {
            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
            ColorPalette mono = bmp.Palette;

            Color[] entries = mono.Entries;

            for (int i = 0; i < 0xff; i++)
                entries[i] = Color.FromArgb(i, i, i);

            return mono;
        }
    }
}
