﻿using System;
using System.Collections;
using PhaseOpt;
//using OPC_Client;


public static class Tester
{
    public static void Main(String[] args)
    {
        bool Test_UMR = false;
        bool Test_DB = true;
        foreach (string arg in args)
        {
            System.Console.WriteLine("args: {0}", arg);
            if (arg.Equals(@"/u"))
            {
                Test_UMR = true;
            }
            else if (arg.Equals(@"/d"))
            {
                Test_DB = true;
            }
        }

        if (Test_UMR)
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

        if (Test_DB)
        {
            string[] Tag_Name = new string[5] { "31AI0157A_A", "31AI0157A_B", "31AI0157A_C", "31AI0157A_D", "31AI0157A_E" };
            Hashtable Comp = DB_Interface.tester(Tag_Name, new DateTime(2014, 6, 13, 12, 48, 45));
            DB_Interface.Read_Config();

            return;
        }

        string Config_File_Path = @"cri.conf";
        OPC_Client Client = new OPC_Client();
        Client.Read_Config(Config_File_Path);
        Client.Read_Values();
        double[] Component_Values = Client.Components;
        int[] Component_IDs = Client.Component_IDs;

        double[] Res = PhaseOpt.PhaseOpt.Calculate_Dew_Point_Line(Component_IDs, Component_Values, 5);

        System.Console.WriteLine("Cricondenbar point");
        System.Console.WriteLine("Pressure: {0} bara", Res[0].ToString());
        System.Console.WriteLine("Temperature: {0} K", Res[1].ToString());
        System.Console.WriteLine();

        System.Console.WriteLine("Cricondentherm point");
        System.Console.WriteLine("Pressure: {0} bara", Res[2].ToString());
        System.Console.WriteLine("Temperature: {0} K", Res[3].ToString());
        System.Console.WriteLine();

        System.Console.WriteLine("Dew Point Line");
        for (int i = 4; i < Res.Length; i += 2)
        {
            System.Console.WriteLine("Pressure: {0} bara", Res[i].ToString());
            System.Console.WriteLine("Temperature: {0} K", Res[i + 1].ToString());
            System.Console.WriteLine();
        }

    }
}
