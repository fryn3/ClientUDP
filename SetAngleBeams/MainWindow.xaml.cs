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

namespace SetAngleBeams
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static IPAddress remoteIPAddress;
        private static int remotePort;
        private static int localPort;
        private static UdpClient varUdpClient;
        private static List<Button> btn;
        public static List<RoutedCommand> MyHotkey;
        private static int flBtnBusy;
        private List<TableRow> resultGrid = new List<TableRow>(4);
        public MainWindow()
        {
            InitializeComponent();
            localPort = 45454;
            remotePort = 45454;
            remoteIPAddress = IPAddress.Parse("192.168.1.163");
            varUdpClient = new UdpClient(localPort);
            btn = new List<Button>();
            btn.Add(BtnVersion);
            btn.Add(BtnSend);
            btn.Add(BtnSave);
            btn.Add(BtnOpen);
            btn.Add(BtnRead);
            MyHotkey = new List<RoutedCommand>();
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.F1));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], ShowMessage_Executed));
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.D1, ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], BtnVersion_Click));
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.D2, ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], BtnSend_Click));
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.D3, ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], BtnSave_Click));
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.D4, ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], BtnOpen_Click));
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.D5, ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], BtnRead_Click));
            MyHotkey.Add(new RoutedCommand());
            MyHotkey[MyHotkey.Count - 1].InputGestures.Add(new KeyGesture(Key.Escape));
            CommandBindings.Add(new CommandBinding(MyHotkey[MyHotkey.Count - 1], BtnExit_Click));
            flBtnBusy = 0;
        }

        //Загрузка содержимого таблицы
        private void grid_Loaded(object sender, RoutedEventArgs e)
        {
            for (byte i = 0; i < 4; i++)
            {
                resultGrid.Add(new TableRow(i, 11.65, 16.53));
            }
            dataGrid.ColumnWidth = 105;
            dataGrid.ItemsSource = resultGrid;
            dataGrid.Columns[0].IsReadOnly = true;
        }

        private void ShowMessage_Executed(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Горячие клавиши:\n" +
                "Справка F1\n" +
                "Запрос версии МК Alt+1\n" +
                "Отправить данные на МК Alt+2\n" +
                "Сохранить параметры в файл Alt+3\n" +
                "Открыть файл с параметрами Alt+4\n" +
                "Считать параметры с МК Alt+5\n" +
                "Выход Esc\n");
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
                String buildMc = Encoding.UTF8.GetString(r.Skip(1).Take(r.Length - 1).ToArray());
                TbVersion.Text = buildMc;
                flBtnBusy--;
                btn.ForEach(x => x.IsEnabled = true);
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            lInfo.Content = "Отправляются данные на МК.";
            dataGrid.IsEnabled = false;
            BtnOpen.IsEnabled = false;
            BtnRead.IsEnabled = false;
            byte[] datagram = new byte[16 + 1];
            datagram[0] = 0xBB;
            foreach (TableRow row in dataGrid.Items)
            {
                Array.Copy(
                    AnglConvector.SetPhiTetta(row.Beam, row.Phi - AnglConvector.Phi, row.Tetta - AnglConvector.Tetta),
                    0,
                    datagram,
                    1 + row.Beam * AnglConvector.SizeBeam,
                    AnglConvector.SizeBeam);
                
            }
            Send(datagram);
            byte[] r = await ReceiverAsync();
            if (!(r[0] == 0xBB && r[1] == 0x55))
                lInfo.Content = "Error";
            else
                lInfo.Content = "Данные успешно отправлены!";
            dataGrid.IsEnabled = true;
            btn.ForEach(x => x.IsEnabled = true);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            lInfo.Content = "Ф-нал кнопки пока не реализован!";
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            lInfo.Content = "Ф-нал кнопки пока не реализован!";
        }

        private async void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.IsEnabled = false;
            btn.ForEach(x => x.IsEnabled = false);
            BtnRead.IsEnabled = true;
            lInfo.Content = "Read!";
            byte[] datagram = new byte[] { 0xBC };
            Send(datagram);
            byte[] r = await ReceiverAsync();
            if (r[0] == 0xBC && r[1] == 0x33 && r.Length == 2)
            {
                lInfo.Content = "Error";
            } else if (r.Length == 17)
            {
                byte[] raw = new byte[16];
                Array.Copy(r, 1, raw, 0, 16);
                double[] doubAngls = AnglConvector.GetPhiTetta(raw);
                //List<TableRow> data = new List<TableRow>(4);
                resultGrid.Clear();
                for (byte i = 0; i < 4; i++)
                {
                    resultGrid.Add(new TableRow(i, doubAngls[2 * i] + AnglConvector.Phi, doubAngls[2 * i + 1] + AnglConvector.Tetta));
                }
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = resultGrid;
                //dataGrid.ItemsSource = data;
                lInfo.Content = "Data readed!";
            } else
            {
                lInfo.Content = "Error2";
            }
            dataGrid.IsEnabled = true;
            btn.ForEach(x => x.IsEnabled = true);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e) { Close(); }

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
    class TableRow
    {
        public TableRow(byte Beam, double Phi, double Tetta)
        {
            this.Beam = Beam;
            this.Phi = Phi;
            this.Tetta = Tetta;
        }
        public byte Beam { get; set; }
        public double Phi { get; set; }
        public double Tetta { get; set; }
    }
}
