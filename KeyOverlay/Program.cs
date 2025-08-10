using System;
using System.IO;
using System.Linq;

namespace KeyOverlay
{
    static class Program
    {
        private static void Main(string[] args)
        {
            AppWindow window;
            try
            {
                window = new(args.FirstOrDefault());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                using var sw = new StreamWriter("errorMessage.txt");
                sw.WriteLine(e);
                throw;
            }
            window.Run();
        }
    }
}
