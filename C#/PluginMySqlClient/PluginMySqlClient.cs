using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using MySql.Data.MySqlClient;
using Rainmeter;

namespace PluginMySqlClient
{
    class Measure : IDisposable
    {
        public string ConnectionString;
        public string SqlQuery;
        public int Interval;
        public double Value = -1.0d;
        private Rainmeter.API api;
        System.Timers.Timer timer;

        private void GetData()
        {
            //var connectionString = "Server=hellsgate.home.net;Port=3307;Database=meteo;Uid=meteo;Pwd=*******;";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(SqlQuery, connection)) //"SELECT Value FROM smog_p25 ORDER BY Date DESC LIMIT 1;"
                {
                    var result = command.ExecuteScalar();
                    try
                    {
                        Value = Convert.ToDouble(result, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exception)
                    {
                        api.Log(API.LogType.Error, exception.Message);
                        Value = -1.0d;
                    }
                    connection.Close();
                }
            }
        }


        public void Init(Rainmeter.API api)
        {
            this.api = api;
            try
            {
                ConnectionString = api.ReadString("ConnectionString", "")?.Trim();
                SqlQuery = api.ReadString("SqlQuery", "")?.Trim();
                Interval = api.ReadInt("Interval", 5000);
                api.Log(API.LogType.Notice, $"Connecting to MySql; ConnectionString={ConnectionString}; SqlQuery={SqlQuery}; Interval = {Interval}");
                GetData();

                timer = new System.Timers.Timer();
                timer.Elapsed += Timer_Elapsed;
                timer.Interval = Interval;
                timer.Enabled = true;
            }
            catch (Exception exception)
            {
                api.Log(API.LogType.Error, $"MySql query failed: {exception.Message}");
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                GetData();
            }
            catch (Exception exception)
            {
                api.Log(API.LogType.Error, $"MySql query failed: {exception.Message}");
            }
        }

        public void Dispose()
        {
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
            measure.Init(api);
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

