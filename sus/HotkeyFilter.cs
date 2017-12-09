//using System;
//using System.ComponentModel;
//using System.Runtime.InteropServices;
//using System.Windows.Forms;
//using System.Xml.Serialization;

//namespace SUS
//{
//    public class HotkeyFilter : IMessageFilter
//	{
//		private static int currentID;
//		private const int maximumID = 0xBFFF;

//        private Form form;
//		private Keys keyCode;
//        private bool shift;
//        private bool control;
//        private bool alt;
//		private bool windows;

//		[XmlIgnore]
//		private int id;
//		[XmlIgnore]
//		private bool registered;
//		[XmlIgnore]
//		private Control windowControl;

//		public event HandledEventHandler Pressed;

//		public HotkeyFilter() : this(Keys.None, false, false, false, false)
//		{
//		}
		
//		public HotkeyFilter(Keys keyCode, bool shift, bool control, bool alt, bool windows)
//		{
//            this.form = new Form();

//			this.keyCode = keyCode;
//			this.shift = shift;
//			this.control = control;
//			this.alt = alt;
//			this.windows = windows;

//            this.Reregister();

//            Application.AddMessageFilter(this);
//		}

//		~HotkeyFilter()
//		{
//			if (this.Registered)
//			    this.Unregister();
//		}

//		public HotkeyFilter Clone()
//		{
//			return new HotkeyFilter(this.keyCode, this.shift, this.control, this.alt, this.windows);
//		}

//        public bool GetCanRegister(Control windowControl)
//        {
//            try
//            {
//                if (!this.Register(windowControl))
//                    return false;

//                this.Unregister();
//                return true;
//            }
//            catch (Win32Exception)
//            {
//                return false;
//            }
//            catch (NotSupportedException)
//            {
//                return false;
//            }
//        }

//        public bool GetCanRegister(IntPtr windowControl)
//        {
//            try
//            {
//                if (!this.Register(windowControl))
//                    return false;

//                this.Unregister();
//                return true;
//            }
//            catch (Win32Exception)
//            {
//                return false;
//            }
//            catch (NotSupportedException)
//            {
//                return false;
//            }
//        }

//		public bool Register(Control windowControl)
//        {
//			if (this.registered)
//                throw new NotSupportedException("You cannot register a hotkey that is already registered");

//            if (this.Empty)
//			    throw new NotSupportedException("You cannot register an empty hotkey");

//            //int id = System.Threading.Interlocked.Increment(ref Hotkey.currentID);

//            this.id = HotkeyFilter.currentID;
//			HotkeyFilter.currentID = HotkeyFilter.currentID + 1 % HotkeyFilter.maximumID;

//			uint modifiers = (uint)((this.Alt ? KeyModifiers.Alt : 0) | (this.Control ? KeyModifiers.Control : 0) |
//							(this.Shift ? KeyModifiers.Shift : 0) | (this.Windows ? KeyModifiers.Windows : 0) /*| KeyModifiers.NoRepeat*/);

//			if (!SuManager.RegisterHotKey(windowControl.Handle, this.id, modifiers, keyCode))
//			{ 
//				if (Marshal.GetLastWin32Error() == SuManager.ERROR_HOTKEY_ALREADY_REGISTERED)
//                    throw new Exception($"Hotkey '{ToString()}' allready registered"); 
//				else
//                    throw new Win32Exception();
//			}

//			this.registered = true;
//			this.windowControl = windowControl;
//			return true;
//		}

//        public bool Register(IntPtr windowControl)
//        {
//            if (this.registered)
//                throw new NotSupportedException("You cannot register a hotkey that is already registered");

//            if (this.Empty)
//                throw new NotSupportedException("You cannot register an empty hotkey");

//            this.id = HotkeyFilter.currentID;
//            HotkeyFilter.currentID = HotkeyFilter.currentID + 1 % HotkeyFilter.maximumID;

//            uint modifiers = (uint)((this.Alt ? KeyModifiers.Alt : 0) | (this.Control ? KeyModifiers.Control : 0) |
//                            (this.Shift ? KeyModifiers.Shift : 0) | (this.Windows ? KeyModifiers.Windows : 0) /*| KeyModifiers.NoRepeat*/);

//            if (!SuManager.RegisterHotKey(windowControl, this.id, modifiers, keyCode))
//            {
//                if (Marshal.GetLastWin32Error() == SuManager.ERROR_HOTKEY_ALREADY_REGISTERED)
//                    throw new Exception($"Hotkey '{ToString()}' allready registered");
//                else
//                    throw new Win32Exception(); 
//            }

//            this.registered = true;
//            //this.windowControl = windowControl;
//            return true;
//        }

//		public void Unregister()
//		{
//			if (!this.registered)
//			    throw new NotSupportedException("You cannot unregister a hotkey that is not registered");
        
//			if (!this.windowControl.IsDisposed)
//			{
//				if (!SuManager.UnregisterHotKey(this.windowControl.Handle, this.id))
//				    throw new Win32Exception();
//			}

//			this.registered = false;
//			this.windowControl = null;
//		}

//		private void Reregister()
//		{
//			if (!this.registered)
//			    return;

//			Control windowControl = this.windowControl;

//			this.Unregister();
//			this.Register(windowControl);
//		}

//		public bool PreFilterMessage(ref Message message)
//		{
//			if (message.Msg != SuManager.WM_HOTKEY)
//			    return false;

//			if (this.registered && (message.WParam.ToInt32() == this.id))
//				return this.OnPressed();
//			else
//			    return false;
//		}

//		private bool OnPressed()
//		{
//			HandledEventArgs handledEventArgs = new HandledEventArgs(false);
//			this.Pressed?.Invoke(this, handledEventArgs);
//			return handledEventArgs.Handled;
//		}

//        public override string ToString()
//        {
//			if (this.Empty)
//			    return "(none)";

//			string keyName = Enum.GetName(typeof(Keys), this.keyCode);

//			switch (this.keyCode)
//			{
//				case Keys.D0:
//				case Keys.D1:
//				case Keys.D2:
//				case Keys.D3:
//				case Keys.D4:
//				case Keys.D5:
//				case Keys.D6:
//				case Keys.D7:
//				case Keys.D8:
//				case Keys.D9:
//					keyName = keyName.Substring(1);
//					break;
//				default:
//					break;
//			}

//            string modifiers = "";

//            if (this.shift)
//                modifiers += "Shift+";

//            if (this.control)
//                modifiers += "Control+";

//            if (this.alt)
//                modifiers += "Alt+";

//			if (this.windows)
//			    modifiers += "Windows+";

//            return modifiers + keyName;
//        }

//		public bool Empty { get => this.keyCode == Keys.None; }
//		public bool Registered { get => this.registered; }

//        public Keys KeyCode
//        {
//            get => this.keyCode;
//            set
//			{
//				this.keyCode = value;
//				this.Reregister();
//			}
//        }

//        public bool Shift
//        {
//            get => this.shift;
//            set 
//			{
//				this.shift = value;
//				this.Reregister();
//			}
//        }

//        public bool Control
//        {
//            get => this.control;
//            set
//			{ 
//				this.control = value;
//				this.Reregister();
//			}
//        }

//        public bool Alt
//        {
//            get => this.alt;
//            set
//			{ 
//				this.alt = value;
//				this.Reregister();
//			}
//        }

//		public bool Windows
//		{
//			get => this.windows;
//			set 
//			{
//				this.windows = value;
//				this.Reregister();
//			}
//		}
//    }
//}
