using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SexprForLinq
{
    public class BindingException : Exception
    {
        public BindingException()
        {
        }

        public BindingException(string msg) : base(msg)
        {

        }

        public BindingException(string msg, Exception inner) : base(msg, inner)
        {

        }
    }
}
