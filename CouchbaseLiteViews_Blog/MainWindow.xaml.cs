using Couchbase.Lite;
using Couchbase.Lite.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CouchbaseLiteViews_Blog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string DB_NAME = "sampledb";
        private const string TAG = "sampledb";

        private Manager _manager;
        private Database _database;

        private DirectoryInfo _dbPath;
        private static ILogger Logger;

        public event PropertyChangedEventHandler PropertyChanged;


        private string _docuemntId;
        public string DocumentId
        {
            get { return _docuemntId; }
            set
            {
                _docuemntId = value;
                NotifyPropertyChanged();
            }
        }

        private string _documentText;
        public string DocumentText
        {
            get { return _documentText; }
            set
            {
                _documentText = value;
                NotifyPropertyChanged();
            }
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set
            {
                _city = value;
                NotifyPropertyChanged();
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            DocumentId = "SomeDocumentId";
            DocumentText = JObject.Parse(@"{'name': 'Roi Katz', 'City':'London' }").ToString();

            InitializeDatabase();
            GenerateViews();
        }

        private void GenerateViews()
        {
            var docsByCity = _database.GetView("docs_by_city");
            docsByCity.SetMap((doc, emit) =>
            {
                if (doc.ContainsKey("City"))
                {
                    emit(doc["City"], doc);
                }
            }, "1");
        }

        private void InitializeDatabase()
        {
            {
                Debug.WriteLine("Initializeing CouchbaseLite");
                try
                {
                    Log.SetDefaultLoggerWithLevel(SourceLevels.Verbose);

                    _dbPath = new DirectoryInfo(Environment.CurrentDirectory);
                    Log.I(TAG, "Creating manager in " + _dbPath);
                    Debug.WriteLine("Creating manager in " + _dbPath);

                    _manager = new Manager(_dbPath, ManagerOptions.Default);
                    Debug.WriteLine("Creating database " + DB_NAME);
                    Log.I(TAG, "Creating database " + DB_NAME);

                    _database = _manager.GetDatabase(DB_NAME);

                }
                catch (Exception ex)
                {
                    var msg = "Could not load database in path " + _dbPath.ToString();
                    MessageBox.Show(msg);
                }
            }
        }

        private void InsertDocumentClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocumentId))
            {
                MessageBox.Show("Please specify ID");
                return;
            }

            InsertDocument(DocumentId, DocumentText);

        }

        private void InsertDocument(string documentId, string documentText)
        {
            var document = _database.GetDocument(documentId);

            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(documentText);
            var revision = document.PutProperties(properties);
        }

        private void GetDocumentClick(object sender, RoutedEventArgs e)
        {
            var doc = _database.GetDocument(DocumentId);

            DocumentText = JsonConvert.SerializeObject(doc.Properties, Formatting.Indented);
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }

        private void InsertSomeDataClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Press Yes to insert some data (10 docs)!", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var count = _database.DocumentCount;
                string[] cities = { "London", "New York", "Tel Aviv" };
                var rnd = new Random();
                for (int i = 0; i < 10; i++)
                {
                    var id = "document" + (i + count);
                    var cityIndex = rnd.Next(0, 3);

                    var properties = new Dictionary<string, string>();
                    properties.Add("name", "Roi Katz");
                    properties.Add("City", cities[cityIndex]);
                    
                    var doc = JsonConvert.SerializeObject(properties);
                    InsertDocument(id, doc);
                }

                MessageBox.Show("10 Records inserted");
            }

        }

        private void GetByCityClick(object sender, RoutedEventArgs e)
        {
            var docsByCity = _database.GetView("docs_by_city");
            var query = docsByCity.CreateQuery();
            
            query.StartKey = City;
            query.EndKey = City + " ";

            var queryResults = query.Run();

            MessageBox.Show(string.Format("{0} documents has been retrieved for that query", queryResults.Count));

            if (queryResults.Count == 0) return;

            var documents = queryResults.Select(result => JsonConvert.SerializeObject(result.Value, Formatting.Indented)).ToArray();
            var commaSeperaterdDocs = "[" + string.Join(",", documents) + "]";

            DocumentText = commaSeperaterdDocs;
        }
    }
}
