using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator
{
    public class LineRecord
    {
        /// <summary>
        /// число в начале строки
        /// </summary>
        public int num;
        /// <summary>
        /// текст после числа в начале строки. Буквы лежат в сжатом виде. Значения от 0 до 27. 
        /// Значение 0 - пробел, остальное символы английского алфавита
        /// </summary>
        public byte[] str;
    }
}
