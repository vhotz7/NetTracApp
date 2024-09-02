using System;
using System.ComponentModel.DataAnnotations;

namespace NetTracApp.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        public string Vendor { get; set; } = string.Empty;

        [Required]
        public string DeviceType { get; set; } = string.Empty;

        [Required]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        public string HostName { get; set; } = string.Empty;

        [Required]
        public string AssetTag { get; set; } = string.Empty;

        [Required]
        public string PartId { get; set; } = string.Empty;

        public string? FutureLocation { get; set; }

        [Required]
        public DateTime DateReceived { get; set; }

        public string? CurrentLocation { get; set; }

        [Required]
        public InventoryStatus Status { get; set; }
    }

    public enum InventoryStatus
    {
        Received,
        InRoute,
        SetToDelete
    }
}
