using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gnucash2ledger_cli
{
    public class Transaction
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public IList<Entry> Entries { get; set; }
    }
}
