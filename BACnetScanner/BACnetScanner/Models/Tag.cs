using System.IO.BACnet;

namespace BACnetScanner.Models
{
    public class Tag
    {
        public Tag(uint tagInstance, BacnetObjectTypes tagType, string tagName, string tagValue, string tagDesc)
        {
            this.tagInstance = tagInstance;
            this.tagType = tagType;
            this.tagName = tagName;
            this.tagValue = tagValue;
            this.tagDesc = tagDesc;
        }

        public uint tagInstance { get; set; }
        public BacnetObjectTypes tagType { get; set; }
        public string tagName { get; set; }
        public string tagValue { get; set; }
        public string tagDesc { get; set; }
    }
}
