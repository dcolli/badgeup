from COMBadge.ProlificLEDBadge import *

#open new badge
badge = ProlificLEDBadge()
badge.Open()
print badge

msg  = LEDMessage("Testing1")
msg2 = LEDMessage("Testing2")
badge.SetMessages([msg, msg2])
