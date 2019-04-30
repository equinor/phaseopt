using System;
using System.Threading.Tasks;

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
            PO_A.Name = "GC A";
            PO_B.Name = "GC B";
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
            int errors_current_A = 0;
            int errors_current_B = 0;

            while (true)
            {
                Start_Time = DateTime.Now;

                Log_File.WriteLine("{0}: Read composition A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                Log_File.WriteLine("{0}: Read composition B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();

                Log_File.WriteLine("{0}: Read from IP21 A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                Log_File.WriteLine("{0}: Read from IP21 B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();

                Parallel.Invoke(
                    () =>
                    {
                        PO_A.Read_Composition();
                        PO_A.Read_Current_Kalsto_Composition();
                        errors_A = PO_A.Validate();
                        errors_current_A = PO_A.Validate_Current();
                    },

                    () =>
                    {
                        PO_B.Read_Composition();
                        PO_B.Read_Current_Kalsto_Composition();
                        errors_B = PO_B.Validate();
                        errors_current_B = PO_B.Validate_Current();
                    }
                );

                // Calculate composition mixes, cricondenbar and set status flag
                Parallel.Invoke(
                    () =>
                    {
                        if (errors_A < 1)
                        {
                            PO_A.Calculate_Karsto();
                            Log_File.WriteLine("{0}: Calculate CCB at Kårstø A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                        else
                        {
                            Log_File.WriteLine("{0}: Errors in A.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                    },

                    () =>
                    {
                        if (errors_B < 1)
                        {
                            PO_B.Calculate_Karsto();
                            Log_File.WriteLine("{0}: Calculate CCB at Kårstø B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                        else
                        {
                            Log_File.WriteLine("{0}: Errors in B.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                    },
                    // Read and calculate cricondenbar for current
                    // compositions at Kalstø
                    () =>
                    {
                        if (errors_A < 1)
                        {
                            PO_A.Calculate_Kalsto_Statpipe();
                            Log_File.WriteLine("{0}: Calculate Statpipe stream at Kalstø A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                        else
                        {
                            Log_File.WriteLine("{0}: Errors in Statpipe current composition A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                    },

                    () =>
                    {
                        if (errors_A < 1)
                        {
                            PO_A.Calculate_Kalsto_Asgard();
                            Log_File.WriteLine("{0}: Calculate Åsgard stream at Kalstø A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                        else
                        {
                            Log_File.WriteLine("{0}: Errors in Asgard current composition A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                    },

                    () =>
                    {
                        if (errors_B < 1)
                        {
                            PO_B.Calculate_Kalsto_Statpipe();
                            Log_File.WriteLine("{0}: Calculate Statpipe stream at Kalstø B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                        else
                        {
                            Log_File.WriteLine("{0}: Errors in Statpipe current composition B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                    },

                    () =>
                    {
                        if (errors_B < 1)
                        {
                            PO_B.Calculate_Kalsto_Asgard();
                            Log_File.WriteLine("{0}: Calculate Åsgard stream at Kalstø B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                        else
                        {
                            Log_File.WriteLine("{0}: Errors in Asgard current composition B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                        }
                    }
                );

                Log_File.WriteLine("{0}: Read current composition from IP21 A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
                Log_File.WriteLine("{0}: Read current composition from IP21 B", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();

                PO_A.Trigger_Watchdog();

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