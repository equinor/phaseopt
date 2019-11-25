using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhaseOpt
{
    public class HistoryDb
    {
        public static double GetValue(string Tag, DateTime TimeStamp)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                return 0.0;
            }
        }

        public static void PutValue(string Tag, DateTime TimeStamp, double Value)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                //cnn.Execute("insert into History (TimeStamp, Tag, Value) values (@TimeStamp, @Tag, @Value)");
            }

        }
        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
