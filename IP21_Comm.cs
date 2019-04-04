using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

public class IP21_Comm: IDisposable
{
    public string IP21_Host;
    public string IP21_Port;
    public bool IP21_Read_Only;
    private System.Data.Odbc.OdbcCommand Cmdr;
    private System.Data.Odbc.OdbcCommand Cmdw;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // dispose managed resources
            Cmdr.Dispose();
            Cmdw.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IP21_Comm(string Host, string Port, bool Read_Only = false)
    {
        IP21_Host = Host;
        IP21_Port = Port;
        IP21_Read_Only = Read_Only;
    }

    public bool isConnected()
    {
        return Cmdr.Connection.State == System.Data.ConnectionState.Open &&
            Cmdw.Connection.State == System.Data.ConnectionState.Open;
    }
    public void Connect()
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;
        Cmdr = new System.Data.Odbc.OdbcCommand();
        Cmdr.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmdr.CommandTimeout = 15;

        Cmdw = new System.Data.Odbc.OdbcCommand();
        Cmdw.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmdw.CommandTimeout = 15;

        try
        {
            Cmdr.Connection.Open();
            Cmdw.Connection.Open();
        }
        catch(System.Data.Odbc.OdbcException e)
        {
            Console.WriteLine("Connection failed {0}", e.Message);
        }
    }

    public void Disconnect()
    {
        if (Cmdr.Connection.State == System.Data.ConnectionState.Open)
        {
            Cmdr.Connection.Close();
        }

        if (Cmdw.Connection.State == System.Data.ConnectionState.Open)
        {
            Cmdw.Connection.Close();
        }

    }

    public Hashtable Read_Values(string[] Tag_Name, DateTime Time_Stamp)
    {
        Hashtable Result = new Hashtable();
        string Tag_cond = "(FALSE"; // Makes it easier to add OR conditions.
        foreach (string Tag in Tag_Name)
        {
            if (Sanitize(Tag))
            {
                Tag_cond += "\n  OR NAME = '" + Tag + "'";
            }
        }
        Tag_cond += ")";
        Cmdr.CommandText =
@"SELECT
  NAME, VALUE, STATUS
FROM
  History
WHERE
  " + Tag_cond + @"
  AND TS > CAST('" + Time_Stamp.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS')
  AND TS < CAST('" + Time_Stamp.AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS')
  AND PERIOD = 000:0:01.0;
";

        System.Data.Odbc.OdbcDataReader DR = Cmdr.ExecuteReader(System.Data.CommandBehavior.SingleResult);

        Console.WriteLine(DR.ToString());

        //if (DR.HasRows)
        //{
            while (DR.Read())
            {
                if (DR.GetValue(2).ToString() == "0") // if status is good
                {
                    Result.Add(DR.GetValue(0).ToString(), DR.GetValue(1));
                }
            }
        //}

        DR.Close();

        return Result;
    }

    public void Write_Value(string Tag_Name, double Value, string Quality = "Good")
    {
        Cmdw.CommandText =
@"UPDATE ip_analogdef
  SET ip_input_value = " + Value.ToString("G", CultureInfo.InvariantCulture) + @", ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        try
        {
            if (!IP21_Read_Only) Cmdw.ExecuteNonQuery();
        }
        catch
        {
            Console.WriteLine("Write_Value failed");
        }
    }

    public void Write_Value(string Tag_Name, int Value, string Quality = "Good")
    {
        Cmdw.CommandText =
@"UPDATE ip_discretedef
  SET ip_input_value = " + Value.ToString() + @", ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        try
        {
            if (!IP21_Read_Only) Cmdw.ExecuteNonQuery();
        }
        catch
        {
            Console.WriteLine("Write_Value failed");
        }
    }

    public void Write_Value(string Tag_Name, string Value, string Quality = "Good")
    {
        Cmdw.CommandText =
@"UPDATE ip_textdef
  SET ip_input_value = '" + Value + @"', ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        try
        {
            if (!IP21_Read_Only) Cmdw.ExecuteNonQuery();
        }
        catch
        { }
    }

    public void Insert_Value(string Tag_Name, double Value, DateTime Time_Stamp)
    {
        Cmdw.CommandText =
@"INSERT INTO " + Tag_Name + @"(IP_TREND_TIME, IP_TREND_VALUE)
  VALUES (CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS'), "
                  + Value.ToString("G", CultureInfo.InvariantCulture) + @");";

        if (double.IsNaN(Value))
        {
            Cmdw.CommandText =
@"INSERT INTO " + Tag_Name + @"(IP_TREND_TIME, IP_TREND_VALUE)
  VALUES (CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS'), 0.0/0.0);";
        }

        try
        {
            if (!IP21_Read_Only) Cmdw.ExecuteNonQuery();
        }
        catch
        {
            Console.WriteLine("Insert_Value failed");
        }
    }

    private bool Sanitize(string stringValue)
    {
        if (Regex.Match(stringValue, "-{2,}").Success  ||
            Regex.Match(stringValue, @"[*/]+").Success ||
            Regex.Match(stringValue, @"(;|\s)(exec|execute|select|insert|update|delete|create|alter|drop|rename|truncate|backup|restore)\s", RegexOptions.IgnoreCase).Success)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
