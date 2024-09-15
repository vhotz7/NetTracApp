using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetTracApp.Models
{
    public class InventoryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Vendor { get; set; }

        [StringLength(255)]
        public string DeviceType { get; set; }

        [StringLength(255)]
        public string SerialNumber { get; set; }

        [StringLength(255)]
        public string HostName { get; set; }

        [StringLength(255)]
        public string AssetTag { get; set; }

        [StringLength(255)]
        public string PartID { get; set; }

        [StringLength(255)]
        public string FutureLocation { get; set; }

        public DateTime DateReceived { get; set; }

        [StringLength(255)]
        public string CurrentLocation { get; set; }

        [StringLength(255)]
        public string Status { get; set; }

        public bool BackOrdered { get; set; }

        public string Notes { get; set; }

        public string ProductDescription { get; set; }

        public bool Ready { get; set; }

        public bool LegacyDevice { get; set; }

        public DateTime Modified { get; set; }

        public DateTime Created { get; set; }

        [StringLength(255)]
        public string CreatedBy { get; set; }

        [StringLength(255)]
        public string ModifiedBy { get; set; }
    }
}
