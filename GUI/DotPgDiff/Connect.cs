using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace BO.DotPgDiff
{
    public partial class Connect : Form
    {
        public Connect()
        {
            InitializeComponent();
            LoadComboBox();
        }

        public Npgsql.NpgsqlConnectionStringBuilder ConnectionString
        {
            get
            {
                return new Npgsql.NpgsqlConnectionStringBuilder {
                    Host = this.host.Text,
                    Port = Int32.Parse(this.port.Text),
                    UserName = this.user.Text,
                    Password = this.password.Text,
                    Database = this.database.Text
                };
            }
        }

        private Exception TryConnect()
        {
            try {
                using (var c = new Query.Loader(this.ConnectionString))
                    return null;
            } catch (Exception e) {
                return e;
            }
        }
        
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (check.Checked)
            {// se il checkbox e' stato segnato allora...
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\UsersXml";
                string pathu = path + "\\users.xml";

                //esiste la cartella?
                if (!ExistsFolderXml(path))
                {
                    //creazione della cartella
                    if (!CreateFolderXml(path))
                    {
                        MessageBox.Show("Errore!\nnon è stato possibile creare la cartella\n" + path);
                    }
                }

                //esiste il file XML?
                if (!ExistFile(pathu))
                {
                    //creazione del file
                    if (!CreateXmlUser(pathu))
                    {
                        MessageBox.Show("Errore!\nnon è stato possibile il file XML");
                    }
                }

                //esiste l'utente?
                if (!ExistUser(host.Text, port.Text, user.Text, password.Text, database.Text, pathu))
                {
                    var rt = this.TryConnect();
                    //se mi collego
                    if (rt == null)
                    {
                        //salvo i dati dell'utente
                        if (!saveUser(host.Text, port.Text, user.Text, password.Text, database.Text, pathu))
                        {
                            MessageBox.Show("non e' stato possibile salvare l'utente indicato");
                        }
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show(rt.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    var rt = this.TryConnect();
                    //se mi collego
                    if (rt == null)
                    {
                        //aggiorno i dati dell'utente
                        if (!refreshUser(string.Format("{0}@{1}@{2}", user.Text, database.Text, host.Text), port.Text, password.Text, pathu))
                        {
                            MessageBox.Show("i nuovi dati dell'utente non sono stati aggiornati nel file\n" + pathu);
                        }
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show(rt.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                var rt = this.TryConnect();
                if (rt == null)
                {
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(rt.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void Connect_Load(object sender, EventArgs e)
        {

        }

        private void host_TextChanged(object sender, EventArgs e)
        {

        }

        #region salvareConnessione
        
        //Verifica l'esistenza della cartella indicata in path
        public static bool ExistsFolderXml(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;  //non è stato possibile creare la cartella...  
            }
        }

        //crea la cartella che avra al suo interno il file XML
        public static bool CreateFolderXml(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                return false;  //non è stato possibile creare la cartella...  
            }
        }

        //verifica l'esistenza del file XML
        public static bool ExistFile(string pathu)
        {
            try
            {
                if (File.Exists(pathu))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        //creazione del file XML sul quale dopo verrano salvati gli utenti
        private static bool CreateXmlUser(string pathu)
        {
            bool crt = false;
            try
            {
                XmlTextWriter write_rec = new XmlTextWriter(pathu, System.Text.Encoding.UTF8);

                write_rec.Formatting = Formatting.Indented;
                //write_rec.Indentation = 2;
                write_rec.WriteStartDocument(false);
                write_rec.WriteStartElement("Users");
                write_rec.WriteEndElement();
                write_rec.WriteEndDocument();
                write_rec.Close();
                crt = true;
            }
            catch (Exception ex)
            {
                crt = false;
            }
            return crt;
        }

        //verifica l'esistenza dell'utente inserito o scelto
        public bool ExistUser(string pHost, string pPort, string pUsername, string pPassword, string pDatabase, string pathu)
        {
            string pString = string.Format("{0}@{1}@{2}", pUsername, pDatabase, pHost);
            bool trovato = false;

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(pathu);

            XmlNodeList usr = xDoc.GetElementsByTagName("Users");
            XmlNodeList list = ((XmlElement)usr[0]).GetElementsByTagName("user");

            foreach (XmlElement nodo in list)
            {
                XmlNodeList nString = nodo.GetElementsByTagName("String");

                //se lo trova assegna il valore true a trovato ed interrompe il ciclo
                if (pString == nString[0].InnerText)
                {
                    trovato = true;
                    break;
                }
            }
            return trovato;
        }

        //salva un utente sul file XML
        public static bool saveUser(string pHost, string pPort, string pUsername, string pPassword, string pDatabase, string pathu)
        {
            XmlDocument XmlDoc;
            XmlNode _node;
            bool rta = false;

            try
            {
                XmlDoc = new XmlDocument();
                XmlDoc.Load(pathu);
                _node = XmlDoc.DocumentElement;

                XmlElement NewUser = XmlDoc.CreateElement("user");
                NewUser.InnerXml = "<Host></Host>" +
                                    "<Port></Port>" +
                                    "<Username></Username>" +
                                    "<Password></Password>" +
                                    "<Database></Database>" +
                                    "<String></String>"; // contenuto del nuovo nodo  

                //NewUser.AppendChild(XmlDoc.CreateWhitespace("\r\n"));
                NewUser["Host"].InnerText = pHost;
                NewUser["Port"].InnerText = pPort;
                NewUser["Username"].InnerText = pUsername;
                NewUser["Password"].InnerText = pPassword;
                NewUser["Database"].InnerText = pDatabase;
                NewUser["String"].InnerText = string.Format("{0}@{1}@{2}", pUsername, pDatabase, pHost);

                _node.InsertAfter(NewUser, _node.LastChild);  // inserisce subito dopo l'ultimo user memorizzato
                XmlTextWriter _wirteRec = new XmlTextWriter(pathu, System.Text.Encoding.UTF8);
                XmlDoc.WriteTo(_wirteRec);
                _wirteRec.Close();
                rta = true;
            }
            catch (Exception ex)
            {
                rta = false; 
            }
            return rta;
        }

        //aggiorna i dati dell'utente data la sua stringa
        public static bool refreshUser(string str, string pPort, string pPassword, string pathu)
        {
            bool rta = false;
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(pathu);
            XmlNodeList usr = xDoc.GetElementsByTagName("Users");
            XmlNodeList list = ((XmlElement)usr[0]).GetElementsByTagName("user");

            foreach (XmlElement nodo in list)
            {
                XmlNodeList nString = nodo.GetElementsByTagName("String");
                if (str == nString[0].InnerText)
                {
                    
                    XmlNodeList nPort = nodo.GetElementsByTagName("Port");
                    XmlNodeList nPassword = nodo.GetElementsByTagName("Password");
                    nPort[0].InnerText = pPort;
                    nPassword[0].InnerText = pPassword;
                    rta = true;
                    break;
                }
            }
            return rta;
        }
        
        #endregion

        #region controlbox
        //carica nella controlbox gli utenti salvati nel file XML se esiste
        private void LoadComboBox()
        {
            //caricare il valore "clean" nel combobox che servira a pulire i campi
            users.Items.Add("clean");
            
            //caricare le stringhe degli utenti se esiste un file che gli contenga
            string pathu = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\UsersXml\\users.xml";
            if (ExistFile(pathu))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(pathu);
                string newcon = users.Text;
                XmlNodeList usr = xDoc.GetElementsByTagName("Users");
                XmlNodeList list = ((XmlElement)usr[0]).GetElementsByTagName("user");

                foreach (XmlElement nodo in list)
                {
                    XmlNodeList nString = nodo.GetElementsByTagName("String");
                    users.Items.Add(nString[0].InnerText);
                }
            }
        }

        //seleziona l'utente dalla controlbox
        private void users_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (users.Text == "clean")
            {
                host.Text = "";
                port.Text = "";
                user.Text = "";
                password.Text = "";
                database.Text = "";
            }
            else
            {
                string pathu = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\UsersXml\\users.xml";
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(pathu);
                string newcon = users.Text;
                XmlNodeList usr = xDoc.GetElementsByTagName("Users");
                XmlNodeList list = ((XmlElement)usr[0]).GetElementsByTagName("user");

                foreach (XmlElement nodo in list)
                {
                    XmlNodeList nString = nodo.GetElementsByTagName("String");
                    if (newcon == nString[0].InnerText)
                    {
                        XmlNodeList nHost = nodo.GetElementsByTagName("Host");
                        XmlNodeList nPort = nodo.GetElementsByTagName("Port");
                        XmlNodeList nUsername = nodo.GetElementsByTagName("Username");
                        XmlNodeList nPassword = nodo.GetElementsByTagName("Password");
                        XmlNodeList nDatabase = nodo.GetElementsByTagName("Database");

                        host.Text = nHost[0].InnerText;
                        port.Text = nPort[0].InnerText;
                        user.Text = nUsername[0].InnerText;
                        password.Text = nPassword[0].InnerText;
                        database.Text = nDatabase[0].InnerText;
                        break;
                    }
                }
            }
        }
        
        #endregion
    }
}
