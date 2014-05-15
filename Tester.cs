using System;
using PhaseOpt;



public static class Tester
{
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
            int[] IDs = new int[25] { 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 14, 8, 15, 16, 17, 18, 701, 705, 707, 801, 806, 710, 901, 906, 911 };
            double[] Values = new double[25] {0.0188591, 0.0053375, 0.8696321, 0.0607237, 0.0267865, 0.0043826,
                    0.0071378, 0.0001517, 0.0019282, 0.0016613, 0.0000497, 0.0001451, 0.0000843, 0.0003587,
                    0.0001976, 0.0004511, 0.0002916, 0.000803, 0.0003357, 0.0000517, 0.0003413, 0.0002315,
                    0.0000106, 0.000013, 0.0000346};

            double[] Result = PhaseOpt.PhaseOpt.Calculate_Dew_Point_Line(IDs, Values, 5);

            System.Console.WriteLine("Cricondenbar point");
            System.Console.WriteLine("Pressure: {0} bara", Result[0].ToString());
            System.Console.WriteLine("Temperature: {0} K", Result[1].ToString());
            System.Console.WriteLine();

            System.Console.WriteLine("Cricondentherm point");
            System.Console.WriteLine("Pressure: {0} bara", Result[2].ToString());
            System.Console.WriteLine("Temperature: {0} K", Result[3].ToString());

            System.Console.WriteLine("Dew Point Line");
            for (int i = 4; i < Result.Length; i += 2)
            {
                System.Console.WriteLine("Pressure: {0} bara", Result[i].ToString());
                System.Console.WriteLine("Temperature: {0} K", Result[i + 1].ToString());
                System.Console.WriteLine();
            }

            return;
        }
    }
}
