using System.ServiceProcess;

namespace SUS
{
    public partial class SuService : ServiceBase
    {
        public SuService()
        {
            InitializeComponent();
        }

        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);

            if (command == SuManager.SWITCH_USER_COMMAND)
            {
                SuManager.SwitchUser();
            }
        }
    }
}
