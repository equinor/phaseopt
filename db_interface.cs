using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace PhaseOpt
{
    public class DB_Interface
    {
        public static Hashtable tester(string[] Tag_Name, DateTime Time_Stamp)
        {
            string Conn = @"dsn=IP21";

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
            double Value;
            Hashtable Composition = new Hashtable();
            while (DR.Read())
            {
                System.Console.WriteLine("{0}\t{1}", DR.GetValue(0), DR.GetValue(1));
                Value = DR.GetFloat(1);
                Composition.Add(DR.GetValue(0), DR.GetValue(1));
            }
            Cmd.Connection.Close();
            return Composition;
        }

        public static void Read_Config()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            XmlReader reader = XmlReader.Create("PhaseOpt.xml", settings);

            List<int>    Asgard_IDs           = new List<int>();
            List<string> Asgard_Tags          = new List<string>();
            List<double> Asgard_Scale_Factors = new List<double>();

            List<int>    Statpipe_IDs           = new List<int>();
            List<string> Statpipe_Tags          = new List<string>();
            List<double> Statpipe_Scale_Factors = new List<double>();

            while (reader.Read())
            {
                switch (reader.Name)
                {
                    case "asgard":
                        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                        {
                            if (reader.Name == "component")
                            {
                                Asgard_IDs.Add(XmlConvert.ToInt32(reader.GetAttribute("id")));
                                Asgard_Tags.Add(reader.GetAttribute("tag"));
                                Asgard_Scale_Factors.Add(XmlConvert.ToDouble(reader.GetAttribute("scale-factor")));
                            }
                        }
                        break;
                    case "statpipe":
                        while (reader.Read())
                        {
                            if (reader.Name == "component")
                            {
                                Statpipe_IDs.Add(XmlConvert.ToInt32(reader.GetAttribute("id")));
                                Statpipe_Tags.Add(reader.GetAttribute("tag"));
                                Statpipe_Scale_Factors.Add(XmlConvert.ToDouble(reader.GetAttribute("scale-factor")));
                            }
                        }
                        break;
                }
               Console.WriteLine("{0}", reader.Name);
            }
        }
    }
}
