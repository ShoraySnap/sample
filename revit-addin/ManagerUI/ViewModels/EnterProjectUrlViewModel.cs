using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagerUI.ViewModels
{
    public class EnterProjectUrlViewModel
    {
        public ICommand BackCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }
        public EnterProjectUrlViewModel()
        {
                
        }
    }
}
