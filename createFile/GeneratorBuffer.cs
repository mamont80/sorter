using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace FileCreator
{
    public class GeneratorBuffer
    {
        private const int RecordsBlockCountLines = 10000;
        private const int MaxBlockStore = 20;

        private Queue<byte[]> Buffers = new Queue<byte[]>();
        private ManualResetEvent EventReady = new ManualResetEvent(false);
        private ManualResetEvent EventWork = new ManualResetEvent(false);
        public bool IsActive { get; internal set; } = false;
        public long CountLines = 0;
        
        public void Run(int threads)
        {
            if (IsActive) return;
            IsActive = true;
            EventWork.Set();
            for(int i = 0; i < threads; i++)
                ThreadPool.QueueUserWorkItem(Worker);
        }
        public void Close()
        {
            IsActive = false;
            EventWork.Set();
        }

        public byte[] GetNextChank()
        {
            while (true)
            {
                EventReady.WaitOne();
                lock (Buffers)
                {
                    var cnt = Buffers.Count;
                    if (cnt > MaxBlockStore) EventWork.Reset();
                    else EventWork.Set();

                    if (cnt > 0)
                    {
                        return Buffers.Dequeue();
                    }
                    else
                    {
                        EventReady.Reset();
                    }
                }
            }
        }
        private void Worker(object state) 
        {
            while (IsActive)
            {
                var b = DoCreateRandomLinesBlock();
                lock (Buffers)
                {
                    Buffers.Enqueue(b);
                    if (Buffers.Count > MaxBlockStore) EventWork.Reset();
                    EventReady.Set();
                }
                EventWork.WaitOne(1000);
            }
        }

        private byte[] DoCreateRandomLinesBlock()
        {
            var lst = RecordUtils.CreateRandomLines(RecordsBlockCountLines);
            Interlocked.Add(ref CountLines, lst.Count);
            return RecordUtils.LinesToTextBuffer(lst);
        }


    }
}
