using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace UdpSample
{
    class Chat
    {
        private static IPAddress remoteIPAddress;
        private static int remotePort;
        private static int localPort;

        public static int Fl_write_to_file { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Размер пакета - 4 байт");
            ConsoleKeyInfo key;
            string rule = @"[0-9a-fA-F]";
            List<string> mass_list = new List<string> { };
            int i_mass;
            try
            {
                localPort = 2054;
                remotePort = 2054;
                
                remoteIPAddress = IPAddress.Parse("192.168.1.163");
                
                // Создаем поток для прослушивания
                Thread tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();
                while (true)
                {
                    string readline = "";
                    i_mass = 0;
                    while (true)
                    {
                        key = Console.ReadKey(true);
                        if (Regex.IsMatch(key.KeyChar.ToString(), rule))// && readline.Length < 8)
                        {
                            readline += key.KeyChar;
                            Console.Write(key.KeyChar);
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            break;
                        }
                        else if (key.Key == ConsoleKey.UpArrow)
                        {
                            if (i_mass < mass_list.Count)
                            {
                                i_mass++;
                            }
                            int cursorCol = Console.CursorLeft;
                            Console.CursorLeft = 0;
                            Console.Write(new string(' ', cursorCol));
                            Console.CursorLeft = 0;
                            if(mass_list.Count != 0)
                            {
                                Console.Write(mass_list[mass_list.Count - i_mass]);
                                readline = mass_list[mass_list.Count - i_mass];
                            }
                        }
                        else if (key.Key == ConsoleKey.DownArrow)
                        {
                            if (i_mass > 0)
                            {
                                i_mass--;
                                int cursorCol = Console.CursorLeft;
                                Console.CursorLeft = 0;
                                Console.Write(new string(' ', cursorCol));
                                Console.CursorLeft = 0;
                                readline = "";
                                if (mass_list.Count != 0 && i_mass != 0)
                                {
                                    Console.Write(mass_list[mass_list.Count - i_mass]);
                                    readline = mass_list[mass_list.Count - i_mass];
                                }
                            }
                        }
                        else if (key.Key == ConsoleKey.Backspace)
                        {
                            if (readline.Length >= 1)
                            {
                                int cursorCol = Console.CursorLeft - 1;
                                int oldLength = readline.Length;
                                int extraRows = oldLength / 80;

                                readline = readline.Substring(0, oldLength - 1);
                                Console.CursorLeft = 0;
                                Console.CursorTop = Console.CursorTop - extraRows;
                                Console.Write(readline + new String(' ', oldLength - readline.Length));
                                Console.CursorLeft = cursorCol;
                            }
                            continue;
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            Console.Clear();
                        }
                        else if (key.Key == ConsoleKey.Spacebar)
                        {
                            readline += '0';
                            Console.Write('0');
                        }
                    }
                    mass_list.Add(readline);
                    //byte[] read_byte = new byte[4];
                    byte[] read_byte = new byte[readline.Length / 2 + readline.Length % 2];
                    try
                    {
                        for (int i = 0; i < readline.Length; i += 2)
                            read_byte[i / 2 + i % 2] = byte.Parse(readline.Substring(i, ((i + 1 == readline.Length) ? 1 : 2)), System.Globalization.NumberStyles.HexNumber);
                        if (read_byte[0] == 0x12)
                        {
                            Fl_write_to_file++;
                        }
                        else
                        {
                            Fl_write_to_file = 0;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }

                    Console.CursorLeft = 0;
                    for (int i = 0; i < read_byte.Length; i++)
                    {
                        Console.Write("0x{0:X2} ", read_byte[i]);
                    }
                    Console.WriteLine();
                    Send(read_byte);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }

        private static void Send4(byte[] datagram)
        {
            UdpClient sender = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);
            try
            {
                sender.Send(datagram, 4, endPoint);
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

        public static void Receiver()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;
            try
            {
                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\n\tMDR:\t");
                    foreach (byte i in receiveBytes)
                        Console.Write("0x{0,2:X2} ", i);
                    Console.WriteLine();
                    Console.ResetColor(); // сбрасываем в стандартный
                    if(Fl_write_to_file > 0)
                    {
                        // запись в файл
                        using (FileStream fstream = new FileStream(@"..\journal.hex", FileMode.OpenOrCreate))
                        {
                            // запись массива байтов в файл
                            fstream.Write(receiveBytes, 0, receiveBytes.Length);
                            Console.WriteLine("Журнал записан в файл: {0}", fstream.Name);
                        }
                        // запись в файл
                        //using (FileStream fstream = new FileStream(@"..\journal.csv", FileMode.OpenOrCreate))
                        //{
                        //    // запись массива байтов в файл
                        //    fstream.Write(BitConverter.ToString(receiveBytes), 0, receiveBytes.Length);
                        //    Console.WriteLine("Текст записан в файл");
                        //}
                        Fl_write_to_file = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
    }
}