using System;
using System.Collections.Generic;
using System.Text;

namespace gen
{
    public class WorkBorder
    {
        public int First;
        public int Last;
    }

    public static class ThreadUtils
    {
        /// <summary>
        /// Разбивает последовательность на отдельные блоки для параллельной обработки в тредах.
        /// Чтобы минимизировать количество переключений потоков и задачи небыли слишком маленькие
        /// Последний элемент может быть больше рекомендуемого.
        /// </summary>
        /// <param name="count">Количество элементов</param>
        /// <param name="blocks">По скольку штук разбивать</param>
        /// <returns></returns>
        public static List<WorkBorder> BreakToBlocks(int count, int blocks = 1000, int start = 0)
        {
            List<WorkBorder> borders = new List<WorkBorder>();
            int Step = blocks;
            int i = start;
            while (i < count)
            {
                int last = i + Step - 1;
                if (last > count - 1) last = count - 1;
                int next = i + Step * 2;
                if (next >= count) { 
                    last = count - 1;
                    borders.Add(new WorkBorder() { First = i, Last = last });
                    break;
                }
                borders.Add(new WorkBorder() { First = i, Last = last });
                i = i + Step;
            }
            return borders;
        }
    }
}
