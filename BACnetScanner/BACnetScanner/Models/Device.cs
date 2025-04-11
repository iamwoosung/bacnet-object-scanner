using System.Collections.Generic;
using System.IO.BACnet;

namespace BACnetScanner.Models
{
    public class Device
    {
        public Device(BacnetAddress address, uint deviceId)
        {
            this.address = address;
            this.deviceId = deviceId;
            this.isCheck = false;
            this.tags = new List<Tag>();
        }
        public BacnetAddress address {  get; set; }
        public uint deviceId { get; set; }
        public bool isCheck { get; set; }
        public List<Tag> tags { get; set; }
    }
}
