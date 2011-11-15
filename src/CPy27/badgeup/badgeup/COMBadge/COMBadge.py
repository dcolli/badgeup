import serial
import SetupDeviceWrapper
import sys

class COMBadge(object):
    """ Abstract class to represent a COM-enabled LEDBadge with Properties and Methods for communication
        Uses pySerial to communicate with a serial device (http://pyserial.sourceforge.net/)
    """
            
    def __init__(self):
        self.ComPort            = None  #COM Port to use in communicating with this Badge.
        self.BaudRate           = None  #BAUD Rate to use in communicating with this Badge.
        self.Parity             = None  #Parity bit to use in communicating with this Badge.
        self.WordSize           = None  #Word size to use in communicating with this Badge.
        self.Stop               = None  #StopBits to use in communicating with this Badge.
        self.QueryBadgeValue    = None  #A string containing a regular expression to match against each COM port's name.
    
    def __str__(self):
        return "COM Port Name:%s|Baud Rate:%d|ParityBit:%s|StopBits:%s|Word Size:%d" % (self.ComPort, self.BaudRate, self.Parity, self.Stop, self.WordSize) 

    def WriteBytes(self, data):
        """Writes the data specified below to the COMBadge this class represents"""
        try:
            ser = serial.Serial(self.ComPort, self.BaudRate, self.WordSize, self.Parity, self.Stop)
            ser.write(data)
            ser.close()
        except:
            print "COMBadge: WriteBytes Error: ", sys.exc_info[0]
            raise COMBadgeCommunicationException, "COMBadge: WriteBytes Error"
    
    def GetBadgePort(self):
        """Queries the system's COM Ports to find a Badge that matches QueryBadgeValue case ignored. If more than one COMport matches the value, the first is returned."""
        return SetupDeviceWrapper.ComPortNameFromFriendlyNamePrefix(self.QueryBadgeValue)
    
class COMBadgeNotFoundException(Exception):
    """Exception for case where a COM Badge is not found"""
    def __init__(self):
        super(Exception,self).__init__()

class COMBadgeCommunicationException(Exception):
    """Exception for case where a COM Badge has some communication error"""
    def __init__(self):
        super(Exception,self).__init__()
        