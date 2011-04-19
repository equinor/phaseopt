using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Softing.OPCToolbox.Client;
using Softing.OPCToolbox;

namespace OPC_Test
{
    public class Testing
    {
        public class UMR_DLL
        {
            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "CCDB", CallingConvention = CallingConvention.Winapi)]
            public static extern void Criconden_Bar(ref int NC, int[] ID,
                double[] XY, ref double T, ref double P);

            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "CCDT", CallingConvention = CallingConvention.Winapi)]
            public static extern void Criconden_Term(ref int NC, int[] ID,
                double[] XY, ref double T, ref double P);

            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "BUBP", CallingConvention = CallingConvention.Winapi)]
            public static extern void Bubble_Point_Bar(ref int NC, int[] ID,
                double[] XY, ref double T, ref double P);

            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "BUBT", CallingConvention = CallingConvention.Winapi)]
            public static extern void Bubble_Point_Term(ref int NC, int[] ID,
                double[] XY, ref double T, ref double P);

            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "DEWP", CallingConvention = CallingConvention.Winapi)]
            public static extern void Dew_Point_Bar(ref int NC, int[] ID,
                double[] XY, ref double T, ref double P);

            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "DEWT", CallingConvention = CallingConvention.Winapi)]
            public static extern void Dew_Point_Term(ref int NC, int[] ID,
                double[] XY, ref double T, ref double P);

        }

        private static void Normalize(double[] Array, double Target)
        {
            double Sum = 0.0;

            foreach (double Value in Array)
            {
                Sum += Value;
            }

            double Factor = Target / Sum;

            for (int i=0; i < Array.Length; i++)
            {
                Array[i] = Array[i] * Factor;
            }
        }


        public static void Main(String[] args)
        {
            DateTime Start_Time_Stamp = System.DateTime.Now;
            string[] Item_Ids = new string[0];
            Int32[] IDs = new Int32[0];
            double[] Scale_Factors = new double[0];
            double Temperature = 250;
            double Pressure = 110;
            ValueQT[] Values;
            int[] Results;
            string Log_File_Path = @"cri.log";
            int Log_File_Lines = 0; 
            string Config_File_Path = @"cri.conf";

            // Read config file.
            try
            {
                string[] Config_File = System.IO.File.ReadAllLines(Config_File_Path);
                string Pattern = @"^\s*(\d+?)\s*;\s*([^#]+)\s*;\s*([\d\.^#]+)\s*#*.*$";

                int n = 0;
                foreach (string Line in Config_File)
                {
                    foreach (Match match in Regex.Matches(Line, Pattern, RegexOptions.None))
                    {
                        //Console.WriteLine("|{0}|{1}|{2}|", match.Groups[1].Value.Trim(),
                        //    match.Groups[2].Value.Trim(), match.Groups[3].Value.Trim());
                        n++;
                        Array.Resize(ref Item_Ids, n);
                        Array.Resize(ref IDs, n);
                        Array.Resize(ref Scale_Factors, n);
                        Item_Ids[n - 1] = match.Groups[2].Value.Trim();
                        IDs[n - 1] = System.Convert.ToInt32(match.Groups[1].Value.Trim());
                        Scale_Factors[n - 1] = System.Convert.ToDouble(match.Groups[3].Value.Trim(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
                System.Console.WriteLine("Error: Could not open configuration file. Exiting.");
                System.Environment.Exit(1);
            }

            System.Console.WriteLine("Opening log file.");
            System.IO.StreamWriter SW;
            SW = System.IO.File.AppendText(Log_File_Path);

            Application OPC_Application = Application.Instance;
            OPC_Application.Initialize();

            // creates a new DaSession object and adds it to the OPC_Application
            DaSession OPC_Session = new DaSession("opcda://localhost/Siemens.MaxumOPCServer.1");

            // sets the execution options
            ExecutionOptions Execution_Options = new ExecutionOptions();
            Execution_Options.ExecutionType = EnumExecutionType.SYNCHRONOUS;

            DaSubscription OPC_Subscription = new DaSubscription(500, OPC_Session);

            DaItem[] Item_List = new DaItem[Item_Ids.Length];
            int j = 0;
            foreach (string Item_Id in Item_Ids)
            {
                Item_List[j] = new DaItem(Item_Id, OPC_Subscription);
                j += 1;
            }

            System.Console.WriteLine("Connecting to OPC server.");
            // connects object to the server
            int Connect_Result = OPC_Session.Connect(true, true, Execution_Options);

            if (ResultCode.SUCCEEDED(Connect_Result))
            {
                System.Console.WriteLine("Connected to OPC server");
            }
            else
            {
                System.Console.WriteLine("Connection failed.");
            }


            // reads the items using OPC_Session object
            int Read_Result = OPC_Subscription.Read(
                0, // the values are read from the device
                Item_List,
                out Values,
                out Results,
                Execution_Options);

            OPC_Session.Disconnect(Execution_Options);

            double Sum = 0.0;

            if (ResultCode.SUCCEEDED(Read_Result))
            {
                System.Console.WriteLine("Read components values from OPC server");
                Int32 Components = Values.Length;
                double[] Component_Values = new double[Components];
                int i = 0;

                SW.WriteLine(Start_Time_Stamp); Log_File_Lines += 1;

                foreach (Int32 ID in IDs)
                {
                    SW.WriteLine(ID); Log_File_Lines += 1;
                }

                foreach (ValueQT V in Values)
                {
                    if (V.Quality == EnumQuality.GOOD)
                    {
                        double Value = System.Convert.ToDouble(V.Data) * Scale_Factors[i];
                        System.Console.WriteLine(V.TimeStamp.ToLocalTime().ToString());
                        if (Value < 1.0E-6)
                        {
                            Component_Values[i] = 1.0E-6;
                        }
                        else
                        {
                            Component_Values[i] = Value;
                        }
                        Sum += Component_Values[i];
                        i += 1;
                    }
                }

                System.Console.WriteLine("Sum: {0}", Sum.ToString());

                Testing.Normalize(Component_Values, 1.0);

                Sum = 0.0;
                foreach (double Val in Component_Values)
                {
                    Sum += Val;
                    System.Console.WriteLine(Val.ToString());
                    SW.WriteLine(Val.ToString()); Log_File_Lines += 1;
                }

                System.Console.WriteLine("Sum after normalization: {0}", Sum.ToString());

                // Read initial values for pressure and temperature.
                OPC_Session = new DaSession("opcda://localhost/Tunneller:KA-WP15:ABB.800xA DA Connect IHGW");
                OPC_Subscription = new DaSubscription(500, OPC_Session);
                Item_List = new DaItem[2];

                Item_List[0] = new DaItem("ANALYSE.CRICONDENBAR", OPC_Subscription);
                Item_List[1] = new DaItem("ANALYSE.CRICONDENBARTEMPERATURE", OPC_Subscription);

                Connect_Result = OPC_Session.Connect(true, true, Execution_Options);
                if (ResultCode.SUCCEEDED(Connect_Result))
                {
                    System.Console.WriteLine("Connected to OPC server, result.");
                    Read_Result = OPC_Subscription.Read(
                        0, // the values are read from the device
                        Item_List,
                        out Values,
                        out Results,
                        Execution_Options);

                    if (ResultCode.SUCCEEDED(Read_Result))
                    {
                        // Check that the initial values are within a reasonable range.
                        double P = System.Convert.ToDouble(Values[0].Data);
                        double T = System.Convert.ToDouble(Values[1].Data);

                        if (System.Math.Abs(P - Pressure) < 50 && System.Math.Abs(T - Temperature) < 50)
                        {
                            Pressure = P - 30.0;
                            Temperature = T - 30.0;
                            System.Console.WriteLine("Read initial values from OPC");
                        }
                    }

                }
                else
                {
                    System.Console.WriteLine("Connection to OPC server, result failed.");
                }

                // Write all data to the log file before attempting to calculate.
                SW.WriteLine("Initial pressure: {0}", Pressure.ToString()); Log_File_Lines += 1;
                System.Console.WriteLine("Initial pressure: {0}", Pressure.ToString());
                SW.WriteLine("Initial temperature: {0}", Temperature.ToString()); Log_File_Lines += 1;
                System.Console.WriteLine("Initial temperature: {0}", Temperature.ToString());
                SW.Flush();
                //SW.Close();

                UMR_DLL.Criconden_Bar(ref Components, IDs, Component_Values,
                 ref Temperature, ref Pressure);

                System.Console.WriteLine("Calculated");
                // After a successfull calculation we can remove the most recent data
                // from the log file.
                /*string[] Log_File = System.IO.File.ReadAllLines(Log_File_Path);
                string[] New_Log_File = new string[Log_File.Length - Log_File_Lines];

                Array.Copy(Log_File, 0, New_Log_File, 0, New_Log_File.Length);

                System.IO.File.WriteAllLines(Log_File_Path, New_Log_File); */

                System.Console.WriteLine("Calculated pressure: {0}", Pressure.ToString());
                SW.WriteLine("Calculated pressure: {0}", Pressure.ToString()); Log_File_Lines += 1;
                System.Console.WriteLine("Calculated temperature: {0}", Temperature.ToString());
                SW.WriteLine("Calculated temperature: {0}", Temperature.ToString()); Log_File_Lines += 1;
                SW.WriteLine(); Log_File_Lines += 1;
                SW.Flush();
                SW.Close();

                // Write results to OPC server.
                ValueQT[] Out_Values = new ValueQT[2];
                Out_Values[0] = new ValueQT(Pressure, EnumQuality.GOOD, System.DateTime.Now);
                Out_Values[1] = new ValueQT(Temperature, EnumQuality.GOOD, System.DateTime.Now);

                if (Pressure > 0.0 & Temperature > 0.0)
                {
                    OPC_Subscription.Write(Item_List, Out_Values, out Results, Execution_Options);
                }

                System.Console.WriteLine("Wrote results to OPC server");
            }
            else
            {
                System.Console.WriteLine("Read components values failed.");
            }

            OPC_Application.Terminate();
            DateTime End_Time_Stamp = System.DateTime.Now;
            System.Console.WriteLine(End_Time_Stamp.ToString());
            System.Console.WriteLine("Time used: {0}", (End_Time_Stamp - Start_Time_Stamp).ToString());
        }
    }
}
