using System;
using System.ComponentModel.DataAnnotations;

namespace NetTracApp.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        public string Vendor { get; set; }
        public string DeviceType { get; set; }
        public string SerialNumber { get; set; }
        public string HostName { get; set; }
        public string AssetTag { get; set; }
        public string PartID { get; set; }  
        public string FutureLocation { get; set; }
        public DateTime DateReceived { get; set; }
        public string CurrentLocation { get; set; }
        public string Status { get; set; }
        public bool BackOrdered { get; set; }
        public string Notes { get; set; }
        public string ProductDescription { get; set; }
        public bool Ready { get; set; }
        public bool LegacyDevice { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
