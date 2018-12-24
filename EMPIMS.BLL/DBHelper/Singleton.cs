using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMPIMS.Code.Security;
using System.Configuration;
using MongoDB.Driver;

namespace EMPIMS.BLL.DBHelper
{
    public sealed class Singleton
    {
        static Singleton instance = null;
        private static readonly object padlock = new object();
        static MongoDatabase database = null;

        private Singleton()
        {
            string connectionStr = MD5Encrypt.Decrypto(ConfigurationManager.ConnectionStrings["DBConnection"].ToString());
            string databaseStr = MD5Encrypt.Decrypto(ConfigurationManager.ConnectionStrings["MongoDB"].ToString());

            if (string.IsNullOrEmpty(connectionStr))
            {
                connectionStr = MD5Encrypt.Decrypto(ConfigurationManager.AppSettings["DBConnection"].ToString());
            }
            if (string.IsNullOrEmpty(databaseStr))
            {
                databaseStr = MD5Encrypt.Decrypto(ConfigurationManager.AppSettings["MongoDB"].ToString());
            }

            var client = new MongoClient(connectionStr);
            var server = client.GetServer();
            database = server.GetDatabase(databaseStr);
        }

        public MongoDatabase GetDataBase()
        {
            return database;
        }

        public static Singleton Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (padlock)
                    {
                        if (instance == null)
                        {
                            instance = new Singleton();
                        }
                    }
                }
                return instance;
            }
        }
    }
}
