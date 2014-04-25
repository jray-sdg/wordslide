//WordSlide
//Copyright (C) 2008-2012 Jonathan Ray <asky314159@gmail.com>

//WordSlide is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//A copy of the GNU General Public License should be in the
//Installer directory of this source tree. If not, see
//<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using WordSlideEngine;

namespace WordSlide
{
    /// <summary>
    /// The Importer class stores static functions used to import slide data from various sources.
    /// </summary>
    public class Importer
    {
        /// <summary>
        /// The ErrorCode enumeration allows the importer functions to communicate back to the Import
        /// form what went wrong during an import, allowing the form to display an apppropriate 
        /// error message.
        /// </summary>
        public enum ImporterErrorCode
        {
            Unset,
            CompletedSucessfully,
            GenericError,
            SearchTermNotFound,
            SearchConstantIncorrect
        }

        /// <summary>
        /// Stores an EditableSlideSet generated by one of the importer functions. Use this property 
        /// to retrieve the results after running the appropriate function.
        /// </summary>
        private static EditableSlideSet iss;

        /// <summary>
        /// Stores an EditableSlideSet generated by one of the importer functions. Use this property 
        /// to retrieve the results after running the appropriate function.
        /// </summary>
        public static EditableSlideSet ImportedSlideSet { get { return iss; } }

        /// <summary>
        /// Import a SlideSet using the data stored at the RUF Hymnbook website.
        /// </summary>
        /// <param name="url">Either the name of a song to search for or a URL directly to a song page.</param>
        /// <returns>Returns an ErrorCode specifying what went wrong during the import.</returns>
        public static ImporterErrorCode importRHO(string url)
        {
            //http://www.developer.com/net/csharp/article.php/2230091 Try this?
            try
            {
                WebClient client = new WebClient();
                iss = new EditableSlideSet();
                if (!url.Contains(".html"))
                {
                    string search = client.DownloadString("http://www.igracemusic.com/hymnbook/hymns.html");
                    url = url.ToLower();
                    search = search.ToLower();
                    int sindex = search.IndexOf(url);
                    if (sindex == -1) return ImporterErrorCode.SearchTermNotFound;
                    string urlfind = "<a href=\"";
                    int uindex = 0;
                    while (true)
                    {
                        int utemp = search.IndexOf(urlfind, uindex + 1);
                        if (utemp > sindex) break;
                        uindex = utemp;
                        if (uindex == -1) return ImporterErrorCode.SearchConstantIncorrect;
                    }
                    uindex += urlfind.Length;
                    int ulength = (search.IndexOf("\"", uindex) - uindex);
                    url = "http://www.igracemusic.com/hymnbook/" + search.Substring(uindex, ulength);
                }
                string titlefind = " class=\"header1\">";
                string versefind = " class=\"body\">";
                string endversefind = "<a href=\"#top\">";
                string page = client.DownloadString(url);
                int index = page.IndexOf(titlefind);
                int length = (page.IndexOf("</p>", index) - index - titlefind.Length);
                iss.Name = page.Substring((index + titlefind.Length), length);
                iss.Name = stripHTML(iss.Name);
                int chop = (page.IndexOf(endversefind, index) - index);
                string verses = page.Substring(index, chop);
                chop = 0;
                int n = 0;
                while (true)
                {
                    chop = verses.IndexOf(versefind, chop + 1);
                    if (chop == -1) break;
                    n++;
                }
                iss.setupTexts(n - 1, 0);
                index = 0;
                for (int x = 0; x < (n - 1); x++)
                {
                    index = verses.IndexOf(versefind, index + 1);
                    length = (verses.IndexOf("</p>", index) - index - versefind.Length);
                    string verse = verses.Substring((index + versefind.Length), length);
                    verse = verse.Replace("&#146;", "'");
                    verse = verse.Replace("&quot;", "\"");
                    verse = verse.Replace("<br>\n", System.Environment.NewLine);
                    verse = verse.Replace("              ", "");
                    verse = stripHTML(verse);
                    iss.addText(x, verse, 0);
                }
            }
            catch (Exception e)
            {
                //Program.ReportError(e);
                return ImporterErrorCode.GenericError;
            }
            return ImporterErrorCode.CompletedSucessfully;
        }

        /// <summary>
        /// Import a SlideSet using data stored at the Cyber Hymnal website.
        /// </summary>
        /// <param name="url">Either the name of a song to search for or a URL directly to a song page.</param>
        /// <returns>Returns an ErrorCode specifying what went wrong during the import.</returns>
        public static ImporterErrorCode importCH(string url)
        {
            try
            {
                WebClient client = new WebClient();
                iss = new EditableSlideSet();
                if (!url.Contains(".htm"))
                {
                    url = url.ToLower();
                    string search = client.DownloadString("http://www.cyberhymnal.org/ttl/ttl-" + url.ToCharArray()[0] + ".htm");
                    search = search.Replace("&#8217;", "'");
                    search = search.ToLower();
                    int sindex = search.IndexOf(url);
                    if (sindex == -1) return ImporterErrorCode.SearchTermNotFound;
                    string urlfind = "<a href=\"..";
                    int uindex = 0;
                    while (true)
                    {
                        int utemp = search.IndexOf(urlfind, uindex + 1);
                        if (utemp > sindex) break;
                        uindex = utemp;
                        if (uindex == -1) return ImporterErrorCode.SearchConstantIncorrect;
                    }
                    uindex += urlfind.Length;
                    int ulength = (search.IndexOf("\"", uindex) - uindex);
                    url = "http://www.cyberhymnal.org" + search.Substring(uindex, ulength);
                }
                string titlefind = "<title>";
                string versefind = "<p>";
                string chorusfind = "<p class=\"chorus\">";
                string versesectionfind = "<div class=\"lyrics\">";
                string page = client.DownloadString(url);
                int index = page.IndexOf(titlefind);
                int length = (page.IndexOf("</title>", index) - index - titlefind.Length);
                iss.Name = page.Substring((index + titlefind.Length), length);

                index = page.IndexOf(versesectionfind);
                int chop = (page.IndexOf("</div>", index) - index);
                string verses = page.Substring(index, chop);
                chop = 0;
                int n = 0;
                while (true)
                {
                    chop = verses.IndexOf(versefind, chop + 1);
                    if (chop == -1) break;
                    n++;
                }
                bool haschorus = false;
                if (verses.Contains(chorusfind))
                {
                    n++;
                    haschorus = true;
                }
                iss.setupTexts(n, 0);
                index = 0;
                for (int x = 0; x < (n - (haschorus ? 1 : 0)); x++)
                {
                    index = verses.IndexOf(versefind, index + 1);
                    length = (verses.IndexOf("</p>", index) - index - versefind.Length);
                    string verse = verses.Substring((index + versefind.Length), length);
                    verse = verse.Replace("<br />", "");
                    verse = verse.Replace("&#8217;", "'");
                    verse = verse.Replace("&#8212;", ";");
                    verse = verse.Replace("&#8220;", "\"");
                    verse = verse.Replace("&#8221;", ",");
                    verse = stripHTML(verse);
                    iss.addText(x, verse, 0);
                }
                if (haschorus)
                {
                    index = 0;
                    index = verses.IndexOf(chorusfind, index + 1);
                    index = verses.IndexOf(chorusfind, index + 1);
                    length = (verses.IndexOf("</p>", index) - index - chorusfind.Length);
                    string chorus = verses.Substring((index + chorusfind.Length), length);
                    chorus = chorus.Replace("<br />", "");
                    chorus = chorus.Replace("&#8217;", "'");
                    chorus = chorus.Replace("&#8212;", ";");
                    chorus = chorus.Replace("&#8220;", "\"");
                    chorus = chorus.Replace("&#8221;", ",");
                    chorus = stripHTML(chorus);
                    iss.addText(n - 1, chorus, 0);
                    iss.Chorus = n - 1;
                }
            }
            catch (Exception e)
            {
                //Program.ReportError(e);
                return ImporterErrorCode.GenericError;
            }
            return ImporterErrorCode.CompletedSucessfully;
        }

        /// <summary>
        /// Import a SlideSet into the program's folder using an existing .sld file.
        /// </summary>
        /// <param name="path">The full path to the .sld file to import.</param>
        /// <returns>Returns an ErrorCode specifying what went wrong during the import.</returns>
        public static ImporterErrorCode importSLD(string path)
        {
            try
            {
                iss = new EditableSlideSet(path);
                iss.loadFile();
                iss.resetPath();
            }
            catch (Exception e)
            {
                //Program.ReportError(e);
                return ImporterErrorCode.GenericError;
            }
            return ImporterErrorCode.CompletedSucessfully;
        }

        /// <summary>
        /// This function does not work. Do not use.
        /// </summary>
        /// <param name="path">This function does not work. Do not use.</param>
        /// <returns>Thus function does not work. Do not use.</returns>
        public static ImporterErrorCode importPPT(string path)
        {
            try
            {
                iss = new EditableSlideSet();
                StreamReader reader = new StreamReader(path);
                bool flag1 = false;
                bool flag2 = false;
                bool flag3 = false;
                int count = 0;
                string text = "";
                while (!reader.EndOfStream)
                {
                    int ch = reader.Read();
                    if (flag1 && flag2)
                    {
                        if (count == 0 && ch == 2)
                        {
                            flag1 = false;
                        }
                        if (count < 4)
                        {
                            count++;
                        }
                        else
                        {
                            if (ch == 0)
                            {
                                if (flag3)
                                {
                                    flag1 = false;
                                    iss.Name += "i";
                                }
                                else
                                {
                                    flag3 = true;
                                }
                            }
                            else
                            {
                                text += (char)ch;
                            }
                        }
                    }
                    else
                    {
                        text = "";
                        count = 0;
                        if (flag1 && ch == 15)
                        {
                            flag2 = true;
                        }
                        else
                        {
                            flag1 = false;
                            flag2 = false;
                            if (ch == 160 || ch == 168)
                            {
                                flag1 = true;
                            }
                        }
                    }
                }
                reader.Close();
            }
            catch (Exception e)
            {
                //Program.ReportError(e);
                return ImporterErrorCode.GenericError;
            }
            return ImporterErrorCode.CompletedSucessfully;
        }

        /// <summary>
        /// Takes a string of text and strips all HTML tags from it. Indended to be used in
        /// conjunction with the various importers.
        /// </summary>
        /// <param name="input">The string to strip tags from.</param>
        /// <returns>The input string without HTML tags.</returns>
        public static string stripHTML(string input)
        {
            int first = -1;
            string temp = input;
            for (int x = 0; x < temp.Length; x++)
            {
                if (temp[x] == '<')
                {
                    first = x;
                }
                if (temp[x] == '>' && first != -1)
                {
                    temp = temp.Remove(first, (x - first + 1));
                    first = -1;
                }
            }
            return temp;
        }
    }
}
