using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace TypingAgent
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(RemoteAgentTag))]
    [ContentType("text")]
    [ContentType("any")]
    internal class AgentTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer != textView.TextBuffer)
                return null;
            
            return GetTaggerForView(textView) as ITagger<T>;
        }

        public static AgentTagger GetTaggerForView(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(() => new AgentTagger(view.TextBuffer));
        }
    }

    /// <summary>
    /// Tracks TypingAgents and tags the areas that they type in.
    /// </summary>
    internal class AgentTagger : ITagger<RemoteAgentTag>
    {
        /// <summary>
        /// The tagger that will do the actual work (adding/removing tags, sending out change events, etc.)
        /// </summary>
        SimpleTagger<RemoteAgentTag> tagger;

        private ITextBuffer _buffer;

        Dictionary<string, List<TrackingTagSpan<RemoteAgentTag>>> agentTags = new Dictionary<string,List<TrackingTagSpan<RemoteAgentTag>>>();

        public AgentTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            tagger = new SimpleTagger<RemoteAgentTag>(buffer);
            tagger.TagsChanged += (sender, args) => TagsChanged(this, args);
        }

        public void AddAgent(TypingAgent agent)
        {
            agent.InsertionSpanUpdated += (sender, args) => tagger.CreateTagSpan(agent.InsertionSpan, agent.Tag);
            this.TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<RemoteAgentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return tagger.GetTags(spans);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
