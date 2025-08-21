#region ---- Includes ---- 
using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace ChassieuVolleyTournament
{
    public class PrivateKeyGenerator
    {
        #region ---- Properties ----
        private static readonly Lazy<PrivateKeyGenerator> _instance =
            new Lazy<PrivateKeyGenerator>(() => new PrivateKeyGenerator());

        private readonly HashSet<string> _generatedKeys = new HashSet<string>();
        private readonly Random _random = new Random();
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int _keyLength = 6;

        #endregion

        #region ---- Constructor ----
        private PrivateKeyGenerator() { }

        #endregion

        #region ---- Getters & Setters ----
        public static PrivateKeyGenerator Instance => _instance.Value;

        #endregion

        #region ---- Methods ----
        public string GenerateKey()
        {
            string key;

            do
            {
                var builder = new StringBuilder(_keyLength);
                for (int i = 0; i < _keyLength; i++)
                {
                    builder.Append(_chars[_random.Next(_chars.Length)]);
                }
                key = builder.ToString();
            }
            while (_generatedKeys.Contains(key));

            _generatedKeys.Add(key);
            return key;
        }

        #endregion
    }
}
