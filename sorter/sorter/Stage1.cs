using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace gen
{
    public class FileBlockInfo
    {
        public int NumFile;
        public bool IsFirstBlock;
        public byte[] Buffer;
        public int Size;
    }
    public class Stage1
    {
        private Stream bs;
        
        public int Process()
        {
            DateTime dt = DateTime.Now;
            DateTime allTime = DateTime.Now;
            
            

            ConcurrentBag<LinePointer> mem4 = new ConcurrentBag<LinePointer>();
            int tempFileCounter = 0;

            using (var fs = new FileStream(Program.dir + Program.filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 1024 * 256, FileOptions.SequentialScan))
            using (bs = new BufferedStream(fs, 2 * 1024 * 1024))
            {
                //определяем количество байт на перевод строки
                byte[] shortBuf = new byte[Program.MaxLengthLine];
                bs.Read(shortBuf, 0, shortBuf.Length);
                DetectEndLineLength(shortBuf);

                bs.Position = 0;
                //Загружаем первичные данные большим блоком, сортируем и пишем в отдельный файл


                //Конвеер при котором 
                //шаг 1) читается блок K+2
                //шаг 2) пишется блок K
                //в это время (1-2) параллельно считается блок K+1
                //тем самым CPU не простаивает, главный поток только либо читает, либо пишет, 
                //вычисления не блокируют IO и не мешают друг другу.
                //Task<List<MemoryStream>> taskCPU, taskRead2;
                FileBlockInfo fbiK2 = null;
                List<MemoryStream> resultK = null;
                Task<List<MemoryStream>> taskCPUK2 = null;
                Task<List<MemoryStream>> taskCPUK = null;
                do
                {
                    long blockSize = Program.BlockSize;
                    //if (tempFileCounter == 0) blockSize = blockSize / 4;//первый блок делаем меньше для быстрого старта
                    fbiK2 = this.ReadBlock(blockSize);
                    fbiK2.NumFile = tempFileCounter;
                    taskCPUK2 = Task.Run(() => this.CpuProcess(fbiK2));
                    if (taskCPUK != null)
                    {
                        taskCPUK.Wait();
                        resultK = taskCPUK.Result;
                        WriteBlock(resultK, tempFileCounter - 1);
                    }
                    taskCPUK = taskCPUK2;
                    tempFileCounter++;
                } while (bs.Position < bs.Length);

                taskCPUK.Wait();
                resultK = taskCPUK.Result;
                WriteBlock(resultK, tempFileCounter - 1);
            }
            return tempFileCounter;
        }

        private void WriteBlock(List<MemoryStream> bufs, int tempFileCounter)
        {
            RecordUtils.WriteFileMemories(tempFileCounter.ToString(), bufs, Program.UseCompress);
        }
        private FileBlockInfo ReadBlock(long blockSize)
        {
            byte[] buf = new byte[blockSize];
            //Console.WriteLine("alloc: " + Math.Round((DateTime.Now - dt).TotalMilliseconds) + "ms"); dt = DateTime.Now;
            bool isFistBlock = true;
            if (bs.Position > 0)
            {
                isFistBlock = false;
                bs.Position = bs.Position - Program.MaxLengthLine;
            }
            var size = bs.Read(buf, 0, buf.Length);
            FileBlockInfo r = new FileBlockInfo();
            r.Buffer = buf;
            r.IsFirstBlock = isFistBlock;
            r.Size = size;
            return r;
        }
        private List<MemoryStream> CpuProcess(FileBlockInfo fbi)
        {
            bool isFistBlock = fbi.IsFirstBlock;
            int size = fbi.Size;
            byte[] buf = fbi.Buffer;
            DateTime dt = DateTime.Now;
            int step = Math.Max(size / Program.Threads, Program.MinBytesPerThread);
            var chunks = ThreadUtils.BreakToBlocks(size, step, (isFistBlock ? 0 : Program.MaxLengthLine - 1));
            var linesArrays = chunks.AsParallel().Select(a =>
            {
                ParserLines pl = new ParserLines();
                pl.Buffer = buf;
                pl.StartIndex = a.First;
                pl.LastIndex = a.Last;
                pl.LineCreater = new StoreSimpleLines();
                pl.FindStart();
                return pl.ReadLines();
            }).ToList();
            List<LinePointer> pntLst = new List<LinePointer>();
            linesArrays.ForEach(a => pntLst.AddRange(a));

            pntLst = pntLst.AsParallel().OrderBy(t => t).ToList();

            return RecordUtils.ArchiveLinesPointer(pntLst, Program.UseCompress);
        }

        /// <summary>
        /// Определяет количество байт используемых для перевода на новую строку. 1 или 2.
        /// </summary>
        /// <param name="buf"></param>
        private void DetectEndLineLength(byte[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] == 10 || buf[i] == 13)
                {
                    i++;
                    if (buf[i] == 10 || buf[i] == 13) Program.EndLineLen = 2;
                    else Program.EndLineLen = 1;
                    return;
                }
            }
            throw new Exception("Line too long");
        }

    }
}
