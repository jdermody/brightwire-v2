﻿namespace BrightWire.ExecutionGraph.Action
{
    /// <summary>
    /// Saves the current graph signal into named memory
    /// </summary>
    internal class SetMemory : IAction
    {
        string _id;

        public SetMemory(string id)
        {
            _id = id;
        }

        public void Initialise(string data)
        {
            _id = data;
        }

        public string Serialise()
        {
            return _id;
        }

        public IGraphData Execute(IGraphData input, IContext context)
        {
            context.ExecutionContext.SetMemory(_id, input.GetMatrix());
            return input;
        }
    }
}
