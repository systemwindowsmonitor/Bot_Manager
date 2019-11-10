using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace wcf_chat
{
  
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServiceChat : IServiceChat
    {
        List<ServerUser> users = new List<ServerUser>();
        List<long> bot_clients;
        TelegramBotClient client;
       
        int nextId = 1;

        ServiceChat()
        {
            client = new TelegramBotClient("987477165:AAETDhLVjdsDQAp-qm30-qAKG7DKoOFDaGs");
            client.StartReceiving();
            client.OnMessage += messageFromBot; 
            this.bot_clients = getUsersChats();
           
        }

        private List<long> getUsersChats()
        {
            var tmp = new List<long>();


            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=UsersDB.db;")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT * FROM User;", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {
                        tmp.Add(record.GetInt64(1));
                    }
                }
            }

            return tmp;
        }

        public void MassMessagig(string message)
        {
            foreach (var item in this.bot_clients)
            {
                client.SendTextMessageAsync(item, message);
            }
        }

        private void messageFromBot(object sender, MessageEventArgs e)
        {
            client.SendTextMessageAsync(e.Message.Chat.Id, "Dont touch me");
            insertNewUserId(e.Message.Chat.Id);
        }

        void insertNewUserId(long id)
        {
            if (this.bot_clients.Contains(id))
                return;
            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=UsersDB.db")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("INSERT INTO User (telegram_id) VALUES(" + id + ");", conn);
                command.ExecuteNonQuery();

            }
        }

        public int Connect(string name)
        {
            
            ServerUser user = new ServerUser() {
                ID = nextId,
                Name = name,
                operationContext = OperationContext.Current
            };
            nextId++;

            SendMsg(": "+user.Name+" подключился к чату!",0);
            users.Add(user);
            return user.ID;
        }

        public void Disconnect(int id)
        {
            var user = users.FirstOrDefault(i => i.ID == id);
            if (user!=null)
            {
                users.Remove(user);
                SendMsg(": "+user.Name + " покинул чат!",0);
            }
        }

        public void SendMsg(string msg, int id)
        {
            foreach (var item in users)
            {
                string answer = DateTime.Now.ToShortTimeString();

                var user = users.FirstOrDefault(i => i.ID == id);
                if (user != null)
                {
                    answer += ": " + user.Name+" ";
                }
                answer += msg;
                item.operationContext.GetCallbackChannel<IServerChatCallback>().MsgCallback(answer);
            }
        }
    }
}
