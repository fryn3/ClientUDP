using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Diss;
using System.Globalization;

namespace ReadLogError
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static IPAddress remoteIPAddress;
        private static int remotePort;
        private static int localPort;

        private static int flBtnBusy;
        private static UdpClient varUdpClient;
        private static string buildMc;
        private static uint buildFpga;
        private static string fileOutBin = @"LogErrors.bin";
        private static List<Button> btn;
        public MainWindow()
        {
            InitializeComponent();
            localPort = 45454;
            remotePort = 45454;
            flBtnBusy = 0;
            remoteIPAddress = IPAddress.Parse("192.168.1.163");
            varUdpClient = new UdpClient(localPort);
            Title += " v." + Properties.VersionInfo.BuildDate.ToShortDateString();
            btn = new List<Button>();
            btn.Add(BtnVersion);
            btn.Add(BtnVersionFPGA);
            btn.Add(BtnLogErrors);
            btn.Add(BtnLogClear);
            //            TbVersion.Text = Properties.VersionInfo.BuildDate.ToString();
        }

        private async void BtnVersion_Click(object sender, RoutedEventArgs e)
        {
            btn.ForEach(x => x.IsEnabled = false);
            BtnVersion.IsEnabled = true;
            byte[] datagram = { 0xA0 };
            Send(datagram);
            if (flBtnBusy == 0)
            {
                flBtnBusy++;
                byte[] r = await ReceiverAsync();
                buildMc = Encoding.UTF8.GetString(r.Skip(1).Take(r.Length - 1).ToArray());
                TbVersion.Text = buildMc;
                flBtnBusy--;
                btn.ForEach(x => x.IsEnabled = true);
            }
        }

        private async void BtnVersionFPGA_Click(object sender, RoutedEventArgs e)
        {
            btn.ForEach(x => x.IsEnabled = false);
            BtnVersionFPGA.IsEnabled = true;
            byte[] datagram = { 0xAE };
            Send(datagram);
            if (flBtnBusy == 0)
            {
                flBtnBusy++;
                byte[] r = await ReceiverAsync();
                r = r.Skip(1).Take(r.Length - 1).ToArray();
                buildFpga = BitConverter.ToUInt32(r, 0);
                TbVersionFpga.Text = string.Format("0x{0,8:X8} ", buildFpga);
                TbVersionFpga.Text += string.Format("{0:hh:mm:ss} {0:dd}/{0:MM}/{0:yyyy}", WorkWithLogs.fpga_version(buildFpga));
                flBtnBusy--;
                btn.ForEach(x => x.IsEnabled = true);
            }
        }

        private async void BtnLogClear_Click(object sender, RoutedEventArgs e)
        {
            btn.ForEach(x => x.IsEnabled = false);
            BtnLogClear.IsEnabled = true;
            byte[] datagram = { 0xA9 };
            Send(datagram);
            if (flBtnBusy == 0)
            {
                flBtnBusy++;
                byte[] r = await ReceiverAsync();
                if (r.Length == 2 && r[0] == 0xA9 && r[1] == 0x55)
                    TbLogClear.Text = "Done!";
                flBtnBusy--;
                btn.ForEach(x => x.IsEnabled = true);
            }
        }

        private async void BtnLogErrors_Click(object sender, RoutedEventArgs e)
        {
            btn.ForEach(x => x.IsEnabled = false);
            BtnLogErrors.IsEnabled = true;
            byte[][] ethRx = new byte[8][];
            byte[][] logPage = new byte[8][];
            flBtnBusy++;
            while (flBtnBusy > 1)
            {
                byte[] datagram = { 0xA0 };
                await Task.Delay(100);
                Send(datagram);
                await Task.Delay(100);
            }
            for (byte i = 0; i < 8; i++)
            {
                byte[] datagram = { 0xA8, i };
                Send(datagram);
                ethRx[i] = await ReceiverAsync();
                if (flBtnBusy > 1)
                {
                    flBtnBusy--;
                    return;
                }
                if (ethRx[i].Length != 1025)
                {   // error! repeat please.
                    i--;
                }
                else
                {
                    logPage[i] = ethRx[i].Skip(1).Take(ethRx[i].Length - 1).ToArray();
                }
                TbLogErrors.Text = i.ToString();
            }
            using (FileStream fstream = new FileStream(fileOutBin, FileMode.OpenOrCreate))
            {
                for (int i = 0; i < logPage.Length; i++)
                {
                    fstream.Write(logPage[i], 0, logPage[i].Length);
                }
            }
            btn.ForEach(x => x.IsEnabled = true);
            TbLogErrors.Text = "Create LogErrors.bin!";

            DissLogs logs = new DissLogs(fileOutBin);
            WorkWithLogs.WriteToCsv(logs, buildFpga, Properties.VersionInfo.BuildDate, buildMc);
            Process.Start(logs.Name + ".csv");
            flBtnBusy--;
        }

        public byte[] Receiver()
        {
            IPEndPoint RemoteIpEndPoint = null;
            byte[] receiveBytes = varUdpClient.Receive(ref RemoteIpEndPoint);
            return receiveBytes;
        }

        Task<byte[]> ReceiverAsync()
        {
            return Task.Run(() => Receiver());
        }

        private void Send(byte[] datagram)
        {
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);
            try
            {
                varUdpClient.SendAsync(datagram, datagram.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
        
    }
}
