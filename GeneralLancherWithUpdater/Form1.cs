﻿using System;
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
using System.Text.RegularExpressions;

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
                        case "NewsURL":
                            LoadNews(parts[1]);
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

        private void LoadNews(string url)
        {
            string result = "";
            using (WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                try
                {
                    result = client.DownloadString(url);
                    richTextBox1.Text = result;
                }
                catch (Exception ex)
                {
                    richTextBox1.Text = "ニュースURLが無効です。";
                    //richTextBox1.Text = ex.Message;
                    return;
                }
            }
            return;
        }

        private void GameStart(string downloadDir, string exepath)
        {
            textBox1.Text = "ゲーム起動中……";

            if (File.Exists(Path.Combine(downloadDir, "temp.zip")))
            {
                try
                {
                    File.Delete(Path.Combine(downloadDir, "temp.zip"));
                }
                catch (Exception ex)
                {
                    // 例外が発生した場合、エラーメッセージを表示して処理を終了する
                    textBox1.Text = "Error: " + ex.ToString();
                    return;
                }
            }

            // ファイルを実行する
            Process.Start(exepath);
            //このランチャーを終了する。
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string currentdir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            // ダウンロード先のディレクトリ
            string downloadDir = currentdir;

            // textBoxに入力されたパスを取得
            string exepath = Path.Combine(currentdir, label2.Text, label3.Text);

            // 宛先指定がちゃんとされているか確認
            if (label7.Text != "" && label9.Text != "")
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += WebClient_DownloadProgressChanged; // ダウンロード進捗イベントの追加
                    client.DownloadFileCompleted += WebClient_DownloadFileCompleted; // ダウンロード完了イベントの追加

                    textBox1.Text = "バージョンチェック中……";
                    string chkurl = label7.Text;

                    //証明書を無視。本番ではこのコードは外す。
                    //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    string result = "";

                    //アクセスしてバージョンをチェック
                    try
                    {
                        result = client.DownloadString(chkurl);
                        //改行以降は削除
                        result = Regex.Replace(result, @"(\r\n|\r|\n).*", "");
                        //textBox1.Text = result;
                    }
                    catch (Exception ex)
                    {
                        textBox1.Text = "バージョンチェックURLが無効です。";
                        return;
                    }

                    //URLから落としてきたバージョンと、iniに書いてあるバージョンが一致すれば起動
                    if (result == label5.Text && File.Exists(exepath))
                    {
                        GameStart(downloadDir, exepath);
                    }
                    else
                    {
                        textBox1.Text = "新バージョンが見つかりました。アップデート中……";
                        button1.Enabled = false;    //これ以上ボタンを押せないようにする

                        // ダウンロードするZipファイルのURL
                        string zipUrl = label9.Text;

                        // 解凍先のディレクトリ
                        string extractDir = currentdir;

                        try
                        {
                            // Zipファイルをダウンロードして保存
                            client.DownloadFileAsync(new Uri(zipUrl), Path.Combine(downloadDir, "temp.zip")); // 非同期ダウンロードを開始
                        }
                        catch (Exception ex)
                        {
                            textBox1.Text = "ダウンロードURLが無効です。iniファイルを確認して再起動してください。";
                            return;
                        }
                    }
                }
            }
            else
            {
                // ファイルが存在しない場合はエラーメッセージを表示する
                textBox1.Text = exepath + "のファイルが見つかりません。";
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // ダウンロード進捗を表示
            textBox1.Text = string.Format("ダウンロード中... {0}% 完了", e.ProgressPercentage);
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                textBox1.Text = "ダウンロードに失敗しました：" + e.Error.Message;
                return;
            }

            string currentdir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string downloadDir = currentdir;
            string extractDir = currentdir;

            try
            {
                // Zipファイルを解凍して上書き
                using (var zipArchive = ZipFile.OpenRead(Path.Combine(downloadDir, "temp.zip")))
                {
                    MyZipFileExtensions.ExtractToDirectory(zipArchive, extractDir, true);
                }
            }
            catch (Exception ex)
            {
                // 例外が発生した場合、エラーメッセージを表示して処理を終了する
                textBox1.Text = "Error: " + ex.ToString();
                return;
            }
            finally
            {
                // Zipファイルを削除
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

            // ダウンロード完了後、ゲーム起動
            string exepath = Path.Combine(currentdir, label2.Text, label3.Text);
            if (File.Exists(exepath))
            {
                GameStart(downloadDir, exepath);
            }
            else
            {
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
