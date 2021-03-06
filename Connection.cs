using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;

namespace TalkToIOS
{
    class Connection
    {
        private static IiDeviceApi idevice;
        private static ILockdownApi lockdown;
        private static iDeviceHandle deviceHandle;
        private static LockdownClientHandle lockdownClientHandle;
        private static string deviceName;
        private static iDeviceConnectionHandle connectionHandle;
        private static bool receivingMessages = false;

        public static void Initialize()
        {
            NativeLibraries.Load();
        }

        public static void StartListening()
        {
            idevice = LibiMobileDevice.Instance.iDevice;
            lockdown = LibiMobileDevice.Instance.Lockdown;

            iDeviceEventCallBack eventCallback = EventCallback;
            iDeviceError iDeviceError = idevice.idevice_event_subscribe(EventCallback, new IntPtr());

            Console.WriteLine("Listening for connections:");
        }

        private static void EventCallback(ref iDeviceEvent devEvent, IntPtr data)
        {
                switch (devEvent.@event)
                {
                    case iDeviceEventType.DeviceAdd:
                        ConnectToDevice(devEvent.udidString);
                        break;
                    case iDeviceEventType.DeviceRemove:
                        break;
                    default:
                        return;
                }
        }

        private static void ConnectToDevice(string udid)
        {
            idevice.idevice_new(out deviceHandle, udid).ThrowOnError();
            lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownClientHandle, "Quamotion").ThrowOnError();
            lockdown.lockdownd_get_device_name(lockdownClientHandle, out deviceName).ThrowOnError();
            Console.WriteLine($"Device connected: {deviceName}");

            iDeviceError connectionError = idevice.idevice_connect(deviceHandle, 12345, out connectionHandle);
            if (connectionError != iDeviceError.Success)
            {
                Console.WriteLine($"Error connecting: {connectionError}");
            }
            else
            {
                Console.WriteLine($"Connected: {connectionError}");
            }

            ReceiveMessages();
            SendMessage("Windows confirms the connection.");
        }

        private static void SendMessage(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            uint sentBytes = 0;
            iDeviceError connectionSendError = idevice.idevice_connection_send(connectionHandle, bytes, (uint)bytes.Length, ref sentBytes);
            if (connectionSendError != iDeviceError.Success)
            {
                Console.WriteLine($"Error sending: {connectionSendError}");
            }
        }

        public static void ReceiveMessages()
        {
            if (receivingMessages)
            {
                return;
            } 
            else
            {
                receivingMessages = true;
            }

            // Receive data from connection
            byte[] receivedBytesBuffer = new byte[1024];
            uint numBytesReceived = 0;
            string decodedBytes = "";
            iDeviceError connectionReceiveError;

            Task.Run(() =>
            {
                while (true)
                {
                    connectionReceiveError = idevice.idevice_connection_receive(connectionHandle, receivedBytesBuffer, (uint)receivedBytesBuffer.Length, ref numBytesReceived);
                    if (connectionReceiveError != iDeviceError.Success)
                    {
                        Console.WriteLine($"Error receiving: {connectionReceiveError}");
                    }

                    if (numBytesReceived <= 0) continue;

                    decodedBytes = Encoding.UTF8.GetString(receivedBytesBuffer);
                    Console.WriteLine($"Received message: {decodedBytes}");

                    receivedBytesBuffer = new byte[1024];

                    SendMessage("Message was received by Windows");
                }

            });

            Console.WriteLine("Listening for messages:");
        }

        public static void DisposeHandles()
        {
            deviceHandle.Dispose();
            lockdownClientHandle.Dispose();
            connectionHandle.Dispose();
        }
    }
}
