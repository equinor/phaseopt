using System;
using System.Collections;
using System.Collections.Generic;
using PhaseOpt;
using System.Xml;

//using OPC_Client;


public static class Tester
{
    private static List<int> Asgard_IDs = new List<int>();
    private static List<string> Asgard_Tags = new List<string>();
    private static List<double> Asgard_Scale_Factors = new List<double>();
    private static List<double> Asgard_Values = new List<double>();
    private static List<string> Asgard_Velocity_Tags = new List<string>();
    private static double Asgard_Pipe_Length;

    private static List<int> Statpipe_IDs = new List<int>();
    private static List<string> Statpipe_Tags = new List<string>();
    private static List<double> Statpipe_Scale_Factors = new List<double>();
    private static List<double> Statpipe_Values = new List<double>();
    private static List<string> Statpipe_Velocity_Tags = new List<string>();
    private static double Statpipe_Pipe_Length;

    private static string Tunneller_Opc;
    private static string IP21_Host;
    private static string IP21_Port;
    private static string IP21_Uid;
    private static string IP21_Pwd;

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
            Read_Config("PhaseOpt.xml");

            // There might not be values in IP21 at Now, so we fetch slightly older values.
            Hashtable A_Velocity = Read_Values(Asgard_Velocity_Tags.ToArray(), DateTime.Now.AddSeconds(-5));
            Hashtable S_Velocity = Read_Values(Statpipe_Velocity_Tags.ToArray(), DateTime.Now.AddSeconds(-5));
            double Asgard_Average_Velocity = ((float)A_Velocity[Asgard_Velocity_Tags[0]] +
                                              (float)A_Velocity[Asgard_Velocity_Tags[1]] ) / 2.0;
            double Statpipe_Average_Velocity = ((float)S_Velocity[Statpipe_Velocity_Tags[0]] +
                                                (float)S_Velocity[Statpipe_Velocity_Tags[1]]) / 2.0;
            DateTime Asgard_Timestamp = DateTime.Now.AddSeconds(-(Asgard_Pipe_Length / Asgard_Average_Velocity));
            DateTime Statpipe_Timestamp = DateTime.Now.AddSeconds(-(Statpipe_Pipe_Length / Statpipe_Average_Velocity));

            string[] A_Tags = Asgard_Tags.ToArray();
            string[] S_Tags = Statpipe_Tags.ToArray();

            Hashtable A_Comp = Read_Values(A_Tags, Asgard_Timestamp);
            Hashtable S_Comp = Read_Values(S_Tags, Statpipe_Timestamp);
            bool Asgard_Composition_Valid = true;
            bool Statpipe_Composition_Valid = true;

            for (int i = 0; i < A_Tags.Length; i++)
            {
                try
                {
                    Asgard_Values.Add((float)A_Comp[A_Tags[i]] * Asgard_Scale_Factors[i]);
                }
                catch
                {
                    System.Console.WriteLine("Tag {0} not valid", A_Tags[i]);
                    Asgard_Composition_Valid = false;
                }
            }

            for (int i = 0; i < S_Tags.Length; i++)
            {
                try
                {
                    Statpipe_Values.Add((float)S_Comp[S_Tags[i]] * Statpipe_Scale_Factors[i]);
                }
                catch
                {
                    System.Console.WriteLine("Tag {0} not valid", S_Tags[i]);
                    Statpipe_Composition_Valid = false;
                }
            }

            if (Asgard_Composition_Valid)
            {
                double[] Result = PhaseOpt.PhaseOpt.Calculate_Dew_Point_Line(Asgard_IDs.ToArray(), Asgard_Values.ToArray(), 5);
            }

            if (Statpipe_Composition_Valid)
            {
                double[] Result = PhaseOpt.PhaseOpt.Calculate_Dew_Point_Line(Statpipe_IDs.ToArray(), Statpipe_Values.ToArray(), 5);
            }

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

    public static void Read_Config(string Config_File)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        XmlReader reader = XmlReader.Create(Config_File, settings);

        try
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "tunneller-opc")
                {
                    reader.Read();
                    Tunneller_Opc = reader.Value;
                }

                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "IP21-connection")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "IP21-connection")
                            break;
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "host")
                        {
                            reader.Read();
                            IP21_Host = reader.Value;
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "port")
                        {
                            reader.Read();
                            IP21_Port = reader.Value;
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "uid")
                        {
                            reader.Read();
                            IP21_Uid = reader.Value;
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "pwd")
                        {
                            reader.Read();
                            IP21_Pwd = reader.Value;
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "streams")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "streams")
                            break;
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "stream" && reader.GetAttribute("name") == "asgard")
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "stream")
                                    break;
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                {
                                    Asgard_IDs.Add(XmlConvert.ToInt32(reader.GetAttribute("id")));
                                    Asgard_Scale_Factors.Add(XmlConvert.ToDouble(reader.GetAttribute("scale-factor")));
                                    Asgard_Tags.Add(reader.GetAttribute("tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "velocity")
                                {
                                    Asgard_Velocity_Tags.Add(reader.GetAttribute("kalsto-tag"));
                                    Asgard_Velocity_Tags.Add(reader.GetAttribute("karsto-tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "length")
                                {
                                    reader.Read();
                                    Asgard_Pipe_Length = XmlConvert.ToDouble(reader.Value);
                                }
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "stream" && reader.GetAttribute("name") == "statpipe")
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "stream")
                                    break;
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                {
                                    Statpipe_IDs.Add(XmlConvert.ToInt32(reader.GetAttribute("id")));
                                    Statpipe_Scale_Factors.Add(XmlConvert.ToDouble(reader.GetAttribute("scale-factor")));
                                    Statpipe_Tags.Add(reader.GetAttribute("tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "velocity")
                                {
                                    Statpipe_Velocity_Tags.Add(reader.GetAttribute("kalsto-tag"));
                                    Statpipe_Velocity_Tags.Add(reader.GetAttribute("karsto-tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "length")
                                {
                                    reader.Read();
                                    Statpipe_Pipe_Length = XmlConvert.ToDouble(reader.Value);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            System.Console.WriteLine("Error reading config file value {0}", reader.Value);
            System.Environment.Exit(1);
        }
    }

    public static Hashtable Read_Values(string[] Tag_Name, DateTime Time_Stamp)
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port + @";UID=" + IP21_Uid +
                      @";PWD=";

        System.Data.Odbc.OdbcCommand Cmd = new System.Data.Odbc.OdbcCommand();
        Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmd.Connection.Open();

        string Tag_cond = "(FALSE"; // Makes it easier to add OR conditions.
        foreach (string Tag in Tag_Name)
        {
            Tag_cond += "\n  OR NAME = '" + Tag + "'";
        }
        Tag_cond += ")";
        Cmd.CommandText =
@"SELECT
  NAME, VALUE
FROM
  History
WHERE
  " + Tag_cond + @"
  AND TS > CAST('" + Time_Stamp.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS')
  AND TS < CAST('" + Time_Stamp.AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS')
  AND PERIOD = 000:0:01.0;
";

        System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();
        Hashtable Result = new Hashtable();
        while (DR.Read())
        {
            System.Console.WriteLine("{0}\t{1}", DR.GetValue(0), DR.GetValue(1));
            Result.Add(DR.GetValue(0), DR.GetValue(1));
        }
        Cmd.Connection.Close();
        return Result;
    }

}
