using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading.Channels;

namespace ConsoleApplication1
{
    class Producer
    {
        private ChannelWriter<string> Writer;

        public Producer(ChannelWriter<string> _writer)
        {
            Writer = _writer;
            Task.WaitAll(Run());
        }

        private async Task Run()
        {
//ожидает, когда освободиться место для записи элемента.
            while (await Writer.WaitToWriteAsync())
            {
                char[] word = new char[5];
                for (int i = 97; i < 123; i++)
                {
                    word[0] = (char) i;
                    for (int k = 97; k < 123; k++)
                    {
                        word[1] = (char) k;
                        for (int l = 97; l < 123; l++)
                        {
                            word[2] = (char) l;
                            for (int m = 97; m < 123; m++)
                            {
                                word[3] = (char) m;
                                for (int n = 97; n < 123; n++)
                                {
                                    word[4] = (char) n;
                                    if (!Program.foundFlag)
                                    {
                                        await Writer.WriteAsync(new string(word));
                                    }
                                    else
                                    {
                                        Writer.Complete();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    class Consumer
    {
        private ChannelReader<string> Reader;
        private string PasswordHash;

        public Consumer(ChannelReader<string> _reader, string _passwordHash)
        {
            Reader = _reader;
            PasswordHash = _passwordHash;
            Task.WaitAll(Run());
        }

        private async Task Run()
        {
// ожидает, когда освободиться место для чтения элемента.
            while (await Reader.WaitToReadAsync())
            {
                if (!Program.foundFlag)
                {
                    var item = await Reader.ReadAsync();
                    if (FoundHash(item.ToString()) == PasswordHash)
                    {
                        Console.WriteLine($"\tПароль подобран - {item}");
                        Program.foundFlag = true;
                    }
                }
                else return;
            }
        }
        /// Находит хеш str
        static public string FoundHash(string str)
        {
            SHA256 sha256Hash = SHA256.Create();
//Из строки в байтовый массив   
            byte[] sourceBytes = Encoding.ASCII.GetBytes(str);
            byte[] hashBytes = sha256Hash.ComputeHash(sourceBytes);
            string hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty);
            return hash;
        }
    }

    class Program
    {
        static public bool foundFlag = false;

        static void printMenu()
        {
            bool flag = true;
            while (flag)
            {
                Console.WriteLine("1. Выполнить задание.");
                Console.WriteLine("2. Очистить консоль.");
                Console.WriteLine("3. Выйти из программы.");
                Console.Write("Выберите пункт меню: ");
                int choice = int.Parse(Console.ReadLine());
                switch (choice)
                {
                    case 1:
                        Console.WriteLine("\tВыберите по какому хеш значению SHA-256 подобрать пароль: ");
                        var passwordHash=Console.ReadLine().ToUpper();
                        Console.Write("\tВведите количество потоков: ");
                        int countStream = int.Parse(Console.ReadLine());
                        Console.WriteLine("\tОжидайте подбор пароля...");

//создаю общий канал данных
                        Channel<string> channel = Channel.CreateBounded<string>(countStream);
                        Stopwatch time = new Stopwatch();
                        time.Reset();
                        time.Start();
//создается производитель
                        var prod = Task.Run(() => { new Producer(channel.Writer); });
                        Task[] streams = new Task[countStream + 1];
                        streams[0] = prod;
//создаются потребители
                        for (int i = 1; i < countStream + 1; i++)
                        {
                            streams[i] = Task.Run(() => { new Consumer(channel.Reader, passwordHash); });
                        }

//Ожидает завершения выполнения всех указанных объектов Task
                        Task.WaitAny(streams);
                        time.Stop();
                        Console.WriteLine($"\tЗатраченное время на подбор: {time.Elapsed}");
                        Console.WriteLine("\tВведите ENTER, чтобы выйти в главное меню.");
                        Console.WriteLine();
                        Console.ReadKey();
                        foundFlag = false;
                        break;
                    case 2:
                        Console.Clear();
                        break;
                    case 3:
                        flag = false;
                        break;
                    default:
                        Console.WriteLine("\tВыбранного пункта нет в меню.");
                        break;
                }
            }
        }

        static public void Main()
        {
            printMenu();
        }
    }
}