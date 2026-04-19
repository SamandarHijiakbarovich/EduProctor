using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services.Settings;

public class SmsSettings
{
    public string Provider { get; set; } = "twilio"; // twilio, eskiz, etc.
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
