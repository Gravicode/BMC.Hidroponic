using System;
using Microsoft.SPOT;
using System.IO;
using System.Text;

namespace BMC.Hidroponic.Device
{
    public class LogHelper
    {
        Gadgeteer.Modules.GHIElectronics.USBHost usbHost;
        public LogHelper(Gadgeteer.Modules.GHIElectronics.USBHost usbHost1)
        {
            this.usbHost = usbHost1;
        }
        public void WriteLogs(string Message)
        {
            var MessageStr = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " => " + Message;
            var PathStr = "\\USB\\Logs";
            var LogName = "log_" + DateTime.Now.ToString("dd_MMM_yyyy") + ".txt";
            if (usbHost.IsMassStorageConnected && usbHost.IsMassStorageMounted)
            {
                if (IsFileExist(PathStr, LogName))
                {
                    /*
                    using (MemoryStream ms = new MemoryStream())
                    {
                        var ExistingData =usbHost.MassStorageDevice.ReadFile(PathStr + "\\" + LogName);
                        ms.Write(ExistingData ,0, ExistingData.Length);
                        var newText = "New Line!\r\n";
                        ms.Write(Encoding.UTF8.GetBytes(newText), 0, newText.Length);
                        usbHost.MassStorageDevice.WriteFile(PathStr + "\\" + LogName, ms.ToArray());
                       
                    }*/

                    using (FileStream stream = new FileStream(PathStr + "\\" + LogName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (TextWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(MessageStr);
                        writer.Flush();
                        writer.Close();
                    }

                }
                else
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(Encoding.UTF8.GetBytes(MessageStr + "\r\n"), 0, MessageStr.Length + 2);
                        Debug.Print(usbHost.MassStorageDevice.RootDirectory);
                        usbHost.MassStorageDevice.CreateDirectory(PathStr);
                        usbHost.MassStorageDevice.WriteFile(PathStr + "\\" + LogName, ms.ToArray());
                        /*
                        var files = usbHost.MassStorageDevice.ListFiles(PathStr);
                        foreach (var item in files)
                        {
                            Debug.Print(item);
                        }*/
                    }
                }
            }
        }

        bool IsFileExist(string Path, string FileName)
        {
            if (usbHost.IsMassStorageConnected && usbHost.IsMassStorageMounted)
            {
                var files = usbHost.MassStorageDevice.ListFiles(Path);
                foreach (var item in files)
                {
                    var fname = System.IO.Path.GetFileName(item);

                    if (fname.ToLower() == FileName.ToLower())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
