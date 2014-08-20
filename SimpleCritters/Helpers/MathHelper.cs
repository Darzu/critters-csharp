using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCritters.Helpers
{
    public static class MathHelper
    {
        public static int Wrap(int val, int max, int min = 0)
        {
            //ToDo(DZ): Implement more effeciently
            while (val >= max)
                val -= max - min;
            while (val < min)
                val += max - min;

            return val;
        }
        public static float Wrap(float val, float max, float min = 0)
        {
            while (val >= max)
                val -= max - min;
            while (val < min)
                val += max - min;

            return val;
        }
        public static readonly Random Random = new Random();
    }
}
