using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Common;

namespace SimpleCritters.Helpers
{
    public static class MiscHelper
    {
        public static IEnumerable<J> Flatten<J>(this IEnumerable<IEnumerable<J>> @this)
        {
            foreach (IEnumerable<J> js in @this)
                foreach (J j in js)
                    yield return j;
        }

        public static IEnumerable<T> Copy<T>(this T @this, int count)
        {
            for (int i = 0; i < count; i++)
                yield return @this;
        }
    }
}
