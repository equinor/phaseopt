using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Softing.OPCToolbox.Client;
using Softing.OPCToolbox;

namespace Cricondenbar_OPC_Client
{
    public class Cricondenbar_Client
    {
        public class UMR_DLL
        {
            [DllImport(@"C:\UMR\UMR.dll", EntryPoint = "CCDB", CallingConvention = CallingConvention.Winapi)]
            public static extern void Criconden_Bar(ref Int32 NC, Int32[] ID,
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

        public class UMR_DLL_V2
        {
            [DllImport(@"C:\Program Files\UMR-PRU\DLL\UMR.dll", EntryPoint = "CCD", CallingConvention = CallingConvention.Winapi)]
            public static extern void Criconden(ref Int32 IND, ref Int32 IEOS, ref Int32 NC,
                Int32[] ID,
                double[] Z, ref double T, ref double P);
        }

        public class UMROL_DLL
        {
            [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "ccd", CallingConvention = CallingConvention.Winapi)]
            public static extern void Criconden(ref Int32 IND, ref Int32 NC,
                Int32[] ID, double[] Z, ref double T, ref double P);

            [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dewt", CallingConvention = CallingConvention.Winapi)]
            public static extern void Dewt(ref Int32 NC,
                Int32[] ID, double[] Z, ref double T, ref double P, double[] XY);

            [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dewp", CallingConvention = CallingConvention.Winapi)]
            public static extern void Dewp(ref Int32 NC,
                Int32[] ID, double[] Z, ref double T, ref double P, double[] XY);

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
            List<string> Item_Ids = new List<string>();
            List<Int32> IDs = new List<Int32>();
            List<double> Scale_Factors = new List<double>();
            double Temperature = 0.0;
            double Pressure = 0.0;
            ValueQT[] Values;
            int[] Results;
            string Log_File_Path = @"cri.log";
            string Config_File_Path = @"cri.conf";
            string GC_OPC_Server_Path = "";
            string Tunneller_OPC_Server_Path = "";
            bool Test_Run = false;

            foreach (string arg in args)
            {
                System.Console.WriteLine("args: {0}", arg);
                if (arg.Equals(@"/t"))
                {
                    Test_Run = true;
                }
            }

            if (Test_Run)
            {
                IDs.Add(1); Scale_Factors.Add(0.0188591);
                IDs.Add(2); Scale_Factors.Add(0.0053375);
                IDs.Add(3); Scale_Factors.Add(0.8696321);
                IDs.Add(4); Scale_Factors.Add(0.0607237);
                IDs.Add(5); Scale_Factors.Add(0.0267865);
                IDs.Add(6); Scale_Factors.Add(0.0043826);
                IDs.Add(7); Scale_Factors.Add(0.0071378);
                IDs.Add(9); Scale_Factors.Add(0.0001517);
                IDs.Add(10); Scale_Factors.Add(0.0019282);
                IDs.Add(11); Scale_Factors.Add(0.0016613);
                IDs.Add(14); Scale_Factors.Add(0.0000497);
                IDs.Add(8); Scale_Factors.Add(0.0001451);
                IDs.Add(15); Scale_Factors.Add(0.0000843);
                IDs.Add(16); Scale_Factors.Add(0.0003587);
                IDs.Add(17); Scale_Factors.Add(0.0001976);
                IDs.Add(18); Scale_Factors.Add(0.0004511);
                IDs.Add(701); Scale_Factors.Add(0.0002916);
                IDs.Add(705); Scale_Factors.Add(0.000803);
                IDs.Add(707); Scale_Factors.Add(0.0003357);
                IDs.Add(801); Scale_Factors.Add(0.0000517);
                IDs.Add(806); Scale_Factors.Add(0.0003413);
                IDs.Add(810); Scale_Factors.Add(0.0002315);
                IDs.Add(901); Scale_Factors.Add(0.0000106);
                IDs.Add(906); Scale_Factors.Add(0.000013);
                IDs.Add(911); Scale_Factors.Add(0.0000346);

                Int32 IND = 1;
                Int32 Components = IDs.Count;

                UMROL_DLL.Criconden(ref IND, ref Components, IDs.ToArray(), Scale_Factors.ToArray(),
                 ref Temperature, ref Pressure);

                System.Console.WriteLine("Test run version 3, ind=1");
                System.Console.WriteLine("Temperature: {0}", Temperature.ToString());
                System.Console.WriteLine("Pressure: {0}", Pressure.ToString());

                IND = 2;
                Temperature = 0;
                Pressure = 0;

                UMROL_DLL.Criconden(ref IND, ref Components, IDs.ToArray(), Scale_Factors.ToArray(),
                 ref Temperature, ref Pressure);

                System.Console.WriteLine("Test run version 3, ind=2");
                System.Console.WriteLine("Temperature: {0}", Temperature.ToString());
                System.Console.WriteLine("Pressure: {0}", Pressure.ToString());

                return;

            }


            // Read config file.
            try
            {
                string[] Config_File = System.IO.File.ReadAllLines(Config_File_Path);
                string Component_Pattern = @"^\s*(\d+?)\s*;\s*([^#]+)\s*;\s*([\d\.^#]+)\s*#*.*$";
                string GC_OPC_Server_Pattern = @"^\s*GC_OPC_Server_Path=\s*([^#]+)\s*#*.*$";
                string Tunneller_OPC_Server_Pattern = @"^\s*Tunneller_OPC_Server_Path=\s*([^#]+)\s*#*.*$";

                foreach (string Line in Config_File)
                {
                    foreach (Match match in Regex.Matches(Line, GC_OPC_Server_Pattern, RegexOptions.None))
                    {
                        GC_OPC_Server_Path = match.Groups[1].Value.Trim();
                    }

                    foreach (Match match in Regex.Matches(Line, Tunneller_OPC_Server_Pattern, RegexOptions.None))
                    {
                        Tunneller_OPC_Server_Path = match.Groups[1].Value.Trim();
                    }

                    foreach (Match match in Regex.Matches(Line, Component_Pattern, RegexOptions.None))
                    {
                        //Console.WriteLine("|{0}|{1}|{2}|", match.Groups[1].Value.Trim(),
                        //    match.Groups[2].Value.Trim(), match.Groups[3].Value.Trim());
                        Item_Ids.Add(match.Groups[2].Value.Trim());
                        IDs.Add(System.Convert.ToInt32(match.Groups[1].Value.Trim()));
                        Scale_Factors.Add(System.Convert.ToDouble(match.Groups[3].Value.Trim(),
                            System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            catch
            {
                System.Console.WriteLine("Error: Could not open configuration file. Exiting.");
                System.Environment.Exit(1);
            }

            System.Console.WriteLine("Opening log file.");
            System.IO.StreamWriter Log_File;
            Log_File = System.IO.File.AppendText(Log_File_Path);

            Application OPC_Application = Application.Instance;
            OPC_Application.Initialize();

            // creates a new DaSession object and adds it to the OPC_Application
            DaSession OPC_Session = new DaSession(GC_OPC_Server_Path);

            // sets the execution options
            ExecutionOptions Execution_Options = new ExecutionOptions();
            Execution_Options.ExecutionType = EnumExecutionType.SYNCHRONOUS;

            DaSubscription OPC_Subscription = new DaSubscription(500, OPC_Session);

            List<DaItem> Item_List = new List<DaItem>();
            foreach (string Item_Id in Item_Ids)
            {
                Item_List.Add(new DaItem(Item_Id, OPC_Subscription));
                System.Console.WriteLine("Item_Id {0}", Item_Id.ToString());
            }

            System.Console.WriteLine("Connecting to OPC server. " + GC_OPC_Server_Path);
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
                Item_List.ToArray(),
                out Values,
                out Results,
                Execution_Options);

            OPC_Session.Disconnect(Execution_Options);

            System.Console.WriteLine("Read_Result: {0}", Read_Result.ToString());

            double Sum = 0.0;

            if (ResultCode.SUCCEEDED(Read_Result))
            {
                System.Console.WriteLine("Read components values from OPC server");
                System.Console.WriteLine("Values:");
                int i = Values.Length - 1;

                List<double> Component_Values = new List<double>(Scale_Factors);

                Log_File.WriteLine(Start_Time_Stamp);

                while (i >= 0)
                {
                    if (Values[i].Quality == EnumQuality.GOOD)
                    {
                        double Value = System.Convert.ToDouble(Values[i].Data) * Scale_Factors[i];
                        // Discard components that are too low.
                        if (Value < 1.0E-10)
                        {
                            System.Console.WriteLine("Removed ID: {0}", IDs[i].ToString());
                            IDs.RemoveAt(i);
                            Component_Values.RemoveAt(i);
                            Scale_Factors.RemoveAt(i);
                        }
                        else
                        {
                            Component_Values[i] = Value;
                            Sum += Value;
                        }
                    }
                    i -= 1;
                }

                foreach (Int32 ID in IDs)
                {
                    Log_File.WriteLine(ID);
                }

                System.Console.WriteLine("Sum: {0}", Sum.ToString());

                double[] Normalized_Values = Component_Values.ToArray();
                Cricondenbar_Client.Normalize(Normalized_Values, 1.0);
                Component_Values.Clear();
                Component_Values.AddRange(Normalized_Values);

                foreach (double Val in Component_Values)
                {
                    System.Console.WriteLine(Val.ToString());
                    Log_File.WriteLine(Val.ToString());
                }

                // Read initial values for pressure and temperature.
                OPC_Session = new DaSession(Tunneller_OPC_Server_Path);
                OPC_Subscription = new DaSubscription(500, OPC_Session);
                Item_List.Clear();

                Item_List.Add(new DaItem("ANALYSE.CRICONDENBAR", OPC_Subscription));
                Item_List.Add(new DaItem("ANALYSE.CRICONDENBARTEMPERATURE", OPC_Subscription));

                Connect_Result = OPC_Session.Connect(true, true, Execution_Options);
                if (ResultCode.SUCCEEDED(Connect_Result))
                {
                    System.Console.WriteLine("Connected to OPC server, result.");
                    Read_Result = OPC_Subscription.Read(
                        0, // the values are read from the device
                        Item_List.ToArray(),
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
                Log_File.WriteLine("Initial pressure: {0}", Pressure.ToString());
                System.Console.WriteLine("Initial pressure: {0}", Pressure.ToString());
                Log_File.WriteLine("Initial temperature: {0}", Temperature.ToString());
                System.Console.WriteLine("Initial temperature: {0}", Temperature.ToString());
                Log_File.Flush();

                Int32 Components = IDs.Count;
                Int32 IND = 2;
                UMROL_DLL.Criconden(ref IND, ref Components, IDs.ToArray(), Component_Values.ToArray(),
                 ref Temperature, ref Pressure);

                System.Console.WriteLine("Calculated");

                System.Console.WriteLine("Calculated pressure: {0}", Pressure.ToString());
                Log_File.WriteLine("Calculated pressure: {0}", Pressure.ToString());
                System.Console.WriteLine("Calculated temperature: {0}", Temperature.ToString());
                Log_File.WriteLine("Calculated temperature: {0}", Temperature.ToString());
                Log_File.WriteLine();
                Log_File.Flush();
                Log_File.Close();

                // Write results to OPC server.
                ValueQT[] Out_Values = new ValueQT[2];
                Out_Values[0] = new ValueQT(Pressure, EnumQuality.GOOD, System.DateTime.Now);
                Out_Values[1] = new ValueQT(Temperature, EnumQuality.GOOD, System.DateTime.Now);

                if (Pressure > 0.0 & Temperature > 0.0)
                {
                    OPC_Subscription.Write(Item_List.ToArray(), Out_Values, out Results, Execution_Options);
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
