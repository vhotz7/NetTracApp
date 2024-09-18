using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Logging;
using NetTracApp.Models;

namespace NetTracApp.Services
{
    public class CsvService
    {
        private readonly ILogger<CsvService> _logger;

        // constructor to inject a logger
        public CsvService(ILogger<CsvService> logger)
        {
            _logger = logger;
        }

        // method to read inventory items from a CSV file
        public IEnumerable<InventoryItem> ReadCsvFile(Stream fileStream)
        {
            var records = new List<InventoryItem>();

            try
            {
                using (var reader = new StreamReader(fileStream)) // open the file stream for reading
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null, // ignore missing fields
                    HeaderValidated = null,   // do not validate header
                    BadDataFound = context =>
                    {
                        // log a warning if bad data is found in the CSV file
                        _logger.LogWarning($"Bad data found: {context.RawRecord}");
                    }
                }))
                {
                    // read all records and convert them to a list of InventoryItem objects
                    records = csv.GetRecords<InventoryItem>().ToList();
                }
            }
            // handle exceptions during CSV data conversion
            catch (TypeConverterException ex)
            {
                _logger.LogError($"Data conversion error: {ex.Message}");
            }
            // handle general CSV parsing errors
            catch (CsvHelperException ex)
            {
                _logger.LogError($"CSV parsing error: {ex.Message}");
            }
            // handle any other unexpected errors
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
            }

            return records; // return the list of inventory items
        }
    }
}
