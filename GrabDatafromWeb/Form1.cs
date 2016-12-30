using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrabDatafromWeb
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        ///     GrabWebpageData
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GrabWebpageData(string url)
        {
            string appURL = url;
            HttpWebRequest Request = WebRequest.Create(appURL) as HttpWebRequest;
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

            StreamReader srResponseReader = new StreamReader(Response.GetResponseStream(), Encoding.UTF8);
            string strResponseData = srResponseReader.ReadToEnd();
            srResponseReader.Close();
            return strResponseData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            DataTable dt = null;
            Boolean AlreadyCreateHeader = false; ;
            String output = "d://stock//";
            String[] codes = richTextInput.Text.Split(',');
            codes = codes.Select(x => x.TrimEnd('\r', '\n')).ToArray();
            richTextLog.Clear();
            for (int i = 0; i < codes.Length; i++)
            {
                //http://www.set.or.th/set/companyprofile.do?symbol=PDI&language=th&country=TH
                //http://www.set.or.th/set/companyprofile.do?symbol=PDI&ssoPageId=4&language=th&country=TH
                String webdata = GrabWebpageData("http://www.set.or.th/set/companyhighlight.do?symbol="+codes[i].Trim() +"&language=th&country=TH");
                //Convert to Encodeing UTF8

                File.WriteAllText(CheckDirectory(output) + codes[i]+ ".txt", webdata, UTF8Encoding.GetEncoding(0));

                string match = "<table class=\"table table-hover table-info\">";

                int  foundAt = 0;
                //สนใจ tag table ที่เรา Grab ข้อมููลมา
                int count_table = 0;
                while (foundAt != -1 && count_table< 1)
                {
                    foundAt = webdata.IndexOf(match, foundAt + 1);
                    if (foundAt > -1)
                    {
                        count_table++;
                        webdata = webdata.Substring(foundAt);
                        foundAt = 0+match.Length;
                        count_table++;
                    }

                    //Console.WriteLine(foundAt);
                }
                if (foundAt == -1)
                {
                    richTextLog.AppendText("===============================\n");
                    richTextLog.AppendText("Not Found: " + codes[i] + "\n");
                    continue;
                }

                File.WriteAllText(CheckDirectory(output) + codes[i] + "Grab.txt", webdata, UTF8Encoding.GetEncoding(0));

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(webdata);

                foreach (HtmlNode table in doc.DocumentNode.SelectNodes("//table"))
                {
                    richTextLog.AppendText("===============================\n");
                    richTextLog.AppendText("Found: " + codes[i] + "\n");

                    //HtmlNode[] nodes = table.SelectNodes("td").Where(x => x.InnerHtml.Contains("table")).ToArray();
                    HtmlAgilityPack.HtmlDocument content = new HtmlAgilityPack.HtmlDocument();
                    content.LoadHtml(table.InnerHtml);
                    HtmlNode[] detail = content.DocumentNode.SelectNodes("//td").ToArray();
                    //สร้างหัวข้อ
                    if (!AlreadyCreateHeader)
                    {
                        dt = BuildTable(detail, codes);
                        AlreadyCreateHeader = true;
                    }

                    //เอาข้อมูลมาหยอด
                    int currentCol = dt.Columns[codes[i]].Ordinal;

                    HtmlNode[] data = content.DocumentNode.SelectNodes("//tr").ToArray();

                    foreach (HtmlNode entry in data)
                    {
                        HtmlAgilityPack.HtmlDocument trNode = new HtmlAgilityPack.HtmlDocument();
                        trNode.LoadHtml(entry.InnerHtml);
                        HtmlNode[] tdData = trNode.DocumentNode.SelectNodes("//td|//th").ToArray();
                        //หา Column และยัดค่า
                        string s = tdData[0].InnerText.Replace("&nbsp;", "");
                        var aa = dt.AsEnumerable();
                        DataRow foundRow = dt.Rows.Find(s);
                        if (foundRow != null)
                        {
                            foundRow[currentCol] = tdData[1].InnerText.Replace("&nbsp;", "");
                        }
                    }

                    break;
                }
                ResultGridView.DataSource = dt;
                dt.TableName = codes[i];
                dt.WriteXml(CheckDirectory(output) + codes[i] + ".xml");
                
            }
        }

        /// <summary>
        ///     สร้าง Table มาเก็บผลลัพธ์
        /// </summary>
        /// <param name="pHeader"></param>
        /// <param name="pSecurityCode"></param>
        /// <returns></returns>
        private DataTable BuildTable(HtmlNode[] pHeader, String[] pSecurityCode)
        {
            DataTable dt = new DataTable();
            //Create Column 
            for (int i = 0; i <= pSecurityCode.Length; i++)
            {
                if (i == 0)
                {
                    DataColumn column = new DataColumn();
                    column.DataType = System.Type.GetType("System.String");
                    column.ColumnName = "CONTENT";
                    dt.Columns.Add(column);
                    dt.PrimaryKey = new DataColumn[] { column };
                }
                else
                {
                    dt.Columns.Add(pSecurityCode[i - 1]);
                }
            }
            //Initial Some Data
            pHeader = pHeader.Where(x => x.GetAttributeValue("height", "0") == "15" 
                                      || x.GetAttributeValue("height", "0") == "30").ToArray();

            foreach(HtmlNode entry in pHeader)
            {
                DataRow rDetail = dt.NewRow();
                rDetail[0] = entry.InnerText.Replace("&nbsp;", "");
                dt.Rows.Add(rDetail);
            }
            return dt;
        }

        /// <summary>
        ///     ตรวจว่ามี Directory ถ้าไม่มีให้สร้างใหม่เลย
        /// </summary>
        /// <param name="pPath"></param>
        /// <returns></returns>
        private String CheckDirectory(String pPath)
        {
            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(pPath))
                {
                    //richTextLog.AppendText("That path exists already.");
                    return pPath;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(pPath);
                //richTextLog.AppendText("The directory was created successfully at " + Directory.GetCreationTime(pPath) + ".\n");
                return pPath;
            }
            catch(IOException)
            {
                throw;
            }
        }

    }
}
