using System;

namespace PhaseOpt
{
    public class DB_Interface
    {
        public static void tester()
        {
            string Conn = @"dsn=IP21";
            string Tag_Name = @"31AI0157A_%";

            System.Data.Odbc.OdbcCommand Cmd = new System.Data.Odbc.OdbcCommand();
            Cmd.Connection = new System.Data.Odbc.OdbcConnection(Conn);
            Cmd.Connection.Open();

            DateTime Time_Stamp = new DateTime(2014, 6, 13, 12, 48, 45);
            System.Console.WriteLine(Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss"));

            Cmd.CommandText =
@"SELECT
  NAME, VALUE
FROM
  History
WHERE
  NAME like '" + Tag_Name + @"'
  AND TS > CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS') - 000:00:01.0
  AND TS < CAST('" + Time_Stamp.ToString("yyyy-MM-dd HH:mm:ss") + @"' AS TIMESTAMP FORMAT 'YYYY-MM-DD HH:MI:SS') + 000:00:01.0
  AND PERIOD = 000:0:01.0;
";

            Cmd.CommandTimeout = 60;

            System.Data.Odbc.OdbcDataReader DR = Cmd.ExecuteReader();

            while (DR.Read())
            {
                System.Console.WriteLine("{0}\t{1}", DR.GetValue(0), DR.GetValue(1));
            }
            Cmd.Connection.Close();
        }
    }
}
