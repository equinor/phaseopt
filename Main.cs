using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class Main_Class
{
    public static void Main(String[] args)
    {
        bool Test_UMR = false;
        foreach (string arg in args)
        {
            if (arg.Equals(@"/u"))
            {
                Test_UMR = true;
            }
        }

        if (Test_UMR)
        {
            int[] IDs = new int[22] { 1, 2, 3, 4, 5, 6, 7, 10, 11, 16, 17, 18, 701, 705, 707, 801, 806, 810, 901, 906, 911, 101101 };
            double[] Values = new double[22] { 2.483, 0.738, 81.667, 8.393, 4.22, 0.605, 1.084, 0.24, 0.23, 0.0801, 0.0243, 0.0614,
                0.0233, 0.0778, 0.0191, 0.0048, 0.0302, 0.0116, 0.0023, 0.0017, 0.0022, 0.0014};


            double[] Result = PhaseOpt.PhaseOpt.Calculate_Dew_Point_Line(IDs, Values, 5);

            Console.WriteLine("Cricondenbar point");
            Console.WriteLine("Pressure: {0} barg", Result[0].ToString());
            Console.WriteLine("Temperature: {0} °C", Result[1].ToString());
            Console.WriteLine();

            Console.WriteLine("Cricondentherm point");
            Console.WriteLine("Pressure: {0} barg", Result[2].ToString());
            Console.WriteLine("Temperature: {0} °C", Result[3].ToString());

            Console.WriteLine("Dew Point Line");
            for (int i = 4; i < Result.Length; i += 2)
            {
                Console.WriteLine("Pressure: {0} barg", Result[i].ToString());
                Console.WriteLine("Temperature: {0} °C", Result[i + 1].ToString());
                Console.WriteLine();
            }

            double[] Dens_Result = PhaseOpt.PhaseOpt.Calculate_Density_And_Compressibility(IDs, Values);

            Console.WriteLine("Vapour density: {0} kg/m­³", Dens_Result[0]);
            Console.WriteLine("Compressibility factor: {0}", Dens_Result[2]);
            Console.WriteLine("Liquid density: {0} kg/m­³", Dens_Result[1]);
            Console.WriteLine("Compressibility factor: {0}", Dens_Result[3]);

            return;
        }

        PhaseOpt_KAR GC_A = new PhaseOpt_KAR();
        PhaseOpt_KAR GC_B = new PhaseOpt_KAR();

        GC_A.Calculate();

    }
}

