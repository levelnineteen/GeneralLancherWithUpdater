using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net;
using System.Threading;

namespace GeneralLancherWithUpdater
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
            if (FirstIniCHK())
            {
                SecondIniCHK();
            }

        }

        private bool FirstIniCHK()
        {
            string iniFilePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "settings.ini"); // iniファイルのパスを取得
            List<string> iniLines = new List<string>();

            //同じディレクトリのiniファイル確認
            if (File.Exists(iniFilePath))
            {
                iniLines = File.ReadAllLines(iniFilePath).ToList<string>();
                foreach (string line in iniLines)
                {
                    string[] parts = line.Split('=');
                    switch (parts[0])
                    {
                        case "Path":
                            label2.Text = parts[1];
                            break;
                        default:
                            break;
                    }
                }
                return true;
            }
            else
            {
                textBox1.Text = "iniファイルが見つかりません。";
                return false;
            }
        }

        private bool SecondIniCHK()
        {
            string iniFilePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), label2.Text, "settings.ini"); // iniファイルのパスを取得
            List<string> iniLines = new List<string>();

            //同じディレクトリのiniファイル確認
            if (File.Exists(iniFilePath))
            {
                iniLines = File.ReadAllLines(iniFilePath).ToList<string>();
                foreach (string line in iniLines)
                {
                    string[] parts = line.Split('=');
                    switch (parts[0])
                    {
                        case "Exe":
                            label3.Text = parts[1];
                            break;
                        case "Version":
                            label5.Text = parts[1];
                            break;
                        case "CheckURL":
                            label7.Text = parts[1];
                            break;
                        case "DownloadURL":
                            label9.Text = parts[1];
                            break;
                        default:
                            break;
                    }
                }
                return true;
            }
            else
            {
                textBox1.Text = "対象フォルダにiniファイルが見つかりません。";
                return false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string currentdir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            // textBoxに入力されたパスを取得
            string exepath = Path.Combine(currentdir, label2.Text, label3.Text);

            // ファイルが存在するかどうかを確認
            if (File.Exists(exepath))
            {
                using (WebClient client = new WebClient())
                {
                    textBox1.Text = "バージョンチェック中……";
                    string chkurl = label7.Text;

                    //証明書を無視。本番ではこのコードは外す。
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    //アクセスしてバージョンをチェック
                    string result = client.DownloadString(chkurl);
                    textBox1.Text = result;

                    //URLから落としてきたバージョンと、iniに書いてあるバージョンが一致すれば起動
                    if (result == label5.Text)
                    {
                        textBox1.Text = "ゲーム起動中……";
                        // ファイルを実行する
                        Process.Start(exepath);
                        //このランチャーを終了する。
                        Application.Exit();
                    }
                    else
                    {
                        textBox1.Text = "新バージョンが見つかりました。アップデート中……";

                        // ダウンロードするZipファイルのURL
                        string zipUrl = label9.Text;

                        // ダウンロード先のディレクトリ
                        string downloadDir = currentdir;

                        // 解凍先のディレクトリ
                        string extractDir = currentdir;

                        try
                        {
                            // Zipファイルをダウンロードして保存
                            using (WebClient client2 = new WebClient())
                            {
                                client2.DownloadFile(zipUrl, Path.Combine(downloadDir, "temp.zip"));
                            }

                            // Zipファイルを解凍して上書き。このZipファイルにはiniファイルも含める。
                            MyZipFileExtensions.ExtractToDirectory(ZipFile.OpenRead(Path.Combine(downloadDir, "temp.zip")), extractDir, true);


                        }
                        catch (Exception ex)
                        {
                            // 例外が発生した場合、エラーメッセージを表示して処理を終了する
                            textBox1.Text = "Error: " + ex.ToString();
                            return;
                        }
                        finally
                        {
                            //Zipファイルを削除
                            try
                            {
                                File.Delete(Path.Combine(downloadDir, "temp.zip"));
                            }
                            catch (IOException)
                            {
                                textBox1.Text = "展開中……";
                                Thread.Sleep(1000);
                            }
                        }

                        textBox1.Text = "アップデート完了。ゲーム起動中……";
                        // ファイルを実行する
                        Process.Start(exepath);
                        //このランチャーを終了する。
                        Application.Exit();
                    }
                }
            }
            else
            {
                // ファイルが存在しない場合はエラーメッセージを表示する
                textBox1.Text = exepath + "のファイルが見つかりません。";
            }
        }
    }

    ///https://notshown.hatenablog.jp/entry/2017/02/15/090908　より
    /// <summary>
    /// ZIP拡張クラス。
    /// </summary>
    public static class MyZipFileExtensions
    {
        /// <summary>
        /// エントリーがディレクトリかどうか取得する。
        /// </summary>
        /// <param name="entry">ZIPアーカイブエントリー</param>
        /// <returns></returns>
        public static bool IsDirectory(this ZipArchiveEntry entry)
        {
            return string.IsNullOrEmpty(entry.Name);
        }

        /// <summary>
        /// ZIPアーカイブ内のすべてのファイルを特定のフォルダに解凍する。
        /// </summary>
        /// <param name="source">ZIPアーカイブ</param>
        /// <param name="destinationDirectoryName">解凍先ディレクトリ。</param>
        /// <param name="overwrite">上書きフラグ。ファイルの上書きを行う場合はtrue。</param>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, bool overwrite)
        {
            foreach (var entry in source.Entries)
            {
                var fullPath = Path.Combine(destinationDirectoryName, entry.FullName);
                if (entry.IsDirectory())
                {
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                }
                else
                {
                    if (overwrite)
                    {
                        entry.ExtractToFile(fullPath, true);
                    }
                    else
                    {
                        if (!File.Exists(fullPath))
                        {
                            entry.ExtractToFile(fullPath, true);
                        }
                    }
                }
            }
        }
    }
}
