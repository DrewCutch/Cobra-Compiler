using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Util
{
    class ListByElementComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> x, List<T> y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
                return false;

            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<T> list)
        {
            int hashcode = 0;
            foreach (T element in list)
            {
                hashcode ^= element.GetHashCode();
            }

            return hashcode;
        }
    }
}
