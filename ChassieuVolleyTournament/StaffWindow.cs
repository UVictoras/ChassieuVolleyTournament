using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace ChassieuVolleyTournament
{
    internal class StaffWindow : Form
    {
        public StaffWindow()
        {
            Text = "Infos privées du tournoi";
            WindowState = FormWindowState.Maximized;
            Icon = new Icon("../../Images/ChassieuLogo.ico");
            BackColor = Color.FromArgb(10, 40, 80);
        }
    }
}
