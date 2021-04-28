using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace FileCreator
{
    public static class RecordUtils
    {
        /// <summary>
        /// Запись линий в текстовый формат
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        public static byte[] LinesToTextBuffer(List<LineRecord> records)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            foreach (var r in records)
            {
                string numStr = r.num.ToString() + ". ";
                bw.Write(Encoding.ASCII.GetBytes(numStr));
                for (int i = 0; i < r.str.Length; i++)
                {
                    byte b = r.str[i];
                    byte c = (byte)' ';
                    if (b > 0)
                    {
                        if (i == 0)//первую букву делаем заглавной
                        {
                            c = (byte)(b - 1 + ((int)'A'));
                        }
                        else
                        {
                            c = (byte)(b - 1 + ((int)'a'));
                        }
                    }

                    bw.Write(c);
                }
                bw.Write((byte)13);//перевод строки
                bw.Write((byte)10);//перевод строки
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Генерация случайных данных в линиях
        /// </summary>
        /// <param name="count">Количество линий</param>
        public static List<LineRecord> CreateRandomLines(int count)
        {
            Random rnd = new Random();
            List<LineRecord> lst = new List<LineRecord>();
            List<byte> prevStr = null;
            for (int i = 0; i < count; i++)
            {
                LineRecord r = new LineRecord();
                
                r.num = rnd.Next(0, 10000);
                if (i % 10 == 0 && prevStr != null)//каждая 10 строка повторяется
                {
                    r.str = prevStr.ToArray();
                }
                else
                {
                    int maxLen = rnd.Next(1, 500);
                    List<byte> q = new List<byte>(1000);
                    q.Add((byte)rnd.Next(1, 27));
                    //первая буква не пробел
                    //первые символы генерируем случайно для хорошего распределения,
                    //остальная строка составлена из заранее заготовленых "фраз" для ускорения
                    q.Add((byte)rnd.Next(0, 27));
                    q.Add((byte)rnd.Next(0, 27));
                    q.Add((byte)rnd.Next(0, 27));
                    q.Add((byte)rnd.Next(0, 27));
                    q.Add((byte)rnd.Next(0, 27));
                    while (q.Count < maxLen)
                    {
                        int idx = rnd.Next(0, RandomWords.Count);
                        var nextWord = RandomWords[idx];
                        if (q.Count + nextWord.Length > maxLen && q.Count > 0) break;
                        q.AddRange(nextWord);
                    }
                    r.str = q.ToArray();
                    prevStr = q;
                }
                lst.Add(r);
            }
            return lst;
        }

        public static List<byte[]> RandomWords = new List<byte[]>();
        /// <summary>
        /// Создание случайного словара фраз из нескольких слов. Используется в дальнейшем для ускорения создания случайных строк.
        /// </summary>
        /// <param name="words"></param>
        public static void CreateRandomDictonary(int words)
        {
            Random rnd = new Random();
            int minChars = 30;
            int maxChars = 100;
            for (int m = minChars; m < maxChars; m++)//по скольку букв в выражении
            {
                int cntWord = words / (maxChars - minChars);
                for(int i = 0; i < cntWord; i++)//сколько слов сгенерировать
                {
                    byte[] chars = new byte[m];
                    for (int c = 0; c < m; c++)
                    { 
                        chars[c] = (byte)rnd.Next(0, 27);//26 букв английского алфавита + пробел
                    }
                    RandomWords.Add(chars);
                }
            }
        }

    }
}
