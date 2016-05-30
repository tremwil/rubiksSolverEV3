using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Management;
using ConsoleExtender;

namespace RubiksSolveEV3
{
    struct SmallPortData
    {
        public string DeviceID, PNPDeviceAdress;

        public SmallPortData(string Caption, string pnpDeviceID)
        {
            // Caption looks like "Standard Serial over Bluethoot (COM 37)"
            // We want the "COM 37" part.
            DeviceID = Caption.Split('(').Last().TrimEnd(')', ' ');

            // PNPDeviceID looks like "BTHENUM\{bunch-of-stuff}\xxxx&xxxx&xxxx&adress_xxxxxxx" 
            // We want to get the "adress" part.
            PNPDeviceAdress = pnpDeviceID.Split('&').Last().Split('_').First();
        }
    }

    class PortInfo
    {
        public static ManagementBaseObject[] getPnpEntities()
        {
            using (var searcher = new ManagementObjectSearcher
                ("SELECT * FROM WIN32_PnPEntity"))
            {
                return searcher.Get().Cast<ManagementBaseObject>().ToArray();
            }
        }

        // Getting serials from Win32_PnPEntity is the only way to to this without
        // killing the COM port for ~5 minutes. I don't know why this is necessary.
        // It's also faster because it only does one query.
        public static SmallPortData[] getSerialPortsFromPNP(ManagementBaseObject[] pnps)
        {
            var ports = new List<SmallPortData>();
            string caption;
            string pnpdev;

            foreach (var pnpobj in pnps)
            {
                caption = (pnpobj["Caption"] ?? "").ToString();
                pnpdev = (pnpobj["PNPDeviceID"] ?? "").ToString();

                if (caption.Contains("(COM"))
                {
                    ports.Add(new SmallPortData(caption, pnpdev));
                }
            }

            return ports.ToArray();
        }

        public static string getSerialPortID(string robotName, bool info = true)
        {
            // First get pnp entity of caption robotName:
            var pnps = getPnpEntities();
            var robots = (from m in pnps
                          where (m["Caption"] ?? "\n").ToString() == robotName
                          select m);

            // If no robot found return error 1
            if (robots.Count() == 0) return "Error 1";
            // If more than one robot found return error 2
            if (robots.Count() >= 2) return "Error 2";
            var robot = robots.First();

            // Inform user
            if (info) ConsoleHelper.WriteLineColor("Robot found. Getting serial port...", ConsoleColor.DarkCyan);

            // Adress of robot in registry
            var robotAdress = robot["PNPDeviceID"].ToString().Split('_').Last();

            // Then get serial ports
            var ports = (from p in getSerialPortsFromPNP(pnps)
                        where p.PNPDeviceAdress == robotAdress
                        select p.DeviceID);

            // If one correct return it
            if (ports.Count() == 1) return ports.First();

            // If none found return error 3
            return "Error 3";
        }
    }
}
