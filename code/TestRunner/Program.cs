
namespace TestRunner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using NUnit.Framework;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                NUnit.ConsoleRunner.Runner.Main(args);
            }
            catch
            {
                // breakpoint
                throw;
            }
        }
    }
}
