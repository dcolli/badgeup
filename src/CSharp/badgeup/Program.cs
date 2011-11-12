namespace badgeup
{
    using System;
    using System.IO;
    using System.IO.Ports;
    using System.Text;
    using badge;

    /// <summary>
    /// Main - Simple test program which uses the ProlificLEDBadge class to set two messages
    /// </summary>
    class Program
    {        
        static void Main(string[] args)
        {
            try
            {
                ProlificLEDBadge badge = new ProlificLEDBadge();
                badge.Open();

                Console.WriteLine(badge.GetConfigString());

                ProlificLEDBadge.LEDMessage msg = new ProlificLEDBadge.LEDMessage("testing1");
                msg.Speed = ProlificLEDBadge.MessageSpeed.Five;
                ProlificLEDBadge.LEDMessage msg2 = new ProlificLEDBadge.LEDMessage("testing2");
                msg2.Speed = ProlificLEDBadge.MessageSpeed.Five;
                msg2.Style = ProlificLEDBadge.MessageStyle.Flash;

                badge.SetMessages(new ProlificLEDBadge.LEDMessage[] {msg, msg2});

                badge.Close();
            }
            catch (Exception ex)
            {
                //error logging - todo
                throw;
            }
            Console.Write("Press any key to exit");
            Console.ReadKey(false);
        }
    }
}
