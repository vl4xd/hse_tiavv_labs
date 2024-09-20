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

namespace PipesClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Int32 PipeHandle; // дескриптор канала

        private Int32 ClientPipeHandle; // дескриптор канала клиента

        // процесс может создать канал только на той рабочей станции, где он запущен, поэтому при создании канала имя сервера никогда не указывается (.)
        private string ClientPipeName = $"\\\\.\\pipe\\";
        private Thread t; // поток для обслуживания канала клиента
        private bool _connected = false; // флаг, указывающий, подключен ли клиент к серверу

        private string ClientName;

        // конструктор формы
        public MainWindow()
        {
            InitializeComponent();
            // запуск клиента
            ClientOn();
        }

        private void ReceiveMessage()
        {
            string msg = "";
            uint realBytesReaded = 0; // количество реально прочитанных из канала байтов

            // входим в бесконечный цикл работы с каналом
            while (this._connected)
            {
                if (DIS.Import.ConnectNamedPipe(ClientPipeHandle, 0))
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(ClientPipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(ClientPipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff
                    msg = Encoding.Unicode.GetString(buff, 0, (int)realBytesReaded);                                 // выполняем преобразование байтов в последовательность символов

                    // создаем динамический объект и десериализуем json строку
                    dynamic json_msg = JsonSerializer.Deserialize<ExpandoObject>(msg);
                    string user_name = Convert.ToString(json_msg.user_name); // получаем имя пользователя
                    string user_message = Convert.ToString(json_msg.user_message); // получаем сообщение пользователя

                    if (ClientName == user_name)
                        user_name += " (Вы) ";

                    all_messages.Dispatcher.Invoke((MethodInvoker)delegate
                    {
                        // msg != "" не выполняется
                        if (msg != "" && realBytesReaded != 0)
                        {
                            this.all_messages.Items.Add($">> {user_name} : {user_message}");                   // выводим полученное сообщение на форму
                        }
                    });

                    DIS.Import.DisconnectNamedPipe(ClientPipeHandle);                             // отключаемся от канала клиента 
                    Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                }
            }
        }

        private void ConnectToServer()
        {
            this._connected = true;
            this.ClientPipeName += this.user_name.Text;

            int res = this.ClientPipeHandle = DIS.Import.CreateNamedPipe(
                ClientPipeName,
                DIS.Types.PIPE_ACCESS_DUPLEX,
                DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT,
                DIS.Types.PIPE_UNLIMITED_INSTANCES,
                0,
                1024,
                DIS.Types.NMPWAIT_WAIT_FOREVER,
                (uint)0
                );

            t = new Thread(ReceiveMessage);
            t.Start();

            SendMessageToServer(isConnection:true);

            ElementsActivator();
        }

        private void DisconnecFromServer()
        {
            this._connected = false; // сообщаем что работа с каналом клиента завершена
            if (this.ClientPipeHandle != -1)
                DIS.Import.CloseHandle(ClientPipeHandle); // закрываем дескриптор канала клиента
            if (t != null)
                this.t.Abort(); // завершаем поток клиента

            ElementsActivator();
        }

        private void ClientOn()
        {
            this.server_pipe_name.Text = "\\\\.\\pipe\\ServerPipe";

            ElementsActivator();
        }

        private void ClientOff()
        {
            DisconnecFromServer();

            ElementsActivator();
        }

        private void button_send_message_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToServer(isConnection:false);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClientOff();
        }

        private void button_disconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnecFromServer();
        }

        private void button_connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectToServer();
        }

        private void SendMessageToServer(bool isConnection)
        {
            uint BytesWritten = 0;  // количество реально записанных в канал байт


            dynamic msg_object = new System.Dynamic.ExpandoObject();
            msg_object.is_connection = isConnection.ToString();
            msg_object.user_name = this.user_name.Text;
            ClientName = msg_object.user_name;
            msg_object.pc_name = Dns.GetHostName().ToString();
            msg_object.user_message = this.user_message.Text;
            string msg_json = JsonSerializer.Serialize(msg_object);

            byte[] buff = Encoding.Unicode.GetBytes(msg_json);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт


            // открываем именованный канал, имя которого указано в поле server_pipe_name
            PipeHandle = DIS.Import.CreateFile(server_pipe_name.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandle);
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
