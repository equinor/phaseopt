using System;
using System.Collections;
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

        PhaseOpt_KAR PO_A = new PhaseOpt_KAR(@"PhaseOpt_Kar_A.log");
        PhaseOpt_KAR PO_B = new PhaseOpt_KAR(@"PhaseOpt_Kar_B.log");

        PO_A.Read_Config("PhaseOpt_A.xml");
        PO_B.Read_Config("PhaseOpt_B.xml");

        Queue GC_A_Comp_Asgard = new Queue();
        Queue GC_A_Comp_Statpipe = new Queue();
        Queue GC_B_Comp_Asgard = new Queue();
        Queue GC_B_Comp_Statpipe = new Queue();
        //int memory = 5;

        System.IO.StreamWriter Log_File;
        Log_File = System.IO.File.AppendText(@"stdev.log");
        while (true)
        {
            double Stdev_Low_Limit = 1.0E-10;

            PO_A.Read_Composition();
            PO_B.Read_Composition();

            if (Composition_Stdev(PO_A.Asgard_Comp, GC_A_Comp_Asgard) < Stdev_Low_Limit)
            {
                Console.WriteLine("Bad composition");
            }
            if (Composition_Stdev(PO_A.Statpipe_Comp, GC_A_Comp_Statpipe) < Stdev_Low_Limit)
            {
                Console.WriteLine("Bad composition");
            }
            if (Composition_Stdev(PO_B.Asgard_Comp, GC_B_Comp_Asgard) < Stdev_Low_Limit)
            {
                Console.WriteLine("Bad composition");
            }
            if (Composition_Stdev(PO_B.Statpipe_Comp, GC_B_Comp_Statpipe) < Stdev_Low_Limit)
            {
                Console.WriteLine("Bad composition");
            }

            Log_File.WriteLine();

            if (Check_Composition(PO_A.Asgard_Comp) && Check_Composition(PO_A.Statpipe_Comp))
            {

            }

            PO_A.Read_From_IP21();
            PO_B.Read_From_IP21();

            Log_File.Flush();
            System.Threading.Thread.Sleep(180000);
        }
    }

    public static double Composition_Stdev(List<Component> PO, Queue GC_Comp, int memory=5)
    {
        double Lowest_Stdev = double.MaxValue;
        List<double> Values = new List<double>();
        Dictionary<int, List<double>> CA = new Dictionary<int, List<double>>();

        foreach (Component c in PO)
        {
            Values.Add(c.Value);
        }
        GC_Comp.Enqueue(Values.ToArray());

        while (GC_Comp.Count > memory)
        {
            GC_Comp.Dequeue();
        }

        for (int i = 0; i < PO.Count; i++)
        {
            CA.Add(i, new List<double>());
        }

        foreach (double[] cl in GC_Comp)
        {
            int i = 0;
            foreach (double v in cl)
            {
                CA[i].Add(v);
                i++;
            }
        }

        if (GC_Comp.Count >= memory)
        {
            foreach (List<double> cl in CA.Values)
            {
                double stdev = CalculateStdDev(cl);
                //Log_File.WriteLine(stdev);
                if (stdev < Lowest_Stdev)
                {
                    Lowest_Stdev = stdev;
                }
            }
        }
        return Lowest_Stdev;
    }
    public static bool Check_Composition(List<Component> Composition)
    {
        bool Return_Value = true;
        double Expected_Sum = 100.0;
        double Sum_Deviation_Limit = 1.0;
        double Sum = 0.0;

        double Lower_Limit = 10E-9;
        int Number_Below_Lower_Limit = 0; // Composition.Count * 25 / 100;
        int Below = 0;

        foreach (Component c in Composition)
        {
            Sum += c.Value;
            if (c.Value < Lower_Limit)
                Below++;
        }

        if (Math.Abs(Expected_Sum - Sum) > Sum_Deviation_Limit)
            Return_Value = false;
        if (Below > Number_Below_Lower_Limit)
            Return_Value = false;

        return Return_Value;
    }

    private static double CalculateStdDev(IEnumerable<double> values)
    {
        double ret = 0;
        if (values.Count() > 0)
        {
            //Compute the Average
            double avg = values.Average();
            //Perform the Sum of (value-avg)_2_2
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            //Put it all together
            ret = Math.Sqrt((sum) / (values.Count() - 1));
        }
        return ret;
    }
}

