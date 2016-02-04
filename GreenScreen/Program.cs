using System;
using System.Collections.Generic;
using System.Drawing;
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
                Directory.GetFiles(d).ToList().ForEach(f => ProcessFile(f));
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
                var output = new Bitmap(input.Width, input.Height);
                var pixels =
                    from x in Enumerable.Range(0, input.Width - 1)
                    from y in Enumerable.Range(0, input.Height - 1)
                    select new { x, y, v = input.GetPixel(x, y) };

                foreach (var p in pixels)
                {
                    byte max = Math.Max(Math.Max(p.v.R, p.v.G), p.v.B);
                    byte min = Math.Min(Math.Min(p.v.R, p.v.G), p.v.B);

                    if (p.v.G != min && (p.v.G == max || max - p.v.G < 7) && (max - min) > 20)
                        output.SetPixel(p.x, p.y, Color.Transparent);
                    else
                        output.SetPixel(p.x, p.y, p.v);
                }

                output.Save(o, System.Drawing.Imaging.ImageFormat.Png);

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
