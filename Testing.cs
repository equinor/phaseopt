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
            IP21_Comm DB_Connection = new IP21_Comm("KAR-IP21.statoil.net", "10014");

            Int32[] IDs = new Int32[22] { 1, 2, 101, 201, 301, 401, 402, 503, 504, 603, 604, 605, 701, 606, 608, 801, 707, 710, 901, 806, 809, 1016 };

            double[] Values = new double[22] { 2.483, 0.738, 81.667, 8.393, 4.22, 0.605, 1.084, 0.24, 0.23, 0.0801, 0.0243, 0.0614,
                    0.0233, 0.0778, 0.0191, 0.0048, 0.0302, 0.0116, 0.0023, 0.0017, 0.0022, 0.0014 };

            double T = -5.0 + 273.15;

            double Result = Dropout_Search(IDs, Values, 0.1, T);

            DB_Connection.Connect();
            DB_Connection.Insert_Value("PO_E_P0", 100.354, DateTime.Now.AddSeconds(-20));

            DB_Connection.Disconnect();

        }

        public static double Dropout_Search(Int32[] IDs, double[] Values, double Dropout, double T, double P_Min = 25.0, double P_Max = 150.0, double limit = 0.0001)
        {
            double PMin = P_Min;
            double PMax = P_Max;
            double P = double.NaN;
            double diff = double.MaxValue;
            double Result = double.NaN;

            while (diff > limit)
            {
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
            }

            return P;
        }
    }
}
