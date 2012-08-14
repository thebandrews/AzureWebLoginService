using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.Diagnostics.Management;
using System.Text;
using System.IO;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        /// <summary>
        /// m_fromUrl contains the url to which we are logging into.
        /// </summary>        
        private string m_formUrl = "https://r.espn.go.com/members/util/loginUser";

        /// <summary>
        /// Temp - used for testing until I have a database setup
        /// </summary>
        private static string m_user = "benapptest";

        /// <summary>
        /// Temp - used for testing until I have a database setup
        /// </summary>
        private static string m_password = "benapptest";

        /// <summary>
        /// Temp - storing cookies until I have a db setup
        /// </summary>
        private CookieCollection m_cookies;

        //
        // This appears to be the money shot POST which will log a user into espn:
        //
        // https://r.espn.go.com/members/util/loginUser?language=en&affiliateName=espn&parentLocation=&registrationFormId=espn&username=benapptest&password=benapptest        


        /// <summary>
        /// TODO - Move this to database and encrypt
        /// </summary>
        string m_formParams = string.Format("language=en&affiliateName=espn&parentLocation=&registrationFormId=espn");


        /// <summary>
        /// Current time.
        /// </summary>
        private static DateTime m_moment = DateTime.Now;


        /// <summary>
        /// Path to log files on local machine
        /// </summary>
        private string m_path = @"\temp\worker_role1_" + m_moment.Hour.ToString() + "." + m_moment.Minute.ToString() + "." + m_moment.Second.ToString() + ".log";


        /// <summary>
        /// Debug write logs - Not used in production.
        /// </summary>
        /// <param name="data"></param>
        private void writeLog(String data)
        {
            //
            // Setup local log fle.
            //
            if (!File.Exists(m_path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(m_path))
                {
                    sw.WriteLine(data);
                }
            }
            else
            {
                // Create a file to write to.
                using (StreamWriter sw = File.AppendText(m_path))
                {
                    sw.WriteLine(data);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginUrl">Login url</param>
        /// <param name="userName">User's login name</param>
        /// <param name="password">User's password</param>
        /// <param name="formParams">Optional string which contains any additional form params</param>
        /// <returns></returns>
        private bool LoginToSite(String loginUrl, String userName, String password, String formParams = null)
        {

            //
            // Setup formUrl string
            //
            string formUrl = "";
            bool result = false;

            if (!String.IsNullOrEmpty(formParams))
            {
                formUrl = formParams + "&";
            }
            formUrl += string.Format("username={0}&password={1}", userName, password);


            //
            // Now setup the POST request
            //
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(loginUrl);
            req.CookieContainer = new CookieContainer();
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";

            byte[] bytes = Encoding.ASCII.GetBytes(formUrl);
            req.ContentLength = bytes.Length;

            using (Stream os = req.GetRequestStream())
            {
                os.Write(bytes, 0, bytes.Length);
            }


            //
            // Read the response
            //
            string respString = "";

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                //
                // Gather cookie info
                //
                m_cookies = resp.Cookies;


                //
                // Do something with the response stream. As an example, we'll 
                // stream the response to the console via a 256 character buffer 
                //
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    Char[] buffer = new Char[256];
                    int count = reader.Read(buffer, 0, 256);
                    while (count > 0)
                    {
                        respString += new String(buffer, 0, count);
                        count = reader.Read(buffer, 0, 256);
                    }

                } // reader is disposed here

            }

            //writeLog(respString);

            //
            // If response string contains "login":"true" this indicates success
            //
            if (respString.Contains("{\"login\":\"true\"}"))
            {
                result = true;
            }


            return result;
        }


        /// <summary>
        /// TODO - Make this more specific
        /// </summary>
        public void ExtractData(String url)
        {
            //
            // Now setup the POST request
            //
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.CookieContainer = new CookieContainer();
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";

            //
            // Add the login cookies
            //
            foreach (Cookie cook in m_cookies)
            {
                //writeLog("**name = " + cook.Name + " value = " + cook.Value);
                req.CookieContainer.Add(cook);
            }


            //
            // Read the response
            //            
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                string respString = "";

                //
                // Do something with the response stream. As an example, we'll 
                // stream the response to the console via a 256 character buffer 
                //
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    Char[] buffer = new Char[256];
                    int count = reader.Read(buffer, 0, 256);
                    while (count > 0)
                    {
                        respString += new String(buffer, 0, count);
                        count = reader.Read(buffer, 0, 256);
                    }

                } // reader is disposed here

                //writeLog("----------------------------------");
                writeLog(respString);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public override void Run()
        {
            //
            // Setup local log fle.
            //
            Trace.WriteLine("WorkerRole1 entry point called", "Information");
            writeLog("**WorkerRole1 entry point called.");

            //
            // Login to site
            //
            LoginToSite(m_formUrl, m_user, m_password, m_formParams);
            ExtractData("http://games.espn.go.com/frontpage/football");

            int count2 = 0;
            while (true)
            {
                Thread.Sleep(3000);
                Trace.WriteLine("Working" + count2.ToString(), "Information");
                //writeLog("**Working.");
                count2++;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            ///////////////////////////////////////////////////////////////////
            //
            // Setup Logging
            //
            ///////////////////////////////////////////////////////////////////
            string wadConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(wadConnectionString));

            RoleInstanceDiagnosticManager roleInstanceDiagnosticManager = storageAccount.CreateRoleInstanceDiagnosticManager(RoleEnvironment.DeploymentId,
                                                                                                                             RoleEnvironment.CurrentRoleInstance.Role.Name,
                                                                                                                             RoleEnvironment.CurrentRoleInstance.Id);
            DiagnosticMonitorConfiguration config = roleInstanceDiagnosticManager.GetCurrentConfiguration();

            if (config == null)
            {
                config = DiagnosticMonitor.GetDefaultInitialConfiguration();
            }

            //
            // Capture logs to WADLogsTable every 5 minutes.
            //
            var transferTime = 5;

            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(transferTime);
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            roleInstanceDiagnosticManager.SetCurrentConfiguration(config);
            ///////////////////////////////////////////////////////////////////

            return base.OnStart();
        }
    }
}

