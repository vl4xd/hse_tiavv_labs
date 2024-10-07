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

namespace PipesClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Int32 HandleMailSlot;   // дескриптор мэйлслота

        private int ClientHandleMailSlot;       // дескриптор мэйлслота клиента

        // процесс может создать канал только на той рабочей станции, где он запущен, поэтому при создании канала имя сервера никогда не указывается (.)
        //private string ClientPipeName = $"\\\\.\\pipe\\";
        private Thread t; // поток для обслуживания канала клиента
        private bool _connected = false; // флаг, указывающий, подключен ли клиент к серверу

        private string ClientName;

        // конструктор формы
        public MainWindow()
        {
            InitializeComponent();
            // запуск клиента
            ClientOn();
            this.Title += "     " + Dns.GetHostName();   // выводим имя текущей машины в заголовок формы
        }

        // присоединение к мэйлслоту

        private void ReceiveMessage()
        {
            string msg = "";
            uint realBytesReaded = 0; // количество реально прочитанных из канала байтов
            int MailslotSize = 0;       // максимальный размер сообщения
            int lpNextSize = 0;         // размер следующего сообщения
            int MessageCount = 0;       // количество сообщений в мэйлслоте

            // входим в бесконечный цикл работы с каналом
            while (this._connected)
            {
                if (DIS.Import.GetMailslotInfo(ClientHandleMailSlot, MailslotSize, ref lpNextSize, ref MessageCount, 0))
                {
                    // если есть сообщения в мэйлслоте, то обрабатываем каждое из них
                    if (MessageCount > 0)
                        for (int i = 0; i < MessageCount; i++)
                        {
                            byte[] buff = new byte[1024];                           // буфер прочитанных из мэйлслота байтов
                            DIS.Import.FlushFileBuffers(ClientHandleMailSlot);      // "принудительная" запись данных, расположенные в буфере операционной системы, в файл мэйлслота
                            DIS.Import.ReadFile(ClientHandleMailSlot, buff, 1024, ref realBytesReaded, 0);      // считываем последовательность байтов из мэйлслота в буфер buff
                            msg = Encoding.Unicode.GetString(buff, 0, (int)realBytesReaded);                 // выполняем преобразование байтов в последовательность символов

                            if (msg != "")
                            {
                                // создаем динамический объект и десериализуем json строку
                                dynamic json_msg = JsonSerializer.Deserialize<ExpandoObject>(msg);
                                //bool is_connection = Convert.ToBoolean(Convert.ToString(json_msg.is_connection)); // получаем статус сообщения
                                bool is_status_check = Convert.ToBoolean(Convert.ToString(json_msg.is_status_check));
                                string user_name = Convert.ToString(json_msg.user_name); // получаем имя пользователя
                                //string pc_name = Convert.ToString(json_msg.pc_name); // получаем имя машины
                                string user_message = Convert.ToString(json_msg.user_message); // получаем сообщение пользователя

                                try
                                {
                                    if (ClientName == user_name)
                                        user_name += " (Вы) ";

                                    all_messages.Dispatcher.Invoke((MethodInvoker)delegate
                                    {
                                        // msg != "" не выполняется
                                        if (msg != "" && realBytesReaded != 0)
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
                                Thread.Sleep(500);                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                            }

                            //CheckUsersForDelete();
                            Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                        }
                }
            }
        }

        private void ConnectToMail()
        {
            ClientName = this.user_name.Text;
            //this.ClientPipeName += this.user_name.Text;

            ClientHandleMailSlot = DIS.Import.CreateMailslot($"\\\\.\\mailslot\\{ClientName}",
                0,
                DIS.Types.MAILSLOT_WAIT_FOREVER,
                0);

            try
            {
                // открываем мэйлслот, имя которого указано в поле tbMailSlot
                HandleMailSlot = DIS.Import.CreateFile(server_pipe_name.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
                if (HandleMailSlot != -1)
                {
                    button_connect.IsEnabled = false;
                    button_send_message.IsEnabled = true;
                    this._connected = true;
                }
                else
                    MessageBox.Show("Не удалось подключиться к мейлслоту");
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к мейлслоту");
            }


            t = new Thread(ReceiveMessage);
            t.Start();

            //SendMessageToServer(isConnection:true);

            ElementsActivator();
        }

        private void DisconnecFromServer()
        {
            //this._connected = false; // сообщаем что работа с каналом клиента завершена
            //if (this.ClientPipeHandle != -1)
            //    DIS.Import.CloseHandle(ClientPipeHandle); // закрываем дескриптор канала клиента
            //if (t != null)
            //    this.t.Abort(); // завершаем поток клиента

            ElementsActivator();
        }

        private void ClientOn()
        {
            this.server_pipe_name.Text = "\\\\.\\mailslot\\ServerMailslot";

            ElementsActivator();
        }

        private void ClientOff()
        {
            DisconnecFromServer();

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
            DisconnecFromServer();
        }

        private void button_connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectToMail();
        }

        private void SendMessageToServer()
        {
            uint BytesWritten = 0;  // количество реально записанных в канал байт


            dynamic msg_object = new System.Dynamic.ExpandoObject();
            msg_object.user_name = this.user_name.Text;
            msg_object.pc_name = Dns.GetHostName().ToString();
            msg_object.user_message = this.user_message.Text;
            string msg_json = JsonSerializer.Serialize(msg_object);


            byte[] buff = Encoding.Unicode.GetBytes(msg_json);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            DIS.Import.WriteFile(HandleMailSlot, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);     // выполняем запись последовательности байт в мэйлслот
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
