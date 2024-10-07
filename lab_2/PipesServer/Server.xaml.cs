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
        private Int32 HandleReverseMailSlot;

        private int ClientHandleMailSlot;       // дескриптор мэйлслота
        private string MailSlotName = "\\\\" + Dns.GetHostName() + "\\mailslot\\ServerMailslot";    // имя мэйлслота, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
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
            // создание мэйлслота
            ClientHandleMailSlot = DIS.Import.CreateMailslot("\\\\.\\mailslot\\ServerMailslot", 0, DIS.Types.MAILSLOT_WAIT_FOREVER, 0);

            // вывод имени мэйлслота в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Title += "    " + "Имя сервера: " + this.MailSlotName;

            // создание потока, отвечающего за работу с мэйлслотом
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        private void MailOff()
        {
            this._continue = false;      // сообщаем, что работа с мэйлслотом завершена

            if (t != null)
                t.Abort();          // завершаем поток

            if (ClientHandleMailSlot != -1)
                DIS.Import.CloseHandle(ClientHandleMailSlot);            // закрываем дескриптор мэйлслота
        }

        private void ReceiveMessage()
        {
            string msg = ""; // прочитанное сообщение
            int MailslotSize = 0;       // максимальный размер сообщения
            int lpNextSize = 0;         // размер следующего сообщения
            int MessageCount = 0;       // количество сообщений в мэйлслоте
            uint realBytesReaded = 0;   // количество реально прочитанных из мэйлслота байтов

            // входим в бесконечный цикл работы с каналом
            while (this._continue)
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
                                string user_name = Convert.ToString(json_msg.user_name); // получаем имя пользователя
                                string pc_name = Convert.ToString(json_msg.pc_name); // получаем имя машины
                                string user_message = Convert.ToString(json_msg.user_message); // получаем сообщение пользователя

                                try
                                {
                                    if (!connected_clients.ContainsKey(user_name))
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
                                catch (Exception)
                                {
                                    //
                                }
                                //

                                SendMessageToClients(user_name, user_message);

                                user_messages.Dispatcher.Invoke((MethodInvoker)delegate
                                {
                                    this.user_messages.Items.Add($">> {user_name} : {user_message}"); // выводим полученное сообщение на форму
                                });
                                Thread.Sleep(500);                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                            }

                            //CheckUsersForDelete();
                            Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                        }
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

                HandleReverseMailSlot = DIS.Import.CreateFile($"\\\\.\\mailslot\\{_user_name}",
                                DIS.Types.EFileAccess.GenericWrite,
                                DIS.Types.EFileShare.Read,
                                0,
                                DIS.Types.ECreationDisposition.OpenExisting,
                                0,
                                0);

                DIS.Import.WriteFile(HandleReverseMailSlot, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);     // выполняем запись последовательности байт в мэйлслот

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
            MailOff();
        }
    }
}
