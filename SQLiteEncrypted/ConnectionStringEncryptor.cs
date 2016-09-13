using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteEncrypted
{
    public class ConnectionStringEncryptor
    {
        private static readonly string FileName = Assembly.GetEntryAssembly().Location;

        public static void EncryptConnectionString(bool encrypt, string fileName)
        {
            try
            {
                // Open the configuration file and retrieve the connectionStrings section.
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(fileName);
                var configSection = configuration.GetSection("connectionStrings") as ConnectionStringsSection;
                if (configSection != null && ((!(configSection.ElementInformation.IsLocked)) && (!(configSection.SectionInformation.IsLocked))))
                {
                    if (encrypt && !configSection.SectionInformation.IsProtected)
                    {
                        //this line will encrypt the file
                        configSection.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    }

                    if (!encrypt && configSection.SectionInformation.IsProtected)//encrypt is true so encrypt
                    {
                        //this line will decrypt the file. 
                        configSection.SectionInformation.UnprotectSection();
                    }
                    //re-save the configuration file section
                    configSection.SectionInformation.ForceSave = true;
                    // Save the current configuration

                    configuration.Save();
                    //Process.Start("notepad.exe", configuration.FilePath);
                    //configFile.FilePath 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public static void EncryptAppSettingsFile()
        {
            if (File.Exists(FileName))
            {
                EncryptConnectionString(true, FileName);
            }
            else
            {
                Console.WriteLine("File not exist");
            }
        }

        public static void DecryptAppSettingFile()
        {
            if (File.Exists(FileName))
            {
                EncryptConnectionString(false, FileName);
            }
            else
            {
                Console.WriteLine("File not exist");
            }
        }
    }
}
