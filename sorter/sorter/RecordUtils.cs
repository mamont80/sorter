using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace gen
{
    public static class RecordUtils
    {
        public static List<MemoryStream> ArchiveLinesPointer(List<LinePointer> lines, bool archive)
        {
            var step = Math.Max(lines.Count / (Program.Threads * 2), Program.MinLinesPerThread);
            var lst = ThreadUtils.BreakToBlocks(lines.Count, step);
            var res = lst.AsParallel().AsOrdered().Select(b =>
            {
                MemoryStream ms = new MemoryStream();
                for (int i = b.First; i <= b.Last; i++)
                {
                    var l = lines[i];
                    var lenLine = l.TextLength + (l.TextIndex - l.StartIndex) + Program.EndLineLen;
                    ms.Write(l.Buffer, l.StartIndex, lenLine);
                }

                ms.Position = 0;
                if (archive) return new MemoryStream(CompressArray(ms.ToArray()));
                return ms;
            }).ToList();
            return res;
        }

        public static byte[] CompressArray(byte[] buffer)
        {
            using (MemoryStream dst = new MemoryStream())
            using (MemoryStream src = new MemoryStream(buffer))
            using (var compress = new GZipStream(dst, Program.CompressMax ? CompressionLevel.Optimal : CompressionLevel.Fastest))
            {
                src.CopyTo(compress);
                compress.Flush();
                return dst.ToArray();
            }
        }
        public static byte[] DeCompressArray(byte[] buffer)
        {
            using (MemoryStream src = new MemoryStream(buffer))
            using (MemoryStream dst = new MemoryStream())
            using (var decompress = new GZipStream(src, CompressionMode.Decompress))
            {
                decompress.CopyTo(dst);
                decompress.Flush();
                return dst.ToArray();
            }
        }

        public static void WriteFileMemories(string filename, List<MemoryStream> memories, bool useArchive)
        {
            using (var f = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
            {
                foreach (var s in memories)
                {
                    if (useArchive)
                    {
                        long len = s.Length;
                        byte[] buf = BitConverter.GetBytes(len);
                        f.Write(buf, 0, buf.Length);
                    }
                    s.Position = 0;
                    s.CopyTo(f);
                }
            }
        }
        public static void WriteMemories(Stream str, List<MemoryStream> memories)
        {
            foreach (var s in memories)
            {
                s.Position = 0;
                s.CopyTo(str);
            }
        }
    }

}
