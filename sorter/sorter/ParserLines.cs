using System;
using System.Collections.Generic;
using System.Text;

namespace gen
{
    public class ParserLines
    {
        public byte[] Buffer;
        public int StartIndex;//включительно
        public int LastIndex;//включительно
        public IStoreLines<LinePointer> LineCreater;

        public void FindStart()
        {
            if (Buffer[StartIndex] == 10 || Buffer[StartIndex] == 13)
            {
                while (StartIndex > 0 && (Buffer[StartIndex] == 10 || Buffer[StartIndex] == 13)) StartIndex--;
            }
            while (StartIndex > 0 && !(Buffer[StartIndex] == 10 || Buffer[StartIndex] == 13)) StartIndex--;
            if (Buffer[StartIndex] == 10 || Buffer[StartIndex] == 13) StartIndex++;
        }

        public List<LinePointer> ReadLines()
        {
            var res = new List<LinePointer>();
            bool findText = false;
            int startIndex = StartIndex;
            int textIndex = 0;
            int i = StartIndex;
            while (i <= LastIndex)
            {
                if (!findText && Buffer[i] == 0x20)
                {
                    textIndex = i + 1;
                    findText = true;
                }
                if (findText && (Buffer[i] == 0x0A || Buffer[i] == 0x0D))
                {
                    if (Program.EndLineLen > 1) 
                    {
                        if (i + 1 > LastIndex) { 
                            break; 
                        }
                    }
                    var lp = LineCreater.CreateObject();
                    lp.Buffer = Buffer;
                    lp.StartIndex = startIndex;
                    lp.TextIndex = textIndex;
                    lp.TextLength = i - textIndex;
                    res.Add(lp);
                    findText = false;
                    i = i + Program.EndLineLen;
                    startIndex = i;
                    continue;
                }
                i++;
            }
            return res;
        }

    }
}
