using System.Net;
using System.Text;
using Logiwall.Application.Abstractions.Tools;
using Logiwall.Server.Data;
using Newtonsoft.Json;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Services.Tools;
public class SMSService : ISMSService
{
    private readonly AppDbContext dbContext;
    private readonly SmsOptions smsOptions;

    public SMSService(AppDbContext dbContext, IOptions<SmsOptions> smsOptions)
    {
        this.dbContext = dbContext;
        this.smsOptions = smsOptions.Value;
    }

    public class SMSPersiaFavaResult
    {
        public bool result { get; set; }
        public string error { get; set; }

    }
    public string SendSMS(string Message, string Destination)
    {
        return SendSMSPersiaFava(Message, Destination);
    }

    public string SendSMSPersiaFava(string Message, string DestinationAddress)
    {
        string[] RetValue = new string[2];
        RetValue[0] = "False";
        RetValue[1] = "0";
        var request = (HttpWebRequest)WebRequest.Create(smsOptions.Url);

        var postData = $"api_key={Uri.EscapeDataString(smsOptions.ApiKey)}&receiver_number={Uri.EscapeDataString(DestinationAddress)}&note_arr[]={Uri.EscapeDataString(Message)}&sender_number={Uri.EscapeDataString(smsOptions.SenderNumber)}";
        var data = Encoding.UTF8.GetBytes(postData);

        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = data.Length;

        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        var response = (HttpWebResponse)request.GetResponse();
        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

        try
        {
            SMSPersiaFavaResult res = JsonConvert.DeserializeObject<SMSPersiaFavaResult>(responseString);
            RetValue[0] = res.result ? "True" : "False";
            RetValue[1] = res.error;
        }
        catch (Exception exp)
        {
            RetValue[1] = exp.Message + ";" + responseString;
        }
        if (RetValue[0] == "True")
            return "OK";
        else
            return RetValue[1].ToString();
    }

}
