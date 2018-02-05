using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Diss
{
    public class DissLogs
    {
        public abstract class Log
        {
            // Корректная запись true.
            public bool IsGood { get; protected set; }
            // Время наработки. Цена разряда - 40 мс.
            public uint Total40ms { get; protected set; }
            // Кол-во включений МПР
            public byte TotalOn { get; protected set; }

            public byte CS { get; protected set; }
            // "Сырые" данные для парсинга
            public byte[] Raw { get; protected set; }
            // Пустой массив  true.
            public bool IsEmpty => (Raw == null || Raw.All(b => b == 0xFF));
//            public bool IsEmpty => Raw?.All(x => x == 0xFF) ?? true;
            // Возвращает время в часах
            public uint GetHours { get { return Total40ms / 90000; } }
            // Возвращает время в минутах
            public uint GetMinutes { get { return (Total40ms % 90000) / 1500; } }
            // Возвращает время в секундах с округлением.
            public uint GetSeconds { get { return (uint)Math.Round((decimal)((Total40ms % 1500) / 25),
                                     MidpointRounding.AwayFromZero); } }

            protected Log()
            {
                IsGood = false;
                Total40ms = 0;
                TotalOn = 0;
            }

            public abstract int GetData(byte[] raw);
        }
        // Класс для работы с записями отказов.
        public class LogError : Log
        {
            public static readonly int Size = 8;
            public static string Header
            {
                get
                {
                    return
              "Код ошибки;Имя;Описание ошибки\n" +
              "0x01;LOG_ERR_IP;Ошибка питания\n" +
              "0x02;LOG_ERR_CNTRL2_IS_0;мощн усилителя в нуле\n" +
              "0x03;LOG_ERR_CNTRL2_IS_1;мощн усилителя в единице\n" +
              "0x04;LOG_ERR_CNTRL3;превышение тока\n" +
              "0x05;LOG_ERR_TD;температурный датчик\n" +
              "0x06;LOG_ERR_FPGA;не прошел стартовый контроль\n" +
              "0x07;LOG_ERR_SRAM;не прошел стартовый контроль\n" +
              "0x08;LOG_ERR_FLASH;ошибка при записи/чтении\n" +
              "0x09;LOG_ERR_ARINC;\n" +
              "0x0a;LOG_ERR_N_RAY;номер луча\n" +
              "0x21;LOG_MEM_MISS_INF;потерян сигнал\n" +
              "0x22;LOG_MEM_START_INIT;вход в стартовый контроль\n" +
              "0x23;LOG_MEM_INH_TEST;вход в расширенный контроль\n" +
              "0x24;LOG_MEM_SILENCE;выключение СВЧМ\n";
                }
            }
            public byte Status { get; private set; }
            public byte Error { get; private set; }

            public LogError() : base()
            {
                Status = 0;
                Error = 0;
            }

            public LogError(byte[] raw) : this() => GetData(raw);

            public override int GetData(byte[] raw)
            {
                if (raw.Length != Size)
                    return 1;
                Raw = raw;
                for (var i = 0; i < Raw.Length; i++)
                {
                    if (i < 4)
                        Total40ms |= ((uint)Raw[i]) << (8 * i);
                    else if (i == 4)
                        TotalOn = Raw[i];
                    else if (i == 5)
                        Status |= Raw[i];
                    else if (i == 6)
                        Error = Raw[i];
                    else
                        CS = Raw[i];
                }
                byte testCs = 0;
                foreach (var i in Raw)
                    testCs += i;    // тут складывается и CS
                if (testCs == 0xFF) // поэтому проверяем на 0xFF
                    IsGood = true;

                return 0;
            }

            public override string ToString()
            {
                if (this != null)
                {
                    if (IsGood)
                        return string.Format("{0}:{1, 2:D2}:{2, 2:D2};{3};0b{4};0x{5,2:X2}",
                            GetHours, GetMinutes, GetSeconds, TotalOn,
                            Convert.ToString(Status, 2).PadLeft(8, '0'), Error);
                    else
                        return string.Format("{0}:{1, 2:D2}:{2, 2:D2};{3};0b{4};0x{5,2:X2};{6}",
                            GetHours, GetMinutes, GetSeconds, TotalOn,
                            Convert.ToString(Status, 2).PadLeft(8, '0'), Error, "Bad");
                }
                else
                    return "null";
            }
        }
        // Класс для работы с записями времени наработки.
        public class LogTime : Log
        {
            public static readonly int Size = 6;

            public LogTime() : base() { }

            public LogTime(byte[] raw) : this() => GetData(raw);

            public LogTime(uint total40ms) : base()
            {
                Total40ms = total40ms;
            }

            public override int GetData(byte[] raw)
            {
                if (raw.Length != Size)
                    return 1;
                Raw = raw;
                for (var i = 0; i < Raw.Length; i++)
                {
                    if (i < 4)
                        Total40ms |= ((uint)Raw[i]) << (8 * i);
                    else if (i == 4)
                        TotalOn = Raw[i];
                    else
                        CS = Raw[i];
                }
                byte testCs = 0;
                foreach (var i in Raw)
                    testCs += i;
                if (testCs == 0xFF)
                    IsGood = true;

                return 0;
            }

            public override string ToString()
            {
                if (this != null)
                    return string.Format("{0}:{1, 2:D2}:{2, 2:D2}.{3, 3:D3}",
                        GetHours, GetMinutes, GetSeconds, (Total40ms % 25) * 40);
                else
                    return "null";
            }
        }
        // Размер flash памяти.
        public static readonly int SizeFile = 8192;
        // Количество запяси отказов.
        public static readonly int CountLogError = 1000;
        // Размер занимаемый журналом отказа. Следом идет журнал наработок.
        public static readonly int SizeAllLogError = CountLogError * LogError.Size;
        // Количество записей времени наработки.
        public static readonly int CountLogTime = 10;
        // Размер занимаемый журнала времени наработки.
        public static readonly int SizeAllLogTime = CountLogTime * LogTime.Size;
        // Может быть использована ф-цией WriteToCsv для выходного файла.
        // Имя задается без формата. По умолчанию LogErrors.
        public string Name { get; protected set; }
        // Список всех записей журнала отказа.
        public List<LogError> Errors { get; protected set; }
        // Список всех записей времени наработки.
        public List<LogTime> Times { get; protected set; }
        // Счетчик пустых записей. 
        public uint CountEmpty { get; protected set; }
        // Возвращает максимальную запись времени наработки.
        public LogTime GetMaxTimes()
        {
            if (Times == null || Times.Count == 0)
                return new LogTime();
            LogTime logMax = Times[0];
            foreach (var t in Times)
                if (t.Total40ms > logMax.Total40ms)
                    logMax = t;
            return logMax;
        }
        // Конструктор, принимает полное имя файла, для парсинга.
        public DissLogs(string fullFineName)
        {
            CountEmpty = 0;
            try
            {
                var f = File.OpenRead(fullFineName);
                if (f.Length != SizeFile)
                {
                    MessageBox.Show("Размер файла не совпадает.\n"
                        + "файл: " + fullFineName + ": " + f.Length + " byte\n"
                        + "Должно быть: " + SizeFile + " byte");
                    return;
                }
                using (var fstream = new BinaryReader(f))
                {
                    Name = fullFineName.Substring(0, fullFineName.Length - 4);
                    byte[] binArray = new byte[SizeFile];
                    fstream.Read(binArray, 0, binArray.Length);

                    Errors = new List<LogError>();
                    for (var i = 0; i < CountLogError; i++)
                    {
                        LogError t = new LogError(binArray
                                                        .Skip(i * LogError.Size)
                                                        .Take(LogError.Size)
                                                        .ToArray());
                        if (t.IsEmpty)
                            CountEmpty++;
                        else
                            Errors.Add(t);
                    }
                    Times = new List<LogTime>();
                    for (var i = 0; i < CountLogTime; i++)
                    {
                        LogTime t = new LogTime(binArray
                                                    .Skip(SizeAllLogError + i * LogTime.Size)
                                                    .Take(LogTime.Size)
                                                    .ToArray());
                        if (t.IsGood)
                            Times.Add(t);
                    }
                }
                Errors = Errors.OrderBy(o => o.Total40ms).ToList();
                Times = Times.OrderBy(o => o.Total40ms).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        // Конструктор, принимает массив byte и имя без формата. Имя может
        // быть использована в ф-ции WriteToCsv.
        public DissLogs(byte[] binArray, string fileOutName = "LogErrors")
        {
            try
            {
                if (binArray.Length != SizeFile)
                {
                    MessageBox.Show("Размер массива не совпадает.\n"
                                + "Для массива: " + binArray.Length + "\n"
                                + "Должна быть: " + SizeFile + "");
                    return;
                }
                Name = fileOutName;
                Errors = new List<LogError>();
                for (var i = 0; i < CountLogError; i++)
                {
                    LogError t = new LogError(binArray
                                                    .Skip(i * LogError.Size)
                                                    .Take(LogError.Size)
                                                    .ToArray());
                    if (t.IsEmpty)
                        CountEmpty++;
                    else
                        Errors.Add(t);
                }
                Times = new List<LogTime>();
                for (var i = 0; i < CountLogTime; i++)
                {
                    LogTime t = new LogTime(binArray
                                                .Skip(SizeAllLogError + i * LogTime.Size)
                                                .Take(LogTime.Size)
                                                .ToArray());
                    if (t.IsGood)
                        Times.Add(t);
                }
                Errors = Errors.OrderBy(o => o.Total40ms).ToList();
                Times = Times.OrderBy(o => o.Total40ms).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
    // Статический класс для работы с DissLogs.
    public static class WorkWithLogs
    {
        private static String FormatHeader =
            "Дата и время чтение журнала: {0:hh:mm:ss} {0:dd}/{0:MM}/{0:yyyy}\n" +
            "Версия ПО для ПК: {1:hh:mm:ss} {1:dd}/{1:MM}/{1:yyyy}\n" +
            "Версия ПО для МПР: {2}\n" +
            "Версия ПО для ПЛИС: {3:hh:mm:ss} {3:dd}/{3:MM}/{3:yyyy}\n" +
            "Время наработки МПР: {4}, кол-во включений: {5}\n" +
            "{6}\n\n" +
            "Всего прочитано записей: {7}, корректных {8}, свободных {9}, ошибочных {10}\n" +
            "LogError\nNum;Time;CountOn;Status;Error;CS\n";
        // Ф-ция для записи DissLogs в файл csv.
        // DissLogs logs - класс, где храняться записи.
        // string fullFileNameOut - полное имя выходного файла.
        // uint buildFpga - версия прошивки ПЛИС в кодированном виде.
        // DateTime buildPc - версия программы для ПК.
        // string buildMc - версия прошивки МПР/
        public static int WriteToCsv(DissLogs logs, string fullFileNameOut, uint buildFpga = 0x01234567,
                                    DateTime buildPc = new DateTime(), string buildMc = "0")
        {
            try
            {
                using (var fstream = new FileStream(fullFileNameOut, FileMode.Create))
                {
                    {
                        byte[] array = Encoding.Default.GetBytes(string.Format(FormatHeader,
                            DateTime.Now, buildPc, buildMc, fpga_version(buildFpga),
                            logs.GetMaxTimes().ToString(), logs.GetMaxTimes().TotalOn,
                            DissLogs.LogError.Header, DissLogs.CountLogError,
                            DissLogs.CountLogError - (logs.CountEmpty + logs.Errors.Count(e => !e.IsGood)),
                            logs.CountEmpty, logs.Errors.Count(e => !e.IsGood)));
                        fstream.Write(array, 0, array.Length);
                    }
                    //foreach (var er in logs.Errors)
                    for (int i = 0; i < logs.Errors.Count; i++)
                    {
                        byte[] array = Encoding.Default.GetBytes((i + 1) + ";" + logs.Errors[i].ToString() + "\n");
                        fstream.Write(array, 0, array.Length);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return 1;
            }
            return 0;
        }
        // Ф-ция для записи DissLogs в файл csv.
        // В отличии от предыдущей ф-ции берет имя файла из logs.Name.
        public static int WriteToCsv(DissLogs logs, uint buildFpga = 0x01234567,
                    DateTime buildPc = new DateTime(), string buildMc = "0")
        {
            return WriteToCsv(logs, logs.Name + ".csv", buildFpga, buildPc, buildMc);
        }
        // Ф-ция перевода кодированной версии ПЛИС в DateTime.
        public static DateTime fpga_version(uint id)
        {
            uint true_id;
            ulong true_id_full;
            string fpga_ver_str;
            true_id = ((id >> 31) & 0x1) << 0 |
                        ((id >> 30) & 0x1) << 2 |
                        ((id >> 29) & 0x1) << 4 |
                        ((id >> 28) & 0x1) << 6 |
                        ((id >> 27) & 0x1) << 8 |
                        ((id >> 26) & 0x1) << 10 |
                        ((id >> 25) & 0x1) << 12 |
                        ((id >> 24) & 0x1) << 14 |
                        ((id >> 23) & 0x1) << 1 |
                        ((id >> 22) & 0x1) << 3 |
                        ((id >> 21) & 0x1) << 5 |
                        ((id >> 20) & 0x1) << 7 |
                        ((id >> 19) & 0x1) << 9 |
                        ((id >> 18) & 0x1) << 11 |
                        ((id >> 17) & 0x1) << 13 |
                        ((id >> 16) & 0x1) << 15 |
                        ((id >> 15) & 0x1) << 16 |
                        ((id >> 14) & 0x1) << 18 |
                        ((id >> 13) & 0x1) << 20 |
                        ((id >> 12) & 0x1) << 22 |
                        ((id >> 11) & 0x1) << 24 |
                        ((id >> 10) & 0x1) << 26 |
                        ((id >> 9) & 0x1) << 28 |
                        ((id >> 8) & 0x1) << 30 |
                        ((id >> 7) & 0x1) << 17 |
                        ((id >> 6) & 0x1) << 19 |
                        ((id >> 5) & 0x1) << 21 |
                        ((id >> 4) & 0x1) << 23 |
                        ((id >> 3) & 0x1) << 25 |
                        ((id >> 2) & 0x1) << 27 |
                        ((id >> 1) & 0x1) << 29 |
                        ((id >> 0) & 0x1) << 31;

            true_id_full = true_id | ((ulong)0x1D5 << 32);

            fpga_ver_str = true_id_full.ToString();

            int y, d, th, tm, ts;
            y = Convert.ToInt32(fpga_ver_str.Substring(0, 4));
            d = Convert.ToInt32(fpga_ver_str.Substring(4, 3));
            th = Convert.ToInt32(fpga_ver_str.Substring(7, 2));
            tm = Convert.ToInt32(fpga_ver_str.Substring(9, 2));
            ts = Convert.ToInt32(fpga_ver_str.Substring(11, 2));

            DateTime dateTime = new DateTime(y, 1, 1);
            dateTime = dateTime.AddDays(d - 1);
            dateTime = dateTime.AddHours(th);
            dateTime = dateTime.AddMinutes(tm);
            dateTime = dateTime.AddSeconds(ts);

            return dateTime;
        }
    }
}