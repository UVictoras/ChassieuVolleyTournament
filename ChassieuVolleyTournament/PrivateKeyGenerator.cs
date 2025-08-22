#region ---- Includes ---- 
using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace ChassieuVolleyTournament
{
    /// --------------------------------------------------------
    /// Singleton class responsible for generating unique
    /// 6-character alphanumeric private keys.
    /// Ensures no duplicate keys are generated during runtime.
    /// --------------------------------------------------------
    public class PrivateKeyGenerator
    {
        #region ---- Properties ----

        /// -----------------------------------------------
        /// Singleton instance of the PrivateKeyGenerator.
        /// -----------------------------------------------
        private static readonly Lazy<PrivateKeyGenerator> _instance =
            new Lazy<PrivateKeyGenerator>(() => new PrivateKeyGenerator());

        /// ---------------------------------------------------------------
        /// Set containing all keys generated so far to ensure uniqueness.
        /// ---------------------------------------------------------------
        private readonly HashSet<string> _generatedKeys = new HashSet<string>();

        private readonly Random _random = new Random();

        /// ----------------------------------------
        /// Allowed characters in a key (A-Z, 0-9).
        /// ----------------------------------------
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ356789";
        private const int _keyLength = 6;

        #endregion

        #region ---- Constructor ----

        /// -------------------------------------------
        /// Private constructor for singleton pattern.
        /// -------------------------------------------
        private PrivateKeyGenerator() { }

        #endregion

        #region ---- Methods ----

        /// --------------------------------------------------
        /// Generates a unique 6-character alphanumeric key.
        /// Ensures that the same key is not generated twice.
        /// Returns the newly generated key as a string.
        /// --------------------------------------------------
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

        #region ---- Getters & Setters ----
        /// --------------------------------------------------------
        /// Gets the singleton instance of the PrivateKeyGenerator.
        /// --------------------------------------------------------
        public static PrivateKeyGenerator Instance => _instance.Value;

        #endregion
    }
}
