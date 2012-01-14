//=================================================================================================
//=================================================================================================
//
// Copyright (c) 2012 Dennis Mnuskin
//
// This file is part of Droppy application.
//
// This source code is distributed under the MIT license.  For full text, see
// http://www.opensource.org/licenses/mit-license.php Same text is found in LICENSE file which
// is located in root directory of the project.
//
//=================================================================================================
//=================================================================================================

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
    /// <summary>
    /// A text block control which is designed specifically for displaying file paths.
    /// </summary>
    /// <remarks>
    /// The goal of this control is to display as much of a file path as possible but if it has to be
    /// clipped, the priority is given to the directories deepest in the hierarchy. It also guarantees
    /// that the first letter of the last directory's name is always guaranteed to be visible.
    /// 
    /// For example, if this is the path: c:\development\personal\droppy\images, then these would be
    /// possible displayed strings depending on how wide the control is:
    ///     * c:\development\personal\droppy\UI Behavior
    ///     * ...onal\droppy\UI Behavior
    ///     * ...oppy\UI Behavior
    ///     * UI Behavior
    ///     * UI Beha...
    /// </remarks>
    class FilePathTextBlock : TextBlock
    {
        #region ----------------------- Public Members ------------------------

        #region - - - - - - - FilePath Dependency Property- - - - - - - - - - -

        /// <summary>
        /// Identifies FilePathTextBlock.FilePath dependency property
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = 
                DependencyProperty.Register( "FilePath", typeof( string ), typeof( FilePathTextBlock ),
                                             new FrameworkPropertyMetadata( ( d, e ) => ( (FilePathTextBlock)d ).OnFilePathChanged( e ) ) );

        /// <summary>
        /// Gets/Sets file path string which is to be displayed by the control
        /// </summary>
        public string FilePath
        {
            get { return (string)GetValue( FilePathProperty ); }
            set { SetValue( FilePathProperty, value ); }
        }

        #endregion

        #region - - - - - - - IsTextClipped Read-Only Dependency Property - - -

        private static readonly DependencyPropertyKey IsTextClippedPropertyKey = 
                DependencyProperty.RegisterReadOnly( "IsTextClipped", typeof( bool ), typeof( FilePathTextBlock ),
                                                     new FrameworkPropertyMetadata( false  )                       );

        /// <summary>
        /// Identifies FilePathTextBlock.IsTextClipped read-only dependency property
        /// </summary>
        public static readonly DependencyProperty IsTextClippedProperty = IsTextClippedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a flag which indicates whether or not currently displayed file path is clipped 
        /// </summary>
        public bool IsTextClipped
        {
            get { return (bool)GetValue( IsTextClippedProperty ); }
            private set { SetValue( IsTextClippedPropertyKey, value ); }
        }

        #endregion

        #endregion

        #region ----------------------- Protected Base Overrides --------------

        /// <inheritdoc/>
        protected override void OnRenderSizeChanged( SizeChangedInfo sizeInfo )
        {
            base.OnRenderSizeChanged( sizeInfo );

            if( FilePath != null ) CalculateTextProperty();
        }

        #endregion

        #region ----------------------- Private Members -----------------------

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
                IsTextClipped = false;
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
                IsTextClipped = true;
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
                    IsTextClipped = false;
                }
                else
                {
                    Text = "..." + str.Substring( i + 1 );
                    IsTextClipped = true;
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

        #endregion
    }
}
