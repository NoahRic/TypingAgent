using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using System.Windows.Threading;

namespace TypingAgent
{
    /// <summary>
    /// Manages the agent badges (names) displayed beside the text being typed.
    /// </summary>
    class AgentBadgeVisualManager
    {
        private IWpfTextView textView;
        private IAdornmentLayer layer;
        private ITagAggregator<RemoteAgentTag> aggregator;
        private IEditorFormatMap formatMap;

        public AgentBadgeVisualManager(IWpfTextView textView, IAdornmentLayer layer, ITagAggregator<RemoteAgentTag> tagAggregator, IEditorFormatMap formatMap)
        {
            this.textView = textView;
            this.layer = layer;
            this.aggregator = tagAggregator;
            this.formatMap = formatMap;

            textView.LayoutChanged += new EventHandler<TextViewLayoutChangedEventArgs>(textView_LayoutChanged);

            // Should use BatchedTagsChanged instead of TagsChanged + Dispatch, when it is available post-Beta 2
            aggregator.TagsChanged += new EventHandler<TagsChangedEventArgs>(aggregator_TagsChanged);
        }

        void textView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (var extent in e.NewOrReformattedSpans)
                RefreshExtent(extent);
        }

        void aggregator_TagsChanged(object sender, TagsChangedEventArgs e)
        {
            textView.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var span in e.Span.GetSpans(textView.TextSnapshot))
                    RefreshExtent(span);
            }), DispatcherPriority.Normal);
        }

        void RefreshExtent(SnapshotSpan extent)
        {
            layer.RemoveAdornmentsByVisualSpan(extent);

            ITextSnapshot snapshot = textView.TextSnapshot;

            foreach (var tag in aggregator.GetTags(extent))
            {
                var spans = tag.Span.GetSpans(snapshot);
                if (spans.Count != 1)
                    continue;

                var span = spans[0];

                Brush background = Brushes.Green;

                var properties = formatMap.GetProperties(tag.Tag.Name);
                if (properties != null)
                {
                    if (properties.Contains(MarkerFormatDefinition.BorderId) && properties[MarkerFormatDefinition.BorderId] is Pen)
                        background = ((Pen)properties[MarkerFormatDefinition.BorderId]).Brush;
                        
                }

                Label label = new Label() { Content = tag.Tag.Name, Foreground = Brushes.White, Background = background };
                Border border = new Border() { BorderThickness = new System.Windows.Thickness(1, 0, 1, 0), Child = label };

                var geometry = textView.TextViewLines.GetMarkerGeometry(span);
                if (geometry != null)
                {
                    Canvas.SetTop(border, geometry.Bounds.Top);
                    Canvas.SetLeft(border, geometry.Bounds.Right + 2);

                    layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, tag.Tag.Name, border, null);
                }
            }
        }
    }
}
