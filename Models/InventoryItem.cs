using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetTracApp.Models
{
    public class InventoryItem
    {
        [Key]
        [Required(ErrorMessage = "Serial Number is required.")]
        [StringLength(255)]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vendor is required.")]
        [StringLength(255)]
        public string Vendor { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DeviceType { get; set; }

        [StringLength(255)]
        public string? HostName { get; set; }

        [StringLength(255)]
        public string? AssetTag { get; set; }

        [StringLength(255)]
        public string? PartID { get; set; }

        [StringLength(255)]
        public string? FutureLocation { get; set; }

        public DateTime? DateReceived { get; set; }  // Nullable

        [StringLength(255)]
        public string? CurrentLocation { get; set; }

        [StringLength(255)]
        public string? Status { get; set; }

        public bool BackOrdered { get; set; } = false;

        public string? Notes { get; set; }

        public string? ProductDescription { get; set; }

        public bool Ready { get; set; } = false;

        public bool LegacyDevice { get; set; } = false;

        public DateTime? Modified { get; set; }  // Nullable
        public DateTime? Created { get; set; }   // Nullable

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        public bool PendingDeletion { get; set; } = false;

        public bool DeletionApproved { get; set; } = false;
    }

}
