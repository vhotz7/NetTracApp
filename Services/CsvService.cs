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

        public CsvService(ILogger<CsvService> logger)
        {
            _logger = logger;
        }

        public IEnumerable<InventoryItem> ReadCsvFile(Stream fileStream)
        {
            var records = new List<InventoryItem>();

            try
            {
                using (var reader = new StreamReader(fileStream))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,  
                    HeaderValidated = null,    
                    BadDataFound = context =>
                    {
                        _logger.LogWarning($"Bad data found: {context.RawRecord}");
                    }
                }))
                {
                    records = csv.GetRecords<InventoryItem>().ToList();
                }
            }
            catch (TypeConverterException ex)
            {
                _logger.LogError($"Data conversion error: {ex.Message}");
                
            }
            catch (CsvHelperException ex)
            {
                _logger.LogError($"CSV parsing error: {ex.Message}");
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                
            }

            return records;
        }
    }
}
