
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.BACnet;
using System.Linq;
using System.Threading;
using BACnetScanner.Models;

namespace BACnetScanner
{
    internal class Program
    {

        private static BacnetClient bacnet_client;
        private static List<Device> bacnet_devices = new List<Device>();
        private static IList<BacnetValue> bacnet_tags = null;
        private static DateTime handlerTime = DateTime.Now;

        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            fnStartActivity();
        }

        private static void fnStartActivity()
        {
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(port: 0xBAC0, localEndpointIp: ""));
            bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            bacnet_client.Start();
            bacnet_client.WhoIs();

            while (true)
            {
                // 60초 동안 핸들러 미발동 시 로직 종료
                // 핸들러 미발동 = 네트워크 상에서 추가 검색되는 디바이스가 없는 상태
                TimeSpan elapsedTime = DateTime.Now - handlerTime;
                if (elapsedTime.TotalSeconds > 60)
                {
                    break; 
                }

                fnReadTagsFromDevice();
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 백넷 서버로부터 수신된 I-am 이벤트 핸들러
        /// </summary>
        /// <param name="sender">백넷 클라이언트 정보</param>
        /// <param name="address">서버 주소</param>
        /// <param name="device_id">서버 Device(Instance) ID</param>
        /// <param name="max_apdu"></param>
        /// <param name="segmentation"></param>
        /// <param name="vendor_id"></param>
        static void handler_OnIam(BacnetClient sender, BacnetAddress address, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            // 핸들러 발동 시간 초기화
            handlerTime = DateTime.Now;

            // 디바이스 리스트 뮤텍스 처리
            lock (bacnet_devices)
            {
                // IP 네트워크로 등록된 디바이스만 검색
                if (address.type != BacnetAddressTypes.IP)
                {
                    return; 
                }
                // 동일 어드레스의 디바이스가 기등록된 상태면 무시
                else if (bacnet_devices.Any(device => device.address.Equals(address) && device.device_id == device_id))
                {
                    return; 
                }

                Debug.WriteLine(address.ToString() + " " + device_id);
                bacnet_devices.Add(new Device(address, device_id, false, null));
            }
        }
        static void fnReadTagsFromDevice()
        {
            foreach (Device device in bacnet_devices)
            {
                // 이미 스캔한 디바이스는 무시
                if (device.is_check)
                {
                    continue;
                } 
                try { 
                    // 초기에 디바이스의 태그 개수를 조회한다.
                    // 태그 개수 조회를 실패하면 Range를 알 수 없기 때문에 해당 디바이스는 무시한다.
                    if (!bacnet_client.ReadPropertyRequest(device.address, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out bacnet_tags, 0, 0))
                    {
                        continue;
                    }

                    // Range만큼 조회를 반복한다.
                    int objectCount = Convert.ToInt32(bacnet_tags.First().Value);
                    for (uint i = 1; i < objectCount; i++)
                    {
                        // 응답없는 index 무시
                        // 응답없는 것만 다시 조회하는 로직을 추가할지는 검토 필요
                        if (!bacnet_client.ReadPropertyRequest(device.address, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out bacnet_tags, 0, i))
                        {
                            continue;
                        }

                        BacnetObjectId objectId = bacnet_tags.First().Value is BacnetObjectId ? (BacnetObjectId)bacnet_tags.First().Value 
                            : ((BacnetDeviceObjectPropertyReference) bacnet_tags.First().Value).objectIdentifier;
                        

                        Debug.WriteLine(objectId.Type);
                    }
                    device.is_check = true;
                }
                catch (Exception e) 
                { 
                    Debug.WriteLine(e);
                    continue;
                }
            }
        }
    }
}
