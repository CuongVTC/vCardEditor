﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using VCFEditor.View;
using VCFEditor.Model;
using Thought.vCards;
using vCardEditor.Repository;
using vCardEditor.Model;
using System.Drawing;
using System.Collections.Generic;
using vCardEditor.View.Customs;

namespace vCardEditor.View
{
    public partial class MainForm : Form, IMainView
    {
        public event EventHandler<EventArg<FormState>> LoadForm;
        public event EventHandler AddContact;
        public event EventHandler SaveContactsSelected;
        public event EventHandler BeforeOpeningNewFile;
        public event EventHandler DeleteContact;
        public event EventHandler<EventArg<string>> NewFileOpened;
        public event EventHandler ChangeContactsSelected;
        public event EventHandler<EventArg<vCard>> BeforeLeavingContact;
        public event EventHandler<EventArg<string>> FilterTextChanged;
        public event EventHandler TextBoxValueChanged;
        public event EventHandler<EventArg<bool>> CloseForm;
        public event EventHandler<EventArg<string>> ModifyImage;
        public event EventHandler<EventArg<List<vCardDeliveryAddressTypes>>> AddressAdded;
        public event EventHandler<EventArg<List<vCardDeliveryAddressTypes>>> AddressModified;
        public event EventHandler<EventArg<int>> AddressRemoved;
        public event EventHandler ExportImage;
        public event EventHandler CopyTextToClipboardEvent;

        ComponentResourceManager resources;

       
        public int SelectedContactIndex
        {
            get
            {
                if (dgContacts.CurrentCell != null)
                    return dgContacts.CurrentCell.RowIndex;
                else
                    return -1;
            }

        }

        public MainForm()
        {
            InitializeComponent();
            resources = new ComponentResourceManager(typeof(MainForm));
            tbcAddress.AddTab += (sender, e) => AddressAdded?.Invoke(sender, e);
            tbcAddress.RemoveTab += (sender, e) => AddressRemoved?.Invoke(sender, e);
            tbcAddress.ModifyTab += (sender, e) => AddressModified?.Invoke(sender, e);
            tbcAddress.TextChangedEvent += (sender, e) => TextBoxValueChanged?.Invoke(sender, e);
            BuildMRUMenu();

        }
        private void tbsOpen_Click(object sender, EventArgs e)
        {
            NewFileOpened?.Invoke(sender, new EventArg<string>(string.Empty));
        }

        public void DisplayContacts(SortableBindingList<Contact> contacts)
        {
            if (contacts != null)
                bsContacts.DataSource = contacts;

        }

        private void tbsSave_Click(object sender, EventArgs e)
        {
            if (SaveContactsSelected != null)
            {
                //make sure the last changes in the textboxes is saved.
                Validate();
                SaveContactsSelected(sender, e);
            }

        }

        private void tbsNew_Click(object sender, EventArgs e)
        {
            AddContact?.Invoke(sender, e);
        }

        private void dgContacts_SelectionChanged(object sender, EventArgs e)
        {
            if (ChangeContactsSelected != null && dgContacts.CurrentCell != null)
            {
                vCard data = GetvCardFromWindow();
                ChangeContactsSelected(sender, new EventArg<vCard>(data));
            }
            else
                ChangeContactsSelected(sender, new EventArg<vCard>(null));
        }

        private void Value_TextChanged(object sender, EventArgs e)
        {
            TextBoxValueChanged?.Invoke(sender, e);
        }

        public void DisplayContactDetail(vCard card, string FileName)
        {
            if (card == null)
                throw new ArgumentException("card must be valid!");

            Text = string.Format("{0} - vCard Editor", FileName);
            gbContactDetail.Enabled = true;
            gbNameList.Enabled = true;

            SetSummaryValue(firstNameValue, card.GivenName);
            SetSummaryValue(lastNameValue, card.FamilyName);
            SetSummaryValue(middleNameValue, card.AdditionalNames);
            SetSummaryValue(FormattedTitleValue, card.Title);
            SetSummaryValue(FormattedNameValue, card.FormattedName);
            SetSummaryValue(HomePhoneValue, card.Phones.GetFirstChoice(vCardPhoneTypes.Home));
            SetSummaryValue(CellularPhoneValue, card.Phones.GetFirstChoice(vCardPhoneTypes.Cellular));
            SetSummaryValue(WorkPhoneValue, card.Phones.GetFirstChoice(vCardPhoneTypes.Work));
            SetSummaryValue(EmailAddressValue, card.EmailAddresses.GetFirstChoice(vCardEmailAddressType.Internet));
            SetSummaryValue(PersonalWebSiteValue, card.Websites.GetFirstChoice(vCardWebsiteTypes.Personal));
            SetAddressesValues(card);
            SetPhotoValue(card.Photos);

        }

        public void ClearContactDetail()
        {
            gbContactDetail.Enabled = false;
            gbNameList.Enabled = false;

            SetSummaryValue(firstNameValue, string.Empty);
            SetSummaryValue(lastNameValue, string.Empty);
            SetSummaryValue(middleNameValue, string.Empty);
            SetSummaryValue(FormattedTitleValue, string.Empty);
            SetSummaryValue(FormattedNameValue, string.Empty);
            SetSummaryValue(HomePhoneValue, string.Empty);
            SetSummaryValue(CellularPhoneValue, string.Empty);
            SetSummaryValue(WorkPhoneValue, string.Empty);
            SetSummaryValue(EmailAddressValue, string.Empty);
            SetSummaryValue(PersonalWebSiteValue, string.Empty);
            SetAddressesValues(new vCard());
            SetPhotoValue(new vCardPhotoCollection());

        }

        private void SetSummaryValue(StateTextBox valueLabel, string value)
        {
            if (valueLabel == null)
                throw new ArgumentNullException("valueLabel");

            //Clear textbox if value is empty!
            valueLabel.Text = value;
            valueLabel.oldText = value;
        }

        private void SetSummaryValue(StateTextBox valueLabel, vCardEmailAddress email)
        {
            valueLabel.Text = string.Empty;
            if (email != null)
                SetSummaryValue(valueLabel, email.Address);
        }

        private void SetSummaryValue(StateTextBox valueLabel, vCardPhone phone)
        {
            valueLabel.Text = string.Empty;
            if (phone != null)
                SetSummaryValue(valueLabel, phone.FullNumber);

        }

        private void SetSummaryValue(StateTextBox valueLabel, vCardWebsite webSite)
        {
            valueLabel.Text = string.Empty;
            if (webSite != null)
                SetSummaryValue(valueLabel, webSite.Url.ToString());
        }

        void SetPhotoValue(vCardPhotoCollection photos)
        {
            if (photos.Any())
            {
                var photo = photos[0];
                try
                {
                    // Get the bytes of the photo if it has not already been loaded.
                    if (!photo.IsLoaded)
                        photo.Fetch();

                    PhotoBox.Image = photo.GetBitmap();
                }
                catch
                {
                    //Empty image icon instead.
                    PhotoBox.Image = (Image)resources.GetObject("PhotoBox.Image");
                }
            }
            else
                PhotoBox.Image = (Image)resources.GetObject("PhotoBox.Image");

        }
        private void SetAddressesValues(vCard card)
        {
            tbcAddress.SetAddresses(card);
        }

        private void tbsDelete_Click(object sender, EventArgs e)
        {
            if (DeleteContact != null)
            {
                //The user can check a box without leaving the cell, calling the EndEdit will cause the 
                //grid to commit the changes.
                dgContacts.EndEdit();
                DeleteContact(sender, e);
            }
        }

        private void tbsAbout_Click(object sender, EventArgs e)
        {
            new AboutDialog().ShowDialog();
        }

        private void textBoxFilter_TextChanged(object sender, EventArgs e)
        {
            //Save before leaving contact.
            BeforeLeavingContact?.Invoke(sender, new EventArg<vCard>(GetvCardFromWindow()));

            FilterTextChanged?.Invoke(sender, new EventArg<string>(textBoxFilter.Text));
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            textBoxFilter.Text = string.Empty;
        }

       
        private vCard GetvCardFromWindow()
        {
            vCard card = new vCard
            {

                Title = FormattedTitleValue.Text,
                FormattedName = FormattedNameValue.Text,
                GivenName = firstNameValue.Text,
                AdditionalNames = middleNameValue.Text,
                FamilyName = lastNameValue.Text,

            };

            if (!string.IsNullOrEmpty(HomePhoneValue.Text))
                card.Phones.Add(new vCardPhone(HomePhoneValue.Text, vCardPhoneTypes.Home));
            if (!string.IsNullOrEmpty(CellularPhoneValue.Text))
                card.Phones.Add(new vCardPhone(CellularPhoneValue.Text, vCardPhoneTypes.Cellular));
            if (!string.IsNullOrEmpty(WorkPhoneValue.Text))
                card.Phones.Add(new vCardPhone(WorkPhoneValue.Text, vCardPhoneTypes.Work));
            if (!string.IsNullOrEmpty(this.EmailAddressValue.Text))
                card.EmailAddresses.Add(new vCardEmailAddress(this.EmailAddressValue.Text));
            if (!string.IsNullOrEmpty(this.PersonalWebSiteValue.Text))
                card.Websites.Add(new vCardWebsite(this.PersonalWebSiteValue.Text));
            

            tbcAddress.getDeliveryAddress(card);

            return card;
        }

        private void dgContacts_RowLeave(object sender, DataGridViewCellEventArgs e)
        {
            vCard data = GetvCardFromWindow();
            BeforeLeavingContact?.Invoke(sender, new EventArg<vCard>(data));
        }

        private void miQuit_Click(object sender, EventArgs e)
        {
            Close();
        }


        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (FileList.Count() > 1)
            {
                MessageBox.Show("Only one file at the time!");
                return;
            }

            NewFileOpened(sender, new EventArg<string>(FileList[0]));

        }

        private void BuildMRUMenu()
        {
            recentFilesMenuItem.DropDownItemClicked += (s, e) =>
            {
                var evt = new EventArg<string>(e.ClickedItem.Text);
                NewFileOpened(s, evt);
            };

            UpdateMRUMenu(ConfigRepository.Instance.Paths);

        }

        public void UpdateMRUMenu(FixedList MostRecentFilesList)
        {
            if (MostRecentFilesList == null || MostRecentFilesList.IsEmpty())
                return;

            recentFilesMenuItem.DropDownItems.Clear();
            for (int i = 0; i < MostRecentFilesList._innerList.Count; i++)
                recentFilesMenuItem.DropDownItems.Add(MostRecentFilesList[i]);

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var evt = new EventArg<bool>(false);
            CloseForm?.Invoke(sender, evt);

            e.Cancel = evt.Data;

        }

        public bool AskMessage(string msg, string caption)
        {
            bool result = true; // true == yes

            DialogResult window = MessageBox.Show(msg, caption, MessageBoxButtons.YesNo);

            if (window != DialogResult.No)
                result = false;

            return result;
        }

        private void miConfig_Click(object sender, EventArgs e)
        {
            new ConfigDialog().ShowDialog();
        }

        public void DisplayMessage(string msg, string caption)
        {
            MessageBox.Show(msg, caption);
        }
        public string DisplayOpenDialog(string filter = "")
        {
            string filename = string.Empty;
            openFileDialog.Filter = filter;

            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
                filename = openFileDialog.FileName;

            return filename;
        }

        public string DisplaySaveDialog(string filename)
        {

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = filename;

            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
                filename = saveFileDialog.FileName;

            return filename;
        }
        private void PhotoBox_Click(object sender, EventArgs e)
        {
            if (ModifyImage != null)
            {
                var fileName = DisplayOpenDialog();
                if (!string.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        PhotoBox.Image = new Bitmap(fileName);
                        var evt = new EventArg<string>(fileName);
                        ModifyImage(sender, evt);
                    }
                    catch (ArgumentException)
                    {
                        MessageBox.Show($"Invalid file! : {fileName}");
                    }

                }

            }

        }

        private void btnRemoveImage_Click(object sender, EventArgs e)
        {
            PhotoBox.Image = (Image)resources.GetObject("PhotoBox.Image");
            //Remove image from vcf
            ModifyImage(sender, new EventArg<string>(""));
        }

        private void btnExportImage_Click(object sender, EventArgs e)
        {
            ExportImage?.Invoke(sender, e);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyTextToClipboardEvent?.Invoke(sender, e);
        }

        public void SendTextToClipBoard(string text)
        {
            Clipboard.SetText(text);
        }

        private void dgContacts_CellContextMenuStripNeeded(object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                e.ContextMenuStrip = contextMenuStrip1;
            }
            
        }

        private void modifiyColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Columns> Columns = GetListColumnsForDataGrid();

            var dialog = new ColumnsDialog(Columns);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ToggleAllColumnsToInvisible();
                ToggleOnlySelected(dialog.Columns);

            }
        }

        private List<Columns> GetListColumnsForDataGrid()
        {
            List<Columns> Columns = new List<Columns>();
            for (int i = 2; i < dgContacts.Columns.Count; i++)
            {
                if (dgContacts.Columns[i].Visible)
                {
                    var name = dgContacts.Columns[i].Name;
                    var enumType = (Columns)Enum.Parse(typeof(Columns), name, true);
                    Columns.Add(enumType);
                }

            }

            return Columns;
        }

        private void ToggleOnlySelected(List<Columns> columns)
        {
            foreach (var item in columns)
            {
                switch (item)
                {
                    case Columns.FamilyName:
                        dgContacts.Columns["FamilyName"].Visible = true;
                        break;
                    case Columns.Cellular:
                        dgContacts.Columns["Cellular"].Visible = true;
                        break;
                }
            }
        }

        private void ToggleAllColumnsToInvisible()
        {
            for (int i = 2; i < dgContacts.Columns.Count; i++)
            {
                dgContacts.Columns[i].Visible = false;
            }
        }

        public FormState GetFormState()
        {

            return new FormState
            {
                Columns = GetListColumnsForDataGrid(),
                X = Location.X,
                Y = Location.Y,
                Height = Size.Height,
                Width = Size.Width,
                splitterPosition = splitContainer1.SplitterDistance
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var evt = new EventArg<FormState>(new FormState());
            LoadForm?.Invoke(sender, evt);

            //TODO: Better way to check if state was serialised!
            var state = evt.Data;
            if (state.Width != 0 && state.Height != 0)
            {
                Size = new Size(state.Width, state.Height);
                Location = new Point(state.X , state.Y);
                splitContainer1.SplitterDistance = state.splitterPosition;
                if (state.Columns != null)
                {
                    ToggleOnlySelected(state.Columns);
                }
            }
            
            
        }
    }
}
