using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using VDS.RDF;
using VDS.RDF.Query;

using AasxPackageLogic;



namespace AasxCapabilitySearch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class SearchWindow : Window
    {
        //
        string[] connectionsettings;

        //variables for Submodel
        Triples result = null;

        //Internal results
        List<Triples> triple = new List<Triples>();
        public SearchWindow(string[] connectionsettings)
        {
            InitializeComponent();

            stardogConnection.DataContext = StardogConnection.GetStardogConnection(connectionsettings);

            liste.ItemsSource = triple;

            Triples result = this.result;

            this.connectionsettings = connectionsettings;
        }
        public void TextBoxOnClick(object sender, RoutedEventArgs e)
        {
            suchfeld.Text = String.Empty;
        }

        public void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            C_Search c_Search = new C_Search();
            c_Search.connectionsettings = connectionsettings;
            c_Search.setConnect();


            Triples trips = new Triples();
            trips.connectionsettings = connectionsettings;

            if (e.Key == Key.Enter)
            {
                try
                {
                    triple.Clear();
                    liste.Items.Refresh();
                    searchCount.Text = "";

                    SparqlResultSet triples = trips.GetTriples(suchfeld.Text);

                    for (int i = 0; i < triples.Count; i++)
                    {
                        string iri = triples.ElementAt(i).Value("s").ToString();

                        SparqlResultSet temp = c_Search.reasoning(iri);

                        for (int j = 0; j < temp.Count; j++)
                        {
                            string iri2 = temp.ElementAt(j).Value("s").ToString();
                            string description2 = temp.ElementAt(j).Value("o").ToString();
                            string obname2;
                            if (iri.Contains("#"))
                            {
                                obname2 = iri2.Substring(iri2.LastIndexOf("#") + 1);
                            }
                            else if (iri2.Contains("/"))
                            {
                                obname2 = iri2.Substring(iri2.LastIndexOf("/") + 1);
                            }
                            else
                            {
                                obname2 = iri2;
                            }

                            Triples newtriples = new Triples { Name = obname2, IRI = iri2, Description = description2 };

                            bool duplicate = false;
                            foreach (Triples trip in triple)
                            {
                                if (trip.IRI == newtriples.IRI)
                                {
                                    trip.Description += "; " + newtriples.Description;
                                    duplicate = true;
                                }
                            }
                            if (!duplicate)
                            {
                                triple.Add(newtriples);
                            }

                        }
                        searchCount.Text = temp.Count().ToString() + " results found.";
                    }
                    liste.Items.Refresh();
                }
                catch
                {
                    return;
                }
            }
        }

        public void SearchOnClick(object sender, RoutedEventArgs e)
        {
            C_Search c_Search = new C_Search();
            c_Search.connectionsettings = connectionsettings;
            c_Search.setConnect();

            Triples trips = new Triples();
            trips.connectionsettings = connectionsettings;
            
            
            try
            {
                triple.Clear();
                liste.Items.Refresh();
                searchCount.Text = "";

                SparqlResultSet triples = trips.GetTriples(suchfeld.Text);

                for (int i = 0; i < triples.Count; i++)
                {
                    string iri = triples.ElementAt(i).Value("s").ToString();

                    SparqlResultSet temp = c_Search.reasoning(iri);

                    for (int j = 0; j < temp.Count; j++)
                    {
                        string iri2 = temp.ElementAt(j).Value("s").ToString();
                        string description2 = temp.ElementAt(j).Value("o").ToString();
                        string obname2 = "";
                        if (iri.Contains("#"))
                        {
                            obname2 = iri2.Substring(iri2.LastIndexOf("#") + 1);
                        }
                        else if (iri2.Contains("/"))
                        {
                            obname2 = iri2.Substring(iri2.LastIndexOf("/") + 1);
                        }
                        else
                        {
                            obname2 = iri2;
                        }

                        Triples newtriples = new Triples { Name = obname2, IRI = iri2, Description = description2 };

                        bool duplicate = false;
                        foreach (Triples trip in triple)
                        {
                            if (trip.IRI == newtriples.IRI)
                            {
                                trip.Description += "; " + newtriples.Description;
                                duplicate = true;
                            }
                        }
                        if (!duplicate)
                        {
                            triple.Add(newtriples);
                        }
                    }
                    searchCount.Text = temp.Count().ToString() + " results found.";
                }
                liste.Items.Refresh();
            }
            catch
            {
                return;
            }
        }

        public void CancelOnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void AcceptOnClick(object sender, RoutedEventArgs e)
        {
            Triples selected = (Triples) liste.SelectedItem;
            SelectedToGenericForm(selected);
            this.Close();
        }

        public void SelectedToGenericForm(Triples selectedItem)
        {
            this.result = selectedItem;

        }
        
        public Triples GetTriples()
        {
            return this.result;
        }

    }
}
 