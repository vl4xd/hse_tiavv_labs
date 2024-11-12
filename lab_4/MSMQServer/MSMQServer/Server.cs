using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Messaging;
using MsgJsonLibrary;
using System.IO;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private MessageQueue q = null;          // очередь сообщений
        private Thread t = null;                // поток, отвечающий за работу с очередью сообщений
        private bool _continue = true;          // флаг, указывающий продолжается ли работа с мэйлслотом
        // словарь для хранения информации user_name клиента и pc_name клиента
        private Dictionary<string, string> clients = new Dictionary<string, string>();
        // словарь для хранения информации user_name клиента и ссылка на элемент ListView
        private Dictionary<string, ListViewItem> lvClients_link = new Dictionary<string, ListViewItem>();

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();

            string path = Dns.GetHostName() + "\\private$\\ServerQueue";    // путь к очереди сообщений, Dns.GetHostName() - метод, возвращающий имя текущей машины
            // если очередь сообщений с указанным путем существует, то открываем ее, иначе создаем новую
            if (MessageQueue.Exists(path))
                q = new MessageQueue(path);
            else
                q = MessageQueue.Create(path);

            // задаем форматтер сообщений в очереди
            q.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

            // вывод пути к очереди сообщений в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + q.Path;

            // создание потока, отвечающего за работу с очередью сообщений
            Thread t = new Thread(ReceiveMessage);
            t.Start();
        }

        // получение сообщения
        private void ReceiveMessage()
        {
            if (q == null)
                return;

            System.Messaging.Message msg = null;

            // входим в бесконечный цикл работы с очередью сообщений
            while (_continue)
            {
                if (q.Peek() != null)   // если в очереди есть сообщение, выполняем его чтение, интервал до следующей попытки чтения равен 10 секундам
                    msg = q.Receive(TimeSpan.FromSeconds(10.0));

                string send_message = "";

                rtbMessages.Invoke((MethodInvoker)delegate
                {
                    if (msg == null) return;

                    MsgJsonMSMQ info = MsgJsonMSMQ.MsgJsonDeserialize(msg.Label);

                    if (info.Is_connection)
                    {
                        send_message = $"Пользователь {info.User_name} присоединился к чату.";
                        rtbMessages.Text += send_message;

                        ListViewItem new_client = new ListViewItem(info.User_name);
                        clients.Add(info.User_name, info.User_pc_name);
                        lvClients_link.Add(info.User_name, new_client);

                        lvClients.Invoke((MethodInvoker)delegate
                        {
                            lvClients.Items.Add(new_client);
                        });
                        
                    }
                    else if (info.Is_disconnection)
                    {
                        send_message = $"Пользователь {info.User_name} покинул чат.";
                        rtbMessages.Text += send_message;

                        clients.Remove(info.User_name);
                        lvClients.Invoke((MethodInvoker)delegate
                        {
                            lvClients.Items.Remove(lvClients_link[info.User_name]);
                        });
                        lvClients_link.Remove(info.User_name);
                    }
                    else
                    {
                        send_message = $"{info.User_name} : {msg.Body}";
                        rtbMessages.Text += send_message;
                    }

                    rtbMessages.Text += "\n";
                });

                List<string> remove_clients = new List<string>();
                foreach (var client in clients)
                {
                    string path_client = $"{client.Value}\\private$\\{client.Key}";
                    if (MessageQueue.Exists(path_client))
                    {
                        // если очередь, путь существует, то открываем ее
                        MessageQueue q_client = new MessageQueue(path_client);
                        q_client.Send(send_message);
                    }
                    else
                    {
                        send_message = $"Пользователь {client.Key} покинул чат.";
                        rtbMessages.Invoke((MethodInvoker)delegate
                        {
                            rtbMessages.Text += send_message;
                        });
                        
                        remove_clients.Add(client.Key);
                        
                        lvClients.Invoke((MethodInvoker)delegate
                        {
                            lvClients.Items.Remove(lvClients_link[client.Key]);
                        });
                        lvClients_link.Remove(client.Key);
                    }
                }
                foreach (var client_name in remove_clients)
                {
                    clients.Remove(client_name);
                }

                Thread.Sleep(500);          // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с очередью сообщений завершена

            if (q != null)
            {
                //MessageQueue.Delete(q.Path);      // в случае необходимости удаляем очередь сообщений
            }

            if (t != null)
            {
                t.Abort();          // завершаем поток
            }
        }
    }
}