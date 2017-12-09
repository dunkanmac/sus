using System.ComponentModel;
using System.Configuration.Install;

namespace SUS
{
    [RunInstaller(true)]
    public partial class SuServiceInstaller : Installer
    {
        public SuServiceInstaller()
        {
            InitializeComponent();
        }
    }
}
