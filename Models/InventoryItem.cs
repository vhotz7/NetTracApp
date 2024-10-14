using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetTracApp.Models
{
    public class InventoryItem
    {
        [Key] // marks this property as the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // auto-generates the ID value
        public int Id { get; set; }

        [Required] // makes this field mandatory
        [StringLength(255)] // sets maximum length for the vendor name
        public string Vendor { get; set; }

        [StringLength(255)] // sets maximum length for the device type
        public string DeviceType { get; set; }

        [StringLength(255)] // sets maximum length for the serial number
        public string SerialNumber { get; set; }

        [StringLength(255)] // sets maximum length for the host name
        public string HostName { get; set; }

        [StringLength(255)] // sets maximum length for the asset tag
        public string AssetTag { get; set; }

        [StringLength(255)] // sets maximum length for the part ID
        public string PartID { get; set; }

        [StringLength(255)] // sets maximum length for the future location
        public string FutureLocation { get; set; }

        public DateTime DateReceived { get; set; } // stores the date the item was received

        [StringLength(255)] // sets maximum length for the current location
        public string CurrentLocation { get; set; }

        [StringLength(255)] // sets maximum length for the status
        public string Status { get; set; }

        public bool BackOrdered { get; set; } // indicates if the item is backordered

        public string Notes { get; set; } // stores additional notes or comments

        public string ProductDescription { get; set; } // stores the product description

        public bool Ready { get; set; } // indicates if the item is ready for use

        public bool LegacyDevice { get; set; } // indicates if the item is a legacy device

        public DateTime Modified { get; set; } // stores the last modified date

        public DateTime Created { get; set; } // stores the created date

        [StringLength(255)] // sets maximum length for the created by field
        public string CreatedBy { get; set; }

        [StringLength(255)] // sets maximum length for the modified by field
        public string ModifiedBy { get; set; }

        public bool PendingDeletion { get; set; } = false;
        public bool DeletionApproved { get; set; } = false;
    }
}
