using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace gen
{
    public class Chunk: IComparable<Chunk>
    {
        public LinePointer TopLine;
        public int NumberChunk;
        public DiskThread Disk;

        public Stream GeneralStream;
        private Stream _File;

        private Stack<LinePointer> Fetched1 = new Stack<LinePointer>();
        //объекты с блокировкой
        private bool EndStream = false;
        private Stack<LinePointer> Fetched2 = null;
        public Task TaskResult = null;

        public void Open()
        {
            _File = new FileStream(NumberChunk.ToString(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1024 * 256, FileOptions.SequentialScan);//| FileOptions.DeleteOnClose
            var BufStr = new BufferedStream(_File, 1 * 1024 * 1024);
            GeneralStream = BufStr;
            
            EndStream = false;

            ReadBlock();
        }
        public void Close()
        {
            _File.Dispose();
            _File = null;
        }

        public void DeleteFile()
        {
            File.Delete(NumberChunk.ToString());
        }

        private void DoProcess(int size, byte[] buffer, bool isFistBlock, bool endStream)
        {
            //разбор блока на отдельные строки
            byte[] buf = buffer;
            if (Program.UseCompress)
            {
                buf = RecordUtils.DeCompressArray(buffer);
                size = buf.Length;
                isFistBlock = true;
            }


            int step = Math.Max(size / Program.Threads, Program.MinBytesPerThread);
            var chunks = ThreadUtils.BreakToBlocks(size, step, (isFistBlock ? 0 : Program.MaxLengthLine - 1));
            var linesArrays = chunks.AsParallel().Select(a =>
            {
                if (a.First == a.Last) return new List<LinePointer>();
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
            pntLst.Reverse();
            var newStack = new Stack<LinePointer>(pntLst);

            lock (this)
            {
                this.Fetched2 = newStack;
                this.EndStream = endStream;
            }
        }

        private void ReadBlock()
        {
            this.TaskResult = Task.Run(() => 
            {
                ReadFileOperation rf = new ReadFileOperation();
                rf.ChunkData = this;
                //rf.Buffer = buf;
                rf.Event = new ManualResetEvent(false);
                Disk.Add(rf);
                rf.Event.WaitOne();
                bool endStream = rf.IsEndStream;
                bool isFirstBlock = rf.IsFirstBlock;

                DoProcess(rf.Size, rf.Buffer, isFirstBlock, endStream);
            });
        }

        public void Pop()
        {
            //Записи делятся на 2 кучки:
            //Fetched1 - текущая очередь записей
            //Fetched2 - следующая порция записей. Её стараемся запросить заранее, в надежде что когда кончится первая, вторая уже будет загружена и разобрана.
            //Если к моменту когда кончилась первая, вторая ещё не успела загрузиться, тогда ожидаем готовности.
            while (true)
            {
                if (Fetched1.Count > 0)
                {
                    TopLine = Fetched1.Pop();
                    return;
                }


                Task t = null;
                lock (this)
                {
                    if (Fetched2 != null)
                    {
                        Fetched1 = Fetched2;
                        Fetched2 = null;
                        if (this.TaskResult == null)
                        {
                            if (!EndStream) ReadBlock();
                        }
                        if (Fetched1.Count > 0)
                        {
                            TopLine = Fetched1.Pop();
                            return;
                        }

                        if (this.TaskResult == null)
                        {
                            TopLine = null;
                            return;
                        }
                        else t = this.TaskResult;
                    }
                    else
                    {
                        if (this.TaskResult == null)
                        {
                            if (!EndStream) ReadBlock();
                        }

                        if (this.TaskResult == null)
                        {
                            TopLine = null;
                            return;
                        }
                        else t = this.TaskResult;
                    }
                }

                t.Wait();
                lock (this)
                {
                    this.TaskResult = null;
                }
            }
        }
        
        public int CompareTo(Chunk other)
        {
            return TopLine.CompareTo(other.TopLine);
        }
    }
}
