using System;
using System.Net.Mail;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableMailAddress
    {
        private SerializableMailAddress(MailAddress address)
        {
            Address = address.Address;
            DisplayName = address.DisplayName;
        }

        public string Address { get; }

        public string DisplayName { get; }

        public static implicit operator MailAddress(SerializableMailAddress address)
            => address == null ? null : new MailAddress(address.Address, address.DisplayName);

        public static implicit operator SerializableMailAddress(MailAddress address)
            => address == null ? null : new SerializableMailAddress(address);
    }
}