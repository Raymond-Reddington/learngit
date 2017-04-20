using ServiceStack.Redis;

namespace svn_assist_client
{
    public class User
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Authority { get; set; }
        public int Count { get; set; }
        public User(string u, string p, int a)
        {
            UserName = u;
            Password = p;
            Authority = a;
            Count = 0;
        }
        public User() { }
        public User(string username)
        {
            UserName = username;
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            Password = redis.Get<string>(RedisKeyName.userInfoPrefix + username + ":password");
            Authority = redis.Get<int>(RedisKeyName.userInfoPrefix + username + ":authority");
            Count = redis.Get<int>(RedisKeyName.userInfoPrefix + username + ":count");
            redis.Quit();
        }
    }
}
