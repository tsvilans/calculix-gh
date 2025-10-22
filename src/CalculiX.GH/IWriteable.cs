using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    internal interface IWriteable
    {
        public void Write(TextWriter tw);
    }
}
