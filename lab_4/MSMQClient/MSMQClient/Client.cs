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
using System.IO;
using System.Messaging;
using MsgJsonLibrary;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Threading;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private MessageQueue q_client = null;          // очередь сообщений
        private Thread t = null;                // поток, отвечающий за работу с очередью сообщений

        private MessageQueue q_server = null;      // очередь сообщений, в которую будет производиться запись сообщений
        bool user_connected = false; // поле статуса подлкючения пользователя к серверу (флаг, указывающий продолжается ли работа с мэйлслотом)

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
        }

        private void ChangeInterface()
        {
            user_connected = !user_connected;
            tbPath.Enabled = !user_connected;
            tbUserName.Enabled = !user_connected;
            btnSend.Enabled = user_connected;
            btnConnectDisconnet.Text = !user_connected ? "Подключиться" : "Отключиться";
        }

        private void btnConnectDisconnect_Click(object sender, EventArgs e)
        {
            if (user_connected) // если пользователь подключен к серверу
            {
                ChangeInterface();
                CloseClientQueue();
            }
            else
            {
                int count_error = 0;
                string message_error = "";

                if (tbUserName.Text.Length == 0)
                    message_error += $"Ошибка #{++count_error}: Введите Имя пользователя.\n";

                if (MessageQueue.Exists(tbPath.Text))
                {
                    // если очередь, путь к которой указан в поле tbPath существует, то открываем ее
                    q_server = new MessageQueue(tbPath.Text);
                }
                else
                    message_error += $"Ошибка #{++count_error}: Указан неверный путь к очереди, либо очередь не существует.\n";

                if (message_error.Length != 0)
                    MessageBox.Show(message_error);
                else
                {
                    ChangeInterface();
                    OpenClientQueue();
                }
            }
            
        }

        private void OpenClientQueue()
        {
            string path = Dns.GetHostName() + $"\\private$\\{tbUserName.Text}";    // путь к очереди сообщений, Dns.GetHostName() - метод, возвращающий имя текущей машины

            // если очередь сообщений с указанным путем существует, то открываем ее, иначе создаем новую
            if (MessageQueue.Exists(path))
                q_client = new MessageQueue(path);
            else
                q_client = MessageQueue.Create(path);

            // задаем форматтер сообщений в очереди
            q_client.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

            // создание потока, отвечающего за работу с очередью сообщений
            Thread t = new Thread(ReceiveMessage);
            t.Start();

            //
            MsgJsonMSMQ info = new MsgJsonMSMQ(user_connected, !user_connected, Dns.GetHostName(), tbUserName.Text, "Text");
            string info_json = MsgJsonMSMQ.MsgJsonSerialize(info);
            q_server.Send("", info_json);
        }

        private void CloseClientQueue()
        {
            //
            MsgJsonMSMQ info = new MsgJsonMSMQ(user_connected, !user_connected, Dns.GetHostName(), tbUserName.Text, "Text");
            string info_json = MsgJsonMSMQ.MsgJsonSerialize(info);
            if (MessageQueue.Exists(tbPath.Text))
                q_server.Send("", info_json);

            if (q_client != null)
            {
                //MessageQueue.Delete(q.Path);      // в случае необходимости удаляем очередь сообщений
            }

            if (t != null)
            {
                t.Abort();          // завершаем поток
            }
        }

        private void ReceiveMessage()
        {
            if (q_client == null)
                return;

            System.Messaging.Message msg = null;

            // входим в бесконечный цикл работы с очередью сообщений
            while (user_connected)
            {
                if (q_client.Peek() != null)   // если в очереди есть сообщение, выполняем его чтение, интервал до следующей попытки чтения равен 10 секундам
                    msg = q_client.Receive(TimeSpan.FromSeconds(10.0));

                rtbMessages.Invoke((MethodInvoker)delegate
                {
                    if (msg == null) return;

                    rtbMessages.Text += $"{msg.Body}\n";
                });

                Thread.Sleep(500);          // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            MsgJsonMSMQ info = new MsgJsonMSMQ(false, false, Dns.GetHostName(), tbUserName.Text, "Text");
            string info_json = MsgJsonMSMQ.MsgJsonSerialize(info);

            // выполняем отправку сообщения в очередь
            q_server.Send(tbMessage.Text, info_json);
        }
    }
}