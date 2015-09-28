
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

namespace BoxTestCSharp
{

    public class Program
    {
        static void Main(string[] args)
        {
            bool bstop = false;
            string boxip = "10.186.81.186";

            Thread t = new Thread(() =>
            {

                while ((!bstop))
                {
                    dynamic result_post = HandleBoxCmd("get_state", false, boxip, 1);

                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    BoxStateObject BoxStateObject = jss.Deserialize<BoxStateObject>(result_post);
                    Console.WriteLine(BoxStateObject.state);

                    dynamic BoxState = jss.Deserialize<object>(result_post);
                    Console.WriteLine(BoxState["state"]);

                    Console.WriteLine(result_post);

                    Thread.Sleep(1000);
                }

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
                    dynamic BoxStateObject = jss.Deserialize<BoxStateObject>(result_post);
                    result_post = HandleBoxCmd("release", true, BoxStateObject.products(0).jlid, boxip, 1);
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
                    if (response.StatusCode != HttpStatusCode.OK)
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
    }
}