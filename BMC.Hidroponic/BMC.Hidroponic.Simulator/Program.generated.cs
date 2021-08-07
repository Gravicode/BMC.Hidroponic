//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BMC.Hidroponic.Simulator {
    using Gadgeteer;
    using GTM = Gadgeteer.Modules;
    
    
    public partial class Program : Gadgeteer.Program {
        
        /// <summary>The XBee Adapter module using socket 5 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.XBeeAdapter xBeeAdapter;
        
        /// <summary>The Display NHVN module using sockets 3, 2 and 1 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.DisplayNHVN displayNHVN;
        
        /// <summary>This property provides access to the Mainboard API. This is normally not necessary for an end user program.</summary>
        protected new static GHIElectronics.Gadgeteer.FEZCobraIIEco Mainboard {
            get {
                return ((GHIElectronics.Gadgeteer.FEZCobraIIEco)(Gadgeteer.Program.Mainboard));
            }
            set {
                Gadgeteer.Program.Mainboard = value;
            }
        }
        
        /// <summary>This method runs automatically when the device is powered, and calls ProgramStarted.</summary>
        public static void Main() {
            // Important to initialize the Mainboard first
            Program.Mainboard = new GHIElectronics.Gadgeteer.FEZCobraIIEco();
            Program p = new Program();
            p.InitializeModules();
            p.ProgramStarted();
            // Starts Dispatcher
            p.Run();
        }
        
        private void InitializeModules() {
            this.xBeeAdapter = new GTM.GHIElectronics.XBeeAdapter(5);
            this.displayNHVN = new GTM.GHIElectronics.DisplayNHVN(3, 2, 1, Socket.Unused, Socket.Unused);
        }
    }
}
