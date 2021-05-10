using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class RegistersViewModel: NotifiableObject
    {
        readonly ILogger<RegistersViewModel> logger;
        readonly IViceBridge viceBridge;
        readonly RegistersMapping mapping;
        public Registers6510 Current { get; private set; } = Registers6510.Empty;
        public Registers6510 Previous { get; private set; } = Registers6510.Empty;
        public RegistersViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, RegistersMapping mapping)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.mapping = mapping;
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
        }
        public async Task InitAsync()
        {
            var request = viceBridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory));
            var response = await request.Response;
            mapping.Init(response);

        }
        void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
        {
            switch (e.Response)
            {
                case RegistersResponse registerResponse:
                    UpdateRegistersFromResponse(registerResponse);
                    break;
            }
        }
        void UpdateRegistersFromResponse(RegistersResponse response)
        {
            if (mapping.IsMappingAvailable)
            {
                Previous = Current;
                Current = mapping.MapValues(response);
            }
        }
        public void Reset()
        {
            mapping.Clear();
            Current = Registers6510.Empty;
            Previous = Registers6510.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            }
            base.Dispose(disposing);
        }
    }
}
