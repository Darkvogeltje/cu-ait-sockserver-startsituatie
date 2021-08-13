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
        public static async Task<List<string>> GetActiveIp4S()
        {
            List<string> activeIps = new List<string>
            {
                "127.0.0.1"
            };
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
            var dataSet = new DataSet();
            var dataTable = new DataTable();
            var dataColumn = new DataColumn
            {
                ColumnName = "IP",
                DataType = typeof(string)
            };
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn
            {
                ColumnName = "Port",
                DataType = typeof(int)
            };
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn
            {
                ColumnName = "Folder",
                DataType = typeof(string)
            };
            dataTable.Columns.Add(dataColumn);
            var dataRow = dataTable.NewRow();
            dataRow[0] = "127.0.0.1";
            dataRow[1] = 49200;
            dataRow[2] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dataTable.Rows.Add(dataRow);
            dataSet.Tables.Add(dataTable);
            var xmlFile = Directory.GetCurrentDirectory() + "\\config.xml";
            dataSet.WriteXml(xmlFile, XmlWriteMode.WriteSchema);
        }

        public static DataTable ReadConfigFile()
        {
            var xmlFile = Directory.GetCurrentDirectory() + "\\config.xml";

            if (!File.Exists(xmlFile))
            {
                MakeConfigFile();
            }

            var ds = new DataSet();
            ds.ReadXml(xmlFile, XmlReadMode.ReadSchema);
            return ds.Tables[0];
        }

        public static void UpdateConfigFile(string ipNumber, int portNumber, string workingFolder)
        {
            var xmlFile = Directory.GetCurrentDirectory() + "\\config.xml";

            if (!File.Exists(xmlFile))
            {
                MakeConfigFile();
            }

            var dataSet = new DataSet();
            dataSet.ReadXml(xmlFile, XmlReadMode.ReadSchema);
            dataSet.Tables[0].Rows[0][0] = ipNumber;
            dataSet.Tables[0].Rows[0][1] = portNumber;
            dataSet.Tables[0].Rows[0][2] = workingFolder;
            dataSet.WriteXml(xmlFile, XmlWriteMode.WriteSchema);

        }
    }

}
