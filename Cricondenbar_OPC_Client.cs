using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Softing.OPCToolbox.Client;
using Softing.OPCToolbox;

namespace PhaseOpt
{
    public class PhaseOpt
    {
        /// <summary>
        /// Calculates the criconden points (bar and/or therm).
        /// </summary>
        /// <param name="IND">Index for criconden therm (=1) or criconden bar (=2) calculations, INTEGER, Input.</param>
        /// <param name="NC">Number of mixture components, INTEGER, Input</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">Criconden point temperature [in K], DOUBLE PRECISION, Input/Output</param>
        /// <param name="P">Criconden point pressure [in bar], DOUBLE PRECISION, Input/Output.</param>
        /// <remarks>
        /// <para>For routine calling, T, P must be equal to or less than zero if the program's default initial
        /// values is to be used.
        /// </para>
        /// <para>Otherwise, if the user wants to specify the initial values of temperature
        /// and pressure (e.g for better convergence), the T, P must be set to specific values. It must be
        /// noted that initial values should be at low pressures (e.g. less than 30 bar) and, of course,
        /// inside the phase envelope, for the algorithm to converge.
        /// </para>
        /// </remarks>
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "ccd", CallingConvention = CallingConvention.Winapi)]
        private static extern void Criconden(ref Int32 IND, ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P);

        /// <summary>
        /// Calculates dew point temperature points.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">System dew point temperature [in K], DOUBLE PRECISION, Input/Output. </param>
        /// <param name="P">System pressure at which the dew point temperature is to be calculated [in bar], DOUBLE PRECISION, Input</param>
        /// <param name="XY">Mixture composition of liquid phase in mol/mol, DOUBLE PRECISION array of 50 elements, Output</param>
        /// <remarks>
        /// <para>
        /// For routine calling, T must be equal to or less than zero if the program's default initial
        /// values is to be used. The default initial values are taken using the Sandler's method.
        /// </para>
        /// <para>
        /// Otherwise, if the user wants to specify the initial value of T (e.g for better convergence),
        /// the T must be set to a specific value. It must be noted that initial values should be near
        /// the dewt and inside the phase envelope for the algorithm to converge.
        /// </para>
        /// <para>
        /// After program's execution, the routine returns the calculated value of T.
        /// </para>
        /// </remarks>
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dewt", CallingConvention = CallingConvention.Winapi)]
        private static extern void Dewt(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P, double[] XY);

        /// <summary>
        /// Calculates dew point pressure.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">System temperature at which the dew point pressure is to be calculated [in K], DOUBLE PRECISION, Input.</param>
        /// <param name="P1">System dew point pressures [in bar] at the given temperature, DOUBLE PRECISION, Input/Output.</param>
        /// <param name="XY1">Mixture composition of liquid phase for each dew point pressure calculated [in mol/mol],
        /// DOUBLE PRECISION array of 50 elements, Output</param>
        /// <param name="P2">System dew point pressures [in bar] at the given temperature, DOUBLE PRECISION, Input/Output.</param>
        /// <param name="XY2">Mixture composition of liquid phase for each dew point pressure calculated [in mol/mol],
        /// DOUBLE PRECISION array of 50 elements, Output</param>
        /// <remarks>
        /// <para>For routine calling, P1, P2 must be equal to or less than zero if the program's default initial
        /// values is to be used. The default initial values are taken using the Sandler's method.
        /// </para>
        /// <para>
        /// Otherwise, if the user wants to specify the initial value of pressure (e.g for better convergence),
        /// the P1, P2 must be set to specific values. It must be noted that initial values should be near the
        /// dewp and inside the phase envelope for the algorithm to converge.
        /// </para>
        /// <para>
        /// After program's execution, the routine returns the calculated values of P1 and P2.
        /// If P2 is equal to 0, no second dew point pressure is present.
        /// </para>
        /// </remarks>
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dewp", CallingConvention = CallingConvention.Winapi)]
        private static extern void Dewp(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P1, double[] XY1, ref double P2, double[] XY2);

        /// <summary>
        /// Calculates densities and compressibility factors.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">System temperature [in K], DOUBLE PRECISION, Input.</param>
        /// <param name="P">System pressure [in bar], DOUBLE PRECISION, Input.</param>
        /// <param name="D1">Densities [in kg/m3], DOUBLE PRECISION, Output.</param>
        /// <param name="D2">Densities [in kg/m3], DOUBLE PRECISION, Output.</param>
        /// <param name="CF1">Compressibility factors [-], DOUBLE PRECISION, Output.</param>
        /// <param name="CF2">Compressibility factors [-], DOUBLE PRECISION, Output.</param>
        /// <param name="XY1">Phase composition of phase [in mol/mol], DOUBLE PRECISION array of 50 elements, Output.</param>
        /// <param name="XY2">Phase composition of phase [in mol/mol], DOUBLE PRECISION array of 50 elements, Output.</param>
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dens", CallingConvention = CallingConvention.Winapi)]
        private static extern void Dens(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P, ref double D1, ref double D2, ref double CF1,
            ref double CF2, double[] XY1, double[] XY2);

        public DateTime Start_Time_Stamp = System.DateTime.Now;
        public string Log_File_Path = @"cri.log";
        public string Config_File_Path = @"cri.conf";

        /// <summary>
        /// Calculates the Dew Point Line of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="Points">Number of points to calculate from the Cricondenbar and Cricondentherm points.
        /// The total number of points calculated will be double of this, pluss the Cricondenbar and Cricondentherm points.
        /// Total points on the dew point line = 2 + (Points * 2)</param>
        /// <returns>An array of (pressure, temperature) pairs. The first pair is the Cricondenbar point.
        /// The second pair is the Cricondentherm point. The following pairs are points on the dew point line.</returns>
        /// <remarks>Compound IDs:
        /// 1	CO2
        /// 2	N2
        /// 3	CH4
        /// 4	C2H6
        /// 5	C3
        /// 6	iC4
        /// 7	nC4
        /// 8	cy-C5
        /// 9	2,2-DM-C3
        /// 10	iC5
        /// 11	nC5
        /// 14	2,2-DM-C4
        /// 15	2,3-DM-C4
        /// 16	2-M-C5
        /// 17	3-M-C5
        /// 18	nC6
        /// 19	nC11
        /// 20	nC14
        /// 21	3-M-4,4-DE-heptane
        /// 22	nC18
        /// 23	nc34
        /// 600	C6 fraction
        /// 700	C7 fraction
        /// 701	nC7
        /// 702	3-M-C6
        /// 703	2-M-C6
        /// 704	2,4-DM-C5
        /// 705	cy-C6
        /// 706	M-cy-C5
        /// 707	Benzene
        /// 709	1,2-DM-cyC5
        /// 710	1,3-dDM-cyC5
        /// 800	C8 fraction
        /// 801	nC8
        /// 802	2-M-C7
        /// 803	3-M-C7
        /// 804	2,3,4-TM-C5
        /// 805	2,4-DM-C6
        /// 806	cy-C7
        /// 807	M-cy-C6
        /// 808	E-cy-C5
        /// 809	cis-1,3-DM-cy-C6
        /// 810	toluene
        /// 811	1,cis-2,trans-4-TMcyC5
        /// 812	2,3-DM-C6
        /// 900	C9 fraction
        /// 901	nC9
        /// 902	2-M-C8
        /// 903	3-M-C8
        /// 904	2,2-DM-C7
        /// 905	2,6-DM-C7
        /// 906	cy-C8
        /// 907	e-cy-C6
        /// 908	i-p-cy-C5
        /// 909	1,1,3-TM-cy-C6
        /// 910	cis-1,2-DM-cy-C6
        /// 911	m-xylene
        /// 912	p-xylene
        /// 913	E-benzene
        /// 914	o-xylene
        /// 915	1,2,3-TMcyC6
        /// 916	4-M-C8
        /// 917	o-E-toluene
        /// 1201	nC12
        /// 1202	nC5-Benzene
        /// 6000	C6+ fraction
        /// 10000	C10+ fraction
        /// 101101	nC10
        /// 101102	1,2,3-TM-Benzene
        /// 101103	cy-C9
        /// 101104	2-E-p-xylene
        /// 131401	nC13
        /// 131402	nC6-Benzene
        /// 131403	nC7-Benzene
        /// 151601	nC15
        /// 151602	nC10-cy-C5
        /// 151603	nC8-Benzene
        /// 151604	nC9-Benzene
        /// 171801	nC17
        /// 171802	nC12-cy-C5
        /// 171803	nC10-Benzene
        /// 192101	nC19
        /// 192102	nC14-cy-C5
        /// 222401	nC22
        /// 222402	nC23
        /// 222403	nC17-cy-C5
        /// 253001	nC27
        /// 318001	nC39
        /// 700900	C7C9 fraction
        /// </remarks>
        public double[] Calculate_Dew_Point_Line(int[] IDs, double[] Values, int Points = 5)
        {
            // Calculate the cricondentherm point
            Int32 IND = 1;
            Int32 Components = IDs.Length;
            double Pressure = 0.0;
            double Temperature = 0.0;
            List<double> Results = new List<double>();

            Criconden(ref IND, ref Components, IDs, Values,
             ref Temperature, ref Pressure);

            System.Console.WriteLine("Test run version 3, ind=1, Cricondentherm point");
            System.Console.WriteLine("Temperature: {0} K", Temperature.ToString());
            System.Console.WriteLine("Pressure: {0} bara", Pressure.ToString());

            double CCTT = Temperature;
            double CCTP = Pressure;

            // Calculate the cricondenbar point
            IND = 2;
            Temperature = 0;
            Pressure = 0;

            Criconden(ref IND, ref Components, IDs, Values,
             ref Temperature, ref Pressure);

            System.Console.WriteLine("Test run version 3, ind=2, Cricondenbar point");
            System.Console.WriteLine("Temperature: {0} K", Temperature.ToString());
            System.Console.WriteLine("Pressure: {0} bara", Pressure.ToString());

            double CCBT = Temperature;
            double CCBP = Pressure;

            Results.Add(CCBP);
            Results.Add(CCBT);
            Results.Add(CCTP);
            Results.Add(CCTT);

            // Dew points from pressure
            // Calculate points on the dew point line starting from the cricondentherm point.
            // Points are calculated approximately halfway towards the cricondenbar point.
            System.Console.WriteLine("Test run version 3, Dew points from pressure");
            double P_Interval = (CCBP - CCTP) / ((Points * 2) - 2);
            for (Int32 i = 1; i <= Points; i++)
            {
                double P = CCTP + P_Interval * i;
                double T = CCBT;
                double[] XY = new double[50];
                Dewt(ref Components, IDs, Values, ref T, ref P, XY);
                Results.Add(P); Results.Add(T);
                System.Console.WriteLine("Temperature: {0} K", T.ToString());
                System.Console.WriteLine("Pressure: {0} bara", P.ToString());
            }

            // Dew points from temperature
            // Calculate points on the dew point line starting from the cricondenbar point.
            // Points are calculated approximately halfway towards the cricondentherm point.
            System.Console.WriteLine("Test run version 3, Dew points from temperature");
            double T_Interval = (CCTT - CCBT) / ((Points * 2) - 2);
            for (Int32 i = 1; i <= Points; i++)
            {
                double T = CCBT + T_Interval * i;
                double P1 = CCTP;
                double P2 = CCTP;
                double[] XY1 = new double[50];
                double[] XY2 = new double[50];
                Dewp(ref Components, IDs, Values, ref T, ref P1, XY1, ref P2, XY2);
                Results.Add(P1); Results.Add(T);
                System.Console.WriteLine("Temperature: {0} K", T.ToString());
                System.Console.WriteLine("Pressure: {0} bara", P1.ToString());
            }
            return Results.ToArray();
        }

        public void Test()
        {

            /*


            System.Console.WriteLine("Opening log file.");
            System.IO.StreamWriter Log_File;
            Log_File = System.IO.File.AppendText(Log_File_Path);



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
            System.Console.WriteLine("Time used: {0}", (End_Time_Stamp - Start_Time_Stamp).ToString()); */
        }
    }

    public class OPC_Client
    {
        private List<string> Item_Ids = new List<string>();
        private List<Int32> IDs = new List<Int32>();
        private List<double> Scale_Factors = new List<double>();
        private string GC_OPC_Server_Path = "";
        private string Tunneller_OPC_Server_Path = "";
        private double Temperature = 0.0;
        private double Pressure = 0.0;
        private ValueQT[] Values;
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

                    List<double> Component_Values = new List<double>(Scale_Factors);

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

                    System.Console.WriteLine("Sum: {0}", Sum.ToString());

                    double[] Normalized_Values = Component_Values.ToArray();
                    //UMR.Normalize(Normalized_Values, 1.0);
                    Component_Values.Clear();
                    Component_Values.AddRange(Normalized_Values);

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
    }

    public static class Tester
    {
        private static void Normalize(double[] Array, double Target = 1.0)
        {
            double Sum = 0.0;

            foreach (double Value in Array)
            {
                Sum += Value;
            }

            double Factor = Target / Sum;

            for (int i = 0; i < Array.Length; i++)
            {
                Array[i] = Array[i] * Factor;
            }
        }

        public static void Main(String[] args)
        {
            bool Test_Run = true;
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
                PhaseOpt Calculator = new PhaseOpt();
                int[] IDs = new int[25] { 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 14, 8, 15, 16, 17, 18, 701, 705, 707, 801, 806, 710, 901, 906, 911 };
                double[] Values = new double[25] {0.0188591, 0.0053375, 0.8696321, 0.0607237, 0.0267865, 0.0043826,
                    0.0071378, 0.0001517, 0.0019282, 0.0016613, 0.0000497, 0.0001451, 0.0000843, 0.0003587,
                    0.0001976, 0.0004511, 0.0002916, 0.000803, 0.0003357, 0.0000517, 0.0003413, 0.0002315,
                    0.0000106, 0.000013, 0.0000346};

                Normalize(Values);

                double[] Result = Calculator.Calculate_Dew_Point_Line(IDs, Values, 5);

                return;
            }
        }
    }
}
