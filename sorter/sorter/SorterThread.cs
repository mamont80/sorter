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
    public interface IFileProcess
    {
        void Process();
    }

    public class FileResult
    {
        public void Process()
        { 
        }
    }

    

    public class SorterThread
    {
        public int ChunckCount;
        public Stream ResultStream;

        private DiskThread Disk;
        private ChunkController controller;
        private List<LinePointer> lst = new List<LinePointer>();
        private long lstSize = 0;

        public void Process()
        {
            //Используется 2 основных потока и второстепенные:
            //1) в текущем происходит сортировка слиянием
            //2) отдельный поток занимается только дисковыми операциями DiskThread
            //3) второстемпенные потоки порождаютсмя по ходу дела и занимаются декомпрессией и разбором входящих данных и формированием блоков исходящих
            
            Disk = new DiskThread();
            Disk.Run();

            controller = new ChunkController();//буферизирует чтение отдельных чанков
            controller.Disk = Disk;

            //файл результата
            using (var fs = new FileStream(Program.dir + Program.result_filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 256, FileOptions.SequentialScan))
            using (BufferedStream bs = new BufferedStream(fs, 5 * 1024 * 1024))
            {
                ResultStream = bs;
                bs.Position = 0;
                controller.Init(ChunckCount);
                while (true)
                {
                    lock (controller)
                    {
                        var lp = controller.GetNextLine();
                        if (lp == null) break;
                        lstSize = lstSize + lp.Size();
                        lst.Add(lp);
                        //если накопилось много данных сбрасываем на диск
                        if (lstSize > Program.WriteResultMinSize) ResultFileFlush();
                    }
                }
                var r = ResultFileFlush();
                r.Wait();
                Disk.Stop();
            }
            controller.CloseAll();
            controller.DeleteAll();
        }
        public Task ResultFileFlush()
        {
            List<LinePointer> temp = lst;
            if (lst.Count == 0) return Task.CompletedTask;
            lst = new List<LinePointer>();
            lstSize = 0;
            return Task.Run(() => 
            {
                var buffers = RecordUtils.ArchiveLinesPointer(temp, false);
                WriteFileOperation wf = new WriteFileOperation();
                wf.Stage = this;
                wf.Buffers = buffers;
                Disk.Add(wf);
            });
        }
    }

}

