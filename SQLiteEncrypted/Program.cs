using System.Collections.Generic;
using System.Configuration;
using log4net.Core;
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
        private static readonly log4net.ILog log =
    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            try
            {
                log.Info("Application is working");
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var connectionStringsSection = (ConnectionStringsSection) config.GetSection("connectionStrings");
                string keySeed;
                const string baseFolder = @"C:\SQLITEDATABASES";
                var dbFilePath = baseFolder + @"\SQLITEDB1.sqlite";

                try
                {
                    var connString = connectionStringsSection.ConnectionStrings["SQLLiteDbconnection"].ConnectionString;
                    Dictionary<string, string> connStringParts = connString.Split(';')
                        .Select(t => t.Split(new char[] {'='}, 2))
                        .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
                    dbFilePath = connStringParts["Data Source"];
                    keySeed = connStringParts["Password"];
                    log.Info("Seed retrieved from encrypted section");
                }
                catch (Exception ex)
                {
                    log.Info(string.Format("Seed missing from encrypted section: {0}",ex.Message));
                    keySeed = GenerateConnectionStringSectionWithSeed(config, dbFilePath);
                    log.Info("Seed generated and section encrypted.");
                }

                if (string.IsNullOrEmpty(keySeed))
                {
                    keySeed = GenerateConnectionStringSectionWithSeed(config, dbFilePath);
                    log.Info("Seed generated and section encrypted.");
                }

                var platform = new SQLite.Net.Platform.Win32.SQLitePlatformWin32();

                Directory.CreateDirectory(baseFolder);

                ISecureDatabase database = new MyDatabase(platform, dbFilePath);
                log.Info("SQLite DB opened.");

                var user = new SampleUser()
                {
                    Name = "Has AlTaiar",
                    Password = "very secure passwor, my dear Alex :)",
                    Id = Guid.NewGuid().ToString()
                };

                var inserted = database.SecureInsert(user, keySeed);
                log.Info("SQLite record inserted.");
                var got = database.SecureGetAll<SampleUser>(keySeed);
                log.Info("SQLite read succeeded.");
            }
            catch (Exception ex)
            {
                log.Fatal(string.Format("{0} - {1}", ex.Message, ex.StackTrace));
            }
        }

        private static string GenerateConnectionStringSectionWithSeed(Configuration config, string dbFilePath)
        {
            var keySeed = Convert.ToBase64String(GenerateKeyOrSalt(128));
            var connectionStringWithSalt = string.Format("Data Source={0};Password={1}", dbFilePath, keySeed);
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
