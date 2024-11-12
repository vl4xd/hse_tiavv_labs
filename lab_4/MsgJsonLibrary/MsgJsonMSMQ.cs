using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace MsgJsonLibrary
{
    public class MsgJsonMSMQ
    {
        private bool _is_connection;
        private bool _is_disconnection;
        private string _user_pc_name;
        private string _user_name;
        private string _user_message;

        public bool Is_connection
        {
            get => _is_connection;
            set => _is_connection = value;
        }

        public bool Is_disconnection
        {
            get => _is_disconnection;
            set => _is_disconnection = value;
        }

        public string User_pc_name
        {
            get => _user_pc_name;
            set => _user_pc_name = value;
        }

        public string User_name
        {
            get => _user_name;
            set => _user_name = value;
        }

        public string User_message
        {
            get => _user_message;
            set => _user_message = value;
        }

        public MsgJsonMSMQ() { }

        public MsgJsonMSMQ(bool is_connection, bool is_disconnection, string user_pc_name, string user_name, string user_message)
        {
            Is_connection = is_connection;
            Is_disconnection = is_disconnection;
            User_pc_name = user_pc_name;
            User_name = user_name;
            User_message = user_message;
        }

        /// <summary>
        /// Десериализация строки json формата через перегруженный конструктор класса MsgJsonMSMQ
        /// </summary>
        /// <param name="json_string">строка json формата с полями класса MsgJsonMSMQ</param>
        public MsgJsonMSMQ(string json_string)
        {
            MsgJsonMSMQ temp = JsonSerializer.Deserialize<MsgJsonMSMQ>(json_string);

            Is_connection = temp.Is_connection;
            Is_disconnection = temp.Is_disconnection;
            User_pc_name = temp.User_pc_name;
            User_name = temp.User_name;
            User_message = temp.User_message;
        }

        /// <summary>
        /// Сериализация класса MsgJsonMSMQ в строку json формата
        /// </summary>
        /// <returns>строка json формата с полями класса MsgJsonMSMQ</returns>
        public static string MsgJsonSerialize(MsgJsonMSMQ obj_MsgJsonMSMQ)
        {
            return JsonSerializer.Serialize(obj_MsgJsonMSMQ);
        }


        public static MsgJsonMSMQ MsgJsonDeserialize(string json_string)
        {
            // https://metanit.com/sharp/tutorial/6.5.php

            return JsonSerializer.Deserialize<MsgJsonMSMQ>(json_string);
        }
    }
}
