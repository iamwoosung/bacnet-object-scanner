using System.Collections.Generic;
using System.IO.BACnet;

namespace BACnetScanner.Models
{
    public class Device
    {
        public Device(BacnetAddress address, uint device_id, bool is_check, List<Tag> tags)
        {
            this.address = address;
            this.device_id = device_id;
            this.is_check = is_check;
            this.tags = tags;
        }
        public BacnetAddress address {  get; set; }
        public uint device_id { get; set; }
        public bool is_check { get; set; }
        public List<Tag> tags { get; set; }
    }
}
