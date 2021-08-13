using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SockServerLib
{
    public class Helper
    {
        public static async Task<List<string>> GetActiveIP4s()
        {
            List<string> activeIps = new List<string>();
            activeIps.Add("127.0.0.1");
            var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            foreach (var ipAddress in host.AddressList)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    activeIps.Add(ipAddress.ToString());
                }
            }

            return activeIps;
        }

        private static void MakeConfigFile()
        {
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable();
            DataColumn dataColumn = new DataColumn();
            dataColumn.ColumnName = "IP";
            dataColumn.DataType = typeof(string);
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn();
            dataColumn.ColumnName = "Port";
            dataColumn.DataType = typeof(int);
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn();
            dataColumn.ColumnName = "Folder";
            dataColumn.DataType = typeof(string);
            dataTable.Columns.Add(dataColumn);
            DataRow dataRow = dataTable.NewRow();
            dataRow[0] = "127.0.0.1";
            dataRow[1] = 49200;
            dataRow[2] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dataTable.Rows.Add(dataRow);
            string XMLFile = Directory.GetCurrentDirectory() + "/config.xml";
            dataSet.WriteXml(XMLFile, XmlWriteMode.WriteSchema);
        }

        public static DataTable ReadConfigFile()
        {
            string XMLFile = Directory.GetCurrentDirectory() + "/config.xml";

            if (!File.Exists(XMLFile))
            {
                MakeConfigFile();
            }

            DataSet ds = new DataSet();
            ds.ReadXml(XMLFile, XmlReadMode.ReadSchema);
            return ds.Tables[0];
        }

        public static void UpdateConfigFile(string ipNumber, int portNumber, string workingFolder)
        {
            string XMLFile = Directory.GetCurrentDirectory() + "/config.xml";

            if (!File.Exists(XMLFile))
            {
                MakeConfigFile();
            }

            DataSet dataSet = new DataSet();
            dataSet.ReadXml(XMLFile, XmlReadMode.ReadSchema);
            dataSet.Tables[0].Rows[0][0] = ipNumber;
            dataSet.Tables[0].Rows[0][1] = portNumber;
            dataSet.Tables[0].Rows[0][2] = workingFolder;
            dataSet.WriteXml(XMLFile, XmlWriteMode.WriteSchema);

        }
    }

}
