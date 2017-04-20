using System.Collections.Generic;
using ServiceStack.Redis;

namespace svn_assist_client
{

    public class Task
    {
        public int ID { get; set; }
        public string Author { get; set; }
        public string Command { get; set; }
        public string State { get; set; }
        public int Time { get; set; }
        public int Type { get; set; }
        public string ServerID { get; set; }
        public string TagID { get; set; }
        public string MoreCommand { get; set; }
        public void SetStateCancel()
        {
            State = "Cancel";
        }
        public void SetStateWait()
        {
            State = "Waiting";
        }
        public void SetStateComplete()
        {
            State = "Done";
        }
        public Task() { }
        
        public Task(string id)
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            ID = int.Parse(id);
            Author = redis.Get<string>(RedisKeyName.taskPrefix + id + ":Author");
            Command = redis.Get<string>(RedisKeyName.taskPrefix + id + ":Command");
            State = redis.Get<string>(RedisKeyName.taskPrefix + id + ":State");
            Time = redis.Get<int>(RedisKeyName.taskPrefix + id + ":Time");
            Type = redis.Get<int>(RedisKeyName.taskPrefix + id + ":Type");
            ServerID = redis.Get<string>(RedisKeyName.taskPrefix + id + ":ServerID");
            TagID = redis.Get<string>(RedisKeyName.taskPrefix + id + ":TagID");
            MoreCommand = Command + 
                " " + redis.Get<string>(RedisKeyName.serverPrefix + ServerID + ":name") +
                " ";
            string tag = redis.Get<string>(RedisKeyName.binTagPrefix + TagID + ":param");
            if (tag != null && tag.Trim() != "")
            {
                MoreCommand += tag.Replace(" ", "/").Replace("_", "__");
            }
            redis.Quit();
        }
        public bool Equal(Task t)
        {
            if (Time != t.Time) return false;
            if (Author != t.Author) return false;
            if (Command != t.Command) return false;
            if (Type != t.Type) return false;
            if (State != t.State) return false;
            if (ServerID != t.ServerID) return false;
            if (TagID != t.TagID) return false;

            return true;
        }

    }
}
