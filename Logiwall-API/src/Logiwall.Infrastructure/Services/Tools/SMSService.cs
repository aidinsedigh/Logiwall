using System.Net;
using System.Text;
using Logiwall.Application.Abstractions.Tools;
using Logiwall.Server.Data;
using Newtonsoft.Json;
using RestSharp;

namespace Logiwall.Infrastructure.Services.Tools;
public class SMSService : ISMSService
{
    private readonly AppDbContext dbContext;

    public SMSService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
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

    public string SendSMSIPPanel(string Message, string Destination)
    {
        //var client = new RestClient("http://188.0.240.110/api/select");
        //var request = new RestRequest(Method.POST);
        //request.AddHeader("cache-control", "no-cache");
        //request.AddHeader("Content-Type", "application/json");
        //request.AddParameter("undefined", "{\"op\" : \"pattern\"" +
        //    ",\"user\" : \"fsamin\"" +
        //    ",\"pass\":  \"F.samin@123\"" +
        //    ",\"fromNum\" : \"+983000505\"" +
        //    ",\"toNum\": \"09125189131\"" +
        //    ",\"patternCode\": \"nqx8akt8g4\"" +
        //    ",\"inputData\" : [{\"validation_code\": \"13131\"}]}"
        //    , ParameterType.RequestBody);
        //IRestResponse response = client.Execute(request);
        //return response.Content;


        var client = new RestClient("http://188.0.240.110/api/select");
        var request = new RestRequest(string.Empty, method: Method.Post);
        request.AddHeader("cache-control", "no-cache");
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("undefined", "{\"op\" : \"send\"" +
            ",\"uname\" : \"fsamin\"" +
            ",\"pass\":  \"F.samin@123\"" +
            ",\"message\" : \"{{Message}}}\"" +
            ",\"from\": \"+983000505\"" +
            ",\"to\" : [\"09125189131\"]}"
            , ParameterType.RequestBody);
        var response = client.Execute(request);
        return response.Content;

    }

    public string SendSMSPersiaFava(string Message, string DestinationAddress)
    {
        string[] RetValue = new string[2];
        RetValue[0] = "False";
        RetValue[1] = "0";
        var request = (HttpWebRequest)WebRequest.Create("http://sms.persiafava.com/webservice/rest/sms_send?");

        var postData = string.Format("api_key=309:65336955f26ea&receiver_number={0}&note_arr[]={1}&sender_number={2}", DestinationAddress, Message, "300084571");
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


    //public async Task SaveSMSLog(string mobileNo, string message, string smsSate, string contractCode, string branchName, string retValue1, string retValue2, string sourceDestination)
    //{
    //    using (IDependencyResolver dependencyResolver = dependencyManager.CreateChildDependencyResolver())
    //    {
    //        IRepository<SmsLog> smsLogRepository = dependencyResolver.Resolve<IRepository<SmsLog>>();
    //        var unit = dependencyResolver.Resolve<IUnitOfWork>();
    //        SmsLog _smsLog = new SmsLog();
    //        _smsLog.MobileNo = mobileNo;
    //        _smsLog.Message = message;
    //        _smsLog.State = smsSate;
    //        _smsLog.ContractCode = contractCode;
    //        _smsLog.BranchName = branchName;
    //        _smsLog.RetValue1 = retValue1;
    //        _smsLog.RetValue2 = retValue2;
    //        _smsLog.InternalTime = DateTimeOffset.Now;
    //        _smsLog.SourceNumber = sourceDestination;

    //        await smsLogRepository.AddAsync(_smsLog, CancellationToken.None);
    //        unit.CommitWork();
    //    }
    //}

}
