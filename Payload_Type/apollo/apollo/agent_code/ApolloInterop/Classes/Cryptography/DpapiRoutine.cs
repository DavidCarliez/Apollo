using System.Security.Cryptography;
using AgInterop.Interfaces;

namespace AgInterop.Classes.Cryptography
{
    public class DpapiRoutine : ICryptographicRoutine
    {
        private readonly byte[] _additionalEntropy;
        private readonly DataProtectionScope _scope;
        public DpapiRoutine(byte[] additionalEntropy, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            _additionalEntropy = additionalEntropy;
            _scope = scope;
        }

        public DpapiRoutine(DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            _scope = scope;
            _additionalEntropy = null;
        }

        public byte[] Encrypt(byte[] data)
        {
            return ProtectedData.Protect(data, _additionalEntropy, _scope);
        }

        public byte[] Decrypt(byte[] data)
        {
            return ProtectedData.Unprotect(data, _additionalEntropy, _scope);
        }
    }
}