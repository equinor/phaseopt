using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test_Space
{
    public static class Testing
    {
        public static void Main()
        {
            IP21_Comm DB_Connection = new IP21_Comm("KAR-IP21.statoil.net", "10014");

            DB_Connection.Connect();
            DB_Connection.Insert_Value("PO_E_P0", 100.354, DateTime.Now.AddSeconds(-20));

            DB_Connection.Disconnect();

        }
    }
}
