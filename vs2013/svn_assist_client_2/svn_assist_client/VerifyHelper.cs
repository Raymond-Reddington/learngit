using ServiceStack.Redis;
using System;
using System.Text;
using System.Web.Script.Serialization;
enum UserAuthority { Alrealy = -1, None = 0, Normal = 1, Manage = 3}

namespace svn_assist_client
{
    public static class VerifyHelper
    {
        // redis静态全局变量，主线程中的redis连接
        
        const string hashID = "svn_assist:users:hash_table";
        // js 启用序列化和反序列化
        static JavaScriptSerializer js = new JavaScriptSerializer();
        /*
         *  Function : VerifyUser
         *  Description: 用户身份验证
         *  Parameters:
         *    username: 用户名
         *    password: 用户密码
         *  Return:
         *    -1: 用户已经在其他地方登陆
         *    0: 没有匹配用户
         *    1: 普通用户
         *    else: 管理员
         */
        public static int VerifyUser(string username, string password)
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            if(username == null || password == null)
            {
                return (int)UserAuthority.None;
            }
            try
            {
                byte[] key = Encoding.ASCII.GetBytes(username);
                if(redis.SIsMember(RedisKeyName.usernameSet, Encoding.ASCII.GetBytes(username)) != 0)
                {
                    if(password == redis.Get<string>(RedisKeyName.userInfoPrefix + username + ":password"))
                    {
                        return redis.Get<int>(RedisKeyName.userInfoPrefix + username + ":authority");
                    }
                }
                redis.Quit();
            }
            catch (Exception e)
            {
                redis.Quit();
                Console.WriteLine(e.Message);
            }
            return (int)UserAuthority.None;
        }

        public static bool IsExist(string username)
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            bool isEx = (redis.SIsMember(RedisKeyName.usernameSet, Encoding.ASCII.GetBytes(username)) != 0);
            redis.Quit();
            return isEx;
        }
    }
}
