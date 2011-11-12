namespace badgeup.badge
{
    using System.IO.Ports;
    using System.Text.RegularExpressions;
    using System;

    /// <summary>
    /// Abstract class to represent a COM-enabled LEDBadge with Properties and Methods 
    /// for communication
    /// </summary>
    public abstract class COMBadge
    {
        /// <summary>
        /// COM Port to use in communicating with this Badge.
        /// </summary>
        public string ComPort           { get; set; }
        /// <summary>
        /// BAUD Rate to use in communicating with this Badge.
        /// </summary>
        public int BaudRate             { get; set; }
        /// <summary>
        /// Parity bit to use in communicating with this Badge.
        /// </summary>
        public Parity Parity            { get; set; }
        /// <summary>
        /// Word size to use in communicating with this Badge.
        /// </summary>
        public int WordSize             { get; set; }
        /// <summary>
        /// StopBits to use in communicating with this Badge.
        /// </summary>
        public StopBits Stop            { get; set; }
        /// <summary>
        /// A string containing a regular expression to match against each COM port's name.
        /// </summary>
        public string QueryBadgeValue   { get; set;}
        /// <summary>
        /// Queries the system's COM Ports to find a Badge that matches QueryBadgeValue case ignored. If more than one COM
        /// port matches the value, the first is returned.
        /// </summary>
        /// <param name="portName">OUT the COM Port that matches QueryBadgeValue</param>
        /// <returns>Returns false if the Badge is not found.</returns>
        public bool TryGetBadgePort(out string portName)
        {
            portName = string.Empty;

            if (!string.IsNullOrEmpty(this.QueryBadgeValue))
            {
                string port = SetupDeviceWrapper.ComPortNameFromFriendlyNamePrefix(this.QueryBadgeValue);
                if (!string.IsNullOrEmpty(port))
                {
                    portName = port;
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Writes the bytes specified below to the COMBadge this class represents
        /// </summary>
        /// <param name="bytes">Bytes to write to the device</param>
        public void WriteBytes(byte[] bytes)
        {
            try
            {
                SerialPort port = new SerialPort(this.ComPort, this.BaudRate, this.Parity, this.WordSize, this.Stop);
                port.Open();
                port.Write(bytes, 0, bytes.Length);
                port.Close();
            }
            catch (Exception ex)
            {
                throw new COMBadgeCommunicationError("COMBadge: WriteBytes error", ex);
            }
        }
        /// <summary>
        /// Returns a printable string of badge settings
        /// </summary>
        public string GetConfigString()
        {
            return string.Format("COM Port Name:{0}|Baud Rate:{1}|ParityBit:{2}|StopBits:{3}|Word Size:{4}", this.ComPort, this.BaudRate, this.Parity.ToString(), this.Stop.ToString(), this.WordSize);
        }
        /// <summary>
        /// Method to open a new connection to this COM Badge. Should initialize the badge settings
        /// and prepare for messages.
        /// </summary>
        public abstract void Open();
        /// <summary>
        /// Method to teardown an existing COM Badge instance.
        /// </summary>
        public abstract void Close();
    }

    public class COMBadgeNotFoundException : Exception { }
    public class COMBadgeCommunicationError : Exception
    {
        public COMBadgeCommunicationError(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
