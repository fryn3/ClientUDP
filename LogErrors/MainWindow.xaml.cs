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

namespace LogErrors
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static IPAddress remoteIPAddress;
        private static int remotePort;
        private static int localPort;

        public static int Fl_btn_version { get; private set; }
        public static int Fl_btn_log_errors { get; private set; }
        UdpClient varUdpClient;
        public MainWindow()
        {
            InitializeComponent();
            localPort = 2054;
            remotePort = 2054;
            Fl_btn_version = 0;
            Fl_btn_log_errors = 0;
            remoteIPAddress = IPAddress.Parse("192.168.1.163");
            varUdpClient = new UdpClient(localPort);
        }

        private async void BtnVersion_Click(object sender, RoutedEventArgs e)
        {
            BtnLogErrors.IsEnabled = false;
            byte[] datagram = { 0xA0 };
            Send(datagram);
            if (Fl_btn_version == 0 && Fl_btn_log_errors == 0)
            {
                Fl_btn_version = 1;
                byte[] r = await ReceiverAsync();
                TbVersion.Text = Encoding.UTF8.GetString(r);
                Fl_btn_version = 0;
                BtnLogErrors.IsEnabled = true;
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)    // закрытие окна
        {
            Close();
        }

        private async void BtnLogErrors_Click(object sender, RoutedEventArgs e)
        {
            BtnVersion.IsEnabled = false;
            byte[][] r = new byte[8][];
            Fl_btn_log_errors++;
            if (Fl_btn_log_errors > 1)
            {
                byte[] datagram = { 0xA0 };
                await Task.Delay(1000);
                Send(datagram);
                await Task.Delay(1000);
            }
            for (byte i = 0; i < 8; i++)
            {
                byte[] datagram = { 0xA7, i };
                Send(datagram);
                r[i] = await ReceiverAsync();
                if (Fl_btn_log_errors > 1)
                {
                    Fl_btn_log_errors--;
                    return;
                }
                TbLogErrors.Text = i.ToString();
            }
            using (FileStream fstream = new FileStream(@"..\LogErrors.hex", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < r.Length; i++)
                {
                    fstream.Write(r[i], 0, r[i].Length);
                }
            }
            BtnVersion.IsEnabled = true;
            TbLogErrors.Text = "Done!";
            Fl_btn_log_errors--;
        }

        public byte[] Receiver()
        {
            //using (UdpClient receivingUdpClient = new UdpClient(localPort))
            //{
                IPEndPoint RemoteIpEndPoint = null;
                byte[] receiveBytes = varUdpClient.Receive(ref RemoteIpEndPoint);
                return receiveBytes;
            //}
        }
        Task<byte[]> ReceiverAsync()
        {
            return Task.Run(() => Receiver());
        }

        private void Send(byte[] datagram)
        {
            //UdpClient sender = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);

            try
            {
                varUdpClient.SendAsync(datagram, datagram.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
            finally
            {
                //varUdpClient.Close();
            }
        }

        public void Message(byte[] receiveBytes)
        {
            if (Fl_btn_log_errors > 0)  // нажата кнопка чтения журнала
            {
                // запись в файл
                if (Fl_btn_log_errors == 1)
                {
                    using (FileStream fstream = new FileStream(@"..\LogErrors.hex", FileMode.OpenOrCreate)) // тут нужно записывать с начала файла
                    {
                        fstream.Write(receiveBytes, 0, receiveBytes.Length);
                    }
                    Fl_btn_log_errors++;
                }
                else if (Fl_btn_log_errors > 1)
                {
                    using (FileStream fstream = new FileStream(@"..\LogErrors.hex", FileMode.Append)) // тут нужно продолжать запись
                    {
                        fstream.Write(receiveBytes, 0, receiveBytes.Length);
                    }
                    Fl_btn_log_errors++;
                }
                if (Fl_btn_log_errors == 7)
                {
                    // тут нужно создать .csv файл.
                    Fl_btn_log_errors = 0;
                    BtnVersion.IsEnabled = true;
                }
            }
            if (Fl_btn_version == 1)    // нажата кнопка чтение версии
            {
                TbVersion.Text = receiveBytes.ToString();
            }
        }
        public delegate void MethodContainer(byte[] receiveBytes);

    }
}
