using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.API
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message) : base(message) { }
    }
    public class NoInternetException : Exception
    {
        public NoInternetException() : base() { }
    }
    public class SnaptrudeDownException : Exception
    {
        public SnaptrudeDownException() : base() { }
    }
}
