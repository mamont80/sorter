using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace gen
{

    public interface IStoreLines<T>
    {
        T CreateObject();
    }
    public class StoreSimpleLines : IStoreLines<LinePointer>
    {
        public LinePointer CreateObject()
        {
            return new LinePointer();
        }
    }


    public class StoreLines: IStoreLines<LinePointer>
    {
        public List<LinePointer> mem = new List<LinePointer>();
        private int _Index;
        public LinePointer CreateObject()
        {
            LinePointer r;
            if (_Index < 0)
            {
                r = new LinePointer();
                mem.Add(r);
                return r;
            }
            r = mem[_Index];
            _Index--;
            return r;
        }
        /// <summary>
        /// Помечаем все объекты как свободные
        /// </summary>
        public void ResetAll()
        {
            _Index = mem.Count - 1;
        }
    }

    public class StoreLinesConcurrent: IStoreLines<LinePointer>
    {
        public ConcurrentBag<LinePointer> mem;
        public LinePointer CreateObject()
        {
            LinePointer lp;
            if (mem.TryTake(out lp)) 
            {
                lp.Reset();
                return lp; 
            }
            return new LinePointer();
        }
        public void Free(LinePointer lp)
        {
            mem.Add(lp);
        }
    }

    

}
