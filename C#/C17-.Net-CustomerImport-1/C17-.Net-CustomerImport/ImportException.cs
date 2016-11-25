using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C17_.Net_CustomerImport
{
    public class ImportException : Exception
    {
        public ImportException(string message) : base(message)
        {
        }
    }
}
