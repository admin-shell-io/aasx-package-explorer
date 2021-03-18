using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class ListOfUnique<T> : List<T> where T : CstIdObjectBase
    {
        public void AddIfUnique(T other)
        {
            var exist = this.Find((x) => x.Equals1(other));
            if (exist != null)
                return;
            this.Add(other);
        }        
    }
}
