using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxUtils
{
    public abstract class MultiTupleBase
    { }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTuple2<T> : MultiTupleBase
    {
        public T one;
        public MultiTuple2(T one)
        {
            this.one = one;
        }
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTuple2<T, U> : MultiTupleBase
    {
        public T one;
        public U two;
        public MultiTuple2(T one, U two)
        {
            this.one = one;
            this.two = two;
        }
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTuple3<T, U, V> : MultiTupleBase
    {
        public T one;
        public U two;
        public V three;
        public MultiTuple3(T one, U two, V three)
        {
            this.one = one;
            this.two = two;
            this.three = three;
        }
    }

    [JetBrains.Annotations.UsedImplicitly]
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
                if (key == null || !dict.ContainsKey(key))
                    return null;
                return dict[key];
            }
        }
    }
}
