using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
//public class MessagesHistory : IMessagesHistory
//{
//    readonly object sync = new object();
//    readonly List<CommunicationData> history = new List<CommunicationData>();
//    readonly Stopwatch watch = new Stopwatch();
//    public event EventHandler? Updated;
//    public void Start()
//    {
//        watch.Start();
//        lock (sync)
//        {
//            history.Clear();
//        }
//    }
//    public int AddCommand(uint sequence, IViceCommand? command)
//    {
//        var data = new CommunicationData(sequence, command, default, watch.ElapsedTicks, default,
//            ImmutableArray<ViceResponse>.Empty);
//        int index;
//        lock (sync)
//        {
//            history.Add(data);
//            index = history.Count;
//        }
//        Updated?.Invoke(this, EventArgs.Empty);
//        return index;
//    }
//    public void UpdateWithLinkedResponse(int id, ViceResponse response)
//    {

//    }
//    public void UpdateWithResponse(int id, ViceResponse response)
//    {
//        lock (sync)
//        {
//            //if (id.HasValue)
//            //{
//            //    var data = history[id.Value];
//            //    data = data with
//            //    {
//            //        Response = response,
//            //        Elapsed = watch.ElapsedTicks - data.StartTime,
//            //    };
//            //    history[id.Value] = data;
//            //}
//            //else
//            //{
//            //    history.Add(new CommunicationData(null, null, response, watch.ElapsedTicks, null));
//            //}
//        }
//    }

//    public void AddsResponseOnly(ViceResponse response)
//    {
//        throw new NotImplementedException();
//    }
//}
