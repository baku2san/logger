using loggerApp.Queue;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace loggerApp.Producers
{
    public class PlcMemoryValueList : IQueueingData
    {
        public string Name { get; set; }
        public SqlCommand SqlCommand { get; set; }
        public DateTime Created { get; set; }

        public List<List<PlcMemoryValue>> PlcMemoryValuesGroups { get; set; }

        public PlcMemoryValueList(string name, SqlCommand sqlCommand)
        {
            Name = name;
            SqlCommand = sqlCommand;
            Created = DateTime.Now;

            PlcMemoryValuesGroups = new List<List<PlcMemoryValue>>();
        }
    }
}
