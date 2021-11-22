namespace BrightWire.ExecutionGraph.Node.Input
{
    /// <summary>
    /// Simple pass through of the input signal
    /// </summary>
    class FlowThrough : NodeBase
    {
        public FlowThrough(string name = null) : base(name)
        {
        }

        public override void ExecuteForward(IContext context)
        {
            _AddNextGraphAction(context, context.Data, null);
        }
    }
}
