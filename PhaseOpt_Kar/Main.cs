using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Main
{
    public static class Main_Class
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
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

            logger.Info("PhaseOpt startup");

            PhaseOpt_KAR PO_A = new PhaseOpt_KAR();
            PhaseOpt_KAR PO_B = new PhaseOpt_KAR();

#if DEBUG
            PO_A.Read_Config("PhaseOpt_A_Test.xml");
            PO_B.Read_Config("PhaseOpt_B_Test.xml");
#else
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

                logger.Info("Read composition A");
                logger.Info("Read composition B");

                logger.Info("Read from IP21 A");
                logger.Info("Read from IP21 B");

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
                            logger.Info("Calculate CCB at Kårstø A");
                        }
                        else
                        {
                            logger.Error("Errors in A.");
                        }
                    },

                    () =>
                    {
                        if (errors_B < 1)
                        {
                            PO_B.Calculate_Karsto();
                            logger.Info("Calculate CCB at Kårstø B");
                        }
                        else
                        {
                            logger.Error("Errors in B.");
                        }
                    },
                    // Read and calculate cricondenbar for current
                    // compositions at Kalstø
                    () =>
                    {
                        if (errors_A < 1)
                        {
                            PO_A.Calculate_Kalsto_Statpipe();
                            logger.Info("Calculate Statpipe stream at Kalstø A");
                        }
                        else
                        {
                            logger.Error("Errors in Statpipe current composition A");
                        }
                    },

                    () =>
                    {
                        if (errors_A < 1)
                        {
                            PO_A.Calculate_Kalsto_Asgard();
                            logger.Info("Calculate Åsgard stream at Kalstø A");
                        }
                        else
                        {
                            logger.Error("Errors in Asgard current composition A");
                        }
                    },

                    () =>
                    {
                        if (errors_B < 1)
                        {
                            PO_B.Calculate_Kalsto_Statpipe();
                            logger.Info("Calculate Statpipe stream at Kalstø B");
                        }
                        else
                        {
                            logger.Error("Errors in Statpipe current composition B");
                        }
                    },

                    () =>
                    {
                        if (errors_B < 1)
                        {
                            PO_B.Calculate_Kalsto_Asgard();
                            logger.Info("Calculate Åsgard stream at Kalstø B");
                        }
                        else
                        {
                            logger.Error("Errors in Asgard current composition B");
                        }
                    }
                );

                PO_A.Trigger_Watchdog();

                if (errors_A < 1)
                {
                    PO_A.Calculate_Dropout_Curves(PO_A.Mix_To_T410, PO_A.T400);
                    PO_A.Calculate_Dropout_Curves(PO_A.Mix_To_T100, PO_A.T100);
                }

                Sleep_Time = (Start_Time.AddSeconds(150.0) - DateTime.Now).TotalMilliseconds;
                if (Sleep_Time > 1.0)
                {
                    logger.Info(CultureInfo.InvariantCulture, "Waiting {0} seconds", Sleep_Time / 1000.0);
                    System.Threading.Thread.Sleep((int)Sleep_Time);
                }
            }
        }
    }
}