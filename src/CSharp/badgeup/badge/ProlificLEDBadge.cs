namespace badgeup.badge
{
    using System.IO.Ports;
    using System.Text;
    using System;
    using System.IO;

    /// <summary>
    /// Class to encapsulate Properties and Methods needed to communicate with the Prolific LED
    /// Badge over COM
    /// </summary>
    public class ProlificLEDBadge : COMBadge
    {
        public const byte StartByte        = 0x00;          //
        public const byte PaddingByte      = 0x00;          //
        public const byte ControlByteOne   = 0x02;          //
        public const byte ControlByteTwo   = 0x31;          //
        public const byte ControlByteThree = 0x33;          //
        public const uint CheckSumMod      = 0x100;         //256 in hex used get a 1-byte result

        public const int MaxLength         = 250;           //Maximum message length

        /// <summary>
        /// Default Constructor - Sets the Prolific LED Badge specific COM Port Settings observed from testing/portmon
        /// </summary>
        public ProlificLEDBadge() 
        {
            this.BaudRate = 38400;
            this.Parity = Parity.None;
            this.WordSize = 8;
            this.Stop = StopBits.One;
            this.QueryBadgeValue = "Prolific";
        }
        /// <summary>
        /// Initializes and prepares the Prolific LED Badge for
        /// communication.
        /// </summary>
        public override void Open()
        {
            string badgePort;
            if (this.TryGetBadgePort(out badgePort))
            {
                this.ComPort = badgePort;
            }
            else
            {
                throw new COMBadgeNotFoundException();
            }
        }
        /// <summary>
        /// Close the connection to the Prolific Badge
        /// </summary>
        public override void Close()
        {
            //do nothing
        }
        /// <summary>
        /// Sets an message on the badge
        /// </summary>
        /// <param name="message">An LEDMessage containing the message to set</param>
        public void SetMessage(LEDMessage message)
        {
            this.SetMessages(new LEDMessage[] { message });
        }
        /// <summary>
        /// Sets the messages on the badge
        /// </summary>
        /// <param name="messages">An Array of up to 6 messages to set</param>
        public void SetMessages(LEDMessage[] messages)
        {
            byte[] badgeBytes = null;
            using (MemoryStream bytePattern = new MemoryStream())
            {
                //write the start byte
                bytePattern.WriteByte(StartByte);

                //get number of messages to process
                int numMsgs = 6;
                if (messages.Length < numMsgs)
                    numMsgs = messages.Length;

                //messages enabled flag
                uint msgCount = 0x00;

                //get the byte pattern foreach message
                for (int m = 0; m < numMsgs; m++)
                {
                    byte[] badgeMsg = CreateMessageBytePattern(messages[m].Message, m, messages[m].Style, messages[m].Speed);
                    bytePattern.Write(badgeMsg, 0, badgeMsg.Length);

                    msgCount += msgCount + 0x01;
                }

                //write end byte pattern
                bytePattern.WriteByte(ControlByteOne);
                bytePattern.WriteByte(ControlByteThree);
                bytePattern.WriteByte((byte)msgCount);

                //get final byte pattern
                badgeBytes = bytePattern.ToArray();
            }

            //write bytes to the badge
            this.WriteBytes(badgeBytes);
        }
        /// <summary>
        /// Clears all the messages from the badge
        /// </summary>
        public void ClearMessages()
        {
            //Sets the messages flag to 0000 0000 = no messages enabled
            this.EnableMessages(0x00);
        }
        /// <summary>
        /// Enables messages on the badge based on a message pattern
        /// 0x00 = No messages
        /// 0xFF = All messages 6 character string + 2 image
        /// </summary>
        /// <param name="messagePattern"></param>
        public void EnableMessages(byte messagePattern)
        {
            byte[] enablePattern = new byte[] { 0x00, ControlByteOne, ControlByteThree, messagePattern };
            this.WriteBytes(enablePattern);
        }
        /// <summary>
        /// Creates the message byte pattern expected by the Prolific LED Badge
        /// </summary>
        /// <param name="msg">A string containing the message, must be less/== 250 chars</param>
        /// <param name="msgId">An int identifying which message this is</param>
        /// <param name="msgStyle">A MessageStyle value (hold, snowing, etc)</param>
        /// <param name="msgSpeed">A MessageSpeed (one through five)</param>
        /// <returns>Message byte array</returns>
        private byte[] CreateMessageBytePattern(string msg, int msgId, MessageStyle msgStyle, MessageSpeed msgSpeed)
        {
            //Get the message offsets to use
            MessageOffsetFirst offset1  = MessageOffsetFirst.One;
            MessageOffsetSecond offset2 = MessageOffsetSecond.One;
            switch(msgId)
            {
                case 0:
                    offset1 = MessageOffsetFirst.One;
                    offset2 = MessageOffsetSecond.One;
                    break;
                case 1:
                    offset1 = MessageOffsetFirst.Two;
                    offset2 = MessageOffsetSecond.Two;
                    break;
                case 2:
                    offset1 = MessageOffsetFirst.Three;
                    offset2 = MessageOffsetSecond.Three;
                    break;
                case 3:
                    offset1 = MessageOffsetFirst.Four;
                    offset2 = MessageOffsetSecond.Four;
                    break;
                case 4:
                    offset1 = MessageOffsetFirst.Five;
                    offset2 = MessageOffsetSecond.Five;
                    break;
                case 5:
                    offset1 = MessageOffsetFirst.Six;
                    offset2 = MessageOffsetSecond.Six;
                    break;
            }

            //ensure length is within bounds
            if(msg.Length > MaxLength)
                msg = msg.Substring(0, MaxLength);

            //Assuming English and using ASCIIEncoding
            byte[] msgBytes = ASCIIEncoding.ASCII.GetBytes(msg);
            byte msgLength  = Convert.ToByte(msgBytes.Length);

            //Initialize data and looping counters
            byte[] msgData;
            int msgBytesRead = 0;
            byte messageBatch = 0x00;
            using (MemoryStream ms = new MemoryStream())
            {
                //Build the 4 message groups
                for (int group = 0; group < 4; group++)
                {
                    //checksum for each group
                    //calculated by: (control bits - 0x02 + data) % 0x100
                    uint checksum = 0;

                    switch (group)
                    {
                        case 0:

                            ms.WriteByte(ControlByteOne);
                            ms.WriteByte(ControlByteTwo);
                            ms.WriteByte((byte)offset1);
                            ms.WriteByte(messageBatch);
                            ms.WriteByte((byte)msgSpeed);
                            ms.WriteByte((byte)offset2);
                            ms.WriteByte((byte)msgStyle);
                            ms.WriteByte(msgLength);
                            checksum += (uint)(ControlByteTwo + (byte)offset1 + messageBatch + (byte)msgSpeed + (byte)offset2 + (byte)msgStyle + msgLength);
                            //write data for this message - only 60 chars/bytes because of control bytes
                            for (int m = 0; m < 60; m++)
                            {
                                //write message data
                                if (msgBytesRead < msgBytes.Length)
                                {
                                    byte current = msgBytes[msgBytesRead];
                                    ms.WriteByte(current);
                                    checksum += (uint)current;

                                    msgBytesRead++;
                                }
                                else
                                {
                                    ms.WriteByte(PaddingByte);
                                }
                            }
                            ms.WriteByte((byte)(checksum % 256));
                            break;
                        case 1:
                        case 2:
                            ms.WriteByte(ControlByteOne);
                            ms.WriteByte(ControlByteTwo);
                            ms.WriteByte((byte)offset1);
                            ms.WriteByte(messageBatch);
                            checksum += (uint)(ControlByteTwo + (byte)offset1 + messageBatch);
                            //write data for this message - 64 chars/bytes since no extra control bits
                            for (int m = 0; m < 64; m++)
                            {
                                //write message data
                                if (msgBytesRead < msgBytes.Length)
                                {
                                    byte current = msgBytes[msgBytesRead];
                                    ms.WriteByte(current);
                                    checksum += (uint)current;

                                    msgBytesRead++;
                                }
                                else
                                {
                                    ms.WriteByte(PaddingByte);
                                }
                            }
                            ms.WriteByte((byte)(checksum % 256));
                            break;
                        case 3:
                            ms.WriteByte(ControlByteOne);
                            ms.WriteByte(ControlByteTwo);
                            ms.WriteByte((byte)offset1);
                            ms.WriteByte(messageBatch);
                            checksum += (uint)(ControlByteTwo + (byte)offset1 + messageBatch);
                            //write data for this message - 62 chars/bytes
                            for (int m = 0; m < 62; m++)
                            {
                                //write message data
                                if (msgBytesRead < msgBytes.Length)
                                {
                                    byte current = msgBytes[msgBytesRead];
                                    ms.WriteByte(current);
                                    checksum += (uint)current;

                                    msgBytesRead++;
                                }
                                else
                                {
                                    ms.WriteByte(PaddingByte);
                                }
                            }
                            ms.WriteByte(PaddingByte);
                            ms.WriteByte(PaddingByte);
                            ms.WriteByte((byte)(checksum % 256));
                            break;
                    }
                    messageBatch += 0x40;
                }

                msgData = ms.ToArray();
            }

            return msgData;
        }
        /// <summary>
        /// LED Badge Message Scrolling/Update speed
        /// One is Slowest, Five is Fastest
        /// </summary>
        public enum MessageSpeed
        {
            One = 0x31,
            Two = 0x32,
            Three = 0x33,
            Four = 0x34,
            Five = 0x35
        }
        /// <summary>
        /// LED Badge Message Styles
        /// </summary>
        public enum MessageStyle
        {
            Hold = 0x41,
            Scrolling = 0x42,
            RainDown = 0x43,
            Flash = 0x44
        }
        /// <summary>
        /// LED Badge first message offset used to set a numbered message
        /// MessageOffsetFirst.One = first message's first offset
        /// </summary>
        public enum MessageOffsetFirst
        {
            One = 0x06,
            Two = 0x07,
            Three = 0x08,
            Four = 0x09,
            Five = 0x0A,
            Six = 0x0B
        }
        /// <summary>
        /// LED Badge second message offset used to set a numbered message
        /// MessageOffsetSecond.One = first message's second offset
        /// </summary>
        public enum MessageOffsetSecond
        {
            One = 0x31,
            Two = 0x32,
            Three = 0x33,
            Four = 0x34,
            Five = 0x35,
            Six = 0x36
        }
        /// <summary>
        /// LEDMessage - Encapsulates an LED Message for the Prolific LED Badge
        /// </summary>
        public class LEDMessage
        {
            public string Message {get; set;}       //message text
            public MessageStyle Style {get; set;}   //message style - hold, snow, scrolling
            public MessageSpeed Speed {get; set;}   //message speed - one through five (one is slow)

            //Defaults
            public static int MAXLENGTH = 250;  
            public static MessageStyle DEFAULTSTYLE = MessageStyle.Scrolling;
            public static MessageSpeed DEFAULTSPEED = MessageSpeed.One;

            public LEDMessage(string message)
            {
                //only allow messages of MAXLENGTH
                if(message.Length > MAXLENGTH)
                    message = message.Substring(0, (MAXLENGTH - 1));

                this.Message = message;
                this.Style = DEFAULTSTYLE;
                this.Speed = DEFAULTSPEED;
            }
        }
    }
}
