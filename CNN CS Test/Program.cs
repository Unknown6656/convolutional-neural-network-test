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

using res = CNN.Properties.Resources;

namespace CNN
{
    public static unsafe class Program
    {
        // "convolver.c", "convolveKernel"
        public static readonly Tuple<string, string> METHOD = new Tuple<string, string>("tests/invertcolors.c", "invert");
        public const PixelFormat PIXEL_FORMAT = PixelFormat.Format32bppArgb;

        public static int Main(string[] args)
        {
            try
            {
                const int SIZE = 500; // 2000;
                string fname = "img-" + DateTime.Now.Ticks;

                using (Bitmap srcimg = new Bitmap(SIZE, SIZE, PIXEL_FORMAT))
                using (Bitmap dstimg = new Bitmap(SIZE, SIZE, PIXEL_FORMAT))
                {
                    srcimg.ResizeDraw(res.trump, SIZE);

                    BitmapData srcdat = srcimg.LockBits(new Rectangle(0, 0, SIZE, SIZE), ImageLockMode.ReadOnly, PIXEL_FORMAT);
                    byte[] srcarr = new byte[srcdat.Stride * srcdat.Height];

                    Marshal.Copy(srcdat.Scan0, srcarr, 0, srcarr.Length);

                    DeviceGlobalMemory src = srcarr;
                    DeviceGlobalMemory dst = new byte[srcarr.Length];
                    DeviceGlobalMemory krnl = new int[] { -1, 0, 1,
                                                          -2, 0, 2,
                                                          -1, 0, 1 };
                    DeviceGlobalMemory wdh = new int[] { SIZE };

                    Kernel kernel = Kernel.Create(METHOD.Item2, File.ReadAllText(METHOD.Item1), src, dst, krnl, wdh);
                    Event evt = kernel.Execute(512, 512); // <-- somehow unable to process more than the first 512 bytes...

                    kernel.CommandQueue.Finish();

                    BitmapData dstdat = dstimg.LockBits(new Rectangle(0, 0, SIZE, SIZE), ImageLockMode.WriteOnly, PIXEL_FORMAT);

                    using (DeviceBufferStream dbs = new DeviceBufferStream(dst))
                    {
                        new UnmanagedReader(dbs).Read(srcarr, 0, srcarr.Length);

                        Marshal.Copy(srcarr, 0, dstdat.Scan0, srcarr.Length);
                    }

                    srcimg.UnlockBits(srcdat);
                    dstimg.UnlockBits(dstdat);
                    srcimg.Save(fname + "-org.png", ImageFormat.Png);
                    dstimg.Save(fname + ".png", ImageFormat.Png);

                    Console.WriteLine("Done, operation took {0}", Profiler.DurationSeconds(evt));
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("nope: {0}\n{1}\n{2}\n{3}", e.Message, e.StackTrace, e.HResult, e.Source);
                Console.ReadKey(true);
                return -1;
            }
        }

        public static void ResizeDraw(this Bitmap target, Bitmap src, int size)
        {
            using (Graphics g = Graphics.FromImage(target))
            {
                float r = src.Width / (float)src.Height;
                float w = r >= 1 ? size : size / r;

                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(src, 0, 0, w, w / r);
            }
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
