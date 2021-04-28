using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gen
{
    public class ChunkController
    {
        public List<Chunk> ChunkList = new List<Chunk>();
        private List<Chunk> ChunkFiles = new List<Chunk>();
        public DiskThread Disk;
        public void Init(int count)
        {
            ChunkList = new List<Chunk>();
            for (int i = 0; i < count; i++)
            {
                Chunk c = new Chunk();
                c.Disk = Disk;
                c.NumberChunk = i;
                c.Open();
                ChunkList.Add(c);
            }
            ChunkFiles = new List<Chunk>(ChunkList);
            ChunkList.ForEach(c => c.Pop());
            ChunkList = ChunkList.Where(a => a.TopLine != null).OrderBy(t => t).ToList();
        }

        public void CloseAll()
        {
            ChunkFiles.ForEach(a => a.Close());
        }
        public void DeleteAll()
        {
            ChunkFiles.ForEach(a => a.DeleteFile());
        }

        public LinePointer GetNextLine()
        {
            if (ChunkList.Count == 0) return null;
            var c = ChunkList[0];
            ChunkList.RemoveAt(0);
            var r = c.TopLine;//очередная линия

            c.Pop();
            var newLine = c.TopLine;
            if (newLine != null)//заного вставляем
            {
                var index = ChunkList.BinarySearch(c);
                if (index < 0) index = -index - 1;
                if (index >= ChunkList.Count) ChunkList.Add(c);
                else ChunkList.Insert(index, c);
            }
            return r;
        }
    }
}
