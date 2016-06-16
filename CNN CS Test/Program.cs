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
        public static readonly Tuple<string, string> Method = new Tuple<string, string>("tests/invertcolors.c", "invert");

        public static int Main(string[] args)
        {
            try
            {
                const int SIZE = 2000;
                string fname = "img-" + DateTime.Now.Ticks;

                using (Bitmap srcimg = new Bitmap(SIZE, SIZE, PixelFormat.Format32bppArgb))
                using (Bitmap dstimg = new Bitmap(SIZE, SIZE, srcimg.PixelFormat))
                {
                    srcimg.ResizeDraw(res.trump, SIZE);

                    BitmapData srcdat = srcimg.LockBits(SIZE);
                    byte[] srcarr = new byte[srcdat.Stride * srcdat.Height];

                    Marshal.Copy(srcdat.Scan0, srcarr, 0, srcarr.Length);

                    DeviceGlobalMemory src = srcarr;
                    DeviceGlobalMemory dst = new byte[srcarr.Length];
                    DeviceGlobalMemory krnl = new int[] { -1, 0, 1,
                                                          -2, 0, 2,
                                                          -1, 0, 1 };
                    DeviceGlobalMemory wdh = new int[] { SIZE };

                    Kernel kernel = Kernel.Create(Method.Item2, File.ReadAllText(Method.Item1), src, dst, krnl, wdh);
                    Event evt = kernel.Execute(256, 256);

                    kernel.CommandQueue.Finish();

                    BitmapData dstdat = dstimg.LockBits(SIZE);

                    using (DeviceBufferStream dbs = new DeviceBufferStream(dst))
                    {
                        UnmanagedReader urd = new UnmanagedReader(dbs);

                        urd.Read(srcarr, 0, srcarr.Length);

                        byte* ptr = (byte*)dstdat.Scan0;

                        fixed (byte* sptr = srcarr)
                            for (int i = 0, l = srcarr.Length; i < l; i++)
                                ptr[i] = sptr[i];
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

        public static BitmapData LockBits(this Bitmap bmp, int size)
        {
            return bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadWrite, bmp.PixelFormat);
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
