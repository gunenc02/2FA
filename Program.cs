using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.EntityFramework;


namespace sms
{
    internal class Program
    {
        static void Main()
        {

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 30000;
            timer.Elapsed += (sender, e) => Master(sender, e);
            timer.Enabled = true;

            Console.Read();
            timer.Stop();
            timer.Dispose();
            
        }      
        
        private static void Master(object sender, EventArgs e)
        {
            try
            {
                string connString = "";
                OracleConnection conn = new OracleConnection(connString);
                conn.Open();
                while (SearchSQL(conn));
                conn.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        
        }

        private static bool SearchSQL(OracleConnection conn)
        {

            // cmd1
            OracleCommand cmd1 = new OracleCommand();
            cmd1.CommandText = "SELECT METIN, TELEFON FROM SMS WHERE GONDERIM_DURUMU = 0 AND GONDERILECEK_TARIH < CURRENT_TIMESTAMP";
            cmd1.Connection = conn;

            string phoneNumber;
            string message;

            OracleDataReader reader = cmd1.ExecuteReader();
            if(reader.Read())
            {
                message = reader.GetString(0);
                phoneNumber = reader.GetString(1);
                cmd1.Dispose();
                MessageSender(conn, phoneNumber, message);
                return true;
            }

            cmd1.Dispose();
            return false;
        }

        private static void MessageSender(OracleConnection conn, string phoneNumber, string message)
        {
            //necessary datas for sms service providers 
            string username = "";
            string password = "";
            string vendor = "";
            string header = "";
            string[] tmp = { phoneNumber };
            SmsService.smsSoapClient sms = new SmsService.smsSoapClient();
            string response = sms.send_sms(username, password, vendor, header, message,  tmp, "", "", "");
            int index = response.IndexOf(':');
            string smsCode = response.Substring(index + 1);

            OracleCommand cmd4 = new OracleCommand();
            cmd4.CommandText = "UPDATE SMS SET GONDERIM_TARIHI = CURRENT_TIMESTAMP WHERE TELEFON =: telefon";
            cmd4.Connection = conn;
            cmd4.Parameters.Add(new OracleParameter("telefon", phoneNumber));
            cmd4.ExecuteNonQuery();
            cmd4.Dispose();
            sms.Close();    
            MarkSQL(conn, phoneNumber, smsCode);
        }


        private static void MarkSQL(OracleConnection conn,  string phoneNumber, string smsCode)
        {
            // cmd2
            OracleCommand cmd2 = new OracleCommand();
            cmd2.CommandText = "UPDATE SMS SET GONDERIM_DURUMU = :gonderimDurumu WHERE TELEFON =: telefon";
            cmd2.Connection = conn;

            cmd2.Parameters.Add(new OracleParameter("gonderimDurumu", 1));
            cmd2.Parameters.Add(new OracleParameter("telefon", phoneNumber));
            cmd2.ExecuteNonQuery();

            cmd2.Dispose();

            //cmd3
            OracleCommand cmd3 = new OracleCommand();
            cmd3.CommandText = "UPDATE SMS SET SMS_KODU =: smsKodu WHERE TELEFON =: telefon";
            cmd3.Connection = conn;

            cmd3.Parameters.Add(new OracleParameter("smsKodu", smsCode));
            cmd3.Parameters.Add(new OracleParameter("telefon", phoneNumber));
            cmd3.ExecuteNonQuery();

            cmd3.Dispose();

        }
    }
}
