using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenScreen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
                DisplayUsage();
            else
            {
                switch (args[0])
                {
                    case "-f":
                        ProcessFile(args[1]);
                        break;
                    case "-d":
                        ProcessDirectory(args[1]);
                        break;
                    default:
                        DisplayUsage();
                        break;
                }
            }
        }

        private static void ProcessDirectory(string d)
        {
            if (Directory.Exists(d))
            {
                Directory.GetFiles(d)
                    .AsParallel()
                    .ForAll(f => ProcessFile(f));
            }
            else
                DisplayUsage();
        }

        private static void ProcessFile(string f)
        {
            string o = f.Replace("." + f.Split('.').Last(), "_greenless.png");
            if (File.Exists(f))
            {
                Bitmap input = null;
                try
                {
                    input = new Bitmap(f);
                }
                catch
                {
                    Console.WriteLine("Error Processing: {0}", f);
                    return;
                }
                using (Bitmap clone = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb))
                {
                    using (input)
                    using (Graphics gr = Graphics.FromImage(clone))
                    {
                        gr.DrawImage(input, new Rectangle(0, 0, clone.Width, clone.Height));
                    }

                    var data = clone.LockBits(new Rectangle(0, 0, clone.Width, clone.Height), ImageLockMode.ReadWrite, clone.PixelFormat);

                    var bytes = Math.Abs(data.Stride) * clone.Height;
                    byte[] rgba = new byte[bytes];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgba, 0, bytes);

                    var pixels = Enumerable.Range(0, rgba.Length / 4).Select(x => new {
                        B = rgba[x * 4],
                        G = rgba[(x * 4) + 1],
                        R = rgba[(x * 4) + 2],
                        A = rgba[(x * 4) + 3],
                        MakeTransparent = new Action(() => rgba[(x * 4) + 3] = 0)
                    });

                    pixels
                        .AsParallel()
                        .ForAll(p =>
                    {
                        byte max = Math.Max(Math.Max(p.R, p.G), p.B);
                        byte min = Math.Min(Math.Min(p.R, p.G), p.B);

                        if (p.G != min && (p.G == max || max - p.G < 7) && (max - min) > 20)
                            p.MakeTransparent();
                    });

                    System.Runtime.InteropServices.Marshal.Copy(rgba, 0, data.Scan0, bytes);
                    clone.UnlockBits(data);

                    clone.Save(o, ImageFormat.Png);
                }
                Console.WriteLine("Finished Processing: {0} saved as {1}", f, o);
            }
            else
                DisplayUsage();
        }

        private static void DisplayUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("GreenScreen -f input.jpg");
            Console.WriteLine("GreenScreen -d c:\\temp\\files\\");
        }
    }
}
