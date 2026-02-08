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

            var random = new Random();

            // Generate default MAC address in the correct format
            if (string.IsNullOrEmpty(MacAddress))
            {
                MacAddress = $"00:1A:79:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}";
            }

            // Generate default Serial Number (format: 12 uppercase alphanumeric characters)
            if (string.IsNullOrEmpty(SerialNumber))
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var serialChars = new char[12];
                for (int i = 0; i < 12; i++)
                {
                    serialChars[i] = chars[random.Next(chars.Length)];
                }
                SerialNumber = new string(serialChars);
            }

            // Generate default Device ID (format: 32 lowercase hex characters)
            if (string.IsNullOrEmpty(DeviceId))
            {
                DeviceId = Guid.NewGuid().ToString("N"); // 32 hex chars without dashes
            }

            // Generate default Device ID2 (format: 32 lowercase hex characters)
            if (string.IsNullOrEmpty(DeviceId2))
            {
                DeviceId2 = Guid.NewGuid().ToString("N"); // 32 hex chars without dashes
            }

            // Generate default Signature (format: 32 lowercase hex characters)
            if (string.IsNullOrEmpty(Signature))
            {
                Signature = Guid.NewGuid().ToString("N"); // 32 hex chars without dashes
            }
        }
    }
}
