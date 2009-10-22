using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace TypingAgent
{
    /// <summary>
    /// A tag representing a remote agent typing in a buffer.
    /// </summary>
    internal class RemoteAgentTag : TextMarkerTag
    {
        /// <summary>
        /// A list of pens/brushes to use for each collaborator
        /// </summary>
        static List<Tuple<Pen, Brush>> _brushes = new List<Tuple<Pen, Brush>>()
        {
            new Tuple<Pen, Brush>(new Pen(Brushes.DarkGreen, 1), Brushes.LightGreen),
            new Tuple<Pen, Brush>(new Pen(Brushes.DarkBlue, 1), Brushes.LightBlue),
            new Tuple<Pen, Brush>(new Pen(Brushes.Orange, 1), Brushes.Yellow),
            new Tuple<Pen, Brush>(new Pen(Brushes.DarkRed, 1), Brushes.LightSalmon)
        };

        static RemoteAgentTag()
        {
            foreach (var brushes in _brushes)
            {
                brushes.Item1.Freeze();
                brushes.Item2.Freeze();
            }
        }

        /// <summary>
        /// Create a remote agent with the given name
        /// </summary>
        public static RemoteAgentTag CreateRemoteAgent(string agentName, ITextView view, IEditorFormatMap formatMap)
        {
            Dictionary<string, RemoteAgentTag> existingAgents = GetExistingAgents(view);

            RemoteAgentTag agent;
            if (existingAgents.TryGetValue(agentName, out agent))
                return agent;

            var brushes = _brushes[existingAgents.Count % _brushes.Count];

            ResourceDictionary agentDictionary = new ResourceDictionary();
            agentDictionary.Add(MarkerFormatDefinition.BorderId, brushes.Item1);
            agentDictionary.Add(MarkerFormatDefinition.FillId, brushes.Item2);

            formatMap.AddProperties(agentName, agentDictionary);

            agent = new RemoteAgentTag(agentName);
            existingAgents[agentName] = agent;

            return agent;
        }

        public string Name { get; private set; }

        private static Dictionary<string, RemoteAgentTag> GetExistingAgents(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(RemoteAgentTag), () => new Dictionary<string, RemoteAgentTag>());
        }

        protected RemoteAgentTag(string agentName) : base(agentName) { Name = agentName; }
    }
}
