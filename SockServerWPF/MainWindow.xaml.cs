using System;
using System.Linq;
using System.Text;
using System.Windows;
using SockServerLib;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;

namespace SockServerWPF
{
    public partial class MainWindow
    {
        private Socket _listener;
        private int _numberOfClients = 1;
        private bool _continueThis = true;
        private string _activeFolder;
        private string _baseFolder;

        public MainWindow()
        {
            InitializeComponent();
            DoStartup();
            FillDictionary();
            btnStart.Visibility = Visibility.Visible;
            btnStop.Visibility = Visibility.Hidden;
        }
        public static void DoEvents()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
        private async void DoStartup()
        {
            cmbIPs.ItemsSource = await Helper.GetActiveIp4S(); 
            var dataTable = Helper.ReadConfigFile(); 
            txtPort.Text = dataTable.Rows[0]["Port"].ToString(); 
            lblWorkingFolder.Content = dataTable.Rows[0]["Folder"].ToString(); 
            _activeFolder = dataTable.Rows[0]["Folder"].ToString(); 
            _baseFolder = dataTable.Rows[0]["Folder"].ToString();
            try
            {
                cmbIPs.SelectedItem = dataTable.Rows[0]["IP"].ToString();
            }
            catch
            {
                cmbIPs.SelectedItem = "127.0.0.1";
            }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = lblWorkingFolder.Content.ToString(); 
            var result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                lblWorkingFolder.Content = folderBrowserDialog.SelectedPath; 
                _activeFolder = folderBrowserDialog.SelectedPath; 
                _baseFolder = folderBrowserDialog.SelectedPath;
            }
        }

        private void BtnSelectWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            var ip = cmbIPs.SelectedItem.ToString(); 
            var workingFolder = lblWorkingFolder.Content.ToString(); 
            _activeFolder = workingFolder; 
            _baseFolder = workingFolder; 
            int.TryParse(txtPort.Text, out var portNumber); 
            if (portNumber == 0) portNumber = 49200;
            if (portNumber < 49152) portNumber = 49200; 
            if (portNumber > 65535) portNumber = 49200; 
            txtPort.Text = portNumber.ToString(); 
            Helper.UpdateConfigFile(ip, portNumber, workingFolder); 
            System.Windows.MessageBox.Show("Configuration saved", "Configuration saved to config.xml", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FillDictionary()
        {
            tbkDictionary.Text = "";
            tbkDictionary.Text += "DIRALL" + Environment.NewLine;
            tbkDictionary.Text += "DIRFILES" + Environment.NewLine;
            tbkDictionary.Text += "DIRFOLDERS" + Environment.NewLine;
            tbkDictionary.Text += "CURRENTDIR" + Environment.NewLine;
            tbkDictionary.Text += "CHANGEDIR <existing foldername>" + Environment.NewLine;
            tbkDictionary.Text += "CHANGEDIR UP" + Environment.NewLine;
            tbkDictionary.Text += "CHANGEDIR ROOT" + Environment.NewLine;
            tbkDictionary.Text += "MAKEDIR <non existing foldername>" + Environment.NewLine;
            tbkDictionary.Text += "REMOVEDIR <remove existing empty folder>" + Environment.NewLine;
            tbkDictionary.Text += "REMOVEDIRALL <remove existing folder>" + Environment.NewLine;
            tbkDictionary.Text += "RENAMEDIR <existing folder>,<new name>" + Environment.NewLine;
            tbkDictionary.Text += "CONTENTFILE <existing file>" + Environment.NewLine;
            tbkDictionary.Text += "REMOVEFILE <remove existing file>" + Environment.NewLine;
            tbkDictionary.Text += "RENAMEFILE <rename existing file>,<new name>" + Environment.NewLine;

        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.Visibility = Visibility.Hidden; 
            btnStop.Visibility = Visibility.Visible; 
            grpConfig.IsEnabled = false; 
            tbkInfo.Text = ""; 
            _activeFolder = _baseFolder; 
            ExecuteServer();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStart.Visibility = Visibility.Visible; 
            btnStop.Visibility = Visibility.Hidden; 
            grpConfig.IsEnabled = true; 
            _continueThis = false;
            try
            {
                _listener.Close();
            }
            catch
            {
                tbkInfo.Text = $"Socket cannot be stopped at : {DateTime.Now:dd/MM/yyyy HH:mm:ss} \n" + tbkInfo.Text;
            }
            _listener = null; 
            tbkInfo.Text = $"Socket stopped at : {DateTime.Now:dd/MM/yyyy HH:mm:ss} \n" + tbkInfo.Text;
        }
        public void ExecuteServer()
        {
            var ipAddress = IPAddress.Parse(cmbIPs.SelectedItem.ToString()); 
            tbkInfo.Text = $"I will listen to IP : {ipAddress} \n" + tbkInfo.Text; 
            var serverEndPoint = new IPEndPoint(ipAddress, int.Parse(txtPort.Text)); 
            tbkInfo.Text = $"My endpoint will be : {serverEndPoint} \n" + tbkInfo.Text; 
            _listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _listener.Bind(serverEndPoint);
                _listener.Listen(_numberOfClients);
                tbkInfo.Text = $"Socket started at : {DateTime.Now:dd/MM/yyyy HH:mm:ss} \n" + tbkInfo.Text;
                tbkInfo.Text = $"My maximum capacity : {_numberOfClients} \n" + tbkInfo.Text;
                _continueThis = true;
                while (_continueThis)
                {
                    DoEvents();
                    if (_listener == null) break;
                    if (!_listener.Poll(1000000, SelectMode.SelectRead)) continue;
                    var clientSocket = _listener.Accept();
                    var clientRequest = new byte[1024];
                    var command = "";
                    while (true)
                    {
                        var numByte = clientSocket.Receive(clientRequest);
                        command += Encoding.ASCII.GetString(clientRequest, 0, numByte);
                        if (command.IndexOf("##EOM", StringComparison.Ordinal) > -1)
                            break;
                    }

                    tbkInfo.Text = "\n=================================\n" + tbkInfo.Text;
                    tbkInfo.Text = $"request = {command}\n" + tbkInfo.Text;
                    command = command.ToUpper();
                    command = command.Replace("##EOM", "").Trim();
                    var result = command.Length < 5
                        ? $"{command} is an unknown instruction\n"
                        : $"{ExecuteCommand(command)}\n";
                    var clientResponse = Encoding.ASCII.GetBytes(result);
                    clientSocket.Send(clientResponse);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    tbkInfo.Text = $"response = {result}" + tbkInfo.Text;
                }
            }
            catch (Exception exception)
            {
                if (_continueThis)
                {
                    tbkInfo.Text = $"Error : {exception.Message} \n" + tbkInfo.Text;
                }
            }
        }

        private string ExecuteCommand(string command)
        {
            if (command == "HELLO")
            {
                return "<cf>" + _activeFolder;
            }
            if (command == "GOODBYE")
            {
                _activeFolder = _baseFolder;
                return "HAVE A NICE DAY";
            }
            if (command == "DIRALL")
            {
                return DIRALL();
            }
            if (command == "DIRFILES")
            {
                return DIRFILES();
            }
            if (command == "DIRFOLDERS")
            {
                return DIRFOLDERS();
            }
            if (command == "CURRENTDIR")
            {
                return CURRENTDIR();
            }
            if (command.IndexOf("CHANGEDIR", StringComparison.Ordinal) > -1)
            {
                switch (command)
                {
                    case "CHANGEDIR UP":
                        return CHANGEDIR_UP();
                    case "CHANGEDIR ROOT":
                        return CHANGEDIR_ROOT();
                    default:
                    {
                        var parts = command.Split('|');
                        return CHANGEDIR(parts[1]);
                    }
                }
            }
            if (command.IndexOf("MAKEDIR", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return MAKEDIR(parts[1]);
            }
            if (command.IndexOf("REMOVEDIR|", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return REMOVEDIR(parts[1]);
            }
            if (command.IndexOf("REMOVEDIRALL", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return REMOVEDIRALL(parts[1]);
            }
            if (command.IndexOf("RENAMEDIR", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return RENAMEDIR(parts[1], parts[2]);
            }
            if (command.IndexOf("REMOVEFILE|", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return REMOVEFILE(parts[1]);
            }
            if (command.IndexOf("RENAMEFILE", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return RENAMEFILE(parts[1], parts[2]);
            }
            if (command.IndexOf("CONTENTFILE", StringComparison.Ordinal) > -1)
            {
                var parts = command.Split('|');
                return CONTENTFILE(parts[1]);
            }
            return "UNKNOWN INSTRUCTION";
        }



        private string DIRALL()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Subfolders & files in current folder : \n");
            var basedir = new DirectoryInfo(_activeFolder);
            foreach (var directoryInfo in basedir.GetDirectories())
            {
                stringBuilder.AppendLine($"<dir>\t{directoryInfo.Name}");
            }
            foreach (var fileInfo in basedir.GetFiles())
            {
                stringBuilder.AppendLine($"\t{fileInfo.Name}");
            }
            return stringBuilder.ToString();
        }
        private string DIRFILES()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Files in current folder : \n");
            var basedir = new DirectoryInfo(_activeFolder);
            foreach (var fileInfo in basedir.GetFiles())
            {
                stringBuilder.AppendLine($"\t{fileInfo.Name}");
            }
            return stringBuilder.ToString();
        }
        private string DIRFOLDERS()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Subfolders in current folder : \n");

            var basedir = new DirectoryInfo(_activeFolder);
            foreach (var directoryInfo in basedir.GetDirectories())
            {
                stringBuilder.AppendLine($"<dir>\t{directoryInfo.Name}");
            }
            return stringBuilder.ToString();
        }
        private string CURRENTDIR()
        {
            return _activeFolder;
        }
        private string CHANGEDIR(string subFolder)
        {
            if (Directory.Exists(_activeFolder + "\\" + subFolder))
            {
                _activeFolder = _activeFolder + "\\" + subFolder;
                return "<cf>" + _activeFolder;
            }
            return $"Error : folder unchanged\nCurrent folder is still : {_activeFolder}";
        }
        private string CHANGEDIR_UP()
        {
            if (_activeFolder == _baseFolder)
            {
                return "<cf>" + _activeFolder;
            }
            var directoryInfo = new DirectoryInfo(_activeFolder);
            if (directoryInfo.Parent != null) _activeFolder = directoryInfo.Parent.FullName;
            return "<cf>" + _activeFolder;

        }
        private string CHANGEDIR_ROOT()
        {
            _activeFolder = _baseFolder;
            return "<cf>" + _activeFolder;

        }
        private string MAKEDIR(string subFolder)
        {
            if (Directory.Exists(_activeFolder + "\\" + subFolder))
                return $"Error : folder already exists\nCurrent folder is still : {_activeFolder}";
            try
            {
                Directory.CreateDirectory(_activeFolder + "\\" + subFolder);
            }
            catch (Exception exception)
            {
                return $"Error : {exception.Message}\nCurrent folder is still : {_activeFolder}";

            }
            _activeFolder = _activeFolder + "\\" + subFolder;
            return "<cf>" + _activeFolder;
        }
        private string REMOVEDIR(string folder)
        {
            if (!Directory.Exists(_activeFolder + "\\" + folder)) return "Error : folder does not exists";
            try
            {
                var directoryInfo = new DirectoryInfo(_activeFolder + "\\" + folder);
                var directoryCount = directoryInfo.GetDirectories().Count();
                var fileCount = directoryInfo.GetFiles().Count();
                if (fileCount > 0 || directoryCount > 0)
                {
                    return "Error : folder NOT empty";

                }
                Directory.Delete(_activeFolder + "\\" + folder);
            }
            catch (Exception exception)
            {
                return $"Error : {exception.Message}";

            }
            return "<cf>" + _activeFolder;
        }
        private string REMOVEDIRALL(string folder)
        {
            if (!Directory.Exists(_activeFolder + "\\" + folder)) return "Error : folder does not exists";
            try
            {
                Directory.Delete(_activeFolder + "\\" + folder, true);
            }
            catch (Exception exception)
            {
                return $"Error : {exception.Message}";

            }
            return "<cf>" + _activeFolder;
        }
        private string RENAMEDIR(string oldFolderName, string newFolderName)
        {
            if (Directory.Exists(_activeFolder + "\\" + oldFolderName))
            {
                try
                {
                    Directory.Move(_activeFolder + "\\" + oldFolderName, _activeFolder + "\\" + newFolderName);
                    return "<cf>" + _activeFolder;
                }
                catch (Exception exception)
                {
                    return $"Error : {exception.Message}";
                }
            }
            return "Error : folder does not exists";
        }
        private string RENAMEFILE(string oldFileName, string newFileName)
        {
            if (File.Exists(_activeFolder + "\\" + oldFileName))
            {
                try
                {
                    File.Move(_activeFolder + "\\" + oldFileName, _activeFolder + "\\" + newFileName);
                    return "<cf>" + _activeFolder;
                }
                catch (Exception exception)
                {
                    return $"Error : {exception.Message}";
                }
            }
            return "Error : file does not exists";
        }
        private string REMOVEFILE(string fileName)
        {
            if (File.Exists(_activeFolder + "\\" + fileName))
            {
                try
                {
                    File.Delete(_activeFolder + "\\" + fileName);
                }
                catch (Exception exception)
                {
                    return $"Error : {exception.Message}";

                }
                return "<cf>" + _activeFolder;
            }
            return "Error : file does not exists";
        }
        private string CONTENTFILE(string filename)
        {
            if (File.Exists(_activeFolder + "\\" + filename))
            {
                try
                {
                    var bytes = File.ReadAllBytes(_activeFolder + "\\" + filename);
                    return Encoding.ASCII.GetString(bytes);
                }
                catch (Exception exception)
                {
                    return $"Error : {exception.Message}";

                }
            }
            return "Error : file does not exists";
        }
    }
}
