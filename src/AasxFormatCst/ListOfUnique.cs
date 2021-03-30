using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public interface IUniqueness<T>
    {
        bool EqualsForUniqueness(T other);
    }

    public class ListOfUnique<T> : List<T> where T : IUniqueness<T>
    {
        public ListOfUnique() : base() { }
        public ListOfUnique(T[] arr) : base(arr) { }

        public T FindExisting(T other)
        {
            var exist = this.Find((x) => x.EqualsForUniqueness(other));
            return exist;
        }

        public void AddIfUnique(T other)
        {
            var exist = FindExisting(other);
            if (exist != null)
                return;
            this.Add(other);
        }        
    }
}
