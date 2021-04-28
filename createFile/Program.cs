using System;
using System.IO;


namespace FileCreator
{
    class Program
    {

        public static long size = 1_000 * 1024 * 1024L;
        public static string filename = null;
        public static string dir = "";//"d:\\7\\";
        public static int Threads = 1;
        static void Main(string[] args)
        {
            Threads = (int)Math.Round(Environment.ProcessorCount * 1.2);

            ReadParams(args);
            if (string.IsNullOrEmpty(filename))
            {
                Help();
                return;
            }
            Generate(args);
        }

        static void Help() 
        {
            Console.WriteLine("Flags:");
            Console.WriteLine("-size <number>      Size of the created file in gigabytes. (Default: 1Gb)");
            Console.WriteLine("-name <file name>   File name. (Default: 'file'");
            
        }

        /// <summary>
        /// Разбор аргументов запуска
        /// </summary>
        /// <param name="args"></param>
        static void ReadParams(string[] args)
        {
            int i = 0;
            while (i < args.Length)
            {
                var l = args[i].ToLower();
                if (l == "-name")
                {
                    filename = args[i + 1];
                    i++;
                }
                if (l == "-size")
                {
                    long gb = int.Parse(args[i + 1]);
                    size = gb * 1024L * 1024L * 1024L;
                    i++;
                }
                i++;
            }
        }

        static void Generate(string[] args)
        {

            DateTime dt = DateTime.Now;
            RecordUtils.CreateRandomDictonary(2000);

            GeneratorBuffer generator = new GeneratorBuffer();
            Console.WriteLine("Processors: " + Threads);
            generator.Run(Threads);
            using (FileStream fs = new FileStream(dir+filename, FileMode.Create))
            {
                //fs.Position = size - 1;
                //fs.Write(new byte[1]);
                //fs.Position = 0;
                long writed = 0;
                while (writed < size)
                {
                    var buf = generator.GetNextChank();
                    fs.Write(buf);
                    writed += buf.Length;
                }
            }
            Console.WriteLine($"Create: {Math.Round((DateTime.Now - dt).TotalSeconds)}sec");
            Console.WriteLine("Lines: " + generator.CountLines.ToString());
            generator.Close();
        }
    }

}
