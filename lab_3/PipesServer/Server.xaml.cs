using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Collections;
using System.Text.Json;
using System.Dynamic;
using System.Linq.Expressions;
using System.Windows.Media.Animation;
using Microsoft.Win32.SafeHandles;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;

namespace PipesServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread t; // поток для обслуживания канала
        private bool _continue = true; // флаг, указывающий продолжается ли работа с каналом

        private Hashtable connected_clients = new Hashtable(); // хэш-таблица {имя_пользователя, имя_машины}
        private Hashtable listbox_connected_clients = new Hashtable(); // хэш-таблица {имя_пользователя, ListBox.Item.index}

        private Socket ClientSock;                      // клиентский сокет
        private TcpListener Listener;                   // сокет сервера
        private List<Thread> Threads = new List<Thread>();      // список потоков приложения (кроме родительского)

        private Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>(); // словарь для хранения подключенных клиентов

        private UdpClient udpListener; // UDP-сокет для прослушивания широковещательных запросов

        // конструктор формы сервера
        public MainWindow()
        {
            InitializeComponent();
            // включение сервера
            ServerOn();
        }

        private void ServerOn()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());    // информация об IP-адресах и имени машины, на которой запущено приложение
            IPAddress IP = hostEntry.AddressList[0];                        // IP-адрес, который будет указан при создании сокета
            int Port = 1010;                                                // порт, который будет указан при создании сокета

            // определяем IP-адрес машины в формате IPv4
            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }

            // вывод IP-адреса машины и номера порта в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Title += "     " + IP.ToString() + "  :  " + Port.ToString();

            // создаем серверный сокет (Listener для приема заявок от клиентских сокетов)
            Listener = new TcpListener(IP, Port);
            Listener.Start();

            // создаем и запускаем поток, выполняющий обслуживание серверного сокета
            Threads.Clear();
            Threads.Add(new Thread(ReceiveMessage));
            Threads[Threads.Count - 1].Start();

            udpListener = new UdpClient(1011); // порт для прослушивания широковещательных запросов
            Thread udpThread = new Thread(ListenForBroadcast);
            udpThread.Start();
        }

        private void ListenForBroadcast()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (_continue)
            {
                try
                {
                    byte[] receiveBytes = udpListener.Receive(ref remoteEndPoint);
                    string receivedMessage = Encoding.ASCII.GetString(receiveBytes);

                    if (receivedMessage == "DISCOVER_SERVER")
                    {
                        byte[] sendBytes = Encoding.ASCII.GetBytes("SERVER_RESPONSE");
                        udpListener.Send(sendBytes, sendBytes.Length, remoteEndPoint);
                    }
                }
                catch (Exception)
                {
                    // Обработка ошибок
                }
            }
        }


        private void ServerOff()
        {
            _continue = false;      // сообщаем, что работа с сокетами завершена

            // завершаем все потоки
            foreach (Thread t in Threads)
            {
                t.Abort();
                t.Join(500);
            }

            // закрываем клиентский сокет
            if (ClientSock != null)
                ClientSock.Close();

            // приостанавливаем "прослушивание" серверного сокета
            if (Listener != null)
                Listener.Stop();
        }

        private void ReceiveMessage()
        {
            // входим в бесконечный цикл работы с каналом
            while (this._continue)
            {
                ClientSock = Listener.AcceptSocket();           // получаем ссылку на очередной клиентский сокет
                TcpClient client = new TcpClient();
                client.Client = ClientSock;

                Threads.Add(new Thread(ReadMessages));          // создаем и запускаем поток, обслуживающий конкретный клиентский сокет
                Threads[Threads.Count - 1].Start(client);
            }
        }

        // получение сообщений от конкретного клиента
        private void ReadMessages(object clientObj)
        {
            string msg = "";        // полученное сообщение
            TcpClient client = (TcpClient)clientObj;

            // входим в бесконечный цикл для работы с клиентским сокетом
            while (_continue)
            {
                try
                {
                    byte[] buff = new byte[1024];                           // буфер прочитанных из сокета байтов
                    NetworkStream stream = client.GetStream();
                    int bytesRead = stream.Read(buff, 0, buff.Length);
                    msg = Encoding.Unicode.GetString(buff, 0, bytesRead);     // выполняем преобразование байтов в последовательность символов
                    //msg = msg.Substring(0, msg.IndexOf("\0"));

                    // создаем динамический объект и десериализуем json строку
                    dynamic json_msg = JsonSerializer.Deserialize<ExpandoObject>(msg);
                    string user_name = Convert.ToString(json_msg.user_name); // получаем имя пользователя
                    string pc_name = Convert.ToString(json_msg.pc_name); // получаем имя машины
                    string user_message = Convert.ToString(json_msg.user_message); // получаем сообщение пользователя

                    try
                    {
                        if (!connected_clients.ContainsKey(user_name))
                        {
                            connected_clients.Add(user_name, pc_name);
                            connectedClients.Add(user_name, client);
                            connected_users.Dispatcher.Invoke((MethodInvoker)delegate
                            {
                                // добавляем нового клиента в ListBox приложения
                                ListBoxItem new_client = new ListBoxItem();
                                new_client.Content = user_name;
                                int new_client_id = this.connected_users.Items.Add(new_client);
                                // добавляем клиента в хэш-таблицу для удаления быстрого удаления из ListBox
                                this.listbox_connected_clients.Add(user_name, new_client);
                            });
                        }
                    }
                    catch (Exception)
                    {
                        //
                    }

                    SendMessageToClients(user_name, user_message);

                    user_messages.Dispatcher.Invoke((MethodInvoker)delegate
                    {
                        this.user_messages.Items.Add($">> {user_name} : {user_message}"); // выводим полученное сообщение на форму
                    });

                    Thread.Sleep(300);
                }
                catch (Exception)
                {
                    // Обработка ошибок соединения
                }
            }
        }

        private void SendMessageToClients(string user_name, string user_message)
        {
            bool isStatusCheck = false;

            dynamic msg_object = new System.Dynamic.ExpandoObject();
            msg_object.is_status_check = isStatusCheck.ToString();
            msg_object.user_name = user_name;
            msg_object.user_message = user_message;
            // Для кириллицы
            var options1 = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            string msg_json = JsonSerializer.Serialize(msg_object, options1);
            byte[] buff = Encoding.Unicode.GetBytes(msg_json);

            List<string> connected_clients_keys_for_delete = new List<string>();

            ICollection connected_clients_keys = connected_clients.Keys;
            foreach (string _user_name in connected_clients_keys)
            {
                if (connectedClients.ContainsKey(_user_name))
                {
                    TcpClient client = connectedClients[_user_name];
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buff, 0, buff.Length);
                    }
                    catch (Exception)
                    {
                        connected_clients_keys_for_delete.Add(_user_name);
                    }
                }
            }

            foreach (string _user_name in connected_clients_keys_for_delete)
            {
                connected_clients.Remove(_user_name);
                connected_users.Dispatcher.Invoke((MethodInvoker)delegate
                {
                    this.connected_users.Items.Remove(listbox_connected_clients[_user_name]);
                });
                listbox_connected_clients.Remove(_user_name);
            }
        }

        private void CheckUsersForDelete()
        {
            bool isStatusCheck = true;

            dynamic msg_object = new System.Dynamic.ExpandoObject();
            msg_object.is_status_check = isStatusCheck.ToString();
            msg_object.user_name = "";
            msg_object.user_message = "";
            // Для кириллицы
            var options1 = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            string msg_json = JsonSerializer.Serialize(msg_object, options1);
            byte[] buff = Encoding.Unicode.GetBytes(msg_json);

            List<string> connected_clients_keys_for_delete = new List<string>();

            ICollection connected_clients_keys = connected_clients.Keys;
            foreach (string _user_name in connected_clients_keys)
            {
                if (connectedClients.ContainsKey(_user_name))
                {
                    TcpClient client = connectedClients[_user_name];
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buff, 0, buff.Length);
                    }
                    catch (Exception)
                    {
                        connected_users.Dispatcher.Invoke((MethodInvoker)delegate
                        {
                            this.connected_users.Items.Remove(listbox_connected_clients[_user_name]);
                        });
                        listbox_connected_clients.Remove(_user_name);
                        connected_clients_keys_for_delete.Add(_user_name);
                    }
                }
            }

            foreach (string _user_name in connected_clients_keys_for_delete)
            {
                connected_clients.Remove(_user_name);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ServerOff();
        }
    }
}
