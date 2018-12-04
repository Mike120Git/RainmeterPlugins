using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using MQTTnet;
using MQTTnet.Client;
using Rainmeter;

namespace PluginMqttClient
{
    class Measure : IDisposable
    {
        public string Topic;
        public string Server;
        private IMqttClient mqttClient;
        public double Value = -1.0d;
        private Rainmeter.API api;

        public void Connect(Rainmeter.API api)
        {
            this.api = api;
            try
            {
                Server = api.ReadString("Server", "")?.Trim();
                Topic = api.ReadString("Topic", "")?.Trim();
                api.Log(API.LogType.Notice, $"Connecting to MQTT; Server={Server}; Topic={Topic}");
                var factory = new MqttFactory();
                mqttClient = factory.CreateMqttClient();
                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer(Server).Build();
                mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();
                mqttClient.SubscribeAsync(Topic).GetAwaiter().GetResult();
                mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;
            }
            catch (Exception exception)
            {
                api.Log(API.LogType.Error, $"MQTT connection failed: {exception.Message}");
            }
        }

        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            api.Log(API.LogType.Notice, $"Message received; Message={message}; Topic={e.ApplicationMessage.Topic}");

            try
            {
                Value = Convert.ToDouble(message, CultureInfo.InvariantCulture);                
            }
            catch (Exception exception)
            {
                api.Log(API.LogType.Error, exception.Message);
                Value = -1.0d;
            }

        }

        public void Dispose()
        {
            if (mqttClient != null)
            {
                mqttClient.Dispose();
                mqttClient = null;
            }
        }

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
        public IntPtr buffer = IntPtr.Zero;
    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            var measure = new Measure();
            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
            var api = (Rainmeter.API)rm;
            measure.Connect(api);
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            measure.Dispose();

            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
            }
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)data;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)data;
            //api.Log(API.LogType.Notice, $"Update; Value={measure.Value}; Topic={measure.Topic}");
            return measure.Value;
        }

        //[DllExport]
        //public static IntPtr GetString(IntPtr data)
        //{
        //    Measure measure = (Measure)data;
        //    if (measure.buffer != IntPtr.Zero)
        //    {
        //        Marshal.FreeHGlobal(measure.buffer);
        //        measure.buffer = IntPtr.Zero;
        //    }
        //
        //    measure.buffer = Marshal.StringToHGlobalUni("");
        //
        //    return measure.buffer;
        //}

        //[DllExport]
        //public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args)
        //{
        //    Measure measure = (Measure)data;
        //}

        //[DllExport]
        //public static IntPtr (IntPtr data, int argc,
        //    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        //{
        //    Measure measure = (Measure)data;
        //    if (measure.buffer != IntPtr.Zero)
        //    {
        //        Marshal.FreeHGlobal(measure.buffer);
        //        measure.buffer = IntPtr.Zero;
        //    }
        //
        //    measure.buffer = Marshal.StringToHGlobalUni("");
        //
        //    return measure.buffer;
        //}
    }
}

