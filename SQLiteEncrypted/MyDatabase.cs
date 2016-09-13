using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Cipher.Data;
using SQLite.Net.Interop;

namespace SQLiteEncrypted
{
    public class MyDatabase : SecureDatabase
    {
        public MyDatabase(ISQLitePlatform platform, string dbfile)
            : base(platform, dbfile)
        {
        }

        protected override void CreateTables()
        {
            CreateTable<SampleUser>();
        }
    }
}
