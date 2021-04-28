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
    
    public abstract class CustomFileOperation
    {
        public abstract void Process();
    }

    public class ReadFileOperation : CustomFileOperation
    {
        public Chunk ChunkData;
        public byte[] Buffer;
        public int Size;
        public ManualResetEvent Event;

        public bool IsFirstBlock;
        public bool IsEndStream;
        public override void Process()
        {
            IsFirstBlock = false;
            if (Program.UseCompress)
            {
                IsFirstBlock = (ChunkData.GeneralStream.Position == 0);
                byte[] bufLen = new byte[8];
                ChunkData.GeneralStream.Read(bufLen, 0, bufLen.Length);
                long len = BitConverter.ToInt64(bufLen, 0);
                Buffer = new byte[len];
                ChunkData.GeneralStream.Read(Buffer, 0, Buffer.Length);
                
                Size = (int)len;
                IsEndStream = (ChunkData.GeneralStream.Position == ChunkData.GeneralStream.Length);
            }
            else 
            {
                IsFirstBlock = (ChunkData.GeneralStream.Position == 0);
                Buffer = new byte[Program.FetchChunkSize];
                Size = ChunkData.GeneralStream.Read(Buffer, 0, Buffer.Length);
                IsEndStream = (ChunkData.GeneralStream.Position == ChunkData.GeneralStream.Length);
            }
            Event.Set();
        }
    }

    public class WriteFileOperation : CustomFileOperation
    {
        public SorterThread Stage;
        public List<MemoryStream> Buffers;

        public override void Process()
        {
            RecordUtils.WriteMemories(Stage.ResultStream, Buffers);
        }
    }

    public class DiskThread
    {
        private Queue<CustomFileOperation> Operations = new Queue<CustomFileOperation>();
        private ManualResetEvent HasOperationEvent = new ManualResetEvent(false);
        private bool Stoping = false;
        private Task Obj;

        public void Run()
        {
            Stoping = false;
            Obj = Task.Run(() =>
            {
                while (true)
                {
                    HasOperationEvent.WaitOne();
                    CustomFileOperation op = null;
                    lock (this)
                    {
                        if (Operations.Count > 0)
                            op = Operations.Dequeue();
                        else
                        {
                            if (Stoping) return;
                            HasOperationEvent.Reset();
                        }
                    }
                    if (op != null) op.Process();
                }
            });
        }

        public void Add(CustomFileOperation op)
        {
            lock (this) 
            {
                Operations.Enqueue(op);
                HasOperationEvent.Set();
            }
        }

        public void Stop()
        {
            lock (this)
            {
                Stoping = true;
                HasOperationEvent.Set();
            }
            Obj.Wait();
        }
    }
}
