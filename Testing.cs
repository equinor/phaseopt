using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test_Space
{
    public static class Testing
    {
        public static void Main_Test()
        {
            Int32[] IDs = new Int32[22] { 1, 2, 101, 201, 301, 401, 402, 503, 504, 603, 604, 605, 701, 606, 608, 801, 707, 710, 901, 806, 809, 1016 };
            double[] Values = new double[22] { 2.483, 0.738, 81.667, 8.393, 4.22, 0.605, 1.084, 0.24, 0.23, 0.0801, 0.0243, 0.0614,
                    0.0233, 0.0778, 0.0191, 0.0048, 0.0302, 0.0116, 0.0023, 0.0017, 0.0022, 0.0014 };
            double[] Temperature = new double[13] { -25.0, -22.5, -20.0, -17.5, -15.0, -12.5, -10.0, -7.5, -5.0, -2.5, 0.0, 2.5, 5.0 };
            double[] Dropout = new double[5] { 0.1, 0.5, 1.0, 2.0, 5.0 };
            double[,] Pres = new double[Dropout.Length + 1, Temperature.Length];
            double[,] Operation_Point = new double[3, 2]; // [Pressure, Temperature]

            Operation_Point[0, 0] = 107.3; Operation_Point[0, 1] = -3.8;
            Operation_Point[1, 0] = 106.6; Operation_Point[1, 1] = -9.3;
            Operation_Point[2, 0] = 108.1; Operation_Point[2, 1] = -1.5;

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

            double[] Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(IDs, Values);

            Double[] Z = PhaseOpt.PhaseOpt.Fluid_Tune(IDs, Values);
            Values = Z;

            Environment.Exit(0);
            


            // Dew point line. We use this later to set the max value when searching for drop out pressures
            for (int i = 0; i < Temperature.Length; i++)
            {
                Pres[0, i] = PhaseOpt.PhaseOpt.DewP(IDs, Values, Temperature[i] + 273.15);
            }

            for (int i = 0; i < Dropout.Length; i++)
            {
                for (int j = 0; j < Temperature.Length; j++)
                {
                    Pres[i + 1, j] = PhaseOpt.PhaseOpt.Dropout_Search(IDs, Values, Dropout[i], Temperature[j] + 273.15, Pres[0, j]);
                    System.Console.WriteLine("Dropout: {0}, Temperture: {1}, Pressure: {2}", Dropout[i], Temperature[j], Pres[i+1, j]);
                }
            }


            DateTime Start_Time = DateTime.Now;
            Start_Time = Start_Time.AddMilliseconds(-Start_Time.Millisecond);
            IP21_Comm DB_Connection = new IP21_Comm("KAR-IP21.statoil.net", "10014");
            DB_Connection.Connect();
            String Tag;
            Int32 Interval = 3;

            for (int j = 0; j < Operation_Point.GetUpperBound(0) + 1; j++)
            {
                if (j == 0)
                {
                    DB_Connection.Insert_Value("PO_OP" + (j).ToString(), double.NaN, Start_Time);
                    DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time);
                }

                DB_Connection.Insert_Value("PO_OP" + (j).ToString(), Operation_Point[j, 0], Start_Time.AddSeconds(-(Interval * j + 1)));
                DB_Connection.Insert_Value("PO_E_T", Operation_Point[j, 1], Start_Time.AddSeconds(-(Interval * j + 1)));

                DB_Connection.Insert_Value("PO_OP" + (j).ToString(), Operation_Point[j, 0], Start_Time.AddSeconds(-(Interval * j + 2)));
                DB_Connection.Insert_Value("PO_E_T", Operation_Point[j, 1], Start_Time.AddSeconds(-(Interval * j + 2)));

                DB_Connection.Insert_Value("PO_OP" + (j).ToString(), double.NaN, Start_Time.AddSeconds(-(Interval * j + 3)));
                DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-(Interval * j + 3)));

            }

            Start_Time = Start_Time.AddSeconds(-(Interval * (Operation_Point.GetUpperBound(0) + 1) + 1));
            Interval = 5;
            for (int j = 0; j < Temperature.Length; j++)
            {
                if (j == 0) DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-Interval * j));
                DB_Connection.Insert_Value("PO_E_T", Temperature[j], Start_Time.AddSeconds(-Interval * (j + 1)));

                for (int i = 0; i < Dropout.Length + 1; i++)
                {
                    Tag = "PO_E_P" + (i).ToString();
                    if (j == 0) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * j));
                    DB_Connection.Insert_Value(Tag, Pres[i, j], Start_Time.AddSeconds(-Interval * (j + 1)));
                    if (j == Temperature.Length - 1) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
                }
                if (j == Temperature.Length - 1) DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
            }
            DB_Connection.Disconnect();
        }
    }
}
