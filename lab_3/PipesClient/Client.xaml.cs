using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
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
using System.Threading;
using System.Windows.Media.Animation;
using System.Windows.Forms;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Interop;
using System.Text.Json;
using System.Dynamic;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Net.Sockets;
using System.IO;

namespace PipesClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread t; // поток для обслуживания канала клиента
        private bool _connected = false; // флаг, указывающий, подключен ли клиент к серверу

        private string ClientName;

        private TcpClient Client = new TcpClient();     // клиентский сокет
        private IPAddress IP;                           // IP-адрес клиента

        private UdpClient udpClient; // UDP-сокет для отправки широковещательных запросов

        // конструктор формы
        public MainWindow()
        {
            InitializeComponent();
            // запуск клиента
            ClientOn();
            this.Title += "     " + Dns.GetHostName();   // выводим имя текущей машины в заголовок формы

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());    // информация об IP-адресах и имени машины, на которой запущено приложение
            IP = hostEntry.AddressList[0];                                  // IP-адрес, который будет указан в заголовке окна для идентификации клиента

            // определяем IP-адрес машины в формате IPv4
            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }
        }

        // присоединение к сокету
        private void ReceiveMessage()
        {
            string msg = "";
            byte[] buff = new byte[1024];

            // входим в бесконечный цикл работы с каналом
            while (this._connected)
            {
                try
                {
                    NetworkStream stream = Client.GetStream();
                    int bytesRead = stream.Read(buff, 0, buff.Length);
                    msg = Encoding.Unicode.GetString(buff, 0, bytesRead);

                    if (msg != "")
                    {
                        // создаем динамический объект и десериализуем json строку
                        dynamic json_msg = JsonSerializer.Deserialize<ExpandoObject>(msg);
                        bool is_status_check = Convert.ToBoolean(Convert.ToString(json_msg.is_status_check));
                        string user_name = Convert.ToString(json_msg.user_name); // получаем имя пользователя
                        string user_message = Convert.ToString(json_msg.user_message); // получаем сообщение пользователя

                        try
                        {
                            if (ClientName == user_name)
                                user_name += " (Вы) ";

                            all_messages.Dispatcher.Invoke((MethodInvoker)delegate
                            {
                                // msg != "" не выполняется
                                if (msg != "" && bytesRead != 0)
                                {
                                    if (!is_status_check)
                                        this.all_messages.Items.Add($">> {user_name} : {user_message}");                   // выводим полученное сообщение на форму
                                }
                            });
                        }
                        catch (Exception)
                        {
                            //
                        }
                    }
                }
                catch (Exception)
                {
                    // Обработка ошибок соединения
                }

                Thread.Sleep(500);                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
            }
        }

        private void ConnectToSocket()
        {
            ClientName = this.user_name.Text;

            try
            {
                // Отправляем широковещательный запрос для поиска сервера
                udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;
                byte[] sendBytes = Encoding.ASCII.GetBytes("DISCOVER_SERVER");
                IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 1011);
                udpClient.Send(sendBytes, sendBytes.Length, broadcastEndPoint);

                // Ожидаем ответа от сервера
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);
                string receivedMessage = Encoding.ASCII.GetString(receiveBytes);

                if (receivedMessage == "SERVER_RESPONSE")
                {
                    IPAddress serverIP = remoteEndPoint.Address;
                    int serverPort = 1010;

                    // Подключаемся к серверу
                    Client.Connect(serverIP, serverPort);
                    button_connect.IsEnabled = false;
                    button_send_message.IsEnabled = true;
                    this._connected = true;

                    // Запускаем поток для получения сообщений
                    t = new Thread(ReceiveMessage);
                    t.Start();
                }
                else
                {
                    MessageBox.Show("Сервер не найден");
                }
            }
            catch
            {
                MessageBox.Show("Введен некорректный IP-адрес");
            }

            ElementsActivator();
        }

        private void ClientOn()
        {
            this.server_pipe_name.Text = "ШИРОКОВЕЩАТЕЛЬНЫЙ ЗАПРОС!";

            ElementsActivator();
        }

        private void ClientOff()
        {
            Client.Close();
            this._connected = false;
            t.Abort();

            ElementsActivator();
        }

        private void button_send_message_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToServer();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClientOff();
        }

        private void button_disconnect_Click(object sender, RoutedEventArgs e)
        {
            ClientOff();
        }

        private void button_connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectToSocket();
        }

        private void SendMessageToServer()
        {
            dynamic msg_object = new System.Dynamic.ExpandoObject();
            msg_object.user_name = this.user_name.Text;
            msg_object.pc_name = Dns.GetHostName().ToString();
            msg_object.user_message = this.user_message.Text;
            string msg_json = JsonSerializer.Serialize(msg_object);

            byte[] buff = Encoding.Unicode.GetBytes(msg_json);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            try
            {
                NetworkStream stm = Client.GetStream();                                                    // получаем файловый поток клиентского сокета
                stm.Write(buff, 0, buff.Length);                                                    // выполняем запись последовательности байт
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка отправки сообщения");
            }
        }

        private void ElementsActivator()
        {
            // включаем, отключаем элементы GUI в зависимости от статуса _connected
            this.server_pipe_name.IsEnabled = !this._connected;
            this.user_name.IsEnabled = !this._connected;
            this.button_send_message.IsEnabled = _connected;
            this.button_connect.IsEnabled = !this._connected;
            this.button_disconnect.IsEnabled = this._connected;
        }
    }
}
