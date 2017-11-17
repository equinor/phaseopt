using System;
using System.Collections;
using System.Collections.Generic;

public class IP21_Comm
{
    public string IP21_Host;
    public string IP21_Port;
    private System.Data.Odbc.OdbcCommand Cmd;

    public IP21_Comm(string Host, string Port)
    {
        IP21_Host = Host;
        IP21_Port = Port;
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

    public void Write_Value(string Tag_Name, double Value, string Quality = "Good")
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

    public void Write_Value(string Tag_Name, int Value, string Quality = "Good")
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;

        System.Data.Odbc.OdbcCommand Cmd = new System.Data.Odbc.OdbcCommand();
        Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmd.Connection.Open();

        Cmd.CommandText =
@"UPDATE ip_discretedef
  SET ip_input_value = " + Value.ToString() + @", ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();
        Cmd.Connection.Close();
    }

    public void Write_Value(string Tag_Name, string Value, string Quality = "Good")
    {
        string Conn = @"DRIVER={AspenTech SQLplus};HOST=" + IP21_Host + @";PORT=" + IP21_Port;

        System.Data.Odbc.OdbcCommand Cmd = new System.Data.Odbc.OdbcCommand();
        Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
        Cmd.Connection.Open();

        Cmd.CommandText =
@"UPDATE ip_textdef
  SET ip_input_value = '" + Value + @"', ip_input_quality = '" + Quality + @"'
  WHERE name = '" + Tag_Name + @"'";

        System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();
        Cmd.Connection.Close();
    }

    public void Insert_Value(string Tag_Name, double Value, DateTime Time_Stamp)
    {
        Cmd.CommandText =
@"INSERT INTO " + Tag_Name + @"(IP_TREND_TIME, IP_TREND_VALUE)
  VALUES (CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS'), "
                  + Value.ToString() + @");";

        Cmd.ExecuteNonQuery();
    }
}
