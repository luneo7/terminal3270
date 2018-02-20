#region License
/* 
 *
 * Open3270 - A C# implementation of the TN3270/TN3270E protocol
 *
 *   Copyright © 2004-2006 Michael Warriner. All rights reserved
 * 
 * This is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation; either version 2.1 of
 * the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
 * 02110-1301 USA, or see the FSF site: http://www.fsf.org.
 */
#endregion
using System;
using System.Data;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using Open3270;
using Open3270.Internal;
using System.Collections.Generic;

namespace Open3270.TN3270
{
    /// <summary>
    /// Do not use this class, use IXMLScreen instead...!
    /// </summary>

    public class Screen : IScreen, IDisposable
    {
        private Guid _ScreenGuid;

        public Guid ScreenGuid { get { return _ScreenGuid; } }

        public List<ScreenField> Fields
        {
            get;
            set;
        }

        public UnformattedScreen Unformatted;

        public bool Formatted;

        private char[] mScreenBuffer = null;

        private int _CX = 80;
        private int _CY = 25;

        private string _stringValueCache = null;


        public int CX { get { return _CX; } set { this._CX = value; } }
        public int CY { get { return _CY; } set { this._CY = value; } }
        public string UserIdentified;
        public string MatchListIdentified;
        public string Name { get { return MatchListIdentified; } }
        public string FileName;
        public Guid UniqueID;

        [XmlIgnore]
        public string Hash;

        bool isDisposed = false;

        ~Screen()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;

            if (disposing)
            {
                Fields = null;
                Unformatted = null;
                MatchListIdentified = null;
                mScreenBuffer = null;
                Hash = null;
            }
        }


        public void Render()
        {
            //   TO DO: REWRITE THIS CLASS! and maybe the whole process of
            //          getting data from the lower classes, why convert buffers
            //          to XML just to convert them again in this Render method?
            //  ALSO: this conversion process does not take into account that
            //        the XML data that this class is converted from might
            //        contain more than _CX characters in a line, since the
            //        previous conversion to XML converts '<' to "&lt;" and
            //        the like, which will also cause shifts in character positions.
            //
            // Reset cache
            //
            _stringValueCache = null;
            //
            if (_CX == 0 || _CY == 0)
            {
                // TODO: Need to fix this
                _CX = 132;
                _CY = 43;
            }

            // CFCJr 2008/07/11
            if (_CX < 80)
                _CX = 80;
            if (_CY < 25)
                _CY = 25;

            UserIdentified = null;
            MatchListIdentified = null;
            //
            // Render text image of screen
            //
            //
            mScreenBuffer = new char[_CX * _CY];

            // CFCJr 2008/07/11
            // The following might be much faster:
            //
            //   string str = "".PadRight(_CX*_CY, ' ');
            //   mScreenBuffer = str.ToCharArray();
            //     ........do operations on mScreenBuffer to fill it......
            //   str = string.FromCharArray(mScreenBuffer);
            //   for (int r = 0; r < _CY; r++)
            //        mScreenRows[i] = str.SubString(r*_CY,_CX);
            //
            //  ie, fill mScreenBuffer with the data from Unformatted and Field, then
            //   create str (for the hash) and mScreenRows[]
            //   with the result.            
            for (int i = 0; i < mScreenBuffer.Length; i++)  // CFCJr. 2008.07/11 replase _CX*CY with mScreenBuffer.Length
            {
                mScreenBuffer[i] = ' ';
            }
            //

            if (Fields == null || Fields.Count == 0 &&
                (Unformatted == null || Unformatted.Text == null))
            {
                if ((Unformatted == null || Unformatted.Text == null))
                    Console.WriteLine("XMLScreen:Render: **BUGBUG** XMLScreen.Unformatted screen is blank");
                else
                    Console.WriteLine("XMLScreen:Render: **BUGBUG** XMLScreen.Field is blank");

                Console.Out.Flush();
            }


            if (Unformatted != null && Unformatted.Text != null)
            {
                for (int i = 0; i < Unformatted.Text.Count; i++)
                {
                    string text = Unformatted.Text[i];

                    // CFCJr, make sure text is not null

                    if (string.IsNullOrEmpty(text))
                        text = string.Empty;

                    /*for (int p = 0; p < text.Length; p++) 
                    {
                        if (text[p] < 32 || (text[p] > 126 && text[p] < 160))
                            text = text.Replace(text[p], ' ');
                    }*/

                    for (int p = 0; p < text.Length; p++)
                    {
                        // CFCJr, calculate mScreenBuffer index only once
                        int bufNdx = p + (i * _CX);

                        if (bufNdx < mScreenBuffer.Length)
                        {
                            mScreenBuffer[bufNdx] = text[p];
                        }
                    }
                }
            }

            // Now superimpose hidden fields

            if (Fields != null && Fields.Count > 0)
            {
                for (int i = 0; i < Fields.Count; i++)
                {
                    if (Fields[i] != null && Fields[i].Attributes.FieldType == "Hidden")
                    {
                        if (Fields[i].Text != null)
                        {
                            for (int j = 0; j < Fields[i].Text.Length; j++)
                            {
                                int bufNdx = j + Fields[i].Location.left + Fields[i].Location.top * _CX;
                                if (bufNdx >= 0 && bufNdx < mScreenBuffer.Length)
                                    mScreenBuffer[bufNdx] = Fields[i].Text[j];
                            }
                        }
                    }
                }
            }

            // now calculate our screen's hash

            HashAlgorithm hash = (HashAlgorithm)CryptoConfig.CreateFromName("MD5");
            byte[] myHash = hash.ComputeHash(Encoding.UTF8.GetBytes(new string(mScreenBuffer)));
            this.Hash = BitConverter.ToString(myHash);
            this._ScreenGuid = Guid.NewGuid();
        }


        // helper functions
        public string GetText(int x, int y, int length)
        {
            return GetText(x + y * _CX, length);
        }

        public string GetText(int offset, int length)
        {
            int i;
            string result = "";
            int maxlen = this.mScreenBuffer.Length;
            for (i = 0; i < length; i++)
            {
                if (i + offset < maxlen)
                    result += this.mScreenBuffer[i + offset];
            }
            return result;
        }
        public char GetCharAt(int offset)
        {
            return this.mScreenBuffer[offset];
        }
        public string GetRow(int row)
        {
            if (Unformatted != null && Unformatted.Text != null)
                return Unformatted.Text[row];
            return null;
        }
        public string Dump()
        {
            if (Unformatted != null && Unformatted.Text != null)
                return String.Join("\n", Unformatted.Text.ToArray());

            if (mScreenBuffer != null && mScreenBuffer.Length > 0)
            {
                StringBuilder sb = new StringBuilder(mScreenBuffer.Length + (mScreenBuffer.Length / _CX) - 1);
                for (int i = 0; i < mScreenBuffer.Length; i++)
                {
                    sb.Append(mScreenBuffer[i]);
                    if (i > 0 && i % _CX == 0) sb.Append("\n");
                }
                return sb.ToString();
            }

            return string.Empty;
        }
        public void Dump(IAudit stream)
        {
            int i;
            //stream.WriteLine("-----");
            //string tens = "   ", singles= "   "; // the quoted strings must be 3 spaces each, it gets lost in translation by codeplex...
            //for (i = 0; i < _CX; i += 10)
            //{
            //	tens += String.Format("{0,-10}", i / 10);
            //	singles += "0123456789";
            //}
            //stream.WriteLine(tens.Substring(0,3+_CX));
            //stream.WriteLine(singles.Substring(0, 3 + _CX));
            for (i = 0; i < _CY; i++)
            {
                string line = GetText(0, i, _CX);
                //string lr = ""+i+"       ";
                stream.WriteLine(line);
            }
            //stream.WriteLine("-----");
        }

        public List<string> GetUnformatedStrings()
        {
            if (Unformatted != null && Unformatted.Text != null)
                return Unformatted.Text;
            return null;
        }

    }

    public class UnformattedScreen
    {
        public List<String> Text;
    }


    public class ScreenField
    {
        public ScreenLocation Location;

        public ScreenAttributes Attributes;

        public string Text;
    }

    public class ScreenLocation
    {
        public int position;
        public int left;
        public int top;
        public int length;
    }

    public class ScreenAttributes
    {
        public int Base;
        public bool Protected;
        public string FieldType;
        public string Foreground;
        public string Background;
        public string Highlighting;
        public string Mask;
    }

    //
    // 
}
