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
        //public static MainWindow Instance { get; private set; } // тут будет форма
        private static IPAddress remoteIPAddress;
        private static int remotePort;
        private static int localPort;

        public static int Fl_btn_version { get; private set; }
        public static int Fl_btn_log_errors { get; private set; }
        public event MethodContainer DoEvent;
        public MainWindow()
        {
            InitializeComponent();
            //Instance = this;
            
            localPort = 2054;
            remotePort = 2054;
            Fl_btn_version = 0;
            Fl_btn_log_errors = 0;
            remoteIPAddress = IPAddress.Parse("192.168.1.163");
            //Thread tRec = new Thread(new ThreadStart(Receiver));
            //tRec.Start();
        }

        private void BtnVersion_Click(object sender, RoutedEventArgs e)
        {
            byte[] datagram = { 0xA0, 0x00, 0x00, 0x00 };
            Fl_btn_version = 1;
            Send(datagram);
            TbVersion.Text = Encoding.UTF8.GetString(ReceiverR());
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)    // закрытие окна
        {
            Close();
        }

        private void BtnLogErrors_Click(object sender, RoutedEventArgs e)
        {
            Fl_btn_log_errors = 1;
            BtnVersion.IsEnabled = false;
        }

        public void Receiver()
        {
            DoEvent += Message;
            //try
            //{
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;
            while (true)
            {
                byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                DoEvent(receiveBytes);
                break;
            }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            //}

        }
        public byte[] ReceiverR()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;
            while (true)
            {
                byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                return receiveBytes;
            }
        }

        private static void Send(byte[] datagram)
        {
            UdpClient sender = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);

            try
            {
                sender.Send(datagram, datagram.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
            finally
            {
                sender.Close();
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
