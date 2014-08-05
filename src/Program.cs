using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gnucash2ledger_cli
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 2)
                {
                    Console.WriteLine("Usage: gnucash2ledger_cli <input_file.html> <output_file.txt>");
                    return 1;
                }

                new Converter().Convert(args[0], args[1]);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
                return 2;
            }
        }
    }
}
