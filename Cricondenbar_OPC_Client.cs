using System;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Softing.OPCToolbox.Client;
using Softing.OPCToolbox;

    public class OPC_Client
    {
        public DateTime Start_Time_Stamp = System.DateTime.Now;
        public string Log_File_Path = @"cri.log";

        private List<string> Item_Ids = new List<string>();
        private List<Int32> IDs = new List<Int32>();
        private List<double> Scale_Factors = new List<double>();
        private string GC_OPC_Server_Path = "";
        private string Tunneller_OPC_Server_Path = "";
        private double Temperature = 0.0;
        private double Pressure = 0.0;
        private ValueQT[] Values;
        private List<double> Component_Values = new List<double>();

        private int[] Results;

        public void Read_Config(string Config_File_Path)
        {
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
        }
        public void Read_Values()
        {
            Application OPC_Application = Application.Instance;
            OPC_Application.Initialize();

            // creates a new DaSession object and adds it to the OPC_Application
            DaSession OPC_Session = new DaSession(GC_OPC_Server_Path);

            // sets the execution options
            ExecutionOptions Execution_Options = new ExecutionOptions();
            Execution_Options.ExecutionType = EnumExecutionType.SYNCHRONOUS;

            DaSubscription OPC_Subscription = new DaSubscription(500, OPC_Session);

            Component_Values.AddRange(Scale_Factors);
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

                    //Log_File.WriteLine(Start_Time_Stamp);

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
                        //Log_File.WriteLine(ID);
                    }

                    foreach (double Val in Component_Values)
                    {
                        System.Console.WriteLine(Val.ToString());
                        //Log_File.WriteLine(Val.ToString());
                    }
                }
                else
                {
                    System.Console.WriteLine("Read components values failed.");
                }
            }
            else
            {
                System.Console.WriteLine("Connection failed.");
            }
            OPC_Application.Terminate();
        }
        public void Read_Initial_Values()
        {
            Application OPC_Application = Application.Instance;
            OPC_Application.Initialize();

            // creates a new DaSession object and adds it to the OPC_Application
            DaSession OPC_Session = new DaSession(Tunneller_OPC_Server_Path);

            // sets the execution options
            ExecutionOptions Execution_Options = new ExecutionOptions();
            Execution_Options.ExecutionType = EnumExecutionType.SYNCHRONOUS;

            DaSubscription OPC_Subscription = new DaSubscription(500, OPC_Session);

            // Read initial values for pressure and temperature.
            List<DaItem> Item_List = new List<DaItem>();

            Item_List.Add(new DaItem("ANALYSE.CRICONDENBAR", OPC_Subscription));
            Item_List.Add(new DaItem("ANALYSE.CRICONDENBARTEMPERATURE", OPC_Subscription));

            int Connect_Result = OPC_Session.Connect(true, true, Execution_Options);
            if (ResultCode.SUCCEEDED(Connect_Result))
            {
                System.Console.WriteLine("Connected to OPC server, result.");
                int Read_Result = OPC_Subscription.Read(
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
            OPC_Application.Terminate();
        }

        public double Pressure_Value
        {
            get { return Pressure; }
        }
        public double Temperature_Value
        {
            get { return Temperature; }
        }
        public double[] Components
        {
            get { return Component_Values.ToArray(); }
        }
        public int[] Component_IDs
        {
            get { return IDs.ToArray(); }
        }
    }
