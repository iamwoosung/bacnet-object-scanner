
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.BACnet;
using System.Linq;
using System.Threading;
using BACnetScanner.LibClass;
using BACnetScanner.Models;

namespace BACnetScanner
{
    internal class Program
    {
        private static DateTime handlerTime = DateTime.Now;
        private static bool isRunning = true;
        private static int port = 0xBAC0;
        private static string localEndPointIP = string.Empty;

        private static SqliteDBService dbService = new SqliteDBService();
        private static LogControl logControl = new LogControl();

        private static BacnetClient bacnet_client;
        private static List<Device> bacnet_devices = new List<Device>();
        private static IList<BacnetValue> bacnet_tags = null;
        // 읽어들일 태그 속성들
        // 편집 가능하나, Tag 모델과 일치시켜야 함
        private static BacnetPropertyReference[] bacnet_properties = new BacnetPropertyReference[] {
            new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_OBJECT_NAME,   System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
            new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_PRESENT_VALUE, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
            new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_DESCRIPTION,   System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
        };
        public enum bacnet_object_types : uint
        {
            ANALOG_INPUT  = BacnetObjectTypes.OBJECT_ANALOG_INPUT,
            ANALOG_OUTPUT = BacnetObjectTypes.OBJECT_ANALOG_OUTPUT,
            ANALOG_VALUE  = BacnetObjectTypes.OBJECT_ANALOG_VALUE,
            BINARY_INPUT  = BacnetObjectTypes.OBJECT_BINARY_INPUT,
            BINARY_OUTPUT = BacnetObjectTypes.OBJECT_BINARY_OUTPUT,
            BINARY_VALUE  = BacnetObjectTypes.OBJECT_BINARY_VALUE,
        }



        static void Main(string[] args)
        {
            fnArgumentsParser(args);
            Trace.Listeners.Add(new ConsoleTraceListener());
            dbService.fnDBInit();
            fnStartActivity();
        }

        /// <summary>
        /// 대상 네트워크 정보(IP, 포트) 입력 받을 수 있도록 수정
        /// </summary>
        /// <param name="args">네트워크 정보[ 0: IP, 1: port ]</param>
        private static void fnArgumentsParser(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                return;
            }
            string ip = Utils.fnCheckIsIP(args[0]);
            localEndPointIP = ip == null ? localEndPointIP : ip;

            if (args.Length <= 1 || string.IsNullOrEmpty(args[1]))
            {
                return;
            }
            int p = Utils.fnCheckIsNumber(args[1]);
            port = p == -1 ? port : p;
        }

        /// <summary>
        /// 메인 프로세스
        /// </summary>
        private static void fnStartActivity()
        {
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(port: port, localEndpointIp: localEndPointIP));
            bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            bacnet_client.Start();
            bacnet_client.WhoIs();

            logControl.LogWrite("fnStartActivity", string.Format("BACnet client start {0}, {1}", localEndPointIP, port));

            while (isRunning)
            {
                // 60초 동안 핸들러 미발동 시 로직 종료
                // 핸들러 미발동 = 네트워크 상에서 추가 검색되는 디바이스가 없는 상태
                TimeSpan elapsedTime = DateTime.Now - handlerTime;
                if (elapsedTime.TotalSeconds > 60)
                {
                    isRunning = false;
                }

                fnReadTagsFromDevice();
                Thread.Sleep(1000);
            }
            logControl.LogWrite("fnStartActivity", "Normal shutdown(event handler not present)");
        }

        /// <summary>
        /// 백넷 서버로부터 수신된 Iam 이벤트 핸들러
        /// </summary>
        /// <param name="sender">백넷 클라이언트 정보</param>
        /// <param name="address">서버 주소</param>
        /// <param name="device_id">서버 Device(Instance) ID</param>
        /// <param name="max_apdu">APDU 최대 크기</param>
        /// <param name="segmentation">세그먼트 지원 여부</param>
        /// <param name="vendor_id">제조사 ID</param>
        private static void handler_OnIam(BacnetClient sender, BacnetAddress address, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
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
                else if (bacnet_devices.Any(device => device.address.Equals(address) && device.deviceId == device_id))
                {
                    return; 
                }

                bacnet_devices.Add(new Device(address, device_id));
                logControl.LogWrite("handler_OnIam", string.Format("Add device: count {0}, address {1}, deviceId {2}", bacnet_devices.Count, address.ToString(), device_id));
            }
        }

        /// <summary>
        /// 디바이스별 하위 태그 리스트 조회
        /// </summary>
        private static void fnReadTagsFromDevice()
        {
            foreach (Device device in bacnet_devices)
            {
                // 이미 스캔한 디바이스는 무시
                if (device.isCheck)
                {
                    continue;
                } 
                try { 
                    // 초기에 디바이스의 태그 개수를 조회한다.
                    // 태그 개수 조회를 실패하면 Range를 알 수 없기 때문에 해당 디바이스는 무시한다.
                    if (!bacnet_client.ReadPropertyRequest(device.address, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out bacnet_tags, 0, 0))
                    {
                        continue;
                    }

                    // Range만큼 조회를 반복한다.
                    int objectCount = Convert.ToInt32(bacnet_tags.First().Value);
                    for (uint i = 1; i < objectCount; i++)
                    {
                        // 응답없는 태그 무시
                        // 응답없는 태그만 다시 조회하는 로직을 추가할지는 검토 필요
                        if (!bacnet_client.ReadPropertyRequest(device.address, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out bacnet_tags, 0, i))
                        {
                            continue;
                        }

                        // Value Type에 따라 다른 방법으로 초기화해야 한다.
                        BacnetObjectId objectId = bacnet_tags.First().Value is BacnetObjectId ? (BacnetObjectId) bacnet_tags.First().Value 
                            : ((BacnetDeviceObjectPropertyReference) bacnet_tags.First().Value).objectIdentifier;

                        // 정의된 객체 타입만 Read
                        if (!Enum.IsDefined(typeof(bacnet_object_types), (uint) objectId.Type))
                        {
                            continue;
                        }

                        // 정의된 속성 Read하는데, 응답없는 태그 무시
                        IList<BacnetReadAccessResult> multiValueList;
                        if (!bacnet_client.ReadPropertyMultipleRequest(device.address, objectId, bacnet_properties, out multiValueList))
                        {
                            continue;
                        }

                        // 현재 디바이스의 태그 리스트에 추가
                        device.tags.Add(
                            new Tag(
                                objectId.instance, 
                                objectId.Type, 
                                multiValueList[0].values[0].value[0].ToString(),
                                multiValueList[0].values[1].value[0].ToString(),
                                multiValueList[0].values[2].value[0].ToString()
                            )
                        );
                    }

                    logControl.LogWrite("fnReadTagsFromDevice", string.Format("Success read tag from {0}, {1}", device.address.ToString(), device.deviceId));
                    device.isCheck = true;
                }
                catch (Exception e) 
                {
                    logControl.LogWrite("fnReadTagsFromDevice", string.Format("Error: {0}", e.Message));
                    continue;
                }
                fnSaveObjectToDB(device);
            }
        }

        /// <summary>
        /// 디바이스와 디바이스 하위 태그 DB 등록
        /// </summary>
        /// <param name="device">추가할 디바이스</param>
        private static void fnSaveObjectToDB(Device device)
        {
            // 디바이스 DB 삽입
            dbService.fnInsertDevice(device.deviceId, device.address.ToString());
            int deviceNo = dbService.fnSelectDeviceNo(device.deviceId, device.address.ToString());

            // 디바이스 조회 실패 시 중단
            if (deviceNo == -1)
            {
                return;
            }

            // 태그 리스트 DB 삽입
            foreach (Tag tag in device.tags)
            {
                dbService.fnInsertTag(deviceNo, tag.tagInstance, tag.tagType.ToString(), tag.tagName, tag.tagValue, tag.tagDesc);
            }
        }
    }
}
