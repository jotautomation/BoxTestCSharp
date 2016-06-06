using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.IO;
using Nancy.Extensions;
using Nancy.ModelBinding;

namespace BoxTestCSharp
{
 
    public class Server : NancyModule
    {
        public Server()
        {

            Post["boxstate/{serialnumber}"] = boxstate =>
            {
                Program.BoxStateObject state = this.Bind();
                Console.WriteLine(String.Format("{0}, {1}, {2}", state.state.ToString(), state.message_timestamp.ToString(), state.products[0].jlid.ToString()));
                return Response.AsJson("ok");
            };
        }
    }
}
