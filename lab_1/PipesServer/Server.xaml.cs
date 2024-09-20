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

namespace PipesServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Int32 PipeHandle; // дескриптор канала
        private string PipeName = "\\\\" + Dns.GetHostName() + "\\pipe\\ServerPipe"; // имя канала, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t; // поток для обслуживания канала
        private bool _continue = true; // флаг, указывающий продолжается ли работа с каналом

        private Int32 ClientPipeHandle; // дескриптор канала клиента
        private Hashtable connected_clients = new Hashtable(); // хэш-таблица {имя_пользователя, имя_машины}
        private Hashtable listbox_connected_clients = new Hashtable(); // хэш-таблица {имя_пользователя, ListBox.Item.index}

        // конструктор формы сервера
        public MainWindow()
        {
            InitializeComponent();
            // включение сервера
            ServerOn();
        }

        private void ServerOn()
        {
            // создание именованного канала
            PipeHandle = DIS.Import.CreateNamedPipe(
                "\\\\.\\pipe\\ServerPipe",
                DIS.Types.PIPE_ACCESS_DUPLEX,
                DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT,
                DIS.Types.PIPE_UNLIMITED_INSTANCES,
                0,
                1024,
                DIS.Types.NMPWAIT_WAIT_FOREVER,
                (uint)0
                );

            // вывод имени канала, в заголовок формы, чтобы можно было его использовать для ввода в форме клиента, запущенного на другом вычислительном узле
            this.Title += "    " + "Имя сервера: " + this.PipeName;

            // создание потока, отвечающего за работу с каналом
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        private void ServerOff()
        {
            this._continue = false;      // сообщаем, что работа с каналом завершена

            if (this.t != null)
                this.t.Abort();          // завершаем поток

            if (this.PipeHandle != -1)
                DIS.Import.CloseHandle(this.PipeHandle);     // закрываем дескриптор канала
        }

        private void ReceiveMessage()
        {
            string msg = ""; // прочитанное сообщение
            uint realBytesReaded = 0; // количество реально прочитанных из канала байтов

            // входим в бесконечный цикл работы с каналом
            while (this._continue)
            {
                if (DIS.Import.ConnectNamedPipe(PipeHandle, 0))
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(PipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff

                    // считываем количество реально прочитанных байт из канала начиная с 0 индекса т.к. изначально создается buff в размере 1024 байт
                    // иначе при парсинге сообщения в json возникает ошибка из-за прочитанных в конце пустых байтов
                    msg = Encoding.Unicode.GetString(buff, 0, (int)realBytesReaded);         // выполняем преобразование байтов в последовательность символов

                    if (msg != "")
                    {
                        // создаем динамический объект и десериализуем json строку
                        dynamic json_msg = JsonSerializer.Deserialize<ExpandoObject>(msg);
                        bool is_connection = Convert.ToBoolean(Convert.ToString(json_msg.is_connection)); // получаем статус сообщения
                        string user_name = Convert.ToString(json_msg.user_name); // получаем имя пользователя
                        string pc_name = Convert.ToString(json_msg.pc_name); // получаем имя машины
                        string user_message = Convert.ToString(json_msg.user_message); // получаем сообщение пользователя

                        if (is_connection)
                        {
                            try
                            {

                                connected_users.Dispatcher.Invoke((MethodInvoker)delegate
                                {
                                    // добавляем нового клиента в хэш-таблицу
                                    this.connected_clients.Add(user_name, pc_name);
                                    // добавляем нового клиента в ListBox приложения
                                    ListBoxItem new_client = new ListBoxItem();
                                    new_client.Content = user_name;
                                    int new_client_id = this.connected_users.Items.Add(new_client);
                                    // добавляем клиента в хэш-таблицу для удаления быстрого удаления из ListBox
                                    this.listbox_connected_clients.Add(user_name, new_client);
                                });

                            }
                            catch
                            {
                                //
                            }
                        }
                        else
                        {
                            //

                            SendMessageToClients(user_name, user_message);
                            
                            user_messages.Dispatcher.Invoke((MethodInvoker)delegate
                            {
                                this.user_messages.Items.Add($">> {user_name} : {user_message}"); // выводим полученное сообщение на форму
                            });
                        }
                    }

                    //CheckUsersForDelete();

                    DIS.Import.DisconnectNamedPipe(PipeHandle);                             // отключаемся от канала клиента 
                    Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
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

            //
            List<string> connected_clients_keys_for_delete = new List<string>();

            ICollection connected_clients_keys = connected_clients.Keys;
            foreach (string _user_name in connected_clients_keys)
            {
                uint BytesWritten = 0;  // количество реально записанных в канал байт

                string _pc_name = (string)connected_clients[_user_name];
                string client_pipe_name;
                if (_pc_name == Dns.GetHostName())
                    client_pipe_name = $"\\\\.\\pipe\\{_user_name}";
                else
                    client_pipe_name = $"\\\\{_pc_name}\\pipe\\{_user_name}";

                // открываем именованный канал, имя которого указано в поле server_pipe_name
                ClientPipeHandle = DIS.Import.CreateFile(client_pipe_name, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
                DIS.Import.WriteFile(ClientPipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал

                if (ClientPipeHandle == -1)
                {
                    connected_clients_keys_for_delete.Add(_user_name);
                }

                DIS.Import.CloseHandle(ClientPipeHandle);
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
                uint BytesWritten = 0;  // количество реально записанных в канал байт

                string _pc_name = (string)connected_clients[_user_name];
                string client_pipe_name;
                if (_pc_name == Dns.GetHostName())
                    client_pipe_name = $"\\\\.\\pipe\\{_user_name}";
                else
                    client_pipe_name = $"\\\\{_pc_name}\\pipe\\{_user_name}";

                ClientPipeHandle = DIS.Import.CreateFile(client_pipe_name, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
                DIS.Import.WriteFile(ClientPipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
                if (ClientPipeHandle == -1)
                {
                    connected_users.Dispatcher.Invoke((MethodInvoker)delegate
                    {
                        this.connected_users.Items.Remove(listbox_connected_clients[_user_name]);
                    });
                    listbox_connected_clients.Remove(_user_name);
                    connected_clients_keys_for_delete.Add(_user_name);
                }
                DIS.Import.CloseHandle(ClientPipeHandle);
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
