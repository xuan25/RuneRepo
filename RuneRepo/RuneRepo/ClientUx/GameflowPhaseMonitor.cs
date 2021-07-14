using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuneRepo.ClientUx
{
    class GameflowPhaseMonitor
    {
        public class PhaseChangedArgs : EventArgs
        {
            public string OldPhase { get; set; }
            public string NewPhase { get; set; }

            public PhaseChangedArgs(string oldPhase, string newPhase)
            {
                OldPhase = oldPhase;
                NewPhase = newPhase;
            }
        }

        public event EventHandler<PhaseChangedArgs> PhaseChanged;

        private Thread MonitingThread { get; set; }
        private bool IsMoniting { get; set; }
        private string LastPhase { get; set; }

        private RequestWrapper Wrapper { get; set; }

        public GameflowPhaseMonitor(RequestWrapper requestWrapper)
        {
            MonitingThread = null;
            IsMoniting = false;
            LastPhase = null;
            Wrapper = requestWrapper;
        }

        public void Start()
        {
            IsMoniting = true;

            if (MonitingThread != null)
                return;
            
            MonitingThread = new Thread(async () =>
            {
                while (IsMoniting)
                {
                    try
                    {
                        string phase = await Wrapper.GetGameflowPhaseAsync();
                        if (phase != LastPhase)
                        {
                            PhaseChanged?.Invoke(this, new PhaseChangedArgs(LastPhase, phase));
                        }
                        LastPhase = phase;
                    }
                    catch (RequestWrapper.NoClientException ex)
                    {

                    }
                    catch (System.Net.WebException ex)
                    {

                    }
                    Thread.Sleep(500);
                }
                MonitingThread = null;
            })
            {
                IsBackground = true,
                Name = "GameflowPhaseMonitor.MonitingThread"
            };
            MonitingThread.Start();
        }

        public void Stop()
        {
            IsMoniting = false;
        }
    }
}
