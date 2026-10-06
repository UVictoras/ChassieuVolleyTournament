using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ChassieuVolleyTournament
{
    /// --------------------------------------------------------
    /// Singleton class responsible for generating unique
    /// 6-character private keys (one per match).
    /// Uses a cryptographic RNG (keys are exposed on the web) and
    /// an alphabet without look-alike characters (no 0/O, 1/I).
    /// --------------------------------------------------------
    public class PrivateKeyGenerator
    {
        #region ---- Properties ----

        private static readonly Lazy<PrivateKeyGenerator> _instance =
            new Lazy<PrivateKeyGenerator>(() => new PrivateKeyGenerator());

        private readonly HashSet<string> _generatedKeys = new HashSet<string>();
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly object _lock = new object();

        /// 32 characters -> a random byte masked with 31 is perfectly unbiased.
        private const string _chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int _keyLength = 6;

        #endregion

        private PrivateKeyGenerator() { }

        #region ---- Methods ----

        /// ---------------------------------------------------
        /// Generates a unique key. Never returns the same key twice.
        /// ---------------------------------------------------
        public string GenerateKey()
        {
            lock (_lock)
            {
                string key;
                byte[] bytes = new byte[_keyLength];

                do
                {
                    _rng.GetBytes(bytes);
                    var builder = new StringBuilder(_keyLength);
                    for (int i = 0; i < _keyLength; i++)
                        builder.Append(_chars[bytes[i] & 31]);
                    key = builder.ToString();
                }
                while (!_generatedKeys.Add(key));

                return key;
            }
        }

        /// ---------------------------------------------------------------
        /// Normalizes what a referee typed: phone keyboards add lowercase
        /// letters, spaces or dashes. Match keys are always upper-case.
        /// ---------------------------------------------------------------
        public static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var builder = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                    builder.Append(char.ToUpperInvariant(c));
            }
            return builder.ToString();
        }

        #endregion

        public static PrivateKeyGenerator Instance => _instance.Value;
    }
}
