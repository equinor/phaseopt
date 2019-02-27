using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

public class IP21_Comm: IDisposable
{
    public string IP21_Host;
    public string IP21_Port;
    public bool IP21_Read_Only;
    private System.Data.Odbc.OdbcCommand Cmd;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // dispose managed resources
            Cmd.Dispose();
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

    public void Connect()
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;
        Cmd = new System.Data.Odbc.OdbcCommand();
        Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmd.Connection.Open();
    }

    public void Disconnect()
    {
        if (Cmd.Connection.State == System.Data.ConnectionState.Open)
        {
            Cmd.Connection.Close();
        }
    }

    public Hashtable Read_Values(string[] Tag_Name, DateTime Time_Stamp)
    {
        string Tag_cond = "(FALSE"; // Makes it easier to add OR conditions.
        foreach (string Tag in Tag_Name)
        {
            if (Sanitize(Tag))
            {
                Tag_cond += "\n  OR NAME = '" + Tag + "'";
            }
        }
        Tag_cond += ")";
        Cmd.CommandText =
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

        System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();
        Hashtable Result = new Hashtable();
        while (DR.Read())
        {
            if (DR.GetValue(2).ToString() == "0") // if status is good
            {
                Result.Add(DR.GetValue(0).ToString(), DR.GetValue(1));
            }
        }

        DR.Close();
        return Result;
    }

    public void Write_Value(string Tag_Name, double Value, string Quality = "Good")
    {
        Cmd.CommandText =
@"UPDATE ip_analogdef
  SET ip_input_value = " + Value.ToString("G", CultureInfo.InvariantCulture) + @", ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        if (!IP21_Read_Only) Cmd.ExecuteNonQuery();
    }

    public void Write_Value(string Tag_Name, int Value, string Quality = "Good")
    {
        Cmd.CommandText =
@"UPDATE ip_discretedef
  SET ip_input_value = " + Value.ToString() + @", ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        if (!IP21_Read_Only) Cmd.ExecuteNonQuery();
    }

    public void Write_Value(string Tag_Name, string Value, string Quality = "Good")
    {
        Cmd.CommandText =
@"UPDATE ip_textdef
  SET ip_input_value = '" + Value + @"', ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        if (!IP21_Read_Only) Cmd.ExecuteNonQuery();
    }

    public void Insert_Value(string Tag_Name, double Value, DateTime Time_Stamp)
    {
        Cmd.CommandText =
@"INSERT INTO " + Tag_Name + @"(IP_TREND_TIME, IP_TREND_VALUE)
  VALUES (CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS'), "
                  + Value.ToString("G", CultureInfo.InvariantCulture) + @");";

        if (double.IsNaN(Value))
        {
            Cmd.CommandText =
@"INSERT INTO " + Tag_Name + @"(IP_TREND_TIME, IP_TREND_VALUE)
  VALUES (CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS'), 0.0/0.0);";
        }

        if (!IP21_Read_Only) Cmd.ExecuteNonQuery();
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
