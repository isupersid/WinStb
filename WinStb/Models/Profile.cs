using System;

namespace WinStb.Models
{
    public class Profile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PortalUrl { get; set; }
        public string MacAddress { get; set; }
        public string SerialNumber { get; set; }
        public string DeviceId { get; set; }
        public string DeviceId2 { get; set; }
        public string Signature { get; set; }
        public string TimeZone { get; set; }
        public string StbType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }

        public Profile()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            TimeZone = "UTC";
            StbType = "MAG254";

            // Generate default MAC address in the correct format
            if (string.IsNullOrEmpty(MacAddress))
            {
                var random = new Random();
                MacAddress = $"00:1A:79:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}";
            }
        }
    }
}
