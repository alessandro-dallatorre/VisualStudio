using System.Collections.Generic;
using System.Configuration;
using SQLite.Net.Cipher.Interfaces;
using SQLite.Net.Cipher.Model;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

namespace SQLiteEncrypted
{
    public class SampleUser : IModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [Secure]
        public string Password { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionStringsSection = (ConnectionStringsSection)config.GetSection("connectionStrings");
            string keySeed;
            const string baseFolder = @"C:\SQLITEDATABASES";
            var dbFilePath = baseFolder + @"\SQLITEDB1.sqlite";

            try
            {
                var connString = connectionStringsSection.ConnectionStrings["SQLLiteDbconnection"].ConnectionString;
                Dictionary<string, string> connStringParts = connString.Split(';')
               .Select(t => t.Split(new char[] { '=' }, 2))
               .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
                dbFilePath = connStringParts["Data Source"];
                keySeed = connStringParts["Password"];
            }
            catch (Exception)
            {
                keySeed = GenerateConnectionStringSectionWithSeed(config, dbFilePath);  
            }

            if (string.IsNullOrEmpty(keySeed))
            {
                keySeed = GenerateConnectionStringSectionWithSeed(config, dbFilePath);
            }

            var platform = new SQLite.Net.Platform.Win32.SQLitePlatformWin32();

            Directory.CreateDirectory(baseFolder);

            ISecureDatabase database = new MyDatabase(platform, dbFilePath);


            var user = new SampleUser()
            {
                Name = "Has AlTaiar",
                Password = "very secure passwor, my dear Alex :)",
                Id = Guid.NewGuid().ToString()
            };

            var inserted = database.SecureInsert(user, keySeed);

            var got = database.SecureGetAll<SampleUser>(keySeed);
        }

        private static string GenerateConnectionStringSectionWithSeed(Configuration config, string dbFilePath)
        {
            var keySeed = Convert.ToBase64String(GenerateKeyOrSalt(128));
            var connectionStringWithSalt = string.Format("Data Source={0};Password={1}", dbFilePath, keySeed);
            config.Save();
            var sqlLiteDbconnection = config.ConnectionStrings.ConnectionStrings["SQLLiteDbconnection"];
            if (sqlLiteDbconnection == null)
            {
                config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings("SQLLiteDbconnection",
                    connectionStringWithSalt));
            }
            else
            {
                sqlLiteDbconnection.ConnectionString = connectionStringWithSalt;
                    
            }
            ConfigurationManager.RefreshSection("connectionStrings");
            config.Save();
            ConnectionStringEncryptor.EncryptAppSettingsFile();
            return keySeed;
        }

        public static byte[] GenerateKeyOrSalt(int size)
        {
            var secretkey = new Byte[size];
            //RNGCryptoServiceProvider is an implementation of a random number generator. 
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(secretkey);
            }
            return secretkey;
        }
    }
}
