using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace gen
{
    public class LinePointer : IComparable<LinePointer>
    {
        public byte[] Buffer;
        public int StartIndex;
        public int TextIndex;
        public int TextLength;
        //разобранное число в начале строки
        private int _numberPrecalc = int.MinValue;


        public static long StatisticShortCompare = 0;
        public static long StatisticNumCompare = 0;

        public override string ToString()
        {
            if (Buffer == null) return "null";
            return Encoding.ASCII.GetString(Buffer, StartIndex, (TextIndex - StartIndex) + TextLength);
        }
        public void Reset()
        {
            _numberPrecalc = int.MinValue;
        }

        public int Size()
        {
            return TextLength + (TextIndex - StartIndex);
        }

        public int CompareTo(LinePointer other)
        {
            int r;
            int max = Math.Min(TextLength, other.TextLength);
            
            max = TextIndex + TextLength;
            for (int i1 = TextIndex, i2 = other.TextIndex; i1 < max; i1++, i2++) 
            {
                r = (this.Buffer[i1] - other.Buffer[i2]);
                if (r != 0)
                {
                    //Interlocked.Increment(ref StatisticShortCompare);
                    return r; 
                }
            }
            //Interlocked.Increment(ref StatisticNumCompare);
            if (TextLength == other.TextLength)
            {
                return this.GetNumber() - other.GetNumber();
            }
            else if (TextLength > other.TextLength) return 1;
            else return -1;
        }

        public int GetNumber()
        {
            if (_numberPrecalc != int.MinValue) return _numberPrecalc;
            //парсим число без выделения памяти в куче
            int r = 0;
            int m = 1;//множитель
            for (int i = TextIndex - 3;  i >= StartIndex; i--)
            {
                r = r + (Buffer[i] - 0x30) * m; // 0x30 = ASCII код '0'
                m = m * 10;
            }
            _numberPrecalc = r;
            return r;
        }
    }
}
