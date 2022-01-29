using System.Collections.Generic;

namespace PwM.Utils
{
    public class VaultDetails
    {
        public string VaultName { get; set; }
        public string SharedPath { get; set; }

        public override bool Equals(object obj)
        {
            return obj is VaultDetails details &&
                   VaultName == details.VaultName &&
                   SharedPath == details.SharedPath;
        }

        public override int GetHashCode()
        {
            int hashCode = -1670917873;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VaultName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SharedPath);
            return hashCode;
        }
    }
}
