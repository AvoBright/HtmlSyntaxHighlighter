using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HtmlSyntaxHighlighter
{
    #region Format definition

    internal static class FormatNames 
    {
        public const string Delimiter = "HtmlDelimiter";
        public const string Element = "HtmlElement";
        public const string AttributeName = "HtmlAttributeName";
        public const string Quote = "HtmlQuote";
        public const string AttributeValue = "HtmlAttributeValue";
    }

    internal static class ClassificationTypeDefinitions
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Delimiter)]
        internal static ClassificationTypeDefinition Delimiter = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Element)]
        internal static ClassificationTypeDefinition Element = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.AttributeName)]
        internal static ClassificationTypeDefinition AttributeName = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Quote)]
        internal static ClassificationTypeDefinition Quote = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.AttributeValue)]
        internal static ClassificationTypeDefinition AttributeValue = null;
    }


    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Delimiter)]
    [Name(FormatNames.Delimiter)]
    [UserVisible(true)]
    [Order(Before = Priority.High)]
    internal sealed class HtmlDelimiterFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlDelimiterFormatDefinition()
        {
            this.DisplayName = "Html Delimiter Characters";
            this.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Element)]
    [Name(FormatNames.Element)]
    [UserVisible(true)]
    [Order(Before = Priority.High)]
    internal sealed class HtmlElementFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlElementFormatDefinition()
        {
            this.DisplayName = "Html Elements";
            this.ForegroundColor = Color.FromRgb(163, 21, 21);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeName)]
    [Name(FormatNames.AttributeName)]
    [UserVisible(true)]
    [Order(Before = Priority.High)]
    internal sealed class HtmlAttributeNameFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlAttributeNameFormatDefinition()
        {
            this.DisplayName = "Html Attribute Names";
            this.ForegroundColor = Colors.Red;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.Quote)]
    [Name(FormatNames.Quote)]
    [UserVisible(true)]
    [Order(Before = Priority.High)]
    internal sealed class HtmlQuoteFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlQuoteFormatDefinition()
        {
            this.DisplayName = "Html Quotes";
            this.ForegroundColor = Colors.Black;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = FormatNames.AttributeValue)]
    [Name(FormatNames.AttributeValue)]
    [UserVisible(true)]
    [Order(Before = Priority.High)]
    internal sealed class HtmlAttributeValueFormatDefinition : ClassificationFormatDefinition
    {
        public HtmlAttributeValueFormatDefinition()
        {
            this.DisplayName = "Html Attribute Values";
            this.ForegroundColor = Colors.Blue;
        }
    }

    #endregion //Format definition
}
