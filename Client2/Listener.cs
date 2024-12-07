using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;

namespace Client2
{
    public class Listener
    {
        public Listener() 
        {
        }

        public void Start()
        {
            //Process.Start("cmd.exe");

        }

        public void AddNew(string Name, string ip, string port, string password)
        {
            string info = String.Join(Name, ip, port, password);

            MessageBox.Show(info);

        }

    }
}
