using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

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

public class Dropout_Curve
{
    public struct Dropout_Point
    {
        public double Value;
        public string Tag;

        public Dropout_Point(double v, string t)
        {
            Value = v;
            Tag = t;
        }
    }

    public List<double> Temperature = new List<double>();
    public string Temperature_Tag;
    public string Dew_Point_Tag;
    public List<Dropout_Point> Dropout = new List<Dropout_Point>();
    public List<PT_Point> Operating_Points = new List<PT_Point>();
}

public class Stream
{
    public string Name;
    public List<string> Status_Tags = new List<string>();
    public List<Component> Comp = new List<Component>();
    public string Molweight_Tag;
    public double Molweight;
    public List<string> Velocity_Tags = new List<string>();
    public List<string> Mass_Flow_Tags = new List<string>();
    public double Pipe_Length;
    public PT_Point Cricondenbar = new PT_Point();
    public List<PT_Point> Dropout_Points = new List<PT_Point>();
    public Dropout_Curve Curve = new Dropout_Curve();
    public double Mass_Flow;
    public double Mol_Flow;

    public List<Int32> Composition_IDs()
    {
        List<Int32> ID = new List<Int32>();

        foreach (Component c in Comp)
        {
            ID.Add(c.ID);
        }
        return ID;
    }

    public List<double> Composition_Values()
    {
        List<double> v = new List<double>();

        foreach (Component c in Comp)
        {
            v.Add(c.Value);
        }
        return v;
    }

    public List<string> Composition_tags()
    {
        List<string> tag = new List<string>();

        foreach (Component c in Comp)
        {
            tag.Add(c.Tag);
        }
        return tag;
    }

}

public class PT_Point
{
    public double pressure;
    public double temperature;
    public double result;
    public string pressure_tag;
    public string temperature_tag;
    public string result_tag;
    public string status_tag;

    public PT_Point() { }

    public PT_Point(string pt, string tt, string rt, string st = "")
    {
        pressure_tag = pt;
        temperature_tag = tt;
        result_tag = rt;
        status_tag = st;
    }
}

public class PhaseOpt_KAR
{
    public string Name;
    private const double to_bara = 1.01325;
    private const double to_Kelvin = 273.15;

    private Hashtable Comp_Values;

    PhaseOpt.PhaseOpt T100 = new PhaseOpt.PhaseOpt();
    PhaseOpt.PhaseOpt T400 = new PhaseOpt.PhaseOpt();
    PhaseOpt.PhaseOpt Asgard_Kalsto = new PhaseOpt.PhaseOpt();
    PhaseOpt.PhaseOpt Statpipe_Kalsto = new PhaseOpt.PhaseOpt();

    public readonly object locker = new object();

    public double Statpipe_Cross_Over_Flow;
    private double Statpipe_Cross_Over_Mol_Flow;

    public Stream Mix_To_T410 = new Stream();
    public Stream Mix_To_T100 = new Stream();
    public Stream Asgard = new Stream();
    public Stream Statpipe = new Stream();
    public Stream Asgard_Current = new Stream();
    public Stream Statpipe_Current = new Stream();

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

    private bool Cross_Over_Status = false;

    public IP21_Comm DB_Connection;

    Queue GC_Comp_Asgard = new Queue();
    Queue GC_Comp_Statpipe = new Queue();
    Queue GC_Molweight_Asgard = new Queue();
    Queue GC_Molweight_Statpipe = new Queue();
    Queue GC_Asgard_Flow = new Queue();
    Queue GC_Statpipe_Flow = new Queue();
    Queue GC_Statpipe_Cross_Over_Flow = new Queue();
    Queue GC_Mix_To_T100_Flow = new Queue();
    Queue Velocity_1_Asgard = new Queue();
    Queue Velocity_2_Asgard = new Queue();
    Queue Velocity_1_Statpipe = new Queue();
    Queue Velocity_2_Statpipe = new Queue();


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

    public void IP21_Reader()
    {
        while (DB_Connection.isConnected())
        {
            Read_Composition();
            Read_Current_Kalsto_Composition();

            Thread.Sleep(500);
        }
    }

    public void Read_Composition()
    {
        // Read velocities
        // There might not be values in IP21 at Now, so we fetch slightly older values.
        Timestamp = DateTime.Now.AddSeconds(-15.0);
        Hashtable A_Velocity;
        Log_File.WriteLine();
        Log_File.WriteLine(Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

        double Asgard_Average_Velocity = 0.0;
        try
        {
            A_Velocity = DB_Connection.Read_Values(Asgard.Velocity_Tags.ToArray(), Timestamp);
            Asgard_Average_Velocity = (Convert.ToDouble(A_Velocity[Asgard.Velocity_Tags[0]]) +
                                       Convert.ToDouble(A_Velocity[Asgard.Velocity_Tags[1]])) / 2.0;
            Asgard_Velocity[0] = Convert.ToDouble(A_Velocity[Asgard.Velocity_Tags[0]]);
            Asgard_Velocity[1] = Convert.ToDouble(A_Velocity[Asgard.Velocity_Tags[1]]);
            Log_File.WriteLine("Åsgard velocity: {0}", Asgard_Average_Velocity);
#if DEBUG
            Console.WriteLine("Åsgard velocity: {0}", Asgard_Average_Velocity);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Asgard.Velocity_Tags[0], Asgard.Velocity_Tags[1]);
            Log_File.Flush();
        }
        double Statpipe_Average_Velocity = 0.0;
        Hashtable S_Velocity;
        try
        {
            S_Velocity = DB_Connection.Read_Values(Statpipe.Velocity_Tags.ToArray(), Timestamp);
            Statpipe_Average_Velocity = (Convert.ToDouble(S_Velocity[Statpipe.Velocity_Tags[0]]) +
                                                Convert.ToDouble(S_Velocity[Statpipe.Velocity_Tags[1]])) / 2.0;
            Statpipe_Velocity[0] = Convert.ToDouble(S_Velocity[Statpipe.Velocity_Tags[0]]);
            Statpipe_Velocity[1] = Convert.ToDouble(S_Velocity[Statpipe.Velocity_Tags[1]]);
            Log_File.WriteLine("Statpipe velocity: {0}", Statpipe_Average_Velocity);
#if DEBUG
            Console.WriteLine("Statpipe velocity: {0}", Statpipe_Average_Velocity);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Statpipe.Velocity_Tags[0], Statpipe.Velocity_Tags[1]);
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
            return;
        }
        Asgard_Timestamp = DateTime.Now.AddSeconds(-(Asgard.Pipe_Length / Asgard_Average_Velocity));
        Statpipe_Timestamp = DateTime.Now.AddSeconds(-(Statpipe.Pipe_Length / Statpipe_Average_Velocity));
        Log_File.WriteLine("Åsgard delay: {0}", Asgard.Pipe_Length / Asgard_Average_Velocity);
        Log_File.WriteLine("Statpipe delay: {0}", Statpipe.Pipe_Length / Statpipe_Average_Velocity);
#if DEBUG
        Console.WriteLine("Åsgard delay: {0}", Asgard.Pipe_Length / Asgard_Average_Velocity);
        Console.WriteLine("Statpipe delay: {0}", Statpipe.Pipe_Length / Statpipe_Average_Velocity);
#endif

        // Read Åsgard composition
        bool Asgard_Stat = false;
        bool Statpipe_Stat = false;

        Hashtable Asgard_Status;
        try
        {
            Asgard_Status = DB_Connection.Read_Values(Asgard.Status_Tags.ToArray(), Asgard_Timestamp);
            foreach (DictionaryEntry Stat in Asgard_Status)
            {
                if (Convert.ToDouble(Stat.Value) > 999.9)
                {
                    Asgard_Stat = true;
                }
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Asgard.Status_Tags[0], Asgard.Status_Tags[1]);
            Log_File.WriteLine("GC bad status ");
            Log_File.Flush();
        }

        string Tag_Name = "";

        if (Asgard_Stat)
        {
            try
            {
                Comp_Values = DB_Connection.Read_Values(Asgard.Composition_tags().ToArray(), Asgard_Timestamp);
                foreach (Component c in Asgard.Comp)
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
        if (Comp_Values != null) Comp_Values.Clear();

        // Read Statpipe composition
        Hashtable Statpipe_Status;

        try
        {
            Statpipe_Status = DB_Connection.Read_Values(Statpipe.Status_Tags.ToArray(), Statpipe_Timestamp);
            foreach (DictionaryEntry Stat in Statpipe_Status)
            {
                if (Convert.ToDouble(Stat.Value) > 999.9)
                {
                    Statpipe_Stat = true;
                }
            }
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Statpipe.Status_Tags[0], Statpipe.Status_Tags[1]);
            Log_File.WriteLine("GC bad status ");
            Log_File.Flush();
        }

        if (Statpipe_Stat)
        {
            try
            {
                Comp_Values = DB_Connection.Read_Values(Statpipe.Composition_tags().ToArray(), Statpipe_Timestamp);
                foreach (Component c in Statpipe.Comp)
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

        // Read_From_IP21()
        // Read Cross over selector
        Hashtable Crossover_Status;
        try
        {
            Crossover_Status = DB_Connection.Read_Values(new string[] { "15HS0105" }, Timestamp);
            Cross_Over_Status = Convert.ToBoolean(Crossover_Status["15HS0105"]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", "15HS0105");
            Log_File.Flush();
        }

        // Read molweight
        Hashtable Molweight;
        Asgard.Molweight = 0.0;
        try
        {
            Molweight = DB_Connection.Read_Values(new string[] { Asgard.Molweight_Tag }, Asgard_Timestamp);
            Asgard.Molweight = Convert.ToDouble(Molweight[Asgard.Molweight_Tag]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Asgard.Molweight_Tag);
            Log_File.Flush();
        }
        Statpipe.Molweight = 0.0;
        try
        {
            Molweight = DB_Connection.Read_Values(new string[] { Statpipe.Molweight_Tag }, Statpipe_Timestamp);
            Statpipe.Molweight = Convert.ToDouble(Molweight[Statpipe.Molweight_Tag]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Statpipe.Molweight_Tag);
            Log_File.Flush();
        }
        double Mix_To_T100_Molweight = 0.0;
        try
        {
            Molweight = DB_Connection.Read_Values(new string[] { Mix_To_T100.Molweight_Tag }, Timestamp);
            Mix_To_T100_Molweight = Convert.ToDouble(Molweight[Mix_To_T100.Molweight_Tag]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Mix_To_T100.Molweight_Tag);
            Log_File.Flush();
        }

        // Read mass flow
        Hashtable Mass_Flow;
        Asgard.Mass_Flow = 0.0;
        try
        {
            Mass_Flow = DB_Connection.Read_Values(Asgard.Mass_Flow_Tags.ToArray(), Asgard_Timestamp);
            Asgard.Mass_Flow = Convert.ToDouble(Mass_Flow[Asgard.Mass_Flow_Tags[0]]);
#if DEBUG
            Console.WriteLine("\nÅsgard flow: {0}\t{1}", Asgard.Mass_Flow_Tags[0], Asgard.Mass_Flow);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Asgard.Mass_Flow_Tags[0]);
            Log_File.Flush();
        }
        Asgard.Mol_Flow = Asgard.Mass_Flow * 1000 / Asgard.Molweight;

        Statpipe.Mass_Flow = 0.0;
        try
        {
            Mass_Flow = DB_Connection.Read_Values(Statpipe.Mass_Flow_Tags.ToArray(), Statpipe_Timestamp);
            Statpipe.Mass_Flow = Convert.ToDouble(Mass_Flow[Statpipe.Mass_Flow_Tags[0]]);
#if DEBUG
            Console.WriteLine("\nStatpipe flow: {0}\t{1}", Statpipe.Mass_Flow_Tags[0], Statpipe.Mass_Flow);
#endif
        }
        catch
        {
            Log_File.WriteLine("Tag {0} not valid", Statpipe.Mass_Flow_Tags[0]);
            Log_File.Flush();
        }
        Statpipe.Mol_Flow = Statpipe.Mass_Flow * 1000 / Statpipe.Molweight;

        Mix_To_T100.Mass_Flow = 0.0;
        Statpipe_Cross_Over_Flow = 0.0;
        try
        {
            Mass_Flow = DB_Connection.Read_Values(Mix_To_T100.Mass_Flow_Tags.ToArray(), Timestamp);
            Mix_To_T100.Mass_Flow = Convert.ToDouble(Mass_Flow[Mix_To_T100.Mass_Flow_Tags[0]]);
            Statpipe_Cross_Over_Flow = Convert.ToDouble(Mass_Flow[Mix_To_T100.Mass_Flow_Tags[1]]);
        }
        catch
        {
            Log_File.WriteLine("Tag {0} and/or {1} not valid", Mix_To_T100.Mass_Flow_Tags[0], Mix_To_T100.Mass_Flow_Tags[1]);
            Log_File.Flush();
        }
        Mix_To_T100.Mol_Flow = Mix_To_T100.Mass_Flow * 1000 / Mix_To_T100_Molweight;

        if (Cross_Over_Status)
        {
            Statpipe_Cross_Over_Mol_Flow = Statpipe_Cross_Over_Flow * 1000 / Statpipe.Molweight;
        }
        else
        {
            Statpipe_Cross_Over_Mol_Flow = Statpipe_Cross_Over_Flow * 1000 / Asgard.Molweight;
        }

        // Calculate mixed compositions at Kårstø
        List<Component> Asgard_Component_Flow = new List<Component>();
        double Asgard_Component_Flow_Sum = 0.0;
        foreach (Component c in Asgard.Comp)
        {
            Asgard_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Asgard.Mol_Flow * c.Value / 100.0));
            Asgard_Component_Flow_Sum += Asgard.Mol_Flow * c.Value / 100.0;
        }

        List<Component> Statpipe_Component_Flow = new List<Component>();
        double Statpipe_Component_Flow_Sum = 0.0;
        foreach (Component c in Statpipe.Comp)
        {
            Statpipe_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Statpipe.Mol_Flow * c.Value / 100.0));
            Statpipe_Component_Flow_Sum += Statpipe.Mol_Flow * c.Value / 100.0;
        }

        foreach (Component c in Mix_To_T410.Comp)
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

        if (Cross_Over_Status)
        {
            foreach (Component c in Statpipe.Comp)
            {
                CY2007_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Statpipe_Cross_Over_Mol_Flow * c.Value / 100.0));
                CY2007_Component_Flow_Sum += Statpipe_Cross_Over_Mol_Flow * c.Value / 100.0;
            }
        }
        else
        {
            foreach (Component c in Asgard.Comp)
            {
                CY2007_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Statpipe_Cross_Over_Mol_Flow * c.Value / 100.0));
                CY2007_Component_Flow_Sum += Statpipe_Cross_Over_Mol_Flow * c.Value / 100.0;
            }
        }

        List<Component> DIXO_Component_Flow = new List<Component>();
        double DIXO_Component_Flow_Sum = 0.0;
        foreach (Component c in Mix_To_T410.Comp)
        {
            DIXO_Component_Flow.Add(new Component(c.ID, c.Tag, 1.0, Mix_To_T100.Mol_Flow * c.Value / 100.0));
            DIXO_Component_Flow_Sum += Mix_To_T100.Mol_Flow * c.Value / 100.0;
        }

        foreach (Component c in Mix_To_T100.Comp)
        {
            if (CY2007_Component_Flow_Sum + DIXO_Component_Flow_Sum > 0.0)
            {
                c.Value = (CY2007_Component_Flow.Find(x => x.ID == c.ID).Value +
                              DIXO_Component_Flow.Find(x => x.ID == c.ID).Value) /
                              (CY2007_Component_Flow_Sum + DIXO_Component_Flow_Sum) * 100.0;
            }
        }

        // Write mixed compositions to IP21
        lock (locker)
        {
            foreach (Component c in Mix_To_T100.Comp)
            {
                DB_Connection.Write_Value(c.Tag, c.Get_Scaled_Value());
            }

            foreach (Component c in Mix_To_T410.Comp)
            {
                DB_Connection.Write_Value(c.Tag, c.Get_Scaled_Value());
            }
        }

        // Read operation points
        Hashtable OP = new Hashtable();

        foreach (PT_Point p in Mix_To_T410.Curve.Operating_Points)
        {
            OP = DB_Connection.Read_Values(new string[] { p.pressure_tag,
            p.temperature_tag}, Timestamp);
            p.pressure = Convert.ToDouble(OP[p.pressure_tag]);
            p.temperature = Convert.ToDouble(OP[p.temperature_tag]);
        }

        foreach (PT_Point p in Mix_To_T100.Dropout_Points)
        {
            OP = DB_Connection.Read_Values(new string[] { p.pressure_tag,
            p.temperature_tag}, Timestamp);
            p.pressure = Convert.ToDouble(OP[p.pressure_tag]);
            p.temperature = Convert.ToDouble(OP[p.temperature_tag]);
        }
    }

    public void Calculate_Karsto()
    {
        List<Int32> Composition_IDs = new List<Int32>();
        List<double> Composition_Values = new List<double>();

        // Mix to T100 cricondenbar
        Composition_IDs.Clear();
        Composition_Values.Clear();
        Log_File.WriteLine("Mix to T100:");
        foreach (Component c in Mix_To_T100.Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        Log_File.Flush();

        T100.Composition_IDs = Composition_IDs.ToArray();
        T100.Composition_Values = Composition_Values.ToArray();

        // Mix to T400 cricondenbar
        Composition_IDs.Clear();
        Composition_Values.Clear();
        Log_File.WriteLine("Mix to T400:");
        foreach (Component c in Mix_To_T410.Comp)
        {
            Composition_IDs.Add(c.ID);
            Composition_Values.Add(c.Value);
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        Log_File.Flush();

        T400.Composition_IDs = Composition_IDs.ToArray();
        T400.Composition_Values = Composition_Values.ToArray();

        double[] Composition_Result_T100 = { 0.0, 0.0 };
        double[] Composition_Result_T400 = { 0.0, 0.0 };
        Parallel.Invoke(
            () =>
            {
                T100.Fluid_Tune();
                Composition_Result_T100 = T100.Cricondenbar();
            },

            () =>
            {
                T400.Fluid_Tune();
                Composition_Result_T400 = T400.Cricondenbar();
            }
        );

        if (!Composition_Result_T100[0].Equals(double.NaN) && !Composition_Result_T100[1].Equals(double.NaN))
        {
            lock (locker)
            {
                DB_Connection.Write_Value(Mix_To_T100.Cricondenbar.pressure_tag, Composition_Result_T100[0]);
                DB_Connection.Write_Value(Mix_To_T100.Cricondenbar.temperature_tag, Composition_Result_T100[1]);
                DB_Connection.Write_Value(Mix_To_T100.Cricondenbar.status_tag, 1);
            }
        }
        Log_File.WriteLine("{0}\t{1}", Mix_To_T100.Cricondenbar.pressure_tag, Composition_Result_T100[0]);
        Log_File.WriteLine("{0}\t{1}", Mix_To_T100.Cricondenbar.temperature_tag, Composition_Result_T100[1]);
#if DEBUG
        Console.WriteLine("{0}\t{1}", Mix_To_T100.Cricondenbar.pressure_tag, Composition_Result_T100[0]);
        Console.WriteLine("{0}\t{1}", Mix_To_T100.Cricondenbar.temperature_tag, Composition_Result_T100[1]);
#endif

        if (!Composition_Result_T400[0].Equals(double.NaN) && !Composition_Result_T400[1].Equals(double.NaN))
        {
            lock (locker)
            {
                DB_Connection.Write_Value(Mix_To_T410.Cricondenbar.pressure_tag, Composition_Result_T400[0]);
                DB_Connection.Write_Value(Mix_To_T410.Cricondenbar.temperature_tag, Composition_Result_T400[1]);
                DB_Connection.Write_Value(Mix_To_T410.Cricondenbar.status_tag, 1);
            }
        }
        Log_File.WriteLine("{0}\t{1}", Mix_To_T410.Cricondenbar.pressure_tag, Composition_Result_T400[0]);
        Log_File.WriteLine("{0}\t{1}", Mix_To_T410.Cricondenbar.temperature_tag, Composition_Result_T400[1]);
#if DEBUG
        Console.WriteLine("{0}\t{1}", Mix_To_T410.Cricondenbar.pressure_tag, Composition_Result_T400[0]);
        Console.WriteLine("{0}\t{1}", Mix_To_T410.Cricondenbar.temperature_tag, Composition_Result_T400[1]);
#endif

        Log_File.Flush();
    }

    public void Read_Current_Kalsto_Composition()
    {
        lock (locker)
        {
            // Åsgard
            if (Comp_Values != null) Comp_Values.Clear();
            Log_File.WriteLine("Åsgard:");
            string Tag_Name = "";
            try
            {
                Comp_Values = DB_Connection.Read_Values(Asgard_Current.Composition_tags().ToArray(), Timestamp);
                foreach (Component c in Asgard_Current.Comp)
                {
                    Tag_Name = c.Tag;
                    c.Value = Convert.ToDouble(Comp_Values[c.Tag]) * c.Scale_Factor;
                    Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
                }
            }
            catch
            {
                Log_File.WriteLine("Tag {0} not valid", Tag_Name);
                Log_File.Flush();
            }

            // Statpipe
            Comp_Values.Clear();
            Log_File.WriteLine("Statpipe:");
            try
            {
                Comp_Values = DB_Connection.Read_Values(Statpipe_Current.Composition_tags().ToArray(), Timestamp);
                foreach (Component c in Statpipe_Current.Comp)
                {
                    Tag_Name = c.Tag;
                    c.Value = Convert.ToDouble(Comp_Values[c.Tag]) * c.Scale_Factor;
                    Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
                }
            }
            catch
            {
                Log_File.WriteLine("Tag {0} not valid", Tag_Name);
                Log_File.Flush();
            }
        }

        Log_File.Flush();
    }

    public void Calculate_Kalsto_Asgard()
    {
        Asgard_Kalsto.Composition_IDs = Asgard_Current.Composition_IDs().ToArray();
        Asgard_Kalsto.Composition_Values = Asgard_Current.Composition_Values().ToArray();

        Asgard_Kalsto.Fluid_Tune();
        double[] Composition_Result = Asgard_Kalsto.Cricondenbar();

        if (!Composition_Result[0].Equals(double.NaN) && !Composition_Result[1].Equals(double.NaN))
        {
            lock (locker)
            {
                DB_Connection.Write_Value(Asgard.Cricondenbar.pressure_tag, Composition_Result[0]);
                DB_Connection.Write_Value(Asgard.Cricondenbar.temperature_tag, Composition_Result[1]);
                DB_Connection.Write_Value(Asgard.Cricondenbar.status_tag, 1);
            }
        }
        Log_File.WriteLine("{0}\t{1}", Asgard.Cricondenbar.pressure_tag, Composition_Result[0]);
        Log_File.WriteLine("{0}\t{1}", Asgard.Cricondenbar.temperature_tag, Composition_Result[1]);
#if DEBUG
        Console.WriteLine("{0}\t{1}", Asgard.Cricondenbar.pressure_tag, Composition_Result[0]);
        Console.WriteLine("{0}\t{1}", Asgard.Cricondenbar.temperature_tag, Composition_Result[1]);
#endif
    }

    public void Calculate_Kalsto_Statpipe()
    {
        Statpipe_Kalsto.Composition_IDs = Statpipe_Current.Composition_IDs().ToArray();
        Statpipe_Kalsto.Composition_Values = Statpipe_Current.Composition_Values().ToArray();

        Statpipe_Kalsto.Fluid_Tune();
        double[] Composition_Result = Statpipe_Kalsto.Cricondenbar();

        if (!Composition_Result[0].Equals(double.NaN) && !Composition_Result[1].Equals(double.NaN))
        {
            lock (locker)
            {
                DB_Connection.Write_Value(Statpipe.Cricondenbar.pressure_tag, Composition_Result[0]);
                DB_Connection.Write_Value(Statpipe.Cricondenbar.temperature_tag, Composition_Result[1]);
                DB_Connection.Write_Value(Statpipe.Cricondenbar.status_tag, 1);
            }
        }
        Log_File.WriteLine("{0}\t{1}", Statpipe.Cricondenbar.pressure_tag, Composition_Result[0]);
        Log_File.WriteLine("{0}\t{1}", Statpipe.Cricondenbar.temperature_tag, Composition_Result[1]);
#if DEBUG
        Console.WriteLine("{0}\t{1}", Statpipe.Cricondenbar.pressure_tag, Composition_Result[0]);
        Console.WriteLine("{0}\t{1}", Statpipe.Cricondenbar.temperature_tag, Composition_Result[1]);
#endif
    }

    public void Calculate_Dropout_Curves()
    {

        // Mix to T400 dropout
        foreach (Component c in Mix_To_T410.Comp)
        {
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        double[,] Pres = new double[Mix_To_T410.Curve.Dropout.Count + 1, Mix_To_T410.Curve.Temperature.Count];
        
        // Dew point line. We use this later to set the max value when searching for drop out pressures
        Parallel.For (0, Mix_To_T410.Curve.Temperature.Count, i =>
        {
            Pres[0, i] = T400.DewP(Mix_To_T410.Curve.Temperature[i] + to_Kelvin);
            System.Console.WriteLine("Dew point: Temperture: {0}, Pressure: {1}", Mix_To_T410.Curve.Temperature[i], Pres[0, i] - to_bara);
        });

        for (int i = 0; i < Mix_To_T410.Curve.Dropout.Count; i++)
        {
            Parallel.For(0, Mix_To_T410.Curve.Temperature.Count, j =>
            {
                Pres[i + 1, j] = T400.Dropout_Search(Mix_To_T410.Curve.Dropout[i].Value, Mix_To_T410.Curve.Temperature[j] + to_Kelvin, Pres[0, j], 0.01);
                System.Console.WriteLine("Dropout: {0}, Temperture: {1}, Pressure: {2}", Mix_To_T410.Curve.Dropout[i].Value, Mix_To_T410.Curve.Temperature[j], Pres[i + 1, j] - to_bara);
            });
        }
        
        DateTime Start_Time = DateTime.Now;
        Start_Time = Start_Time.AddMilliseconds(-Start_Time.Millisecond);
        String Tag;
        Int32 Interval = 3;
        Double Result = 0.0;

        lock (locker)
        {
            for (int j = 0; j < Mix_To_T410.Curve.Operating_Points.Count; j++)
            {
                if (j == 0)
                {
                    DB_Connection.Insert_Value(Mix_To_T410.Curve.Operating_Points[j].result_tag , double.NaN, Start_Time);
                    DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, double.NaN, Start_Time);
                }

                DB_Connection.Insert_Value(Mix_To_T410.Curve.Operating_Points[j].result_tag , Mix_To_T410.Curve.Operating_Points[j].pressure, Start_Time.AddSeconds(-(Interval * j + 1)));
                DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, Mix_To_T410.Curve.Operating_Points[j].temperature, Start_Time.AddSeconds(-(Interval * j + 1)));

                DB_Connection.Insert_Value(Mix_To_T410.Curve.Operating_Points[j].result_tag , Mix_To_T410.Curve.Operating_Points[j].pressure, Start_Time.AddSeconds(-(Interval * j + 2)));
                DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, Mix_To_T410.Curve.Operating_Points[j].temperature, Start_Time.AddSeconds(-(Interval * j + 2)));

                DB_Connection.Insert_Value(Mix_To_T410.Curve.Operating_Points[j].result_tag, double.NaN, Start_Time.AddSeconds(-(Interval * j + 3)));
                DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, double.NaN, Start_Time.AddSeconds(-(Interval * j + 3)));

                Result = T400.Dropout(Mix_To_T410.Curve.Operating_Points[j].pressure + to_bara, Mix_To_T410.Curve.Operating_Points[j].temperature + to_Kelvin)[0] * 100.0;

                DB_Connection.Write_Value("T_PO_LD" + (j).ToString(), Result);
            }

            Start_Time = Start_Time.AddSeconds(-(Interval * Mix_To_T410.Curve.Operating_Points.Count + 1) );
            Interval = 5;
            for (int j = 0; j < Mix_To_T410.Curve.Temperature.Count; j++)
            {
                if (j == 0) DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, double.NaN, Start_Time.AddSeconds(-Interval * j));
                DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, Mix_To_T410.Curve.Temperature[j], Start_Time.AddSeconds(-Interval * (j + 1)));

                for (int i = 0; i < Mix_To_T410.Curve.Dropout.Count + 1; i++)
                {
                    if (i == 0) Tag = Mix_To_T410.Curve.Dew_Point_Tag;
                    else Tag = Mix_To_T410.Curve.Dropout[i-1].Tag;
                    if (j == 0) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * j));
                    DB_Connection.Insert_Value(Tag, Pres[i, j] - to_bara, Start_Time.AddSeconds(-Interval * (j + 1)));
                    if (j == Mix_To_T410.Curve.Temperature.Count - 1) DB_Connection.Insert_Value(Tag, double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
                }
                if (j == Mix_To_T410.Curve.Temperature.Count - 1) DB_Connection.Insert_Value(Mix_To_T410.Curve.Temperature_Tag, double.NaN, Start_Time.AddSeconds(-Interval * (j + 2)));
            }
        }

        // Mix to T100/200 dropout
        foreach (Component c in Mix_To_T100.Comp)
        {
            Log_File.WriteLine("{0}\t{1}", c.ID, c.Value);
        }

        Result = 0.0;

        foreach (PT_Point p in Mix_To_T100.Dropout_Points)
        {
            Result = T100.Dropout(p.pressure + to_bara, p.temperature + to_Kelvin)[0] * 100.0;
            lock (locker)
            {
                DB_Connection.Write_Value(p.result_tag, Result);
            }
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
                                    Asgard.Status_Tags.Add(reader.GetAttribute("tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                {
                                    Asgard.Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                             reader.GetAttribute("tag"),
                                                             XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                    Asgard_Current.Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                             reader.GetAttribute("tag"),
                                                             XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "velocity")
                                {
                                    Asgard.Velocity_Tags.Add(reader.GetAttribute("kalsto-tag"));
                                    Asgard.Velocity_Tags.Add(reader.GetAttribute("karsto-tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "length")
                                {
                                    reader.Read();
                                    Asgard.Pipe_Length = XmlConvert.ToDouble(reader.Value);
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "molweight")
                                {
                                    Asgard.Molweight_Tag = reader.GetAttribute("tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Asgard.Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Asgard.Cricondenbar.pressure_tag = reader.GetAttribute("pressure-tag");
                                    Asgard.Cricondenbar.temperature_tag = reader.GetAttribute("temperature-tag");
                                    Asgard.Cricondenbar.status_tag = reader.GetAttribute("status-tag");
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
                                    Statpipe.Status_Tags.Add(reader.GetAttribute("tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                {
                                    Statpipe.Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                               reader.GetAttribute("tag"),
                                                               XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                    Statpipe_Current.Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                               reader.GetAttribute("tag"),
                                                               XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "velocity")
                                {
                                    Statpipe.Velocity_Tags.Add(reader.GetAttribute("kalsto-tag"));
                                    Statpipe.Velocity_Tags.Add(reader.GetAttribute("karsto-tag"));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "length")
                                {
                                    reader.Read();
                                    Statpipe.Pipe_Length = XmlConvert.ToDouble(reader.Value);
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "molweight")
                                {
                                    Statpipe.Molweight_Tag = reader.GetAttribute("tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Statpipe.Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Statpipe.Cricondenbar.pressure_tag = reader.GetAttribute("pressure-tag");
                                    Statpipe.Cricondenbar.temperature_tag = reader.GetAttribute("temperature-tag");
                                    Statpipe.Cricondenbar.status_tag = reader.GetAttribute("status-tag");
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
                                    Mix_To_T410.Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                                       reader.GetAttribute("tag"),
                                                                       XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Mix_To_T410.Cricondenbar.pressure_tag = reader.GetAttribute("pressure-tag");
                                    Mix_To_T410.Cricondenbar.temperature_tag = reader.GetAttribute("temperature-tag");
                                    Mix_To_T410.Cricondenbar.status_tag = reader.GetAttribute("status-tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Mix_To_T410.Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "liquid-dropout")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "liquid-dropout")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "temperature")
                                        {
                                            Mix_To_T410.Curve.Temperature_Tag = reader.GetAttribute("tag");

                                            while (reader.Read())
                                            {
                                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "temperature")
                                                    break;
                                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                                                {
                                                    reader.Read();
                                                    Mix_To_T410.Curve.Temperature.Add(XmlConvert.ToDouble(reader.Value));
                                                }
                                            }
                                        }
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "dew-point")
                                        {
                                            Mix_To_T410.Curve.Dew_Point_Tag = reader.GetAttribute("tag");
                                        }
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "dropout")
                                        {
                                            Mix_To_T410.Curve.Dropout.Add(new Dropout_Curve.Dropout_Point
                                                (XmlConvert.ToDouble(reader.GetAttribute("value")), reader.GetAttribute("tag")));
                                        }
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "operating-points")
                                        {
                                            while (reader.Read())
                                            {
                                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "operating-points")
                                                    break;
                                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "point")
                                                {
                                                    Mix_To_T410.Curve.Operating_Points.Add(new PT_Point(reader.GetAttribute("pressure-tag"),
                                                        reader.GetAttribute("temperature-tag"), reader.GetAttribute("result-tag")));
                                                }
                                            }
                                        }
                                    }
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
                                    Mix_To_T100.Comp.Add(new Component(XmlConvert.ToInt32(reader.GetAttribute("id")),
                                                                       reader.GetAttribute("tag"),
                                                                       XmlConvert.ToDouble(reader.GetAttribute("scale-factor"))));
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "cricondenbar")
                                {
                                    Mix_To_T100.Cricondenbar.pressure_tag = reader.GetAttribute("pressure-tag");
                                    Mix_To_T100.Cricondenbar.temperature_tag = reader.GetAttribute("temperature-tag");
                                    Mix_To_T100.Cricondenbar.status_tag = reader.GetAttribute("status-tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "molweight")
                                {
                                    Mix_To_T100.Molweight_Tag = reader.GetAttribute("tag");
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "mass-flow")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "mass-flow")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "component")
                                        {
                                            Mix_To_T100.Mass_Flow_Tags.Add(reader.GetAttribute("tag"));
                                        }
                                    }
                                }
                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "liquid-dropout")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "liquid-dropout")
                                            break;
                                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "dropout-points")
                                        {
                                            while (reader.Read())
                                            {
                                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "dropout-points")
                                                    break;
                                                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "point")
                                                {
                                                    Mix_To_T100.Dropout_Points.Add(new PT_Point(reader.GetAttribute("pressure-tag"),
                                                        reader.GetAttribute("temperature-tag"), reader.GetAttribute("result-tag")));
                                                }
                                            }
                                        }
                                    }
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

    public int Validate_Current()
    {
        int errors = 0;
        double Stdev_Low_Limit = 1.0E-10;

        if (Composition_Stdev(Statpipe_Current.Comp, GC_Comp_Statpipe) < Stdev_Low_Limit
            || Check_Composition(Statpipe_Current.Comp) == false)
        {
            DB_Connection.Write_Value(Statpipe.Cricondenbar.status_tag, 0);
            Console.WriteLine("Bad composition Statpipe A");
            Log_File.WriteLine("{0}: Bad composition Statpipe A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
            errors++;
        }
        if (Composition_Stdev(Asgard_Current.Comp, GC_Comp_Asgard) < Stdev_Low_Limit
            || Check_Composition(Asgard_Current.Comp) == false)
        {
            DB_Connection.Write_Value(Asgard.Cricondenbar.status_tag, 0);
            Console.WriteLine("Bad composition Asgard A");
            Log_File.WriteLine("{0}: Bad composition Asgard A", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); Log_File.Flush();
            errors++;
        }

        return errors;
    }
    public int Validate()
    {
        int errors = 0;
        double Stdev_Low_Limit = 1.0E-10;

        if (Check_Composition(Asgard.Comp) == false)
        {
            Console.WriteLine("Bad composition Asgard A");
            Log_File.WriteLine("{0}: Bad composition Asgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Check_Composition(Statpipe.Comp) == false)
        {
            Console.WriteLine("Bad composition Statpipe A");
            Log_File.WriteLine("{0}: Bad composition Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Asgard.Molweight, GC_Molweight_Asgard) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad molweight Asgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad molweight Asgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Statpipe.Molweight, GC_Molweight_Statpipe) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad molweight Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad molweight Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Asgard.Mass_Flow, GC_Asgard_Flow) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad flow Asgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad flow Asgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Statpipe.Mass_Flow, GC_Statpipe_Flow) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad flow Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad flow Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Statpipe_Cross_Over_Flow, GC_Statpipe_Cross_Over_Flow) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad flow Statpipe cross over {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad flow Statpipe cross over {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
        }
        if (Molweight_Stdev(Mix_To_T100.Mass_Flow, GC_Mix_To_T100_Flow) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad flow Mix to T100 flow {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad flow Mix to T100 flow {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Asgard_Velocity[0], Velocity_1_Asgard, 30) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad gas velocity Åsgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad gas velocity Åsgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Asgard_Velocity[1], Velocity_2_Asgard, 30) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad gas velocity Åsgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad gas velocity Åsgard {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Statpipe_Velocity[0], Velocity_1_Statpipe, 30) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad gas velocity Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad gas velocity Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }
        if (Molweight_Stdev(Statpipe_Velocity[1], Velocity_2_Statpipe, 30) < Stdev_Low_Limit)
        {
            Console.WriteLine("{0}: Bad gas velocity Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name);
            Log_File.WriteLine("{0}: Bad gas velocity Statpipe {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Name); Log_File.Flush();
            errors++;
        }

        if (errors > 0)
        {
            DB_Connection.Write_Value(Mix_To_T100.Cricondenbar.status_tag, 0);
            DB_Connection.Write_Value(Mix_To_T410.Cricondenbar.status_tag, 0);
        }

        return errors;
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

    public static double Molweight_Stdev(double MW, Queue GC_Comp, int memory = 5)
    {
        double Lowest_Stdev = double.MaxValue;
        List<double> Values = new List<double>();

        GC_Comp.Enqueue(MW);

        while (GC_Comp.Count > memory)
        {
            GC_Comp.Dequeue();
        }

        if (GC_Comp.Count >= memory)
        {
            foreach (double v in GC_Comp)
            {
                Values.Add(v);
            }
            double stdev = CalculateStdDev(Values);
            if (stdev < Lowest_Stdev)
            {
                Lowest_Stdev = stdev;
            }
        }

        return Lowest_Stdev;
    }

    public static double Composition_Stdev(List<Component> PO, Queue GC_Comp, int memory = 10)
    {
        double Lowest_Stdev = double.MaxValue;
        List<double> Values = new List<double>();
        Dictionary<int, List<double>> CA = new Dictionary<int, List<double>>();

        foreach (Component v in PO)
        {
            Values.Add(v.Value);
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
                if (stdev < Lowest_Stdev)
                {
                    Lowest_Stdev = stdev;
                }
            }
        }
        return Lowest_Stdev;
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
