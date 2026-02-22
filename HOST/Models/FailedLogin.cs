using System;

namespace HOST.Models
{
    public class FailedLogin
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
    }
}
