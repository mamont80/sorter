using System;
using System.IO;


namespace gen
{
    class Program
    {
        public static long BlockSize = 1_300_000_000;
        public static int FetchChunkSize = 20 * 1024 * 1024;//по сколько читать из чанка << Эту уменьшаем и глюки
        public static int WriteResultMinSize = 40 * 1024 * 1024;//по сколько читать из чанка

        public static string filename = "file";
        public static string result_filename = null;
        public static string dir = "";
        public static bool GenerateFileMode = false;
        public static bool SortFileMode = false;
        public static bool ShowInfo = false;
        public static int Threads = 1;
        public static bool UseCompress = true;//использовать компрессию промежуточных данных
        public static bool CompressMax = false;

        public static int EndLineLen = 1;//сколько байт перевод строки
        public static int MaxLengthLine = 1024+16;//максимальная длина строки

        public static int MinLinesPerThread = 100;
        public static int MinBytesPerThread = 10000;

        static void Main(string[] args)
        {
            ReadParams(args);
            if (!File.Exists(Program.filename))
            {
                Help();
                return;
            }
            if (string.IsNullOrEmpty(Program.result_filename)) Program.result_filename = Program.filename;
            Console.WriteLine("Input file: " + Program.filename);
            Console.WriteLine("Output file: " + Program.result_filename);
            Console.WriteLine("BlockSize: " + BlockSize/1024/1024);
            Console.WriteLine("Compress: " + UseCompress);
            if (UseCompress) Console.WriteLine("CompressMax: " + CompressMax);
            Threads = (int)Math.Round(Environment.ProcessorCount * 1.2);
            Sort(args);
        }

        static void Help() 
        {
            Console.WriteLine("Sort large file");
            Console.WriteLine("Flags:");
            Console.WriteLine("-input <file name>     Input file name. (Default: 'file')");
            Console.WriteLine("-output <file name>    File name. (Default: 'file2')");
            Console.WriteLine("-nocompress            Don't use compression");
            Console.WriteLine("-maxcompress           Maximum compress level");
            
        }

        static void Sort(string[] args)
        {
            DateTime dt = DateTime.Now;
            DateTime allTime = DateTime.Now;
            //разбиение на сортированные блоки
            Stage1 st = new Stage1();
            int tempFileCounter = st.Process();
            Console.WriteLine("Stage1: " + Math.Round((DateTime.Now - dt).TotalMilliseconds) + "ms");
            //слияние сортированых блоков
            dt = DateTime.Now;
            SorterThread st2 = new SorterThread();
            st2.ChunckCount = tempFileCounter;
            st2.Process();
            Console.WriteLine("Stage2: " + Math.Round((DateTime.Now - dt).TotalMilliseconds) + "ms");
            Console.WriteLine($"Total: {Math.Round((DateTime.Now - allTime).TotalMilliseconds)}ms ({Math.Round((DateTime.Now - allTime).TotalSeconds)})sec");
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
                if (l == "-gen") GenerateFileMode = true;
                if (l == "-sort") SortFileMode = true;
                if (l == "-info") ShowInfo = true;
                if (l == "-nocompress") UseCompress = false;
                if (l == "-maxcompress") CompressMax = true;
                if (l == "-input")
                {
                    Program.filename = args[i + 1];
                    i++;
                }
                if (l == "-output")
                {
                    Program.result_filename = args[i + 1];
                    i++;
                }
                i++;
            }
        }


    }

}
