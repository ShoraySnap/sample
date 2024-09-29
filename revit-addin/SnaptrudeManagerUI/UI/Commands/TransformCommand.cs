using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Stores;
using SnaptrudeManagerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.Commands
{
    public class TransformCommand : CommandBase
    {
        private readonly TransformService transformService;

        public TransformCommand(TransformService transformService)
        {
            this.transformService = transformService;
        }

        public override void Execute(object parameter)
        {
            transformService.Transform();
        }
    }
}