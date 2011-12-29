using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Droppy
{
    public class FilePathTextBlock : TextBlock
    {
        static FilePathTextBlock()
        {
            //DefaultStyleKeyProperty.OverrideMetadata( typeof( FilePathTextBlock ), new FrameworkPropertyMetadata( typeof( FilePathTextBlock ) ) );
        }

        public static readonly DependencyProperty FilePathProperty = 
                DependencyProperty.Register( "FilePath", typeof( string ), typeof( FilePathTextBlock ),
                                             new FrameworkPropertyMetadata( ( d, e ) => ( (FilePathTextBlock)d ).OnFilePathChanged( e ) ) );

        public string FilePath
        {
            get { return (string)GetValue( FilePathProperty ); }
            set { SetValue( FilePathProperty, value ); }
        }

        protected override void OnRenderSizeChanged( SizeChangedInfo sizeInfo )
        {
            base.OnRenderSizeChanged( sizeInfo );

            if( FilePath != null ) CalculateTextProperty();
        }

        private void OnFilePathChanged( DependencyPropertyChangedEventArgs e )
        {
            CalculateTextProperty();
        }


        private void CalculateTextProperty()
        {
            double  availableWidth = ActualWidth - Padding.Width();
            double  ellipsesWidth, totalWidth;
            double  fontSize = FontSize;
            int     i, lastSlashPos;
            string  str = FilePath;

            if( string.IsNullOrEmpty( str ) )
            {
                Text = null;
                return;
            }

            if( _glyphTypeface == null )
            {
                Typeface typeface = new Typeface( FontFamily, FontStyle, FontWeight, FontStretch );

                if( !typeface.TryGetGlyphTypeface( out _glyphTypeface ) )
                {
                    throw new InvalidOperationException( "Glyph typeface is not found!!" );
                }
            }

            ellipsesWidth = _glyphTypeface.AdvanceWidths[ 
                                    _glyphTypeface.CharacterToGlyphMap[ '.' ] ] * fontSize * 3;
            totalWidth = ellipsesWidth;

            if( availableWidth <= totalWidth ) str = string.Empty;

            lastSlashPos = str.LastIndexOf( '\\' );
            if( lastSlashPos == -1 ) lastSlashPos = str.Length;

            totalWidth += ellipsesWidth;

            for( i = lastSlashPos; i < str.Length; i++ )
            {
                if( !AddCharToTotalWidth( str[i], fontSize, availableWidth, ref totalWidth ) ) break;
            }

            if( i < str.Length )
            {
                Text = "..." + str.Substring( lastSlashPos, i - lastSlashPos ) + "...";
            }
            else
            {   
                totalWidth -= ellipsesWidth;
         
                for( i = lastSlashPos - 1; i >= 0; i-- )
                {
                    if( !AddCharToTotalWidth( str[i], fontSize, availableWidth, ref totalWidth ) ) break;
                }

                if( i == -1 )
                {
                    Text = str;
                }
                else
                {
                    Text = "..." + str.Substring( i + 1 );
                }
            }
        }

        private bool AddCharToTotalWidth( char ch, double fontSize, double maxWidth, ref double totalWidth )
        {
            ushort glyphIndex = _glyphTypeface.CharacterToGlyphMap[ ch ];
            double width = _glyphTypeface.AdvanceWidths[ glyphIndex ] * fontSize;
            double newTotalWidth = totalWidth + width;

            if( newTotalWidth > maxWidth ) return false;

            totalWidth = newTotalWidth;

            return true;
        }


        private GlyphTypeface    _glyphTypeface;
    }
}
