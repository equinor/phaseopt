using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

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

    public Component(Component other)
    {
        ID = other.ID;
        Tag = other.Tag;
        Scale_Factor = other.Scale_Factor;
        Value = other.Value;
    }

    public double Get_Scaled_Value()
    {
        return Value * Scale_Factor;
    }
}

public class PhaseOpt_KAR
{
    private List<string> Tags;
    private Hashtable Comp_Values;

    public List<Component> Asgard_Comp = new List<Component>();
    private List<string> Asgard_Velocity_Tags = new List<string>();
    private List<string> Asgard_Mass_Flow_Tags = new List<string>();
    public double Asgard_Transport_Flow;
    private double Asgard_Pipe_Length;
    private string Asgard_Molweight_Tag;
    public double Asgard_Molweight;
    private double Asgard_Mol_Flow;
    private List<string> Asgard_Cricondenbar_Tags = new List<string>();

    public List<Component> Statpipe_Comp = new List<Component>();
    private List<string> Statpipe_Velocity_Tags = new List<string>();
    private List<string> Statpipe_Mass_Flow_Tags = new List<string>();
    public double Statpipe_Transport_Flow;
    private double Statpipe_Pipe_Length;
    private string Statpipe_Molweight_Tag;
    public double Statpipe_Molweight;
    private double Statpipe_Mol_Flow;
    private List<string> Statpipe_Cricondenbar_Tags = new List<string>();
    public double Statpipe_Cross_Over_Flow;
    private double Statpipe_Cross_Over_Mol_Flow;

    private List<Component> Mix_To_T410_Comp = new List<Component>();
    private List<string> Mix_To_T410_Cricondenbar_Tags = new List<string>();

    private List<Component> Mix_To_T100_Comp = new List<Component>();
    private List<string> Mix_To_T100_Cricondenbar_Tags = new List<string>();
    private List<string> Mix_To_T100_Mass_Flow_Tags = new List<string>();
    public double Mix_To_T100_Flow;
    private string Mix_To_T100_Molweight_Tag;
    private double Mix_To_T100_Mol_Flow;

    private string IP21_Host;
    private string IP21_Port;
    private string IP21_Uid;
    private string IP21_Pwd;

    private System.IO.StreamWriter Log_File;
    private DateTime Timestamp;
    private DateTime Asgard_Timestamp;
    private DateTime Statpipe_Timestamp;

    public PhaseOpt_KAR(string Log_File_Name)
    {
        Log_File = System.IO.File.AppendText(Log_File_Name);
    }

    public void Read_Composition()
    {
        // Read velocities
        // There might not be values in IP21 at Now, so we fetch slightly older values.
        Timestamp = DateTime.Now.AddSeconds(-15);
        Log_File.WriteLine();
        Log_File.WriteLine(Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        Hashtable A_Velocity = Read_Values(Asgard_Velocity_Tags.ToArray(), Timestamp);
        double Asgard_Average_Velocity = 0.0;
        try
        {
            Asgard_Average_Velocity = ((float)A_Velocity[Asgard_Velocity_Tags[0]] +
                                                (float)A_Velocity[Asgard_Velocity_Tags[1]]) / 2.0;
            Log_File.WriteLine("Åsgard velocity: {0}", Asgard_Average_Velocity);
#if DEBUG
            Console.WriteLine("Åsgard velocity: {0}", Asgard_Average_Velocity);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Asgard_Velocity_Tags[0], Asgard_Velocity_Tags[1]);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        Hashtable S_Velocity = Read_Values(Statpipe_Velocity_Tags.ToArray(), Timestamp);
        double Statpipe_Average_Velocity = 0.0;
        try
        {
            Statpipe_Average_Velocity = ((float)S_Velocity[Statpipe_Velocity_Tags[0]] +
                                                (float)S_Velocity[Statpipe_Velocity_Tags[1]]) / 2.0;
            Log_File.WriteLine("Statpipe velocity: {0}", Statpipe_Average_Velocity);
#if DEBUG
            Console.WriteLine("Statpipe velocity: {0}", Statpipe_Average_Velocity);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Statpipe_Velocity_Tags[0], Statpipe_Velocity_Tags[1]);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        double Velocity_Threshold = 0.1;
        if (Asgard_Average_Velocity < Velocity_Threshold && Statpipe_Average_Velocity > Velocity_Threshold)
            Asgard_Average_Velocity = Statpipe_Average_Velocity;
        if (Statpipe_Average_Velocity < Velocity_Threshold && Asgard_Average_Velocity > Velocity_Threshold)
            Statpipe_Average_Velocity = Asgard_Average_Velocity;
        if (Asgard_Average_Velocity < Velocity_Threshold && Statpipe_Average_Velocity < Velocity_Threshold)
            return;
        Asgard_Timestamp = DateTime.Now.AddSeconds(-(Asgard_Pipe_Length / Asgard_Average_Velocity));
        Statpipe_Timestamp = DateTime.Now.AddSeconds(-(Statpipe_Pipe_Length / Statpipe_Average_Velocity));
        Log_File.WriteLine("Åsgard delay: {0}", Asgard_Pipe_Length / Asgard_Average_Velocity);
        Log_File.WriteLine("Statpipe delay: {0}", Statpipe_Pipe_Length / Statpipe_Average_Velocity);
#if DEBUG
        Console.WriteLine("Åsgard delay: {0}", Asgard_Pipe_Length / Asgard_Average_Velocity);
        Console.WriteLine("Statpipe delay: {0}", Statpipe_Pipe_Length / Statpipe_Average_Velocity);
#endif

        // Read composition
        Tags = new List<string>();
        foreach (Component c in Asgard_Comp)
        {
            Tags.Add(c.Tag);
        }
        Comp_Values = Read_Values(Tags.ToArray(), Asgard_Timestamp);
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
            Log_File.WriteLine("Tag {0} not valid", Tag_Name);
            Log_File.Flush();
            //Environment.Exit(13);
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
            Log_File.WriteLine("Tag {0} not valid", Tag_Name);
            Log_File.Flush();
            //Environment.Exit(13);
        }
    }

    public void Read_From_IP21()
    {
        // Read molweight
        Hashtable Molweight = Read_Values(new string[] { Asgard_Molweight_Tag }, Asgard_Timestamp);
        Asgard_Molweight = 0.0;
        try
        {
            Asgard_Molweight = (float)Molweight[Asgard_Molweight_Tag];
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Asgard_Molweight_Tag);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        Molweight = Read_Values(new string[] { Statpipe_Molweight_Tag }, Statpipe_Timestamp);
        Statpipe_Molweight = 0.0;
        try
        {
            Statpipe_Molweight = (float)Molweight[Statpipe_Molweight_Tag];
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Statpipe_Molweight_Tag);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        Molweight = Read_Values(new string[] { Mix_To_T100_Molweight_Tag }, Timestamp);
        double Mix_To_T100_Molweight = 0.0;
        try
        {
            Mix_To_T100_Molweight = (float)Molweight[Mix_To_T100_Molweight_Tag];
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Mix_To_T100_Molweight_Tag);
            Log_File.Flush();
            //Environment.Exit(13);
        }

        // Read mass flow
        Hashtable Mass_Flow = Read_Values(Asgard_Mass_Flow_Tags.ToArray(), Asgard_Timestamp);
        Asgard_Transport_Flow = 0.0;
#if DEBUG
        Console.WriteLine("\nÅsgard flow:");
#endif
        try
        {
            Asgard_Transport_Flow = (float)Mass_Flow[Asgard_Mass_Flow_Tags[0]];
#if DEBUG
            Console.WriteLine("{0}\t{1}", Asgard_Mass_Flow_Tags[0], Asgard_Transport_Flow);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Asgard_Mass_Flow_Tags[0]);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        Asgard_Mol_Flow = Asgard_Transport_Flow * 1000 / Asgard_Molweight;

        Mass_Flow = Read_Values(Statpipe_Mass_Flow_Tags.ToArray(), Statpipe_Timestamp);
        Statpipe_Transport_Flow = 0.0;
#if DEBUG
        Console.WriteLine("\nStatpipe flow:");
#endif
        try
        {
            Statpipe_Transport_Flow = (float)Mass_Flow[Statpipe_Mass_Flow_Tags[0]];
#if DEBUG
            Console.WriteLine("{0}\t{1}", Statpipe_Mass_Flow_Tags[0], Statpipe_Transport_Flow);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Statpipe_Mass_Flow_Tags[0]);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        Statpipe_Mol_Flow = Statpipe_Transport_Flow * 1000 / Statpipe_Molweight;

        Mass_Flow = Read_Values(Mix_To_T100_Mass_Flow_Tags.ToArray(), Timestamp);
        Mix_To_T100_Flow = 0.0;
        Statpipe_Cross_Over_Flow = 0.0;
        try
        {
            Mix_To_T100_Flow = (float)Mass_Flow[Mix_To_T100_Mass_Flow_Tags[0]];
            Statpipe_Cross_Over_Flow = (float)Mass_Flow[Mix_To_T100_Mass_Flow_Tags[1]];
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Mix_To_T100_Mass_Flow_Tags[0], Mix_To_T100_Mass_Flow_Tags[1]);
            Log_File.Flush();
            //Environment.Exit(13);
        }
        Mix_To_T100_Mol_Flow = Mix_To_T100_Flow * 1000 / Mix_To_T100_Molweight;
        Statpipe_Cross_Over_Mol_Flow = Statpipe_Cross_Over_Flow * 1000 / Mix_To_T100_Molweight;
    }

    public void Calculate()
    {
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

        //List<double> Mix_To_T410 = new List<double>();
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

        //List<double> Mix_To_T100 = new List<double>();
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

        // Åsgard cricondenbar
        List<int> Composition_IDs = new List<int>();
        List<double> Composition_Values = new List<double>();
        Tags.Clear();
        Comp_Values.Clear();
        foreach (Component c in Asgard_Comp)
        {
            Tags.Add(c.Tag);
        }
        Comp_Values = Read_Values(Tags.ToArray(), Timestamp);
        Log_File.WriteLine("Åsgard:");
        string Tag_Name = "";
        try
        {
            foreach (Component c in Asgard_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = (float)Comp_Values[c.Tag] * c.Scale_Factor;
                Composition_IDs.Add(c.ID);
                Composition_Values.Add(c.Value);
                Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Tag_Name);
            Log_File.Flush();
            //Environment.Exit(13);
        }

        Log_File.Flush();
        double[] Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Asgard_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                Write_Value(Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }

        // Statpipe cricondenbar
        Composition_IDs.Clear();
        Composition_Values.Clear();
        Tags.Clear();
        Comp_Values.Clear();
        foreach (Component c in Statpipe_Comp)
        {
            Tags.Add(c.Tag);
        }
        Comp_Values = Read_Values(Tags.ToArray(), Timestamp);
        Log_File.WriteLine("Statpipe:");
        try
        {
            foreach (Component c in Statpipe_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = (float)Comp_Values[c.Tag] * c.Scale_Factor;
                Composition_IDs.Add(c.ID);
                Composition_Values.Add(c.Value);
                Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Tag_Name);
            Log_File.Flush();
            //Environment.Exit(13);
        }

        Log_File.Flush();
        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Statpipe_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                Write_Value(Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }

        // Mix to T100 cricondenbar
        Composition_IDs.Clear();
        Composition_Values.Clear();
        Log_File.WriteLine("Mix to T100:");
        foreach (Component c in Mix_To_T100_Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        Log_File.Flush();
        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Mix_To_T100_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                Write_Value(Mix_To_T100_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Mix_To_T100_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Mix_To_T100_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }

        // Mix to T400 cricondenbar
        Composition_IDs.Clear();
        Composition_Values.Clear();
        Log_File.WriteLine("Mix to T400:");
        foreach (Component c in Mix_To_T410_Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        Log_File.Flush();
        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), Composition_Values.ToArray());
        for (int i = 0; i < Mix_To_T410_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                Write_Value(Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }

        Log_File.Flush();
        //Log_File.Close();
    }

    public void Read_Config(string Config_File)
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
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "IP21-connection")
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
            Log_File.WriteLine("Error reading config file value {0}", reader.Value);
            Log_File.Flush();
            Environment.Exit(1);
        }
    }

    private Hashtable Read_Values(string[] Tag_Name, DateTime Time_Stamp)
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

    private void Write_Value(string Tag_Name, double Value, string Quality = "Good")
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;

        System.Data.Odbc.OdbcCommand Cmd = new System.Data.Odbc.OdbcCommand();
        Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmd.Connection.Open();

        Cmd.CommandText =
@"UPDATE ip_analogdef
  SET ip_input_value = " + Value.ToString() + @", ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();
        Cmd.Connection.Close();
    }
}
