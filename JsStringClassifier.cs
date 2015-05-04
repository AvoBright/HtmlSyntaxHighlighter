using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Diagnostics;

namespace HtmlSyntaxHighlighter
{

    #region Provider definition

    [Export(typeof(IClassifierProvider))]
    [ContentType("JavaScript")]
    internal class HtmlClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IClassifierAggregatorService ClassifierAggregator = null;

        private static bool createdClassifier = false;

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            if (createdClassifier)
            {
                return null;
            }

            try
            {
                createdClassifier = true;
                return buffer.Properties.GetOrCreateSingletonProperty<HtmlClassifier>(delegate
                {
                    return new HtmlClassifier(ClassificationTypeRegistry, ClassifierAggregator.GetClassifier(buffer));
                });
            }
            finally
            {
                createdClassifier = false;
            }
            
        }
    }
    #endregion

    #region Classifier

    internal class HtmlClassifier : IClassifier
    {
        IClassificationType htmlDelimiterType;
        IClassificationType htmlElementType;
        IClassificationType htmlAttributeNameType;
        IClassificationType htmlQuoteType;
        IClassificationType htmlAttributeValueType;

        IClassifier classifier;

        internal HtmlClassifier(IClassificationTypeRegistryService registry, IClassifier classifier)
        {
            htmlDelimiterType = registry.GetClassificationType(FormatNames.Delimiter);
            htmlElementType = registry.GetClassificationType(FormatNames.Element);
            htmlAttributeNameType = registry.GetClassificationType(FormatNames.AttributeName);
            htmlQuoteType = registry.GetClassificationType(FormatNames.Quote);
            htmlAttributeValueType = registry.GetClassificationType(FormatNames.AttributeValue);

            this.classifier = classifier;
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();


            foreach (ClassificationSpan cs in classifier.GetClassificationSpans(span))
	        {
	            string cs_class = cs.ClassificationType.Classification.ToLower();
	
	            /* Only apply our rules if we found a string literal */
	            if (cs_class == "string")
	            {
                    if (cs.Span.Length > 2)
                    {
                        var sspan = new SnapshotSpan(cs.Span.Start.Add(1), cs.Span.End.Subtract(1)); // exclude quote

                        var classification = ScanLiteral(sspan);

                        if (classification != null)
                        {
                            result.AddRange(classification);
                        }
                    }
	            }

                result.Add(cs);
	        }

            return result;
        }

        private enum State
        {
            Default,
            AfterOpenAngleBracket,
            ElementName,
            InsideAttributeList,
            AttributeName,
            AfterAttributeName,
            AfterAttributeEqualSign,
            AfterOpenDoubleQuote,
            AfterOpenSingleQuote,
            AttributeValue,
            InsideElement,
            AfterCloseAngleBracket,
            AfterOpenTagSlash,
            AfterCloseTagSlash,
        }

        private bool IsNameChar(char c)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        private List<ClassificationSpan> ScanLiteral(SnapshotSpan span)
        {
            State state = State.Default;

            var result = new List<ClassificationSpan>();

            string literal = span.GetText();
            int currentCharIndex = 0;

            int? continuousMark = null;
            bool insideSingleQuote = false;
            bool insideDoubleQuote = false;

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                switch (state)
                {
                    case State.Default:
                        {
                            if (c != '<')
                            {
                                return null;
                            }
                            else
                            {
                                state = State.AfterOpenAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            break;
                        }
                    case State.AfterOpenAngleBracket:
                        {
                            if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.ElementName;
                            }
                            else if (c == '/')
                            {
                                state = State.AfterCloseTagSlash;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.ElementName:
                        {
                            if (IsNameChar(c))
                            {

                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), htmlElementType));
                                    continuousMark = null;
                                }
                                state = State.InsideAttributeList;
                            }
                            else if (c == '>')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), htmlElementType));
                                    continuousMark = null;
                                }

                                state = State.AfterCloseAngleBracket;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else if (c == '/')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), htmlElementType));
                                    continuousMark = null;
                                }

                                state = State.AfterOpenTagSlash;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.InsideAttributeList:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeName;
                            }
                            else if (c == '>')
                            {
                                state = State.AfterCloseAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else if (c == '/')
                            {
                                state = State.AfterOpenTagSlash;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AttributeName:
                        {
                            if (char.IsWhiteSpace(c))
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), htmlAttributeNameType));
                                    continuousMark = null;
                                }
                                state = State.AfterAttributeName;
                            }
                            else if (IsNameChar(c))
                            {
                                
                            }
                            else if (c == '=')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int attrNameStart = continuousMark.Value;
                                    int attrNameLength = currentCharIndex - attrNameStart;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), htmlAttributeNameType));
                                }

                                state = State.AfterAttributeEqualSign;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else if (c == '>')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int attrNameStart = continuousMark.Value;
                                    int attrNameLength = currentCharIndex - attrNameStart;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), htmlAttributeNameType));
                                }

                                state = State.AfterCloseAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else if (c == '/')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int attrNameStart = continuousMark.Value;
                                    int attrNameLength = currentCharIndex - attrNameStart;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), htmlAttributeNameType));
                                }

                                state = State.AfterOpenTagSlash;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterAttributeName:
                        {
                            if (char.IsWhiteSpace(c))
                            {
                                
                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeName;
                            }
                            else if (c == '=')
                            {
                                state = State.AfterAttributeEqualSign;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else if (c == '/')
                            {
                                state = State.AfterOpenTagSlash;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterAttributeEqualSign:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeValue;
                            }
                            else if (c == '\"')
                            {
                                state = State.AfterOpenDoubleQuote;
                                insideDoubleQuote = true;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlQuoteType));
                            }
                            else if (c == '\'')
                            {
                                state = State.AfterOpenSingleQuote;
                                insideSingleQuote = true;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlQuoteType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterOpenDoubleQuote:
                        {
                            if (c == '\"')
                            {
                                state = State.InsideAttributeList;
                                insideDoubleQuote = false;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlQuoteType));
                            }
                            else
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeValue;
                            }
                            break;
                        }
                    case State.AfterOpenSingleQuote:
                        {
                            if (c == '\'')
                            {
                                state = State.InsideAttributeList;
                                insideSingleQuote = false;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlQuoteType));
                            }
                            else
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeValue;
                            }
                            break;
                        }
                    case State.AttributeValue:
                        {
                            if (c == '\'')
                            {
                                if (insideSingleQuote)
                                {
                                    state = State.InsideAttributeList;
                                    insideSingleQuote = false;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlQuoteType));

                                    if (continuousMark.HasValue)
                                    {
                                        int start = continuousMark.Value;
                                        int length = currentCharIndex - start;
                                        continuousMark = null;

                                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), htmlAttributeValueType));
                                    }
                                }
                            }
                            else if (c == '\"')
                            {
                                if (insideDoubleQuote)
                                {
                                    state = State.InsideAttributeList;
                                    insideDoubleQuote = false;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlQuoteType));

                                    if (continuousMark.HasValue)
                                    {
                                        int start = continuousMark.Value;
                                        int length = currentCharIndex - start;
                                        continuousMark = null;

                                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), htmlAttributeValueType));
                                    }
                                }
                            }
                            else
                            {

                            }

                            break;
                        }
                    case State.AfterCloseAngleBracket:
                        {
                            if (c == '<')
                            {
                                state = State.AfterOpenAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                continuousMark = null;
                                state = State.InsideElement;
                            }
                            break;
                        }
                    case State.InsideElement:
                        {
                            if (c == '<')
                            {
                                state = State.AfterOpenAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            break;
                        }
                    case State.AfterCloseTagSlash:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.ElementName;
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterOpenTagSlash:
                        {
                            if (c == '>')
                            {
                                state = State.AfterCloseAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), htmlDelimiterType));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    default:
                        break;
                }

                ++currentCharIndex;
            }

            // if the continuous span is stopped because of end of literal,
            // the span was not colored, handle it here
            if (currentCharIndex >= literal.Length)
            {
                if (continuousMark.HasValue)
                {
                    if (state == State.ElementName)
                    {
                        int start = continuousMark.Value;
                        int length = literal.Length - start;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), htmlElementType));
                    }
                    else if (state == State.AttributeName)
                    {
                        int attrNameStart = continuousMark.Value;
                        int attrNameLength = literal.Length - attrNameStart;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), htmlAttributeNameType));
                    }
                    else if (state == State.AttributeValue)
                    {
                        int start = continuousMark.Value;
                        int length = literal.Length - start;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), htmlAttributeValueType));
                    }
                }
            }

            return result;
        }

#pragma warning disable 67
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67
    }
    #endregion //Classifier
}
