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

            // Dew point line. We use this later to set the max value when searching for drop out pressures
            for (int i = 0; i < Temperature.Length; i++)
            {
                Pres[0, i] = PhaseOpt.PhaseOpt.DewP(IDs, Values, Temperature[i] + 273.15);
            }

            for (int i = 0; i < Dropout.Length; i++)
            {
                for (int j = 0; j < Temperature.Length; j++)
                {
                    Pres[i+1, j] = Dropout_Search(IDs, Values, Dropout[i], Temperature[j] + 273.15, Pres[0, j]);
                    System.Console.WriteLine("Dropout: {0}, Temperture: {1}, Pressure: {2}", Dropout[i], Temperature[j], Pres[i+1, j]);
                }
            }
            DateTime Start_Time = DateTime.Now;
            IP21_Comm DB_Connection = new IP21_Comm("KAR-IP21.statoil.net", "10014");
            DB_Connection.Connect();
            String Tag;
            Int32 Interval = 5;
            for (int j = 0; j < Temperature.Length; j++)
            {
                if (j == 0) DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-Interval * j));
                DB_Connection.Insert_Value("PO_E_T", Temperature[j], Start_Time.AddSeconds(-Interval * (j + 1)));
                
                for (int i = 0; i < Dropout.Length + 1; i++)
                {
                    Tag = "PO_E_P" +  (i).ToString();
                    if (j == 0) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * j));
                    DB_Connection.Insert_Value(Tag, Pres[i, j], Start_Time.AddSeconds(-Interval * (j + 1)));
                    if (j == Temperature.Length - 1) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
                }
                if (j == Temperature.Length - 1) DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
            }
            DB_Connection.Disconnect();
        }

        public static double Dropout_Search(Int32[] IDs, double[] Values, double Dropout, double T, double P_Max, double limit = 0.01)
        {
            double PMax = P_Max;
            double PMin = PMax - 40.0;
            double P = double.NaN;
            double diff = double.MaxValue;
            double Result = double.NaN;
            Int32 n = 0;

            while (diff > limit && n < 50)
            {
                n++;
                if (Result > Dropout)
                {
                    PMin = P;
                }
                else if (Result < Dropout)
                {
                    PMax = P;
                }

                P = PMin + (PMax - PMin) / 2;

                Result = PhaseOpt.PhaseOpt.Dropout(IDs, Values, P, T)[0] * 100.0;
                diff = Math.Abs(Result - Dropout);

                System.Console.WriteLine("dropout: {0}", Result);
                System.Console.WriteLine("diff:    {0}", diff);
            }

            if (n == 50) P = double.NaN;

            return P;
        }
    }
}
