import serial
import array

#import COMBadge base class functionality
from COMBadge import COMBadge
from COMBadge import COMBadgeNotFoundException

class ProlificLEDBadge(COMBadge):
    
    #constants
    StartByte        = 0x00
    PaddingByte      = 0x00
    ControlByteOne   = 0x02
    ControlByteTwo   = 0x31
    ControlByteThree = 0x33
    CheckSumMod      = 0x100        #256 in hex used get a 1-byte result

    MaxLength        = 250          #Maximum message length
    MaxMessages      = 6
    
    def __init__(self):
        super(COMBadge,self).__init__()
        
        self.BaudRate           = 38400
        self.Parity             = serial.PARITY_NONE
        self.WordSize           = 8
        self.Stop               = serial.STOPBITS_ONE
        self.QueryBadgeValue    = "Prolific"
    
    def Open(self):
        """Opens the Prolific LED Badge or throws an error"""
        badgePort = self.GetBadgePort()
        if(badgePort):
            self.ComPort = badgePort
        else:
            raise COMBadgeNotFoundException
    
    def Close(self):
        """ do nothing """

    def ClearMessages(self):
        """Clears all the messages from the badge - Sets the messages flag to 0000 0000 = no messages enabled"""
        self.EnableMessages(0x00)

    def EnableMessages(self, messagePattern):
        """ Enables messages on the badge based on a message pattern
            0x00 = No messages
            0xFF = All messages 6 character string + 2 image"""
        bytesToWrite = array.array('B')
        bytesToWrite.append(0x00)
        bytesToWrite.append(self.ControlByteOne)
        bytesToWrite.append(self.ControlByteThree)
        bytesToWrite.append(messagePattern)
        
        self.WriteBytes(bytesToWrite)

    def SetMessage(self, message):
        """Sets a message on the badge"""
        self.SetMessages([ message ])
        
    def SetMessages(self, messages):
        """Sets messages on the badge - up to six text messages"""
        
        #begin byte buffer and add starting bytes
        badgeBytes = bytearray()
        badgeBytes.append(self.StartByte)
        
        #get number of messages
        numMsgs = self.MaxMessages
        messagesLength = len(messages)
        if messagesLength < numMsgs :
            numMsgs = messagesLength
        
        #messages enabled flag
        msgCount = 0x00
        
        #for each message, write out the byte pattern
        for i in range(len(messages)):
            badgeBytes.extend(self.__CreateMessageBytePattern(messages[i].Message, i, messages[i].Style, messages[i].Speed))
            msgCount += 1
        
        #write end byte pattern
        badgeBytes.append(self.ControlByteOne)
        badgeBytes.append(self.ControlByteThree)
        badgeBytes.append(msgCount)
        
        for c in badgeBytes:
            print "'0x%X" % c        
        
        #write bytes to badge
        self.WriteBytes(badgeBytes)

    def __CreateMessageBytePattern(self, message, messageId, style, speed):
        """Creates the message byte pattern expected by the Prolific LED Badge"""
        
        #get message offsets
        offset1 = None
        offset2 = None
        if   messageId == 0:
            offset1 = MessageOffsetFirst.One
            offset2 = MessageOffsetSecond.One
        elif messageId == 1:
            offset1 = MessageOffsetFirst.Two
            offset2 = MessageOffsetSecond.Two
        elif messageId == 2:
            offset1 = MessageOffsetFirst.Three
            offset2 = MessageOffsetSecond.Three
        elif messageId == 3:
            offset1 = MessageOffsetFirst.Four
            offset2 = MessageOffsetSecond.Four
        elif messageId == 4:
            offset1 = MessageOffsetFirst.Five
            offset2 = MessageOffsetSecond.Five
        elif messageId == 5:
            offset1 = MessageOffsetFirst.Six
            offset2 = MessageOffsetSecond.Six

        #ensure length is within bounds
        if len(message) > self.MaxLength:
            message = message[1:self.MaxLength]
        
        #initialize buffer and looping counters
        messageBytes = array.array('B')
        msgBytesRead = 0
        messageBatch = 0x00
        
        for m in range(1, 5):
            checksum = 0x00
            
            if m == 1:
                messageBytes.append(self.ControlByteOne)
                messageBytes.append(self.ControlByteTwo)
                messageBytes.append(offset1)
                messageBytes.append(messageBatch)
                messageBytes.append(speed)
                messageBytes.append(offset2)
                messageBytes.append(style)
                messageBytes.append(len(message))
                checksum += self.ControlByteTwo + offset1 + messageBatch + speed + offset2 + style + len(message)
                
                for i in range(1, 61):
                    #write message data
                    if msgBytesRead < len(message):
                        current = ord(message[msgBytesRead])
                        messageBytes.append(current)
                        checksum += current
                        msgBytesRead += 1
                    else:
                        messageBytes.append(self.PaddingByte)
                
                messageBytes.append(checksum % 256)
                
            elif m == 2 or m == 3:
                messageBytes.append(self.ControlByteOne)
                messageBytes.append(self.ControlByteTwo)
                messageBytes.append(offset1)
                messageBytes.append(messageBatch)
                checksum += self.ControlByteTwo + offset1 + messageBatch
                
                for i in range (1, 65):
                    #write message data
                    if msgBytesRead < len(message):
                        current = ord(message[msgBytesRead])
                        messageBytes.append(current)
                        checksum += current
                        msgBytesRead += 1
                    else:
                        messageBytes.append(self.PaddingByte)
                
                messageBytes.append(checksum % 256)
            
            elif m == 4:
                messageBytes.append(self.ControlByteOne)
                messageBytes.append(self.ControlByteTwo)
                messageBytes.append(offset1)
                messageBytes.append(messageBatch)
                checksum += self.ControlByteTwo + offset1 + messageBatch
                
                for i in range (1, 63):
                    #write message data
                    if msgBytesRead < len(message):
                        current = ord(message[msgBytesRead])
                        messageBytes.append(current)
                        checksum += current
                        msgBytesRead += 1
                    else:
                        messageBytes.append(self.PaddingByte)
                
                messageBytes.append(self.PaddingByte)
                messageBytes.append(self.PaddingByte)
                messageBytes.append(checksum % 256)
            
            #increment message batch
            messageBatch += 0x40
        
        return messageBytes
    
#Helpers

class MessageSpeed(object):
    """LED Badge Message Scrolling/Update speed. One if slowest, Five is fastest"""
    One     = 0x31
    Two     = 0x32
    Three   = 0x33
    Four    = 0x34
    Five    = 0x35

class MessageStyle(object):
    """LED Badge Message Styles"""
    Hold        = 0x41
    Scrolling   = 0x42
    RainDown    = 0x43
    Flash       = 0x44

class MessageOffsetFirst(object):
    """LED Badge first message offset used to set a numbered message - MessageOffsetFirst.One = first message's first offset"""
    One     = 0x06
    Two     = 0x07
    Three   = 0x08
    Four    = 0x09
    Five    = 0x0A
    Six     = 0x0B
    
class MessageOffsetSecond(object):
    """LED Badge second message offset used to set a numbered message - MessageOffsetSecond.One = first message's second offset"""
    One     = 0x31
    Two     = 0x32
    Three   = 0x33
    Four    = 0x34
    Five    = 0x35
    Six     = 0x36

class LEDMessage(object):
    """LEDMessage - Encapsulates an LED Message for the Prolific LED Badge"""
    
    #constants
    MAXLENGTH = 250
    DEFAULTSTYLE = MessageStyle.Scrolling
    DEFAULTSPEED = MessageSpeed.One

    def __init__(self, message):
        self.Message    = message
        self.Style      = self.DEFAULTSTYLE
        self.Speed      = self.DEFAULTSPEED

        
        