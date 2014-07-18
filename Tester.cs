using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public static class Tester
{
    public class Component
    {
        public int ID;
        public string Tag;
        public double Scale_Factor;
        public double Value;

        public Component(int i, string t, double s, double v = 0.0)
        {
            ID = i;
            Tag = t;
            Scale_Factor = s;
            Value = v;
        }

        public double Get_Scaled_Value()
        {
            return Value * Scale_Factor;
        }
    }

    private static List<Component> Asgard_Comp = new List<Component>();
    private static List<string> Asgard_Velocity_Tags = new List<string>();
    private static List<string> Asgard_Mass_Flow_Tags = new List<string>();
    private static double Asgard_Pipe_Length;
    private static string Asgard_Molweight_Tag;
    private static List<string> Asgard_Cricondenbar_Tags = new List<string>();

    private static List<Component> Statpipe_Comp = new List<Component>();
    private static List<string> Statpipe_Velocity_Tags = new List<string>();
    private static List<string> Statpipe_Mass_Flow_Tags = new List<string>();
    private static double Statpipe_Pipe_Length;
    private static string Statpipe_Molweight_Tag;
    private static List<string> Statpipe_Cricondenbar_Tags = new List<string>();

    private static List<Component> Mix_To_T410_Comp = new List<Component>();
    private static List<string> Mix_To_T410_Cricondenbar_Tags = new List<string>();

    private static List<Component> Mix_To_T100_Comp = new List<Component>();
    private static List<string> Mix_To_T100_Cricondenbar_Tags = new List<string>();
    private static List<string> Mix_To_T100_Mass_Flow_Tags = new List<string>();
    private static string Mix_To_T100_Molweight_Tag;

    private static string Tunneller_Opc;
    private static string IP21_Host;
    private static string IP21_Port;
    private static string IP21_Uid;
    private static string IP21_Pwd;

    public static void Main(String[] args)
    {
        bool Test_UMR = false;
        foreach (string arg in args)
        {
            Console.WriteLine("args: {0}", arg);
            if (arg.Equals(@"/u"))
            {
                Test_UMR = true;
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

            Console.WriteLine("Cricondenbar point");
            Console.WriteLine("Pressure: {0} bara", Result[0].ToString());
            Console.WriteLine("Temperature: {0} K", Result[1].ToString());
            Console.WriteLine();

            Console.WriteLine("Cricondentherm point");
            Console.WriteLine("Pressure: {0} bara", Result[2].ToString());
            Console.WriteLine("Temperature: {0} K", Result[3].ToString());

            Console.WriteLine("Dew Point Line");
            for (int i = 4; i < Result.Length; i += 2)
            {
                Console.WriteLine("Pressure: {0} bara", Result[i].ToString());
                Console.WriteLine("Temperature: {0} K", Result[i + 1].ToString());
                Console.WriteLine();
            }

            return;
        }

        Read_Config("PhaseOpt.xml");

        // Read velocities
        // There might not be values in IP21 at Now, so we fetch slightly older values.
        DateTime Timestamp = DateTime.Now.AddSeconds(-15);
        Hashtable A_Velocity = Read_Values(Asgard_Velocity_Tags.ToArray(), Timestamp);
        double Asgard_Average_Velocity = 0.0;
        try
        {
            Asgard_Average_Velocity = ((float)A_Velocity[Asgard_Velocity_Tags[0]] +
                                                (float)A_Velocity[Asgard_Velocity_Tags[1]]) / 2.0;
        }
        catch
        {
            Console.WriteLine("Tag {0} and/or {1} not valid", Asgard_Velocity_Tags[0], Asgard_Velocity_Tags[1]);
            Environment.Exit(13);
        }
        Hashtable S_Velocity = Read_Values(Statpipe_Velocity_Tags.ToArray(), Timestamp);
        double Statpipe_Average_Velocity = 0.0;
        try
        {
            Statpipe_Average_Velocity = ((float)S_Velocity[Statpipe_Velocity_Tags[0]] +
                                                (float)S_Velocity[Statpipe_Velocity_Tags[1]]) / 2.0;
        }
        catch
        {
            Console.WriteLine("Tag {0} and/or {1} not valid", Statpipe_Velocity_Tags[0], Statpipe_Velocity_Tags[1]);
            Environment.Exit(13);
        }
        if (Asgard_Average_Velocity < 0.1 && Statpipe_Average_Velocity > 0.1) Asgard_Average_Velocity = Statpipe_Average_Velocity;
        if (Statpipe_Average_Velocity < 0.1 && Asgard_Average_Velocity > 0.1) Statpipe_Average_Velocity = Asgard_Average_Velocity;
        if (Asgard_Average_Velocity < 0.1 && Statpipe_Average_Velocity < 0.1) return;
        DateTime Asgard_Timestamp = DateTime.Now.AddSeconds(-(Asgard_Pipe_Length / Asgard_Average_Velocity));
        DateTime Statpipe_Timestamp = DateTime.Now.AddSeconds(-(Statpipe_Pipe_Length / Statpipe_Average_Velocity));

        // Read molweight
        Hashtable Molweight = Read_Values(new string[] { Asgard_Molweight_Tag }, Asgard_Timestamp);
        double Asgard_Molweight = 0.0;
        try
        {
            Asgard_Molweight = (float)Molweight[Asgard_Molweight_Tag];
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Asgard_Molweight_Tag);
            Environment.Exit(13);
        }
        Molweight = Read_Values(new string[] { Statpipe_Molweight_Tag }, Statpipe_Timestamp);
        double Statpipe_Molweight = 0.0;
        try
        {
            Statpipe_Molweight = (float)Molweight[Statpipe_Molweight_Tag];
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Statpipe_Molweight_Tag);
            Environment.Exit(13);
        }
        Molweight = Read_Values(new string[] { Mix_To_T100_Molweight_Tag }, Timestamp);
        double Mix_To_T100_Molweight = 0.0;
        try
        {
            Mix_To_T100_Molweight = (float)Molweight[Mix_To_T100_Molweight_Tag];
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Mix_To_T100_Molweight_Tag);
            Environment.Exit(13);
        }

        // Read mass flow
        Hashtable Mass_Flow = Read_Values(Asgard_Mass_Flow_Tags.ToArray(), Asgard_Timestamp);
        double Asgard_Transport_Flow = 0.0;
        try
        {
            Asgard_Transport_Flow = (float)Mass_Flow[Asgard_Mass_Flow_Tags[0]];
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Asgard_Mass_Flow_Tags[0]);
            Environment.Exit(13);
        }
        double Asgard_Mol_Flow = Asgard_Transport_Flow * 1000 / Asgard_Molweight;

        Mass_Flow = Read_Values(Statpipe_Mass_Flow_Tags.ToArray(), Statpipe_Timestamp);
        double Statpipe_Transport_Flow = 0.0;
        try
        {
            Statpipe_Transport_Flow = (float)Mass_Flow[Statpipe_Mass_Flow_Tags[0]];
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Statpipe_Mass_Flow_Tags[0]);
            Environment.Exit(13);
        }
        double Statpipe_Mol_Flow = Statpipe_Transport_Flow * 1000 / Statpipe_Molweight;

        Mass_Flow = Read_Values(Mix_To_T100_Mass_Flow_Tags.ToArray(), Timestamp);
        double Mix_To_T100_Flow = 0.0;
        double Statpipe_Cross_Over_Flow = 0.0;
        try
        {
            Mix_To_T100_Flow = (float)Mass_Flow[Mix_To_T100_Mass_Flow_Tags[0]];
            Statpipe_Cross_Over_Flow = (float)Mass_Flow[Mix_To_T100_Mass_Flow_Tags[1]];
        }
        catch
        {
            Console.WriteLine("Tag {0} and/or {1} not valid", Mix_To_T100_Mass_Flow_Tags[0], Mix_To_T100_Mass_Flow_Tags[1]);
            Environment.Exit(13);
        }
        double Mix_To_T100_Mol_Flow = Mix_To_T100_Flow * 1000 / Mix_To_T100_Molweight;
        double Statpipe_Cross_Over_Mol_Flow = Statpipe_Cross_Over_Flow * 1000 / Mix_To_T100_Molweight;

        // Read composition
        List<string> Tags = new List<string>();
        foreach (Component c in Asgard_Comp)
        {
            Tags.Add(c.Tag);
        }
        Hashtable Comp_Values = Read_Values(Tags.ToArray(), Asgard_Timestamp);
        string Tag_Name = "";
        try
        {
            foreach (Component c in Asgard_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = (float)Comp_Values[c.Tag] * c.Scale_Factor;
            }
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Tag_Name);
            Environment.Exit(13);
        }
        Tags.Clear();
        Comp_Values.Clear();
        foreach (Component c in Statpipe_Comp)
        {
            Tags.Add(c.Tag);
        }
        Comp_Values = Read_Values(Tags.ToArray(), Statpipe_Timestamp);
        try
        {
            foreach (Component c in Statpipe_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = (float)Comp_Values[c.Tag] * c.Scale_Factor;
            }
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Tag_Name);
            Environment.Exit(13);
        }

        List<Component> Asgard_Component_Flow = new List<Component>();
        double Asgard_Component_Flow_Sum = 0.0;
        foreach (Component c in Asgard_Comp)
        {
            Asgard_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Asgard_Mol_Flow * c.Value / 100.0));
            Asgard_Component_Flow_Sum += Asgard_Mol_Flow * c.Value / 100.0;
        }

        List<Component> Statpipe_Component_Flow = new List<Component>();
        double Statpipe_Component_Flow_Sum = 0.0;
        foreach (Component c in Statpipe_Comp)
        {
            Statpipe_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Statpipe_Mol_Flow * c.Value / 100.0));
            Statpipe_Component_Flow_Sum += Statpipe_Mol_Flow * c.Value / 100.0;
        }

        List<double> Mix_To_T410 = new List<double>();
        foreach (Component c in Mix_To_T410_Comp)
        {
            if (Asgard_Component_Flow_Sum + Statpipe_Component_Flow_Sum > 0.0)
            {
                c.Value = (Asgard_Component_Flow.Find(x => x.ID == c.ID).Value +
                              Statpipe_Component_Flow.Find(x => x.ID == c.ID).Value) /
                              (Asgard_Component_Flow_Sum + Statpipe_Component_Flow_Sum) * 100.0;
            }
        }

        List<Component> CY2007_Component_Flow = new List<Component>();
        double CY2007_Component_Flow_Sum = 0.0;
        foreach (Component c in Asgard_Comp)
        {
            CY2007_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Statpipe_Cross_Over_Mol_Flow * c.Value / 100.0));
            CY2007_Component_Flow_Sum += Statpipe_Cross_Over_Mol_Flow * c.Value / 100.0;
        }

        List<Component> DIXO_Component_Flow = new List<Component>();
        double DIXO_Component_Flow_Sum = 0.0;
        foreach (Component c in Mix_To_T410_Comp)
        {
            DIXO_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Mix_To_T100_Mol_Flow * c.Value / 100.0));
            DIXO_Component_Flow_Sum += Mix_To_T100_Mol_Flow * c.Value / 100.0;
        }

        List<double> Mix_To_T100 = new List<double>();
        foreach (Component c in Mix_To_T100_Comp)
        {
            if (CY2007_Component_Flow_Sum + DIXO_Component_Flow_Sum > 0.0)
            {
                c.Value = (CY2007_Component_Flow.Find(x => x.ID == c.ID).Value +
                              DIXO_Component_Flow.Find(x => x.ID == c.ID).Value) /
                              (CY2007_Component_Flow_Sum + DIXO_Component_Flow_Sum) * 100.0;
            }
        }

        foreach (Component c in Mix_To_T100_Comp)
        {
            Write_Value(c.Tag, c.Get_Scaled_Value());
        }

        foreach (Component c in Mix_To_T410_Comp)
        {
            Write_Value(c.Tag, c.Get_Scaled_Value());
        }

        List<int> Composition_IDs = new List<int>();
        List<double> Composition_Values = new List<double>();
        Tags.Clear();
        Comp_Values.Clear();
        foreach (Component c in Asgard_Comp)
        {
            Tags.Add(c.Tag);
        }
        Comp_Values = Read_Values(Tags.ToArray(), Timestamp);
        try
        {
            foreach (Component c in Asgard_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = (float)Comp_Values[c.Tag] * c.Scale_Factor;
                Composition_IDs.Add(c.ID);
                Composition_Values.Add(c.Value);
            }
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Tag_Name);
            Environment.Exit(13);
        }

        double[] Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Asgard_Cricondenbar_Tags.Count; i++)
        {
            Write_Value(Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
        }

        Composition_IDs.Clear();
        Composition_Values.Clear();
        Tags.Clear();
        Comp_Values.Clear();
        foreach (Component c in Statpipe_Comp)
        {
            Tags.Add(c.Tag);
        }
        Comp_Values = Read_Values(Tags.ToArray(), Statpipe_Timestamp);
        try
        {
            foreach (Component c in Statpipe_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = (float)Comp_Values[c.Tag] * c.Scale_Factor;
                Composition_IDs.Add(c.ID);
                Composition_Values.Add(c.Value);
            }
        }
        catch
        {
            Console.WriteLine("Tag {0} not valid", Tag_Name);
            Environment.Exit(13);
        }

        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Statpipe_Cricondenbar_Tags.Count; i++)
        {
            Write_Value(Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
        }

        Composition_IDs.Clear();
        Composition_Values.Clear();
        foreach (Component c in Mix_To_T100_Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
        }
        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Mix_To_T100_Cricondenbar_Tags.Count; i++)
        {
            Write_Value(Mix_To_T100_Cricondenbar_Tags[i], Composition_Result[i]);
        }

        Composition_IDs.Clear();
        Composition_Values.Clear();
        foreach (Component c in Mix_To_T410_Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
        }
        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Mix_To_T410_Cricondenbar_Tags.Count; i++)
        {
            Write_Value(Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
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
                                    Asgard_Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                             reader.GetAttribute("tag"),
                                                             XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
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
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "molweight")
                                {
                                    Asgard_Molweight_Tag = reader.GetAttribute("tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Asgard_Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Asgard_Cricondenbar_Tags.Add(reader.GetAttribute("pressure-tag"));
                                    Asgard_Cricondenbar_Tags.Add(reader.GetAttribute("temperature-tag"));
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
                                    Statpipe_Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                               reader.GetAttribute("tag"),
                                                               XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));

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
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "molweight")
                                {
                                    Statpipe_Molweight_Tag = reader.GetAttribute("tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Statpipe_Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Statpipe_Cricondenbar_Tags.Add(reader.GetAttribute("pressure-tag"));
                                    Statpipe_Cricondenbar_Tags.Add(reader.GetAttribute("temperature-tag"));
                                }
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "stream" && reader.GetAttribute("name") == "DIXO-mix to T410/T420/DPCU")
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "stream")
                                    break;
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                {
                                    Mix_To_T410_Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                                       reader.GetAttribute("tag"),
                                                                       XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Mix_To_T410_Cricondenbar_Tags.Add(reader.GetAttribute("pressure-tag"));
                                    Mix_To_T410_Cricondenbar_Tags.Add(reader.GetAttribute("temperature-tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Statpipe_Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Mix_To_T410_Cricondenbar_Tags.Add(reader.GetAttribute("pressure-tag"));
                                    Mix_To_T410_Cricondenbar_Tags.Add(reader.GetAttribute("temperature-tag"));
                                }
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "stream" && reader.GetAttribute("name") == "DIXO-mix to T100/T200")
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "stream")
                                    break;
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                {
                                    Mix_To_T100_Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                                       reader.GetAttribute("tag"),
                                                                       XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Mix_To_T100_Cricondenbar_Tags.Add(reader.GetAttribute("pressure-tag"));
                                    Mix_To_T100_Cricondenbar_Tags.Add(reader.GetAttribute("temperature-tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "molweight")
                                {
                                    Mix_To_T100_Molweight_Tag = reader.GetAttribute("tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Mix_To_T100_Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Mix_To_T100_Cricondenbar_Tags.Add(reader.GetAttribute("pressure-tag"));
                                    Mix_To_T100_Cricondenbar_Tags.Add(reader.GetAttribute("temperature-tag"));
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            Console.WriteLine("Error reading config file value {0}", reader.Value);
            Environment.Exit(1);
        }
    }

    public static Hashtable Read_Values(string[] Tag_Name, DateTime Time_Stamp)
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;

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
            Result.Add(DR.GetValue(0), DR.GetValue(1));
        }
        Cmd.Connection.Close();
        return Result;
    }

    public static void Write_Value(string Tag_Name, double Value)
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;

        System.Data.Odbc.OdbcCommand Cmd = new System.Data.Odbc.OdbcCommand();
        Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmd.Connection.Open();

        Cmd.CommandText =
@"UPDATE ip_analogdef
  SET ip_input_value = " + Value.ToString() + @", ip_input_quality = 'Good'
  WHERE name = '" + Tag_Name + @"'";

        System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();
        Cmd.Connection.Close();
    }

}
