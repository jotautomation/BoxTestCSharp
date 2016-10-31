
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading;
using Nancy;
using Nancy.Hosting.Self;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;

namespace BoxTestCSharp
{

    public class Program
    {
        static void Main(string[] args)
        {
            bool bstop = false;
            string boxip = "62.241.233.94"; // Enter the box IP here
            // If you are using multiple network connections, please update your own IP address at line 236

            NancyHost MyNancyHost = new NancyHost(new Uri("http://localhost:1234/"));
            try
            {
                MyNancyHost.Start();
            }
            catch (Nancy.Hosting.Self.AutomaticUrlReservationCreationFailureException) 
            {
                MessageBox.Show("Please run with Administrator privileges");
                Environment.Exit(0);
            }
            catch (Exception Ex)
            { throw Ex; }


            Thread t = new Thread(() =>
            {
                dynamic result_post = HandleBoxCmd("get_info", false, boxip, 1);
                JavaScriptSerializer jss = new JavaScriptSerializer();
                dynamic BoxInfo = jss.Deserialize<object>(result_post);

                Console.WriteLine(BoxInfo["serial_number"]);

                bool subscribed = false;
                dynamic subscriptionResult = HandleBoxSubscription(BoxInfo["serial_number"], true, boxip, 1);
                Console.WriteLine(subscriptionResult);

                /*
                while ((!bstop))
                {
                    if (!subscribed)
                    {
                        result_post = HandleBoxCmd("get_state", false, boxip, 1);

                        BoxStateObject BoxStateObject = jss.Deserialize<BoxStateObject>(result_post);
                        Console.WriteLine(BoxStateObject.state);

                        dynamic BoxState = jss.Deserialize<object>(result_post);
                        Console.WriteLine(BoxState["state"]);

                        Console.WriteLine(result_post);


                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }

                }
                */

                HandleBoxSubscription(BoxInfo["serial_number"], false, boxip, 1);

            });
            t.Start();


            while (true)
            {
                string result_post = "";
                dynamic input = Console.ReadLine();
                if (input.Equals("!"))
                {
                    bstop = true;
                    t.Join();
                    return;
                }

                if (input.Equals("st"))
                {
                    result_post = HandleBoxCmd("get_state", false, boxip, 1);
                }

                if (input.Equals("in"))
                {
                    result_post = HandleBoxCmd("initialize", false, boxip, 1);
                }

                if (input.Equals("o"))
                {
                    result_post = HandleBoxCmd("open", false, boxip, 1);
                }

                if (input.Equals("cn"))
                {
                    result_post = HandleBoxCmd("close", false, boxip, 1);
                }

                if (input.Equals("cp"))
                {
                    result_post = HandleBoxCmd("close", true, boxip, 1);
                }

                if (input.Equals("en"))
                {
                    result_post = HandleBoxCmd("engage", false, boxip, 1);
                }

                if (input.Equals("res"))
                {
                    result_post = HandleBoxCmd("reset", false, boxip, 1);
                }

                if (input.Equals("dis"))
                {
                    result_post = HandleBoxSetting("disabled", true, boxip, 1);
                }

                if (input.Equals("ena"))
                {
                    result_post = HandleBoxSetting("disabled", false, boxip, 1);
                }

                if (input.Equals("cali"))
                {
                    result_post = HandleBoxSetting("calibrated", true, boxip, 1);
                }

                if (input.Equals("uncali"))
                {
                    result_post = HandleBoxSetting("calibrated", false, boxip, 1);
                }

                if (input.Equals("calreq"))
                {
                    result_post = HandleBoxSetting("calibration_requested", true, boxip, 1);
                }

                if (input.Equals("uncalreq"))
                {
                    result_post = HandleBoxSetting("calibration_requested", false, boxip, 1);
                }

                if (input.Equals("re"))
                {
                    result_post = HandleBoxCmd("get_state", false, boxip, 1);
                    dynamic jss = new JavaScriptSerializer();
                    dynamic BoxState = jss.Deserialize<object>(result_post);
                    result_post = HandleBoxCmd("release", true, BoxState["products"][0]["jlid"], boxip, 1);
                }

                Console.WriteLine(result_post);

            }

        }

        public static string HandleBoxCmd(string cmd, bool withproduct, string jlid, string boxip, int slidenumber)
        {
            BoxCommand commandToBeSent = default(BoxCommand);
            Random rnd = new Random();

            String thejlid = null;
            if ((!withproduct))
            {
                thejlid = "";
            }
            else
            {
                if ((jlid.Equals("")))
                {
                    thejlid = rnd.Next(1000, 100000).ToString();
                }
                else
                {
                    thejlid = jlid;
                }
            }

            ProductInBoxInfo theproduct = new ProductInBoxInfo
            {
                jlid = thejlid,
                test_result = "not_tested"
            };
            commandToBeSent = new BoxCommand
            {
                cmd = cmd,
                products = new ProductInBoxInfo[] { theproduct }
            };
            String response = PostCmd(string.Format("http://{0}/slide{1}/command", boxip, slidenumber.ToString()), commandToBeSent);
            return response;

        }

        public static string HandleBoxCmd(string cmd, bool withproduct, string boxip, int slidenumber)
        {
            return HandleBoxCmd(cmd, withproduct, "", boxip, slidenumber);
        }

        public static string HandleBoxSetting(string SettingName, bool value, string boxip, int slidenumber)
        {

            object commandToBeSent = new
            {
                cmd = "change_setting",
                name = SettingName,
                value = value
            };


            String response = PostCmd(string.Format("http://{0}/slide{1}/command", boxip, slidenumber.ToString()), commandToBeSent);
            return response;

        }

        public static string HandleBoxSubscription(string boxid, bool subscribe, string boxip, int slidenumber)
        {
            string LocalIpAddress = GetLocalIPAddress();

            BoxCommandSubscription thesubscription = new BoxCommandSubscription()
            {
                cmd = (subscribe) ? "subscribe" : "unsubscribe",
                address = string.Format("http://{0}:1234/boxstate/{1}", LocalIpAddress, boxid),
                error_mode = "ignore"
            };

            String response = PostCmd(string.Format("http://{0}/slide{1}/command", boxip, slidenumber.ToString()), thesubscription);
            Console.WriteLine("Handled box subscription");
            return response;
        }

        private static string GetLocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();

        }

        public static string PostCmd(string postDestination, object objectToSend)
        {
            //Send the message
            var subrequest = (HttpWebRequest)WebRequest.Create(postDestination);
            subrequest.Method = "POST";
            subrequest.Timeout = 4000;

            try
            {
                using (var requestStream = subrequest.GetRequestStream())
                {
                    using (var requestStreamWriter = new System.IO.StreamWriter(requestStream))
                    {
                        JavaScriptSerializer ser = new JavaScriptSerializer();
                        requestStreamWriter.Write(ser.Serialize(objectToSend));
                    }
                }
                using (var response = (HttpWebResponse)subrequest.GetResponse())
                {
                    // todo: consider a more lenient check here?
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Console.WriteLine("command sent ok");
                        string theresponse = "";
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            theresponse = reader.ReadToEnd().ToString();
                        }
                        return theresponse;

                    }
                }
            }
            catch (System.Net.WebException ex1)
            {

            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public struct BoxStateObject
        {
            public string state;
            public ProductInBoxInfo[] products;
            public string error_message;
            public string error_description;
            public uint message_timestamp;
            public bool disabled;
            public bool calibrated;
            public bool calibration_requested;
        }

        public struct ProductInBoxInfo
        {
            public string jlid;
            public string test_result;
        }

        public struct BoxCommand
        {
            public string cmd;
            public ProductInBoxInfo[] products;
            public string scanner_secret;
        }

        public struct BoxCommandSubscription
        {
            public string cmd;
            public string address;
            public string error_mode;
            public string scanner_secret;
        }
    }
}