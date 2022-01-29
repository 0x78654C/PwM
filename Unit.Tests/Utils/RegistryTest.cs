using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using UtilsPwM = PwM.Utils;
using PwM;

namespace Unit.Tests.Utils
{
    public class RegistryTest
    {
        private static  string currentUserReg= @"HKEY_CURRENT_USER\";

        [Theory]
        [InlineData(@"HKEY_CURRENT_USER\Environment\", "Path")]
        public void CheckIfSubKeyExist(string key ,string subKey)
        {
            Assert.True(UtilsPwM.RegistryManagement.RegKey_Check(key, subKey));
        }

        [Theory]
        [InlineData(@"PwM", "VaulSharePoint")]
        public void CreateKey(string key, string subKey)
        {
            UtilsPwM.RegistryManagement.RegKey_CreateKey(key, subKey, "1");
            Assert.True(UtilsPwM.RegistryManagement.RegKey_Check(currentUserReg+ key, subKey));
          //  UtilsPwM.RegistryManagement.RegKey_Delete(key, subKey);
        }


        [Theory]
        [InlineData(@"PwM", "VaulSharePoint")]
        public void ReadSubKey(string key, string subKey)
        {
            string read = UtilsPwM.RegistryManagement.RegKey_Read(currentUserReg+ key, subKey);
            bool exits;
            if (read == "1")
            {
                exits = true;
            }
            else { exits = false;
            }
            Assert.True(exits);
        }

        //[Theory]
        //[InlineData(@"PwM", "VaulSharePoint")]
        //public void DeleteSubKey(string key, string subKey)
        //{
        //    UtilsPwM.RegistryManagement.RegKey_Delete(key, subKey);
        //    Assert.False(UtilsPwM.RegistryManagement.RegKey_Check(currentUserReg +key, subKey));
        //} //TODO will see if keep registry class
    }
}
