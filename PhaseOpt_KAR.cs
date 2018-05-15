using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Globalization;

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
    public List<double> Composition_Values_Asgard_Current = new List<double>();
    private List<Int32> Composition_IDs_Asgard = new List<Int32>();
    private List<string> Asgard_Velocity_Tags = new List<string>();
    private List<string> Asgard_Mass_Flow_Tags = new List<string>();
    public double Asgard_Transport_Flow;
    private double Asgard_Pipe_Length;
    private string Asgard_Molweight_Tag;
    public double Asgard_Molweight;
    private double Asgard_Mol_Flow;
    private List<string> Asgard_Cricondenbar_Tags = new List<string>();
    private List<string> Asgard_Status_Tags = new List<string>();

    public List<Component> Statpipe_Comp = new List<Component>();
    public List<double> Composition_Values_Statpipe_Current = new List<double>();
    private List<Int32> Composition_IDs_Statpipe = new List<Int32>();
    private List<string> Statpipe_Velocity_Tags = new List<string>();
    private List<string> Statpipe_Mass_Flow_Tags = new List<string>();
    public double Statpipe_Transport_Flow;
    private double Statpipe_Pipe_Length;
    private string Statpipe_Molweight_Tag;
    public double Statpipe_Molweight;
    private double Statpipe_Mol_Flow;
    private List<string> Statpipe_Cricondenbar_Tags = new List<string>();
    private List<string> Statpipe_Status_Tags = new List<string>();
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
    public List<double> Asgard_Velocity = new List<double>();
    public List<double> Statpipe_Velocity = new List<double>();

    public IP21_Comm DB_Connection;

    public PhaseOpt_KAR(string Log_File_Name)
    {
        Log_File = System.IO.File.AppendText(Log_File_Name);
        Asgard_Velocity.Add(0.0); Asgard_Velocity.Add(0.0);
        Statpipe_Velocity.Add(0.0); Statpipe_Velocity.Add(0.0);
    }

    public void Connect_DB()
    {
        DB_Connection = new IP21_Comm(IP21_Host, IP21_Port);
        DB_Connection.Connect();
    }


    public bool Read_Composition()
    {
        // Read velocities
        // There might not be values in IP21 at Now, so we fetch slightly older values.
        Timestamp = DateTime.Now.AddSeconds(-15);
        Hashtable A_Velocity;
        Log_File.WriteLine();
        Log_File.WriteLine(Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

        double Asgard_Average_Velocity = 0.0;
        try
        {
            A_Velocity = DB_Connection.Read_Values(Asgard_Velocity_Tags.ToArray(), Timestamp);
            Asgard_Average_Velocity = (Convert.ToDouble(A_Velocity[Asgard_Velocity_Tags[0]]) +
                                       Convert.ToDouble(A_Velocity[Asgard_Velocity_Tags[1]])) / 2.0;
            Asgard_Velocity[0] = Convert.ToDouble(A_Velocity[Asgard_Velocity_Tags[0]]);
            Asgard_Velocity[1] = Convert.ToDouble(A_Velocity[Asgard_Velocity_Tags[1]]);
            Log_File.WriteLine("Åsgard velocity: {0}", Asgard_Average_Velocity);
#if DEBUG
            Console.WriteLine("Åsgard velocity: {0}", Asgard_Average_Velocity);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Asgard_Velocity_Tags[0], Asgard_Velocity_Tags[1]);
            Log_File.Flush();
        }
        double Statpipe_Average_Velocity = 0.0;
        Hashtable S_Velocity;
        try
        {
            S_Velocity = DB_Connection.Read_Values(Statpipe_Velocity_Tags.ToArray(), Timestamp);
            Statpipe_Average_Velocity = (Convert.ToDouble(S_Velocity[Statpipe_Velocity_Tags[0]]) +
                                                Convert.ToDouble(S_Velocity[Statpipe_Velocity_Tags[1]])) / 2.0;
            Statpipe_Velocity[0] = Convert.ToDouble(S_Velocity[Statpipe_Velocity_Tags[0]]);
            Statpipe_Velocity[1] = Convert.ToDouble(S_Velocity[Statpipe_Velocity_Tags[1]]);
            Log_File.WriteLine("Statpipe velocity: {0}", Statpipe_Average_Velocity);
#if DEBUG
            Console.WriteLine("Statpipe velocity: {0}", Statpipe_Average_Velocity);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Statpipe_Velocity_Tags[0], Statpipe_Velocity_Tags[1]);
            Log_File.Flush();
        }
        double Velocity_Threshold = 0.1;
        if (Asgard_Average_Velocity < Velocity_Threshold && Statpipe_Average_Velocity > Velocity_Threshold)
            Asgard_Average_Velocity = Statpipe_Average_Velocity;
        if (Statpipe_Average_Velocity < Velocity_Threshold && Asgard_Average_Velocity > Velocity_Threshold)
            Statpipe_Average_Velocity = Asgard_Average_Velocity;
        if (Asgard_Average_Velocity < Velocity_Threshold && Statpipe_Average_Velocity < Velocity_Threshold)
        {
            Log_File.WriteLine("Velocity below threshold 0.1 m/s");
#if DEBUG
            Console.WriteLine("Velocity below threshold 0.1 m/s");
#endif
            return false;
        }
        Asgard_Timestamp = DateTime.Now.AddSeconds(-(Asgard_Pipe_Length / Asgard_Average_Velocity));
        Statpipe_Timestamp = DateTime.Now.AddSeconds(-(Statpipe_Pipe_Length / Statpipe_Average_Velocity));
        Log_File.WriteLine("Åsgard delay: {0}", Asgard_Pipe_Length / Asgard_Average_Velocity);
        Log_File.WriteLine("Statpipe delay: {0}", Statpipe_Pipe_Length / Statpipe_Average_Velocity);
#if DEBUG
        Console.WriteLine("Åsgard delay: {0}", Asgard_Pipe_Length / Asgard_Average_Velocity);
        Console.WriteLine("Statpipe delay: {0}", Statpipe_Pipe_Length / Statpipe_Average_Velocity);
#endif

        // Read Åsgard composition
        bool Asgard_Stat = false;
        bool Statpipe_Stat = false;
        Hashtable Asgard_Status;
        try
        {
            Asgard_Status = DB_Connection.Read_Values(Asgard_Status_Tags.ToArray(), Asgard_Timestamp);
            foreach (DictionaryEntry Status in Asgard_Status)
            {
                if (Convert.ToDouble(Status.Value) > 999.9)
                {
                    Asgard_Stat = true;
                }
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Asgard_Status_Tags[0], Asgard_Status_Tags[1]);
            Log_File.WriteLine("GC bad status ");
            Log_File.Flush();
        }

        Tags = new List<string>();
        string Tag_Name = "";

        if (Asgard_Stat)
        {
            foreach (Component c in Asgard_Comp)
            {
                Tags.Add(c.Tag);
            }

            try
            {
                Comp_Values = DB_Connection.Read_Values(Tags.ToArray(), Asgard_Timestamp);
                foreach (Component c in Asgard_Comp)
                {
                    Tag_Name = c.Tag;
                    c.Value = Convert.ToDouble(Comp_Values[c.Tag]) * c.Scale_Factor;
                }
            }
            catch
            {
                Log_File.WriteLine("Tag {0} not valid", Tag_Name);
                Log_File.Flush();
            }

        }
        if (Tags != null) Tags.Clear();
        if (Comp_Values != null) Comp_Values.Clear();

        // Read Statpipe composition
        Hashtable Statpipe_Status;
        try
        {
            Statpipe_Status = DB_Connection.Read_Values(Statpipe_Status_Tags.ToArray(), Statpipe_Timestamp);
            foreach (DictionaryEntry Status in Statpipe_Status)
            {
                if (Convert.ToDouble(Status.Value) > 999.9)
                {
                    Statpipe_Stat = true;
                }
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Statpipe_Status_Tags[0], Statpipe_Status_Tags[1]);
            Log_File.WriteLine("GC bad status ");
            Log_File.Flush();
        }

        if (Statpipe_Stat)
        {
            foreach (Component c in Statpipe_Comp)
            {
                Tags.Add(c.Tag);
            }
            try
            {
                Comp_Values = DB_Connection.Read_Values(Tags.ToArray(), Statpipe_Timestamp);
                foreach (Component c in Statpipe_Comp)
                {
                    Tag_Name = c.Tag;
                    c.Value = Convert.ToDouble(Comp_Values[c.Tag]) * c.Scale_Factor;
                }
            }
            catch
            {
                Log_File.WriteLine("Tag {0} not valid", Tag_Name);
                Log_File.Flush();
            }
        }
        return Asgard_Stat & Statpipe_Stat;
    }

    public void Read_From_IP21()
    {
        // Read molweight
        Hashtable Molweight;
        Asgard_Molweight = 0.0;
        try
        {
            Molweight = DB_Connection.Read_Values(new string[] { Asgard_Molweight_Tag }, Asgard_Timestamp);
            Asgard_Molweight = Convert.ToDouble(Molweight[Asgard_Molweight_Tag]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Asgard_Molweight_Tag);
            Log_File.Flush();
        }
        Statpipe_Molweight = 0.0;
        try
        {
            Molweight = DB_Connection.Read_Values(new string[] { Statpipe_Molweight_Tag }, Statpipe_Timestamp);
            Statpipe_Molweight = Convert.ToDouble(Molweight[Statpipe_Molweight_Tag]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Statpipe_Molweight_Tag);
            Log_File.Flush();
        }
        double Mix_To_T100_Molweight = 0.0;
        try
        {
            Molweight = DB_Connection.Read_Values(new string[] { Mix_To_T100_Molweight_Tag }, Timestamp);
            Mix_To_T100_Molweight = Convert.ToDouble(Molweight[Mix_To_T100_Molweight_Tag]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Mix_To_T100_Molweight_Tag);
            Log_File.Flush();
        }

        // Read mass flow
        Hashtable Mass_Flow;
        Asgard_Transport_Flow = 0.0;
#if DEBUG
        Console.WriteLine("\nÅsgard flow:");
#endif
        try
        {
            Mass_Flow = DB_Connection.Read_Values(Asgard_Mass_Flow_Tags.ToArray(), Asgard_Timestamp);
            Asgard_Transport_Flow = Convert.ToDouble(Mass_Flow[Asgard_Mass_Flow_Tags[0]]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Asgard_Mass_Flow_Tags[0], Asgard_Transport_Flow);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Asgard_Mass_Flow_Tags[0]);
            Log_File.Flush();
        }
        Asgard_Mol_Flow = Asgard_Transport_Flow * 1000 / Asgard_Molweight;

        Statpipe_Transport_Flow = 0.0;
#if DEBUG
        Console.WriteLine("\nStatpipe flow:");
#endif
        try
        {
            Mass_Flow = DB_Connection.Read_Values(Statpipe_Mass_Flow_Tags.ToArray(), Statpipe_Timestamp);
            Statpipe_Transport_Flow = Convert.ToDouble(Mass_Flow[Statpipe_Mass_Flow_Tags[0]]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Statpipe_Mass_Flow_Tags[0], Statpipe_Transport_Flow);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Statpipe_Mass_Flow_Tags[0]);
            Log_File.Flush();
        }
        Statpipe_Mol_Flow = Statpipe_Transport_Flow * 1000 / Statpipe_Molweight;

        Mix_To_T100_Flow = 0.0;
        Statpipe_Cross_Over_Flow = 0.0;
        try
        {
            Mass_Flow = DB_Connection.Read_Values(Mix_To_T100_Mass_Flow_Tags.ToArray(), Timestamp);
            Mix_To_T100_Flow = Convert.ToDouble(Mass_Flow[Mix_To_T100_Mass_Flow_Tags[0]]);
            Statpipe_Cross_Over_Flow = Convert.ToDouble(Mass_Flow[Mix_To_T100_Mass_Flow_Tags[1]]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Mix_To_T100_Mass_Flow_Tags[0], Mix_To_T100_Mass_Flow_Tags[1]);
            Log_File.Flush();
        }
        Mix_To_T100_Mol_Flow = Mix_To_T100_Flow * 1000 / Mix_To_T100_Molweight;
        Statpipe_Cross_Over_Mol_Flow = Statpipe_Cross_Over_Flow * 1000 / Mix_To_T100_Molweight;
    }

    public void Calculate_Karsto()
    {
        // Calculate mixed compositions at Kårstø
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

        foreach (Component c in Mix_To_T100_Comp)
        {
            if (CY2007_Component_Flow_Sum + DIXO_Component_Flow_Sum > 0.0)
            {
                c.Value = (CY2007_Component_Flow.Find(x => x.ID == c.ID).Value +
                              DIXO_Component_Flow.Find(x => x.ID == c.ID).Value) /
                              (CY2007_Component_Flow_Sum + DIXO_Component_Flow_Sum) * 100.0;
            }
        }

        // Write mixed compositions to IP21
        foreach (Component c in Mix_To_T100_Comp)
        {
            DB_Connection.Write_Value(c.Tag, c.Get_Scaled_Value());
        }

        foreach (Component c in Mix_To_T410_Comp)
        {
            DB_Connection.Write_Value(c.Tag, c.Get_Scaled_Value());
        }

        List<Int32> Composition_IDs = new List<Int32>();
        List<double> Composition_Values = new List<double>();

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
        double[] Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), PhaseOpt.PhaseOpt.Fluid_Tune(Composition_IDs.ToArray(),Composition_Values.ToArray()));
        for (int i = 0; i < Mix_To_T100_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                DB_Connection.Write_Value(Mix_To_T100_Cricondenbar_Tags[i], Composition_Result[i]);
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
        Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs.ToArray(), PhaseOpt.PhaseOpt.Fluid_Tune(Composition_IDs.ToArray(), Composition_Values.ToArray()));
        for (int i = 0; i < Mix_To_T410_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                DB_Connection.Write_Value(Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Mix_To_T410_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }

        Log_File.Flush();
    }

    public void Read_Current_Kalsto_Composition()
    {
        if (Tags == null) Tags = new List<string>();

        // Åsgard
        Composition_Values_Asgard_Current.Clear();
        Composition_IDs_Asgard.Clear();
        Tags.Clear();
        if (Comp_Values != null) Comp_Values.Clear();
        foreach (Component c in Asgard_Comp)
        {
            Tags.Add(c.Tag);
        }
        Log_File.WriteLine("Åsgard:");
        string Tag_Name = "";
        try
        {
            Comp_Values = DB_Connection.Read_Values(Tags.ToArray(), Timestamp);
            foreach (Component c in Asgard_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = Convert.ToDouble(Comp_Values[c.Tag]) * c.Scale_Factor;
                Composition_IDs_Asgard.Add(c.ID);
                Composition_Values_Asgard_Current.Add(c.Value);
                Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Tag_Name);
            Log_File.Flush();
            //Environment.Exit(13);
        }

        // Statpipe
        Composition_Values_Statpipe_Current.Clear();
        Composition_IDs_Statpipe.Clear();
        Tags.Clear();
        Comp_Values.Clear();
        foreach (Component c in Statpipe_Comp)
        {
            Tags.Add(c.Tag);
        }
        Log_File.WriteLine("Statpipe:");
        try
        {
            Comp_Values = DB_Connection.Read_Values(Tags.ToArray(), Timestamp);
            foreach (Component c in Statpipe_Comp)
            {
                Tag_Name = c.Tag;
                c.Value = Convert.ToDouble(Comp_Values[c.Tag]) * c.Scale_Factor;
                Composition_IDs_Statpipe.Add(c.ID);
                Composition_Values_Statpipe_Current.Add(c.Value);
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
    }

    public void Calculate_Kalsto_Asgard()
    {
        double[] Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs_Asgard.ToArray(), PhaseOpt.PhaseOpt.Fluid_Tune(Composition_IDs_Asgard.ToArray(), Composition_Values_Asgard_Current.ToArray()));
        for (int i = 0; i < Asgard_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                DB_Connection.Write_Value(Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Asgard_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }
    }

    public void Calculate_Kalsto_Statpipe()
    {
        double[] Composition_Result = PhaseOpt.PhaseOpt.Cricondenbar(Composition_IDs_Statpipe.ToArray(), PhaseOpt.PhaseOpt.Fluid_Tune(Composition_IDs_Statpipe.ToArray(), Composition_Values_Statpipe_Current.ToArray()));
        for (int i = 0; i < Statpipe_Cricondenbar_Tags.Count; i++)
        {
            if (!Composition_Result[i].Equals(double.NaN))
            {
                DB_Connection.Write_Value(Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
            }
            Log_File.WriteLine("{0}\t{1}", Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
#if DEBUG
            Console.WriteLine("{0}\t{1}", Statpipe_Cricondenbar_Tags[i], Composition_Result[i]);
#endif
        }

    }

    public void Calculate_Dropout_Curves()
    {
        List<Int32> Composition_IDs = new List<Int32>();
        List<double> Composition_Values = new List<double>();

        // Mix to T400 dropout
        foreach (Component c in Mix_To_T410_Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        double[] Temperature = new double[13] { -25.0, -22.5, -20.0, -17.5, -15.0, -12.5, -10.0, -7.5, -5.0, -2.5, 0.0, 2.5, 5.0 };
        double[] Dropout = new double[5] { 0.1, 0.5, 1.0, 2.0, 5.0 };
        double[,] Pres = new double[Dropout.Length + 1, Temperature.Length];
        double[,] Operation_Point = new double[3, 2]; // [Pressure, Temperature]

        DateTime Time_Stamp = DateTime.Now.AddSeconds(-15);
        Hashtable OP = DB_Connection.Read_Values(new string[] { "21PI5108", "21TI5044" }, Time_Stamp);
        Operation_Point[0, 0] = Convert.ToDouble(OP["21PI5108"]);
        Operation_Point[0, 1] = Convert.ToDouble(OP["21TI5044"]);

        OP = DB_Connection.Read_Values(new string[] { "21PI5230", "21TC5236" }, Time_Stamp);
        Operation_Point[1, 0] = Convert.ToDouble(OP["21PI5230"]);
        Operation_Point[1, 1] = Convert.ToDouble(OP["21TC5236"]);

        Operation_Point[2, 0] = 108.1; Operation_Point[2, 1] = -1.5;

        double[] Z = PhaseOpt.PhaseOpt.Fluid_Tune(Composition_IDs.ToArray(), Composition_Values.ToArray());


        // Dew point line. We use this later to set the max value when searching for drop out pressures
        for (int i = 0; i < Temperature.Length; i++)
        {
            Pres[0, i] = PhaseOpt.PhaseOpt.DewP(Composition_IDs.ToArray(), Z, Temperature[i] + 273.15);
            System.Console.WriteLine("Dew point: Temperture: {0}, Pressure: {1}", Temperature[i], Pres[0, i] - 1.01325);
        }

        for (int i = 0; i < Dropout.Length; i++)
        {
            for (int j = 0; j < Temperature.Length; j++)
            {
                Pres[i + 1, j] = PhaseOpt.PhaseOpt.Dropout_Search(Composition_IDs.ToArray(), Z, Dropout[i], Temperature[j] + 273.15, Pres[0, j]);
                System.Console.WriteLine("Dropout: {0}, Temperture: {1}, Pressure: {2}", Dropout[i], Temperature[j], Pres[i+1, j] - 1.01325);
            }
        }
        DateTime Start_Time = DateTime.Now;
        Start_Time = Start_Time.AddMilliseconds(-Start_Time.Millisecond);
        String Tag;
        Int32 Interval = 3;
        Double Result = 0.0;

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

            Result = PhaseOpt.PhaseOpt.Dropout(Composition_IDs.ToArray(), Z, Operation_Point[j, 0] + 1.01325, Operation_Point[j, 1] + 273.15)[0] * 100.0;
            DB_Connection.Write_Value("PO_LD" + (j).ToString(), Result);
        }

        Start_Time = Start_Time.AddSeconds(-(Interval * (Operation_Point.GetUpperBound(0) + 1) + 1));
        Interval = 5;
        for (int j = 0; j < Temperature.Length; j++)
        {
            if (j == 0) DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-Interval * j));
            DB_Connection.Insert_Value("PO_E_T", Temperature[j], Start_Time.AddSeconds(-Interval * (j + 1)));

            for (int i = 0; i < Dropout.Length + 1; i++)
            {
                Tag = "PO_E_P" +  (i).ToString();
                if (j == 0) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * j));
                DB_Connection.Insert_Value(Tag, Pres[i, j] - 1.01325, Start_Time.AddSeconds(-Interval * (j + 1)));
                if (j == Temperature.Length - 1) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
            }
            if (j == Temperature.Length - 1) DB_Connection.Insert_Value("PO_E_T", double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
        }
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
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "status")
                                {
                                    Asgard_Status_Tags.Add(reader.GetAttribute("tag"));
                                }
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
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "status")
                                {
                                    Statpipe_Status_Tags.Add(reader.GetAttribute("tag"));
                                }
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
}
