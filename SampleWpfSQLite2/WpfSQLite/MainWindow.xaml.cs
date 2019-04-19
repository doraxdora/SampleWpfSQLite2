using log4net;
using System;
using System.Data.Linq;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            InitializeComponent();
            // SampleDb.sqlite を作成（存在しなければ）
            using (var conn = new SQLiteConnection("Data Source=SampleDb.sqlite"))
            {
                // データベースに接続
                conn.Open();
                // コマンドの実行
                using (var command = conn.CreateCommand())
                {
                    // テーブルが存在しなければ作成する
                    // 種別マスタ
                    StringBuilder sb = new StringBuilder();
                    sb.Append("CREATE TABLE IF NOT EXISTS MSTKIND (");
                    sb.Append("  KIND_CD NCHAR NOT NULL");
                    sb.Append("  , KIND_NAME NVARCHAR");
                    sb.Append("  , primary key (KIND_CD)");
                    sb.Append(")");

                    command.CommandText = sb.ToString();
                    command.ExecuteNonQuery();

                    // 猫テーブル
                    sb.Clear();
                    sb.Append("CREATE TABLE IF NOT EXISTS TBLCAT (");
                    sb.Append("  NO INT NOT NULL");
                    sb.Append("  , NAME NVARCHAR NOT NULL");
                    sb.Append("  , SEX NVARCHAR NOT NULL");
                    sb.Append("  , AGE INT DEFAULT 0 NOT NULL");
                    sb.Append("  , KIND_CD NCHAR DEFAULT 0 NOT NULL");
                    sb.Append("  , FAVORITE NVARCHAR");
                    sb.Append("  , primary key (NO)");
                    sb.Append(")");

                    command.CommandText = sb.ToString();
                    command.ExecuteNonQuery();

                    // 種別マスタを取得してコンボボックスに設定する
                    using (DataContext con = new DataContext(conn))
                    {
                        // データを取得
                        Table<Kind> mstKind = con.GetTable<Kind>();
                        IQueryable<Kind> result = from x in mstKind orderby x.KindCd select x;

                        // 最初の要素は「指定なし」とする
                        Kind empty = new Kind();
                        empty.KindCd = "";
                        empty.KindName = "指定なし";
                        var list = result.ToList();
                        list.Insert(0, empty);

                        // コンボボックスに設定
                        this.search_kind.ItemsSource = list;
                        this.search_kind.DisplayMemberPath = "KindName";
                    }

                }
                // 切断
                conn.Close();
            }
        }

        /// <summary>
        /// 検索ボタンクリックイベント.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void search_button_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("検索ボタンクリック");
            searchData();
        }

        /// <summary>
        /// データ検索処理.
        /// </summary>
        private void searchData()
        {
            using (var conn = new SQLiteConnection("Data Source=SampleDb.sqlite"))
            {
                conn.Open();

                // 猫データマスタを取得してコンボボックスに設定する
                using (DataContext con = new DataContext(conn))
                {
                    String searchName = this.search_name.Text;
                    String searchKind = (this.search_kind.SelectedValue as Kind).KindCd;

                    // データを取得
                    Table<Cat> tblCat = con.GetTable<Cat>();

                    // サンプルなので適当に組み立てる
                    IQueryable<Cat> result;
                    if (searchKind == "")
                    {
                        // 名前は前方一致のため常に条件していしても問題なし
                        result = from x in tblCat
                                 where x.Name.StartsWith(searchName)
                                 orderby x.No
                                 select x;
                    }
                    else
                    {
                        result = from x in tblCat
                                 where x.Name.StartsWith(searchName) & x.Kind == searchKind
                                 orderby x.No
                                 select x;

                    }
                    this.dataGrid.ItemsSource = result.ToList();

                }

                conn.Close();
            }
        }

        /// <summary>
        /// 追加ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void add_button_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("追加ボタンクリック");

            // 接続
            using (var conn = new SQLiteConnection("Data Source=SampleDb.sqlite"))
            {
                conn.Open();

                // データを追加する
                using (DataContext context = new DataContext(conn))
                {
                    // 対象のテーブルオブジェクトを取得
                    var table = context.GetTable<Cat>();
                    // データ作成
                    Cat cat = new Cat();
                    cat.No = 5;
                    cat.Name = "こなつ";
                    cat.Sex = "♀";
                    cat.Age = 7;
                    cat.Kind = "01";
                    cat.Favorite = "布団";
                    // データ追加
                    table.InsertOnSubmit(cat);
                    // DBの変更を確定
                    context.SubmitChanges();
                }
                conn.Close();
            }
            // データ再検索
            searchData();
            MessageBox.Show("データを追加しました。");
        }

        /// <summary>
        /// 更新ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void upd_button_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("更新ボタンクリック");

            // 選択チェック
            if (this.dataGrid.SelectedItem == null)
            {
                MessageBox.Show("更新対象を選択してください。");
                return;
            }

            // 接続
            using (var conn = new SQLiteConnection("Data Source=SampleDb.sqlite"))
            {
                conn.Open();

                // データを追加する
                using (DataContext context = new DataContext(conn))
                {
                    // 対象のテーブルオブジェクトを取得
                    var table = context.GetTable<Cat>();
                    // 選択されているデータを取得
                    Cat cat = this.dataGrid.SelectedItem as Cat;
                    // テーブルから対象のデータを取得
                    var target = table.Single(x => x.No == cat.No);
                    // データ変更
                    target.Favorite = "高いところ";
                    // DBの変更を確定
                    context.SubmitChanges();
                }
                conn.Close();
            }

            // データ再検索
            searchData();

            MessageBox.Show("データを更新しました。");
        }

        /// <summary>
        /// 削除ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void del_button_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("追加ボタンクリック");

            // 選択チェック
            if (this.dataGrid.SelectedItem == null)
            {
                MessageBox.Show("削除対象を選択してください。");
                return;
            }

            // 接続
            using (var conn = new SQLiteConnection("Data Source=SampleDb.sqlite"))
            {
                conn.Open();

                // データを削除する
                using (DataContext context = new DataContext(conn))
                {
                    // 対象のテーブルオブジェクトを取得
                    var table = context.GetTable<Cat>();
                    // 選択されているデータを取得
                    Cat cat = this.dataGrid.SelectedItem as Cat;
                    // テーブルから対象のデータを取得
                    var target = table.Single(x => x.No == cat.No);
                    // データ削除
                    table.DeleteOnSubmit(target);
                    // DBの変更を確定
                    context.SubmitChanges();
                }
                conn.Close();
            }

            // データ再検索
            searchData();

            MessageBox.Show("データを削除しました。");
        }
    }
}