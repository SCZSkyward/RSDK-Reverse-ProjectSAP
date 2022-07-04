using System;

namespace RSDKv3_4
{
    public class NameIdentifier
    {
        /// <summary>
        /// the MD5 hash of the name in bytes
        /// </summary>
        public byte[] hash;

        /// <summary>
        /// the name in plain text
        /// </summary>
        public string name = null;

        public bool usingHash = true;

        public NameIdentifier(string name)
        {
            hash = MD5Hasher.GetHash(new System.Text.ASCIIEncoding().GetBytes(name));
            this.name = name;
            usingHash = false;
        }

        public NameIdentifier(byte[] hash)
        {
            this.hash = hash;
        }

        public NameIdentifier(Reader reader)
        {
            Read(reader);
        }

        public void Read(Reader reader)
        {
            hash = reader.ReadBytes(16);
        }

        public void Write(Writer writer)
        {
            writer.Write(hash);
        }

        public string HashString()
        {
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }

        public override string ToString()
        {
            if (name != null) return name;
            return HashString();
        }
    }
}
