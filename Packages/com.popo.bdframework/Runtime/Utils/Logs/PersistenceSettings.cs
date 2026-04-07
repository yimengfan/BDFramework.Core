using System;
using UnityEngine;

namespace BDFramework.Logs
{
    [Serializable]
    public class PersistenceSettings
    {
        public const string DEFAULT_DIRECTORY_NAME = "playerlogs";
        public const int DEFAULT_FLUSH_INTERVAL_MS = 1000;
        public const int MIN_FLUSH_INTERVAL_MS = 100;

        public bool EnablePersistence = true;
        public string DirectoryName = DEFAULT_DIRECTORY_NAME;
        public int FlushIntervalMs = DEFAULT_FLUSH_INTERVAL_MS;
        public bool EnableEncryption = true;
        public string EncryptPassword = LogCrypto.DEFAULT_PASSWORD;

        public PersistenceSettings Normalize()
        {
            this.DirectoryName = string.IsNullOrWhiteSpace(this.DirectoryName)
                ? DEFAULT_DIRECTORY_NAME
                : this.DirectoryName.Trim();
            this.FlushIntervalMs = Mathf.Max(MIN_FLUSH_INTERVAL_MS, this.FlushIntervalMs);
            this.EncryptPassword = string.IsNullOrEmpty(this.EncryptPassword)
                ? LogCrypto.DEFAULT_PASSWORD
                : this.EncryptPassword;
            return this;
        }

        public PersistenceSettings CloneNormalized()
        {
            return new PersistenceSettings()
            {
                EnablePersistence = this.EnablePersistence,
                DirectoryName = this.DirectoryName,
                FlushIntervalMs = this.FlushIntervalMs,
                EnableEncryption = this.EnableEncryption,
                EncryptPassword = this.EncryptPassword,
            }.Normalize();
        }

        public static PersistenceSettings CreatePlayerDefault()
        {
            return new PersistenceSettings()
            {
                EnablePersistence = true,
                EnableEncryption = true,
            }.Normalize();
        }
    }
}

