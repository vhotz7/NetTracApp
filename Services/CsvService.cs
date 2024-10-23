using CsvHelper;
using CsvHelper.Configuration;
using NetTracApp.Models;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace NetTracApp.Services
{
    public class CsvService
    {
        public IEnumerable<InventoryItem> ReadCsvFile(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null,
                MissingFieldFound = null
            });

            var records = new List<InventoryItem>();

            while (csv.Read())
            {
                try
                {
                    var item = csv.GetRecord<InventoryItem>();

                    // Handle incorrect date formats gracefully
                    if (!DateTime.TryParse(csv.GetField("DateReceived"), out DateTime dateReceived))
                        item.DateReceived = null;  // Leave date blank if invalid
                    else
                        item.DateReceived = dateReceived;

                    if (!DateTime.TryParse(csv.GetField("Created"), out DateTime created))
                        item.Created = null;
                    else
                        item.Created = created;

                    records.Add(item);
                }
                catch
                {
                    // Skip rows with invalid data
                    continue;
                }
            }

            return records;
        }
    }
}
