using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace TypingAgent
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("csharp")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class ViewCreationLister : IWpfTextViewCreationListener
    {
        [Import]
        IEditorFormatMapService FormatMapService = null;

        [Import]
        IViewTagAggregatorFactoryService TagAggregatorService = null;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("AgentNameLayer")]
        [TextViewRole(PredefinedTextViewRoles.Editable)]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        AdornmentLayerDefinition layer = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            string desiredStartText = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;";

            string desiredEndText = @"{
	class Program
	{
		static void Main(string[] args)
		{
		}
	}
}
";
            string snapshotText = textView.TextBuffer.CurrentSnapshot.GetText();

            if (!snapshotText.StartsWith(desiredStartText) || !snapshotText.EndsWith(desiredEndText))
                return;

            var formatMap = FormatMapService.GetEditorFormatMap(textView);

            // Create a visual manager for the agent names
            var layer = textView.GetAdornmentLayer("AgentNameLayer");
            AgentBadgeVisualManager manager = new AgentBadgeVisualManager(textView, layer, TagAggregatorService.CreateTagAggregator<RemoteAgentTag>(textView), formatMap);

            Dispatcher dispatcher = textView.VisualElement.Dispatcher;

            // First agent, typing some text in Main
            ThreadPool.QueueUserWorkItem((state) =>
                {
                    Thread.Sleep(2000);

                    ITextSnapshot snapshot = textView.TextBuffer.CurrentSnapshot;

                    SnapshotPoint insertionPoint = snapshot.GetLineFromLineNumber(10).End;

                    // Type something with errors, to show that the user can fix it while this agent is typing
                    string text = @"
            string foo = args.Count.ToString();
            Console.WriteLine(""Foo is {1}"", foos);
            Thread.Sleep(10000000);
            Console.WriteLine(""I win!!!"");";

                    RemoteAgentTag tag = RemoteAgentTag.CreateRemoteAgent("Chris", textView, formatMap);

                    TypingAgent agent = new TypingAgent(tag, text, insertionPoint, dispatcher);

                    var tagger = AgentTaggerProvider.GetTaggerForView(textView);
                    tagger.AddAgent(agent);

                    agent.Start();
                });

            // Second agent, typing a comment above the namespace
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Thread.Sleep(3000);

                ITextSnapshot snapshot = textView.TextBuffer.CurrentSnapshot;

                SnapshotPoint insertionPoint = snapshot.GetLineFromLineNumber(4).End;
                string text = @"
/// <summary>
/// This is a namespace!
/// I really like namespaces :)
/// </summary>";

                RemoteAgentTag tag = RemoteAgentTag.CreateRemoteAgent("Michael", textView, formatMap);

                TypingAgent agent = new TypingAgent(tag, text, insertionPoint, dispatcher);

                var tagger = AgentTaggerProvider.GetTaggerForView(textView);
                tagger.AddAgent(agent);

                agent.Start();
            });

        }
    }
}
