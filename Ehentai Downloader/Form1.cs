using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace Ehantai_Downloader
{
    public partial class Form1 : Form
    {
        delegate void updateProgress(int i);
        delegate void updateUrl(string str);
        delegate void changeBool(bool bl);

        //執行續123456789
        Thread download_thread;
        Thread img_thread;

        //執行續集
        WaitCallback func_pool;

        //變數
        string program_dic;     //程式路徑
        string download_dic;    //下載資料夾路徑
        string bookname;        //書名
        int TotalImagePages;     //本本總頁數
        int webPagingNo;            //下一頁的分頁網址數量
        int downloaded_image = 0;   //已下載的圖片數量
        string[] EachPageAddress = new string[] { };    //各分頁的網址 
        string[] ImagePageAddress = new string[] { };   //圖片網頁的網址
        string[] ImageAddress = new string[] { };       //圖片網址
        string[] tempAddress = new string[] { };        //佔存變數

        public Form1()
        {
            InitializeComponent();
            program_dic = System.Environment.CurrentDirectory;
            download_dic = program_dic + @"\E-Hentai_download";
            if (!Directory.Exists(download_dic))
            {
                Directory.CreateDirectory(download_dic);    //在程式下建立download資料夾
            }
        }

        private void BTN_Conn_Click(object sender, EventArgs e)
        {
            btnFuc(false);

            //舊版本(停用)
            #region 利用HttpWebRequest下載
            /*
            //擷取網址原始碼，並存入result字串裡
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(TB_Address.Text);
            myRequest.Method = "GET";
            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();

            StreamReader sr = new StreamReader(myResponse.GetResponseStream());     //接收網頁傳回來的串流
            string AddressResource = sr.ReadToEnd();    //將串流讀取，讀取原始碼意思
            
            //分析本本的全名，並建立資料夾
            string[] bookname_str = new string[] { };
            bookname_str = Regex.Split(AddressResource, "<h1 id=\"gj\">");
            bookname_str = Regex.Split(bookname_str[1], "</h1>");
            bookname = bookname_str[0];

            if (!Directory.Exists(download_dic + @"\" + bookname)) { Directory.CreateDirectory(download_dic + @"\" + bookname); }
            */

            //取得本本總頁數
            /*
            TotalImagePages = 0;
            ImagePageAddress = Regex.Split(AddressResource, "<td class=\"gdt2\">");
            ImagePageAddress = Regex.Split(ImagePageAddress[6], "pages</td>");
            TotalImagePages = Convert.ToInt16(ImagePageAddress[0]);
            */
            #endregion

            //20170928新版本
            #region 利用HttpAigility下載

            //分析本本的全名，並建立資料夾
            HtmlWeb web = new HtmlWeb();
            var HtmlDoc = web.Load(TB_Address.Text);
            bookname = HtmlDoc.DocumentNode.SelectSingleNode("//div[@class='gm']/div[@id ='gd2']/h1[@id ='gj']").InnerText;

            if (!Directory.Exists(download_dic + @"\" + bookname)) { Directory.CreateDirectory(download_dic + @"\" + bookname); }

            var Pages = HtmlDoc.DocumentNode.SelectNodes("//div[@id='gdd']/table/tr/td[@class='gdt2']");
            char splitchar = ' ';
            string[] str_pages = Pages[5].InnerText.Split(splitchar);
            TotalImagePages = Convert.ToInt16(str_pages[0]);
            #endregion

            SetProgressMaxValue(TotalImagePages);   //設定Progess最大值

            if (TotalImagePages / 40 > 0)    //分頁數，一頁有40張圖，219張圖要(219/40)+1頁的分頁
            {
                webPagingNo = (TotalImagePages / 40) + 1;

                //利用Threading pool方式執行多個download_func執行續
                for (int i = 0; i < webPagingNo; i++)
                {
                    Console.WriteLine("開始頁數{0}下載", i);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Generate_Multi_DownloadFunc_Thread), i);
                }
            }
            else    //無分頁時，直接用執行續執行download_func下載
            {
                //開啟執行續執行分析下載工作
                download_thread = new Thread(() => download_func(TB_Address.Text, 0));
                download_thread.IsBackground = true;
                download_thread.Start();
            }

        }

        private void Generate_Multi_DownloadFunc_Thread(object page)
        {
            try
            {
                if ((int)page > 0)
                {
                    download_thread = new Thread(() => download_func(TB_Address.Text + "?p=" + page, (int)page));   //第N頁網址，p=1為二頁
                }
                else
                {
                    download_thread = new Thread(() => download_func(TB_Address.Text, 0));  //第一頁網址
                }
                download_thread.IsBackground = true;
                download_thread.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void download_func(string add, int webpageno)
        {
            //lock(this)
            {
                //舊版本(停用)
                #region 利用HttpWebRequest下載
                /*
                //擷取網址原始碼，並存入result字串裡
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(add);
                myRequest.Method = "GET";

                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();

                StreamReader sr = new StreamReader(myResponse.GetResponseStream());
                string AddressResource = sr.ReadToEnd();

                //取得各圖片的網址
                ImageAddress = Regex.Split(AddressResource, "<div id=\"gdt\">");
                ImageAddress = Regex.Split(ImageAddress[1], "<div class=\"gtb\">");
                ImageAddress = Regex.Split(ImageAddress[0], "href=\"");
                int ImageCount = 0;
                foreach (string str in ImageAddress)
                {
                    lock(this)  //沒有lock住會解析錯誤網址
                    {
                        tempAddress = Regex.Split(ImageAddress[ImageCount], "\">");
                        ImageAddress[ImageCount] = tempAddress[0];
                        ImageCount++;
                    }
                }
                Console.WriteLine("第{0}頁總共{1}張圖片", webpageno, ImageAddress.Length - 1);

                //將剛剛取出的網址，用foreach或for迴圈方式取出圖片
                for (int i = 1; i < ImageAddress.Length; i++)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Generate_Multi_DownloadImageFunc_Threadimg), ImageAddress[i]);

                    #region 前程式碼
                    ////擷取圖片網頁的原始碼
                    //WebRequest imgWebRequest = WebRequest.Create(ImageWebAddress[i]);
                    //imgWebRequest.Method = "GET";

                    //WebResponse imgWebResponse = imgWebRequest.GetResponse();

                    //StreamReader imgsr = new StreamReader(imgWebResponse.GetResponseStream());
                    //string imgAddressResource = imgsr.ReadToEnd();

                    ////將原始碼做處理，取出各圖片的網址
                    //string[] ImageAddress = new string[] { };

                    //ImageAddress = Regex.Split(imgAddressResource, "<div id=\"i3\">");
                    //ImageAddress = Regex.Split(ImageAddress[1], "src=\"");
                    //ImageAddress = Regex.Split(ImageAddress[1], "\"");
                    ////Console.WriteLine("圖片網址:" + ImageAddress[0]);

                    //img_thread = new Thread(() => imgDownloading(ImageAddress[0], bookname, 40 * webpageno + i));
                    //img_thread.IsBackground = true;
                    //img_thread.Start();
                    //Thread.Sleep(20);
                    ////////將網址丟進來，呼叫下載到download資料夾
                    //////WebRequest req = WebRequest.Create(ImageAddress[0]);
                    //////WebResponse res = req.GetResponse();
                    //////Stream imgStream = res.GetResponseStream();
                    //////FileStream output = new FileStream(download_dic + @"\" + bookname + @"\" + i + ".jpg", FileMode.Create, FileAccess.Write);
                    //////imgStream.CopyTo(output);   //將imgStream的資料流放入output裡面

                    //////////只能下載，無法指定到資料夾
                    //////////ImageAddress[0]就是圖片網址了，接下來就是下載
                    ////////WebClient myWebClient = new WebClient();
                    ////////myWebClient.DownloadFile(ImageAddress[0], i + ".jpg");

                    ////Console.WriteLine("下載了{0}張圖片", i);
                    ////SetProgressValue(i);
                    ////showImg(ImageAddress[0]);
                    #endregion
                }
                sr.Close();
                myResponse.Close();
                */
                #endregion

                //20170928新版本
                #region 利用HttpAigility下載
                HtmlWeb web = new HtmlWeb();
                var HtmlDoc = web.Load(add);
                var ImgAdds = HtmlDoc.DocumentNode.SelectNodes("//div[@id='gdt']/div[@class ='gdtm']/div/a");
                string[] ImageAddress = new string[ImgAdds.Count];  //ImgAdds.Count = 圖片數量

                int Count = 0;
                foreach (var node in ImgAdds)
                {
                    ImageAddress[Count] = node.Attributes["href"].Value;
                    Count++;
                }

                Console.WriteLine("第{0}頁總共{1}張圖片", webpageno, ImgAdds.Count);

                //將剛剛取出的網址，用foreach或for迴圈方式取出圖片
                for (int i = 0; i < ImageAddress.Length; i++)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Generate_Multi_DownloadImageFunc_Threadimg), ImageAddress[i]);
                }
                #endregion

                SetProgressValue(0);
            }
        }

        private void Generate_Multi_DownloadImageFunc_Threadimg(object add)
        {
            //舊版本
            #region 利用HttpWebRequest下載
            //擷取圖片網頁的原始碼
            /*
            HttpWebRequest imgWebRequest = (HttpWebRequest)WebRequest.Create((string)add);
            imgWebRequest.Method = "GET";

            HttpWebResponse imgWebResponse = (HttpWebResponse)imgWebRequest.GetResponse();

            StreamReader imgsr = new StreamReader(imgWebResponse.GetResponseStream());
            string imgAddressResource = imgsr.ReadToEnd();

            //將原始碼做處理，取出各圖片的網址
            string[] ImageAddress = new string[] { };

            ImageAddress = Regex.Split(imgAddressResource, "<div id=\"i3\">");
            ImageAddress = Regex.Split(ImageAddress[1], "src=\"");
            ImageAddress = Regex.Split(ImageAddress[1], "\"");
            //Console.WriteLine("圖片網址:" + ImageAddress[0]);

            string[] ImageNo = new string[] { };
            ImageNo = Regex.Split(ImageAddress[0], ".jpg");
            ImageNo = Regex.Split(ImageNo[ImageNo.Length - 2], "/");

            try
            {
                img_thread = new Thread(() => imgDownloading(ImageAddress[0], bookname, ImageNo[ImageNo.Length - 1]));
                img_thread.IsBackground = true;
                img_thread.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            imgsr.Close();
            imgWebResponse.Close();
            */
            #endregion

            //20170928新版本
            #region 利用HttpAigility下載
            HtmlWeb imgweb = new HtmlWeb();
            var imgDoc = imgweb.Load((string)add);

            var imgAddress = imgDoc.DocumentNode.SelectSingleNode("//div[@id='i3']/a/img").Attributes["src"].Value;
            Console.WriteLine("圖片網址:" + imgAddress);

            string[] ImageNo = new string[] { };
            ImageNo = Regex.Split((string)imgAddress, ".jpg");
            ImageNo = Regex.Split(ImageNo[ImageNo.Length - 2], "/");

            try
            {
                img_thread = new Thread(() => imgDownloading(imgAddress, bookname, ImageNo[ImageNo.Length - 1]));
                img_thread.IsBackground = true;
                img_thread.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            #endregion

        }

        private void imgDownloading(object add, object bn, object no)
        {
            //lock (this)
            {
                try
                {
                    #region 下載方式1
                    //將網址丟進來，呼叫下載到download資料夾
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create((string)add);
                    req.Timeout = 15000;
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                    Stream imgStream = res.GetResponseStream();
                    FileStream output = new FileStream(download_dic + @"\" + (string)bn + @"\" + no + ".jpg", FileMode.Create, FileAccess.Write);
                    imgStream.CopyTo(output);
                    imgStream.Close();
                    #endregion

                    #region 下載方式2
                    //WebClient myWebClient = new WebClient();
                    //if (!myWebClient.IsBusy)
                    //{   
                    //    ////能確保progressvalue不會跑完
                    //    //myWebClient.DownloadFile((string)add, download_dic + @"\" + (string)bn + @"\" + no + ".jpg");
                    //    //下載完整度比較高
                    //    myWebClient.DownloadFileAsync(new Uri((string)add), download_dic + @"\" + (string)bn + @"\" + no + ".jpg");
                    //}
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}.jpg下載錯誤，原因:{1}，自動重新下載", no, ex.Message);
                    imgDownloading(add, bn, no);
                    downloaded_image--;
                }

                Console.WriteLine("下載了{0}.jpg圖片", no);
                downloaded_image++;
                Console.WriteLine("downloaded_image:"+downloaded_image);
                SetProgressValue(downloaded_image);
                showImg((string)add);
            }
        }

        private void SetProgressMaxValue(int i)
        {
            if (this.progressBar1.InvokeRequired)
            {
                updateProgress maxvalue = new updateProgress(SetProgressMaxValue);
                this.Invoke(maxvalue, new object[] { i });
            }
            else
            {
                this.progressBar1.Maximum = i;
            }
        }
        private void SetProgressValue(int i)
        {
            if (this.progressBar1.InvokeRequired)
            {
                updateProgress value = new updateProgress(SetProgressValue);
                this.Invoke(value, new object[] { i });
            }
            else
            {
                this.progressBar1.Value = i;
            }

            if(i == TotalImagePages)
                btnFuc(true);

        }
        private void showImg(string url)
        {
            if (this.pictureBox1.InvokeRequired)
            {
                updateUrl Address = new updateUrl(showImg);
                this.Invoke(Address, new object[] { url });
            }
            else
            {
                this.pictureBox1.ImageLocation = url;
            }
        }
        private void btnFuc(bool bl)
        {
            if (this.pictureBox1.InvokeRequired)
            {
                changeBool btn = new changeBool(btnFuc);
                this.Invoke(btn, new object[] { bl });
            }
            else
            {
                this.BTN_Conn.Enabled = bl;
            }
        }
    }
}
