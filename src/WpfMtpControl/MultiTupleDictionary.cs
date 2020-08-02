using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxUtils
{
#if NOT
    public abstract class MultiTupleBase
    { }

    public class MultiTuple<T> : MultiTupleBase
    {
        public T one;
        public MultiTuple(T one)
        {
            this.one = one;
        }
    }

    public class MultiTuple<T,U> : MultiTupleBase
    {
        public T one;
        public U two;
        public MultiTuple(T one, U two)
        {
            this.one = one;
            this.two = two;
        }
    }

    public class MultiTuple<T, U, V> : MultiTupleBase
    {
        public T one;
        public U two;
        public V three;
        public MultiTuple(T one, U two, V three)
        {
            this.one = one;
            this.two = two;
            this.three = three;
        }
    }

    public class MultiTupleDictionary<KEY, MT>
    {
        private Dictionary<KEY, List<MT>> dict = new Dictionary<KEY, List<MT>>();

        public void Add(KEY key, MT mt)
        {
            if (dict.ContainsKey(key))
                dict[key].Add(mt);
            else
            {
                dict.Add(key, new List<MT>());
                dict[key].Add(mt);
            }
        }

        public bool ContainsKey(KEY key)
        {
            return dict.ContainsKey(key);
        }

        public List<MT> this[KEY key]
        {
            get
            {
                if (!dict.ContainsKey(key))
                    return null;
                return dict[key];
            }
        }
    }
#endif
}
