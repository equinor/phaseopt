using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using System.Threading;

namespace Main
{
    public static class Main_Class
    {
        public static void Main(String[] args)
        {
            bool Test_UMR = false;
            bool Read_Only = false;

            foreach (string arg in args)
            {
                if (arg.Equals(@"/u"))
                {
                    Test_UMR = true;
                }
                if (arg.Equals(@"/r"))
                {
                    Read_Only = true;
                }
            }

            if (Test_UMR)
            {
                Test_Space.Testing.Main_Test();

                Int32[] IDs = new Int32[22] { 1, 2, 101, 201, 301, 401, 402, 503, 504, 603, 604, 605, 701, 606, 608, 801, 707, 710, 901, 806, 809, 1016 };

                double[] Values = new double[22] { 2.483, 0.738, 81.667, 8.393, 4.22, 0.605, 1.084, 0.24, 0.23, 0.0801, 0.0243, 0.0614,
                    0.0233, 0.0778, 0.0191, 0.0048, 0.0302, 0.0116, 0.0023, 0.0017, 0.0022, 0.0014 };

                Values[0] = 0.019589630031103186;
                Values[1] = 0.0069316153019771043;
                Values[2] = 0.85927475631962247;
                Values[3] = 0.066976027653551679;
                Values[4] = 0.027726113415945896;
                Values[5] = 0.0041137726628022253;
                Values[6] = 0.0075626129494001451;
                Values[7] = 0.0019319335825040585;
                Values[8] = 0.00200034452952193;
                Values[9] = 0.00077486680187614494;
                Values[10] = 0.00023703678307125027;
                Values[11] = 0.00062108276271134418;
                Values[12] = 0.00031021799009610152;
                Values[13] = 0.00087619108295439586;
                Values[14] = 0.0002509733272680488;
                Values[15] = 8.3306340077155677E-05;
                Values[16] = 0.00042925548924499968;
                Values[17] = 0.00017954942822573852;
                Values[18] = 3.310099045445734E-05;
                Values[19] = 3.1032777420227576E-05;
                Values[20] = 5.0705449690578696E-05;
                Values[21] = 1.5874330480810202E-05;

                /*
                double[] Result = PhaseOpt.PhaseOpt.Calculate_Dew_Point_Line(IDs, Values, 5);

                Console.WriteLine("Cricondenbar point");
                Console.WriteLine("Pressure: {0} barg", Result[0].ToString("G", CultureInfo.InvariantCulture));
                Console.WriteLine("Temperature: {0} °C", Result[1].ToString("G", CultureInfo.InvariantCulture));
                Console.WriteLine();

                Console.WriteLine("Cricondentherm point");
                Console.WriteLine("Pressure: {0} barg", Result[2].ToString("G", CultureInfo.InvariantCulture));
                Console.WriteLine("Temperature: {0} °C", Result[3].ToString("G", CultureInfo.InvariantCulture));

                Console.WriteLine("Dew Point Line");
                for (int i = 4; i < Result.Length; i += 2)
                {
                    Console.WriteLine("Pressure: {0} barg", Result[i].ToString("G", CultureInfo.InvariantCulture));
                    Console.WriteLine("Temperature: {0} °C", Result[i + 1].ToString("G", CultureInfo.InvariantCulture));
                    Console.WriteLine();
                }

                //double[] Dens_Result = PhaseOpt.PhaseOpt.Calculate_Density_And_Compressibility(IDs, Values);

                Console.WriteLine("Vapour density: {0} kg/m­³", Dens_Result[0]);
                Console.WriteLine("Compressibility factor: {0}", Dens_Result[2]);
                Console.WriteLine("Liquid density: {0} kg/m­³", Dens_Result[1]);
                Console.WriteLine("Compressibility factor: {0}", Dens_Result[3]);
                */
                return;
            }

            System.IO.StreamWriter Log_File;
#if DEBUG
            Log_File = System.IO.File.AppendText(@"PhaseOpt_Kar_Main_Test.log");
#else
            Log_File = System.IO.File.AppendText(@"PhaseOpt_Kar_Main.log");
#endif

            Log_File.WriteLine("{0}: PhaseOpt startup", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();

#if DEBUG
            PhaseOpt_KAR PO_A = new PhaseOpt_KAR(@"PhaseOpt_Kar_A_Test.log");
            PhaseOpt_KAR PO_B = new PhaseOpt_KAR(@"PhaseOpt_Kar_B_Test.log");

            PO_A.Read_Config("PhaseOpt_A_Test.xml");
            PO_B.Read_Config("PhaseOpt_B_Test.xml");
#else
            PhaseOpt_KAR PO_A = new PhaseOpt_KAR(@"PhaseOpt_Kar_A.log");
            PhaseOpt_KAR PO_B = new PhaseOpt_KAR(@"PhaseOpt_Kar_B.log");

            PO_A.Read_Config("PhaseOpt_A.xml");
            PO_B.Read_Config("PhaseOpt_B.xml");

#endif
            PO_A.Connect_DB();
            PO_B.Connect_DB();

            if (Read_Only)
            {
                PO_A.DB_Connection.IP21_Read_Only = true;
                PO_B.DB_Connection.IP21_Read_Only = true;
            }

            DateTime Start_Time;
            double Sleep_Time = 0.0;
            int errors_A = 0;
            int errors_B = 0;

            Thread IP_21_Reader_Thread_A = new Thread(PO_A.IP21_Reader);
            Thread IP_21_Reader_Thread_B = new Thread(PO_B.IP21_Reader);
            IP_21_Reader_Thread_A.Name = "IP21 Reader A";
            IP_21_Reader_Thread_B.Name = "IP21 Reader B";
            IP_21_Reader_Thread_A.Start();
            IP_21_Reader_Thread_B.Start();

            while (true)
            {
                Start_Time = DateTime.Now;
                errors_A = PO_A.Validate();
                errors_B = PO_B.Validate();

                Log_File.WriteLine("{0}: Read composition A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                Log_File.WriteLine("{0}: Read composition B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();

                Log_File.WriteLine("{0}: Read from IP21 A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                Log_File.WriteLine("{0}: Read from IP21 B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();


                // Calculate composition mixes, cricondenbar and set status flag
                Parallel.Invoke(
                    () =>
                    {
                        lock (PO_A.locker)
                        {
                            if (errors_A < 1)
                            {
                                PO_A.Calculate_Karsto();
                                PO_A.DB_Connection.Write_Value("T_20XI7146_A", 1);
                                Log_File.WriteLine("{0}: Calculate CCB at Kårstø A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                            else
                            {
                                PO_A.DB_Connection.Write_Value("T_20XI7146_A", 0);
                                Log_File.WriteLine("{0}: Errors in A.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                        }
                    },

                    () =>
                    {
                        lock (PO_B.locker)
                        {
                            if (PO_B.Validate() < 1)
                            {
                                PO_B.Calculate_Karsto();
                                PO_B.DB_Connection.Write_Value("T_20XI7146_B", 1);
                                Log_File.WriteLine("{0}: Calculate CCB at Kårstø B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                            else
                            {
                                PO_B.DB_Connection.Write_Value("T_20XI7146_B", 0);
                                Log_File.WriteLine("{0}: Errors in B.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                        }
                    },
                    // Read and calculate cricondenbar for current
                    // compositions at Kalstø
                    () =>
                    {
                        lock (PO_A.locker)
                        {
                            if (errors_A < 1)
                            {
                                PO_A.Calculate_Kalsto_Statpipe();
                                PO_A.DB_Connection.Write_Value("T_31XI0157_A", 1);
                                Log_File.WriteLine("{0}: Calculate Statpipe stream at Kalstø A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                            else
                            {
                                PO_A.DB_Connection.Write_Value("T_31XI0157_A", 0);
                                Log_File.WriteLine("{0}: Errors in Statpipe current composition A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                        }
                    },

                    () =>
                    {
                        lock (PO_A.locker)
                        {
                            if (errors_A < 1)
                            {
                                PO_A.Calculate_Kalsto_Asgard();
                                PO_A.DB_Connection.Write_Value("T_31XI0161_A", 1);
                                Log_File.WriteLine("{0}: Calculate Åsgard stream at Kalstø A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                            else
                            {
                                PO_A.DB_Connection.Write_Value("T_31XI0161_A", 0);
                                Log_File.WriteLine("{0}: Errors in Asgard current composition A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                        }
                    },

                    () =>
                    {
                        lock (PO_B.locker)
                        {
                            if (errors_B < 1)
                            {
                                PO_B.Calculate_Kalsto_Statpipe();
                                PO_B.DB_Connection.Write_Value("T_31XI0157_B", 1);
                                Log_File.WriteLine("{0}: Calculate Statpipe stream at Kalstø B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                            else
                            {
                                PO_B.DB_Connection.Write_Value("T_31XI0157_B", 0);
                                Log_File.WriteLine("{0}: Errors in Statpipe current composition B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                        }
                    },

                    () =>
                    {
                        lock (PO_B.locker)
                        {
                            if (errors_B < 1)
                            {
                                PO_B.Calculate_Kalsto_Asgard();
                                PO_B.DB_Connection.Write_Value("T_31XI0161_B", 1);
                                Log_File.WriteLine("{0}: Calculate Åsgard stream at Kalstø B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                            else
                            {
                                PO_B.DB_Connection.Write_Value("T_31XI0161_B", 0);
                                Log_File.WriteLine("{0}: Errors in Asgard current composition B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                            }
                        }
                    }
                );

                Log_File.WriteLine("{0}: Read current composition from IP21 A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                Log_File.WriteLine("{0}: Read current composition from IP21 B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();

                //PO_A.DB_Connection.Write_Value("PhaseOpt.ST", Start_Time.ToString("yyy-MM-dd HH:mm:ss"));

                if (errors_A < 1)
                {
                    PO_A.Calculate_Dropout_Curves();
                }

                Sleep_Time = (Start_Time.AddSeconds(150.0) - DateTime.Now).TotalMilliseconds;
                if (Sleep_Time > 1.0)
                {
                    Console.WriteLine("Waiting {0} seconds", Sleep_Time / 1000.0);
                    System.Threading.Thread.Sleep((int)Sleep_Time);
                }
            }
        }
    }
}