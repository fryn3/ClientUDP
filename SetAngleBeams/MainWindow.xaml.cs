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
        public enum PanelComandEnum : byte
        {
            FLAG_PANEL_MIN = 0xA0 - 1,  // НЕ КОМАНДА! флаг минимальной команды.

            PANEL_BUILD_MK,             // A0 отправляет версию МК
            PANEL_RK_IN,                // A1 отправляет RK_IN
            PANEL_RK_OUT_SET,           // A2 выставляет RK_OUT (светодиоды)  (только в техн)
            PANEL_RK_OUT_GET,           // A3 отправка RK_OUT (светодиоды)
            PANEL_INDEX_MODULATION_SET, // A4 выставляет индекс модуляции
            PANEL_INDEX_MODULATION_GET, // A5 отправляет индекс модуляции
            PANEL_TEMPERATURE,          // A6 отправляет температуру 3х датчиков
            PANEL_TEMPERATURE_RAW,      // A7 отправляет значение АЦП 3х датчиков
            PANEL_LOG_ERROR_GET,        // A8 отправляет 1 Кб журнала отказа
            PANEL_LOG_ERROR_CLEAR,      // A9 очищает журнал отказа
            PANEL_DOPLER_GET,           // AA отправляет смещение по 4ем лучам
            PANEL_none,                 // AB пустышка
            PANEL_MAIN_MODE_SET,        // * AC выход в боевой
            PANEL_MAIN_MODE_GET,        // AD возвращаем режим работы
            PANEL_BUILD_FPGA,           // AE отправка версию FPGA
            PANEL_TEST_SIGNAL_SET,      // AF Установка тестового сигнала
            PANEL_TEST_SIGNAL_GET,      // B0 Запросить параметр тестового сигнала
            PANEL_N_RAY_SET,            // B1 Установка номер луча
            PANEL_N_RAY_GET,            // B2 Прочитать статус номер луча
            PANEL_LVTTL_OUT_SET,        // B3 Установить выходные LVTTL
            PANEL_LVTTL_OUT_GET,        // B4 Запросить состояние выходных LVTTL
            PANEL_LVTTL_IN,             // B5 Запросить состояние входных LVTTL
            PANEL_40_80_SET,            // B6 Выставить плис в режиме 40/80
            PANEL_40_80_GET,            // B7 Запрос режима плис
            PANEL_GO_TO_ERROR,          // B8 переходи в отказ, только в боевом
            PANEL_TECH_MODE_SET,        // B9 "запрет СС, запрет ЖуО, запрет перехода 40/80" in 1 byte
            PANEL_TECH_MODE_GET,        // BA "запрет СС, запрет ЖуО, запрет перехода 40/80" in 1 byte
            PANEL_ANGLE_ALL_SET,        // BB Выставление углов
            PANEL_ANGLE_ALL_GET,        // BC Прочитать значение углов всех лучей
            PANEL_TEMP_KALIB_SET,       // BD Выставить калибровачные данные для темп датчиков
            PANEL_TEMP_KALIB_GET,       // BE Прочитать калибровачные данные для темп датчиков

            FLAG_PANEL_MAX              // Не команда! флаг макс команды
        };
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
            //lInfo.Content = "В версии А этой команды нет";
            btn.ForEach(x => x.IsEnabled = false);
            BtnVersion.IsEnabled = true;
            byte[] datagram = { (byte)PanelComandEnum.PANEL_BUILD_MK};
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
            datagram[0] = (byte) PanelComandEnum.PANEL_ANGLE_ALL_SET;
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
            if (!(r[0] == (byte) PanelComandEnum.PANEL_ANGLE_ALL_SET && r[1] == 0x55))
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
            byte[] datagram = new byte[] { (byte)PanelComandEnum.PANEL_ANGLE_ALL_GET };
            Send(datagram);
            byte[] r = await ReceiverAsync();
            if (r[0] == (byte)PanelComandEnum.PANEL_ANGLE_ALL_GET && r[1] == 0x33 && r.Length == 2)
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
