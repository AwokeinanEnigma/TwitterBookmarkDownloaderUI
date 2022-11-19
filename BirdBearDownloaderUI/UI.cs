using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace BirdBearDownloaderUI
{
    public partial class BirdBearDownloader : Form
    {
        public BirdBearDownloader()
        {
            InitializeComponent();
        }

        private string path = string.Empty;
        private string jsonFileName = string.Empty;
        private string actualPath { get 
            { return path + "\\"; 
            }
            set {
                actualPath = value;
            } 
        }

        // define up here
        private static WebClient webClient = new WebClient();

        // start at one - it'll make more sense for the end user
        private static int glblImageCounter = 1;
        private static int glblVideoCounter = 1;

        private static List<string> blackListedVideoIDs = new List<string>();

        public enum ImageType
        {
            PNG,
            JPG,
        }

        #region Images
        public bool ValidateImageLink(string link)
        {
            // we want the big version of our link, not the small one
            if (link.Contains("&name=large"))
            {
                // it's the big version
                return true;
            }
            // it's the small version
            return false;
        }

        public ImageType DetermineImageType(string link)
        {
            if (link.Contains("png"))
            {
                return ImageType.PNG;
            }
            else if (link.Contains("jpg"))
            {
                return ImageType.JPG;
            }

            // if we don't know what it is, panic!
            throw new Exception($"Unrecognized image type! Link {link}");
        }

        public void DownloadImage(string link)
        {
            // reject small version!
            if (ValidateImageLink(link) == false)
            {
                return;
            }
            ImageType type = DetermineImageType(link);
            string fileName = $"{actualPath}image{glblImageCounter}";
            if (type is ImageType.JPG)
            {
                webClient.DownloadFile(new Uri(link), fileName + ".jpg");
            }
            // it's either or
            else
            {
                webClient.DownloadFile(new Uri(link), fileName + ".png");
            }
            // increment image counter
            glblImageCounter++;

            // only sleep if the value is greater than 0 ( le epic sharpless reference )
            if (numericUpDown1.Value > 0)
            {
                Thread.Sleep((int)(numericUpDown1.Value * 1000));
            }
        }

        public string IsImageLink(string txt, out bool isImage)
        {
            Regex regx = new Regex("https://pbs.twimg.com/media/([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
            MatchCollection mactches = regx.Matches(txt);
            if (mactches.Count > 0)
            {
                isImage = true;
                return mactches[0].Value;
            }
            isImage = false;
            return "no link found";
        }

        #endregion

        #region Videos
        public bool ValidateVideoLink(string link)
        {
            // some of these files are .m3u8 files so we just gotta prune em
            if (link.Contains(".mp4?tag=12"))
            {
                // it's an actual mp4 file 
                return true;
            }
            // it's not
            return false;
        }
        public void DownloadVideo(string link)
        {
            // the thing here
            // twitter makes 3 different versions of any given video
            // the highest bitrate version
            // the lowest bitrate version
            // something inbetween - who knows!
            // we ONLY want the highest bitrate version
            string videoId = link.Replace("https://video.twimg.com/ext_tw_video/", string.Empty);
            //'id/pu/vid/480x582/videolink.mp4?tag=12'
            //every id is 19 characters long so we just remove everything after 19 characters
            videoId = videoId.Remove(19);
            if (!blackListedVideoIDs.Contains(videoId))
            {
                // reject non-videos!
                if (ValidateVideoLink(link) == false)
                {
                    return;
                }

                blackListedVideoIDs.Add(videoId);

                // sometimes there's a 403 or a 504 error here and i'm 95% sure that it's just twitter's servers getting pissed that i'm repeatedly downloading from their servers
                // it probably looks like a volumetric attack
                // whoops!
                // update: copyrighted media casues a 403 too so unfortunately we need to account for this 
                bool error = false;
                try
                {
                    webClient.DownloadFile(new Uri(link), $"{actualPath}video{glblVideoCounter}.mp4");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"The link '{link}' causes {e} (error). Skipping!");
                    error = true;
                }

                if (error == false)
                {
                    // increment image counter
                    glblVideoCounter++;

                    // only sleep if the value is greater than 0 ( le epic sharpless reference )
                    if (numericUpDown1.Value > 0)
                    {
                        Thread.Sleep((int)(numericUpDown1.Value * 1000));
                    }
                }
            }
        }
        public string IsVideoLink(string txt, out bool isVideo)
        {
            Regex regx = new Regex("https://video.twimg.com/ext_tw_video/([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
            MatchCollection mactches = regx.Matches(txt);
            if (mactches.Count > 0)
            {
                isVideo = true;
                return mactches[0].Value;
            }
            isVideo = false;
            return "no link found";
        }

        #endregion
        private bool downloading;

        // taken from:
        // https://stackoverflow.com/questions/4580397/json-formatter-in-c
        // thanks yallie!
        public static string FormatJson(string json, string indent = "  ")
        {
            var indentation = 0;
            var quoteCount = 0;
            var escapeCount = 0;

            var result =
                from ch in json ?? string.Empty
                let escaped = (ch == '\\' ? escapeCount++ : escapeCount > 0 ? escapeCount-- : escapeCount) > 0
                let quotes = ch == '"' && !escaped ? quoteCount++ : quoteCount
                let unquoted = quotes % 2 == 0
                let colon = ch == ':' && unquoted ? ": " : null
                let nospace = char.IsWhiteSpace(ch) && unquoted ? string.Empty : null
                let lineBreak = ch == ',' && unquoted ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(indent, indentation)) : null
                let openChar = (ch == '{' || ch == '[') && unquoted ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(indent, ++indentation)) : ch.ToString()
                let closeChar = (ch == '}' || ch == ']') && unquoted ? Environment.NewLine + string.Concat(Enumerable.Repeat(indent, --indentation)) + ch : ch.ToString()
                select colon ?? nospace ?? lineBreak ?? (
                    openChar.Length > 1 ? openChar : closeChar
                );

            return string.Concat(result);
        }
        public void StartDownload()
        {

            if (downloading == false)
            {
                if (path != string.Empty && jsonFileName != string.Empty)
                {
                    // Loretta!

                    // You might be asking - "Why not just read it as a json file?"
                    // Here's why: I don't have the patience required to mess around with whatever weird ass JSON structure that BirdBear produces
                    // Also I don't know enough about the json format in general, so bite me!
                    string text = File.ReadAllText(jsonFileName);

                    // split into lines
                    string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    Console.WriteLine(lines.Length);

                    // unformatted json that's on a single line
                    // yucky!
                    if (lines.Length == 1) {
                        Console.WriteLine(FormatJson(text, Environment.NewLine));
                        lines = FormatJson(text, Environment.NewLine).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        //Console.WriteLine(f);
                    }

                    // go over each line
                    foreach (string line in lines)
                    {

                        string link = IsImageLink(line, out bool isImage);
                        if (isImage)
                        {
                            DownloadImage(link);
                        }
                        else
                        {
                            bool isVideo;
                            string videolink = IsVideoLink(line, out isVideo);

                            if (isVideo)
                            {
                                DownloadVideo(videolink);
                            }
                        }
                    }

                    label4.Visible = true;
                    // webClient.DownloadFile(new Uri("https://video.twimg.com/ext_tw_video/1583163829189476352/pu/vid/320x568/qMZFf6wYLsZx8S8k.mp4?tag=12"), "D:\\thing.mp4");
                }
                downloading = true;
            }
        }

        #region Ignore
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void BirdBearDownloader_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
#endregion

        // open json button
        private void button1_Click(object sender, EventArgs e)
        {
            if (OpenJSONDialog.ShowDialog(this) == DialogResult.OK)
            {
                jsonFileName = OpenJSONDialog.FileName;
            }
        }

        // set folder button
        private void button2_Click(object sender, EventArgs e)
        {
            if (OpenFolderToSaveToDialog.ShowDialog(this) == DialogResult.OK) {
                path = OpenFolderToSaveToDialog.SelectedPath;
            }
        }

        // download button
        private void button3_Click(object sender, EventArgs e)
        {
            if (path != string.Empty) {
                StartDownload();
            }
        }

        //restart
        private void button4_Click(object sender, EventArgs e)
        {
            this.downloading = false;
            label4.Visible = false;
            blackListedVideoIDs.Clear();
        }
    }
}
