using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gnucash2ledger_cli
{
    public class Converter
    {
        private int _dateIndex;
        private int _descriptionIndex;
        private int _accountIndex;
        private int _debitIndex;
        private int _creditIndex;

        private HtmlNodeCollection _rows;
        private int _rowIndex;

        public void Convert(string inputFile, string outputFile)
        {
            var doc = new HtmlDocument();
            doc.Load(inputFile, Encoding.UTF8);

            ReadTable(doc);
            ParseHeader();

            using (var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false)))
            {
                Transaction t = null;
                while (true)
                {
                    try
                    {
                        t = ReadNextTransaction();
                        if (t == null)
                        {
                            break;
                        }

                        WriteTransaction(writer, t);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning! Transaction skipped: {0}", ex.Message);
                        continue;
                    }
                }
            }
        }

        private void ReadTable(HtmlDocument doc)
        {
            var table = doc.DocumentNode.SelectSingleNode("//table");
            if (table == null)
            {
                throw new Exception("Html <table> tag not found.");
            }

            _rows = table.SelectNodes("tr");
            _rowIndex = 0;
        }

        private void ParseHeader()
        {
            _dateIndex = _descriptionIndex = _accountIndex = _debitIndex = _creditIndex = -1;

            var cols = GetNextNonEmptyRow();
            for (int i = 0; i < cols.Length; i++)
            {
                switch (cols[i])
                {
                    case "Date": _dateIndex = i; break;
                    case "Description": _descriptionIndex = i; break;
                    case "Account": _accountIndex = i; break;
                    case "Debit": _debitIndex = i; break;
                    case "Credit": _creditIndex = i; break;
                }
            }

            if (_dateIndex == -1)
            {
                throw new Exception("Date column not found.");
            }
            if (_descriptionIndex == -1)
            {
                throw new Exception("Description column not found.");
            }
            if (_accountIndex == -1)
            {
                throw new Exception("Account column not found.");
            }
            if (_debitIndex == -1)
            {
                throw new Exception("Debit column not found.");
            }
            if (_creditIndex == -1)
            {
                throw new Exception("Credit column not found.");
            }
        }

        private Transaction ReadNextTransaction()
        {
            var t = new Transaction();

            string[] cols;
            do
            {
                cols = GetNextNonEmptyRow();
                if (cols == null)
                {
                    return null;
                }
            }
            while (string.IsNullOrEmpty(cols[_dateIndex]));
            
            t.Date = DateTime.Parse(cols[_dateIndex]);
            t.Description = FormatDescription(cols[_descriptionIndex]);

            t.Entries = new List<Entry>();
            while (true)
            {
                cols = GetNextNonEmptyRow();
                if (cols == null)
                {
                    break;
                }

                if (!string.IsNullOrEmpty(cols[_dateIndex]))
                {
                    GoToPreviousRow();
                    break;
                }

                var entry = new Entry();
                entry.Account = cols[_accountIndex];

                if (string.IsNullOrEmpty(cols[_debitIndex]) &&
                    string.IsNullOrEmpty(cols[_creditIndex]))
                {
                    throw new Exception(string.Format("Debit or credit is not provided at date: {0:d}.",
                        t.Date));
                }

                if (!string.IsNullOrEmpty(cols[_debitIndex]) &&
                    !string.IsNullOrEmpty(cols[_creditIndex]))
                {
                    throw new Exception(string.Format("Both debit and credit are provided at date: {0:d}.",
                        t.Date));
                }

                if (!string.IsNullOrEmpty(cols[_debitIndex]))
                {
                    entry.Amount = FormatAmount(cols[_debitIndex]);
                }
                else
                {
                    entry.Amount = "-" + FormatAmount(cols[_creditIndex]);
                }

                t.Entries.Add(entry);
            }

            return t;
        }

        private string FormatDescription(string value)
        {
            return value.Replace("\n", " ").Replace("\r", "");
        }

        private string FormatAmount(string value)
        {
            return value.Replace("\u00A0", "").Replace(",",".");
        }

        private string[] GetNextNonEmptyRow()
        {
            HtmlNode row;

            while ((row = GetNextRow()) != null)
            {
                var cols = row.SelectNodes("td|th");
                var res = new string[cols.Count];
                var isEmpty = true;
                for (int i = 0; i < cols.Count; i++)
                {
                    res[i] = cols[i].InnerText.Trim();
                    if (!string.IsNullOrEmpty(res[i]))
                    {
                        isEmpty = false;
                    }
                }

                if (isEmpty)
                {
                    continue;
                }

                return res;
            }

            return null;
        }

        private HtmlNode GetNextRow()
        {
            if (_rowIndex >= _rows.Count)
            {
                return null;
            }

            return _rows[_rowIndex++];
        }

        private void GoToPreviousRow()
        {
            _rowIndex--;
        }

        private void WriteTransaction(StreamWriter writer, Transaction t)
        {
            writer.WriteLine(string.Format("{0:d} {1}",
                t.Date, t.Description));
            foreach (var e in t.Entries)
            {
                writer.WriteLine("\t{0}\t{1}", e.Account, e.Amount);
            }
            writer.WriteLine();
        }
    }
}
